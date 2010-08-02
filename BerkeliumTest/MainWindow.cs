using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using Berkelium.Managed;

namespace BerkeliumWinFormsTest {
    public partial class MainWindow : Form {
        FileProtocolHandler AssetProtocol;
        ViewSourceProtocolHandler ViewSourceProtocol;

        public MainWindow () {
            InitializeComponent();
        }

        private void AddressBar_KeyDown (object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                WebKit.GetWindow().NavigateTo(AddressBar.Text);
                e.Handled = true;
            }
        }

        private void WebKit_AddressChanged (object sender, string newUrl) {
            AddressBar.Text = newUrl;
        }

        private void Form1_Load (object sender, EventArgs e) {
            AssetProtocol = new FileProtocolHandler(
                WebKit.GetWindow().Context,
                "asset",
                (filename) => {
                    try {
                        return File.OpenRead(Path.Combine(
                            Path.GetDirectoryName(Application.ExecutablePath), filename
                        ));
                    } catch {
                        return null;
                    }
                }
            );
            ViewSourceProtocol = new ViewSourceProtocolHandler(
                WebKit.GetWindow().Context
            );

            var filePath = "asset://./test.html";
            WebKit.GetWindow().NavigateTo(filePath);
        }

        private void WebKit_ChromeSend (object sender, string message, string[] arguments) {
            HandleChromeSend(this, message, arguments);
        }

        public static void HandleChromeSend (Form parent, string message, string[] arguments) {
            var messageText = String.Format(
                "The webpage invoked the following command:\r\nchrome.send(\"{0}\", [{1}])",
                message, String.Join(", ",
                    (from a in arguments select String.Format("\"{0}\"", a)).ToArray()
                )
            );

            MessageBox.Show(parent, messageText, "Webpage Callback");
        }

        private void WebKit_WindowOpened (object sender, WebKitFrame newFrame, Rectangle initialRect, string creatorUrl) {
            HandleWindowOpen(this, newFrame, initialRect);
        }

        public static void HandleWindowOpen (Form parent, WebKitFrame newFrame, Rectangle initialRect) {
            var form = new Form() {
                Text = "Popup Window",
                FormBorderStyle = FormBorderStyle.Sizable,
                Icon = parent.Icon
            };

            form.Controls.Add(newFrame);
            newFrame.Dock = DockStyle.Fill;

            newFrame.WindowOpened += (s, nf, ir, cu) =>
                HandleWindowOpen(form, nf, ir);
            newFrame.ScriptAlert += (s, args) =>
                HandleScriptAlert(form, (ScriptAlertEventArgs)args);
            // Doesn't work since chrome.send wasn't enabled when the renderer created the new window =[
            newFrame.ChromeSend += (s, msg, args) =>
                HandleChromeSend(form, msg, args);
            newFrame.TitleChanged += (s, title) =>
                form.Text = title;

            if (!initialRect.IsEmpty)
                form.Size = initialRect.Size;

            form.Show(parent);
        }

        private void WebKit_PageLoaded (object sender) {
            RefreshToolbar();
        }

        private void tbBack_Click (object sender, EventArgs e) {
            WebKit.GetWindow().GoBack();
        }

        private void tbForward_Click (object sender, EventArgs e) {
            WebKit.GetWindow().GoForward();
        }

        private void tbRefresh_Click (object sender, EventArgs e) {
            WebKit.GetWindow().Refresh();
        }

        void RefreshToolbar () {
            var w = WebKit.GetWindow();

            tbBack.Enabled = w.CanGoBack;
            tbForward.Enabled = w.CanGoForward;
            tbRefresh.Enabled = !WebKit.IsLoading;
            tbStop.Enabled = WebKit.IsLoading;
        }

        private void WebKit_ScriptAlert (object sender, ScriptAlertEventArgs args) {
            HandleScriptAlert(this, args);
        }

        public static void HandleScriptAlert (Form parent, ScriptAlertEventArgs e) {
            if ((e.Flags & ScriptAlertFlags.HasPromptField) == ScriptAlertFlags.HasPromptField) {
                using (var dlg = new PromptDialog(e.DefaultValue)) {
                    e.Success = (dlg.ShowDialog() == DialogResult.OK);
                    e.Value = dlg.Value.Text;
                }
            } else {
                string dialogType = "Alert";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBoxIcon icon = MessageBoxIcon.Information;

                if ((e.Flags & ScriptAlertFlags.HasCancelButton) == ScriptAlertFlags.HasCancelButton) {
                    dialogType = "Confirmation";
                    buttons = MessageBoxButtons.OKCancel;
                    icon = MessageBoxIcon.Question;
                }

                var result = MessageBox.Show(
                    parent, e.Message, String.Format("{0} dialog", dialogType), buttons, icon
                );

                e.Success = (result == DialogResult.OK);
            }
        }

        private void WebKit_NavigationRequested (object sender, NavigationRequestedEventArgs args) {
            if (args.IsNewWindow) {
                var result = MessageBox.Show(
                    String.Format(
                        "The page at {0} has asked to open a new window containing the following page:\r\n{1}\r\nWould you like to load it?",
                        args.Referrer, args.Url
                    ),
                    "New window request",
                    MessageBoxButtons.YesNo
                );

                args.CancelDefaultAction = (result != DialogResult.Yes);
            }
        }

        private void WebKit_LoadingStateChanged (object sender, bool isLoading) {
            RefreshToolbar();
        }

        private void WebKit_TitleChanged (object sender, string newTitle) {
            Text = String.Format("Berkelium Test - {0}", newTitle);
        }

        private void tbStop_Click (object sender, EventArgs e) {
            WebKit.GetWindow().Stop();
        }
    }

    public class ViewSourceProtocolHandler : ProtocolHandler {
        public ViewSourceProtocolHandler (Context context)
            : base(context, "viewsource") {
        }

        protected override bool HandleRequest (string url, ref byte[] responseBody, ref string[] responseHeaders) {
            var uri = new Uri(url);

            var body = new StringBuilder();
            body.AppendFormat("<html><head><title>View Source: {0}</title>", uri.AbsolutePath);
            body.Append("<style>iframe { position: absolute; top: 0%; left: 0%; width: 100%; height: 100%; border: 0px; margin: 0px; padding: 0px; }</style>");
            body.AppendFormat("<body><iframe src=\"{0}\" viewsource=\"true\"></iframe></body></html>", uri.AbsolutePath);

            responseBody = Encoding.UTF8.GetBytes(body.ToString());

            responseHeaders = new string[] {
                "HTTP/1.1 200 OK",
                String.Format("Content-type: text/html; charset=utf-8")
            };

            return true;
        }
    }
}
