using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Xml;
using System.Xml.XPath;
using dnGREP.Common;
using dnGREP.Engines;
using dnGREP.Everything;
using NLog;

namespace dnGREP.WPF
{
    public class BaseMainViewModel : WorkspaceViewModel, IDataErrorInfo
    {
        public static int FastBookmarkCapacity = 20;

        public BaseMainViewModel()
        {
            RequestClose += MainViewModel_RequestClose;
            PropertyChanged += MainViewModel_PropertyChanged;

            CurrentGrepOperation = GrepOperation.None;
            IsCaseSensitiveEnabled = true;
            IsMultilineEnabled = true;
            IsWholeWordEnabled = true;
            Utils.ArchiveExtensions = GrepEngineFactory.GetArchiveExtenstions();
            CanSearchArchives = Utils.ArchiveExtensions.Count > 0;
            LoadSettings();

            IsEverythingAvailable = EverythingSearch.IsAvailable;
        }

        #region Private Variables and Properties
        private XmlDocument doc = new XmlDocument();
        private XPathNavigator nav;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        protected GrepSettings settings
        {
            get { return GrepSettings.Instance; }
        }

        protected PathSearchText PathSearchText { get; private set; } = new PathSearchText();
        #endregion

        #region Properties
        private SyntaxHighlighterViewModel contentPreviewModel;
        public SyntaxHighlighterViewModel ContentPreviewModel
        {
            get { return contentPreviewModel; }
            set
            {
                if (value == contentPreviewModel)
                    return;

                contentPreviewModel = value;

                base.OnPropertyChanged(() => ContentPreviewModel);
            }
        }

        private ObservableGrepSearchResults searchResults = new ObservableGrepSearchResults();
        public ObservableGrepSearchResults SearchResults
        {
            get { return searchResults; }
        }

        private ObservableCollection<string> fastSearchBookmarks = new ObservableCollection<string>();
        public ObservableCollection<string> FastSearchBookmarks
        {
            get { return fastSearchBookmarks; }
        }

        private ObservableCollection<string> fastReplaceBookmarks = new ObservableCollection<string>();
        public ObservableCollection<string> FastReplaceBookmarks
        {
            get { return fastReplaceBookmarks; }
        }

        private ObservableCollection<string> fastFileMatchBookmarks = new ObservableCollection<string>();
        public ObservableCollection<string> FastFileMatchBookmarks
        {
            get { return fastFileMatchBookmarks; }
        }

        private ObservableCollection<string> fastFileNotMatchBookmarks = new ObservableCollection<string>();
        public ObservableCollection<string> FastFileNotMatchBookmarks
        {
            get { return fastFileNotMatchBookmarks; }
        }

        private ObservableCollection<string> fastPathBookmarks = new ObservableCollection<string>();
        public ObservableCollection<string> FastPathBookmarks
        {
            get { return fastPathBookmarks; }
        }

        private ObservableCollection<KeyValuePair<string, int>> encodings = new ObservableCollection<KeyValuePair<string, int>>();
        public ObservableCollection<KeyValuePair<string, int>> Encodings
        {
            get { return encodings; }
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

                base.OnPropertyChanged(() => FileOrFolderPath);
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

                base.OnPropertyChanged(() => SearchFor);
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

                base.OnPropertyChanged(() => ReplaceWith);
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

                base.OnPropertyChanged(() => IsFiltersExpanded);
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

                base.OnPropertyChanged(() => FilePattern);
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

                base.OnPropertyChanged(() => FilePatternIgnore);
            }
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

                base.OnPropertyChanged(() => IncludeSubfolder);
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

                base.OnPropertyChanged(() => IncludeHidden);
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

                base.OnPropertyChanged(() => IncludeBinary);
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

                base.OnPropertyChanged(() => IncludeArchive);
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

                base.OnPropertyChanged(() => SearchParallel);
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

                base.OnPropertyChanged(() => TypeOfSearch);
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

                base.OnPropertyChanged(() => TypeOfFileSearch);
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

                base.OnPropertyChanged(() => IsEverythingAvailable);
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

                base.OnPropertyChanged(() => IsEverythingSearchMode);
            }
        }

        private string patternColumnWidth = "*";
        public string PatternColumnWidth
        {
            get { return patternColumnWidth; }
            set
            {
                if (value == patternColumnWidth)
                    return;

                patternColumnWidth = value;

                base.OnPropertyChanged(() => PatternColumnWidth);
            }
        }

        private string searchTextBoxLabel = "Folder:";
        public string SearchTextBoxLabel
        {
            get { return searchTextBoxLabel; }
            set
            {
                if (value == searchTextBoxLabel)
                    return;

                searchTextBoxLabel = value;

                base.OnPropertyChanged(() => SearchTextBoxLabel);
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

                base.OnPropertyChanged(() => UseFileSizeFilter);
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

                base.OnPropertyChanged(() => SizeFrom);
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

                base.OnPropertyChanged(() => SizeTo);
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

                base.OnPropertyChanged(() => UseFileDateFilter);
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

                base.OnPropertyChanged(() => TypeOfTimeRangeFilter);
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

                base.OnPropertyChanged(() => MinStartDate);
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

                base.OnPropertyChanged(() => StartDate);
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

                base.OnPropertyChanged(() => MinEndDate);
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

                base.OnPropertyChanged(() => EndDate);
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

                base.OnPropertyChanged(() => HoursFrom);
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

                base.OnPropertyChanged(() => HoursTo);
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

                base.OnPropertyChanged(() => IsDateFilterSet);
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

                base.OnPropertyChanged(() => IsDatesRangeSet);
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

                base.OnPropertyChanged(() => IsHoursRangeSet);
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

                base.OnPropertyChanged(() => CaseSensitive);
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

                base.OnPropertyChanged(() => PreviewFileContent);
            }
        }

        private bool isBookmarked;
        public bool IsBookmarked
        {
            get { return isBookmarked; }
            set
            {
                if (value == isBookmarked)
                    return;

                isBookmarked = value;

                base.OnPropertyChanged(() => IsBookmarked);
                base.OnPropertyChanged(() => IsBookmarkedTooltip);
            }
        }

        public string IsBookmarkedTooltip
        {
            get
            {
                if (!IsBookmarked)
                    return "Add search pattern to bookmarks";
                else
                    return "Clear bookmark";
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

                base.OnPropertyChanged(() => IsCaseSensitiveEnabled);
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

                base.OnPropertyChanged(() => Multiline);
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

                base.OnPropertyChanged(() => IsMultilineEnabled);
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

                base.OnPropertyChanged(() => Singleline);
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

                base.OnPropertyChanged(() => IsSinglelineEnabled);
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

                base.OnPropertyChanged(() => StopAfterFirstMatch);
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

                base.OnPropertyChanged(() => WholeWord);
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

                base.OnPropertyChanged(() => IsWholeWordEnabled);
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

                base.OnPropertyChanged(() => IsSizeFilterSet);
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

                base.OnPropertyChanged(() => FilesFound);
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

                base.OnPropertyChanged(() => CanSearch);
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

                base.OnPropertyChanged(() => CanSearchInResults);
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

                base.OnPropertyChanged(() => SearchInResultsContent);
            }
        }

        private bool canReplace;
        public bool CanReplace
        {
            get { return canReplace; }
            set
            {
                if (value == canReplace)
                    return;

                canReplace = value;

                base.OnPropertyChanged(() => CanReplace);
                // Refresh buttons
                CommandManager.InvalidateRequerySuggested();
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

                base.OnPropertyChanged(() => CanCancel);
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

                base.OnPropertyChanged(() => CurrentGrepOperation);
                base.OnPropertyChanged(() => IsOperationInProgress);
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

                base.OnPropertyChanged(() => FileFiltersSummary);
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

                base.OnPropertyChanged(() => MaxFileFiltersSummaryWidth);
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

                base.OnPropertyChanged(() => ValidationMessage);
            }
        }

        private string windowTitle = "dnGREP";
        public string WindowTitle
        {
            get { return windowTitle; }
            set
            {
                if (value == windowTitle)
                    return;

                windowTitle = value;

                base.OnPropertyChanged(() => WindowTitle);
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

                base.OnPropertyChanged(() => TextBoxStyle);
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

                base.OnPropertyChanged(() => CodePage);
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

                base.OnPropertyChanged(() => CanUndo);
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

                base.OnPropertyChanged(() => StatusMessage);
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

                base.OnPropertyChanged(() => IsSaveInProgress);
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
                base.OnPropertyChanged(() => OptionsOnMainPanel);
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
                base.OnPropertyChanged(() => CanSearchArchives);
            }
        }



        public bool IsOperationInProgress
        {
            get { return CurrentGrepOperation != GrepOperation.None; }
        }

        #endregion

        #region Presentation Properties
        // Need to be implemented separately
        #endregion

        #region Public Methods
        public virtual void UpdateState(string name)
        {
            List<string> tempList = null;
            switch (name)
            {
                case "Multiline":
                case "Singleline":
                case "WholeWord":
                case "CaseSensitive":
                case "StopAfterFirstMatch":
                    if (Multiline)
                        TextBoxStyle = "{StaticResource ExpandedTextbox}";
                    else
                        TextBoxStyle = "";

                    CanReplace = false;
                    break;

                case "UseFileSizeFilter":
                    IsSizeFilterSet = UseFileSizeFilter == FileSizeFilter.Yes;
                    break;

                case "UseFileDateFilter":
                    IsDateFilterSet = UseFileDateFilter != FileDateFilter.None;
                    IsDatesRangeSet = IsDateFilterSet && TypeOfTimeRangeFilter == FileTimeRange.Dates;
                    IsHoursRangeSet = IsDateFilterSet && TypeOfTimeRangeFilter == FileTimeRange.Hours;
                    if (!IsDateFilterSet)
                        TypeOfTimeRangeFilter = FileTimeRange.None;
                    else if (TypeOfTimeRangeFilter == FileTimeRange.None)
                        TypeOfTimeRangeFilter = FileTimeRange.Dates;
                    break;

                case "TypeOfTimeRangeFilter":
                    IsDatesRangeSet = IsDateFilterSet && TypeOfTimeRangeFilter == FileTimeRange.Dates;
                    IsHoursRangeSet = IsDateFilterSet && TypeOfTimeRangeFilter == FileTimeRange.Hours;
                    break;

                case "TypeOfFileSearch":
                    if (TypeOfFileSearch == FileSearchType.Everything)
                    {
                        FilePattern = string.Empty;
                        FilePatternIgnore = string.Empty;
                        IsEverythingSearchMode = true;
                        PatternColumnWidth = "Auto";
                        SearchTextBoxLabel = "Everything search:";
                    }
                    else
                    {
                        IsEverythingSearchMode = false;
                        PatternColumnWidth = "*";
                        SearchTextBoxLabel = "Folder:";
                    }
                    break;
            }

            if (name == "IncludeSubfolder" || name == "IncludeHidden" || name == "IncludeBinary" ||
                name == "UseFileSizeFilter" || name == "UseFileDateFilter")
            {
                tempList = new List<string>();
                if (!IncludeSubfolder)
                    tempList.Add("No subfolders");
                if (!IncludeHidden)
                    tempList.Add("No hidden");
                if (!IncludeBinary)
                    tempList.Add("No binary");
                if (UseFileSizeFilter == FileSizeFilter.Yes)
                    tempList.Add("By Size");
                if (UseFileDateFilter == FileDateFilter.Modified)
                    tempList.Add("By Modified Date");
                if (UseFileDateFilter == FileDateFilter.Created)
                    tempList.Add("By Created Date");

                if (tempList.Count == 0)
                {
                    FileFiltersSummary = "All files";
                }
                else
                {
                    FileFiltersSummary = string.Join(", ", tempList.ToArray());
                }
            }

            //Files found
            if (name == "FileOrFolderPath" || name == "SearchFor" || name == "FilePattern" || name == "FilePatternIgnore")
            {
                FilesFound = false;
            }

            //Change title
            if (name == "FileOrFolderPath" || name == "SearchFor")
            {
                if (string.IsNullOrWhiteSpace(FileOrFolderPath))
                    WindowTitle = "dnGREP";
                else
                    WindowTitle = string.Format("{0} in \"{1}\" - dnGREP", 
                        (SearchFor == null ? "Empty" : SearchFor.Replace('\n', ' ').Replace('\r', ' ')), 
                        PathSearchText.CleanPath);
            }

            //Change validation
            if (name == "SearchFor" || name == "TypeOfSearch")
            {
                if (string.IsNullOrWhiteSpace(SearchFor))
                {
                    ValidationMessage = "";
                }
                else if (TypeOfSearch == SearchType.Regex)
                {
                    try
                    {
                        Regex regex = new Regex(SearchFor);
                        ValidationMessage = "Regex is OK!";
                    }
                    catch
                    {
                        ValidationMessage = "Regex is not valid!";
                    }
                }
                else if (TypeOfSearch == SearchType.XPath)
                {
                    try
                    {
                        nav = doc.CreateNavigator();
                        XPathExpression expr = nav.Compile(SearchFor);
                        ValidationMessage = "XPath is OK!";
                    }
                    catch
                    {
                        ValidationMessage = "XPath is not valid!";
                    }
                }
                else
                {
                    ValidationMessage = "";
                }
            }

            //Can search
            if (name == "FileOrFolderPath"  || name == "TypeOfFileSearch" || name == "CurrentGrepOperation" || name == "SearchFor" || name == "IsSaveInProgress")
            {
                if (PathSearchText.IsValidPath && CurrentGrepOperation == GrepOperation.None && !IsSaveInProgress &&
                    (!string.IsNullOrEmpty(SearchFor) || settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern)))
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

            if (name == "CurrentGrepOperation" || name == "IsSaveInProgress")
            {
                if (searchResults.Count > 0 && !IsSaveInProgress)
                {
                    CanSearchInResults = true;
                }
                else
                {
                    CanSearchInResults = false;
                }
            }

            //searchResults
            searchResults.FolderPath = PathSearchText.BaseFolder;

            // btnReplace
            if (name == "FileOrFolderPath" || name == "FilesFound" || name == "CurrentGrepOperation" || name == "SearchFor" || name == "IsSaveInProgress")
            {
                if (PathSearchText.IsValidBaseFolder && FilesFound && CurrentGrepOperation == GrepOperation.None &&
                    !IsSaveInProgress && !string.IsNullOrEmpty(SearchFor))
                {
                    CanReplace = true;
                }
                else
                {
                    CanReplace = false;
                }
            }

            //btnCancel
            if (name == "CurrentGrepOperation")
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
            if (name == "TypeOfSearch")
            {
                if (TypeOfSearch == SearchType.XPath)
                {
                    IsCaseSensitiveEnabled = false;
                    IsMultilineEnabled = false;
                    IsSinglelineEnabled = false;
                    IsWholeWordEnabled = false;
                    CaseSensitive = false;
                    Multiline = false;
                    Singleline = false;
                    WholeWord = false;
                }
                else if (TypeOfSearch == SearchType.PlainText)
                {
                    IsCaseSensitiveEnabled = true;
                    IsMultilineEnabled = true;
                    IsSinglelineEnabled = false;
                    IsWholeWordEnabled = true;
                    Singleline = false;
                }
                else if (TypeOfSearch == SearchType.Soundex)
                {
                    IsMultilineEnabled = true;
                    IsCaseSensitiveEnabled = false;
                    IsSinglelineEnabled = false;
                    IsWholeWordEnabled = true;
                    CaseSensitive = false;
                    Singleline = false;
                }
                else if (TypeOfSearch == SearchType.Regex)
                {
                    IsCaseSensitiveEnabled = true;
                    IsMultilineEnabled = true;
                    IsSinglelineEnabled = true;
                    IsWholeWordEnabled = true;
                }
            }

            if (IsProperty(() => SearchFor, name) || IsProperty(() => ReplaceWith, name) || IsProperty(() => FilePattern, name))
            {
                if (BookmarkLibrary.Instance.Bookmarks.Contains(new Bookmark(SearchFor, ReplaceWith, FilePattern, "")))
                    IsBookmarked = true;
                else
                    IsBookmarked = false;
            }
        }

        protected void ResetOptions()
        {
            UseFileSizeFilter = FileSizeFilter.No;
            IncludeBinary = true;
            IncludeHidden = true;
            IncludeSubfolder = true;
            IncludeArchive = Utils.ArchiveExtensions.Count > 0;
            UseFileDateFilter = FileDateFilter.None;
            TypeOfTimeRangeFilter = FileTimeRange.None;
            FilePattern = "*";
            FilePatternIgnore = "";
            TypeOfFileSearch = FileSearchType.Asterisk;
        }

        virtual public void LoadSettings()
        {
            List<string> fsb = settings.Get<List<string>>(GrepSettings.Key.FastSearchBookmarks);

            string _searchFor = settings.Get<string>(GrepSettings.Key.SearchFor);
            FastSearchBookmarks.Clear();
            if (fsb != null)
            {
                foreach (string bookmark in fsb)
                {
                    if (!FastSearchBookmarks.Contains(bookmark))
                        FastSearchBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.SearchFor] = _searchFor;

            string _replaceWith = settings.Get<string>(GrepSettings.Key.ReplaceWith);
            FastReplaceBookmarks.Clear();
            List<string> frb = settings.Get<List<string>>(GrepSettings.Key.FastReplaceBookmarks);
            if (frb != null)
            {
                foreach (string bookmark in frb)
                {
                    if (!FastReplaceBookmarks.Contains(bookmark))
                        FastReplaceBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.ReplaceWith] = _replaceWith;

            string _filePattern = settings.Get<string>(GrepSettings.Key.FilePattern);
            FastFileMatchBookmarks.Clear();
            List<string> ffmb = settings.Get<List<string>>(GrepSettings.Key.FastFileMatchBookmarks);
            if (ffmb != null)
            {
                foreach (string bookmark in ffmb)
                {
                    if (!FastFileMatchBookmarks.Contains(bookmark))
                        FastFileMatchBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.FilePattern] = _filePattern;

            string _filePatternIgnore = settings.Get<string>(GrepSettings.Key.FilePatternIgnore);
            FastFileNotMatchBookmarks.Clear();
            List<string> ffnmb = settings.Get<List<string>>(GrepSettings.Key.FastFileNotMatchBookmarks);
            if (ffnmb != null)
            {
                foreach (string bookmark in ffnmb)
                {
                    if (!FastFileNotMatchBookmarks.Contains(bookmark))
                        FastFileNotMatchBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.FilePatternIgnore] = _filePatternIgnore;

            string searchFolder = settings.Get<string>(GrepSettings.Key.SearchFolder);
            FastPathBookmarks.Clear();
            List<string> pb = settings.Get<List<string>>(GrepSettings.Key.FastPathBookmarks);
            if (pb != null)
            {
                foreach (string bookmark in pb)
                {
                    if (!FastPathBookmarks.Contains(bookmark))
                        FastPathBookmarks.Add(bookmark);
                }
            }
            settings[GrepSettings.Key.SearchFolder] = searchFolder;

            SearchFor = settings.Get<string>(GrepSettings.Key.SearchFor);
            ReplaceWith = settings.Get<string>(GrepSettings.Key.ReplaceWith);
            IncludeHidden = settings.Get<bool>(GrepSettings.Key.IncludeHidden);
            IncludeBinary = settings.Get<bool>(GrepSettings.Key.IncludeBinary);
            IncludeArchive = settings.Get<bool>(GrepSettings.Key.IncludeArchive) && Utils.ArchiveExtensions.Count > 0;
            SearchParallel = settings.Get<bool>(GrepSettings.Key.SearchParallel);
            IncludeSubfolder = settings.Get<bool>(GrepSettings.Key.IncludeSubfolder);
            TypeOfSearch = settings.Get<SearchType>(GrepSettings.Key.TypeOfSearch);
            TypeOfFileSearch = settings.Get<FileSearchType>(GrepSettings.Key.TypeOfFileSearch);
            // FileOrFolderPath depends on TypeOfFileSearch, so must be after
            FileOrFolderPath = settings.Get<string>(GrepSettings.Key.SearchFolder);
            CodePage = settings.Get<int>(GrepSettings.Key.CodePage);
            FilePattern = settings.Get<string>(GrepSettings.Key.FilePattern);
            FilePatternIgnore = settings.Get<string>(GrepSettings.Key.FilePatternIgnore);
            UseFileSizeFilter = settings.Get<FileSizeFilter>(GrepSettings.Key.UseFileSizeFilter);
            CaseSensitive = settings.Get<bool>(GrepSettings.Key.CaseSensitive);
            Multiline = settings.Get<bool>(GrepSettings.Key.Multiline);
            Singleline = settings.Get<bool>(GrepSettings.Key.Singleline);
            StopAfterFirstMatch = settings.Get<bool>(GrepSettings.Key.StopAfterFirstMatch);
            WholeWord = settings.Get<bool>(GrepSettings.Key.WholeWord);
            SizeFrom = settings.Get<int>(GrepSettings.Key.SizeFrom);
            SizeTo = settings.Get<int>(GrepSettings.Key.SizeTo);
            IsFiltersExpanded = settings.Get<bool>(GrepSettings.Key.IsFiltersExpanded);
            PreviewFileContent = settings.Get<bool>(GrepSettings.Key.PreviewFileContent);
            OptionsOnMainPanel = settings.Get<bool>(GrepSettings.Key.OptionsOnMainPanel);
            UseFileDateFilter = settings.Get<FileDateFilter>(GrepSettings.Key.UseFileDateFilter);
            TypeOfTimeRangeFilter = settings.Get<FileTimeRange>(GrepSettings.Key.TypeOfTimeRangeFilter);
            StartDate = settings.GetNullableDateTime(GrepSettings.Key.StartDate);
            EndDate = settings.GetNullableDateTime(GrepSettings.Key.EndDate);
            HoursFrom = settings.Get<int>(GrepSettings.Key.HoursFrom);
            HoursTo = settings.Get<int>(GrepSettings.Key.HoursTo);
        }

        public virtual void SaveSettings()
        {
            settings.Set<string>(GrepSettings.Key.SearchFolder, FileOrFolderPath);
            settings.Set<string>(GrepSettings.Key.SearchFor, SearchFor);
            settings.Set<string>(GrepSettings.Key.ReplaceWith, ReplaceWith);
            settings.Set<bool>(GrepSettings.Key.IncludeHidden, IncludeHidden);
            settings.Set<bool>(GrepSettings.Key.IncludeBinary, IncludeBinary);
            settings.Set<bool>(GrepSettings.Key.IncludeArchive, IncludeArchive);
            settings.Set<bool>(GrepSettings.Key.SearchParallel, SearchParallel);
            settings.Set<bool>(GrepSettings.Key.IncludeSubfolder, IncludeSubfolder);
            settings.Set<SearchType>(GrepSettings.Key.TypeOfSearch, TypeOfSearch);
            settings.Set<int>(GrepSettings.Key.CodePage, CodePage);
            settings.Set<FileSearchType>(GrepSettings.Key.TypeOfFileSearch, TypeOfFileSearch);
            settings.Set<string>(GrepSettings.Key.FilePattern, FilePattern);
            settings.Set<string>(GrepSettings.Key.FilePatternIgnore, FilePatternIgnore);
            settings.Set<FileSizeFilter>(GrepSettings.Key.UseFileSizeFilter, UseFileSizeFilter);
            settings.Set<bool>(GrepSettings.Key.CaseSensitive, CaseSensitive);
            settings.Set<bool>(GrepSettings.Key.Multiline, Multiline);
            settings.Set<bool>(GrepSettings.Key.Singleline, Singleline);
            settings.Set<bool>(GrepSettings.Key.StopAfterFirstMatch, StopAfterFirstMatch);
            settings.Set<bool>(GrepSettings.Key.WholeWord, WholeWord);
            settings.Set<int>(GrepSettings.Key.SizeFrom, SizeFrom);
            settings.Set<int>(GrepSettings.Key.SizeTo, SizeTo);
            settings.Set<bool>(GrepSettings.Key.IsFiltersExpanded, IsFiltersExpanded);
            settings.Set<bool>(GrepSettings.Key.PreviewFileContent, PreviewFileContent);
            settings.Set<FileDateFilter>(GrepSettings.Key.UseFileDateFilter, UseFileDateFilter);
            settings.Set<FileTimeRange>(GrepSettings.Key.TypeOfTimeRangeFilter, TypeOfTimeRangeFilter);
            settings.SetNullableDateTime(GrepSettings.Key.StartDate, StartDate);
            settings.SetNullableDateTime(GrepSettings.Key.EndDate, EndDate);
            settings.Set<int>(GrepSettings.Key.HoursFrom, HoursFrom);
            settings.Set<int>(GrepSettings.Key.HoursTo, HoursTo);

            settings.Save();
        }

        #endregion

        #region Private Methods

        void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateState(e.PropertyName);
        }

        private void MainViewModel_RequestClose(object sender, EventArgs e)
        {
            Utils.CancelSearch = true;
            SaveSettings();
            CloseChildWindows();
        }

        protected virtual void CloseChildWindows()
        {
            // do nothing in base class
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
