using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace Berkelium.Managed {
    public class TextureBackedWindow : Window {
        private static Regex ArgumentRegex = new Regex(@"\$(?'id'[0-9]*)", RegexOptions.Compiled);
        private static int[] TemporaryBuffer;

        private Queue<Texture2D> DeadTextures;

        public object Lock = null;
        public readonly GraphicsDevice Device;
        public Texture2D Texture;
        public JavaScriptSerializer Serializer;

        new public ChromeSendListener ChromeSend;

        private Dictionary<Widget, Texture2D> WidgetTextures;

        public TextureBackedWindow (Context context, GraphicsDevice device)
            : base(context) {

            Device = device;
            Transparent = true;
            DeadTextures = new Queue<Texture2D>();
            WidgetTextures = new Dictionary<Widget, Texture2D>();
            ChromeSend = new ChromeSendListener(this);
            Serializer = new JavaScriptSerializer();
        }

        public override void Resize (int width, int height) {
            if ((width == Width) && (height == Height))
                return;

            if (Lock != null)
                Monitor.Enter(Lock);

            var oldTexture = Texture;
            Texture = null;
            var newTexture = new Texture2D(
                Device, width, height, 1,
                TextureUsage.Linear, SurfaceFormat.Color
            );

            if (oldTexture != null) {
                int w = Math.Min(oldTexture.Width, newTexture.Width);
                int h = Math.Min(oldTexture.Height, newTexture.Height);
                int sz = w * h;

                if ((TemporaryBuffer == null) || (TemporaryBuffer.Length < sz))
                    TemporaryBuffer = new int[sz];

                oldTexture.GetData(0, new Rectangle(0, 0, w, h), TemporaryBuffer, 0, sz);
                newTexture.SetData(0, new Rectangle(0, 0, w, h), TemporaryBuffer, 0, sz, SetDataOptions.Discard);
                oldTexture.Dispose();
            }

            Texture = newTexture;

            if (Lock != null)
                Monitor.Exit(Lock);

            BerkeliumSharp.Update();

            base.Resize(width, height);

            BerkeliumSharp.Update();
        }

        public void Cleanup () {
            while (DeadTextures.Count > 0)
                lock (Lock)
                    DeadTextures.Dequeue().Dispose();                    
        }

        public void ExecuteJavascript (string javascript, params object[] variables) {
            var serializedVariables = (from v in variables select Serializer.Serialize(v)).ToArray();

            javascript = ArgumentRegex.Replace(
                javascript, (m) => serializedVariables[int.Parse(m.Groups["id"].Value)]
            );

            base.ExecuteJavascript(javascript);
        }

        protected override void OnWidgetCreated (Widget widget, int zIndex) {
            OnWidgetResized(widget, widget.Rect.Width, widget.Rect.Height);

            base.OnWidgetCreated(widget, zIndex);
        }

        protected override void OnWidgetResized (Widget widget, int newWidth, int newHeight) {
            Texture2D texture;
            if (WidgetTextures.TryGetValue(widget, out texture))
                texture.Dispose();

            texture = new Texture2D(
                Device, newWidth, newHeight, 1,
                TextureUsage.Linear, SurfaceFormat.Color
            );
            WidgetTextures[widget] = texture;

            base.OnWidgetResized(widget, newWidth, newHeight);
        }

        protected override void OnWidgetDestroyed (Widget widget) {
            if (Lock != null)
                Monitor.Enter(Lock);

            Texture2D texture;

            if (WidgetTextures.TryGetValue(widget, out texture)) {
                DeadTextures.Enqueue(texture);
                WidgetTextures.Remove(widget);
            }

            if (Lock != null)
                Monitor.Exit(Lock);

            base.OnWidgetDestroyed(widget);
        }

        protected override void OnPaint (IntPtr sourceBuffer, Rect rect, int dx, int dy, Rect scrollRect) {
            HandlePaintEvent(Texture, sourceBuffer, rect, dx, dy, scrollRect);

            base.OnPaint(sourceBuffer, rect, dx, dy, scrollRect);
        }

        protected override void OnWidgetPaint (Widget widget, IntPtr sourceBuffer, Rect rect, int dx, int dy, Rect scrollRect) {
            Texture2D texture;
            if (WidgetTextures.TryGetValue(widget, out texture))
                HandlePaintEvent(texture, sourceBuffer, rect, dx, dy, scrollRect);

            base.OnWidgetPaint(widget, sourceBuffer, rect, dx, dy, scrollRect);
        }

        protected void HandlePaintEvent (Texture2D texture, IntPtr sourceBuffer, Rect rect, int dx, int dy, Rect scrollRect) {
            if (Lock != null)
                Monitor.Enter(Lock);

            var clientRect = new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);

            Device.Textures[0] = null;

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
                    if ((TemporaryBuffer == null) || (TemporaryBuffer.Length < (sourceRect.Width * sourceRect.Height)))
                        TemporaryBuffer = new int[sourceRect.Width * sourceRect.Height];

                    if ((sourceRect.Right <= texture.Width) && (sourceRect.Bottom <= texture.Height) &&
                        (destRect.Right <= texture.Width) && (destRect.Bottom <= texture.Height)) {
                        // Copy the scrolled portion out of the texture into our temporary buffer
                        texture.GetData<int>(0, sourceRect, TemporaryBuffer, 0, sourceRect.Width * sourceRect.Height);
                        // And then copy it back into the new location
                        texture.SetData<int>(0, destRect, TemporaryBuffer, 0, destRect.Width * destRect.Height, SetDataOptions.Discard);
                    }
                }
            }

            if ((TemporaryBuffer == null) || (TemporaryBuffer.Length < (rect.Width * rect.Height)))
                TemporaryBuffer = new int[rect.Width * rect.Height];

            // Ugh. Why doesn't SetData accept a pointer? Terrible.
            var copySize = rect.Width * rect.Height;
            Marshal.Copy(sourceBuffer, TemporaryBuffer, 0, copySize);

            if ((clientRect.Right <= texture.Width) && (clientRect.Bottom <= texture.Height))
                texture.SetData<int>(0, clientRect, TemporaryBuffer, 0, copySize, SetDataOptions.Discard);

            if (Lock != null)
                Monitor.Exit(Lock);
        }

        public IEnumerable<KeyValuePair<Texture2D, Point>> RenderList {
            get {
                yield return new KeyValuePair<Texture2D, Point>(
                    Texture, new Point(0, 0)
                );

                foreach (var kvp in WidgetTextures) {
                    var rect = kvp.Key.Rect;

                    yield return new KeyValuePair<Texture2D, Point>(
                        kvp.Value, new Point(rect.Left, rect.Top)
                    );
                }
            }
        }

        protected override void Dispose (bool __p1) {
            if (Lock != null)
                Monitor.Enter(Lock);
                
            if (Texture != null) {
                Texture.Dispose();
                Texture = null;
            }

            if (Lock != null)
                Monitor.Exit(Lock);

            base.Dispose(__p1);
        }
    }
}
