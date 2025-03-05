using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    internal partial class KeyGestureLocalizer
    {
        public static string LocalizeKeyGestureText(string input)
        {
            if (!string.IsNullOrEmpty(input) &&
                TryParse(input, out Key key, out ModifierKeys modifierKeys))
            {
                string keyString = KeyToString(key);

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

                if (modifiers.Count == 0)
                {
                    return keyString;
                }
                else
                {
                    return string.Format("{0}+{1}", string.Join("+", modifiers), keyString);
                }
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

            if (matches.Count > 0)
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
                if (!string.IsNullOrEmpty(keyStr) && Enum.TryParse<Key>(keyStr, out Key value))
                {
                    key = value;
                    return true;
                }
            }

            return false;
        }

        public static string ToString(KeyCategory category)
        {
            return category switch
            {
                KeyCategory.Main => Resources.Window_Main,
                KeyCategory.Replace => Resources.Replace_Title,
                KeyCategory.Script => Resources.Script_Editor_Title,
                KeyCategory.Bookmark => Resources.Bookmarks_Title,
                _ => string.Empty,
            };
        }

        public static bool KeyNeedsModifier(Key key)
        {
            return key switch
            {
                Key.F1 or Key.F2 or Key.F3 or Key.F4 or Key.F5 or Key.F6 or Key.F7 or Key.F8 or
                Key.F9 or Key.F10 or Key.F11 or Key.F12 or Key.F13 or Key.F14 or Key.F15 or
                Key.F16 or Key.F17 or Key.F18 or Key.F19 or Key.F20 or Key.F21 or Key.F22 or
                Key.F23 or Key.F24 or Key.Escape or Key.Delete or Key.Back => false,
                _ => true,
            };
        }

        public static string KeyToString(Key key)
        {
            if (keyMap.TryGetValue(key, out string? value))
                return value;

            string? str = keyConverter.ConvertToString(key);
            if (str != null)
                return str;

            return key.ToString();
        }

        private static readonly KeyConverter keyConverter = new();

        private static readonly Dictionary<Key, string> keyMap = new Dictionary<Key, string>
        {
            {Key.D0, "0"},
            {Key.D1, "1"},
            {Key.D2, "2"},
            {Key.D3, "3"},
            {Key.D4, "4"},
            {Key.D5, "5"},
            {Key.D6, "6"},
            {Key.D7, "7"},
            {Key.D8, "8"},
            {Key.D9, "9"},
            {Key.OemTilde, "~"},
            {Key.OemMinus, "-"},
            {Key.OemPlus, "="},
            {Key.OemOpenBrackets, "["},
            {Key.OemCloseBrackets, "]"},
            {Key.OemPipe, "\\"},
            {Key.OemSemicolon, ";"},
            {Key.OemQuotes, "'"},
            {Key.OemComma, ","},
            {Key.OemPeriod, "."},
            {Key.OemQuestion, "/"},
        };
    }


}
