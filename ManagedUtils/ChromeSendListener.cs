using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Script.Serialization;

namespace Berkelium.Managed {
    public class ChromeSendListener : IDisposable {
        public event ChromeSendHandler Unhandled;

        public readonly Window Window;
        public Dictionary<string, ChromeSendHandler> RegisteredHandlers;

        public ChromeSendListener (Window window) {
            RegisteredHandlers = new Dictionary<string, ChromeSendHandler>();
            Window = window;
            Window.ChromeSend += GlobalHandler;
        }

        public void Dispose () {
            RegisteredHandlers.Clear();
            Window.ChromeSend -= GlobalHandler;
        }

        protected void GlobalHandler (Window window, string msg, string[] args) {
            ChromeSendHandler handler;
            if (RegisteredHandlers.TryGetValue(msg, out handler)) {
                handler(window, msg, args);
                return;
            }

            if (Unhandled != null)
                Unhandled(window, msg, args);
        }

        public void Register (string msg, ChromeSendHandler handler) {
            RegisteredHandlers[msg] = handler;
        }

        public void Register (string msg, Action<string[]> handler) {
            Register(msg, (w, m, a) => handler(a));
        }

        public void Register (string msg, Action handler) {
            Register(msg, (w, m, a) => handler());
        }

        public void Unregister (string msg) {
            RegisteredHandlers.Remove(msg);
        }
    }
}
