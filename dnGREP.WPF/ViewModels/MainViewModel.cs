using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Engines;
using dnGREP.WPF.MVHelpers;
using dnGREP.WPF.Properties;
using DockFloat;
using Microsoft.Win32;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public class MainViewModel : BaseMainViewModel, IFileDragDropTarget
    {
        public event EventHandler PreviewHide;
        public event EventHandler PreviewShow;

        private Brush highlightForeground;
        private Brush highlightBackground;

        public MainViewModel()
            : base()
        {
            double maxPreviewWidth = Application.Current.MainWindow.Width - DockPanelSplitter.Panel1MinSize;
            double maxPreviewHeight = Application.Current.MainWindow.Height - DockPanelSplitter.Panel1MinSize;

            _previewWindowBounds = LayoutProperties.PreviewBounds;
            _previewWindowState = LayoutProperties.PreviewWindowState;
            _isPreviewDocked = LayoutProperties.PreviewDocked;
            _previewDockedWidth = Math.Min(LayoutProperties.PreviewDockedWidth, maxPreviewWidth);
            if (Enum.TryParse(LayoutProperties.PreviewDockSide, out Dock side))
            {
                _previewDockSide = side;
            }
            _previewDockedHeight = Math.Min(LayoutProperties.PreviewDockedHeight, maxPreviewHeight);
            _isPreviewHidden = LayoutProperties.PreviewHidden;
            _previewAutoPosition = settings.Get<bool>(GrepSettings.Key.PreviewAutoPosition);

            SearchResults.PreviewFileLineRequest += SearchResults_PreviewFileLineRequest;
            SearchResults.PreviewFileRequest += SearchResults_PreviewFileRequest;
            SearchResults.OpenFileLineRequest += SearchResults_OpenFileLineRequest;
            SearchResults.OpenFileRequest += SearchResults_OpenFileRequest;

            CheckVersion();
            ControlsInit();
            PopulateEncodings();

            highlightBackground = Application.Current.Resources["Match.Highlight.Background"] as Brush;
            highlightForeground = Application.Current.Resources["Match.Highlight.Foreground"] as Brush;
            ToggleHighlights();

            AppTheme.Instance.CurrentThemeChanging += (s, e) =>
            {
                Application.Current.Resources.Remove("Match.Highlight.Background");
                Application.Current.Resources.Remove("Match.Highlight.Foreground");
            };

            AppTheme.Instance.CurrentThemeChanged += (s, e) =>
            {
                highlightBackground = Application.Current.Resources["Match.Highlight.Background"] as Brush;
                highlightForeground = Application.Current.Resources["Match.Highlight.Foreground"] as Brush;
                ToggleHighlights();
            };

            idleTimer.Interval = TimeSpan.FromMilliseconds(250);
            idleTimer.Tick += IdleTimer_Tick;
        }

        void SearchResults_OpenFileRequest(object sender, GrepResultEventArgs e)
        {
            OpenFile(e.FormattedGrepResult, e.UseCustomEditor);
        }

        void SearchResults_OpenFileLineRequest(object sender, GrepLineEventArgs e)
        {
            OpenFile(e.FormattedGrepLine, e.UseCustomEditor);
        }

        void SearchResults_PreviewFileRequest(object sender, GrepResultEventArgs e)
        {
            PreviewFile(e.FormattedGrepResult);
        }

        void SearchResults_PreviewFileLineRequest(object sender, GrepLineEventArgs e)
        {
            PreviewFile(e.FormattedGrepLine);
        }

        #region Private Variables and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private DateTime timer = DateTime.Now;
        private readonly FileFolderDialogWin32 fileFolderDialog = new FileFolderDialogWin32();
        private readonly BackgroundWorker workerSearchReplace = new BackgroundWorker();
        private BookmarksWindow bookmarkWindow;
        private readonly HashSet<string> currentSearchFiles = new HashSet<string>();
        private int processedFiles;
        private readonly List<ReplaceDef> undoList = new List<ReplaceDef>();
        private readonly DispatcherTimer idleTimer = new DispatcherTimer(DispatcherPriority.ContextIdle);
        private string latestStatusMessage;

        #endregion

        #region Properties

        public Window ParentWindow { get; set; }

        public PreviewViewModel PreviewModel { get; set; }

        #endregion

        #region Presentation Properties

        private string applicationFontFamily;
        public string ApplicationFontFamily
        {
            get { return applicationFontFamily; }
            set
            {
                if (applicationFontFamily == value)
                    return;

                applicationFontFamily = value;
                base.OnPropertyChanged(() => ApplicationFontFamily);
            }
        }

        private double mainFormfontSize;
        public double MainFormFontSize
        {
            get { return mainFormfontSize; }
            set
            {
                if (mainFormfontSize == value)
                    return;

                mainFormfontSize = value;
                base.OnPropertyChanged(() => MainFormFontSize);
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
                    return Resources.AddSearchPatternToBookmarks;
                else
                    return Resources.ClearBookmark;
            }
        }

        private bool isFolderBookmarked;
        public bool IsFolderBookmarked
        {
            get { return isFolderBookmarked; }
            set
            {
                if (value == isFolderBookmarked)
                    return;

                isFolderBookmarked = value;

                base.OnPropertyChanged(() => IsFolderBookmarked);
                base.OnPropertyChanged(() => IsFolderBookmarkedTooltip);
            }
        }

        public string IsFolderBookmarkedTooltip
        {
            get
            {
                if (!IsFolderBookmarked)
                    return Resources.AssociateBookmarkWithFolder;
                else
                    return Resources.RemoveFolderFromBookmarkAssociation;
            }
        }

        private string _previewTitle = " ";
        public string PreviewTitle
        {
            get { return _previewTitle; }
            set
            {
                if (_previewTitle == value)
                    return;

                _previewTitle = value;
                base.OnPropertyChanged(() => PreviewTitle);
            }
        }

        private Rect _previewWindowBounds = Rect.Empty;
        public Rect PreviewWindowBounds
        {
            get { return _previewWindowBounds; }
            set
            {
                if (_previewWindowBounds == value)
                    return;

                _previewWindowBounds = value;
                base.OnPropertyChanged(() => PreviewWindowBounds);
            }
        }

        private WindowState _previewWindowState = WindowState.Normal;
        public WindowState PreviewWindowState
        {
            get { return _previewWindowState; }
            set
            {
                if (_previewWindowState == value)
                    return;

                _previewWindowState = value;
                base.OnPropertyChanged(() => PreviewWindowState);
            }
        }

        private bool _isPreviewDocked = false;
        public bool IsPreviewDocked
        {
            get { return _isPreviewDocked; }
            set
            {
                if (_isPreviewDocked == value)
                    return;

                _isPreviewDocked = value;
                base.OnPropertyChanged(() => IsPreviewDocked);
            }
        }

        private bool _previewAutoPosition = true;
        public bool PreviewAutoPosition
        {
            get { return _previewAutoPosition; }
            set
            {
                if (_previewAutoPosition == value)
                    return;

                _previewAutoPosition = value;
                base.OnPropertyChanged(() => PreviewAutoPosition);
            }
        }

        private Dock _previewDockSide = Dock.Right;
        public Dock PreviewDockSide
        {
            get { return _previewDockSide; }
            set
            {
                if (_previewDockSide == value)
                    return;

                _previewDockSide = value;
                base.OnPropertyChanged(() => PreviewDockSide);
            }
        }

        private bool _isPreviewHidden = false;
        public bool IsPreviewHidden
        {
            get { return _isPreviewHidden; }
            set
            {
                if (_isPreviewHidden == value)
                    return;

                _isPreviewHidden = value;
                base.OnPropertyChanged(() => IsPreviewHidden);
            }
        }

        private double _previewDockedWidth = 200;
        public double PreviewDockedWidth
        {
            get { return _previewDockedWidth; }
            set
            {
                if (_previewDockedWidth == value)
                    return;

                _previewDockedWidth = Math.Max(value, 25);
                base.OnPropertyChanged(() => PreviewDockedWidth);
            }
        }

        private double _previewDockedHeight = 200;
        public double PreviewDockedHeight
        {
            get { return _previewDockedHeight; }
            set
            {
                if (_previewDockedHeight == value)
                    return;

                _previewDockedHeight = Math.Max(value, 25);
                base.OnPropertyChanged(() => PreviewDockedHeight);
            }
        }

        private SortType sortType;
        public SortType SortType
        {
            get { return sortType; }
            set
            {
                if (sortType != value)
                {
                    sortType = value;
                    base.OnPropertyChanged(() => SortType);
                }
                SortResults();
            }
        }

        private ListSortDirection sortDirection;
        public ListSortDirection SortDirection
        {
            get { return sortDirection; }
            set
            {
                if (sortDirection != value)
                {
                    sortDirection = value;
                    base.OnPropertyChanged(() => SortDirection);
                }
                SortResults();
            }
        }

        private bool highlightsOn;
        public bool HighlightsOn
        {
            get { return highlightsOn; }
            set
            {
                if (value == highlightsOn)
                    return;

                highlightsOn = value;
                base.OnPropertyChanged(() => HighlightsOn);
            }
        }

        private bool showLinesInContext;
        public bool ShowLinesInContext
        {
            get { return showLinesInContext; }
            set
            {
                if (value == showLinesInContext)
                    return;

                showLinesInContext = value;

                base.OnPropertyChanged(() => ShowLinesInContext);
            }
        }

        private int contextLinesBefore;
        public int ContextLinesBefore
        {
            get { return contextLinesBefore; }
            set
            {
                if (value == contextLinesBefore)
                    return;

                contextLinesBefore = value;

                base.OnPropertyChanged(() => ContextLinesBefore);
            }
        }

        private int contextLinesAfter;
        public int ContextLinesAfter
        {
            get { return contextLinesAfter; }
            set
            {
                if (value == contextLinesAfter)
                    return;

                contextLinesAfter = value;

                base.OnPropertyChanged(() => ContextLinesAfter);
            }
        }

        private bool isResultOptionsExpanded;
        public bool IsResultOptionsExpanded
        {
            get { return isResultOptionsExpanded; }
            set
            {
                if (value == isResultOptionsExpanded)
                    return;

                isResultOptionsExpanded = value;

                base.OnPropertyChanged(() => IsResultOptionsExpanded);
                base.OnPropertyChanged(() => ResultOptionsButtonTooltip);
            }
        }

        public string ResultOptionsButtonTooltip
        {
            get
            {
                if (IsResultOptionsExpanded)
                    return Resources.HideResultOptions;
                else
                    return Resources.ShowResultOptions;
            }
        }

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
                        param => this.Undo(),
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
                        param => this.ShowOptions()
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
                        param => this.ShowHelp()
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
                        param => this.ShowAbout()
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
                        param => this.Browse()
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
                        param => Search(),
                        param => CanSearch
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
                        param => Replace(),
                        param => CanReplace
                        );
                }
                return _replaceCommand;
            }
        }

        RelayCommand _sortCommand;
        /// <summary>
        /// Returns a command that sorts the results.
        /// </summary>
        public ICommand SortCommand
        {
            get
            {
                if (_sortCommand == null)
                {
                    _sortCommand = new RelayCommand(
                        param => SortResults(),
                        param => CanSortResults
                        );
                }
                return _sortCommand;
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
                        param => this.CopyFiles()
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
                        param => this.MoveFiles()
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
                        param => this.DeleteFiles()
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
                        param => this.CopyToClipboard()
                        );
                }
                return _copyToClipboardCommand;
            }
        }
        RelayCommand _saveResultsCommand;
        /// <summary>
        /// Returns a command that copies content to clipboard
        /// </summary>
        public ICommand SaveResultsCommand
        {
            get
            {
                if (_saveResultsCommand == null)
                {
                    _saveResultsCommand = new RelayCommand(
                        param => SaveResultsToFile(param as string)
                        );
                }
                return _saveResultsCommand;
            }
        }
        RelayCommand _copyMatchingLinesCommand;
        /// <summary>
        /// Returns a command that copies matching lines to clipboard
        /// </summary>
        public ICommand CopyMatchingLinesCommand
        {
            get
            {
                if (_copyMatchingLinesCommand == null)
                {
                    _copyMatchingLinesCommand = new RelayCommand(
                        param => CopyResults()
                        );
                }
                return _copyMatchingLinesCommand;
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
                        param => this.Cancel(),
                        param => CanCancel
                        );
                }
                return _cancelCommand;
            }
        }
        RelayCommand _highlightsCommand;
        /// <summary>
        /// Returns a command that toggles match highlights
        /// </summary>
        public ICommand HighlightsCommand
        {
            get
            {
                if (_highlightsCommand == null)
                {
                    _highlightsCommand = new RelayCommand(
                        param => ToggleHighlights()
                        );
                }
                return _highlightsCommand;
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
                        param => this.Test()
                        );
                }
                return _testCommand;
            }
        }
        RelayCommand _bookmarkAddCommand;
        public ICommand BookmarkAddCommand
        {
            get
            {
                if (_bookmarkAddCommand == null)
                {
                    _bookmarkAddCommand = new RelayCommand(
                        param => this.BookmarkAddRemove(false)
                        );
                }
                return _bookmarkAddCommand;
            }
        }
        RelayCommand _folderBookmarkAddCommand;
        public ICommand FolderBookmarkAddCommand
        {
            get
            {
                if (_folderBookmarkAddCommand == null)
                {
                    _folderBookmarkAddCommand = new RelayCommand(
                        param => this.BookmarkAddRemove(true)
                        );
                }
                return _folderBookmarkAddCommand;
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
                        param => this.BookmarkOpen()
                        );
                }
                return _bookmarkOpenCommand;
            }
        }

        RelayCommand _resetOptionsCommand;
        /// <summary>
        /// Returns a command that resets the search options.
        /// </summary>
        public ICommand ResetOptionsCommand
        {
            get
            {
                if (_resetOptionsCommand == null)
                {
                    _resetOptionsCommand = new RelayCommand(
                        param => this.ResetOptions()
                        );
                }
                return _resetOptionsCommand;
            }
        }

        RelayCommand _toggleFileOptionsCommand;
        /// <summary>
        /// Returns a command that resets the search options.
        /// </summary>
        public ICommand ToggleFileOptionsCommand
        {
            get
            {
                if (_toggleFileOptionsCommand == null)
                {
                    _toggleFileOptionsCommand = new RelayCommand(
                        param => IsFiltersExpanded = !IsFiltersExpanded
                        );
                }
                return _toggleFileOptionsCommand;
            }
        }

        RelayCommand _reloadThemeCommand;
        /// <summary>
        /// Returns a command that reloads the current theme file.
        /// </summary>
        public ICommand ReloadThemeCommand
        {
            get
            {
                if (_reloadThemeCommand == null)
                {
                    _reloadThemeCommand = new RelayCommand(
                        param => AppTheme.Instance.ReloadCurrentTheme()
                        );
                }
                return _reloadThemeCommand;
            }
        }

        #endregion

        #region Public Methods

        public void OnFileDrop(bool append, string[] filePaths)
        {
            string paths = append ? FileOrFolderPath : string.Empty;

            foreach (string path in filePaths)
            {
                if (!string.IsNullOrEmpty(paths))
                    paths += ";";

                paths += Utils.QuoteIfNeeded(path);
            }

            FileOrFolderPath = paths;
        }

        public override void UpdateState(string name)
        {
            base.UpdateState(name);

            if (bookmarkParameters.Contains(name))
            {
                Bookmark current = CurrentBookmarkSettings();
                var bmk = BookmarkLibrary.Instance.Find(current);
                if (bmk != null)
                {
                    IsBookmarked = true;
                    IsFolderBookmarked = bmk.FolderReferences.Contains(FileOrFolderPath);
                }
                else
                {
                    IsBookmarked = false;
                    IsFolderBookmarked = false;
                }
            }

            if (name == nameof(FileOrFolderPath) && !inUpdateBookmarks)
            {
                var bmk = BookmarkLibrary.Instance.Bookmarks.FirstOrDefault(b => b.FolderReferences.Contains(FileOrFolderPath));
                IsFolderBookmarked = bmk != null;
                if (bmk != null)
                {
                    ApplyBookmark(bmk);
                }
            }

            if (name == nameof(PreviewFileContent))
            {
                if (PreviewFileContent)
                {
                    if (SearchResults.SelectedNodes.Count > 0)
                    {
                        var item = SearchResults.SelectedNodes[0];

                        if (item is FormattedGrepLine grepLine)
                        {
                            PreviewFile(grepLine);
                        }
                        else if (item is FormattedGrepResult grepResult)
                        {
                            PreviewFile(grepResult);
                        }
                    }
                }
                else
                    PreviewHide?.Invoke(this, EventArgs.Empty);
            }
        }

        public override void LoadSettings()
        {
            base.LoadSettings();

            // changing the private field so as to not trigger sorting the results when
            // the Options dialog is closed
            sortType = GrepSettings.Instance.Get<SortType>(GrepSettings.Key.TypeOfSort);
            sortDirection = GrepSettings.Instance.Get<ListSortDirection>(GrepSettings.Key.SortDirection);
            SearchResults.ResultsScale = GrepSettings.Instance.Get<double>(GrepSettings.Key.ResultsTreeScale);
            SearchResults.WrapText = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ResultsTreeWrap);
            IsResultOptionsExpanded = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowResultOptions);
            HighlightsOn = GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightMatches);
            ShowLinesInContext = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext);
            ContextLinesBefore = GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore);
            ContextLinesAfter = GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter);

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            MainFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.MainFormFontSize);

            if (PreviewModel != null)
            {
                PreviewModel.ApplicationFontFamily = ApplicationFontFamily;
                PreviewModel.MainFormFontSize = MainFormFontSize;
            }
        }

        public override void SaveSettings()
        {
            CopyBookmarksToSettings();

            settings.Set(GrepSettings.Key.SortDirection, SortDirection);
            settings.Set(GrepSettings.Key.TypeOfSort, SortType);
            settings.Set(GrepSettings.Key.ShowResultOptions, IsResultOptionsExpanded);
            settings.Set(GrepSettings.Key.ResultsTreeScale, SearchResults.ResultsScale);
            settings.Set(GrepSettings.Key.ResultsTreeWrap, SearchResults.WrapText);
            settings.Set(GrepSettings.Key.HighlightMatches, HighlightsOn);
            settings.Set(GrepSettings.Key.ShowLinesInContext, ShowLinesInContext);
            settings.Set(GrepSettings.Key.ContextLinesBefore, ContextLinesBefore);
            settings.Set(GrepSettings.Key.ContextLinesAfter, ContextLinesAfter);

            LayoutProperties.PreviewBounds = PreviewWindowBounds;
            LayoutProperties.PreviewWindowState = PreviewWindowState;
            LayoutProperties.PreviewDocked = IsPreviewDocked;
            LayoutProperties.PreviewDockSide = PreviewDockSide.ToString();
            LayoutProperties.PreviewDockedWidth = PreviewDockedWidth;
            LayoutProperties.PreviewDockedHeight = PreviewDockedHeight;
            LayoutProperties.PreviewHidden = IsPreviewHidden;

            settings.Set(GrepSettings.Key.PreviewAutoPosition, PreviewAutoPosition);

            base.SaveSettings();
        }

        public void OpenFile(FormattedGrepLine selectedNode, bool useCustomEditor)
        {
            try
            {
                // Line was selected
                int lineNumber = selectedNode.GrepLine.LineNumber;

                int columnNumber = 1;
                string matchText = string.Empty;
                var firstMatch = selectedNode.GrepLine.Matches.FirstOrDefault();
                if (firstMatch != null)
                {
                    columnNumber = firstMatch.StartLocation + 1;
                    matchText = selectedNode.GrepLine.LineText.Substring(firstMatch.StartLocation, firstMatch.Length);
                }

                FormattedGrepResult result = selectedNode.Parent;
                OpenFileArgs fileArg = new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, matchText, columnNumber,
                    useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor),
                    settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                bool isInArchive = Utils.IsArchive(result.GrepResult.FileNameReal);
                if (isInArchive)
                {
                    ArchiveDirectory.OpenFile(fileArg);
                }
                else
                {
                    IGrepEngine engine = GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, GrepEngineInitParams.Default, new FileFilter());
                    if (engine != null)
                    {
                        engine.OpenFile(fileArg);
                        GrepEngineFactory.ReturnToPool(result.GrepResult.FileNameReal, engine);
                    }
                    if (fileArg.UseBaseEngine)
                        Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, matchText, columnNumber,
                            useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor),
                            settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to open file.");
                if (useCustomEditor)
                    MessageBox.Show(Resources.CustomEditorFileOpenError + Environment.NewLine +
                        Resources.CheckEditorPath,
                        Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show(Resources.ErrorOpeningFile + App.LogDir,
                        Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenFile(FormattedGrepResult result, bool useCustomEditor)
        {
            try
            {
                // Line was selected
                int lineNumber = 0;

                int columnNumber = 1;
                string matchText = string.Empty;
                var firstLine = result.GrepResult.SearchResults.FirstOrDefault(r => !r.IsContext);
                if (firstLine != null)
                {
                    lineNumber = firstLine.LineNumber;

                    var firstMatch = firstLine.Matches.FirstOrDefault();
                    if (firstMatch != null)
                    {
                        columnNumber = firstMatch.StartLocation + 1;
                        matchText = firstLine.LineText.Substring(firstMatch.StartLocation, firstMatch.Length);
                    }
                }

                OpenFileArgs fileArg = new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, matchText, columnNumber,
                    useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor),
                    settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                if (Utils.IsArchive(result.GrepResult.FileNameReal))
                {
                    ArchiveDirectory.OpenFile(fileArg);
                }
                else
                {
                    IGrepEngine engine = GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, GrepEngineInitParams.Default, new FileFilter());
                    if (engine != null)
                    {
                        engine.OpenFile(fileArg);
                        GrepEngineFactory.ReturnToPool(result.GrepResult.FileNameReal, engine);
                    }
                    if (fileArg.UseBaseEngine)
                        Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, matchText, columnNumber,
                            useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor),
                            settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to open file.");
                if (useCustomEditor)
                    MessageBox.Show(Resources.CustomEditorFileOpenError + Environment.NewLine +
                        Resources.CheckEditorPath,
                        Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show(Resources.ErrorOpeningFile + App.LogDir,
                        Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PreviewFile(FormattedGrepLine formattedGrepLine)
        {
            if (PreviewFileContent)
            {
                int lineNumber = formattedGrepLine.GrepLine.LineNumber;
                FormattedGrepResult result = formattedGrepLine.Parent;
                PreviewFile(result.GrepResult.FileNameReal, result.GrepResult, lineNumber);
            }
        }

        public void PreviewFile(FormattedGrepResult formattedGrepResult)
        {
            if (PreviewFileContent)
            {
                int lineNumber = 0;
                if (formattedGrepResult.GrepResult != null &&
                    formattedGrepResult.GrepResult.Matches != null &&
                    formattedGrepResult.GrepResult.Matches.Count > 0)
                {
                    lineNumber = formattedGrepResult.GrepResult.Matches[0].LineNumber;
                }

                PreviewFile(formattedGrepResult.GrepResult.FileNameReal, formattedGrepResult.GrepResult, lineNumber);
            }
        }

        #endregion

        #region Private Methods

        internal void CancelSearch()
        {
            Utils.CancelSearch = true;
            if (workerSearchReplace.IsBusy)
                workerSearchReplace.CancelAsync();
        }

        private void DoSearchReplace(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (e.Argument is SearchReplaceCriteria param && !workerSearchReplace.CancellationPending)
                {
                    timer = DateTime.Now;

                    if (param.Operation == GrepOperation.Search || param.Operation == GrepOperation.SearchInResults)
                    {
                        int sizeFrom = 0;
                        int sizeTo = 0;
                        if (param.UseFileSizeFilter == FileSizeFilter.Yes)
                        {
                            sizeFrom = param.SizeFrom;
                            sizeTo = param.SizeTo;
                        }

                        DateTime? startTime = null, endTime = null;
                        if (param.UseFileDateFilter != FileDateFilter.None)
                        {
                            if (param.TypeOfTimeRangeFilter == FileTimeRange.Dates)
                            {
                                startTime = param.StartDate;
                                endTime = param.EndDate;
                                // the end date should go through the end of the day
                                if (endTime.HasValue)
                                    endTime = endTime.Value.AddDays(1.0);
                            }
                            else if (param.TypeOfTimeRangeFilter == FileTimeRange.Hours)
                            {
                                int low = Math.Min(param.HoursFrom, param.HoursTo);
                                int high = Math.Max(param.HoursFrom, param.HoursTo);
                                startTime = DateTime.Now.AddHours(-1 * high);
                                endTime = DateTime.Now.AddHours(-1 * low);
                            }
                        }

                        string filePatternInclude = "*.*";
                        if (param.TypeOfFileSearch == FileSearchType.Regex)
                            filePatternInclude = ".*";
                        else if (param.TypeOfFileSearch == FileSearchType.Everything)
                            filePatternInclude = string.Empty;

                        if (!string.IsNullOrEmpty(param.FilePattern))
                            filePatternInclude = param.FilePattern;

                        string filePatternExclude = "";
                        if (!string.IsNullOrEmpty(param.FilePatternIgnore))
                            filePatternExclude = param.FilePatternIgnore;

                        IEnumerable<string> files;

                        Utils.CancelSearch = false;

                        FileFilter fileParams = new FileFilter(FileOrFolderPath, filePatternInclude, filePatternExclude,
                            param.TypeOfFileSearch == FileSearchType.Regex, param.UseGitIgnore, param.TypeOfFileSearch == FileSearchType.Everything,
                            param.IncludeSubfolder, param.MaxSubfolderDepth, param.IncludeHidden, param.IncludeBinary, param.IncludeArchive,
                            param.FollowSymlinks, sizeFrom, sizeTo, param.UseFileDateFilter, startTime, endTime);

                        if (param.Operation == GrepOperation.SearchInResults)
                        {
                            files = param.SearchInFiles;
                        }
                        else
                        {
                            files = Utils.GetFileListEx(fileParams);
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
                                MessageBox.Show(Resources.IncorrectPattern + regException.Message,
                                    Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Warning);
                                e.Result = null;
                                return;
                            }
                        }

                        GrepCore grep = new GrepCore();
                        grep.SearchParams = new GrepEngineInitParams(
                            settings.Get<bool>(GrepSettings.Key.ShowLinesInContext),
                            settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                            settings.Get<int>(GrepSettings.Key.ContextLinesAfter),
                            settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                            settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
                            SearchParallel);

                        grep.FileFilter = new FileFilter(FileOrFolderPath, filePatternInclude, filePatternExclude,
                            param.TypeOfFileSearch == FileSearchType.Regex, param.UseGitIgnore, param.TypeOfFileSearch == FileSearchType.Everything,
                            param.IncludeSubfolder, param.MaxSubfolderDepth, param.IncludeHidden, param.IncludeBinary, param.IncludeArchive,
                            param.FollowSymlinks, sizeFrom, sizeTo, param.UseFileDateFilter, startTime, endTime);

                        GrepSearchOption searchOptions = GrepSearchOption.None;
                        if (Multiline)
                            searchOptions |= GrepSearchOption.Multiline;
                        if (CaseSensitive)
                            searchOptions |= GrepSearchOption.CaseSensitive;
                        if (Singleline)
                            searchOptions |= GrepSearchOption.SingleLine;
                        if (WholeWord)
                            searchOptions |= GrepSearchOption.WholeWord;
                        if (BooleanOperators)
                            searchOptions |= GrepSearchOption.BooleanOperators;
                        if (StopAfterFirstMatch)
                            searchOptions |= GrepSearchOption.StopAfterFirstMatch;

                        grep.ProcessedFile += GrepCore_ProcessedFile;

                        if (CaptureGroupSearch && param.TypeOfFileSearch == FileSearchType.Regex &&
                            !string.IsNullOrEmpty(param.SearchFor))
                        {
                            e.Result = grep.CaptureGroupSearch(files, filePatternInclude, searchOptions, param.TypeOfSearch, param.SearchFor, param.CodePage);
                        }
                        else
                        {
                            e.Result = grep.Search(files, param.TypeOfSearch, param.SearchFor, searchOptions, param.CodePage);
                        }
                        grep.ProcessedFile -= GrepCore_ProcessedFile;
                    }
                    else
                    {
                        GrepCore grep = new GrepCore();
                        grep.SearchParams = new GrepEngineInitParams(
                            settings.Get<bool>(GrepSettings.Key.ShowLinesInContext),
                            settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                            settings.Get<int>(GrepSettings.Key.ContextLinesAfter),
                            settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                            settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
                            SearchParallel);

                        GrepSearchOption searchOptions = GrepSearchOption.None;
                        if (Multiline)
                            searchOptions |= GrepSearchOption.Multiline;
                        if (CaseSensitive)
                            searchOptions |= GrepSearchOption.CaseSensitive;
                        if (Singleline)
                            searchOptions |= GrepSearchOption.SingleLine;
                        if (WholeWord)
                            searchOptions |= GrepSearchOption.WholeWord;
                        if (BooleanOperators)
                            searchOptions |= GrepSearchOption.BooleanOperators;
                        if (StopAfterFirstMatch)
                            searchOptions |= GrepSearchOption.StopAfterFirstMatch;

                        grep.ProcessedFile += GrepCore_ProcessedFile;
                        e.Result = grep.Replace(param.ReplaceFiles, param.TypeOfSearch, param.SearchFor, param.ReplaceWith, searchOptions, param.CodePage);
                        grep.ProcessedFile -= GrepCore_ProcessedFile;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed in search/replace");
                bool isSearch = true;
                if (e.Argument is MainViewModel param)
                {
                    if (param.CurrentGrepOperation == GrepOperation.Search || param.CurrentGrepOperation == GrepOperation.SearchInResults)
                        isSearch = true;
                    else
                        isSearch = false;
                }
                if (isSearch)
                    MessageBox.Show(Resources.SearchFailedError + App.LogDir,
                        Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show(Resources.ReplaceFailedError + App.LogDir,
                        Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private readonly object lockObjOne = new object();
        private void SearchProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (!Utils.CancelSearch)
                {
                    ProgressStatus progress = (ProgressStatus)e.UserState;

                    if (progress != null && !progress.BeginSearch && progress.SearchResults != null && progress.SearchResults.Count > 0)
                    {
                        lock (lockObjOne)
                        {
                            SearchResults.AddRange(progress.SearchResults);
                        }
                    }

                    UpdateStatus(progress);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failure in search progress changed");
                MessageBox.Show(Resources.SearchOrReplaceFailed + App.LogDir,
                    Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private readonly object lockObjTwo = new object();
        private void UpdateStatus(ProgressStatus progress)
        {
            // When running in parallel, multiple files will be in progress at the same time.
            // This keeps track of the files that are running and the long running file names
            // are shown again as the short runs finish.
            lock (lockObjTwo)
            {
                string fileName = progress.FileName;
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    if (progress.BeginSearch)
                    {
                        if (!currentSearchFiles.Contains(fileName))
                            currentSearchFiles.Add(fileName);
                    }
                    else
                    {
                        if (currentSearchFiles.Contains(fileName))
                            currentSearchFiles.Remove(fileName);

                        if (currentSearchFiles.Count > 0)
                            fileName = currentSearchFiles.FirstOrDefault();
                    }
                }

                // keep the total count of processed files to report at completion
                processedFiles = Math.Max(processedFiles, progress.ProcessedFiles);

                // the search has gotten fast enough that setting the StatusMessage is slowing 
                // down the main thread.  Update the latest status message, but only update the 
                // UI periodically when the dispatcher is idle.
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    latestStatusMessage = string.Format(Resources.Searched0FilesFound1MatchingFilesProcessing2,
                        progress.ProcessedFiles, progress.SuccessfulFiles, fileName);
                }
                else
                {
                    latestStatusMessage = string.Format(Resources.Searched0FilesFound1MatchingFiles,
                        progress.ProcessedFiles, progress.SuccessfulFiles);
                }
            }
        }

        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(latestStatusMessage))
                StatusMessage = latestStatusMessage;
        }

        private void SearchComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            latestStatusMessage = string.Empty;
            idleTimer.Stop();
            try
            {
                if (CurrentGrepOperation == GrepOperation.Search || CurrentGrepOperation == GrepOperation.SearchInResults)
                {
                    if (e.Result == null)
                    {
                        StatusMessage = Resources.SearchCanceledOrFailed;
                    }
                    else if (!e.Cancelled)
                    {
                        TimeSpan duration = DateTime.Now.Subtract(timer);
                        int successCount = 0;
                        if (e.Result is List<GrepSearchResult> results)
                            successCount = results.Where(r => r.IsSuccess).Count();

                        StatusMessage = string.Format(Resources.SearchCompleteSearched0FilesFound1FilesIn2,
                            processedFiles, successCount, duration.GetPrettyString());
                    }
                    else
                    {
                        StatusMessage = Resources.SearchCanceled;
                    }
                    if (SearchResults.Count > 0)
                        FilesFound = true;
                    CurrentGrepOperation = GrepOperation.None;
                    base.OnPropertyChanged(() => CurrentGrepOperation);
                    CanSearch = true;

                    if (Application.Current is App app)
                    {
                        ProcessCommands(app.AppArgs);
                    }
                }
                else if (CurrentGrepOperation == GrepOperation.Replace)
                {
                    if (!e.Cancelled)
                    {
                        if (e.Result == null || ((int)e.Result) == -1)
                        {
                            StatusMessage = Resources.ReplaceFailed;
                            MessageBox.Show(Resources.ReplaceFailedError + App.LogDir,
                                Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            StatusMessage = string.Format(Resources.ReplaceComplete0FilesReplaced,
                                (int)e.Result);
                            CanUndo = undoList.Count > 0;
                        }
                    }
                    else
                    {
                        StatusMessage = Resources.ReplaceCanceled;
                    }
                    CurrentGrepOperation = GrepOperation.None;
                    base.OnPropertyChanged(() => CurrentGrepOperation);
                    CanSearch = true;
                    SearchResults.Clear();
                }

                string outdatedEngines = dnGREP.Engines.GrepEngineFactory.GetListOfFailedEngines();
                if (!string.IsNullOrEmpty(outdatedEngines))
                {
                    MessageBox.Show(Resources.TheFollowingPluginsFailedToLoad +
                        Environment.NewLine + Environment.NewLine +
                        outdatedEngines + Environment.NewLine + Environment.NewLine +
                        Resources.DefaultEngineWasUsedInstead,
                        Resources.DnGrep + "  " + Resources.PluginErrors, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failure in search complete update");
                MessageBox.Show(Resources.SearchOrReplaceFailed + App.LogDir,
                    Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Utils.CancelSearch = false;
                currentSearchFiles.Clear();
            }
        }

        void GrepCore_ProcessedFile(object sender, ProgressStatus progress)
        {
            workerSearchReplace.ReportProgress((int)progress.ProcessedFiles, progress);
        }

        private void Browse()
        {
            string filePattern = PathSearchText.FilePattern;

            fileFolderDialog.Dialog.Multiselect = true;
            fileFolderDialog.SelectedPath = PathSearchText.BaseFolder;
            if (string.IsNullOrWhiteSpace(PathSearchText.BaseFolder))
            {
                string clipboard = Clipboard.GetText();
                try
                {
                    if (Path.IsPathRooted(clipboard))
                        fileFolderDialog.SelectedPath = clipboard;
                }
                catch
                {
                    // Ignore
                }
            }
            if (fileFolderDialog.ShowDialog() == true)
            {
                string newPath = string.Empty;
                if (fileFolderDialog.SelectedPaths != null)
                {
                    if (TypeOfFileSearch == FileSearchType.Everything)
                    {
                        string[] paths = fileFolderDialog.SelectedPaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        newPath = string.Join("|", paths.Select(p => Utils.Quote(p)).ToArray());
                    }
                    else
                    {
                        newPath = fileFolderDialog.SelectedPaths;
                    }
                }
                else
                {
                    newPath = fileFolderDialog.SelectedPath;
                }

                if (!string.IsNullOrWhiteSpace(filePattern))
                {
                    if (newPath.Contains(" ") && !newPath.StartsWith("\""))
                        newPath = Utils.Quote(newPath);

                    newPath += " " + filePattern;
                }

                FileOrFolderPath = newPath;
            }
        }

        private void Search()
        {
            if (CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
            {
                SaveSettings();
                SearchParametersChanged = false;

                if (TypeOfFileSearch == FileSearchType.Regex)
                {
                    if (!ValidateFilePatterns())
                        return;
                }

                if (SearchInResultsContent && CanSearchInResults)
                    CurrentGrepOperation = GrepOperation.SearchInResults;
                else
                    CurrentGrepOperation = GrepOperation.Search;
                StatusMessage = Resources.Searching;

                PreviewModel.FilePath = string.Empty;
                PreviewTitle = string.Empty;


                SearchReplaceCriteria workerParams = new SearchReplaceCriteria(this);
                if (SearchInResultsContent && CanSearchInResults)
                {
                    List<string> foundFiles = new List<string>();
                    foreach (FormattedGrepResult n in SearchResults) foundFiles.Add(n.GrepResult.FileNameReal);
                    workerParams.AddSearchFiles(foundFiles);
                }
                SearchResults.Clear();
                processedFiles = 0;
                idleTimer.Start();
                workerSearchReplace.RunWorkerAsync(workerParams);
                UpdateBookmarks();
                // toggle value to move focus to the results tree, and enable keyboard actions on the tree
                SearchResults.IsResultsTreeFocused = false;
                SearchResults.IsResultsTreeFocused = true;
            }
        }

        private bool ValidateFilePatterns()
        {
            if (!string.IsNullOrWhiteSpace(FilePattern))
            {
                foreach (string pattern in Utils.SplitPattern(FilePattern))
                {
                    string msg = ValidateRegex(pattern);
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        MessageBox.Show(string.Format(Resources.TheFilePattern0IsNotAValidRegularExpression12, pattern, Environment.NewLine, msg),
                            Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(FilePatternIgnore))
            {
                foreach (string pattern in Utils.SplitPattern(FilePatternIgnore))
                {
                    string msg = ValidateRegex(pattern);
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        MessageBox.Show(string.Format(Resources.TheFilePattern0IsNotAValidRegularExpression12, pattern, Environment.NewLine, msg),
                            Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            return true;
        }

        private string ValidateRegex(string FilePattern)
        {
            try
            {
                Regex regex = new Regex(FilePattern);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void Replace()
        {
            if (CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
            {
                if (string.IsNullOrEmpty(ReplaceWith))
                {
                    if (MessageBox.Show(Resources.AreYouSureYouWantToReplaceSearchPatternWithEmptyString,
                        Resources.DnGrep + "  " + Resources.Replace, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                List<string> roFiles = Utils.GetReadOnlyFiles(SearchResults.GetList());
                if (roFiles.Count > 0)
                {
                    StringBuilder sb = new StringBuilder(Resources.SomeOfTheFilesCannotBeModifiedIfYouContinueTheseFilesWillBeSkipped);
                    sb.Append(Environment.NewLine)
                      .Append(Resources.WouldYouLikeToContinue)
                      .Append(Environment.NewLine).Append(Environment.NewLine);
                    foreach (string fileName in roFiles)
                    {
                        sb.AppendLine(" - " + new FileInfo(fileName).Name);
                    }
                    if (MessageBox.Show(sb.ToString(), Resources.DnGrep + "  " + Resources.Replace,
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                List<GrepSearchResult> replaceList = SearchResults.GetWritableList();
                foreach (var file in roFiles)
                {
                    var item = replaceList.FirstOrDefault(r => r.FileNameReal == file);
                    if (item != null)
                        replaceList.Remove(item);
                }

                ReplaceWindow dlg = new ReplaceWindow();
                dlg.ViewModel.SearchFor = SearchFor;
                dlg.ViewModel.ReplaceWith = ReplaceWith;
                dlg.ViewModel.SearchResults = replaceList;
                var result = dlg.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    CanUndo = false;
                    undoList.Clear();
                    foreach (GrepSearchResult gsr in replaceList)
                    {
                        string filePath = gsr.FileNameReal;
                        if (!gsr.ReadOnly && !undoList.Any(r => r.OrginalFile == filePath) && gsr.Matches.Any(m => m.ReplaceMatch))
                        {
                            undoList.Add(new ReplaceDef(filePath, gsr.Matches));
                        }
                    }

                    if (undoList.Count > 0)
                    {
                        StatusMessage = Resources.Replacing;

                        PreviewModel.FilePath = string.Empty;
                        PreviewTitle = string.Empty;

                        CurrentGrepOperation = GrepOperation.Replace;

                        SearchReplaceCriteria workerParams = new SearchReplaceCriteria(this);

                        workerParams.AddReplaceFiles(undoList);

                        SearchResults.Clear();
                        idleTimer.Start();
                        workerSearchReplace.RunWorkerAsync(workerParams);
                        UpdateBookmarks();
                    }
                }
            }
        }

        private void Undo()
        {
            if (CanUndo)
            {
                MessageBoxResult response = MessageBox.Show(
                    Resources.UndoWillRevertModifiedFiles,
                    Resources.DnGrep + "  " + Resources.Undo, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (response == MessageBoxResult.Yes)
                {
                    GrepCore core = new GrepCore();
                    bool result = core.Undo(undoList);
                    if (result)
                    {
                        MessageBox.Show(Resources.FilesHaveBeenSuccessfullyReverted, 
                            Resources.DnGrep + "  " + Resources.Undo, MessageBoxButton.OK, MessageBoxImage.Information);
                        Utils.DeleteTempFolder();
                        undoList.Clear();
                    }
                    else
                    {
                        MessageBox.Show(Resources.ThereWasAnErrorRevertingFiles + App.LogDir,
                            Resources.DnGrep + "  " + Resources.Undo, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CanUndo = false;
                }
            }
        }

        private bool inUpdateBookmarks;
        private void UpdateBookmarks()
        {
            inUpdateBookmarks = true;

            int maxSearchReplaceCount = settings.Get<int>(GrepSettings.Key.MaxSearchBookmarks);
            int maxPathCount = settings.Get<int>(GrepSettings.Key.MaxPathBookmarks);
            int maxExtCount = settings.Get<int>(GrepSettings.Key.MaxExtensionBookmarks);

            // Update bookmarks, moving current to the top of the list
            if (FastSearchBookmarks.IndexOf(SearchFor) != 0)
            {
                FastSearchBookmarks.Insert(0, SearchFor);
                int idx = FastSearchBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == SearchFor).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = SearchFor;
                    FastSearchBookmarks.RemoveAt(idx);
                    SearchFor = s;
                }
            }
            while (FastSearchBookmarks.Count > maxSearchReplaceCount)
                FastSearchBookmarks.RemoveAt(FastSearchBookmarks.Count - 1);

            if (FastReplaceBookmarks.IndexOf(ReplaceWith) != 0)
            {
                FastReplaceBookmarks.Insert(0, ReplaceWith);
                int idx = FastReplaceBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == ReplaceWith).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = ReplaceWith;
                    FastReplaceBookmarks.RemoveAt(idx);
                    ReplaceWith = s;
                }
            }
            while (FastReplaceBookmarks.Count > maxSearchReplaceCount)
                FastReplaceBookmarks.RemoveAt(FastReplaceBookmarks.Count - 1);

            if (FastFileMatchBookmarks.IndexOf(FilePattern) != 0)
            {
                FastFileMatchBookmarks.Insert(0, FilePattern);
                int idx = FastFileMatchBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == FilePattern).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = FilePattern;
                    FastFileMatchBookmarks.RemoveAt(idx);
                    FilePattern = s;
                }
            }
            while (FastFileMatchBookmarks.Count > maxExtCount)
                FastFileMatchBookmarks.RemoveAt(FastFileMatchBookmarks.Count - 1);

            if (FastFileNotMatchBookmarks.IndexOf(FilePatternIgnore) != 0)
            {
                FastFileNotMatchBookmarks.Insert(0, FilePatternIgnore);
                int idx = FastFileNotMatchBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == FilePatternIgnore).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = FilePatternIgnore;
                    FastFileNotMatchBookmarks.RemoveAt(idx);
                    FilePatternIgnore = s;
                }
            }
            while (FastFileNotMatchBookmarks.Count > maxExtCount)
                FastFileNotMatchBookmarks.RemoveAt(FastFileNotMatchBookmarks.Count - 1);

            string searchPath = FileOrFolderPath;
            if (FastPathBookmarks.IndexOf(searchPath) != 0)
            {
                FastPathBookmarks.Insert(0, searchPath);
                int idx = FastPathBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == searchPath).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = searchPath;
                    FastPathBookmarks.RemoveAt(idx);
                    FileOrFolderPath = s;
                }
            }
            while (FastPathBookmarks.Count > maxPathCount)
                FastPathBookmarks.RemoveAt(FastPathBookmarks.Count - 1);

            inUpdateBookmarks = false;
        }

        private void Cancel()
        {
            if (CurrentGrepOperation != GrepOperation.None)
            {
                Utils.CancelSearch = true;
            }
        }

        private void ShowOptions()
        {
            SaveSettings();
            var optionsForm = new OptionsView();
            optionsForm.Owner = ParentWindow;
            var optionsViewModel = new OptionsViewModel();
            optionsForm.DataContext = optionsViewModel;
            try
            {
                optionsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error saving options");
                MessageBox.Show(Resources.ThereWasAnErrorSavingOptions + App.LogDir,
                    Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            LoadSettings();
            SearchResults.RaiseSettingsPropertiesChanged();
        }

        private void ShowHelp()
        {
            System.Diagnostics.Process.Start(@"https://github.com/dnGrep/dnGrep/wiki");
        }

        private void ShowAbout()
        {
            AboutWindow aboutForm = new AboutWindow();
            aboutForm.Owner = Application.Current.MainWindow;
            aboutForm.ShowDialog();
        }

        private void ToggleHighlights()
        {
            if (HighlightsOn)
            {
                Application.Current.Resources["Match.Highlight.Background"] = highlightBackground;
                Application.Current.Resources["Match.Highlight.Foreground"] = highlightForeground;
            }
            else
            {
                Application.Current.Resources["Match.Highlight.Background"] = Application.Current.Resources["TreeView.Background"];
                Application.Current.Resources["Match.Highlight.Foreground"] = Application.Current.Resources["TreeView.Foreground"];
            }

            if (PreviewModel != null)
            {
                PreviewModel.HighlightsOn = HighlightsOn;
            }
        }

        // list of properties that are saved in a bookmarks
        private static readonly HashSet<string> bookmarkParameters = new HashSet<string>
        {
            nameof(SearchFor),
            nameof(ReplaceWith),
            nameof(FilePattern),
            nameof(FilePatternIgnore),
            nameof(TypeOfFileSearch),
            nameof(TypeOfSearch),
            nameof(CaseSensitive),
            nameof(WholeWord),
            nameof(Multiline),
            nameof(Singleline),
            nameof(BooleanOperators),
            nameof(IncludeSubfolder),
            nameof(IncludeHidden),
            nameof(IncludeBinary),
            nameof(FollowSymlinks),
            nameof(MaxSubfolderDepth),
            nameof(UseGitignore),
            nameof(IncludeArchive),
            nameof(CodePage),
        };

        public Bookmark CurrentBookmarkSettings()
        {
            return new Bookmark(SearchFor, ReplaceWith, FilePattern)
            {
                IgnoreFilePattern = FilePatternIgnore,
                TypeOfFileSearch = TypeOfFileSearch,
                TypeOfSearch = TypeOfSearch,
                CaseSensitive = CaseSensitive,
                WholeWord = WholeWord,
                Multiline = Multiline,
                Singleline = Singleline,
                BooleanOperators = BooleanOperators,
                IncludeSubfolders = IncludeSubfolder,
                IncludeHiddenFiles = IncludeHidden,
                IncludeBinaryFiles = IncludeBinary,
                MaxSubfolderDepth = MaxSubfolderDepth,
                UseGitignore = UseGitignore,
                IncludeArchive = IncludeArchive,
                FollowSymlinks = FollowSymlinks,
                CodePage = CodePage,
            };
        }

        private void BookmarkAddRemove(bool associateWithFolder)
        {
            Bookmark current = CurrentBookmarkSettings();

            if (associateWithFolder)
            {
                if (IsFolderBookmarked && !string.IsNullOrWhiteSpace(FileOrFolderPath))
                {
                    Bookmark bmk = BookmarkLibrary.Instance.Find(current);
                    if (bmk == null)
                    {
                        bmk = current;
                        BookmarkLibrary.Instance.Bookmarks.Add(current);
                        IsBookmarked = true;
                    }
                    BookmarkLibrary.Instance.AddFolderReference(bmk, FileOrFolderPath);
                    IsFolderBookmarked = true;
                }
                else
                {
                    Bookmark bmk = BookmarkLibrary.Instance.Find(current);
                    if (bmk != null && bmk.FolderReferences.Contains(FileOrFolderPath))
                    {
                        bmk.FolderReferences.Remove(FileOrFolderPath);
                    }
                    IsFolderBookmarked = false;
                }
            }
            else
            {
                if (IsBookmarked)
                {
                    if (!BookmarkLibrary.Instance.Bookmarks.Contains(current))
                        BookmarkLibrary.Instance.Bookmarks.Add(current);
                    IsBookmarked = true;
                }
                else
                {
                    Bookmark bmk = BookmarkLibrary.Instance.Find(current);
                    if (bmk != null)
                    {
                        int count = bmk.FolderReferences.Count(s => s != FileOrFolderPath);
                        if (count > 0)
                        {
                            string message;
                            if (count == 1)
                            {
                                message = Resources.ThisBookmarkIsAssociatedWithOneOtherFolder +
                                    Environment.NewLine + Resources.ClearingThisBookmarkWillAlsoClearThatBookmark;
                            }
                            else
                            {
                                message = string.Format(Resources.ThisBookmarkIsAssociatedWith0OtherFolders, count) +
                                    Environment.NewLine + Resources.ClearingThisBookmarkWillAlsoRemoveThoseBookmarks;

                            }
                            var ans = MessageBox.Show(message + Environment.NewLine + Environment.NewLine +
                                Resources.DoYouWantToContinue, Resources.DnGrep,
                                MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (ans == MessageBoxResult.No)
                            {
                                IsBookmarked = true;
                                return;
                            }
                        }

                        BookmarkLibrary.Instance.Bookmarks.Remove(bmk);

                        IsBookmarked = false;
                        IsFolderBookmarked = false;
                    }
                }
            }
            BookmarkLibrary.Save();
        }

        private void BookmarkOpen()
        {
            try
            {
                bookmarkWindow = new BookmarksWindow(bk =>
                    {
                        if (CurrentBookmarkSettings() == bk)
                        {
                            IsBookmarked = false;
                            IsFolderBookmarked = false;
                        }
                    });
                bookmarkWindow.UseBookmark += BookmarkForm_UseBookmark;

                var wnd = Application.Current.MainWindow;
                Point pt = Mouse.GetPosition(wnd);
                pt.Offset(-bookmarkWindow.Width + 100, 20);
                bookmarkWindow.SetWindowPosition(pt, wnd);
                bookmarkWindow.ShowDialog();
            }
            finally
            {
                bookmarkWindow.UseBookmark -= BookmarkForm_UseBookmark;
            }
        }

        private void CopyFiles()
        {
            if (FilesFound)
            {
                if (fileFolderDialog.ShowDialog() == true)
                {
                    try
                    {
                        var fileList = SearchResults.GetList();
                        string destinationFolder = Utils.GetBaseFolder(fileFolderDialog.SelectedPath);
                        bool hasSingleBaseFolder = Utils.HasSingleBaseFolder(PathSearchText.FileOrFolderPath);
                        string baseFolder = PathSearchText.BaseFolder;

                        if (!Utils.CanCopyFiles(fileList, destinationFolder))
                        {
                            MessageBox.Show(Resources.SomeOfTheFilesAreLocatedInTheSelectedDirectory + Environment.NewLine +
                                Resources.PleaseSelectAnotherDirectoryAndTryAgain,
                                Resources.DnGrep + "  " + Resources.CopyFiles, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        int count = 0;
                        if (hasSingleBaseFolder && !string.IsNullOrWhiteSpace(baseFolder))
                        {
                            count = Utils.CopyFiles(fileList, baseFolder, destinationFolder, OverwriteFile.Prompt);
                        }
                        else
                        {
                            // without a common base path, copy all files to a single directory 
                            count = Utils.CopyFiles(fileList, destinationFolder, OverwriteFile.Prompt);
                        }
                        MessageBox.Show(string.Format(Resources.CountFilesHaveBeenSuccessfullyCopied, count),
                            Resources.DnGrep + "  " + Resources.CopyFiles,
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error copying files");
                        MessageBox.Show(Resources.ThereWasAnErrorCopyingFiles + App.LogDir,
                            Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CanUndo = false;
                }
            }
        }

        private void MoveFiles()
        {
            if (FilesFound)
            {
                if (fileFolderDialog.ShowDialog() == true)
                {
                    try
                    {
                        var fileList = SearchResults.GetList();
                        string destinationFolder = Utils.GetBaseFolder(fileFolderDialog.SelectedPath);
                        bool hasSingleBaseFolder = Utils.HasSingleBaseFolder(PathSearchText.FileOrFolderPath);
                        string baseFolder = PathSearchText.BaseFolder;

                        if (!Utils.CanCopyFiles(fileList, destinationFolder))
                        {
                            MessageBox.Show(Resources.SomeOfTheFilesAreLocatedInTheSelectedDirectory + Environment.NewLine +
                                Resources.PleaseSelectAnotherDirectoryAndTryAgain,
                                Resources.DnGrep + "  " + Resources.MoveFiles, MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        int count = 0;
                        if (hasSingleBaseFolder && !string.IsNullOrWhiteSpace(baseFolder))
                        {
                            count = Utils.MoveFiles(fileList, baseFolder, destinationFolder, OverwriteFile.Prompt);
                        }
                        else
                        {
                            // without a common base path, move all files to a single directory 
                            count = Utils.MoveFiles(fileList, destinationFolder, OverwriteFile.Prompt);
                        }
                        MessageBox.Show(string.Format(Resources.CountFilesHaveBeenSuccessfullyMoved, count),
                            Resources.DnGrep + "  " + Resources.MoveFiles, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error moving files");
                        MessageBox.Show(Resources.ThereWasAnErrorMovingFiles + App.LogDir,
                            Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CanUndo = false;
                    SearchResults.Clear();
                    FilesFound = false;
                }
            }
        }

        private void DeleteFiles()
        {
            if (FilesFound)
            {
                try
                {
                    if (MessageBox.Show(Resources.YouAreAboutToDeleteFilesFoundDuringSearch + Environment.NewLine +
                        Resources.AreYouSureYouWantToContinue,
                        Resources.DnGrep + "  " + Resources.DeleteFiles, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    int count = Utils.DeleteFiles(SearchResults.GetList());
                    MessageBox.Show(string.Format(Resources.CountFilesHaveBeenSuccessfullyDeleted, count),
                        Resources.DnGrep + "  " + Resources.DeleteFiles, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error deleting files");
                    MessageBox.Show(Resources.ThereWasAnErrorDeletingFiles + App.LogDir,
                        Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                CanUndo = false;
                SearchResults.Clear();
                FilesFound = false;
            }
        }

        private void CopyToClipboard()
        {
            StringBuilder sb = new StringBuilder();
            foreach (GrepSearchResult result in SearchResults.GetList())
            {
                sb.AppendLine(result.FileNameReal);
            }
            NativeMethods.SetClipboardText(sb.ToString());
        }

        private void CopyResults()
        {
            // can be a long process if the results are not yet cached
            UIServices.SetBusyState();
            NativeMethods.SetClipboardText(Utils.GetResultLines(SearchResults.GetList()));
        }

        private async void SaveResultsToFile(string reportType)
        {
            if (FilesFound)
            {
                SaveFileDialog dlg = new SaveFileDialog();

                switch (reportType)
                {
                    case "Report":
                        dlg.Filter = Resources.ReportFileFormat + "|*.txt";
                        dlg.DefaultExt = "*.txt";
                        break;
                    case "Text":
                        dlg.Filter = Resources.ResultsFileFormat + "|*.txt";
                        dlg.DefaultExt = "*.txt";
                        break;
                    case "CSV":
                        dlg.Filter = Resources.CSVFileFormat + "|*.csv";
                        dlg.DefaultExt = "*.csv";
                        break;
                }

                dlg.InitialDirectory = PathSearchText.BaseFolder;

                var result = dlg.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    try
                    {
                        IsSaveInProgress = true;
                        await Task.Run(() =>
                        {
                            switch (reportType)
                            {
                                case "Report":
                                    Utils.SaveResultsReport(SearchResults.GetList(), BooleanOperators, SearchFor, GetSearchOptions(), dlg.FileName);
                                    break;
                                case "Text":
                                    Utils.SaveResultsAsText(SearchResults.GetList(), dlg.FileName);
                                    break;
                                case "CSV":
                                    Utils.SaveResultsAsCSV(SearchResults.GetList(), dlg.FileName);
                                    break;
                            }
                        });

                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error creating results file");
                        MessageBox.Show(Resources.ThereWasAnErrorCreatingTheFile + App.LogDir,
                            Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsSaveInProgress = false;
                    }
                }
            }
        }

        private void ProcessCommands(CommandLineArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.ReportPath))
            {
                Utils.SaveResultsReport(SearchResults.GetList(), BooleanOperators, SearchFor, GetSearchOptions(), args.ReportPath);
            }
            if (!string.IsNullOrWhiteSpace(args.TextPath))
            {
                Utils.SaveResultsAsText(SearchResults.GetList(), args.TextPath);
            }
            if (!string.IsNullOrWhiteSpace(args.CsvPath))
            {
                Utils.SaveResultsAsCSV(SearchResults.GetList(), args.CsvPath);
            }
            if (args.Exit)
            {
                Application.Current.MainWindow.Close();
            }
        }

        public bool CanReplace
        {
            get
            {
                return PathSearchText.IsValidBaseFolder && FilesFound && CurrentGrepOperation == GrepOperation.None &&
                        !IsSaveInProgress && !string.IsNullOrEmpty(SearchFor) && SearchResults.GetWritableList().Count > 0 &&
                        // can only replace using the same parameters as was used for the search
                        !SearchParametersChanged &&
                        // if using boolean operators, only allow replace for plain text searches (not implemented for regex)
                        (BooleanOperators ? TypeOfSearch == SearchType.PlainText : true);
            }
        }

        public bool CanSortResults
        {
            get
            {
                return SearchResults.Count > 0 &&
                    CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy;
            }
        }

        private void SortResults()
        {
            if (!CanSortResults) return;

            var selections = SearchResults.SelectedItems;
            using (var d = Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                SearchResults.DeselectAllItems();
                var list = SearchResults.ToList();
                SearchResults.Clear();

                switch (sortType)
                {
                    case SortType.FileNameOnly:
                        list.Sort(new FileNameOnlyComparer(sortDirection));
                        break;
                    case SortType.FileTypeAndName:
                        list.Sort(new FileTypeAndNameComparer(sortDirection));
                        break;
                    case SortType.FileNameDepthFirst:
                    default:
                        list.Sort(new FileNameDepthFirstComparer(sortDirection));
                        break;
                    case SortType.FileNameBreadthFirst:
                        list.Sort(new FileNameBreadthFirstComparer(sortDirection));
                        break;
                    case SortType.Size:
                        list.Sort(new FileSizeComparer(sortDirection));
                        break;
                    case SortType.Date:
                        list.Sort(new FileDateComparer(sortDirection));
                        break;
                    case SortType.MatchCount:
                        list.Sort(new MatchCountComparer(sortDirection));
                        break;
                }

                SearchResults.AddRange(list);
            }
            SearchResults.SelectItems(selections);
        }

        private string GetSearchOptions()
        {
            StringBuilder sb = new StringBuilder();

            List<string> options = new List<string>();

            sb.Append(Resources.SearchFor).Append(" '").Append(SearchFor).AppendLine("'")
              .Append(Resources.Using).Append(" ").Append(TypeOfSearch.ToString().ToLower()).Append(" ").AppendLine(Resources.SearchL);

            if (CaseSensitive) options.Add(Resources.CaseSensitive);
            if (WholeWord) options.Add(Resources.WholeWord);
            if (Multiline) options.Add(Resources.Multiline);
            if (Singleline) options.Add(Resources.DotAsNewline);
            if (BooleanOperators) options.Add(Resources.BooleanOperators);
            if (SearchInResultsContent) options.Add(Resources.SearchInResults);
            if (StopAfterFirstMatch) options.Add(Resources.StopAfterFirstMatch);
            if (options.Count > 0)
                sb.AppendLine(string.Join(", ", options.ToArray()));
            sb.AppendLine();

            sb.Append(Resources.SearchIn).Append(": ").AppendLine(FileOrFolderPath)
              .Append(Resources.FilePattern).Append(" ").AppendLine(FilePattern);
            if (!string.IsNullOrWhiteSpace(FilePatternIgnore))
                sb.Append(Resources.ExcludePattern).Append(" ").AppendLine(FilePatternIgnore);
            if (TypeOfFileSearch == FileSearchType.Regex)
                sb.AppendLine(Resources.UsingRegexFilePattern);
            else if (TypeOfFileSearch == FileSearchType.Everything)
                sb.AppendLine(Resources.UsingEverythingIndexSearch);

            options.Clear();
            if (!IncludeSubfolder || (IncludeSubfolder && MaxSubfolderDepth == 0)) options.Add(Resources.NoSubfolders);
            if (IncludeSubfolder && MaxSubfolderDepth > 0) options.Add(string.Format(Resources.MaxFolderDepth, MaxSubfolderDepth));
            if (!IncludeHidden) options.Add(Resources.NoHiddenFiles);
            if (!IncludeBinary) options.Add(Resources.NoBinaryFiles);
            if (!IncludeArchive) options.Add(Resources.NoArchives);
            if (!FollowSymlinks) options.Add(Resources.NoSymlinks);
            if (options.Count > 0)
                sb.AppendLine(string.Join(", ", options.ToArray()));

            if (UseFileSizeFilter == FileSizeFilter.Yes)
                sb.AppendFormat(Resources.SizeFrom0To1KB, SizeFrom, SizeTo).AppendLine();

            if (UseFileDateFilter != FileDateFilter.None && TypeOfTimeRangeFilter == FileTimeRange.Dates)
                sb.AppendFormat(Resources.Type0DateFrom1To2, UseFileDateFilter.ToString(),
                    StartDate.HasValue ? StartDate.Value.ToShortDateString() : "*",
                    EndDate.HasValue ? EndDate.Value.ToShortDateString() : "*").AppendLine();

            if (UseFileDateFilter != FileDateFilter.None && TypeOfTimeRangeFilter == FileTimeRange.Hours)
                sb.AppendFormat(Resources.Type0DateInPast1To2Hours, UseFileDateFilter.ToString(), HoursFrom, HoursTo)
                  .AppendLine();

            if (CodePage != -1)
            {
                string encoding = Encodings.Where(r => r.Value == CodePage).Select(r => r.Key).FirstOrDefault();
                sb.Append(Resources.Encoding).Append(" ").AppendLine(encoding);
            }

            return sb.ToString();
        }

        private void Test()
        {
            try
            {
                SaveSettings();
                TestPattern testForm = new TestPattern();
                Point pt = new Point(40, 40);
                testForm.SetWindowPosition(pt, Application.Current.MainWindow);
                testForm.ShowDialog();
                LoadSettings();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error running regex");
                MessageBox.Show(Resources.ThereWasAnErrorRunningRegexTest + App.LogDir,
                    Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CheckVersion()
        {
            try
            {
                if (settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking))
                {
                    DateTime lastCheck = settings.Get<DateTime>(GrepSettings.Key.LastCheckedVersion);
                    TimeSpan duration = DateTime.Now.Subtract(lastCheck);
                    if (duration.TotalDays >= settings.Get<int>(GrepSettings.Key.UpdateCheckInterval))
                    {
                        var versionChk = new PublishedVersionExtractor();
                        string version = await versionChk.QueryLatestVersion();

                        if (!string.IsNullOrEmpty(version))
                        {
                            string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                            if (PublishedVersionExtractor.IsUpdateNeeded(currentVersion, version))
                            {
                                if (MessageBox.Show(string.Format(Resources.NewVersionOfDnGREP0IsAvailableForDownload, version) +
                                    Environment.NewLine + Resources.WouldYouLikeToDownloadItNow,
                                    Resources.DnGrep + "  " + Resources.NewVersion, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                {
                                    System.Diagnostics.Process.Start("http://dngrep.github.io/");
                                }
                            }
                        }

                        settings.Set<DateTime>(GrepSettings.Key.LastCheckedVersion, DateTime.Now);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failure in check version");
            }
        }

        private void ControlsInit()
        {
            this.workerSearchReplace.WorkerReportsProgress = true;
            this.workerSearchReplace.WorkerSupportsCancellation = true;
            this.workerSearchReplace.DoWork += DoSearchReplace;
            this.workerSearchReplace.RunWorkerCompleted += SearchComplete;
            this.workerSearchReplace.ProgressChanged += SearchProgressChanged;

            DiginesisHelpProvider.HelpNamespace = @"https://github.com/dnGrep/dnGrep/wiki/";
            DiginesisHelpProvider.ShowHelp = true;
        }

        private void PopulateEncodings()
        {
            KeyValuePair<string, int> defaultValue = new KeyValuePair<string, int>(Resources.EncodingAutoDetection, -1);

            List<KeyValuePair<string, int>> tempUni = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> tempEnc = new List<KeyValuePair<string, int>>();
            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                Encoding e = ei.GetEncoding();
                if (e.EncodingName.Contains(Resources.Unicode, StringComparison.OrdinalIgnoreCase))
                    tempUni.Add(new KeyValuePair<string, int>(e.EncodingName, e.CodePage));
                else
                    tempEnc.Add(new KeyValuePair<string, int>(e.EncodingName, e.CodePage));
            }

            tempUni.Sort(new KeyValueComparer());
            tempUni.Insert(0, defaultValue);
            tempEnc.Sort(new KeyValueComparer());
            Encodings.Clear();
            BookmarkViewModel.Encodings.Clear();
            foreach (var enc in tempUni.Concat(tempEnc))
            {
                Encodings.Add(enc);
                BookmarkViewModel.Encodings.Add(enc);
            }
        }

        private void BookmarkForm_UseBookmark(object sender, EventArgs e)
        {
            var bmk = bookmarkWindow.ViewModel.SelectedBookmark;
            if (bmk != null)
            {
                FilePattern = bmk.FilePattern;
                SearchFor = bmk.SearchFor;
                ReplaceWith = bmk.ReplaceWith;

                FilePatternIgnore = bmk.IgnoreFilePattern;
                TypeOfFileSearch = bmk.TypeOfFileSearch;
                IncludeSubfolder = bmk.IncludeSubfolders;
                MaxSubfolderDepth = bmk.MaxSubfolderDepth;
                IncludeHidden = bmk.IncludeHidden;
                IncludeBinary = bmk.IncludeBinary;
                IncludeArchive = bmk.IncludeArchive;
                FollowSymlinks = bmk.FollowSymlinks;
                UseGitignore = bmk.UseGitignore;
                CodePage = bmk.CodePage;

                TypeOfSearch = bmk.TypeOfSearch;
                CaseSensitive = bmk.CaseSensitive;
                WholeWord = bmk.WholeWord;
                Multiline = bmk.Multiline;
                Singleline = bmk.Singleline;
                BooleanOperators = bmk.BooleanOperators;
            }
        }

        private void ApplyBookmark(Bookmark bmk)
        {
            if (bmk != null)
            {
                FilePattern = bmk.FileNames;
                SearchFor = bmk.SearchPattern;
                ReplaceWith = bmk.ReplacePattern;

                if (bmk.Version > 1)
                {
                    FilePatternIgnore = bmk.IgnoreFilePattern;
                    TypeOfFileSearch = bmk.TypeOfFileSearch;
                    IncludeSubfolder = bmk.IncludeSubfolders;
                    MaxSubfolderDepth = bmk.MaxSubfolderDepth;
                    IncludeHidden = bmk.IncludeHiddenFiles;
                    IncludeBinary = bmk.IncludeBinaryFiles;
                    IncludeArchive = bmk.IncludeArchive;
                    FollowSymlinks = bmk.FollowSymlinks;
                    UseGitignore = bmk.UseGitignore;
                    CodePage = bmk.CodePage;

                    TypeOfSearch = bmk.TypeOfSearch;
                    CaseSensitive = bmk.CaseSensitive;
                    WholeWord = bmk.WholeWord;
                    Multiline = bmk.Multiline;
                    Singleline = bmk.Singleline;
                    BooleanOperators = bmk.BooleanOperators;
                }
            }
        }

        private void CopyBookmarksToSettings()
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

        private void PreviewFile(string filePath, GrepSearchResult result, int line)
        {
            if (PreviewFileContent)
            {
                string displayfileName = filePath;

                if (Utils.IsArchive(filePath))
                {
                    string tempFile = ArchiveDirectory.ExtractToTempFile(result);

                    if (string.IsNullOrWhiteSpace(tempFile))
                    {
                        MessageBox.Show(Resources.FailedToExtractFileFromArchive + App.LogDir,
                            Resources.DnGrep, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    else
                    {
                        displayfileName = result.FileNameDisplayed;
                        filePath = tempFile;
                    }
                }

                PreviewTitle = Path.GetFileName(displayfileName);

                PreviewModel.GrepResult = result;
                PreviewModel.LineNumber = line;
                PreviewModel.Encoding = result.Encoding;
                PreviewModel.DisplayFileName = displayfileName;
                PreviewModel.FilePath = filePath;

                if (!IsPreviewDocked)
                    PreviewShow?.Invoke(this, EventArgs.Empty);
            }
        }
        #endregion
    }
}
