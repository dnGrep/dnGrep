using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using dnGREP.Localization;
using dnGREP.Localization.Properties;

namespace dnGREP.TestLocalizedStrings
{
    public class TestStringsViewModel : CultureAwareViewModel
    {
        private readonly ObservableCollection<ResourceString> list = new();

        private const string sFile = "%file";
        private const string Line = "%line";
        private const string Pattern = "%pattern";
        private const string Match = "%match";
        private const string Column = "%column";

        private static Dictionary<string, string> AppCultures => new()
            {
                { "ar", "العربية" },
                { "bg", "Български" },
                { "ca", "català" },
                { "de", "Deutsch" },
                { "en", "English" },
                { "es", "español" },
                { "et", "eesti" },
                { "fr", "français" },
                { "he", "עברית" },
                { "it", "italiano" },
                { "ja", "日本語" },
                { "ko", "한국어" },
                { "nb-NO", "norsk (bokmål)" },
                { "pt", "Português" },
                { "ru", "pусский" },
                { "sr", "српски" },
                { "th", "ไทย" },
                { "tr", "Türkçe" },
                { "zh-CN", "简体中文" },
                { "zh-Hant", "中文" },
            };

        public TestStringsViewModel()
        {
            CultureNames = AppCultures.ToArray();
            CurrentCulture = TranslationSource.Instance.CurrentCulture.Name;

            TranslationSource.Instance.CurrentCultureChanged += (s, e) =>
            {
                InitializeStrings();
            };
        }

        public ObservableCollection<ResourceString> ListOfStrings
        {
            get { return list; }
        }

        /// <summary>
        /// This is the set of Resource strings used in string.Format
        /// </summary>
        internal void InitializeStrings()
        {
            list.Clear();
            list.Add(new ResourceString("Bookmarks_Summary_MaxFolderDepth", TranslationSource.Format(Resources.Bookmarks_Summary_MaxFolderDepth, 3)));
            list.Add(new ResourceString("Main_ExcelSheetName", TranslationSource.Format(Resources.Main_ExcelSheetName, "Sheet1")));
            list.Add(new ResourceString("Main_FilterSummary_MaxFolderDepth", TranslationSource.Format(Resources.Main_FilterSummary_MaxFolderDepth, 2)));
            list.Add(new ResourceString("Main_PowerPointSlideNumber", TranslationSource.Format(Resources.Main_PowerPointSlideNumber, 3)));
            list.Add(new ResourceString("Main_ResultList_AtPosition", TranslationSource.Format(Resources.Main_ResultList_AtPosition, 234)));
            list.Add(new ResourceString("Main_ResultList_CountMatches", TranslationSource.Format(Resources.Main_ResultList_CountMatches, @"folder\testFile1.text", 14)));
            list.Add(new ResourceString("Main_ResultList_CountMatchesOnLines", TranslationSource.Format(Resources.Main_ResultList_CountMatchesOnLines, @"folder\testFile1.text", 14, 5)));
            list.Add(new ResourceString("Main_ResultList_MatchToolTip1", TranslationSource.Format(Resources.Main_ResultList_MatchToolTip1, 1, Environment.NewLine, "The quick brown fox")));
            list.Add(new ResourceString("Main_ResultList_MatchToolTip2", TranslationSource.Format(Resources.Main_ResultList_MatchToolTip2, 2, Environment.NewLine, 1, "fox")));
            list.Add(new ResourceString("Main_ResultList_PlusCountMoreMatches", TranslationSource.Format(Resources.Main_ResultList_PlusCountMoreMatches, 7)));
            list.Add(new ResourceString("Main_Status_ReplaceComplete0FilesReplaced", TranslationSource.Format(Resources.Main_Status_ReplaceComplete0FilesReplaced, 6)));
            list.Add(new ResourceString("Main_Status_SearchCompletedIn0_1MatchesFoundIn2FilesOf3Searched", TranslationSource.Format(Resources.Main_Status_SearchCompletedIn0_1MatchesFoundIn2FilesOf3Searched, "0.184s", 42, 3, 7)));
            list.Add(new ResourceString("Main_Status_Searched0FilesFound1MatchingFiles", TranslationSource.Format(Resources.Main_Status_Searched0FilesFound1MatchingFiles, 7, 3)));
            list.Add(new ResourceString("Main_Status_Searched0FilesFound1MatchingFilesProcessing2", TranslationSource.Format(Resources.Main_Status_Searched0FilesFound1MatchingFilesProcessing2, 4, 2, "large.xml")));
            list.Add(new ResourceString("Main_WindowTitle", TranslationSource.Format(Resources.Main_WindowTitle, "test", @"C:\testFiles\test")));
            list.Add(new ResourceString("MessageBox_CouldNotLoadResourcesFile0", TranslationSource.Format(Resources.MessageBox_CouldNotLoadResourcesFile0, "Resources.en.resx")));
            list.Add(new ResourceString("MessageBox_CouldNotLoadTheme", TranslationSource.Format(Resources.MessageBox_CouldNotLoadTheme, "Sunset")));
            list.Add(new ResourceString("MessageBox_CountFilesHaveBeenSuccessfullyCopied", TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyCopied, 3)));
            list.Add(new ResourceString("MessageBox_CountFilesHaveBeenSuccessfullyDeleted", TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyDeleted, 2)));
            list.Add(new ResourceString("MessageBox_CountFilesHaveBeenSuccessfullyMoved", TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyMoved, 5)));
            list.Add(new ResourceString("MessageBox_NewVersionOfDnGREP0IsAvailableForDownload", TranslationSource.Format(Resources.MessageBox_NewVersionOfDnGREP0IsAvailableForDownload, "2.9.454")));
            list.Add(new ResourceString("MessageBox_ResourcesFile0IsNotAResxFile", TranslationSource.Format(Resources.MessageBox_ResourcesFile0IsNotAResxFile, "Resources.yy.resx")));
            list.Add(new ResourceString("MessageBox_SearchPathInTheFieldIsNotValid", TranslationSource.Format(Resources.MessageBox_SearchPathInTheFieldIsNotValid, Resources.Main_Folder)));
            list.Add(new ResourceString("MessageBox_TheFile0AlreadyExistsIn1OverwriteExisting", TranslationSource.Format(Resources.MessageBox_TheFile0AlreadyExistsIn1OverwriteExisting, "config.xml", @"C:\test\config")));
            list.Add(new ResourceString("MessageBox_TheFilePattern0IsNotAValidRegularExpression12", TranslationSource.Format(Resources.MessageBox_TheFilePattern0IsNotAValidRegularExpression12, "(.*", Environment.NewLine, "Not enough )'s")));
            list.Add(new ResourceString("MessageBox_ThisBookmarkIsAssociatedWith0OtherFolders", TranslationSource.Format(Resources.MessageBox_ThisBookmarkIsAssociatedWith0OtherFolders, 2)));
            list.Add(new ResourceString("MessageBox_WindowTitleIsName", TranslationSource.Format(Resources.MessageBox_WindowTitleIsName, "Word")));
            list.Add(new ResourceString("Options_CustomEditorHelp", TranslationSource.Format(Resources.Options_CustomEditorHelp, sFile, Line, Pattern, Match, Column)));
            list.Add(new ResourceString("Replace_FileNumberOfCountName", TranslationSource.Format(Resources.Replace_FileNumberOfCountName, 1, 3, "test.xml", "9", "6")));
            list.Add(new ResourceString("Replace_NumberOfMatchesMarkedForReplacement", TranslationSource.Format(Resources.Replace_NumberOfMatchesMarkedForReplacement, 1, 3)));
            list.Add(new ResourceString("Report_Found0MatchesOn1LinesIn2Files", TranslationSource.Format(Resources.Report_Found0MatchesOn1LinesIn2Files, 138, 83, 6)));
            list.Add(new ResourceString("Report_Has0MatchesOn1Lines", TranslationSource.Format(Resources.Report_Has0MatchesOn1Lines, 14, 6)));
            list.Add(new ResourceString("ReportSummary_MaxFolderDepth", TranslationSource.Format(Resources.ReportSummary_MaxFolderDepth, 2)));
            list.Add(new ResourceString("ReportSummary_SizeFrom0To1KB", TranslationSource.Format(Resources.ReportSummary_SizeFrom0To1KB, 25, 1000)));
            list.Add(new ResourceString("ReportSummary_Type0DateFrom1To2", TranslationSource.Format(Resources.ReportSummary_Type0DateFrom1To2, "Modified", "*", "*")));
            list.Add(new ResourceString("ReportSummary_Type0DateInPast1To2Hours", TranslationSource.Format(Resources.ReportSummary_Type0DateInPast1To2Hours, "Created", 0, 8)));
            list.Add(new ResourceString("ReportSummary_UsingTypeOfSeach", TranslationSource.Format(Resources.ReportSummary_UsingTypeOfSeach, "Regex")));
            list.Add(new ResourceString("Help_CmdLineVersion", TranslationSource.Format(Resources.Help_CmdLineVersion, "2.9.454.0", DateTime.Now.ToString())));
        }


        public KeyValuePair<string, string>[] CultureNames { get; }

        private string currentCulture;
        public string CurrentCulture
        {
            get { return currentCulture; }
            set
            {
                if (currentCulture == value)
                    return;

                currentCulture = value;
                OnPropertyChanged(nameof(CurrentCulture));
                TranslationSource.Instance.SetCulture(value);
            }
        }
    }

    public class ResourceString
    {
        public ResourceString(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; set; }
        public string Value { get; set; }
    }

}
