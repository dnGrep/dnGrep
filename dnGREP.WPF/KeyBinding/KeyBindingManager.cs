using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using dnGREP.Common;
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

            list.Add(new(category, commandName, labelKey, defaultKeyGesture, keyGesture, settings.Count > 0));
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
            list.Add(new(category, commandName, editorLabel, keyGesture, settings.Count > 0));
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
            list.Add(new(category, commandName, editorLabel, keyGesture, settings.Count > 0));
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

    public class KeyBindingInfo : IEquatable<KeyBindingInfo>
    {
        public KeyBindingInfo(KeyCategory category, string commandName,
            string labelKey, string defaultKeyGesture, string keyGesture,
            bool hasUserConfiguration)
        {
            Category = category;
            CommandName = commandName;
            LabelKey = labelKey;
            Label = string.Empty;
            DefaultKeyGesture = defaultKeyGesture;
            if (!string.IsNullOrEmpty(keyGesture))
                KeyGesture = keyGesture;
            HasUserConfiguration = hasUserConfiguration;
        }

        public KeyBindingInfo(KeyCategory category, string commandName, string label,
            string keyGesture, bool hasUserConfiguration)
        {
            Category = category;
            CommandName = commandName;
            LabelKey = string.Empty;
            Label = label;
            DefaultKeyGesture = string.Empty;
            if (!string.IsNullOrEmpty(keyGesture))
                KeyGesture = keyGesture;
            HasUserConfiguration = hasUserConfiguration;
        }

        public bool HasUserConfiguration { get; private set; }

        public KeyCategory Category { get; private set; }

        public string CommandName { get; private set; }

        public string LabelKey { get; private set; }

        public string Label { get; private set; }

        public string DefaultKeyGesture { get; private set; }

        private string? userKeyGesture;
        public string KeyGesture
        {
            get { return HasUserConfiguration ? userKeyGesture ?? string.Empty : DefaultKeyGesture; }
            set
            {
                if (userKeyGesture != value)
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
