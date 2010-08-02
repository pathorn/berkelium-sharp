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
    public class ProtocolHandlerTests : BasicFixture {
        protected Stream FilenameProtocolHandler (string filename) {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine("<body>");
            writer.WriteLine("<span id='filename'>");
            writer.WriteLine(filename);
            writer.WriteLine("</span>");
            writer.WriteLine("<script type='text/javascript'>chrome.send('filename', [document.getElementById('filename').innerText]);</script>");
            writer.WriteLine("</body>");
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        [Test]
        public void TestSingleFileProtocolHandler () {
            var filename = new Holder<string>();

            using (var protocolHandler = new FileProtocolHandler(Context, "asset", FilenameProtocolHandler, (fn) => "text/html"))
            using (var window = new Window(Context)) {
                window.ChromeSend += (w, msg, args) => {
                    if (msg == "filename")
                        filename.Value = args[0];
                };
                window.NavigateTo("asset://test/foo.html");

                WaitFor(filename, "test/foo.html", 5);
            }
        }

        [Test]
        public void TestTwoProtocolHandlers () {
            var filename = new Holder<string>();

            using (var protocolHandler1 = new FileProtocolHandler(Context, "pone", FilenameProtocolHandler, (fn) => "text/html"))
            using (var protocolHandler2 = new FileProtocolHandler(Context, "ptwo", FilenameProtocolHandler, (fn) => "text/html"))
            using (var window = new Window(Context)) {
                window.ChromeSend += (w, msg, args) => {
                    if (msg == "filename")
                        filename.Value = args[0];
                };

                window.NavigateTo("pone://test/one.html");

                WaitFor(filename, "test/one.html", 5);

                window.NavigateTo("ptwo://test/two.html");

                WaitFor(filename, "test/two.html", 5);
            }
        }
    }
}
