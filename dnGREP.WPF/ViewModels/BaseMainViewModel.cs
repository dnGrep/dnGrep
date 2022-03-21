using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Xml;
using System.Xml.XPath;
using dnGREP.Common;
using dnGREP.Everything;
using dnGREP.Localization;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public class BaseMainViewModel : CultureAwareViewModel, IDataErrorInfo
    {
        public static readonly int FastBookmarkCapacity = 20;
        public static readonly string STAR = "*";
        public static readonly string AUTO = "Auto";

        public BaseMainViewModel()
        {
            PropertyChanged += MainViewModel_PropertyChanged;

            CurrentGrepOperation = GrepOperation.None;
            IsCaseSensitiveEnabled = true;
            IsMultilineEnabled = true;
            IsWholeWordEnabled = true;
            IsBooleanOperatorsEnabled = TypeOfSearch == SearchType.PlainText || TypeOfSearch == SearchType.Regex;
            CanSearchArchives = Utils.ArchiveExtensions.Count > 0;
            LoadSettings();

            IsEverythingAvailable = EverythingSearch.IsAvailable;
        }

        #region Private Variables and Properties
        private readonly XmlDocument doc = new XmlDocument();
        private XPathNavigator nav;

        // list of properties that affect the search results
        private static readonly HashSet<string> searchParameters = new HashSet<string>
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
            nameof(StartDate),
            nameof(TypeOfFileSearch),
            nameof(TypeOfSearch),
            nameof(TypeOfTimeRangeFilter),
            nameof(UseFileDateFilter),
            nameof(UseFileSizeFilter),
            nameof(UseGitignore),
            nameof(WholeWord),
        };

        protected GrepSettings Settings => GrepSettings.Instance;

        protected PathSearchText PathSearchText { get; private set; } = new PathSearchText();
        #endregion

        #region Properties

        public ObservableGrepSearchResults SearchResults { get; } = new ObservableGrepSearchResults();

        public ObservableCollection<string> FastSearchBookmarks { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> FastReplaceBookmarks { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> FastFileMatchBookmarks { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> FastFileNotMatchBookmarks { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> FastPathBookmarks { get; } = new ObservableCollection<string>();

        public ObservableCollection<KeyValuePair<string, int>> Encodings { get; } = new ObservableCollection<KeyValuePair<string, int>>();

        private bool searchParametersChanged;
        public bool SearchParametersChanged
        {
            get { return searchParametersChanged; }
            set
            {
                if (value == searchParametersChanged)
                    return;

                searchParametersChanged = value;
                base.OnPropertyChanged(nameof(SearchParametersChanged));
            }
        }

        private string fileOrFolderPath;
        public string FileOrFolderPath
        {
            get { return fileOrFolderPath; }
            set
            {
                if (value == fileOrFolderPath)
                    return;

                fileOrFolderPath = value;
                PathSearchText.FileOrFolderPath = value;
                base.OnPropertyChanged(nameof(FileOrFolderPath));
            }
        }

        private string searchFor;
        public string SearchFor
        {
            get { return searchFor; }
            set
            {
                if (value == searchFor)
                    return;

                searchFor = value;
                base.OnPropertyChanged(nameof(SearchFor));
            }
        }

        private string replaceWith;
        public string ReplaceWith
        {
            get { return replaceWith; }
            set
            {
                if (value == replaceWith)
                    return;

                replaceWith = value;
                base.OnPropertyChanged(nameof(ReplaceWith));
            }
        }

        private bool isFiltersExpanded;
        public bool IsFiltersExpanded
        {
            get { return isFiltersExpanded; }
            set
            {
                if (value == isFiltersExpanded)
                    return;

                isFiltersExpanded = value;
                base.OnPropertyChanged(nameof(IsFiltersExpanded));
            }
        }

        private string filePattern;
        public string FilePattern
        {
            get { return filePattern; }
            set
            {
                if (value == filePattern)
                    return;

                filePattern = value;
                base.OnPropertyChanged(nameof(FilePattern));
            }
        }

        private string filePatternIgnore;
        public string FilePatternIgnore
        {
            get { return filePatternIgnore; }
            set
            {
                if (value == filePatternIgnore)
                    return;

                filePatternIgnore = value;
                base.OnPropertyChanged(nameof(FilePatternIgnore));
            }
        }

        private bool useGitignore;
        public bool UseGitignore
        {
            get { return useGitignore; }
            set
            {
                if (value == useGitignore)
                    return;

                useGitignore = value;
                base.OnPropertyChanged(nameof(UseGitignore));
            }
        }

        public bool IsGitInstalled
        {
            get { return Utils.IsGitInstalled; }
        }

        private bool includeSubfolder;
        public bool IncludeSubfolder
        {
            get { return includeSubfolder; }
            set
            {
                if (value == includeSubfolder)
                    return;

                includeSubfolder = value;
                base.OnPropertyChanged(nameof(IncludeSubfolder));

                if (!includeSubfolder)
                {
                    MaxSubfolderDepth = -1;
                }
            }
        }

        private int maxSubfolderDepth = -1;
        public int MaxSubfolderDepth
        {
            get { return maxSubfolderDepth; }
            set
            {
                if (value == maxSubfolderDepth)
                    return;

                maxSubfolderDepth = value;
                base.OnPropertyChanged(nameof(MaxSubfolderDepth));
            }
        }

        private bool includeHidden;
        public bool IncludeHidden
        {
            get { return includeHidden; }
            set
            {
                if (value == includeHidden)
                    return;

                includeHidden = value;
                base.OnPropertyChanged(nameof(IncludeHidden));
            }
        }

        private bool includeBinary;
        public bool IncludeBinary
        {
            get { return includeBinary; }
            set
            {
                if (value == includeBinary)
                    return;

                includeBinary = value;
                base.OnPropertyChanged(nameof(IncludeBinary));
            }
        }

        private bool followSymlinks;
        public bool FollowSymlinks
        {
            get { return followSymlinks; }
            set
            {
                if (value == followSymlinks)
                    return;

                followSymlinks = value;
                base.OnPropertyChanged(nameof(FollowSymlinks));
            }
        }

        private bool includeArchive;
        public bool IncludeArchive
        {
            get { return includeArchive; }
            set
            {
                if (value == includeArchive)
                    return;

                includeArchive = value;
                base.OnPropertyChanged(nameof(IncludeArchive));
            }
        }


        private bool searchParallel;
        public bool SearchParallel
        {
            get { return searchParallel; }
            set
            {
                if (value == searchParallel)
                    return;

                searchParallel = value;
                base.OnPropertyChanged(nameof(SearchParallel));
            }
        }

        private SearchType typeOfSearch;
        public SearchType TypeOfSearch
        {
            get { return typeOfSearch; }
            set
            {
                if (value == typeOfSearch)
                    return;

                typeOfSearch = value;
                base.OnPropertyChanged(nameof(TypeOfSearch));
            }
        }

        private FileSearchType typeOfFileSearch;
        public FileSearchType TypeOfFileSearch
        {
            get { return typeOfFileSearch; }
            set
            {
                if (value == typeOfFileSearch)
                    return;

                typeOfFileSearch = value;
                PathSearchText.TypeOfFileSearch = value;
                base.OnPropertyChanged(nameof(TypeOfFileSearch));
            }
        }

        private bool isEverythingAvailable;
        public bool IsEverythingAvailable
        {
            get { return isEverythingAvailable; }
            set
            {
                if (value == isEverythingAvailable)
                    return;

                isEverythingAvailable = value;
                base.OnPropertyChanged(nameof(IsEverythingAvailable));
            }
        }


        private bool isEverythingSearchMode;
        public bool IsEverythingSearchMode
        {
            get { return isEverythingSearchMode; }
            set
            {
                if (value == isEverythingSearchMode)
                    return;

                isEverythingSearchMode = value;
                base.OnPropertyChanged(nameof(IsEverythingSearchMode));
            }
        }

        private string patternColumnWidth = STAR;
        public string PatternColumnWidth
        {
            get { return patternColumnWidth; }
            set
            {
                if (value == patternColumnWidth)
                    return;

                patternColumnWidth = value;
                base.OnPropertyChanged(nameof(PatternColumnWidth));
            }
        }

        private string searchTextBoxLabel = Resources.Main_Folder;
        public string SearchTextBoxLabel
        {
            get { return searchTextBoxLabel; }
            set
            {
                if (value == searchTextBoxLabel)
                    return;

                searchTextBoxLabel = value;
                base.OnPropertyChanged(nameof(SearchTextBoxLabel));
            }
        }

        private FileSizeFilter useFileSizeFilter = FileSizeFilter.None;
        public FileSizeFilter UseFileSizeFilter
        {
            get { return useFileSizeFilter; }
            set
            {
                if (value == useFileSizeFilter)
                    return;

                useFileSizeFilter = value;
                base.OnPropertyChanged(nameof(UseFileSizeFilter));
            }
        }

        private int sizeFrom;
        public int SizeFrom
        {
            get { return sizeFrom; }
            set
            {
                if (value == sizeFrom)
                    return;

                sizeFrom = value;
                base.OnPropertyChanged(nameof(SizeFrom));
            }
        }

        private int sizeTo;
        public int SizeTo
        {
            get { return sizeTo; }
            set
            {
                if (value == sizeTo)
                    return;

                sizeTo = value;
                base.OnPropertyChanged(nameof(SizeTo));
            }
        }

        private FileDateFilter useFileDateFilter;
        public FileDateFilter UseFileDateFilter
        {
            get { return useFileDateFilter; }
            set
            {
                if (value == useFileDateFilter)
                    return;

                useFileDateFilter = value;
                base.OnPropertyChanged(nameof(UseFileDateFilter));
            }
        }

        private FileTimeRange typeOfTimeRangeFilter;
        public FileTimeRange TypeOfTimeRangeFilter
        {
            get { return typeOfTimeRangeFilter; }
            set
            {
                if (value == typeOfTimeRangeFilter)
                    return;

                typeOfTimeRangeFilter = value;
                base.OnPropertyChanged(nameof(TypeOfTimeRangeFilter));
            }
        }

        public readonly static DateTime minDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);

        private DateTime minStartDate = minDate;
        public DateTime MinStartDate
        {
            get { return minStartDate; }
            set
            {
                if (value == minStartDate)
                    return;

                minStartDate = value;
                base.OnPropertyChanged(nameof(MinStartDate));
            }
        }

        private DateTime? startDate;
        public DateTime? StartDate
        {
            get { return startDate; }
            set
            {
                if (value == startDate)
                    return;

                startDate = value;
                if (startDate.HasValue)
                {
                    MinEndDate = startDate.Value;
                    if (EndDate.HasValue && EndDate.Value < MinEndDate)
                        EndDate = MinEndDate;
                }
                else
                {
                    MinEndDate = minDate;
                }

                base.OnPropertyChanged(nameof(StartDate));
            }
        }

        private DateTime minEndDate = minDate;
        public DateTime MinEndDate
        {
            get { return minEndDate; }
            set
            {
                if (value == minEndDate)
                    return;

                minEndDate = value;
                base.OnPropertyChanged(nameof(MinEndDate));
            }
        }

        private DateTime? endDate;
        public DateTime? EndDate
        {
            get { return endDate; }
            set
            {
                if (value == endDate)
                    return;

                endDate = value;
                base.OnPropertyChanged(nameof(EndDate));
            }
        }

        private int hoursFrom;
        public int HoursFrom
        {
            get { return hoursFrom; }
            set
            {
                if (value == hoursFrom)
                    return;

                hoursFrom = value;
                base.OnPropertyChanged(nameof(HoursFrom));
            }
        }

        private int hoursTo;
        public int HoursTo
        {
            get { return hoursTo; }
            set
            {
                if (value == hoursTo)
                    return;

                hoursTo = value;
                base.OnPropertyChanged(nameof(HoursTo));
            }
        }

        private bool isDateFilterSet;
        public bool IsDateFilterSet
        {
            get { return isDateFilterSet; }
            set
            {
                if (value == isDateFilterSet)
                    return;

                isDateFilterSet = value;
                base.OnPropertyChanged(nameof(IsDateFilterSet));
            }
        }

        private bool isDatesRangeSet;
        public bool IsDatesRangeSet
        {
            get { return isDatesRangeSet; }
            set
            {
                if (value == isDatesRangeSet)
                    return;

                isDatesRangeSet = value;
                base.OnPropertyChanged(nameof(IsDatesRangeSet));
            }
        }

        private bool isHoursRangeSet;
        public bool IsHoursRangeSet
        {
            get { return isHoursRangeSet; }
            set
            {
                if (value == isHoursRangeSet)
                    return;

                isHoursRangeSet = value;
                base.OnPropertyChanged(nameof(IsHoursRangeSet));
            }
        }

        private bool caseSensitive;
        public bool CaseSensitive
        {
            get { return caseSensitive; }
            set
            {
                if (value == caseSensitive)
                    return;

                caseSensitive = value;
                base.OnPropertyChanged(nameof(CaseSensitive));
            }
        }

        private bool previewFileContent;
        public bool PreviewFileContent
        {
            get { return previewFileContent; }
            set
            {
                if (value == previewFileContent)
                    return;

                previewFileContent = value;
                base.OnPropertyChanged(nameof(PreviewFileContent));
            }
        }

        private bool isCaseSensitiveEnabled;
        public bool IsCaseSensitiveEnabled
        {
            get { return isCaseSensitiveEnabled; }
            set
            {
                if (value == isCaseSensitiveEnabled)
                    return;

                isCaseSensitiveEnabled = value;
                base.OnPropertyChanged(nameof(IsCaseSensitiveEnabled));
            }
        }

        private bool multiline;
        public bool Multiline
        {
            get { return multiline; }
            set
            {
                if (value == multiline)
                    return;

                multiline = value;
                base.OnPropertyChanged(nameof(Multiline));
            }
        }

        private bool isMultilineEnabled;
        public bool IsMultilineEnabled
        {
            get { return isMultilineEnabled; }
            set
            {
                if (value == isMultilineEnabled)
                    return;

                isMultilineEnabled = value;
                base.OnPropertyChanged(nameof(IsMultilineEnabled));
            }
        }

        private bool singleline;
        public bool Singleline
        {
            get { return singleline; }
            set
            {
                if (value == singleline)
                    return;

                if (value)
                    Multiline = true;

                singleline = value;
                base.OnPropertyChanged(nameof(Singleline));
            }
        }

        private bool isSinglelineEnabled;
        public bool IsSinglelineEnabled
        {
            get { return isSinglelineEnabled; }
            set
            {
                if (value == isSinglelineEnabled)
                    return;

                isSinglelineEnabled = value;
                base.OnPropertyChanged(nameof(IsSinglelineEnabled));
            }
        }

        private bool highlightCaptureGroups;
        public bool HighlightCaptureGroups
        {
            get { return highlightCaptureGroups; }
            set
            {
                if (value == highlightCaptureGroups)
                    return;

                highlightCaptureGroups = value;
                base.OnPropertyChanged(nameof(HighlightCaptureGroups));
            }
        }

        private bool isHighlightGroupsEnabled;
        public bool IsHighlightGroupsEnabled
        {
            get { return isHighlightGroupsEnabled; }
            set
            {
                if (value == isHighlightGroupsEnabled)
                    return;

                isHighlightGroupsEnabled = value;
                base.OnPropertyChanged(nameof(IsHighlightGroupsEnabled));
            }
        }

        private bool stopAfterFirstMatch;
        public bool StopAfterFirstMatch
        {
            get { return stopAfterFirstMatch; }
            set
            {
                if (value == stopAfterFirstMatch)
                    return;

                stopAfterFirstMatch = value;
                base.OnPropertyChanged(nameof(StopAfterFirstMatch));
            }
        }

        private bool wholeWord;
        public bool WholeWord
        {
            get { return wholeWord; }
            set
            {
                if (value == wholeWord)
                    return;

                wholeWord = value;
                base.OnPropertyChanged(nameof(WholeWord));
            }
        }

        private bool isWholeWordEnabled;
        public bool IsWholeWordEnabled
        {
            get { return isWholeWordEnabled; }
            set
            {
                if (value == isWholeWordEnabled)
                    return;

                isWholeWordEnabled = value;
                base.OnPropertyChanged(nameof(IsWholeWordEnabled));
            }
        }

        private bool booleanOperators;
        public bool BooleanOperators
        {
            get { return booleanOperators; }
            set
            {
                if (value == booleanOperators)
                    return;

                booleanOperators = value;
                base.OnPropertyChanged(nameof(BooleanOperators));
            }
        }

        private bool isBooleanOperatorsEnabled;
        public bool IsBooleanOperatorsEnabled
        {
            get { return isBooleanOperatorsEnabled; }
            set
            {
                if (value == isBooleanOperatorsEnabled)
                    return;

                isBooleanOperatorsEnabled = value;
                base.OnPropertyChanged(nameof(IsBooleanOperatorsEnabled));
            }
        }

        private bool captureGroupSearch;
        public bool CaptureGroupSearch
        {
            get { return captureGroupSearch; }
            set
            {
                if (value == captureGroupSearch)
                    return;

                captureGroupSearch = value;
                base.OnPropertyChanged(nameof(CaptureGroupSearch));
            }
        }

        private bool isSizeFilterSet;
        public bool IsSizeFilterSet
        {
            get { return isSizeFilterSet; }
            set
            {
                if (value == isSizeFilterSet)
                    return;

                isSizeFilterSet = value;
                base.OnPropertyChanged(nameof(IsSizeFilterSet));
            }
        }

        private bool filesFound;
        public bool FilesFound
        {
            get { return filesFound; }
            set
            {
                if (value == filesFound)
                    return;

                filesFound = value;
                base.OnPropertyChanged(nameof(FilesFound));
            }
        }

        private bool canSearch;
        public bool CanSearch
        {
            get { return canSearch; }
            set
            {
                if (value == canSearch)
                    return;

                canSearch = value;
                base.OnPropertyChanged(nameof(CanSearch));
                // Refresh buttons
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool canSearchInResults;
        public bool CanSearchInResults
        {
            get { return canSearchInResults; }
            set
            {
                if (value == canSearchInResults)
                    return;

                canSearchInResults = value;
                base.OnPropertyChanged(nameof(CanSearchInResults));
                // Refresh buttons
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool searchInResultsContent;
        public bool SearchInResultsContent
        {
            get { return searchInResultsContent; }
            set
            {
                if (value == searchInResultsContent)
                    return;

                searchInResultsContent = value;
                base.OnPropertyChanged(nameof(SearchInResultsContent));
            }
        }

        private bool canCancel;
        public bool CanCancel
        {
            get { return canCancel; }
            set
            {
                if (value == canCancel)
                    return;

                canCancel = value;
                base.OnPropertyChanged(nameof(CanCancel));
                // Refresh buttons
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private GrepOperation currentGrepOperation;
        public GrepOperation CurrentGrepOperation
        {
            get { return currentGrepOperation; }
            set
            {
                if (value == currentGrepOperation)
                    return;

                currentGrepOperation = value;
                base.OnPropertyChanged(nameof(CurrentGrepOperation));
                base.OnPropertyChanged(nameof(IsOperationInProgress));
            }
        }

        private string fileFiltersSummary;
        public string FileFiltersSummary
        {
            get { return fileFiltersSummary; }
            set
            {
                if (value == fileFiltersSummary)
                    return;

                fileFiltersSummary = value;
                base.OnPropertyChanged(nameof(FileFiltersSummary));
            }
        }

        private double maxFileFiltersSummaryWidth;
        public double MaxFileFiltersSummaryWidth
        {
            get { return maxFileFiltersSummaryWidth; }
            set
            {
                if (value == maxFileFiltersSummaryWidth)
                    return;

                maxFileFiltersSummaryWidth = value;
                base.OnPropertyChanged(nameof(MaxFileFiltersSummaryWidth));
            }
        }

        private bool isValidPattern;

        public bool IsValidPattern
        {
            get { return isValidPattern; }
            set
            {
                if (value == isValidPattern)
                    return;

                isValidPattern = value;
                base.OnPropertyChanged(nameof(IsValidPattern));
            }
        }

        private string validationMessage;
        public string ValidationMessage
        {
            get { return validationMessage; }
            set
            {
                if (value == validationMessage)
                    return;

                validationMessage = value;
                base.OnPropertyChanged(nameof(ValidationMessage));
            }
        }

        private string validationToolTip;
        public string ValidationToolTip
        {
            get { return validationToolTip; }
            set
            {
                if (value == validationToolTip)
                    return;

                validationToolTip = value;
                base.OnPropertyChanged(nameof(ValidationToolTip));
            }
        }

        private bool hasValidationMessage;

        public bool HasValidationMessage
        {
            get { return hasValidationMessage; }
            set
            {
                if (value == hasValidationMessage)
                    return;

                hasValidationMessage = value;
                base.OnPropertyChanged(nameof(HasValidationMessage));
            }
        }

        private string windowTitle = Resources.Main_DnGREP_Title;
        public string WindowTitle
        {
            get { return windowTitle; }
            set
            {
                if (value == windowTitle)
                    return;

                windowTitle = value;
                base.OnPropertyChanged(nameof(WindowTitle));
            }
        }

        private string textBoxStyle;
        public string TextBoxStyle
        {
            get { return textBoxStyle; }
            set
            {
                if (value == textBoxStyle)
                    return;

                textBoxStyle = value;
                base.OnPropertyChanged(nameof(TextBoxStyle));
            }
        }

        private int codePage;
        public int CodePage
        {
            get { return codePage; }
            set
            {
                if (value == codePage)
                    return;

                codePage = value;
                base.OnPropertyChanged(nameof(CodePage));
            }
        }

        private bool canUndo;
        public bool CanUndo
        {
            get { return canUndo; }
            set
            {
                if (value == canUndo)
                    return;

                canUndo = value;
                base.OnPropertyChanged(nameof(CanUndo));
            }
        }

        private string statusMessage;
        public string StatusMessage
        {
            get { return statusMessage; }
            set
            {
                if (value == statusMessage)
                    return;

                statusMessage = value;
                base.OnPropertyChanged(nameof(StatusMessage));
            }
        }

        private bool isSaveInProgress;
        public bool IsSaveInProgress
        {
            get { return isSaveInProgress; }
            set
            {
                if (value == isSaveInProgress)
                    return;

                isSaveInProgress = value;
                base.OnPropertyChanged(nameof(IsSaveInProgress));
            }
        }

        private bool optionsOnMainPanel = true;
        public bool OptionsOnMainPanel
        {
            get { return optionsOnMainPanel; }
            set
            {
                if (optionsOnMainPanel == value)
                    return;

                optionsOnMainPanel = value;
                base.OnPropertyChanged(nameof(OptionsOnMainPanel));
            }
        }

        private bool canSearchArchives = false;
        public bool CanSearchArchives
        {
            get { return canSearchArchives; }
            set
            {
                if (canSearchArchives == value)
                    return;

                canSearchArchives = value;
                base.OnPropertyChanged(nameof(CanSearchArchives));
            }
        }

        private string resultsFontFamily;
        public string ResultsFontFamily
        {
            get { return resultsFontFamily; }
            set
            {
                if (resultsFontFamily == value)
                    return;

                resultsFontFamily = value;
                base.OnPropertyChanged(nameof(ResultsFontFamily));
            }
        }

        private double resultsfontSize;
        public double ResultsFontSize
        {
            get { return resultsfontSize; }
            set
            {
                if (resultsfontSize == value)
                    return;

                resultsfontSize = value;
                base.OnPropertyChanged(nameof(ResultsFontSize));
            }
        }

        public bool IsOperationInProgress
        {
            get { return CurrentGrepOperation != GrepOperation.None; }
        }

        #endregion

        #region Public Methods

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

            //Change validation
            if (name == nameof(SearchFor) || name == nameof(TypeOfSearch) || name == nameof(BooleanOperators))
            {
                ValidationMessage = string.Empty;
                ValidationToolTip = null;
                IsValidPattern = true;

                if (!string.IsNullOrWhiteSpace(SearchFor))
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
                    else if (TypeOfSearch == SearchType.XPath)
                    {
                        try
                        {
                            nav = doc.CreateNavigator();
                            XPathExpression expr = nav.Compile(SearchFor);
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
                if (SearchResults.Count > 0 && !IsSaveInProgress)
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
                BooleanExpression exp = new BooleanExpression();
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
                Regex regex = new Regex(pattern);
                ValidationMessage = Resources.Main_Validation_RegexIsOK;
                IsValidPattern = true;
            }
            catch (Exception ex)
            {
                ValidationMessage = Resources.Main_Validation_RegexIsNotValid;
                ValidationToolTip = ex.Message;
                IsValidPattern = false;
            }
            return isValidPattern;
        }

        protected void ResetOptions()
        {
            UseFileSizeFilter = FileSizeFilter.No;
            IncludeBinary = true;
            IncludeHidden = true;
            IncludeSubfolder = true;
            MaxSubfolderDepth = -1;
            IncludeArchive = Utils.ArchiveExtensions.Count > 0;
            FollowSymlinks = false;
            UseFileDateFilter = FileDateFilter.None;
            TypeOfTimeRangeFilter = FileTimeRange.None;
            FilePattern = "*";
            FilePatternIgnore = "";
            TypeOfFileSearch = FileSearchType.Asterisk;
            UseGitignore = Utils.IsGitInstalled;
        }

        virtual public void LoadSettings()
        {
            List<string> fsb = Settings.Get<List<string>>(GrepSettings.Key.FastSearchBookmarks);
            string _searchFor = Settings.Get<string>(GrepSettings.Key.SearchFor);
            if (fsb != null)
            {
                var toRemove = FastSearchBookmarks.Except(fsb).ToList();
                foreach (var item in toRemove)
                {
                    FastSearchBookmarks.Remove(item);
                }

                foreach (string bookmark in fsb)
                {
                    if (!FastSearchBookmarks.Contains(bookmark))
                        FastSearchBookmarks.Add(bookmark);
                }
            }
            Settings[GrepSettings.Key.SearchFor] = _searchFor;

            string _replaceWith = Settings.Get<string>(GrepSettings.Key.ReplaceWith);
            List<string> frb = Settings.Get<List<string>>(GrepSettings.Key.FastReplaceBookmarks);
            if (frb != null)
            {
                var toRemove = FastReplaceBookmarks.Except(frb).ToList();
                foreach (var item in toRemove)
                {
                    FastReplaceBookmarks.Remove(item);
                }

                foreach (string bookmark in frb)
                {
                    if (!FastReplaceBookmarks.Contains(bookmark))
                        FastReplaceBookmarks.Add(bookmark);
                }
            }
            Settings[GrepSettings.Key.ReplaceWith] = _replaceWith;

            string _filePattern = Settings.Get<string>(GrepSettings.Key.FilePattern);
            List<string> ffmb = Settings.Get<List<string>>(GrepSettings.Key.FastFileMatchBookmarks);
            if (ffmb != null)
            {
                var toRemove = FastFileMatchBookmarks.Except(ffmb).ToList();
                foreach (var item in toRemove)
                {
                    FastFileMatchBookmarks.Remove(item);
                }

                foreach (string bookmark in ffmb)
                {
                    if (!FastFileMatchBookmarks.Contains(bookmark))
                        FastFileMatchBookmarks.Add(bookmark);
                }
            }
            Settings[GrepSettings.Key.FilePattern] = _filePattern;

            string _filePatternIgnore = Settings.Get<string>(GrepSettings.Key.FilePatternIgnore);
            List<string> ffnmb = Settings.Get<List<string>>(GrepSettings.Key.FastFileNotMatchBookmarks);
            if (ffnmb != null)
            {
                var toRemove = FastFileNotMatchBookmarks.Except(ffmb).ToList();
                foreach (var item in toRemove)
                {
                    FastFileNotMatchBookmarks.Remove(item);
                }

                foreach (string bookmark in ffnmb)
                {
                    if (!FastFileNotMatchBookmarks.Contains(bookmark))
                        FastFileNotMatchBookmarks.Add(bookmark);
                }
            }
            Settings[GrepSettings.Key.FilePatternIgnore] = _filePatternIgnore;

            List<string> pb = Settings.Get<List<string>>(GrepSettings.Key.FastPathBookmarks);
            if (pb != null)
            {
                var toRemove = FastPathBookmarks.Except(pb).ToList();
                foreach (var item in toRemove)
                {
                    FastPathBookmarks.Remove(item);
                }

                foreach (string bookmark in pb)
                {
                    if (!FastPathBookmarks.Contains(bookmark))
                        FastPathBookmarks.Add(bookmark);
                }
            }

            SearchFor = Settings.Get<string>(GrepSettings.Key.SearchFor);
            ReplaceWith = Settings.Get<string>(GrepSettings.Key.ReplaceWith);
            IncludeHidden = Settings.Get<bool>(GrepSettings.Key.IncludeHidden);
            IncludeBinary = Settings.Get<bool>(GrepSettings.Key.IncludeBinary);
            IncludeArchive = Settings.Get<bool>(GrepSettings.Key.IncludeArchive) && Utils.ArchiveExtensions.Count > 0;
            SearchParallel = Settings.Get<bool>(GrepSettings.Key.SearchParallel);
            IncludeSubfolder = Settings.Get<bool>(GrepSettings.Key.IncludeSubfolder);
            MaxSubfolderDepth = Settings.Get<int>(GrepSettings.Key.MaxSubfolderDepth);
            FollowSymlinks = Settings.Get<bool>(GrepSettings.Key.FollowSymlinks);
            TypeOfSearch = Settings.Get<SearchType>(GrepSettings.Key.TypeOfSearch);
            TypeOfFileSearch = Settings.Get<FileSearchType>(GrepSettings.Key.TypeOfFileSearch);
            // FileOrFolderPath depends on TypeOfFileSearch, so must be after
            FileOrFolderPath = Settings.Get<string>(GrepSettings.Key.SearchFolder);
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
            StartDate = Settings.GetNullableDateTime(GrepSettings.Key.StartDate);
            EndDate = Settings.GetNullableDateTime(GrepSettings.Key.EndDate);
            HoursFrom = Settings.Get<int>(GrepSettings.Key.HoursFrom);
            HoursTo = Settings.Get<int>(GrepSettings.Key.HoursTo);
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
            Settings.SetNullableDateTime(GrepSettings.Key.StartDate, StartDate);
            Settings.SetNullableDateTime(GrepSettings.Key.EndDate, EndDate);
            Settings.Set(GrepSettings.Key.HoursFrom, HoursFrom);
            Settings.Set(GrepSettings.Key.HoursTo, HoursTo);

            Settings.Save();
        }

        #endregion

        #region Private Methods

        void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateState(e.PropertyName);
        }

        #endregion

        #region IDataErrorInfo Members

        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                string error = null;

                // Do validation

                // Dirty the commands registered with CommandManager,
                // such as our Save command, so that they are queried
                // to see if they can execute now.
                CommandManager.InvalidateRequerySuggested();

                return error;
            }
        }


        public string Error
        {
            get { return null; }
        }

        #endregion
    }
}
