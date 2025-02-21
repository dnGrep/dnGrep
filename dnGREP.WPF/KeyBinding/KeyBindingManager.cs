using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace dnGREP.WPF
{
    internal static class KeyBindingManager
    {
        private static readonly Dictionary<string, KeyBindingInfo> commandBindings = [];

        public static void RegisterCommand(string id, string label, string keyGesture)
        {
            commandBindings.Add(id, new KeyBindingInfo(label, keyGesture));
        }

        public static bool TryGetCommandInfo(string id, [MaybeNullWhen(false)] out KeyBindingInfo keyBindingInfo)
        {
            if (commandBindings.TryGetValue(id, out KeyBindingInfo? info))
            {
                keyBindingInfo = info;
                return true;
            }
            keyBindingInfo = default;
            return false;
        }

        public static KeyBinding CreateFrozenKeyBinding(ICommand command, string keyGesture)
        {
            if (command is RelayCommand cmd)
                cmd.KeyGestureText = keyGesture;

            KeyBinding kb = new(command, new KeyGestureConverter().ConvertFromString(keyGesture) as KeyGesture);
            kb.Freeze();
            return kb;
        }

    }

    public class KeyBindingInfo(string label, string keyGesture)
    {
        public string Label { get; private set; } = label;

        public string KeyGesture { get; private set; } = keyGesture;
    }

}
