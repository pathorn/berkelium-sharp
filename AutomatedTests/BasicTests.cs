using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Berkelium.Managed;
using System.IO;
using System.Reflection;
using System.Web;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AutomatedTests {
    public class Holder<T> {
        public T Value;
    }

    public class BasicFixture : ITestFixture {
        public const string UnicodeText = "κόσμε";
        public byte[] TemporaryBuffer;
        public Context Context;

        public void Setup () {
            Context = Context.Create();
            BerkeliumSharp.Update();
        }

        public void Teardown () {
            Context.Dispose();
            Context = null;
            BerkeliumSharp.Update();
        }

        public unsafe Bitmap HandlePaint (Window window, string outputFilename) {
            var wid = window.Widget;
            var windowBitmap = new Bitmap(wid.Rect.Width, wid.Rect.Height, PixelFormat.Format32bppArgb);

            window.Paint += delegate(Window w, IntPtr sourceBuffer, Rect rect, int dx, int dy, Rect scrollRect) {
                BitmapData sourceData;
                var clientRect = new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);

                if (dx != 0 || dy != 0) {
                    var sourceRect = new Rectangle(scrollRect.Left, scrollRect.Top, scrollRect.Width, scrollRect.Height);
                    var destRect = sourceRect;
                    destRect.X += dx;
                    destRect.Y += dy;

                    // We want to only draw the overlapping portion of the scrolled and unscrolled
                    //  rectangles, since the scrolled rectangle is probably partially offscreen
                    var overlap = Rectangle.Intersect(destRect, sourceRect);

                    // We need to handle scrolling to the left
                    if (destRect.Left < 0) {
                        sourceRect.X -= destRect.Left;
                        destRect.X = 0;
                    }
                    // And upward
                    if (destRect.Top < 0) {
                        sourceRect.Y -= destRect.Top;
                        destRect.Y = 0;
                    }

                    destRect.Width = sourceRect.Width = overlap.Width;
                    destRect.Height = sourceRect.Height = overlap.Height;

                    // If the clipping calculations resulted in a rect that contains zero pixels, 
                    //  don't bother trying to do the blit.
                    if ((sourceRect.Width > 0) && (sourceRect.Height > 0)) {
                        sourceData = windowBitmap.LockBits(
                            sourceRect, ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppRgb
                        );

                        int totalSize = sourceData.Stride * sourceData.Height;

                        if ((TemporaryBuffer == null) || (totalSize > TemporaryBuffer.Length))
                            TemporaryBuffer = new byte[totalSize];

                        Marshal.Copy(sourceData.Scan0, TemporaryBuffer, 0, totalSize);
                        windowBitmap.UnlockBits(sourceData);

                        fixed (byte* ptr = &(TemporaryBuffer[0])) {
                            sourceData.Scan0 = new IntPtr(ptr);

                            var destData = windowBitmap.LockBits(
                                destRect, ImageLockMode.WriteOnly | ImageLockMode.UserInputBuffer,
                                PixelFormat.Format32bppRgb, sourceData
                            );

                            windowBitmap.UnlockBits(destData);
                        }

                        windowBitmap.Save(outputFilename, ImageFormat.Png);
                    }
                }

                // If we get a paint event after a resize, the rect can be larger than the buffer.
                if ((clientRect.Right > windowBitmap.Width) || (clientRect.Bottom > windowBitmap.Height))
                    return;

                // This probably looks wrong when you first read it, but we're filling
                //  out a BitmapData structure to represent the source buffer.
                sourceData = new BitmapData();
                sourceData.Width = clientRect.Width;
                sourceData.Height = clientRect.Height;
                sourceData.PixelFormat = PixelFormat.Format32bppRgb;
                sourceData.Stride = clientRect.Width * 4;
                sourceData.Scan0 = sourceBuffer;

                // Sometimes this can fail if we process an old paint event after we
                //  request a resize, so we just eat the exception in that case.
                // Yes, this is terrible.

                try {
                    // This oddball form of LockBits performs a write to the bitmap's
                    //  internal buffer by copying from another BitmapData you pass in.
                    // In this case we're passing in the source buffer.
                    var bd = windowBitmap.LockBits(
                        clientRect,
                        ImageLockMode.WriteOnly | ImageLockMode.UserInputBuffer,
                        PixelFormat.Format32bppRgb, sourceData
                    );

                    // For some reason we still have to unlock the bits afterward.
                    windowBitmap.UnlockBits(bd);

                } catch {
                }

                windowBitmap.Save(outputFilename, ImageFormat.Png);
            };

            return windowBitmap;
        }

        public string MakeDataUrl (string pageText) {
            return String.Format(
                "data:text/html;charset=utf-8,{0}",
                HttpUtility.UrlEncode(pageText).Replace('+', ' ')
            );
        }

        public void WaitFor<T> (Holder<T> holder, T expectedValue, double timeoutSeconds) {
            var comparer = Comparer<T>.Default;
            long start = DateTime.UtcNow.Ticks;
            long end = start + TimeSpan.FromSeconds(timeoutSeconds).Ticks;

            while (comparer.Compare(holder.Value, expectedValue) != 0) {
                if (DateTime.UtcNow.Ticks > end)
                    throw new TimeoutException(String.Format(
                        "Timed out while waiting for value. Expected {0}, was {1}",
                        expectedValue, holder.Value
                    ));

                BerkeliumSharp.Update();
            }
        }
    }

    public class BasicTests : BasicFixture {
        [Test]
        public void TestCreateWindow () {
            using (var window = new Window(Context)) {
                Assert.IsNotNull(window);
                Assert.AreEqual(Context, window.Context);
            }
        }

        [Test]
        public void TestNavigateToUpdatesAddressBar () {
            var exampleUrl = MakeDataUrl("test");

            var addressBarText = new Holder<string>();
            var loadingState = new Holder<bool>();

            using (var window = new Window(Context)) {
                window.AddressBarChanged += (w, url) => addressBarText.Value = url;

                Assert.IsTrue(window.NavigateTo(exampleUrl));

                WaitFor(addressBarText, exampleUrl, 5);
            }
        }

        [Test]
        public void TestChromeSendUnicode () {
            var chromeSendText = new Holder<string>();

            using (var window = new Window(Context)) {
                window.ChromeSend += (w, msg, args) => chromeSendText.Value = msg;

                window.ExecuteJavascript(
                    String.Format("chrome.send(\"{0}\")", UnicodeText)
                );

                WaitFor(chromeSendText, UnicodeText, 5);
            }
        }

        [Test]
        public void TestUnicodePageTitle () {
            var testUrl = MakeDataUrl(String.Format(
                "<html><head><title>{0}</title></head></html>", UnicodeText
            ));

            var titleText = new Holder<string>();

            using (var window = new Window(Context)) {
                window.TitleChanged += (w, title) => titleText.Value = title;

                window.NavigateTo(testUrl);

                WaitFor(titleText, UnicodeText, 5);
            }
        }

        [Test]
        public void TestUnicodePageUrl () {
            var testUrl = MakeDataUrl(UnicodeText);

            var urlText = new Holder<string>();

            using (var window = new Window(Context)) {
                window.AddressBarChanged += (w, url) => urlText.Value = url;

                window.NavigateTo(testUrl);

                WaitFor(urlText, testUrl, 5);
            }
        }

        [Test]
        public void TestAlertUnicode () {
            var alertText = new Holder<string>();

            using (var window = new Window(Context)) {
                window.ScriptAlert += delegate(Window w, string message, string defaultPrompt, string url, ScriptAlertFlags flags, ref bool success, ref string prompt) {
                    alertText.Value = message;
                };

                window.ExecuteJavascript(
                    String.Format("alert(\"{0}\")", UnicodeText)
                );

                WaitFor(alertText, UnicodeText, 5);
            }
        }

        [Test]
        public void TestPromptUnicode () {
            var resultText = new Holder<string>();

            using (var window = new Window(Context)) {
                window.ChromeSend += (w, msg, args) => resultText.Value = msg;
                window.ScriptAlert += delegate(Window w, string message, string defaultPrompt, string url, ScriptAlertFlags flags, ref bool success, ref string prompt) {
                    success = true;
                    prompt = UnicodeText;
                };

                window.ExecuteJavascript(
                    "chrome.send(prompt())"
                );

                WaitFor(resultText, UnicodeText, 5);
            }
        }

        [Test]
        public void TestClickButton () {
            var testUrl = MakeDataUrl(
                "<html><body onload=\"chrome.send('loaded')\">" +
                "<button style=\"position: absolute; left: 0; top: 0; width: 100; height: 25;\" " +
                "onclick=\"alert('clicked')\" onmouseover=\"chrome.send('mouseover')\">Click Me</button></body></html>"
            );

            var alertText = new Holder<string>();
            var chromeSendText = new Holder<string>();

            using (var window = new Window(Context)) {
                window.ScriptAlert += delegate(Window w, string message, string defaultPrompt, string url, ScriptAlertFlags flags, ref bool success, ref string prompt) {
                    alertText.Value = message;
                };
                window.ChromeSend += (w, msg, args) => chromeSendText.Value = msg;

                window.Resize(128, 128);
                window.NavigateTo(testUrl);

                WaitFor(chromeSendText, "loaded", 5);

                window.Focus();
                window.MouseMoved(8, 8);

                // If we send the mouse click before the mouse movement event
                //  has been fully processed, it won't click the button :(
                WaitFor(chromeSendText, "mouseover", 5);

                window.MouseButton(MouseButton.Left, true);
                window.MouseButton(MouseButton.Left, false);

                WaitFor(alertText, "clicked", 5);
            }
        }

        [Test]
        public void TestUnicodeTextboxInput () {
            var testUrl = MakeDataUrl(
                "<html><body onload=\"chrome.send('loaded')\">" +
                "<input id=\"textbox\" style=\"position: absolute; left: 0; top: 0; width: 100; height: 25;\" " +
                "onmouseover=\"chrome.send('mouseover')\" onmouseup=\"chrome.send('mouseup')\" /></body></html>"
            );

            var chromeSendText = new Holder<string>();

            using (var window = new Window(Context)) {
                window.ChromeSend += (w, msg, args) => chromeSendText.Value = msg;

                window.Resize(128, 128);
                window.NavigateTo(testUrl);

                WaitFor(chromeSendText, "loaded", 5);

                window.Focus();
                window.MouseMoved(8, 8);

                WaitFor(chromeSendText, "mouseover", 5);

                window.MouseButton(MouseButton.Left, true);
                window.MouseButton(MouseButton.Left, false);

                WaitFor(chromeSendText, "mouseup", 5);

                window.TextEvent(UnicodeText);

                window.ExecuteJavascript("chrome.send(document.getElementById('textbox').value)");

                WaitFor(chromeSendText, UnicodeText, 5);
            }
        }
    }
}
