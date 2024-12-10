using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace dnGREP.WPF
{
    internal class HotKey : IDisposable
    {
        private static Dictionary<int, HotKey> _dictHotKeyToCalBackProc = [];
        private bool beenDisposed;
        internal Key Key { get; private set; }
        internal HOT_KEY_MODIFIERS KeyModifiers { get; private set; }
        internal Action<HotKey> Action { get; private set; }
        internal int Id { get; set; }

        private const int WmHotKey = 0x0312;

        internal HotKey(Key k, HOT_KEY_MODIFIERS keyModifiers, Action<HotKey> action)
        {
            Key = k;
            KeyModifiers = keyModifiers;
            Action = action;
            Register();
        }

        private bool Register()
        {
            int virtualKeyCode = KeyInterop.VirtualKeyFromKey(Key);
            Id = virtualKeyCode + ((int)KeyModifiers * 0x10000);
            bool result = PInvoke.RegisterHotKey(HWND.Null, Id, KeyModifiers, (uint)virtualKeyCode);

            if (_dictHotKeyToCalBackProc.Count == 0)
            {
                ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);
            }

            _dictHotKeyToCalBackProc.Add(Id, this);

            return result;
        }

        private void Unregister()
        {
            if (_dictHotKeyToCalBackProc.TryGetValue(Id, out _))
            {
                PInvoke.UnregisterHotKey(HWND.Null, Id);
            }
        }

        private static void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (!handled)
            {
                if (msg.message == WmHotKey)
                {
                    if (_dictHotKeyToCalBackProc.TryGetValue((int)msg.wParam, out HotKey? hotKey))
                    {
                        hotKey.Action?.Invoke(hotKey);
                        handled = true;
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!beenDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    Unregister();
                }

                beenDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
