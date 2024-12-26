using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace dnGREP.WPF
{
    internal partial class HotKey : IDisposable
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
        }

        internal HotKey(KeyAndModifiers keyAndModifiers, Action<HotKey> action)
        {
            Key = keyAndModifiers.Key;
            KeyModifiers = keyAndModifiers.KeyModifiers;
            Action = action;
        }

        public bool Register()
        {
            int virtualKeyCode = KeyInterop.VirtualKeyFromKey(Key);
            Id = virtualKeyCode + ((int)KeyModifiers * 0x10000);
            bool result = PInvoke.RegisterHotKey(HWND.Null, Id, KeyModifiers, (uint)virtualKeyCode);

            if (_dictHotKeyToCalBackProc.Count == 0)
            {
                ComponentDispatcher.ThreadFilterMessage -= ComponentDispatcherThreadFilterMessage;
                ComponentDispatcher.ThreadFilterMessage += ComponentDispatcherThreadFilterMessage;
            }

            _dictHotKeyToCalBackProc.Add(Id, this);

            return result;
        }

        private void Unregister()
        {
            if (_dictHotKeyToCalBackProc.TryGetValue(Id, out _))
            {
                PInvoke.UnregisterHotKey(HWND.Null, Id);
                _dictHotKeyToCalBackProc.Remove(Id);
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

        public static bool TryParse(string input, [NotNullWhen(true)] out KeyAndModifiers? hotKeyAndModifiers)
        {
            var matches = HotKeyRegex().Matches(input);

            if (matches.Count > 1)
            {
                HOT_KEY_MODIFIERS modifiers = 0;
                foreach (Match match in matches.SkipLast(1))
                {
                    switch (match.Value)
                    {
                        case nameof(ModifierKeys.Alt):
                            modifiers |= HOT_KEY_MODIFIERS.MOD_ALT;
                            break;
                        case nameof(ModifierKeys.Control):
                            modifiers |= HOT_KEY_MODIFIERS.MOD_CONTROL;
                            break;
                        case nameof(ModifierKeys.Shift):
                            modifiers |= HOT_KEY_MODIFIERS.MOD_SHIFT;
                            break;
                        case nameof(ModifierKeys.Windows):
                            modifiers |= HOT_KEY_MODIFIERS.MOD_WIN;
                            break;
                    }
                }
                string keyStr = matches.Last().Value;
                if (!string.IsNullOrEmpty(keyStr) && Enum.TryParse<Key>(keyStr, out Key key) &&
                    modifiers > 0)
                {
                    hotKeyAndModifiers = new(key, modifiers);
                    return true;
                }
            }

            hotKeyAndModifiers = null;
            return false;
        }


        [GeneratedRegex(@"((\bControl\b)|(\bAlt\b)|(\bShift\b)|(\bWindows\b)|(\b\w+$))")]
        private static partial Regex HotKeyRegex();

    }

    internal record KeyAndModifiers(Key Key, HOT_KEY_MODIFIERS KeyModifiers)
    {
    }

}
