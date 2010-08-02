using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Berkelium.Managed;
using System.Drawing.Imaging;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;

namespace BerkeliumWinFormsTest {
    public partial class WebKitFrame : UserControl {
        public const string DesignModeUrl = "data:text/html;charset=utf-8;base64,PGh0bWw%2BPGJvZHk%2BV2ViS2l0RnJhbWU8L2JvZHk%2BPC9odG1sPg0K";

        public event Action<object, string> AddressChanged;
        public event Action<object, string, string[]> ChromeSend;
        public event Action<object, WebKitFrame, Rectangle, string> WindowOpened;
        public event Action<object, string> BeginLoad;
        public event Action<object> PageLoaded;
        public event Action<object, ScriptAlertEventArgs> ScriptAlert;
        public event Action<object, NavigationRequestedEventArgs> NavigationRequested;
        public event Action<object, bool> LoadingStateChanged;
        public event Action<object, string> TitleChanged;

        static byte[] TemporaryBuffer;
        Context Context;
        Window Window;
        Bitmap WindowBitmap;
        static int InitCount = 0;

        protected void WireEventHandlers () {
            foreach (var method in this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (!method.Name.StartsWith("WebKit_"))
                    continue;

                var eventName = method.Name.Replace("WebKit_", "");

                var evt = Window.GetType().GetEvent(eventName);

                evt.AddEventHandler(Window, Delegate.CreateDelegate(evt.EventHandlerType, this, method));
            }
        }

        // Application.Idle might not be the best place to do this, but it
        //  works pretty well considering how easy it is.
        protected static void OnIdle (object sender, EventArgs e) {
            if (InitCount > 0)
                BerkeliumSharp.Update();
        }

        // We clean up in Application.Exit to avoid any issues with events
        //  being processed after teardown.
        protected static void OnExit (object sender, EventArgs e) {
            if (InitCount > 0) {
                InitCount -= 1;
                BerkeliumSharp.Destroy();
            }
        }

        protected static void OnPureCall () {
            throw new ApplicationException("Pure virtual call in Berkelium");
        }

        protected static void OnOutOfMemory () {
            throw new ApplicationException("Allocation in Berkelium");
        }

        protected static void OnAssertion (string assertionMessage) {
            throw new ApplicationException(String.Format("Assertion failed: {0}", assertionMessage));
        }

        protected static void OnInvalidParameter (string expression, string function, string file, int lineNumber) {
            throw new ApplicationException(String.Format(
                "Invalid parameter in Berkelium: {0} at line {3} in function {1} in file {2}",
                expression, function, file, lineNumber
            ));
        }

        public WebKitFrame () {
            InitializeComponent();

            // This turns off WM_PAINTBACKGROUND and some other useless things
            //  that would interfere with what we're doing
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.UserPaint |
                ControlStyles.Opaque, true
            );

            // Inexplicably this event does not show up in the WinForms designer.
            MouseWheel += UserControl_MouseWheel;
        }

        private WebKitFrame (Window window) 
            : this() {
            Window = window;
        }

        private void WebKit_StartLoading (Window window, string newUrl) {
            if (BeginLoad != null)
                BeginLoad(this, newUrl);
        }

        private void WebKit_LoadingStateChanged (Window window, bool isLoading) {
            IsLoading = isLoading;

            if (LoadingStateChanged != null)
                LoadingStateChanged(this, isLoading);

            // The content under the mouse may have changed at this point so we trigger a mousemove
            //  event to update the cursor.
            var localCursorPos = this.PointToClient(Cursor.Position);
            Window.Widget.MouseMoved(localCursorPos.X, localCursorPos.Y);
        }

        private void WebKit_TitleChanged (Window window, string newTitle) {
            Title = newTitle;

            if (TitleChanged != null)
                TitleChanged(this, newTitle);
        }

        private void WebKit_TooltipChanged (Window window, string newTooltip) {
            ToolTipProvider.SetToolTip(this, newTooltip);
        }

        private void WebKit_ChromeSend (Window window, string message, string[] arguments) {
            if (ChromeSend != null)
                ChromeSend(this, message, arguments);
        }

        private void WebKit_AddressBarChanged (Window window, string newUrl) {
            if (AddressChanged != null)
                AddressChanged(this, newUrl);
        }

        private void WebKit_CursorChanged (Window window, IntPtr cursorHandle) {
            // I hope this doesn't clone the cursor or anything like that which would
            //  waste resources :)
            if (cursorHandle != IntPtr.Zero)
                this.Cursor = new Cursor(cursorHandle);
            else
                this.Cursor = null;
        }

        private void WebKit_CreatedWindow (Window source, Window newWindow, Rect initialRect, string url) {
            if (WindowOpened != null) {
                var frame = new WebKitFrame(newWindow);
                WindowOpened(
                    this, frame, 
                    new Rectangle(initialRect.Left, initialRect.Top, initialRect.Width, initialRect.Height), 
                    url
                );
            }
        }

        private void WebKit_WidgetCreated (Window source, Widget newWidget, int zIndex) {
            var fw = new FloatingWindow(this, newWidget);
            fw.Show(this.ParentForm);
            fw.Focus();
        }

        private void WebKit_Load (Window source) {
            if (PageLoaded != null)
                PageLoaded(this);
        }

        private void WebKit_ScriptAlert (Window source, string message, string defaultValue, string url, ScriptAlertFlags flags, ref bool success, ref string value) {
            if (ScriptAlert != null) {
                var args = new ScriptAlertEventArgs(message, defaultValue, url, flags, success, value);
                ScriptAlert(this, args);
                success = args.Success;
                value = args.Value;
            }
        }

        private void WebKit_NavigationRequested (Window source, string url, string referrer, bool isNewWindow, ref bool cancelDefaultAction) {
            if (NavigationRequested != null) {
                var args = new NavigationRequestedEventArgs(url, referrer, isNewWindow, cancelDefaultAction);
                NavigationRequested(this, args);
                cancelDefaultAction = args.CancelDefaultAction;
            }
        }

        private void WebKit_ShowContextMenu (Window window, ContextMenuEventArgs args) {
            var menu = new ContextMenuStrip();

            if (!string.IsNullOrEmpty(args.LinkUrl)) {
                menu.Items.Add("&Open Link", null,
                    (s, e) => Window.NavigateTo(args.LinkUrl)
                );
                menu.Items.Add("Copy Link Address", null,
                    (s, e) => {
                        Clipboard.Clear(); Clipboard.SetText(args.LinkUrl);
                    }
                );
                menu.Items.Add("-");
            }

            if (args.IsEditable || !string.IsNullOrEmpty(args.SelectedText)) {
                menu.Items.Add(
                    "&Undo", null,
                    (s, e) => Window.Undo()
                ).Enabled = (args.EditFlags & EditFlags.CanUndo) == EditFlags.CanUndo;
                menu.Items.Add(
                    "&Redo", null,
                    (s, e) => Window.Redo()
                ).Enabled = (args.EditFlags & EditFlags.CanRedo) == EditFlags.CanRedo;

                menu.Items.Add("-");

                menu.Items.Add(
                    "Cu&t", null,
                    (s, e) => Window.Cut()
                ).Enabled = (args.EditFlags & EditFlags.CanCut) == EditFlags.CanCut;
                menu.Items.Add(
                    "&Copy", null,
                    (s, e) => Window.Copy()
                ).Enabled = (args.EditFlags & EditFlags.CanCopy) == EditFlags.CanCopy;
                menu.Items.Add(
                    "&Paste", null,
                    (s, e) => Window.Paste()
                ).Enabled = (args.EditFlags & EditFlags.CanPaste) == EditFlags.CanPaste;
                menu.Items.Add(
                    "&Delete", null,
                    (s, e) => Window.DeleteSelection()
                ).Enabled = (args.EditFlags & EditFlags.CanDelete) == EditFlags.CanDelete;
            } else {
                menu.Items.Add("&Back", null,
                    (s, e) => Window.GoBack()
                ).Enabled = Window.CanGoBack;
                menu.Items.Add("&Forward", null,
                    (s, e) => Window.GoForward()
                ).Enabled = Window.CanGoForward;
                menu.Items.Add("&Reload", null,
                    (s, e) => Window.Refresh()
                ).Enabled = !IsLoading;
                menu.Items.Add("&Stop", null,
                    (s, e) => Window.Stop()
                ).Enabled = IsLoading;
            }

            menu.Items.Add("-");

            menu.Items.Add(
                "Select &All", null,
                (s, e) => Window.SelectAll()
            ).Enabled = (args.EditFlags & EditFlags.CanSelectAll) == EditFlags.CanSelectAll;

            switch (args.MediaType) {
                case MediaType.Image:
                    menu.Items.Add("-");
                    menu.Items.Add(
                        "Copy Image URL", null,
                        (s, e) => Clipboard.SetText(args.SrcUrl)
                    );
                    break;
                default:
                    break;
            }

            menu.Items.Add("-");
            menu.Items.Add("View Page Source", null,
                (s, e) => Window.NavigateTo("viewsource:" + args.PageUrl)
            );

            menu.Closed += (s, e) => 
                this.BeginInvoke((Action)(() => menu.Dispose()));
            menu.Show(this, args.MouseX, args.MouseY);
        }

        private void WebKit_Paint (Window window, IntPtr sourceBuffer, Rect rect, int dx, int dy, Rect scrollRect) {
            HandlePaintEvent(WindowBitmap, sourceBuffer, rect, dx, dy, scrollRect, Invalidate);
        }

        unsafe internal static void HandlePaintEvent (Bitmap windowBitmap, IntPtr sourceBuffer, Rect rect, int dx, int dy, Rect scrollRect, Action<Rectangle> invalidate) {
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

                    invalidate(destRect);
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

            invalidate(clientRect);
        }

        private void UserControl_Paint (object sender, PaintEventArgs e) {
            // Tweak the Graphics object for maximum performance and then copy
            //  our backing store to the screen.

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

            e.Graphics.DrawImage(WindowBitmap, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
        }

        private void UserControl_MouseMove (object sender, MouseEventArgs e) {
            if (DesignMode)
                return;

            Window.Widget.MouseMoved(e.X, e.Y);
        }

        private void UserControl_MouseWheel (object sender, MouseEventArgs e) {
            if (DesignMode)
                return;

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                Window.AdjustZoom(
                    e.Delta > 0 ? ZoomFunction.ZoomIn : ZoomFunction.ZoomOut
                );
            } else {
                // I think e.Delta might be in the wrong scale - mousewheel scrolling in
                //  berkelium via this method does not scroll at the same speed as Chrome.
                Window.Widget.MouseWheel(0, e.Delta);
            }

            // Since the content under the mouse has likely changed, we send a MouseMove event
            //  to trigger an update of the mouse cursor if necessary
            Window.Widget.MouseMoved(e.X, e.Y);
        }

        // It might appear that this function doesn't work, but it actually does - web pages
        //  that handle multiple buttons get all the right events. We just don't see Chrome
        //  context menus and such because Berkelium doesn't implement them yet.
        internal static MouseButton MapMouseButton (MouseButtons button) {
            switch (button) {
                default:
                case MouseButtons.Left:
                    return MouseButton.Left;
                case MouseButtons.Middle:
                    return MouseButton.Middle;
                case MouseButtons.Right:
                    return MouseButton.Right;
            }
        }

        private void UserControl_MouseDown (object sender, MouseEventArgs e) {
            if (DesignMode)
                return;

            Window.Widget.MouseButton(MapMouseButton(e.Button), true);
        }

        private void UserControl_MouseUp (object sender, MouseEventArgs e) {
            if (DesignMode)
                return;

            Window.Widget.MouseButton(MapMouseButton(e.Button), false);
        }

        private void UserControl_Resize (object sender, EventArgs e) {
            if (Window == null)
                return;

            if (WindowBitmap != null)
                WindowBitmap.Dispose();

            var width = ClientSize.Width;
            var height = ClientSize.Height;

            // If we get minimized our size will be zero, which tends to make things
            //  explode, so we clamp our size at 1.
            if (width < 1)
                width = 1;
            if (height < 1)
                height = 1;

            // For efficiency in the case of a shrinking resize, we could reuse the
            //  existing bitmap. But why bother?
            using (var g = this.CreateGraphics())
                WindowBitmap = new Bitmap(width, height, g);

            // Notify WebKit of the resize *after* the buffer is ready,
            //  not before. Just to be safe.
            Window.Resize(width, height);
        }

        // Note that we probably should be generating AUTOREPEAT_KEY here too,
        //  but getting at that information requires P/Invoke
        public static KeyModifier MapKeyModifiers (KeyEventArgs e) {
            KeyModifier result = 0;

            if ((e.KeyData & Keys.Alt) == Keys.Alt)
                result |= KeyModifier.ALT_MOD;
            if ((e.KeyData & Keys.Control) == Keys.Control)
                result |= KeyModifier.CONTROL_MOD;
            if ((e.KeyData & Keys.Shift) == Keys.Shift)
                result |= KeyModifier.SHIFT_MOD;

            return result;
        }

        private void UserControl_KeyDown (object sender, KeyEventArgs e) {
            if (DesignMode)
                return;

            // I think we probably should generate scancodes here too, but I haven't
            //  gotten around to it yet since it requires P/Invoke.
            Window.Widget.KeyEvent(true, MapKeyModifiers(e), (int)e.KeyCode, 0);

            e.Handled = true;
        }

        private void UserControl_KeyUp (object sender, KeyEventArgs e) {
            if (DesignMode)
                return;

            // I think we probably should generate scancodes here too, but I haven't
            //  gotten around to it yet since it requires P/Invoke.
            Window.Widget.KeyEvent(false, MapKeyModifiers(e), (int)e.KeyCode, 0);

            e.Handled = true;
        }

        private void WebKitFrame_Enter (object sender, EventArgs e) {
            if (DesignMode)
                return;

            Window.Widget.Focus();
        }

        private void WebKitFrame_Leave (object sender, EventArgs e) {
            if (DesignMode)
                return;

            Window.Widget.Unfocus();
        }

        // By default, the arrow keys and tab generate focus changes
        //  in the default DialogProc, which prevents them from being 
        //  handled by WebKit. So we suppress that behavior for those
        //  keys. (We can't return true for everything, because that
        //  breaks the native KeyPress events).
        protected override bool ProcessDialogKey (Keys keyData) {
            switch (keyData & Keys.KeyCode) {
                case Keys.Tab:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                    return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        // We can't override DestroyHandle, because it's not always
        //  called during teardown by WinForms. Inexplicably, this method
        //  *is* always called.
        protected override void OnHandleDestroyed (EventArgs e) {
            if (Window != null)
                Window.Dispose();
            Window = null;
            if (WindowBitmap != null)
                WindowBitmap.Dispose();
            WindowBitmap = null;

            base.OnHandleDestroyed(e);
        }

        // We defer creating the Window until Load because the end-user
        //  can't see it before then anyway. If our goal were to show a
        //  fully loaded page as soon as our window appears, though, we
        //  might want to create it earlier and trigger a page load.
        private void WebKitFrame_Load (object sender, EventArgs e) {
            if (InitCount <= 0) {
                BerkeliumSharp.Init(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "BerkeliumTest"
                    )
                );

                BerkeliumSharp.PureCall += OnPureCall;
                BerkeliumSharp.OutOfMemory += OnOutOfMemory;
                BerkeliumSharp.InvalidParameter += OnInvalidParameter;
                BerkeliumSharp.Assertion += OnAssertion;

                Application.Idle += OnIdle;
                Application.ApplicationExit += OnExit;

                InitCount += 1;
            }

            if (Window == null) {
                Context = Context.Create();
                Window = new Window(Context);
            } else {
                Context = Window.Context;
            }

            WireEventHandlers();
            
            // We just fake a resize to wire up the buffer and everything,
            //  because I'm lazy.
            UserControl_Resize(this, EventArgs.Empty);

            if (DesignMode)
                Window.NavigateTo(DesignModeUrl);
        }

        public Window GetWindow () {
            return Window;
        }

        public bool IsLoading {
            get;
            private set;
        }

        public string Title {
            get;
            private set;
        }

        // I'm not sure this is correct in the presence of non-english
        //  character sets.
        private void WebKitFrame_KeyPress (object sender, KeyPressEventArgs e) {
            e.Handled = true;
            Window.TextEvent(new String(e.KeyChar, 1));
        }
    }

    public class NavigationRequestedEventArgs {
        public readonly string Url;
        public readonly string Referrer;
        public readonly bool IsNewWindow;

        public bool CancelDefaultAction = false;

        public NavigationRequestedEventArgs (string url, string referrer, bool isNewWindow, bool cancelDefaultAction) {
            Url = url;
            Referrer = referrer;
            IsNewWindow = isNewWindow;
            CancelDefaultAction = cancelDefaultAction;
        }
    }

    public class ScriptAlertEventArgs {
        public readonly string Message;
        public readonly string DefaultValue;
        public readonly string Url;
        public readonly ScriptAlertFlags Flags;

        public bool Success = false;
        public string Value = null;

        public ScriptAlertEventArgs (string message, string defaultValue, string url, ScriptAlertFlags flags, bool success, string value) {
            Message = message;
            DefaultValue = defaultValue;
            Url = url;
            Flags = flags;
            Success = success;
            Value = value;
        }
    }
}
