using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    public enum KeyCategory { Main, Bookmark, Options, Replace, Script }

    internal static class KeyBindingManager
    {
        private static readonly List<KeyBindingInfo> mainCommandBindings = [];
        private static readonly List<KeyBindingInfo> bookmarkCommandBindings = [];
        private static readonly List<KeyBindingInfo> optionsCommandBindings = [];
        private static readonly List<KeyBindingInfo> replaceCommandBindings = [];
        private static readonly List<KeyBindingInfo> scriptCommandBindings = [];

        private static List<KeyBindingInfo> GetList(KeyCategory category)
        {
            return category switch
            {
                KeyCategory.Bookmark => bookmarkCommandBindings,
                KeyCategory.Options => optionsCommandBindings,
                KeyCategory.Replace => replaceCommandBindings,
                KeyCategory.Script => scriptCommandBindings,
                _ => mainCommandBindings,
            };
        }

        public static void RegisterCommand(KeyCategory category, string commandName, string labelKey, string defaultKeyGesture)
        {
            var list = GetList(category);
            list.Add(new(commandName, labelKey, defaultKeyGesture));
        }

        public static List<KeyBindingInfo> GetCommandGestures(KeyCategory category)
        {
            var list = GetList(category);
            return [.. list.Where(r => !string.IsNullOrEmpty(r.KeyGesture))];
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

    internal partial class KeyGestureLocalizer
    {
        public static string LocalizeKeyGestureText(string input)
        {
            if (!string.IsNullOrEmpty(input) &&
                TryParse(input, out Key key, out ModifierKeys modifierKeys))
            {
                List<string> modifiers = [];
                if (modifierKeys.HasFlag(ModifierKeys.Control))
                {
                    modifiers.Add(Resources.Keyboard_ControlKey);
                }
                if (modifierKeys.HasFlag(ModifierKeys.Shift))
                {
                    modifiers.Add(Resources.Keyboard_ShiftKey);
                }
                if (modifierKeys.HasFlag(ModifierKeys.Alt))
                {
                    modifiers.Add(Resources.Keyboard_AltKey);
                }

                return string.Format("{0}+{1}", string.Join("+", modifiers), key);
            }
            return input;
        }

        [GeneratedRegex(@"((\bControl\b)|(\bCtrl\b)|(\bShift\b)|(\bAlt\b)|(\b\w+$))")]
        private static partial Regex ShortcutKeysRegex();

        public static bool TryParse(string input, out Key key, out ModifierKeys modifiers)
        {
            key = Key.None;
            modifiers = ModifierKeys.None;

            var matches = ShortcutKeysRegex().Matches(input);

            if (matches.Count > 1)
            {
                foreach (Match match in matches.SkipLast(1))
                {
                    switch (match.Value)
                    {
                        case nameof(ModifierKeys.Alt):
                            modifiers |= ModifierKeys.Alt;
                            break;
                        case nameof(ModifierKeys.Control):
                        case "Ctrl":
                            modifiers |= ModifierKeys.Control;
                            break;
                        case nameof(ModifierKeys.Shift):
                            modifiers |= ModifierKeys.Shift;
                            break;
                    }
                }
                string keyStr = matches.Last().Value;
                if (!string.IsNullOrEmpty(keyStr) && Enum.TryParse<Key>(keyStr, out Key value) &&
                    modifiers > 0)
                {
                    key = value;
                    return true;
                }
            }

            return false;
        }
    }


    public class KeyBindingInfo
    {
        public KeyBindingInfo(string commandName, string labelKey, string defaultKeyGesture)
        {
            CommandName = commandName;
            LabelKey = labelKey;
            DefaultKeyGesture = defaultKeyGesture;
        }

        public string CommandName { get; private set; }

        public string LabelKey { get; private set; }

        public string DefaultKeyGesture { get; private set; }

        private string? userKeyGesture;
        public string KeyGesture
        {
            get { return userKeyGesture ?? DefaultKeyGesture; }
            set
            {
                if (userKeyGesture != value)
                    userKeyGesture = value;
            }
        }

    }

}
