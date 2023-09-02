using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.XPath;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Everything;
using dnGREP.Localization;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public partial class BaseMainViewModel : CultureAwareViewModel
    {
        public static readonly string STAR = "*";
        public static readonly string AUTO = "Auto";
        public readonly static DateTime minDate = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);

        public BaseMainViewModel()
        {
            PropertyChanged += MainViewModel_PropertyChanged;

            CurrentGrepOperation = GrepOperation.None;
            IsCaseSensitiveEnabled = true;
            IsMultilineEnabled = true;
            IsWholeWordEnabled = true;
            IsBooleanOperatorsEnabled = TypeOfSearch == SearchType.PlainText || TypeOfSearch == SearchType.Regex;
            LoadSettings();
            SetToolTipText();

            IsEverythingAvailable = EverythingSearch.IsAvailable;
        }

        #region Private Variables and Properties
        private readonly XmlDocument doc = new();
        private XPathNavigator? nav;

        // list of properties that affect the search results
        private static readonly HashSet<string> searchParameters = new()
        {
            nameof(BooleanOperators),
            nameof(CaseSensitive),
            nameof(CodePage),
            nameof(EndDate),
            nameof(FileOrFolderPath),
            nameof(FilePattern),
            nameof(FilePatternIgnore),
            nameof(HoursFrom),
            nameof(HoursTo),
            nameof(IncludeArchive),
            nameof(IncludeBinary),
            nameof(IncludeHidden),
            nameof(IncludeSubfolder),
            nameof(FollowSymlinks),
            nameof(IsDateFilterSet),
            nameof(IsDatesRangeSet),
            nameof(IsEverythingSearchMode),
            nameof(IsHoursRangeSet),
            nameof(IsSizeFilterSet),
            nameof(MaxSubfolderDepth),
            nameof(Multiline),
            nameof(SearchFor),
            nameof(Singleline),
            nameof(SizeFrom),
            nameof(SizeTo),
            nameof(SkipRemoteCloudStorageFiles),
            nameof(StartDate),
            nameof(TypeOfFileSearch),
            nameof(TypeOfSearch),
            nameof(TypeOfTimeRangeFilter),
            nameof(UseFileDateFilter),
            nameof(UseFileSizeFilter),
            nameof(UseGitignore),
            nameof(WholeWord),
        };

        protected static GrepSettings Settings => GrepSettings.Instance;

        protected PathSearchText PathSearchText { get; private set; } = new();
        #endregion

        #region Properties

        public GrepSearchResultsViewModel ResultsViewModel { get; } = new();

        public ObservableCollection<MRUViewModel> FastSearchBookmarks { get; } = new ObservableCollection<MRUViewModel>();

        public ObservableCollection<MRUViewModel> FastReplaceBookmarks { get; } = new ObservableCollection<MRUViewModel>();

        public ObservableCollection<MRUViewModel> FastFileMatchBookmarks { get; } = new ObservableCollection<MRUViewModel>();

        public ObservableCollection<MRUViewModel> FastFileNotMatchBookmarks { get; } = new ObservableCollection<MRUViewModel>();

        public ObservableCollection<MRUViewModel> FastPathBookmarks { get; } = new ObservableCollection<MRUViewModel>();

        public ObservableCollection<KeyValuePair<string, int>> Encodings { get; } = new();

        [ObservableProperty]
        private bool searchParametersChanged;

        [ObservableProperty]
        private string fileOrFolderPath = string.Empty;
        partial void OnFileOrFolderPathChanged(string value)
        {
            PathSearchText.FileOrFolderPath = value;
        }


        [ObservableProperty]
        private string searchFor = string.Empty;

        [ObservableProperty]
        private string searchToolTip = string.Empty;

        [ObservableProperty]
        private bool searchToolTipVisible = false;

        [ObservableProperty]
        private string replaceWith = string.Empty;

        [ObservableProperty]
        private string replaceToolTip = string.Empty;

        [ObservableProperty]
        private bool replaceToolTipVisible = false;

        [ObservableProperty]
        private bool isFiltersExpanded;

        [ObservableProperty]
        private string filePattern = string.Empty;

        [ObservableProperty]
        private string filePatternIgnore = string.Empty;

        [ObservableProperty]
        private bool useGitignore;

        [ObservableProperty]
        private bool includeSubfolder;
        partial void OnIncludeSubfolderChanged(bool value)
        {
            if (!value)
            {
                MaxSubfolderDepth = -1;
            }
        }

        [ObservableProperty]
        private int maxSubfolderDepth = -1;

        [ObservableProperty]
        private bool includeHidden;

        [ObservableProperty]
        private bool includeBinary;

        [ObservableProperty]
        private bool followSymlinks;

        [ObservableProperty]
        private bool skipRemoteCloudStorageFiles;

        [ObservableProperty]
        private bool includeArchive;

        [ObservableProperty]
        private bool searchParallel;

        [ObservableProperty]
        private SearchType typeOfSearch;

        [ObservableProperty]
        private FileSearchType typeOfFileSearch;
        partial void OnTypeOfFileSearchChanged(FileSearchType value)
        {
            if (value == FileSearchType.Everything)
            {
                // changing from Everything, clean the search path
                FileOrFolderPath = UiUtils.CleanPath(FileOrFolderPath);
            }

            PathSearchText.TypeOfFileSearch = value;
        }

        [ObservableProperty]
        private bool isEverythingAvailable;

        [ObservableProperty]
        private bool isEverythingSearchMode;

        [ObservableProperty]
        private string patternColumnWidth = STAR;

        [ObservableProperty]
        private string searchTextBoxLabel = Resources.Main_Folder;

        [ObservableProperty]
        private FileSizeFilter useFileSizeFilter = FileSizeFilter.None;

        [ObservableProperty]
        private int sizeFrom;

        [ObservableProperty]
        private int sizeTo;

        [ObservableProperty]
        private FileDateFilter useFileDateFilter;

        [ObservableProperty]
        private FileTimeRange typeOfTimeRangeFilter;

        [ObservableProperty]
        private DateTime minStartDate = minDate;

        [ObservableProperty]
        private DateTime? startDate;
        partial void OnStartDateChanged(DateTime? value)
        {
            if (value.HasValue)
            {
                MinEndDate = value.Value;
                if (EndDate.HasValue && EndDate.Value < MinEndDate)
                    EndDate = MinEndDate;
            }
            else
            {
                MinEndDate = minDate;
            }
        }

        [ObservableProperty]
        private DateTime minEndDate = minDate;

        [ObservableProperty]
        private DateTime? endDate;

        [ObservableProperty]
        private int hoursFrom;

        [ObservableProperty]
        private int hoursTo;

        [ObservableProperty]
        private bool isDateFilterSet;

        [ObservableProperty]
        private bool isDatesRangeSet;

        [ObservableProperty]
        private bool isHoursRangeSet;

        [ObservableProperty]
        private bool caseSensitive;

        [ObservableProperty]
        private bool previewFileContent;

        [ObservableProperty]
        private bool isCaseSensitiveEnabled;

        [ObservableProperty]
        private bool multiline;

        [ObservableProperty]
        private bool isMultilineEnabled;

        [ObservableProperty]
        private bool singleline;
        partial void OnSinglelineChanged(bool value)
        {
            if (value)
            {
                Multiline = true;
            }
        }

        [ObservableProperty]
        private bool isSinglelineEnabled;

        [ObservableProperty]
        private bool highlightCaptureGroups;

        [ObservableProperty]
        private bool isHighlightGroupsEnabled;

        [ObservableProperty]
        private bool stopAfterFirstMatch;

        [ObservableProperty]
        private bool wholeWord;

        [ObservableProperty]
        private bool isWholeWordEnabled;

        [ObservableProperty]
        private bool booleanOperators;

        [ObservableProperty]
        private bool isBooleanOperatorsEnabled;

        [ObservableProperty]
        private bool captureGroupSearch;

        [ObservableProperty]
        private bool isSizeFilterSet;

        [ObservableProperty]
        private bool filesFound;

        [ObservableProperty]
        private bool canSearch;
        partial void OnCanSearchChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        [ObservableProperty]
        private bool canSearchInResults;
        partial void OnCanSearchInResultsChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        [ObservableProperty]
        private bool searchInResultsContent;

        [ObservableProperty]
        private bool canCancel;
        partial void OnCanCancelChanged(bool value)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOperationInProgress))]
        private GrepOperation currentGrepOperation;

        [ObservableProperty]
        private string fileFiltersSummary = string.Empty;

        [ObservableProperty]
        private double maxFileFiltersSummaryWidth;

        [ObservableProperty]
        private bool isValidPattern = true;

        [ObservableProperty]
        private string validationMessage = string.Empty;

        [ObservableProperty]
        private string? validationToolTip = null;

        [ObservableProperty]
        private bool hasValidationMessage;

        [ObservableProperty]
        private string windowTitle = Resources.Main_DnGREP_Title;

        [ObservableProperty]
        private string textBoxStyle = string.Empty;

        [ObservableProperty]
        private int codePage = -1;

        [ObservableProperty]
        private bool canUndo;

        [ObservableProperty]
        private bool isSaveInProgress;

        [ObservableProperty]
        private bool optionsOnMainPanel = true;

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private string resultsFontFamily = GrepSettings.DefaultMonospaceFontFamily;

        [ObservableProperty]
        private double resultsFontSize;

        [ObservableProperty]
        private double dialogFontSize;

        public bool IsOperationInProgress => IsScriptRunning || CurrentGrepOperation != GrepOperation.None;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOperationInProgress))]
        private bool isScriptRunning = false;

        #endregion

        #region Public Methods

        public void SetFileOrFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                FileOrFolderPath = string.Empty;
            }
            else
            {
                if (TypeOfFileSearch == FileSearchType.Everything)
                {
                    // Is this a list of paths?  A path may contain a comma or semi-colon
                    // so check that it isn't a valid directory or file first
                    string trimmedPath = path.Trim('\"', ' ');
                    if (!(Directory.Exists(trimmedPath) || File.Exists(trimmedPath)) &&
                        (path.Contains(',', StringComparison.Ordinal) || path.Contains(';', StringComparison.Ordinal) || path.Contains('|', StringComparison.Ordinal)))
                    {
                        try
                        {
                            var parts = UiUtils.SplitPath(path, true).Select(p => UiUtils.QuoteIfIncludesSpaces(p));
                            path = string.Join(" | ", parts);
                        }
                        catch { }
                    }
                    else
                    {
                        path = UiUtils.QuoteIfIncludesSpaces(path);
                    }
                }

                FileOrFolderPath = path;
            }
        }

        public virtual void UpdateState(string name)
        {
            if (searchParameters.Contains(name))
                SearchParametersChanged = true;

            switch (name)
            {
                case nameof(Multiline):
                case nameof(Singleline):
                case nameof(WholeWord):
                case nameof(CaseSensitive):
                case nameof(StopAfterFirstMatch):
                    if (Multiline)
                        TextBoxStyle = "{StaticResource ExpandedTextbox}";
                    else
                        TextBoxStyle = "";
                    break;

                case nameof(UseFileSizeFilter):
                    IsSizeFilterSet = UseFileSizeFilter == FileSizeFilter.Yes;
                    break;

                case nameof(UseFileDateFilter):
                    IsDateFilterSet = UseFileDateFilter != FileDateFilter.None;
                    IsDatesRangeSet = IsDateFilterSet && TypeOfTimeRangeFilter == FileTimeRange.Dates;
                    IsHoursRangeSet = IsDateFilterSet && TypeOfTimeRangeFilter == FileTimeRange.Hours;
                    if (!IsDateFilterSet)
                        TypeOfTimeRangeFilter = FileTimeRange.None;
                    else if (TypeOfTimeRangeFilter == FileTimeRange.None)
                        TypeOfTimeRangeFilter = FileTimeRange.Dates;
                    break;

                case nameof(TypeOfTimeRangeFilter):
                    IsDatesRangeSet = IsDateFilterSet && TypeOfTimeRangeFilter == FileTimeRange.Dates;
                    IsHoursRangeSet = IsDateFilterSet && TypeOfTimeRangeFilter == FileTimeRange.Hours;
                    break;

                case nameof(TypeOfFileSearch):
                    if (TypeOfFileSearch == FileSearchType.Everything)
                    {
                        SetFileOrFolderPath(FileOrFolderPath);
                        FilePattern = string.Empty;
                        FilePatternIgnore = string.Empty;
                        UseGitignore = false;
                        IsEverythingSearchMode = true;
                        PatternColumnWidth = AUTO;
                        SearchTextBoxLabel = Resources.Main_EverythingSearch;
                    }
                    else
                    {
                        IsEverythingSearchMode = false;
                        PatternColumnWidth = STAR;
                        SearchTextBoxLabel = Resources.Main_Folder;
                    }

                    if (TypeOfFileSearch != FileSearchType.Regex)
                    {
                        CaptureGroupSearch = false;
                    }
                    break;
            }

            if (name == nameof(IncludeSubfolder) || name == nameof(MaxSubfolderDepth) || name == nameof(IncludeHidden) ||
                name == nameof(IncludeBinary) || name == nameof(UseFileSizeFilter) || name == nameof(UseFileDateFilter) ||
                name == nameof(FollowSymlinks))
            {
                var tempList = new List<string>();
                if (!IncludeSubfolder || (IncludeSubfolder && MaxSubfolderDepth == 0))
                    tempList.Add(Resources.Main_FilterSummary_NoSubfolders);
                if (IncludeSubfolder && MaxSubfolderDepth > 0)
                    tempList.Add(TranslationSource.Format(Resources.Main_FilterSummary_MaxFolderDepth, MaxSubfolderDepth));
                if (!IncludeHidden)
                    tempList.Add(Resources.Main_FilterSummary_NoHidden);
                if (!IncludeBinary)
                    tempList.Add(Resources.Main_FilterSummary_NoBinary);
                if (!FollowSymlinks)
                    tempList.Add(Resources.Main_FilterSummary_NoSymlinks);
                if (UseFileSizeFilter == FileSizeFilter.Yes)
                    tempList.Add(Resources.Main_FilterSummary_BySize);
                if (UseFileDateFilter == FileDateFilter.Modified)
                    tempList.Add(Resources.Main_FilterSummary_ByModifiedDate);
                if (UseFileDateFilter == FileDateFilter.Created)
                    tempList.Add(Resources.Main_FilterSummary_ByCreatedDate);

                if (tempList.Count == 0)
                {
                    FileFiltersSummary = Resources.Main_FilterSummary_AllFiles;
                }
                else
                {
                    FileFiltersSummary = string.Join(", ", tempList.ToArray());
                }
            }

            //Files found
            if (name == nameof(FileOrFolderPath) || name == nameof(SearchFor) || name == nameof(FilePattern) ||
                name == nameof(FilePatternIgnore) || name == nameof(UseGitignore))
            {
                FilesFound = false;
            }

            //Change title
            if (name == nameof(FileOrFolderPath) || name == nameof(SearchFor))
            {
                if (string.IsNullOrWhiteSpace(FileOrFolderPath))
                    WindowTitle = Resources.Main_DnGREP_Title;
                else
                    WindowTitle = TranslationSource.Format(Resources.Main_WindowTitle,
                        string.IsNullOrEmpty(SearchFor) ? Resources.Main_Empty : SearchFor.Replace('\n', ' ').Replace('\r', ' '),
                        FileOrFolderPath);
            }

            if (name == nameof(TypeOfSearch))
            {
                SetToolTipText();
            }

            //Change validation
            if (name == nameof(SearchFor) || name == nameof(TypeOfSearch) || name == nameof(BooleanOperators))
            {
                ValidationMessage = string.Empty;
                ValidationToolTip = null;
                IsValidPattern = true;

                // Whitespace is a valid search pattern for Text and Regex
                if (!string.IsNullOrEmpty(SearchFor))
                {
                    if (TypeOfSearch == SearchType.PlainText)
                    {
                        if (BooleanOperators)
                        {
                            ValidateBooleanExpression();
                        }
                    }
                    else if (TypeOfSearch == SearchType.Regex)
                    {
                        if (BooleanOperators)
                        {
                            ValidateBooleanExpression();
                        }
                        else
                        {
                            ValidateRegex(SearchFor);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(SearchFor))
                {
                    if (TypeOfSearch == SearchType.XPath)
                    {
                        try
                        {
                            nav = doc.CreateNavigator();
                            XPathExpression? expr = (nav?.Compile(SearchFor)) ??
                                throw new XPathException(SearchFor);
                            ValidationMessage = Resources.Main_Validation_XPathIsOK;
                            IsValidPattern = true;
                        }
                        catch (XPathException ex)
                        {
                            ValidationMessage = Resources.Main_Validation_XPathIsNotValid;
                            ValidationToolTip = ex.Message;
                            IsValidPattern = false;
                        }
                    }
                    else if (TypeOfSearch == SearchType.Hex)
                    {
                        string[] parts = SearchFor.TrimEnd().Split(' ');
                        bool valid = true;
                        bool hasDigit = false;  // need at least 1 byte specified
                        foreach (string num in parts)
                        {
                            if (num != "?" && num != "??")
                            {
                                if (byte.TryParse(num, System.Globalization.NumberStyles.HexNumber, null, out byte result))
                                {
                                    hasDigit = true;
                                }
                                else
                                {
                                    valid = false;
                                }
                            }
                        }
                        if (!hasDigit)
                        {
                            valid = false;
                        }
                        ValidationMessage = valid ? Resources.Main_Validation_HexStringIsOK : Resources.Main_Validation_HexStringIsNotValid;
                        IsValidPattern = valid;
                    }
                }
            }

            if (name == nameof(ValidationMessage))
            {
                HasValidationMessage = !string.IsNullOrWhiteSpace(ValidationMessage);
            }

            //Can search
            if (name == nameof(CurrentGrepOperation) || name == nameof(SearchFor) || name == nameof(IsSaveInProgress))
            {
                if (CurrentGrepOperation == GrepOperation.None && !IsSaveInProgress &&
                    (!string.IsNullOrEmpty(SearchFor) || Settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern)))
                {
                    CanSearch = true;
                }
                else
                {
                    CanSearch = false;
                }
                // Refresh buttons
                CommandManager.InvalidateRequerySuggested();
            }

            if (name == nameof(CurrentGrepOperation) || name == nameof(IsSaveInProgress))
            {
                if (ResultsViewModel.SearchResults.Count > 0 && !IsSaveInProgress)
                {
                    CanSearchInResults = true;
                }
                else
                {
                    CanSearchInResults = false;
                }
            }

            //btnCancel
            if (name == nameof(CurrentGrepOperation))
            {
                if (CurrentGrepOperation != GrepOperation.None)
                {
                    CanCancel = true;
                }
                else
                {
                    CanCancel = false;
                }
            }

            //Search type specific options
            if (name == nameof(TypeOfSearch))
            {
                if (TypeOfSearch == SearchType.XPath)
                {
                    IsCaseSensitiveEnabled = false;
                    IsMultilineEnabled = false;
                    IsSinglelineEnabled = false;
                    IsWholeWordEnabled = false;
                    IsBooleanOperatorsEnabled = false;
                    IsHighlightGroupsEnabled = false;
                    CaseSensitive = false;
                    Multiline = false;
                    Singleline = false;
                    WholeWord = false;
                    BooleanOperators = false;
                }
                else if (TypeOfSearch == SearchType.PlainText)
                {
                    IsCaseSensitiveEnabled = true;
                    IsMultilineEnabled = true;
                    IsSinglelineEnabled = false;
                    IsWholeWordEnabled = true;
                    IsBooleanOperatorsEnabled = true;
                    IsHighlightGroupsEnabled = false;
                    Singleline = false;
                }
                else if (TypeOfSearch == SearchType.Soundex)
                {
                    IsMultilineEnabled = true;
                    IsCaseSensitiveEnabled = false;
                    IsSinglelineEnabled = false;
                    IsWholeWordEnabled = true;
                    IsBooleanOperatorsEnabled = false;
                    IsHighlightGroupsEnabled = false;
                    CaseSensitive = false;
                    Singleline = false;
                    BooleanOperators = false;
                }
                else if (TypeOfSearch == SearchType.Regex)
                {
                    IsCaseSensitiveEnabled = true;
                    IsMultilineEnabled = true;
                    IsSinglelineEnabled = true;
                    IsWholeWordEnabled = true;
                    IsBooleanOperatorsEnabled = true;
                    IsHighlightGroupsEnabled = true;
                }
                else if (TypeOfSearch == SearchType.Hex)
                {
                    IsCaseSensitiveEnabled = false;
                    IsMultilineEnabled = false;
                    IsSinglelineEnabled = false;
                    IsWholeWordEnabled = false;
                    IsBooleanOperatorsEnabled = false;
                    IsHighlightGroupsEnabled = false;
                    CaseSensitive = false;
                    Multiline = false;
                    Singleline = false;
                    WholeWord = false;
                    BooleanOperators = false;
                    HighlightCaptureGroups = false;
                }
            }
        }

        protected bool ValidateBooleanExpression()
        {
            if (BooleanOperators && !string.IsNullOrEmpty(SearchFor))
            {
                BooleanExpression exp = new();
                if (exp.TryParse(SearchFor))
                {
                    if (exp.Operands.Count > 0 && TypeOfSearch == SearchType.Regex)
                    {
                        foreach (string pattern in exp.Operands.Select(o => o.Value))
                        {
                            if (!ValidateRegex(pattern))
                                return false;
                        }
                    }
                }
                else
                {
                    ValidationMessage = Resources.Main_Validation_BooleanExpressionIsNotValid;
                    ReportParserState(exp.ParserState);
                    IsValidPattern = false;
                    return false;
                }
            }
            ValidationMessage = Resources.Main_Validation_BooleanExpressionIsOK;
            return true;
        }

        protected void ReportParserState(ParserErrorState parserState)
        {
            string msg = string.Empty;
            switch (parserState)
            {
                case ParserErrorState.MismatchedParentheses:
                    msg += Resources.Main_Validation_BooleanExpressionHasMismatchedParentheses;
                    break;
                case ParserErrorState.MissingOperator:
                    msg += Resources.Main_Validation_BooleanExpressionIsMissingABooleanOperator;
                    break;
                case ParserErrorState.MissingOperand:
                    msg += Resources.Main_Validation_BooleanExpressionIsMissingASearchPattern;
                    break;
                case ParserErrorState.UnknownToken:
                    msg += Resources.Main_Validation_BooleanExpressionContainsAnUnknownToken;
                    break;
                case ParserErrorState.UnknownError:
                    msg += Resources.Main_Validation_BooleanExpressionHasAnUnknownError;
                    break;
            }
            ValidationToolTip = msg;
        }

        private bool ValidateRegex(string pattern)
        {
            try
            {
                Regex regex = new(pattern);
                ValidationMessage = Resources.Main_Validation_RegexIsOK;
                IsValidPattern = true;
            }
            catch (Exception ex)
            {
                ValidationMessage = Resources.Main_Validation_RegexIsNotValid;
                ValidationToolTip = ex.Message;
                IsValidPattern = false;
            }
            return IsValidPattern;
        }

        protected virtual void ResetOptions()
        {
            UseFileSizeFilter = FileSizeFilter.No;
            IncludeBinary = true;
            IncludeHidden = true;
            IncludeSubfolder = true;
            MaxSubfolderDepth = -1;
            IncludeArchive = Utils.ArchiveExtensions.Count > 0;
            FollowSymlinks = false;
            SkipRemoteCloudStorageFiles = true;
            UseFileDateFilter = FileDateFilter.None;
            TypeOfTimeRangeFilter = FileTimeRange.None;
            FilePattern = "*";
            FilePatternIgnore = "";
            TypeOfFileSearch = FileSearchType.Asterisk;
            UseGitignore = Utils.IsGitInstalled;
        }

        virtual public void LoadSettings()
        {
            LoadMRULists();

            SearchFor = Settings.Get<string>(GrepSettings.Key.SearchFor);
            ReplaceWith = Settings.Get<string>(GrepSettings.Key.ReplaceWith);
            IncludeHidden = Settings.Get<bool>(GrepSettings.Key.IncludeHidden);
            IncludeBinary = Settings.Get<bool>(GrepSettings.Key.IncludeBinary);
            IncludeArchive = Settings.Get<bool>(GrepSettings.Key.IncludeArchive) && Utils.ArchiveExtensions.Count > 0;
            SearchParallel = Settings.Get<bool>(GrepSettings.Key.SearchParallel);
            IncludeSubfolder = Settings.Get<bool>(GrepSettings.Key.IncludeSubfolder);
            MaxSubfolderDepth = Settings.Get<int>(GrepSettings.Key.MaxSubfolderDepth);
            FollowSymlinks = Settings.Get<bool>(GrepSettings.Key.FollowSymlinks);
            SkipRemoteCloudStorageFiles = Settings.Get<bool>(GrepSettings.Key.SkipRemoteCloudStorageFiles);
            TypeOfSearch = Settings.Get<SearchType>(GrepSettings.Key.TypeOfSearch);
            TypeOfFileSearch = Settings.Get<FileSearchType>(GrepSettings.Key.TypeOfFileSearch);
            // FileOrFolderPath depends on TypeOfFileSearch, so must be after
            SetFileOrFolderPath(Settings.Get<string>(GrepSettings.Key.SearchFolder));
            CodePage = Settings.Get<int>(GrepSettings.Key.CodePage);
            FilePattern = Settings.Get<string>(GrepSettings.Key.FilePattern);
            FilePatternIgnore = Settings.Get<string>(GrepSettings.Key.FilePatternIgnore);
            UseGitignore = Settings.Get<bool>(GrepSettings.Key.UseGitignore) && Utils.IsGitInstalled;
            UseFileSizeFilter = Settings.Get<FileSizeFilter>(GrepSettings.Key.UseFileSizeFilter);
            CaseSensitive = Settings.Get<bool>(GrepSettings.Key.CaseSensitive);
            Multiline = Settings.Get<bool>(GrepSettings.Key.Multiline);
            Singleline = Settings.Get<bool>(GrepSettings.Key.Singleline);
            StopAfterFirstMatch = Settings.Get<bool>(GrepSettings.Key.StopAfterFirstMatch);
            WholeWord = Settings.Get<bool>(GrepSettings.Key.WholeWord);
            BooleanOperators = Settings.Get<bool>(GrepSettings.Key.BooleanOperators);
            CaptureGroupSearch = GrepSettings.Instance.Get<bool>(GrepSettings.Key.CaptureGroupSearch);
            HighlightCaptureGroups = Settings.Get<bool>(GrepSettings.Key.HighlightCaptureGroups);
            SizeFrom = Settings.Get<int>(GrepSettings.Key.SizeFrom);
            SizeTo = Settings.Get<int>(GrepSettings.Key.SizeTo);
            IsFiltersExpanded = Settings.Get<bool>(GrepSettings.Key.IsFiltersExpanded);
            PreviewFileContent = Settings.Get<bool>(GrepSettings.Key.PreviewFileContent);
            OptionsOnMainPanel = Settings.Get<bool>(GrepSettings.Key.OptionsOnMainPanel);
            UseFileDateFilter = Settings.Get<FileDateFilter>(GrepSettings.Key.UseFileDateFilter);
            TypeOfTimeRangeFilter = Settings.Get<FileTimeRange>(GrepSettings.Key.TypeOfTimeRangeFilter);
            StartDate = Settings.GetNullable<DateTime?>(GrepSettings.Key.StartDate);
            EndDate = Settings.GetNullable<DateTime?>(GrepSettings.Key.EndDate);
            HoursFrom = Settings.Get<int>(GrepSettings.Key.HoursFrom);
            HoursTo = Settings.Get<int>(GrepSettings.Key.HoursTo);
        }

        protected void LoadMRULists()
        {
            string saveSearchFor = Settings.Get<string>(GrepSettings.Key.SearchFor);
            LoadMRUList(MRUType.SearchFor, FastSearchBookmarks, GrepSettings.Key.FastSearchBookmarks);
            Settings.Set(GrepSettings.Key.SearchFor, saveSearchFor);

            string saveReplaceWith = Settings.Get<string>(GrepSettings.Key.ReplaceWith);
            LoadMRUList(MRUType.ReplaceWith, FastReplaceBookmarks, GrepSettings.Key.FastReplaceBookmarks);
            Settings.Set(GrepSettings.Key.ReplaceWith, saveReplaceWith);

            string saveFilePattern = Settings.Get<string>(GrepSettings.Key.FilePattern);
            LoadMRUList(MRUType.IncludePattern, FastFileMatchBookmarks, GrepSettings.Key.FastFileMatchBookmarks);
            Settings.Set(GrepSettings.Key.FilePattern, saveFilePattern);

            string saveFilePatternIgnore = Settings.Get<string>(GrepSettings.Key.FilePatternIgnore);
            LoadMRUList(MRUType.ExcludePattern, FastFileNotMatchBookmarks, GrepSettings.Key.FastFileNotMatchBookmarks);
            Settings.Set(GrepSettings.Key.FilePatternIgnore, saveFilePatternIgnore);

            string saveSearchFolder = Settings.Get<string>(GrepSettings.Key.SearchFolder);
            LoadMRUList(MRUType.SearchPath, FastPathBookmarks, GrepSettings.Key.FastPathBookmarks);
            Settings.Set(GrepSettings.Key.SearchFolder, saveSearchFolder);
        }

        private static void LoadMRUList(MRUType valueType, IList<MRUViewModel> list, string itemKey)
        {
            var mruItems = Settings.Get<List<MostRecentlyUsed>>(itemKey);
            if (mruItems != null && mruItems.Count > 0)
            {
                var vmList = mruItems.Select(p => new MRUViewModel(valueType, p.StringValue, p.IsPinned)).ToList();
                if (vmList != null && vmList.Count > 0)
                {
                    var toRemove = list.Except(vmList).ToList();
                    foreach (var item in toRemove)
                    {
                        list.Remove(item);
                    }

                    foreach (var mru in vmList)
                    {
                        if (!list.Contains(mru))
                            list.Add(mru);
                    }
                }
            }
        }

        public virtual void SaveSettings()
        {
            Settings.Set(GrepSettings.Key.SearchFolder, FileOrFolderPath);
            Settings.Set(GrepSettings.Key.SearchFor, SearchFor);
            Settings.Set(GrepSettings.Key.ReplaceWith, ReplaceWith);
            Settings.Set(GrepSettings.Key.IncludeHidden, IncludeHidden);
            Settings.Set(GrepSettings.Key.IncludeBinary, IncludeBinary);
            Settings.Set(GrepSettings.Key.IncludeArchive, IncludeArchive);
            Settings.Set(GrepSettings.Key.SearchParallel, SearchParallel);
            Settings.Set(GrepSettings.Key.IncludeSubfolder, IncludeSubfolder);
            Settings.Set(GrepSettings.Key.MaxSubfolderDepth, MaxSubfolderDepth);
            Settings.Set(GrepSettings.Key.FollowSymlinks, FollowSymlinks);
            Settings.Set(GrepSettings.Key.SkipRemoteCloudStorageFiles, SkipRemoteCloudStorageFiles);
            Settings.Set(GrepSettings.Key.TypeOfSearch, TypeOfSearch);
            Settings.Set(GrepSettings.Key.CodePage, CodePage);
            Settings.Set(GrepSettings.Key.TypeOfFileSearch, TypeOfFileSearch);
            Settings.Set(GrepSettings.Key.FilePattern, FilePattern);
            Settings.Set(GrepSettings.Key.FilePatternIgnore, FilePatternIgnore);
            Settings.Set(GrepSettings.Key.UseGitignore, UseGitignore);
            Settings.Set(GrepSettings.Key.UseFileSizeFilter, UseFileSizeFilter);
            Settings.Set(GrepSettings.Key.CaseSensitive, CaseSensitive);
            Settings.Set(GrepSettings.Key.Multiline, Multiline);
            Settings.Set(GrepSettings.Key.Singleline, Singleline);
            Settings.Set(GrepSettings.Key.StopAfterFirstMatch, StopAfterFirstMatch);
            Settings.Set(GrepSettings.Key.WholeWord, WholeWord);
            Settings.Set(GrepSettings.Key.BooleanOperators, BooleanOperators);
            Settings.Set(GrepSettings.Key.CaptureGroupSearch, CaptureGroupSearch);
            Settings.Set(GrepSettings.Key.HighlightCaptureGroups, HighlightCaptureGroups);
            Settings.Set(GrepSettings.Key.SizeFrom, SizeFrom);
            Settings.Set(GrepSettings.Key.SizeTo, SizeTo);
            Settings.Set(GrepSettings.Key.IsFiltersExpanded, IsFiltersExpanded);
            Settings.Set(GrepSettings.Key.PreviewFileContent, PreviewFileContent);
            Settings.Set(GrepSettings.Key.UseFileDateFilter, UseFileDateFilter);
            Settings.Set(GrepSettings.Key.TypeOfTimeRangeFilter, TypeOfTimeRangeFilter);
            Settings.Set(GrepSettings.Key.StartDate, StartDate);
            Settings.Set(GrepSettings.Key.EndDate, EndDate);
            Settings.Set(GrepSettings.Key.HoursFrom, HoursFrom);
            Settings.Set(GrepSettings.Key.HoursTo, HoursTo);

            Settings.Save();
        }

        #endregion

        #region Private Methods

        private void SetToolTipText()
        {
            switch (TypeOfSearch)
            {
                case SearchType.PlainText:
                case SearchType.Soundex:
                    SearchToolTip = string.Empty;
                    ReplaceToolTip = Resources.TTB0_InsertsTabNewline;
                    break;

                case SearchType.Regex:
                    SearchToolTip = string.Join(Environment.NewLine, new string[]
                    {
                        Resources.TTA1_MatchesAllCharacters,
                        Resources.TTA2_MatchesAlphaNumerics,
                        Resources.TTA3_MatchesDigits,
                        Resources.TTA4_MatchesSpace,
                        Resources.TTA5_MatchesAnyNumberOfCharacters,
                        Resources.TTA6_Matches1To3Characters,
                        Resources.TTA7_ForMoreRegexPatternsHitF1,

                    });

                    ReplaceToolTip = string.Join(Environment.NewLine, new string[]
                        {
                            Resources.TTB0_InsertsTabNewline,
                            Resources.TTB1_ReplacesEntireRegex,
                            Resources.TTB2_InsertsTheTextMatchedIntoTheReplacementText,
                            Resources.TTB3_InsertsASingleDollarSignIntoTheReplacementText,
                        });
                    break;
                default:
                    SearchToolTip = string.Empty;
                    ReplaceToolTip = string.Empty;
                    break;
            }

            SearchToolTipVisible = !string.IsNullOrEmpty(SearchToolTip);
            ReplaceToolTipVisible = !string.IsNullOrEmpty(ReplaceToolTip);
        }

        void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName))
            {
                UpdateState(e.PropertyName);
            }
        }

        #endregion
    }
}
