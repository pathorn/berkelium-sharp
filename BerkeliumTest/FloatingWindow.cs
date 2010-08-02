using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Berkelium.Managed;

namespace BerkeliumWinFormsTest {
    public partial class FloatingWindow : Form {
        public readonly Widget Widget;
        public readonly WebKitFrame ParentFrame;
        public Bitmap Bitmap;

        public FloatingWindow (WebKitFrame parent, Widget widget) {
            InitializeComponent();
            ParentFrame = parent;
            Widget = widget;

            Widget.Paint += Widget_Paint;
            Widget.Destroyed += Widget_Destroyed;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.Opaque, true
            );

            MouseWheel += FloatingWindow_MouseWheel;

            UpdateSizeAndPosition();
        }

        void Widget_Paint (Window window, Widget widget, IntPtr sourceBuffer, Rect rect, int dx, int dy, Rect scrollRect) {
            WebKitFrame.HandlePaintEvent(Bitmap, sourceBuffer, rect, dx, dy, scrollRect, Invalidate);
        }

        void Widget_Destroyed (Window window, Widget widget) {
            Close();
            Dispose();
        }

        protected void UpdateSizeAndPosition () {
            int l, t, w, h;

            var rect = Widget.Rect;
            var parentScreenPos = ParentFrame.PointToScreen(new Point(0, 0));
            w = rect.Width;
            h = rect.Height;
            l = rect.Left + parentScreenPos.X;
            t = rect.Top + parentScreenPos.Y;

            if (Bitmap != null)
                Bitmap.Dispose();

            Bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            SetBounds(l, t, w, h);
        }

        protected override void OnPaint (PaintEventArgs e) {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

            e.Graphics.DrawImage(Bitmap, ClientRectangle);
        }

        private void FloatingWindow_MouseMove (object sender, MouseEventArgs e) {
            var rect = Widget.Rect;
            Widget.MouseMoved(e.X, e.Y);
        }

        private void FloatingWindow_MouseUp (object sender, MouseEventArgs e) {
            Widget.MouseButton(WebKitFrame.MapMouseButton(e.Button), false);
        }

        private void FloatingWindow_MouseDown (object sender, MouseEventArgs e) {
            Widget.MouseButton(WebKitFrame.MapMouseButton(e.Button), true);
        }

        private void FloatingWindow_MouseWheel (object sender, MouseEventArgs e) {
            Widget.MouseWheel(0, e.Delta);
        }

        private void FloatingWindow_KeyDown (object sender, KeyEventArgs e) {
            Widget.KeyEvent(true, WebKitFrame.MapKeyModifiers(e), e.KeyValue, 0);
        }

        private void FloatingWindow_KeyPress (object sender, KeyPressEventArgs e) {
            Widget.TextEvent(new string(e.KeyChar, 1));
        }

        private void FloatingWindow_KeyUp (object sender, KeyEventArgs e) {
            Widget.KeyEvent(false, WebKitFrame.MapKeyModifiers(e), e.KeyValue, 0);
        }

        private void FloatingWindow_MouseLeave (object sender, EventArgs e) {
            var localPos = PointToClient(Cursor.Position);
            Widget.MouseMoved(localPos.X, localPos.Y);
        }

        private void FloatingWindow_Deactivate (object sender, EventArgs e) {
            Widget.Unfocus();
        }

        private void FloatingWindow_Activated (object sender, EventArgs e) {
            Widget.Focus();
        }

        private void FloatingWindow_Shown (object sender, EventArgs e) {
            UpdateSizeAndPosition();
        }
    }
}
