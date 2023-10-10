using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using NLog;

namespace dnGREP.Common
{
    /// <summary>
    /// Singleton class used to maintain and persist application settings
    /// </summary>
    public class GrepSettings
    {
        public static class Key
        {
            [DefaultValue("")]
            public const string SearchFolder = "SearchFolder";
            [DefaultValue("")]
            public const string SearchFor = "SearchFor";
            [DefaultValue("")]
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
            [DefaultValue("")]
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
            [DefaultValue("")]
            public const string CustomEditor = "CustomEditor";
            [DefaultValue("")]
            public const string CustomEditorArgs = "CustomEditorArgs";
            [DefaultValue("")]
            public const string CompareApplication = "CompareApplication";
            [DefaultValue("")]
            public const string CompareApplicationArgs = "CompareApplicationArgs";
            public const string ExpandResults = "ExpandResults";
            [DefaultValue(true)]
            public const string ShowVerboseMatchCount = "ShowVerboseMatchCount";
            [DefaultValue(false)]
            public const string IsFiltersExpanded = "IsFiltersExpanded";
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
            [DefaultValue("")]
            public const string ApplicationFontFamily = "ApplicationFontFamily";
            public const string MainFormFontSize = "MainFormFontSize";
            public const string ReplaceFormFontSize = "ReplaceFormFontSize";
            public const string DialogFontSize = "DialogFontSize";
            [DefaultValue("")]
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
            [DefaultValue(true)]
            public const string DeleteToRecycleBin = "DeleteToRecycleBin";
            [DefaultValue(true)]
            public const string SkipRemoteCloudStorageFiles = "SkipRemoteCloudStorageFiles";

            [DefaultValue(false)]
            public const string PersonalizationOn = "PersonalizationOn";
            [DefaultValue(true)]
            public const string BookmarksVisible = "BookmarksVisible";
            [DefaultValue(true)]
            public const string TestExpressionVisible = "TestExpressionVisible";
            [DefaultValue(true)]
            public const string ReplaceVisible = "ReplaceVisible";
            [DefaultValue(true)]
            public const string SortVisible = "SortVisible";
            [DefaultValue(true)]
            public const string MoreVisible = "MoreVisible";
            [DefaultValue(true)]
            public const string SearchInArchivesVisible = "SearchInArchivesVisible";
            [DefaultValue(true)]
            public const string SizeFilterVisible = "SizeFilterVisible";
            [DefaultValue(true)]
            public const string SubfoldersFilterVisible = "SubfoldersFilterVisible";
            [DefaultValue(true)]
            public const string HiddenFilterVisible = "HiddenFilterVisible";
            [DefaultValue(true)]
            public const string BinaryFilterVisible = "BinaryFilterVisible";
            [DefaultValue(true)]
            public const string SymbolicLinkFilterVisible = "SymbolicLinkFilterVisible";
            [DefaultValue(true)]
            public const string DateFilterVisible = "DateFilterVisible";
            [DefaultValue(true)]
            public const string SearchParallelVisible = "SearchParallelVisible";
            [DefaultValue(true)]
            public const string UseGitIgnoreVisible = "UseGitIgnoreVisible";
            [DefaultValue(true)]
            public const string SkipCloudStorageVisible = "SkipCloudStorageVisible";
            [DefaultValue(true)]
            public const string EncodingVisible = "EncodingVisible";
            [DefaultValue(true)]
            public const string SearchTypeRegexVisible = "SearchTypeRegexVisible";
            [DefaultValue(true)]
            public const string SearchTypeXPathVisible = "SearchTypeXPathVisible";
            [DefaultValue(true)]
            public const string SearchTypeTextVisible = "SearchTypeTextVisible";
            [DefaultValue(true)]
            public const string SearchTypePhoneticVisible = "SearchTypePhoneticVisible";
            [DefaultValue(true)]
            public const string SearchTypeByteVisible = "SearchTypeByteVisible";
            [DefaultValue(true)]
            public const string BooleanOperatorsVisible = "BooleanOperatorsVisible";
            [DefaultValue(true)]
            public const string CaptureGroupSearchVisible = "CaptureGroupSearchVisible";
            [DefaultValue(true)]
            public const string SearchInResultsVisible = "SearchInResultsVisible";
            [DefaultValue(true)]
            public const string PreviewFileVisible = "PreviewFileVisible";
            [DefaultValue(true)]
            public const string StopAfterFirstMatchVisible = "StopAfterFirstMatchVisible";
            [DefaultValue(true)]
            public const string HighlightMatchesVisible = "HighlightMatchesVisible";
            [DefaultValue(true)]
            public const string HighlightGroupsVisible = "HighlightGroupsVisible";
            [DefaultValue(true)]
            public const string ShowContextLinesVisible = "ShowContextLinesVisible";
            [DefaultValue(true)]
            public const string ZoomResultsTreeVisible = "ZoomResultsTreeVisible";
            [DefaultValue(true)]
            public const string WrapTextResultsTreeVisible = "WrapTextResultsTreeVisible";
            [DefaultValue(true)]
            public const string PreviewZoomWndVisible = "PreviewZoomWndVisible";
            [DefaultValue(true)]
            public const string WrapTextPreviewWndVisible = "WrapTextPreviewWndVisible";
            [DefaultValue(true)]
            public const string SyntaxPreviewWndVisible = "SyntaxPreviewWndVisible";
            [DefaultValue(Common.ReportMode.FullLine)]
            public const string ReportMode = "ReportMode";
            [DefaultValue(true)]
            public const string IncludeFileInformation = "IncludeFileInformation";
            [DefaultValue(false)]
            public const string TrimWhitespace = "TrimWhitespace";
            [DefaultValue(false)]
            public const string FilterUniqueValues = "FilterUniqueValues";
            [DefaultValue(Common.UniqueScope.PerFile)]
            public const string UniqueScope = "UniqueScope";
            [DefaultValue(false)]
            public const string OutputOnSeparateLines = "OutputOnSeparateLines";
            [DefaultValue(",")]
            public const string ListItemSeparator = "ListItemSeparator";
            [DefaultValue(false)]
            public const string RestoreLastModifiedDate = "RestoreLastModifiedDate";
            [DefaultValue(false)]
            public const string MaximizeResultsTreeOnSearch = "MaximizeResultsTreeOnSearch";
            [DefaultValue(PdfNumberType.PageNumber)]
            public const string PdfNumberStyle = "PdfNumberStyle";
            [DefaultValue(false)]
            public const string PinBookmarkWindow = "PinBookmarkWindow";
            [DefaultValue(OverwriteFile.Prompt)]
            public const string OverwriteFilesOnCopy = "OverwriteFilesOnCopy";
            [DefaultValue(OverwriteFile.Prompt)]
            public const string OverwriteFilesOnMove = "OverwriteFilesOnMove";
            [DefaultValue(-1)]
            public const string MaxDegreeOfParallelism = "MaxDegreeOfParallelism";
            [DefaultValue(true)]
            public const string PreserveFolderLayoutOnCopy = "PreserveFolderLayoutOnCopy";
            [DefaultValue(true)]
            public const string PreserveFolderLayoutOnMove = "PreserveFolderLayoutOnMove";
            [DefaultValue("")]
            public const string IgnoreFilter = "IgnoreFilter";
            [DefaultValue(false)]
            public const string IsSingletonInstance = "IsSingletonInstance";
            [DefaultValue(true)]
            public const string PassSearchFolderToSingleton = "PassSearchFolderToSingleton";
            [DefaultValue(false)]
            public const string ConfirmExitScript = "ConfirmExitScript";
            [DefaultValue(false)]
            public const string ConfirmExitSearch = "ConfirmExitSearch";
            [DefaultValue(10.0)]
            public const string ConfirmExitSearchDuration = "ConfirmExitSearchDuration";
        }

        private static GrepSettings? instance;
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

        private readonly Dictionary<string, string> settings = new();

        public int Version { get; private set; } = 1;

        /// <summary>
        /// Loads settings from default location - baseFolder\\dnGREP.Settings.dat
        /// </summary>
        public void Load()
        {
            Load(Path.Combine(Utils.GetDataFolderPath(), storageFileName));
        }

        public void Clear()
        {
            settings.Clear();
        }

        public int Count => settings.Count;

        public bool ContainsKey(string key)
        {
            return settings.ContainsKey(key);
        }

        /// <summary>
        /// Load settings from location specified
        /// </summary>
        /// <param name="path">Path to settings file</param>
        public void Load(string path)
        {
            // check file version
            Version = GetFileVersion(path);
            if (Version == 1)
            {
                LoadV1(path);
            }
            else if (Version == 2)
            {
                LoadV2(path);
            }

            InitializeFonts();
        }

        private static int GetFileVersion(string path)
        {
            if (File.Exists(path))
            {
                using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                XDocument doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
                if (doc != null)
                {
                    var attr = doc.Root?.Attribute("version");
                    if (attr != null && !string.IsNullOrEmpty(attr.Value) &&
                        int.TryParse(attr.Value, out int version))
                    {
                        return version;
                    }
                }
                return 1;
            }
            return 2;
        }

        private void LoadV1(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (stream == null)
                        return;

                    settings.Clear();
                    XmlSerializer serializer = new(typeof(SerializableDictionary));
                    if (serializer.Deserialize(stream) is SerializableDictionary appData)
                    {
                        foreach (KeyValuePair<string, string> pair in appData)
                            settings[pair.Key] = pair.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load settings: " + ex.Message);
            }
        }

        private void LoadV2(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (stream != null)
                    {
                        XDocument doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
                        if (doc != null && doc.Root != null && doc.Root.Name.LocalName.Equals("dictionary", StringComparison.Ordinal))
                        {
                            settings.Clear();

                            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

                            foreach (XElement elem in doc.Root.Descendants("item"))
                            {
                                if (elem.Attribute("key") is XAttribute key &&
                                    !string.IsNullOrEmpty(key.Value))
                                {
                                    var nil = elem.Attribute(xsi + "nil");
                                    if (nil != null && nil.Value == "true")
                                    {
                                        settings[key.Value] = "xsi:nil";
                                    }
                                    else if (elem.HasElements)
                                    {
                                        var elem2 = elem.Element("stringArray");
                                        if (elem2 != null)
                                        {
                                            settings[key.Value] = elem2.ToString();
                                        }
                                    }
                                    else
                                    {
                                        settings[key.Value] = elem.Value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load settings: " + ex.Message);
            }
        }

        private void InitializeFonts()
        {
            if (!settings.TryGetValue(Key.ApplicationFontFamily, out string? value) ||
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

            if (!settings.TryGetValue(Key.ResultsFontFamily, out string? value2) ||
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

            using var mutex = new Mutex(false, mutexId);
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
                var dirName = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(dirName);
                }

                SaveV2(path);
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

        private void SaveV2(string path)
        {
            // Create temp file in case save crashes
            using (FileStream stream = File.OpenWrite(path + "~"))
            using (XmlWriter xmlStream = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
            {
                if (xmlStream == null)
                    return;

                XNamespace xml = "http://www.w3.org/XML/1998/namespace";
                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

                XDocument doc = new();
                doc.Add(new XElement("dictionary"));
                doc.Root?.SetAttributeValue("version", "2");
                doc.Root?.SetAttributeValue(XNamespace.Xmlns + "xml", xml);
                doc.Root?.SetAttributeValue(XNamespace.Xmlns + "xsi", xsi);
                foreach (string key in settings.Keys)
                {
                    XElement elem;
                    if (!settings.TryGetValue(key, out string? value) ||
                        value == "xsi:nil")
                    {
                        elem = new XElement("item");
                        elem.SetAttributeValue("key", key);
                        elem.SetAttributeValue(xsi + "nil", "true");
                    }
                    else if (value.StartsWith("<stringArray", StringComparison.Ordinal))
                    {
                        elem = new XElement("item", XElement.Parse(value));
                        elem.SetAttributeValue("key", key);
                    }
                    else
                    {
                        elem = new XElement("item", value);
                        elem.SetAttributeValue("key", key);
                        if (!string.IsNullOrEmpty(value) && string.IsNullOrWhiteSpace(value))
                        {
                            elem.SetAttributeValue(xml + "space", "preserve");
                        }
                    }
                    doc.Root?.Add(elem);
                }

                doc.Save(xmlStream);
            }
            File.Copy(path + "~", path, true);
            Utils.DeleteFile(path + "~");
        }

        private static string Serialize(List<string> list)
        {
            if (list.Count > 0)
            {
                XNamespace xml = "http://www.w3.org/XML/1998/namespace";
                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

                XElement root = new("stringArray");
                foreach (string value in list)
                {
                    if (value == null)
                    {
                        var nil = new XElement("string");
                        nil.SetAttributeValue(xsi + "nil", "true");
                        root.Add(nil);
                    }
                    else
                    {
                        var elem = new XElement("string", value);
                        if (!string.IsNullOrEmpty(value) && string.IsNullOrWhiteSpace(value))
                        {
                            elem.SetAttributeValue(xml + "space", "preserve");
                        }
                        root.Add(elem);
                    }
                }

                return root.ToString();
            }
            return string.Empty;
        }

        private static List<string?> Deserialize(string xmlContent)
        {
            List<string?> list = new();

            if (!string.IsNullOrEmpty(xmlContent))
            {
                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
                XElement root = XElement.Parse(xmlContent, LoadOptions.PreserveWhitespace);
                if (root != null)
                {
                    foreach (var elem in root.Descendants("string"))
                    {
                        var nil = elem.Attribute(xsi + "nil");
                        if (nil != null && nil.Value.Equals("true", StringComparison.Ordinal))
                        {
                            list.Add(null);
                        }

                        list.Add(elem.Value);
                    }
                }
            }

            return list;
        }

        private static string SerializeMRU(List<MostRecentlyUsed> list)
        {
            if (list.Count > 0)
            {
                XNamespace xml = "http://www.w3.org/XML/1998/namespace";

                XElement root = new("stringArray");
                foreach (var item in list)
                {
                    XElement elem = new("string");
                    elem.SetAttributeValue("isPinned", item.IsPinned);
                    if (!string.IsNullOrEmpty(item.StringValue) && string.IsNullOrWhiteSpace(item.StringValue))
                    {
                        elem.SetAttributeValue(xml + "space", "preserve");
                    }
                    elem.Value = item.StringValue;
                    root.Add(elem);
                }

                return root.ToString();
            }
            return string.Empty;
        }

        private static List<MostRecentlyUsed> DeserializeMRU(string xmlContent)
        {
            List<MostRecentlyUsed> list = new();

            if (!string.IsNullOrEmpty(xmlContent))
            {
                XElement root = XElement.Parse(xmlContent, LoadOptions.PreserveWhitespace);
                if (root != null)
                {
                    foreach (var elem in root.Descendants("string"))
                    {
                        MostRecentlyUsed item = new();
                        if (elem.Attribute("isPinned") is XAttribute attr && !string.IsNullOrEmpty(attr.Value) &&
                            bool.TryParse(attr.Value, out bool isPinned))
                        {
                            item.IsPinned = isPinned;
                        }

                        item.StringValue = elem.Value;

                        list.Add(item);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Gets value of a nullable object in dictionary and deserializes it to specified type
        /// </summary>
        /// <typeparam name="T">Type of object to deserialize from</typeparam>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public T? GetNullable<T>(string key)
        {
            if (!settings.ContainsKey(key))
            {
                return GetDefaultValueNullable<T>(key);
            }

            if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value) || value == "xsi:nil")
            {
                return default;
            }

            try
            {
                return Get<T>(key);
            }
            catch (Exception)
            {
                return default;
            }
        }

        /// <summary>
        /// Gets value of object in dictionary and deserializes it to specified type
        /// </summary>
        /// <typeparam name="T">Type of object to deserialize from</typeparam>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            string? value = null;
            try
            {
                if (!settings.ContainsKey(key))
                {
                    return GetDefaultValue<T>(key);
                }

                if (settings.TryGetValue(key, out value))
                {
                    if (value == "xsi:nil")
                    {
                        if (typeof(T) == typeof(string))
                        {
                            value = string.Empty;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Null is not allowed for key {key}");
                        }
                    }

                    if (typeof(T) == typeof(string))
                    {
                        return (T)Convert.ChangeType(value.Replace("&#010;", "\n", StringComparison.Ordinal).Replace("&#013;", "\r", StringComparison.Ordinal), typeof(string));
                    }

                    if (typeof(T).IsEnum)
                    {
                        return (T)Enum.Parse(typeof(T), value);
                    }

                    if (typeof(T) == typeof(Point))
                    {
                        return (T)Convert.ChangeType(Point.Parse(value), typeof(Point));
                    }

                    if (typeof(T) == typeof(Rect))
                    {
                        return (T)Convert.ChangeType(Rect.Parse(value), typeof(Rect));
                    }

                    if (typeof(T) == typeof(List<string>))
                    {
                        List<string?> list;
                        if (!string.IsNullOrEmpty(value) && value.StartsWith("<stringArray", StringComparison.Ordinal))
                        {
                            list = Deserialize(value);
                        }
                        else
                        {
                            list = new();
                        }
                        return (T)Convert.ChangeType(list, typeof(List<string>));
                    }

                    if (typeof(T) == typeof(List<MostRecentlyUsed>))
                    {
                        List<MostRecentlyUsed> list;
                        if (!string.IsNullOrEmpty(value) && value.StartsWith("<stringArray", StringComparison.Ordinal))
                        {
                            list = DeserializeMRU(value);
                        }
                        else
                        {
                            list = new List<MostRecentlyUsed>();
                        }
                        return (T)Convert.ChangeType(list, typeof(List<MostRecentlyUsed>));
                    }

                    if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            DateTime? dt = value.FromIso8601DateTime();
                            if (dt.HasValue)
                            {
                                return (T)Convert.ChangeType(dt.Value, typeof(DateTime));
                            }
                            else
                            {
                                return GetDefaultValue<T>(key);
                            }
                        }
                    }

                    if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
                    {
                        if (bool.TryParse(value, out bool result))
                        {
                            return (T)Convert.ChangeType(result, typeof(bool));
                        }
                        else
                        {
                            return GetDefaultValue<T>(key);
                        }
                    }

                    if (typeof(T) == typeof(int))
                    {
                        if (int.TryParse(value, CultureInfo.InvariantCulture, out int result))
                        {
                            return (T)Convert.ChangeType(result, typeof(T));
                        }
                        else
                        {
                            return (T)Convert.ChangeType(0, typeof(T));
                        }
                    }

                    if (typeof(T) == typeof(float))
                    {
                        if (float.TryParse(value, CultureInfo.InvariantCulture, out float result))
                        {
                            return (T)Convert.ChangeType(result, typeof(T));
                        }
                        else
                        {
                            return (T)Convert.ChangeType(0, typeof(T));
                        }
                    }

                    if (typeof(T) == typeof(double))
                    {
                        if (double.TryParse(value, CultureInfo.InvariantCulture, out double result))
                        {
                            return (T)Convert.ChangeType(result, typeof(T));
                        }
                        else
                        {
                            return (T)Convert.ChangeType(0, typeof(T));
                        }
                    }

                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error converting settings key [{key}] of type [{typeof(T)}] from value [{value}]");
            }

            return GetDefaultValue<T>(key);
        }

        /// <summary>
        /// Returns true if the value is set; otherwise false
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsSet(string key)
        {
            return settings.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value);
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
            {
                settings[key] = "xsi:nil";
            }
            else if (typeof(T) == typeof(string) && value is string str)
            {
                settings[key] = str.Replace("\n", "&#010;", StringComparison.Ordinal).Replace("\r", "&#013;", StringComparison.Ordinal);
            }
            else if (typeof(T).IsEnum && value is Enum en)
            {
                settings[key] = en.ToString();
            }
            else if (typeof(T) == typeof(int))
            {
                int num = (int)Convert.ChangeType(value, typeof(int));
                settings[key] = num.ToString(CultureInfo.InvariantCulture);
            }
            else if (typeof(T) == typeof(float))
            {
                float num = (float)Convert.ChangeType(value, typeof(float));
                settings[key] = num.ToString(CultureInfo.InvariantCulture);
            }
            else if (typeof(T) == typeof(double))
            {
                double num = (double)Convert.ChangeType(value, typeof(double));
                settings[key] = num.ToString(CultureInfo.InvariantCulture);
            }
            else if (typeof(T) == typeof(Point))
            {
                Point pt = (Point)Convert.ChangeType(value, typeof(Point));
                // need invariant culture for Rect.Parse to work
                settings[key] = pt.ToString(CultureInfo.InvariantCulture);
            }
            else if (typeof(T) == typeof(Rect))
            {
                Rect rect = (Rect)Convert.ChangeType(value, typeof(Rect));
                // need invariant culture for Rect.Parse to work
                settings[key] = rect.ToString(CultureInfo.InvariantCulture);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime dt = (DateTime)Convert.ChangeType(value, typeof(DateTime));
                settings[key] = dt.ToIso8601DateTime();
            }
            else if (IsNullable(typeof(T)))
            {
                if (value is DateTime dt)
                {
                    settings[key] = dt.ToIso8601DateTime();
                }
                else
                {
                    settings[key] = value.ToString() ?? string.Empty;
                }
            }
            else if (value is List<string> list)
            {
                settings[key] = Serialize(list);
            }
            else if (value is List<MostRecentlyUsed> items)
            {
                settings[key] = SerializeMRU(items);
            }
            else
            {
                settings[key] = value.ToString() ?? string.Empty;
            }
        }

        private static bool IsNullable(Type type) => Nullable.GetUnderlyingType(type) != null;

        private List<FieldInfo>? constantKeys;

        private void InitializeConstantKeys()
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
        }

        private T GetDefaultValue<T>(string key)
        {
            InitializeConstantKeys();
            FieldInfo? info = constantKeys?.Find(fi => fi.Name == key);

            if (info != null &&
                info.GetCustomAttributes(typeof(DefaultValueAttribute), false) is DefaultValueAttribute[] attr &&
                attr.Length == 1)
            {
                if (attr[0].Value is T val)
                {
                    return val;
                }
                else
                {
                    throw new InvalidOperationException($"Null default is not allowed for key {key}");
                }
            }

            if (default(T) is T defValue)
            {
                return defValue;
            }
            else if (typeof(T) == typeof(List<string>))
            {
                return (T)Convert.ChangeType(new List<string>(), typeof(List<string>));
            }
            else if (typeof(T) == typeof(List<MostRecentlyUsed>))
            {
                return (T)Convert.ChangeType(new List<MostRecentlyUsed>(), typeof(List<MostRecentlyUsed>));
            }
            else
            {
                throw new InvalidOperationException($"Null default is not allowed for key {key}");
            }
        }

        private T? GetDefaultValueNullable<T>(string key)
        {
            InitializeConstantKeys();
            FieldInfo? info = constantKeys?.Find(fi => fi.Name == key);

            if (info != null &&
                info.GetCustomAttributes(typeof(DefaultValueAttribute), false) is DefaultValueAttribute[] attr &&
                attr.Length == 1)
            {
                return (T?)attr[0].Value;
            }

            return default;
        }

        public IList<string> GetExtensionList(string nameKey, IList<string> defaultExtensions)
        {
            nameKey = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nameKey)
                 .Replace(" ", "", StringComparison.Ordinal);
            string addKey = "Add" + nameKey + "Extensions";
            string remKey = "Rem" + nameKey + "Extensions";
            string listKey = nameKey + "Extensions";

            List<string> list = new();

            if (ContainsKey(listKey))
            {
                var csv = Get<string>(listKey)?.Trim();
                if (!string.IsNullOrEmpty(csv))
                {
                    string[] split = csv.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var items = split.Select(s => s.TrimStart('.').Trim().ToLowerInvariant());

                    list = new List<string>(items);
                }
            }
            else
            {
                list = new List<string>(defaultExtensions);

                if (ContainsKey(addKey))
                {
                    var addCsv = Get<string>(addKey)?.Trim();
                    if (!string.IsNullOrEmpty(addCsv))
                    {
                        var parts = addCsv.Split(new char[] { ',', ';', ' ' },
                            StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim(' ', '.').ToLower());
                        list.AddRange(parts);
                    }
                }

                if (ContainsKey(remKey))
                {
                    var remCsv = Get<string>(remKey)?.Trim();
                    if (!string.IsNullOrEmpty(remCsv))
                    {
                        var parts = remCsv.Split(new char[] { ',', ';', ' ' },
                          StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim(' ', '.').ToLower());
                        foreach (var ext in parts)
                        {
                            if (list.Contains(ext))
                            {
                                list.Remove(ext);
                            }
                        }
                    }
                }
            }

            return list;
        }

        public void SetExtensions(string nameKey, string extensions)
        {
            nameKey = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nameKey)
                .Replace(" ", "", StringComparison.Ordinal);
            string addKey = "Add" + nameKey + "Extensions";
            string remKey = "Rem" + nameKey + "Extensions";
            string listKey = nameKey + "Extensions";

            Set(listKey, CleanExtensions(extensions));
            settings.Remove(addKey);
            settings.Remove(remKey);
        }

        private static string CleanExtensions(string extensions)
        {
            if (string.IsNullOrWhiteSpace(extensions))
                return string.Empty;

            string[] split = extensions.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var cleaned = split.Select(s => s.TrimStart('.').Trim());
            return string.Join(",", cleaned);
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
        public System.Xml.Schema.XmlSchema? GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;

            if (wasEmpty)
                return;

            reader.Read();
            while (reader.NodeType == XmlNodeType.Element)
            {
                string? key = reader.GetAttribute("key");
                string value = reader.ReadElementContentAsString();
                if (!string.IsNullOrEmpty(key))
                {
                    this[key] = value;
                }
            }
            reader.ReadEndElement();
        }



        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("xml", "http://www.w3.org/2000/xmlns/", "http://www.w3.org/XML/1998/namespace");

            foreach (var key in this.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteAttributeString("key", key);
                string value = this[key];
                if (!string.IsNullOrEmpty(value) && string.IsNullOrWhiteSpace(value))
                {
                    writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                }
                writer.WriteString(value);
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}
