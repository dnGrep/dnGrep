using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using dnGREP.Common;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using System.Xml;
using System.Windows.Input;
using NLog;
using dnGREP.Common.UI;
using Blue.Windows;
using System.Reflection;
using System.IO;
using System.Drawing;
using dnGREP.Engines;

namespace dnGREP.WPF
{
    public class MainViewModel : WorkspaceViewModel, IDataErrorInfo
	{
		public static int FastBookmarkCapacity = 20;

		public MainViewModel()
		{
            searchResults = new ObservableGrepSearchResults();
            searchResults.PreviewFileLineRequest += searchResults_PreviewFileLineRequest;
            searchResults.PreviewFileRequest += searchResults_PreviewFileRequest;
            searchResults.OpenFileLineRequest += searchResults_OpenFileLineRequest;
            searchResults.OpenFileRequest += searchResults_OpenFileRequest;

            ve.RetrievedVersion += ve_RetrievedVersion;
            this.RequestClose += MainViewModel_RequestClose;
            this.PropertyChanged += MainViewModel_PropertyChanged;
            CurrentGrepOperation = GrepOperation.None;
            IsCaseSensitiveEnabled = true;
            IsMultilineEnabled = true;
            IsWholeWordEnabled = true;
            LoadSettings();
            checkVersion();
            winFormControlsInit();
            populateEncodings();            
		}

        void searchResults_OpenFileRequest(object sender, MVHelpers.GrepResultEventArgs e)
        {
            OpenFile(e.FormattedGrepResult);
        }

        void searchResults_OpenFileLineRequest(object sender, MVHelpers.GrepLineEventArgs e)
        {
            OpenFile(e.FormattedGrepLine);
        }

        void searchResults_PreviewFileRequest(object sender, MVHelpers.GrepResultEventArgs e)
        {
            PreviewFile(e.FormattedGrepResult, e.ParentWindowSize);
        }

        void searchResults_PreviewFileLineRequest(object sender, MVHelpers.GrepLineEventArgs e)
        {
            PreviewFile(e.FormattedGrepLine, e.ParentWindowSize);
        }

        #region Private Variables and Properties
        private XmlDocument doc = new XmlDocument();
        private XPathNavigator nav;
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DateTime timer = DateTime.Now;
        private PublishedVersionExtractor ve = new PublishedVersionExtractor();
        private FileFolderDialogWin32 fileFolderDialog = new FileFolderDialogWin32();
        private BackgroundWorker workerSearchReplace = new BackgroundWorker();
        private BookmarksForm bookmarkForm;
        private System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
        private PreviewView preview;
        private PreviewViewModel previewModel;
        private StickyWindow stickyWindow;

        private GrepSettings settings
        {
            get { return GrepSettings.Instance; }
        }
        #endregion

        #region Properties
        //private ObservableCollection<CodeSnippet> codeSnippets = new ObservableCollection<CodeSnippet>();
        //public ObservableCollection<CodeSnippet> CodeSnippets
        //{
        //    get { return codeSnippets; }
        //}

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

        private ObservableGrepSearchResults searchResults;
        public ObservableGrepSearchResults SearchResults
        {
            get
            {
                return searchResults;
            }
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

        private bool isOptionsExpanded;
        public bool IsOptionsExpanded
        {
            get { return isOptionsExpanded; }
            set
            {
                if (value == isOptionsExpanded)
                    return;

                isOptionsExpanded = value;

                base.OnPropertyChanged(() => IsOptionsExpanded);
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

        private bool fileFilters;
        public bool FileFilters
        {
            get { return fileFilters; }
            set
            {
                if (value == fileFilters)
                    return;

                fileFilters = value;

                base.OnPropertyChanged(() => FileFilters);
            }
        }

        private TextFormattingMode textFormatting;
        public TextFormattingMode TextFormatting
        {
            get { return textFormatting; }
            set
            {
                if (value == textFormatting)
                    return;

                textFormatting = value;

                base.OnPropertyChanged(() => TextFormatting);
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

                base.OnPropertyChanged(() => TypeOfFileSearch);
            }
        }

        private FileSizeFilter useFileSizeFilter;
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
            get {
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
                // Refersh buttons
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
                // Refersh buttons
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string searchButtonMode;
        public string SearchButtonMode
        {
            get { return searchButtonMode; }
            set
            {
                if (value == searchButtonMode)
                    return;

                searchButtonMode = value;

                base.OnPropertyChanged(() => SearchButtonMode);
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
                // Refersh buttons
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
                // Refersh buttons
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

        private string optionsSummary;
        public string OptionsSummary
        {
            get { return optionsSummary; }
            set
            {
                if (value == optionsSummary)
                    return;

                optionsSummary = value;

                base.OnPropertyChanged(() => OptionsSummary);
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

        private string undoFolder;
        public string UndoFolder
        {
            get { return undoFolder; }
            set
            {
                if (value == undoFolder)
                    return;

                undoFolder = value;

                base.OnPropertyChanged(() => UndoFolder);
            }
        }


        private bool otherActionsMenuOpen;
        public bool OtherActionsMenuOpen
        {
            get { return otherActionsMenuOpen; }
            set
            {
                if (value == otherActionsMenuOpen)
                    return;

                otherActionsMenuOpen = value;

                base.OnPropertyChanged(() => OtherActionsMenuOpen);
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

        public bool IsOperationInProgress
        {
            get { return CurrentGrepOperation != GrepOperation.None; }            
        }

        public StickyWindow StickyWindow
        {
            get 
            {
                return stickyWindow; 
            }
            set
            {
                stickyWindow = value;
            }
        }

        #endregion

        #region Presentation Properties
        RelayCommand _undoCommand;
        /// <summary>
        /// Returns an undo command
        /// </summary>
        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand == null)
                {
                    _undoCommand = new RelayCommand(
                        param => this.undo(),
                        param => CanUndo
                        );
                }
                return _undoCommand;
            }
        }

        RelayCommand _optionsCommand;
        /// <summary>
        /// Returns an options command
        /// </summary>
        public ICommand OptionsCommand
        {
            get
            {
                if (_optionsCommand == null)
                {
                    _optionsCommand = new RelayCommand(
                        param => this.showOptions()
                        );
                }
                return _optionsCommand;
            }
        }
        RelayCommand _helpCommand;
        /// <summary>
        /// Returns a help command
        /// </summary>
        public ICommand HelpCommand
        {
            get
            {
                if (_helpCommand == null)
                {
                    _helpCommand = new RelayCommand(
                        param => this.showHelp()
                        );
                }
                return _helpCommand;
            }
        }
        RelayCommand _aboutCommand;
        /// <summary>
        /// Returns an about command
        /// </summary>
        public ICommand AboutCommand
        {
            get
            {
                if (_aboutCommand == null)
                {
                    _aboutCommand = new RelayCommand(
                        param => this.showAbout()
                        );
                }
                return _aboutCommand;
            }
        }
        RelayCommand _browseCommand;
        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BrowseCommand
        {
            get
            {
                if (_browseCommand == null)
                {
                    _browseCommand = new RelayCommand(
                        param => this.browse()
                        );
                }
                return _browseCommand;
            }
        }
        RelayCommand _searchCommand;
        /// <summary>
        /// Returns a command that starts a search.
        /// </summary>
        public ICommand SearchCommand
        {
            get
            {
                if (_searchCommand == null)
                {
                    _searchCommand = new RelayCommand(
                        param => this.search(),
                        param => this.CanSearch
                        );
                }
                return _searchCommand;
            }
        }
        RelayCommand _replaceCommand;
        /// <summary>
        /// Returns a command that starts a search in results.
        /// </summary>
        public ICommand ReplaceCommand
        {
            get
            {
                if (_replaceCommand == null)
                {
                    _replaceCommand = new RelayCommand(
                        param => this.replace(),
                        param => this.CanReplace
                        );
                }
                return _replaceCommand;
            }
        }
        RelayCommand _otherActionsCommand;
        /// <summary>
        /// Returns a command that opens other actions
        /// </summary>
        public ICommand OtherActionsCommand
        {
            get
            {
                if (_otherActionsCommand == null)
                {
                    _otherActionsCommand = new RelayCommand(
                        param => { OtherActionsMenuOpen = true; },
                        param => this.CanSearchInResults
                        );
                }
                return _otherActionsCommand;
            }
        }
        RelayCommand _copyFilesCommand;
        /// <summary>
        /// Returns a command that copies files
        /// </summary>
        public ICommand CopyFilesCommand
        {
            get
            {
                if (_copyFilesCommand == null)
                {
                    _copyFilesCommand = new RelayCommand(
                        param => this.copyFiles()
                        );
                }
                return _copyFilesCommand;
            }
        }
        RelayCommand _moveFilesCommand;
        /// <summary>
        /// Returns a command that moves files
        /// </summary>
        public ICommand MoveFilesCommand
        {
            get
            {
                if (_moveFilesCommand == null)
                {
                    _moveFilesCommand = new RelayCommand(
                        param => this.moveFiles()
                        );
                }
                return _moveFilesCommand;
            }
        }
        RelayCommand _deleteFilesCommand;
        /// <summary>
        /// Returns a command that deletes files
        /// </summary>
        public ICommand DeleteFilesCommand
        {
            get
            {
                if (_deleteFilesCommand == null)
                {
                    _deleteFilesCommand = new RelayCommand(
                        param => this.deleteFiles()
                        );
                }
                return _deleteFilesCommand;
            }
        }
        RelayCommand _copyToClipboardCommand;
        /// <summary>
        /// Returns a command that copies content to clipboard
        /// </summary>
        public ICommand CopyToClipboardCommand
        {
            get
            {
                if (_copyToClipboardCommand == null)
                {
                    _copyToClipboardCommand = new RelayCommand(
                        param => this.copyToClipboard()
                        );
                }
                return _copyToClipboardCommand;
            }
        }
        RelayCommand _saveAsCsvCommand;
        /// <summary>
        /// Returns a command that copies content to clipboard
        /// </summary>
        public ICommand SaveAsCsvCommand
        {
            get
            {
                if (_saveAsCsvCommand == null)
                {
                    _saveAsCsvCommand = new RelayCommand(
                        param => this.saveAsCsv()
                        );
                }
                return _saveAsCsvCommand;
            }
        }
        RelayCommand _cancelCommand;
        /// <summary>
        /// Returns a command that cancels search
        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                {
                    _cancelCommand = new RelayCommand(
                        param => this.cancel(),
                        param => CanCancel
                        );
                }
                return _cancelCommand;
            }
        }
        RelayCommand _testCommand;
        /// <summary>
        /// Returns a command that opens test view
        /// </summary>
        public ICommand TestCommand
        {
            get
            {
                if (_testCommand == null)
                {
                    _testCommand = new RelayCommand(
                        param => this.test()
                        );
                }
                return _testCommand;
            }
        }
        RelayCommand _bookmarkAddCommand;
        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BookmarkAddCommand
        {
            get
            {
                if (_bookmarkAddCommand == null)
                {
                    _bookmarkAddCommand = new RelayCommand(
                        param => this.bookmarkAdd()
                        );
                }
                return _bookmarkAddCommand;
            }
        }
        RelayCommand _bookmarkOpenCommand;
        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BookmarkOpenCommand
        {
            get
            {
                if (_bookmarkOpenCommand == null)
                {
                    _bookmarkOpenCommand = new RelayCommand(
                        param => this.bookmarkOpen()
                        );
                }
                return _bookmarkOpenCommand;
            }
        }
        #endregion

        #region Public Methods
        public virtual void UpdateState(string name)
        {
            List<string> tempList = null;
            switch (name)
            {
                case "Initial":
                case "Multiline":
                case "Singleline":
                case "WholeWord":
                case "CaseSensitive":
                    tempList = new List<string>();
                    if (CaseSensitive)
                        tempList.Add("Case sensitive");
                    if (Multiline)
                        tempList.Add("Multiline");
                    if (WholeWord)
                        tempList.Add("Whole word");
                    if (Singleline)
                        tempList.Add("Match dot as new line");
                    OptionsSummary = "[";
                    if (tempList.Count == 0)
                    {
                        OptionsSummary += "None";
                    }
                    else
                    {
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            OptionsSummary += tempList[i];
                            if (i < tempList.Count - 1)
                                OptionsSummary += ", ";
                        }
                    }
                    OptionsSummary += "]";

                    if (Multiline)
                        TextBoxStyle = "{StaticResource ExpandedTextbox}";
                    else
                        TextBoxStyle = "";

                    CanReplace = false;

                    break;
                case "UseFileSizeFilter":
                    if (UseFileSizeFilter == FileSizeFilter.Yes)
                    {
                        IsSizeFilterSet = true;
                    }
                    else
                    {
                        IsSizeFilterSet = false;
                    }
                    break;
                case "FileFilters":
                    // Set all properties to correspond to ON value
                    if (FileFilters)
                    {
                        UseFileSizeFilter = FileSizeFilter.No;
                        IncludeBinary = true;
                        IncludeHidden = true;
                        IncludeSubfolder = true;
                        FilePattern = "*";
                        FilePatternIgnore = "";
                        TypeOfFileSearch = FileSearchType.Asterisk;
                        CodePage = 0;
                    }
                    break;
            }

            if (name == "FileFilters" || name == "FilePattern" || name == "IncludeSubfolder" ||
                name == "IncludeHidden" || name == "IncludeBinary" || name == "UseFileSizeFilter")
            {
                if (FileFilters)
                    FileFiltersSummary = "[All files]";
                else
                {
                    tempList = new List<string>();
                    if (FilePattern != "*.*")
                        tempList.Add(FilePattern);
                    if (!IncludeSubfolder)
                        tempList.Add("No subfolders");
                    if (!IncludeHidden)
                        tempList.Add("No hidden");
                    if (!IncludeBinary)
                        tempList.Add("No binary");
                    if (UseFileSizeFilter == FileSizeFilter.Yes)
                        tempList.Add("Size");
                    FileFiltersSummary = "[";
                    if (tempList.Count == 0)
                    {
                        FileFiltersSummary += "Off";
                    }
                    else
                    {
                        for (int i = 0; i < tempList.Count; i++)
                        {
                            FileFiltersSummary += tempList[i];
                            if (i < tempList.Count - 1)
                                FileFiltersSummary += ", ";
                        }
                    }

                    FileFiltersSummary += "]";
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
                    WindowTitle = string.Format("{0} in \"{1}\" - dnGREP", (SearchFor == null ? "Empty" : SearchFor.Replace('\n', ' ').Replace('\r', ' ')), FileOrFolderPath);
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
            if (name == "FileOrFolderPath" || name == "CurrentGrepOperation" || name == "SearchFor")
            {
                if (Utils.IsPathValid(FileOrFolderPath) && CurrentGrepOperation == GrepOperation.None &&
                    (!string.IsNullOrEmpty(SearchFor) || settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern)))
                {
                    CanSearch = true;
                }
                else
                {
                    CanSearch = false;
                }
                // Refersh buttons
                CommandManager.InvalidateRequerySuggested();
            }

            //Set all files if FileOrFolderPath is a file
            if (name == "FileOrFolderPath")
            {
                if (System.IO.File.Exists(FileOrFolderPath))
                    FileFilters = true;
            }

            //btnSearch.ShowAdvance
            if (name == "CurrentGrepOperation" || name == "Initial")
            {
                if (searchResults.Count > 0)
                {
                    //TODO
                    CanSearchInResults = true;
                    SearchButtonMode = "Split";
                }
                else
                {
                    //TODO
                    CanSearchInResults = false;
                    SearchButtonMode = "Button";
                }
            }

            //searchResults
            searchResults.FolderPath = FileOrFolderPath;

            // btnReplace
            if (name == "FileOrFolderPath" || name == "FilesFound" || name == "CurrentGrepOperation" || name == "SearchFor")
            {
                if (Utils.IsPathValid(FileOrFolderPath) && FilesFound && CurrentGrepOperation == GrepOperation.None &&
                    !string.IsNullOrEmpty(SearchFor))
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

            if (IsProperty(() => PreviewFileContent, name))
            {
                if (preview != null)
                    preview.Hide();
            }

            if (IsProperty(() => SearchFor, name) || IsProperty(() => ReplaceWith, name) || IsProperty(() => FilePattern, name))
            {
                if (BookmarkLibrary.Instance.Bookmarks.Contains(new Bookmark(SearchFor, ReplaceWith, FilePattern, "")))
                    IsBookmarked = true;
                else
                    IsBookmarked = false;
            }
        }

        public void LoadSettings()
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

            string _fileOrFolderPath = settings.Get<string>(GrepSettings.Key.SearchFolder);
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
            settings[GrepSettings.Key.SearchFolder] = _fileOrFolderPath;

            FileOrFolderPath = settings.Get<string>(GrepSettings.Key.SearchFolder);
            SearchFor = settings.Get<string>(GrepSettings.Key.SearchFor);
            ReplaceWith = settings.Get<string>(GrepSettings.Key.ReplaceWith);
            IncludeHidden = settings.Get<bool>(GrepSettings.Key.IncludeHidden);
            IncludeBinary = settings.Get<bool>(GrepSettings.Key.IncludeBinary);
            IncludeSubfolder = settings.Get<bool>(GrepSettings.Key.IncludeSubfolder);
            TypeOfSearch = settings.Get<SearchType>(GrepSettings.Key.TypeOfSearch);
            TypeOfFileSearch = settings.Get<FileSearchType>(GrepSettings.Key.TypeOfFileSearch);
            FilePattern = settings.Get<string>(GrepSettings.Key.FilePattern);
            FilePatternIgnore = settings.Get<string>(GrepSettings.Key.FilePatternIgnore);
            UseFileSizeFilter = settings.Get<FileSizeFilter>(GrepSettings.Key.UseFileSizeFilter);
            CaseSensitive = settings.Get<bool>(GrepSettings.Key.CaseSensitive);
            Multiline = settings.Get<bool>(GrepSettings.Key.Multiline);
            Singleline = settings.Get<bool>(GrepSettings.Key.Singleline);
            WholeWord = settings.Get<bool>(GrepSettings.Key.WholeWord);
            SizeFrom = settings.Get<int>(GrepSettings.Key.SizeFrom);
            SizeTo = settings.Get<int>(GrepSettings.Key.SizeTo);
            TextFormatting = settings.Get<TextFormattingMode>(GrepSettings.Key.TextFormatting);
            IsOptionsExpanded = settings.Get<bool>(GrepSettings.Key.IsOptionsExpanded);
            IsFiltersExpanded = settings.Get<bool>(GrepSettings.Key.IsFiltersExpanded);
            FileFilters = settings.Get<bool>(GrepSettings.Key.FileFilters);
            PreviewFileContent = settings.Get<bool>(GrepSettings.Key.PreviewFileContent);
        }

        public void SaveSettings()
        {
            copyBookmarksToSettings();
            if (preview != null)
            {
                settings.Set<System.Drawing.Rectangle>(GrepSettings.Key.PreviewWindowSize, preview.StickyWindow.OriginalForm.Bounds);
                settings.Set<StickyWindow.StickDir>(GrepSettings.Key.PreviewWindowPosition, preview.StickyWindow.IsStuckTo(stickyWindow.OriginalForm, true));
                preview.ForceClose();
            }
            settings.Set<string>(GrepSettings.Key.SearchFolder, FileOrFolderPath);
            settings.Set<string>(GrepSettings.Key.SearchFor, SearchFor);
            settings.Set<string>(GrepSettings.Key.ReplaceWith, ReplaceWith);
            settings.Set<bool>(GrepSettings.Key.IncludeHidden, IncludeHidden);
            settings.Set<bool>(GrepSettings.Key.IncludeBinary, IncludeBinary);
            settings.Set<bool>(GrepSettings.Key.IncludeSubfolder, IncludeSubfolder);
            settings.Set<SearchType>(GrepSettings.Key.TypeOfSearch, TypeOfSearch);
            settings.Set<FileSearchType>(GrepSettings.Key.TypeOfFileSearch, TypeOfFileSearch);
            settings.Set<string>(GrepSettings.Key.FilePattern, FilePattern);
            settings.Set<string>(GrepSettings.Key.FilePatternIgnore, FilePatternIgnore);
            settings.Set<FileSizeFilter>(GrepSettings.Key.UseFileSizeFilter, UseFileSizeFilter);
            settings.Set<bool>(GrepSettings.Key.CaseSensitive, CaseSensitive);
            settings.Set<bool>(GrepSettings.Key.Multiline, Multiline);
            settings.Set<bool>(GrepSettings.Key.Singleline, Singleline);
            settings.Set<bool>(GrepSettings.Key.WholeWord, WholeWord);
            settings.Set<int>(GrepSettings.Key.SizeFrom, SizeFrom);
            settings.Set<int>(GrepSettings.Key.SizeTo, SizeTo);
            settings.Set<TextFormattingMode>(GrepSettings.Key.TextFormatting, TextFormatting);
            settings.Set<bool>(GrepSettings.Key.IsOptionsExpanded, IsOptionsExpanded);
            settings.Set<bool>(GrepSettings.Key.IsFiltersExpanded, IsFiltersExpanded);
            settings.Set<bool>(GrepSettings.Key.FileFilters, FileFilters);
            settings.Set<bool>(GrepSettings.Key.PreviewFileContent, PreviewFileContent);
        }

        public void OpenFile(FormattedGrepLine selectedNode)
        {
            try
            {
                // Line was selected
                int lineNumber = selectedNode.GrepLine.LineNumber;

                FormattedGrepResult result = selectedNode.Parent;
                OpenFileArgs fileArg = new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, settings.Get<bool>(GrepSettings.Key.UseCustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                dnGREP.Engines.GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, new GrepEngineInitParams(false, 0, 0, 0.5)).OpenFile(fileArg);
                if (fileArg.UseBaseEngine)
                    Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, settings.Get<bool>(GrepSettings.Key.UseCustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, "Failed to open file.", ex);
                if (settings.Get<bool>(GrepSettings.Key.UseCustomEditor))
                    MessageBox.Show("There was an error opening file by custom editor. \nCheck editor path via \"Options..\".", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("There was an error opening file. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenFile(FormattedGrepResult result)
        {
            try
            {
                // Line was selected
                int lineNumber = 0;
                OpenFileArgs fileArg = new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, settings.Get<bool>(GrepSettings.Key.UseCustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                dnGREP.Engines.GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, new GrepEngineInitParams(false, 0, 0, 0.5)).OpenFile(fileArg);
                if (fileArg.UseBaseEngine)
                    Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, settings.Get<bool>(GrepSettings.Key.UseCustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, "Failed to open file.", ex);
                if (settings.Get<bool>(GrepSettings.Key.UseCustomEditor))
                    MessageBox.Show("There was an error opening file by custom editor. \nCheck editor path via \"Options..\".", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("There was an error opening file. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PreviewFile(FormattedGrepLine formattedGrepLine, RectangleF parentWindow)
        {
            if (PreviewFileContent)
            {
                int lineNumber = formattedGrepLine.GrepLine.LineNumber;
                FormattedGrepResult result = formattedGrepLine.Parent;
                previewFile(result.GrepResult.FileNameReal, result.GrepResult, lineNumber, parentWindow);
            }
        }

        public void PreviewFile(FormattedGrepResult formattedGrepResult, RectangleF parentWindow)
        {
            if (PreviewFileContent)
            {                
                previewFile(formattedGrepResult.GrepResult.FileNameReal, formattedGrepResult.GrepResult, 0, parentWindow);
            }            
        }

        public void ActivatePreviewWindow()
        {
            if (preview != null && preview.IsVisible)
            {
                preview.Topmost = true;  // important
                preview.Topmost = false; // important
                preview.Focus();         // important
            }
        }

        public void ChangePreviewWindowState(System.Windows.WindowState state)
        {
            if (preview != null && preview.IsVisible)
            {
                if (state != System.Windows.WindowState.Maximized)
                    preview.WindowState = state;
            }
        }

        public void SetCodeSnippets(ICollection<FormattedGrepResult> results)
        {
            foreach (var result in results)
            {
                StringBuilder blocks = new StringBuilder();
                List<int> lines = new List<int>();

                foreach (var block in Utils.GetSnippets(result.GrepResult,
                        settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                        settings.Get<int>(GrepSettings.Key.ContextLinesAfter)))
                {
                    blocks.AppendLine(block.Text);
                    blocks.AppendLine("...");
                    lines.AddRange(Utils.GetIntArray(block.FirstLineNumber, block.LineCount));
                    lines.Add(-1);
                }
                var previewViewModel = new SyntaxHighlighterViewModel();
                previewViewModel.Text = blocks.ToString().TrimEndOfLine();
                previewViewModel.LineNumbers = lines.ToArray();
                previewViewModel.SearchResult = result.GrepResult;
                previewViewModel.FileName = result.GrepResult.FileNameDisplayed;
                ContentPreviewModel = previewViewModel;
            }           
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
            if (workerSearchReplace.IsBusy)
                workerSearchReplace.CancelAsync();
            SaveSettings();
            settings.Save();
        }

        private void doSearchReplace(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!workerSearchReplace.CancellationPending)
                {
                    timer = DateTime.Now;
                    Dictionary<string, object> workerParams = (Dictionary<string, object>)e.Argument;
                    //TODO: Check if this is needed
                    MainViewModel param = (MainViewModel)workerParams["State"];
                    if (param.CurrentGrepOperation == GrepOperation.Search || param.CurrentGrepOperation == GrepOperation.SearchInResults)
                    {
                        int sizeFrom = 0;
                        int sizeTo = 0;
                        if (param.UseFileSizeFilter == FileSizeFilter.Yes)
                        {
                            sizeFrom = param.SizeFrom;
                            sizeTo = param.SizeTo;
                        }

                        string filePatternInclude = "*.*";
                        if (param.TypeOfFileSearch == FileSearchType.Regex)
                            filePatternInclude = ".*";

                        if (!string.IsNullOrEmpty(param.FilePattern))
                            filePatternInclude = param.FilePattern;

                        if (param.TypeOfFileSearch == FileSearchType.Asterisk)
                            filePatternInclude = filePatternInclude.Replace("\\", "");

                        string filePatternExclude = "";
                        if (!string.IsNullOrEmpty(param.FilePatternIgnore))
                            filePatternExclude = param.FilePatternIgnore;

                        if (param.TypeOfFileSearch == FileSearchType.Asterisk)
                            filePatternExclude = filePatternExclude.Replace("\\", "");

                        IEnumerable<string> files;

                        Utils.CancelSearch = false;

                        if (param.CurrentGrepOperation == GrepOperation.SearchInResults)
                        {
                            files = (List<string>)workerParams["Files"];
                        }
                        else
                        {
                            files = Utils.GetFileListEx(FileOrFolderPath, filePatternInclude, filePatternExclude, param.TypeOfFileSearch == FileSearchType.Regex, param.IncludeSubfolder,
                                param.IncludeHidden, param.IncludeBinary, sizeFrom, sizeTo);
                        }

                        if (Utils.CancelSearch)
                        {
                            e.Result = null;
                            return;
                        }

                        if (param.TypeOfSearch == SearchType.Regex)
                        {
                            try
                            {
                                Regex pattern = new Regex(param.SearchFor);
                            }
                            catch (ArgumentException regException)
                            {
                                MessageBox.Show("Incorrect pattern: " + regException.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                e.Result = null;
                                return;
                            }
                        }

                        GrepCore grep = new GrepCore();
                        grep.SearchParams.FuzzyMatchThreshold = settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold);
                        grep.SearchParams.LinesBefore = settings.Get<int>(GrepSettings.Key.ContextLinesBefore);
                        grep.SearchParams.LinesAfter = settings.Get<int>(GrepSettings.Key.ContextLinesAfter);
                        grep.SearchParams.ShowLinesInContext = settings.Get<bool>(GrepSettings.Key.ShowLinesInContext);

                        GrepSearchOption searchOptions = GrepSearchOption.None;
                        if (Multiline)
                            searchOptions |= GrepSearchOption.Multiline;
                        if (CaseSensitive)
                            searchOptions |= GrepSearchOption.CaseSensitive;
                        if (Singleline)
                            searchOptions |= GrepSearchOption.SingleLine;
                        if (WholeWord)
                            searchOptions |= GrepSearchOption.WholeWord;

                        grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
                        e.Result = grep.Search(files, param.TypeOfSearch, param.SearchFor, searchOptions, param.CodePage);
                        grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
                    }
                    else
                    {
                        GrepCore grep = new GrepCore();
                        grep.SearchParams.FuzzyMatchThreshold = settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold);
                        grep.SearchParams.LinesBefore = settings.Get<int>(GrepSettings.Key.ContextLinesBefore);
                        grep.SearchParams.LinesAfter = settings.Get<int>(GrepSettings.Key.ContextLinesAfter);
                        grep.SearchParams.ShowLinesInContext = settings.Get<bool>(GrepSettings.Key.ShowLinesInContext);

                        GrepSearchOption searchOptions = GrepSearchOption.None;
                        if (Multiline)
                            searchOptions |= GrepSearchOption.Multiline;
                        if (CaseSensitive)
                            searchOptions |= GrepSearchOption.CaseSensitive;
                        if (Singleline)
                            searchOptions |= GrepSearchOption.SingleLine;
                        if (WholeWord)
                            searchOptions |= GrepSearchOption.WholeWord;

                        grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
                        string[] files = ((List<string>)workerParams["Files"]).ToArray();
                        e.Result = grep.Replace(files, param.TypeOfSearch, Utils.GetBaseFolder(param.FileOrFolderPath), param.SearchFor, param.ReplaceWith, searchOptions, param.CodePage);

                        grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex.Message, ex);
                bool isSearch = true;
                if (e.Argument is MainViewModel)
                {
                    MainViewModel param = (MainViewModel)e.Argument;
                    if (param.CurrentGrepOperation == GrepOperation.Search || param.CurrentGrepOperation == GrepOperation.SearchInResults)
                        isSearch = true;
                    else
                        isSearch = false;
                }
                if (isSearch)
                    MessageBox.Show("Search failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void searchProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (!Utils.CancelSearch)
                {
                    GrepCore.ProgressStatus progress = (GrepCore.ProgressStatus)e.UserState;
                    string result = string.Empty;
                    if (progress.SearchResults != null)
                    {
                        SearchResults.AddRange(progress.SearchResults);
                        result = string.Format("Searched {0} files. Found {1} matching files.", progress.ProcessedFiles, SearchResults.Count);
                    }
                    else
                    {
                        result = string.Format("Searched {0} files.", progress.ProcessedFiles);
                    }

                    StatusMessage = result;
                }
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex.Message, ex);
                MessageBox.Show("Search or replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void searchComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (CurrentGrepOperation == GrepOperation.Search || CurrentGrepOperation == GrepOperation.SearchInResults)
                {
                    List<GrepSearchResult> results = new List<GrepSearchResult>();
                    if (e.Result == null)
                    {
                        StatusMessage = "Search Canceled or Failed";
                    }
                    else if (!e.Cancelled)
                    {
                        TimeSpan duration = DateTime.Now.Subtract(timer);
                        results = (List<GrepSearchResult>)e.Result;
                        StatusMessage = "Search Complete - " + results.Count + " files found in " + duration.TotalMilliseconds + "ms.";
                    }
                    else
                    {
                        StatusMessage = "Search Canceled";
                    }
                    if (SearchResults.Count > 0)
                        FilesFound = true;
                    CurrentGrepOperation = GrepOperation.None;
                    base.OnPropertyChanged(() => CurrentGrepOperation);
                    CanSearch = true;
                }
                else if (CurrentGrepOperation == GrepOperation.Replace)
                {
                    if (!e.Cancelled)
                    {
                        if (e.Result == null || ((int)e.Result) == -1)
                        {
                            StatusMessage = "Replace Failed.";
                            MessageBox.Show("Replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            StatusMessage = "Replace Complete - " + (int)e.Result + " files replaced.";
                            CanUndo = true;
                        }
                    }
                    else
                    {
                        StatusMessage = "Replace Canceled";
                    }
                    CurrentGrepOperation = GrepOperation.None;
                    base.OnPropertyChanged(() => CurrentGrepOperation);
                    CanSearch = true;
                    SearchResults.Clear();
                }
                
                string outdatedEngines = dnGREP.Engines.GrepEngineFactory.GetListOfFailedEngines();
                if (!string.IsNullOrEmpty(outdatedEngines))
                {
                    MessageBox.Show("The following plugins failed to load:\n\n" + outdatedEngines + "\n\nDefault engine was used instead.", "Plugin Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex.Message, ex);
                MessageBox.Show("Search or replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Utils.CancelSearch = false;
            }
        }

        void grep_ProcessedFile(object sender, GrepCore.ProgressStatus progress)
        {
            workerSearchReplace.ReportProgress((int)progress.ProcessedFiles, progress);
        }

        private void browse()
        {
            fileFolderDialog.Dialog.Multiselect = true;
            fileFolderDialog.SelectedPath = Utils.GetBaseFolder(FileOrFolderPath);
            if (FileOrFolderPath == "")
            {
                string clipboard = Clipboard.GetText();
                try
                {
                    if (System.IO.Path.IsPathRooted(clipboard))
                        fileFolderDialog.SelectedPath = clipboard;
                }
                catch
                {
                    // Ignore
                }
            }
            if (fileFolderDialog.ShowDialog() == true)
            {
                if (fileFolderDialog.SelectedPaths != null)
                    FileOrFolderPath = fileFolderDialog.SelectedPaths;
                else
                    FileOrFolderPath = fileFolderDialog.SelectedPath;
            }
        }

        private void search()
        {
            if (CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
            {
                if (SearchInResultsContent && CanSearchInResults)
                    CurrentGrepOperation = GrepOperation.SearchInResults;
                else
                    CurrentGrepOperation = GrepOperation.Search;
                StatusMessage = "Searching...";
                if (preview != null)
                    preview.ResetTextEditor();
                Dictionary<string, object> workerParames = new Dictionary<string, object>();
                if (SearchInResultsContent && CanSearchInResults)
                {
                    List<string> foundFiles = new List<string>();
                    foreach (FormattedGrepResult n in SearchResults) foundFiles.Add(n.GrepResult.FileNameReal);
                    workerParames["Files"] = foundFiles;
                }
                SearchResults.Clear();
                workerParames["State"] = this;
                workerSearchReplace.RunWorkerAsync(workerParames);
                updateBookmarks();
            }
        }

        private void replace()
        {
            if (CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
            {
                if (string.IsNullOrEmpty(ReplaceWith))
                {
                    if (MessageBox.Show("Are you sure you want to replace search pattern with empty string?", "Replace", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;
                }
                List<string> roFiles = Utils.GetReadOnlyFiles(SearchResults.GetList());
                if (roFiles.Count > 0)
                {
                    StringBuilder sb = new StringBuilder("Some of the files can not be modified. If you continue, these files will be skipped.\nWould you like to continue?\n\n");
                    foreach (string fileName in roFiles)
                    {
                        sb.AppendLine(" - " + new FileInfo(fileName).Name);
                    }
                    if (MessageBox.Show(sb.ToString(), "Replace", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;
                }
                StatusMessage = "Replacing...";
                if (preview != null)
                    preview.ResetTextEditor();
                CurrentGrepOperation = GrepOperation.Replace;
                CanUndo = false;
                UndoFolder = Utils.GetBaseFolder(FileOrFolderPath);
                List<string> foundFiles = new List<string>();
                foreach (FormattedGrepResult n in SearchResults)
                {
                    if (!n.GrepResult.ReadOnly)
                        foundFiles.Add(n.GrepResult.FileNameReal);
                }
                Dictionary<string, object> workerParames = new Dictionary<string, object>();
                workerParames["State"] = this;
                workerParames["Files"] = foundFiles;
                SearchResults.Clear();
                workerSearchReplace.RunWorkerAsync(workerParames);
                updateBookmarks();
            }
        }

        private void updateBookmarks()
        {
            // Update bookmarks
            if (!FastSearchBookmarks.Contains(SearchFor))
            {
                FastSearchBookmarks.Insert(0, SearchFor);
            }
            if (!FastFileMatchBookmarks.Contains(FilePattern))
            {
                FastFileMatchBookmarks.Insert(0, FilePattern);
            }
            if (!FastFileNotMatchBookmarks.Contains(FilePatternIgnore))
            {
                FastFileNotMatchBookmarks.Insert(0, FilePatternIgnore);
            }
            if (!FastPathBookmarks.Contains(FileOrFolderPath))
            {
                FastPathBookmarks.Insert(0, FileOrFolderPath);
            }
        }

        private void cancel()
        {
            if (CurrentGrepOperation != GrepOperation.None)
            {
                Utils.CancelSearch = true;
            }
        }

        private void undo()
        {
            if (CanUndo)
            {
                MessageBoxResult response = MessageBox.Show("Undo will revert modified file(s) back to their original state. Any changes made to the file(s) after the replace will be overwritten. Are you sure you want to procede?",
                    "Undo", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                if (response == MessageBoxResult.Yes)
                {
                    GrepCore core = new GrepCore();
                    bool result = core.Undo(UndoFolder);
                    if (result)
                    {
                        MessageBox.Show("Files have been successfully reverted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        Utils.DeleteTempFolder();
                    }
                    else
                    {
                        MessageBox.Show("There was an error reverting files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CanUndo = false;
                }
            }
        }

        private void showOptions()
        {
            string fileOrFolderPath = FileOrFolderPath;
            string searchFor = SearchFor;
            string replaceWith = ReplaceWith;
            string filePattern = FilePattern;
            string filePatternIgnore = FilePatternIgnore;

            copyBookmarksToSettings();
            OptionsView optionsForm = new OptionsView();
            OptionsViewModel optionsViewModel = new OptionsViewModel();
            // When the ViewModel asks to be closed, 
            // close the window.
            EventHandler handler = null;
            handler = delegate
            {
                optionsViewModel.RequestClose -= handler;
                optionsForm.Close();
            };
            optionsViewModel.RequestClose += handler;
            optionsForm.DataContext = optionsViewModel;
            try
            {
                optionsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error saving options.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.LogException(LogLevel.Error, "Error saving options", ex);
            }
            LoadSettings();
            FileOrFolderPath = fileOrFolderPath;
            SearchFor = searchFor;
            ReplaceWith = replaceWith;
            FilePattern = filePattern;
            FilePatternIgnore = filePatternIgnore;
        }

        private void showHelp()
        {
            //TODO: Fix
            //ApplicationCommands.Help.Execute(null, helpToolStripMenuItem);
        }

        private void showAbout()
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void bookmarkAdd()
        {
            Bookmark newBookmark = new Bookmark(SearchFor, ReplaceWith, FilePattern, "");
            if (IsBookmarked)
            {
                BookmarkDetails bookmarkEditForm = new BookmarkDetails(CreateOrEdit.Create);
                bookmarkEditForm.Bookmark = newBookmark;
                if (bookmarkEditForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!BookmarkLibrary.Instance.Bookmarks.Contains(newBookmark))
                    {
                        BookmarkLibrary.Instance.Bookmarks.Add(newBookmark);
                        BookmarkLibrary.Save();
                    }
                }
                if (BookmarkLibrary.Instance.Bookmarks.Contains(newBookmark))
                    IsBookmarked = true;
                else
                    IsBookmarked = false;
            }
            else
            {
                if (BookmarkLibrary.Instance.Bookmarks.Contains(newBookmark))
                    BookmarkLibrary.Instance.Bookmarks.Remove(newBookmark);
                IsBookmarked = false;
            }
        }

        private void bookmarkOpen()
        {
            try
            {
                bookmarkForm = new BookmarksForm();
                bookmarkForm.PropertyChanged += new PropertyChangedEventHandler(bookmarkForm_PropertyChanged);
                bookmarkForm.ShowDialog();
            }
            finally
            {
                bookmarkForm.PropertyChanged -= new PropertyChangedEventHandler(bookmarkForm_PropertyChanged);
            }
        }

        private void copyFiles()
        {
            if (FilesFound)
            {
                if (fileFolderDialog.ShowDialog() == true)
                {
                    try
                    {
                        if (!Utils.CanCopyFiles(SearchResults.GetList(), Utils.GetBaseFolder(fileFolderDialog.SelectedPath)))
                        {
                            MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        Utils.CopyFiles(SearchResults.GetList(), Utils.GetBaseFolder(FileOrFolderPath), Utils.GetBaseFolder(fileFolderDialog.SelectedPath), true);
                        MessageBox.Show("Files have been successfully copied.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("There was an error copying files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.LogException(LogLevel.Error, "Error copying files", ex);
                    }
                    CanUndo = false;
                }
            }
        }

        private void moveFiles()
        {
            if (FilesFound)
            {
                if (fileFolderDialog.ShowDialog() == true)
                {
                    try
                    {
                        if (!Utils.CanCopyFiles(SearchResults.GetList(), Utils.GetBaseFolder(fileFolderDialog.SelectedPath)))
                        {
                            MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.",
                                "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        Utils.CopyFiles(SearchResults.GetList(), Utils.GetBaseFolder(FileOrFolderPath), Utils.GetBaseFolder(fileFolderDialog.SelectedPath), true);
                        Utils.DeleteFiles(SearchResults.GetList());
                        MessageBox.Show("Files have been successfully moved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(LogLevel.Error, "Error moving files", ex);
                        MessageBox.Show("There was an error moving files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CanUndo = false;
                    SearchResults.Clear();
                    FilesFound = false;
                }
            }
        }

        private void deleteFiles()
        {
            if (FilesFound)
            {
                try
                {
                    if (MessageBox.Show("Attention, you are about to delete files found during search.\nAre you sure you want to procede?", "Attention", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    Utils.DeleteFiles(SearchResults.GetList());
                    MessageBox.Show("Files have been successfully deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was an error deleting files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    logger.LogException(LogLevel.Error, "Error deleting files", ex);
                }
                CanUndo = false;
                SearchResults.Clear();
                FilesFound = false;
            }
        }

        private void copyToClipboard()
        {
            StringBuilder sb = new StringBuilder();
            foreach (GrepSearchResult result in SearchResults.GetList())
            {
                sb.AppendLine(result.FileNameReal);
            }
            Clipboard.SetText(sb.ToString());
        }

        private void saveAsCsv()
        {
            if (FilesFound)
            {
                saveFileDialog.InitialDirectory = Utils.GetBaseFolder(FileOrFolderPath);
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        Utils.SaveResultsAsCSV(SearchResults.GetList(), saveFileDialog.FileName);
                        MessageBox.Show("CSV file has been successfully created.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("There was an error creating a CSV file. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.LogException(LogLevel.Error, "Error creating CSV file", ex);
                    }
                }
            }
        }

        private void test()
        {
            try
            {
                TestPattern testForm = new TestPattern();
                testForm.ShowDialog();
                LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error running regex test. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.LogException(LogLevel.Error, "Error running regex", ex);
            }
        }

        private void checkVersion()
        {
            try
            {
                if (settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking))
                {
                    DateTime lastCheck = settings.Get<DateTime>(GrepSettings.Key.LastCheckedVersion);
                    TimeSpan duration = DateTime.Now.Subtract(lastCheck);
                    if (duration.TotalDays >= settings.Get<int>(GrepSettings.Key.UpdateCheckInterval))
                    {
                        ve.StartWebRequest();
                        settings.Set<DateTime>(GrepSettings.Key.LastCheckedVersion, DateTime.Now);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex.Message, ex);
            }
        }

        void ve_RetrievedVersion(object sender, PublishedVersionExtractor.PackageVersion version)
        {
            try
            {
                if (version.Version != null)
                {
                    string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    if (PublishedVersionExtractor.IsUpdateNeeded(currentVersion, version.Version))
                    {
                        if (MessageBox.Show("New version of dnGREP (" + version.Version + ") is available for download.\nWould you like to download it now?", "New version", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start("http://code.google.com/p/dngrep/");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex.Message, ex);
            }
        }

        private void winFormControlsInit()
        {
            this.workerSearchReplace.WorkerReportsProgress = true;
            this.workerSearchReplace.WorkerSupportsCancellation = true;
            this.workerSearchReplace.DoWork += new System.ComponentModel.DoWorkEventHandler(this.doSearchReplace);
            this.workerSearchReplace.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.searchComplete);
            this.workerSearchReplace.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.searchProgressChanged);
            this.saveFileDialog.Filter = "CSV file|*.csv";
            DiginesisHelpProvider.HelpNamespace = "Doc\\dnGREP.chm";
            DiginesisHelpProvider.ShowHelp = true;
        }

        private void populateEncodings()
        {
            KeyValuePair<string, int> defaultValue = new KeyValuePair<string, int>("Auto detection (default)", -1);

            List<KeyValuePair<string, int>> tempEnc = new List<KeyValuePair<string, int>>();
            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                Encoding e = ei.GetEncoding();
                tempEnc.Add(new KeyValuePair<string, int>(e.EncodingName, e.CodePage));
            }

            tempEnc.Sort(new KeyValueComparer());
            tempEnc.Insert(0, defaultValue);
            encodings = new ObservableCollection<KeyValuePair<string, int>>(tempEnc);
        }

        void bookmarkForm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FilePattern")
                FilePattern = bookmarkForm.FilePattern;
            else if (e.PropertyName == "SearchFor")
                SearchFor = bookmarkForm.SearchFor;
            else if (e.PropertyName == "ReplaceWith")
                ReplaceWith = bookmarkForm.ReplaceWith;
        }

        private void copyBookmarksToSettings()
        {
            //Saving bookmarks
            List<string> fsb = new List<string>();
            for (int i = 0; i < FastSearchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                fsb.Add(FastSearchBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastSearchBookmarks, fsb);
            List<string> frb = new List<string>();
            for (int i = 0; i < FastReplaceBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                frb.Add(FastReplaceBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastReplaceBookmarks, frb);
            List<string> ffmb = new List<string>();
            for (int i = 0; i < FastFileMatchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                ffmb.Add(FastFileMatchBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastFileMatchBookmarks, ffmb);
            List<string> ffnmb = new List<string>();
            for (int i = 0; i < FastFileNotMatchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                ffnmb.Add(FastFileNotMatchBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastFileNotMatchBookmarks, ffnmb);
            List<string> fpb = new List<string>();
            for (int i = 0; i < FastPathBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                fpb.Add(FastPathBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastPathBookmarks, fpb);
        }

        private void previewFile(string filePath, GrepSearchResult result, int line, RectangleF parentWindow)
        {
            if (PreviewFileContent)
            {
                if (previewModel == null)
                {
                    previewModel = new PreviewViewModel();
                }
            
                if (preview == null)
                {
                    preview = new PreviewView();
                    preview.DataContext = previewModel;
                    System.Drawing.Rectangle bounds = settings.Get<System.Drawing.Rectangle>(GrepSettings.Key.PreviewWindowSize);
                    if (bounds.Left == 0 && bounds.Right == 0)
                    {
                        preview.Height = parentWindow.Height;
                        preview.Left = parentWindow.Left + parentWindow.Width;
                        preview.Width = parentWindow.Width;
                        preview.Top = parentWindow.Top;
                    }
                    else
                    {
                        var stickyDir = GrepSettings.Instance.Get<StickyWindow.StickDir>(GrepSettings.Key.PreviewWindowPosition);
                        bounds = StickyWindow.PositionRelativeTo(stickyWindow.OriginalForm, stickyDir, bounds);
                        preview.Height = bounds.Height;
                        preview.Left = bounds.Left;
                        preview.Width = bounds.Width;
                        preview.Top = bounds.Top;
                    }
                }
                previewModel.GrepResult = result;
                previewModel.LineNumber = line;
                previewModel.FilePath = filePath;
            }  
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
