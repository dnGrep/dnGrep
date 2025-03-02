using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using dnGREP.Common;
using dnGREP.Localization.Properties;
using NLog;

namespace dnGREP.WPF
{
    public enum KeyCategory { Main, Bookmark, Replace, Script }

    internal static class KeyBindingManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string storageFileName = "dnGrep.keyboard.xml";
        private const string mutexId = "{ED634C6B-A7D3-465E-85C1-AD099AEA955E}";

        private static readonly Dictionary<KeyCategory, Dictionary<string, string>> settings = [];

        private static readonly List<KeyBindingInfo> mainCommandBindings = [];
        private static readonly List<KeyBindingInfo> bookmarkCommandBindings = [];
        private static readonly List<KeyBindingInfo> replaceCommandBindings = [];
        private static readonly List<KeyBindingInfo> scriptCommandBindings = [];

        public static IEnumerable<KeyBindingInfo> AllCommandBindings => mainCommandBindings
            .Concat(bookmarkCommandBindings)
            .Concat(replaceCommandBindings)
            .Concat(scriptCommandBindings);

        private static List<KeyBindingInfo> GetList(KeyCategory category)
        {
            return category switch
            {
                KeyCategory.Bookmark => bookmarkCommandBindings,
                KeyCategory.Replace => replaceCommandBindings,
                KeyCategory.Script => scriptCommandBindings,
                _ => mainCommandBindings,
            };
        }

        public static void RegisterCommand(KeyCategory category,
            string commandName, string labelKey, string defaultKeyGesture)
        {
            var list = GetList(category);
            if (list.Any(r => r.CommandName.Equals(commandName, StringComparison.Ordinal)))
                return;

            // look up the user's key gesture
            string keyGesture = string.Empty;
            if (settings.TryGetValue(category, out Dictionary<string, string>? commands) &&
                commands.TryGetValue(commandName, out string? gesture))
            {
                keyGesture = gesture;
            }

            list.Add(new(category, commandName, labelKey, defaultKeyGesture, keyGesture));
        }

        public static void RegisterCustomEditor(KeyCategory category, string editorLabel)
        {
            string commandName = GetCommandName(editorLabel, "OpenWith");

            // look up the user's key gesture
            string keyGesture = string.Empty;
            if (settings.TryGetValue(category, out Dictionary<string, string>? commands) &&
                commands.TryGetValue(commandName, out string? gesture))
            {
                keyGesture = gesture;
            }

            var list = GetList(category);
            list.Add(new(category, commandName, editorLabel, keyGesture));
        }

        public static void RegisterScript(KeyCategory category, string editorLabel)
        {
            string commandName = GetCommandName(editorLabel, "RunScript");

            // look up the user's key gesture
            string keyGesture = string.Empty;
            if (settings.TryGetValue(category, out Dictionary<string, string>? commands) &&
                commands.TryGetValue(commandName, out string? gesture))
            {
                keyGesture = gesture;
            }

            var list = GetList(category);
            list.Add(new(category, commandName, editorLabel, keyGesture));
        }

        public static List<KeyBindingInfo> GetCategoryCommands(KeyCategory category)
        {
            return GetList(category);
        }

        public static List<KeyBindingInfo> GetCommandGestures(KeyCategory category)
        {
            var list = GetList(category);
            return [.. list.Where(r => !string.IsNullOrEmpty(r.KeyGesture))];
        }

        public static KeyBindingInfo? GetCustomEditorGesture(KeyCategory category, string editorLabel)
        {
            var list = GetList(category);
            var cmd = list.FirstOrDefault(r => r.CommandName.Equals(
                GetCommandName(editorLabel, "OpenWith"), StringComparison.Ordinal));
            if (cmd != null && !string.IsNullOrEmpty(cmd.KeyGesture))
                return cmd;
            return null;
        }

        public static KeyBindingInfo? GetRunScriptGesture(KeyCategory category, string editorLabel)
        {
            var list = GetList(category);
            var cmd = list.FirstOrDefault(r => r.CommandName.Equals(
                GetCommandName(editorLabel, "RunScript"), StringComparison.Ordinal));
            if (cmd != null && !string.IsNullOrEmpty(cmd.KeyGesture))
                return cmd;
            return null;
        }

        private static string GetCommandName(string label, string prefix)
        {
            return string.Concat(prefix, '_', label.Replace(' ', '_'));
        }

        public static KeyBinding CreateKeyBinding(ICommand command, string keyGesture)
        {
            if (command is RelayCommand cmd)
                cmd.KeyGestureText = keyGesture;

            KeyBinding kb = new(command, new KeyGestureConverter().ConvertFromString(keyGesture) as KeyGesture);
            kb.Freeze();
            return kb;
        }

        public static void ModifyKeyBinding(KeyBinding keyBinding, string keyGesture)
        {
            if (keyBinding.Command is RelayCommand cmd)
                cmd.KeyGestureText = keyGesture;
            keyBinding.Gesture = new KeyGestureConverter().ConvertFromString(keyGesture) as KeyGesture;
        }

        internal static void LoadBindings()
        {
            LoadV1(Path.Combine(DirectoryConfiguration.Instance.DataDirectory, storageFileName));
        }

        private static void LoadV1(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (stream != null)
                    {
                        XDocument doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
                        if (doc != null && doc.Root != null && doc.Root.Name.LocalName.Equals("keyBindings", StringComparison.Ordinal))
                        {
                            settings.Clear();

                            foreach (XElement elem in doc.Root.Descendants("keyBinding"))
                            {
                                if (elem.Attribute("category") is XAttribute key1 &&
                                    !string.IsNullOrEmpty(key1.Value) &&
                                    elem.Attribute("commandName") is XAttribute key2 &&
                                    !string.IsNullOrEmpty(key2.Value) &&
                                    elem.Attribute("keyGesture") is XAttribute gesture &&
                                    !string.IsNullOrEmpty(gesture.Value))
                                {
                                    if (Enum.TryParse(key1.Value, out KeyCategory category))
                                    {
                                        if (!settings.TryGetValue(category, out Dictionary<string, string>? commands))
                                        {
                                            commands = [];
                                            settings.Add(category, commands);
                                        }

                                        commands[key2.Value] = gesture.Value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load key bindings: " + ex.Message);
            }
        }

        internal static void SaveBindings()
        {
            string filePath = Path.Combine(DirectoryConfiguration.Instance.DataDirectory, storageFileName);
            using var mutex = new Mutex(false, mutexId);
            bool hasHandle = false;
            try
            {
                try
                {
                    hasHandle = mutex.WaitOne(5000, false);
                    if (hasHandle == false)
                    {
                        logger.Info("Timeout waiting for exclusive access to save key bindings.");
                        return;
                    }
                }
                catch (AbandonedMutexException)
                {
                    // The mutex was abandoned in another process,
                    // it will still get acquired
                    hasHandle = true;
                }

                // Perform work here.
                var dirName = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                SaveV1(filePath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to save key bindings: " + ex.Message);
            }
            finally
            {
                if (hasHandle)
                    mutex.ReleaseMutex();
            }

            // reload the settings dictionary
            LoadBindings();
        }

        private static void SaveV1(string filePath)
        {
            // write to temp file, then replace the original file
            string tempFile = filePath + "~";
            using (FileStream stream = File.OpenWrite(tempFile))
            using (XmlWriter xmlStream = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
            {
                if (xmlStream == null)
                    return;

                XNamespace xml = "http://www.w3.org/XML/1998/namespace";

                XDocument doc = new();
                doc.Add(new XElement("keyBindings"));
                if (doc.Root != null)
                {
                    doc.Root.SetAttributeValue("version", "1");
                    doc.Root.SetAttributeValue(XNamespace.Xmlns + "xml", xml);
                    foreach (var item in AllCommandBindings)
                    {
                        item.Serialize(doc.Root);
                    }
                }
                doc.Save(xmlStream);
            }
            File.Copy(tempFile, filePath, true);
            Utils.DeleteFile(tempFile);
        }
    }

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


    public class KeyBindingInfo : IEquatable<KeyBindingInfo>
    {
        public KeyBindingInfo(KeyCategory category, string commandName,
            string labelKey, string defaultKeyGesture, string keyGesture)
        {
            Category = category;
            CommandName = commandName;
            LabelKey = labelKey;
            Label = string.Empty;
            DefaultKeyGesture = defaultKeyGesture;
            if (!string.IsNullOrEmpty(keyGesture))
                KeyGesture = keyGesture;
        }

        public KeyBindingInfo(KeyCategory category, string commandName, string label,
            string keyGesture)
        {
            Category = category;
            CommandName = commandName;
            LabelKey = string.Empty;
            Label = label;
            DefaultKeyGesture = string.Empty;
            if (!string.IsNullOrEmpty(keyGesture))
                KeyGesture = keyGesture;
        }

        public KeyCategory Category { get; private set; }

        public string CommandName { get; private set; }

        public string LabelKey { get; private set; }

        public string Label { get; private set; }

        public string DefaultKeyGesture { get; private set; }

        private string? userKeyGesture;
        public string KeyGesture
        {
            get { return userKeyGesture ?? DefaultKeyGesture; }
            set
            {
                if (value == DefaultKeyGesture)
                {
                    userKeyGesture = null;
                }
                else if (userKeyGesture != value)
                {
                    userKeyGesture = value;
                }
            }
        }

        override public int GetHashCode()
        {
            return HashCode.Combine(Category, CommandName);
        }

        public bool Equals(KeyBindingInfo? other)
        {
            if (other == null)
                return false;

            return Category == other.Category &&
                CommandName == other.CommandName;
        }

        public override bool Equals(object? obj)
        {
            if (obj is KeyBindingInfo other)
                return Equals(other);
            return false;
        }

        public override string ToString()
        {
            return $"{Category} {CommandName} {KeyGesture}";
        }

        internal void Serialize(XElement parent)
        {
            if (!string.IsNullOrEmpty(KeyGesture))
            {
                var elem = new XElement("keyBinding");
                elem.Add(new XAttribute("category", Category.ToString("G")));
                elem.Add(new XAttribute("commandName", CommandName));
                elem.Add(new XAttribute("keyGesture", KeyGesture));
                parent.Add(elem);
            }
        }
    }
}
