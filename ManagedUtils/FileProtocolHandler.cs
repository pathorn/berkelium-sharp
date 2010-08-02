using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Berkelium.Managed;
using System.IO;

namespace Berkelium.Managed {
    public class FileProtocolHandler : ProtocolHandler {
        public readonly Func<string, Stream> OpenFile;
        public readonly Func<string, string> SelectMimeType;
        public readonly string Scheme;

        public FileProtocolHandler (Context context, string scheme, Func<string, Stream> openFile)
            : this(context, scheme, openFile, AutoSelectMimeType) {
        }

        public FileProtocolHandler (Context context, string scheme, Func<string, Stream> openFile, Func<string, string> selectMimeType)
            : base(context, scheme) {

            Scheme = scheme + "://";
            OpenFile = openFile;
            SelectMimeType = selectMimeType;
        }

        public static string AutoSelectMimeType (string filename) {
            var extension = Path.GetExtension(filename).ToLowerInvariant();

            switch (extension) {
                case ".htm":
                case ".html":
                    return "text/html";
                case ".js":
                    return "text/javascript";
                case ".css":
                    return "text/css";
                case ".gif":
                    return "image/gif";
                case ".png":
                    return "image/png";
                case ".jpeg":
                case ".jpg":
                    return "image/jpeg";
                default:
                    return "text/plain";
            }
        }

        protected override bool HandleRequest (string url, ref byte[] responseBody, ref string[] responseHeaders) {
            var uri = new Uri(url);
            var path = uri.GetLeftPart(UriPartial.Path).Replace(Scheme, "");

            if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);

            var stream = OpenFile(path);

            if (stream == null) {
                responseHeaders = new string[] {
                    "HTTP/1.1 404 Not Found"
                };
                return false;
            }

            using (stream) {
                responseBody = new byte[stream.Length];
                stream.Read(responseBody, 0, responseBody.Length);
            }

            var mimeType = SelectMimeType(path);
            responseHeaders = new string[] {
                "HTTP/1.1 200 OK",
                String.Format("Content-type: {0}; charset=utf-8", mimeType)
            };

            return true;
        }
    }
}
