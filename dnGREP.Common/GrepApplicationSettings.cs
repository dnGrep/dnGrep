using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using NLog;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Common
{
    /// <summary>
    /// Singleton class used to maintain and persist application settings
    /// </summary>
    public class GrepSettings : SerializableDictionary
    {
        public static class Key
        {
            public const string SearchFolder = "SearchFolder";
            public const string SearchFor = "SearchFor";
            public const string ReplaceWith = "ReplaceWith";
            [DefaultValue(true)]
            public const string IncludeHidden = "IncludeHidden";
            [DefaultValue(true)]
            public const string IncludeBinary = "IncludeBinary";
            [DefaultValue(true)]
            public const string IncludeArchive = "IncludeArchive";
            [DefaultValue(true)]
            public const string IncludeSubfolder = "IncludeSubfolder";
            [DefaultValue(-1)]
            public const string MaxSubfolderDepth = "MaxSubfolderDepth";
            [DefaultValue(SearchType.Regex)]
            public const string TypeOfSearch = "TypeOfSearch";
            [DefaultValue(FileSearchType.Asterisk)]
            public const string TypeOfFileSearch = "TypeOfFileSearch";
            [DefaultValue(-1)]
            public const string CodePage = "CodePage";
            [DefaultValue("*.*")]
            public const string FilePattern = "FilePattern";
            public const string FilePatternIgnore = "FilePatternIgnore";
            [DefaultValue(true)]
            public const string UseGitignore = "UseGitignore";
            [DefaultValue(FileSizeFilter.No)]
            public const string UseFileSizeFilter = "UseFileSizeFilter";
            public const string CaseSensitive = "CaseSensitive";
            [DefaultValue(true)]
            public const string PreviewFileContent = "PreviewFileContent";
            public const string Multiline = "Multiline";
            public const string Singleline = "Singleline";
            public const string StopAfterFirstMatch = "StopAfterFirstMatch";
            public const string WholeWord = "WholeWord";
            public const string BooleanOperators = "BooleanOperators";
            public const string SizeFrom = "SizeFrom";
            [DefaultValue(100)]
            public const string SizeTo = "SizeTo";
            [DefaultValue(0.5)]
            public const string FuzzyMatchThreshold = "FuzzyMatchThreshold";
            [DefaultValue(true)]
            public const string ShowLinesInContext = "ShowLinesInContext";
            [DefaultValue(2)]
            public const string ContextLinesBefore = "ContextLinesBefore";
            [DefaultValue(3)]
            public const string ContextLinesAfter = "ContextLinesAfter";
            [DefaultValue(true)]
            public const string EnableUpdateChecking = "EnableUpdateChecking";
            [DefaultValue(10)]
            public const string UpdateCheckInterval = "UpdateCheckInterval";
            public const string LastCheckedVersion = "LastCheckedVersion";
            [DefaultValue(true)]
            public const string ShowFilePathInResults = "ShowFilePathInResults";
            [DefaultValue(true)]
            public const string AllowSearchingForFileNamePattern = "AllowSearchingForFileNamePattern";
            [DefaultValue(true)]
            public const string DetectEncodingForFileNamePattern = "DetectEncodingForFileNamePattern";
            public const string CustomEditor = "CustomEditor";
            public const string CustomEditorArgs = "CustomEditorArgs";
            public const string CompareApplication = "CompareApplication";
            public const string CompareApplicationArgs = "CompareApplicationArgs";
            public const string ExpandResults = "ExpandResults";
            [DefaultValue(true)]
            public const string ShowVerboseMatchCount = "ShowVerboseMatchCount";
            [DefaultValue(false)]
            public const string IsFiltersExpanded = "IsFiltersExpanded";
            public const string FileFilters = "FileFilters";
            public const string FastSearchBookmarks = "FastSearchBookmarks";
            public const string FastReplaceBookmarks = "FastReplaceBookmarks";
            public const string FastFileMatchBookmarks = "FastFileMatchBookmarks";
            public const string FastFileNotMatchBookmarks = "FastFileNotMatchBookmarks";
            public const string FastPathBookmarks = "FastPathBookmarks";
            [DefaultValue(12)]
            public const string PreviewWindowFont = "PreviewWindowFont";
            [DefaultValue(false)]
            public const string PreviewWindowWrap = "PreviewWindowWrap";
            [DefaultValue(20)]
            public const string MaxPathBookmarks = "MaxPathBookmarks";
            [DefaultValue(20)]
            public const string MaxSearchBookmarks = "MaxSearchBookmarks";
            [DefaultValue(10)]
            public const string MaxExtensionBookmarks = "MaxExtensionBookmarks";
            [DefaultValue(true)]
            public const string OptionsOnMainPanel = "OptionsOnMainPanel";
            [DefaultValue(FileDateFilter.None)]
            public const string UseFileDateFilter = "UseFileDateFilter";
            [DefaultValue(FileTimeRange.None)]
            public const string TypeOfTimeRangeFilter = "TypeOfTimeRangeFilter";
            public const string StartDate = "StartDate";
            public const string EndDate = "EndDate";
            [DefaultValue(0)]
            public const string HoursFrom = "HoursFrom";
            [DefaultValue(8)]
            public const string HoursTo = "HoursTo";
            [DefaultValue(true)]
            public const string SearchParallel = "SearchParallel";
            [DefaultValue(4.0)]
            public const string MatchTimeout = "MatchTimeout";
            [DefaultValue(14)]
            public const string ReplaceWindowFontSize = "ReplaceWindowFontSize";
            [DefaultValue(false)]
            public const string ReplaceWindowWrap = "ReplaceWindowWrap";
            [DefaultValue(true)]
            public const string FollowWindowsTheme = "FollowWindowsTheme";
            [DefaultValue("Light")]
            public const string CurrentTheme = "CurrentTheme";
            [DefaultValue("en")]
            public const string CurrentCulture = "CurrentCulture";
            [DefaultValue(SortType.FileNameDepthFirst)]
            public const string TypeOfSort = "TypeOfSort";
            [DefaultValue(ListSortDirection.Ascending)]
            public const string SortDirection = "SortDirection";
            [DefaultValue(true)]
            public const string ShowFileInfoTooltips = "ShowFileInfoTooltips";
            [DefaultValue(true)]
            public const string HighlightMatches = "HighlightMatches";
            [DefaultValue(true)]
            public const string ShowResultOptions = "ShowResultOptions";
            [DefaultValue(1.0)]
            public const string ResultsTreeScale = "ResultsTreeScale";
            [DefaultValue(false)]
            public const string ResultsTreeWrap = "ResultsTreeWrap";
            [DefaultValue(false)]
            public const string HighlightCaptureGroups = "HighlightCaptureGroups";
            [DefaultValue(true)]
            public const string UseDefaultFont = "UseDefaultFont";
            public const string ApplicationFontFamily = "ApplicationFontFamily";
            public const string MainFormFontSize = "MainFormFontSize";
            public const string ReplaceFormFontSize = "ReplaceFormFontSize";
            public const string DialogFontSize = "DialogFontSize";
            public const string ResultsFontFamily = "ResultsFontFamily";
            public const string ResultsFontSize = "ResultsFontSize";
            [DefaultValue("-layout -enc UTF-8 -bom")]
            public const string PdfToTextOptions = "PdfToTextOptions";
            [DefaultValue(false)]
            public const string FollowSymlinks = "FollowSymlinks";
            public const string MainWindowState = "MainWindowState";
            public const string MainWindowBounds = "MainWindowBounds";
            public const string ReplaceBounds = "ReplaceBounds";
            public const string PreviewBounds = "PreviewBounds";
            public const string PreviewWindowState = "PreviewWindowState";
            public const string PreviewDocked = "PreviewDocked";
            public const string PreviewDockSide = "PreviewDockSide";
            public const string PreviewDockedWidth = "PreviewDockedWidth";
            public const string PreviewDockedHeight = "PreviewDockedHeight";
            public const string PreviewHidden = "PreviewHidden";
            [DefaultValue(true)]
            public const string PreviewAutoPosition = "PreviewAutoPosition";
            [DefaultValue(false)]
            public const string CaptureGroupSearch = "CaptureGroupSearch";
            [DefaultValue(16)]
            public const string HexResultByteLength = "HexResultByteLength";
            [DefaultValue(true)]
            public const string ShowFullReplaceDialog = "ShowFullReplaceDialog";
        }

        private static GrepSettings instance;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string storageFileName = "dnGREP.Settings.dat";
        private const string mutexId = "{83D660FA-E399-4BBC-A3FC-09897115D2E2}";
        public readonly static string DefaultMonospaceFontFamily = "Consolas";

        private GrepSettings() { }

        public static GrepSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GrepSettings();
                    instance.Load();
                }
                return instance;
            }
        }

        /// <summary>
        /// Loads settings from default location - baseFolder\\dnGREP.Settings.dat
        /// </summary>
        public void Load()
        {
            Load(Path.Combine(Utils.GetDataFolderPath(), storageFileName));
        }

        /// <summary>
        /// Load settings from location specified
        /// </summary>
        /// <param name="path">Path to settings file</param>
        public void Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (stream == null)
                            return;
                        XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary));
                        this.Clear();
                        SerializableDictionary appData = (SerializableDictionary)serializer.Deserialize(stream);
                        foreach (KeyValuePair<string, string> pair in appData)
                            this[pair.Key] = pair.Value;
                    }
                }
                InitializeFonts();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load settings: " + ex.Message);
            }
        }

        private void InitializeFonts()
        {
            if (!TryGetValue(Key.ApplicationFontFamily, out string value) ||
                string.IsNullOrWhiteSpace(value))
            {
                Set(Key.ApplicationFontFamily, SystemFonts.MessageFontFamily.Source);
            }

            if (Get<double>(Key.MainFormFontSize) == 0)
            {
                Set(Key.MainFormFontSize, SystemFonts.MessageFontSize);
            }

            if (Get<double>(Key.ReplaceFormFontSize) == 0)
            {
                Set(Key.ReplaceFormFontSize, SystemFonts.MessageFontSize);
            }

            if (Get<double>(Key.DialogFontSize) == 0)
            {
                Set(Key.DialogFontSize, SystemFonts.MessageFontSize);
            }

            if (!TryGetValue(Key.ResultsFontFamily, out string value2) ||
                string.IsNullOrWhiteSpace(value2))
            {
                Set(Key.ResultsFontFamily, DefaultMonospaceFontFamily);
            }

            if (Get<double>(Key.ResultsFontSize) == 0)
            {
                Set(Key.ResultsFontSize, SystemFonts.MessageFontSize);
            }
        }

        /// <summary>
        /// Saves settings to default location - baseFolder\\dnGREP.Settings.dat
        /// </summary>
        public void Save()
        {
            Save(Path.Combine(Utils.GetDataFolderPath(), storageFileName));
        }

        /// <summary>
        /// Saves settings to location specified
        /// </summary>
        /// <param name="path">Path to settings file</param>
        public void Save(string path)
        {
            // don't save in warmUp mode
            if (Environment.GetCommandLineArgs().Contains("/warmUp", StringComparison.OrdinalIgnoreCase))
                return;

            using (var mutex = new Mutex(false, mutexId))
            {
                bool hasHandle = false;
                try
                {
                    try
                    {
                        hasHandle = mutex.WaitOne(5000, false);
                        if (hasHandle == false)
                        {
                            logger.Info("Timeout waiting for exclusive access to save app settings.");
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
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                    // Create temp file in case save crashes
                    using (FileStream stream = File.OpenWrite(path + "~"))
                    using (XmlWriter xmlStream = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
                    {
                        if (xmlStream == null)
                            return;
                        XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary));
                        serializer.Serialize(xmlStream, this);
                    }
                    File.Copy(path + "~", path, true);
                    Utils.DeleteFile(path + "~");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to save app settings: " + ex.Message);
                }
                finally
                {
                    if (hasHandle)
                        mutex.ReleaseMutex();
                }
            }
        }

        public new string this[string key]
        {
            get { return ContainsKey(key) ? base[key] : null; }
            set { base[key] = value; }
        }

        /// <summary>
        /// Gets value of object in dictionary and deserializes it to specified type
        /// </summary>
        /// <typeparam name="T">Type of object to deserialize from</typeparam>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            string value = this[key];

            if (value == null)
                return GetDefaultValue<T>(key);

            try
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)Convert.ChangeType(value.Replace("&#010;", "\n").Replace("&#013;", "\r"), typeof(string));
                }
                else if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), value);
                }
                else if (typeof(T) == typeof(Rect))
                {
                    return (T)Convert.ChangeType(Rect.Parse(value), typeof(Rect));
                }
                else if (!typeof(T).IsPrimitive)
                {
                    using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(value)))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        return (T)formatter.Deserialize(stream);
                    }
                }
                else
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch
            {
                return GetDefaultValue<T>(key);
            }
        }

        /// <summary>
        /// Special handling for nullable value
        /// </summary>
        public void SetNullableDateTime(string key, DateTime? value)
        {
            Set<DateTime>(key, value ?? DateTime.MinValue);
        }

        /// <summary>
        /// Special handling for nullable value
        /// </summary>
        public DateTime? GetNullableDateTime(string key)
        {
            DateTime dt = Get<DateTime>(key);
            if (dt == DateTime.MinValue)
                return null;
            return dt;
        }

        /// <summary>
        /// Returns true if the value is set; otherwise false
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsSet(string key)
        {
            string value = this[key];

            if (string.IsNullOrWhiteSpace(value))
                return false;
            else
                return true;
        }

        /// <summary>
        /// Sets value of object in dictionary and serializes it to specified type
        /// </summary>
        /// <typeparam name="T">Type of object to serialize into</typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set<T>(string key, T value)
        {
            if (value == null)
                return;

            if (typeof(T) == typeof(string))
            {
                this[key] = value.ToString().Replace("\n", "&#010;").Replace("\r", "&#013;");
            }
            else if (typeof(T).IsEnum)
            {
                this[key] = value.ToString();
            }
            else if (typeof(T) == typeof(Rect))
            {
                Rect rect = (Rect)Convert.ChangeType(value, typeof(Rect));
                // need invariant culture for Rect.Parse to work
                this[key] = rect.ToString(CultureInfo.InvariantCulture);
            }
            else if (!typeof(T).IsPrimitive)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, value);
                    stream.Position = 0;
                    this[key] = Convert.ToBase64String(stream.ToArray());
                }
            }
            else
            {
                this[key] = value.ToString();
            }
        }

        private List<FieldInfo> constantKeys;
        private T GetDefaultValue<T>(string key)
        {
            if (constantKeys == null)
            {
                constantKeys = new List<FieldInfo>();
                FieldInfo[] thisObjectProperties = typeof(Key).GetFields();
                foreach (FieldInfo fi in thisObjectProperties)
                {
                    if (fi.IsLiteral)
                    {
                        constantKeys.Add(fi);
                    }
                }
            }
            FieldInfo info = constantKeys.Find(fi => fi.Name == key);
            if (info == null)
                return default;

            if (info.GetCustomAttributes(typeof(DefaultValueAttribute), false) is DefaultValueAttribute[] attr && attr.Length == 1)
                return (T)attr[0].Value;

            return default;
        }
    }



    /// <summary>
    /// Serializable generic dictionary 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [XmlRoot("dictionary")]
    public class SerializableDictionary : Dictionary<string, string>, IXmlSerializable
    {
        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;

            if (wasEmpty)
                return;

            reader.Read();
            while (reader.NodeType == XmlNodeType.Element)
            {
                string key = reader.GetAttribute("key");
                string value = reader.ReadElementContentAsString();
                this[key] = value;
            }
            reader.ReadEndElement();
        }



        public void WriteXml(System.Xml.XmlWriter writer)
        {
            foreach (var key in this.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteAttributeString("key", key);
                string value = this[key];
                writer.WriteString(value);
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}
