using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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
using dnGREP.DockFloat;
using dnGREP.Engines;
using dnGREP.Localization;
using dnGREP.WPF.MVHelpers;
using dnGREP.WPF.Properties;
using Microsoft.Win32;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public partial class MainViewModel : BaseMainViewModel, IFileDragDropTarget
    {
        public event EventHandler PreviewHide;
        public event EventHandler PreviewShow;

        private Brush highlightForeground;
        private Brush highlightBackground;

        private readonly string enQuad = char.ConvertFromUtf32(0x2000);

        public MainViewModel()
            : base()
        {
            if (Application.Current != null)
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
                _previewAutoPosition = Settings.Get<bool>(GrepSettings.Key.PreviewAutoPosition);

                SearchResults.GrepLineSelected += SearchResults_GrepLineSelected;
                SearchResults.PreviewFileLineRequest += SearchResults_PreviewFileLineRequest;
                SearchResults.PreviewFileRequest += SearchResults_PreviewFileRequest;
                SearchResults.OpenFileLineRequest += SearchResults_OpenFileLineRequest;
                SearchResults.OpenFileRequest += SearchResults_OpenFileRequest;
                SearchResults.CollectionChanged += SearchResults_CollectionChanged;

                CheckVersion();
                ControlsInit();
                PopulateEncodings();
                PopulateScripts();

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

                TranslationSource.Instance.CurrentCultureChanged += CurrentCultureChanged;

                PropertyChanged += OnMainViewModel_PropertyChanged;

                idleTimer.Interval = TimeSpan.FromMilliseconds(250);
                idleTimer.Tick += IdleTimer_Tick;
            }
        }

        private void CurrentCultureChanged(object sender, EventArgs e)
        {
            PreviewModel.FilePath = string.Empty;
            PreviewTitle = string.Empty;

            // reload the Encodings list, the "Auto" encoding name (at least) has changed languages
            int value = CodePage;
            CodePage = -2;
            PopulateEncodings();
            CodePage = value;

            // this call will repopulate the FileFiltersSummary
            // IncludeSubfolder didn't really change, but triggers the refresh
            UpdateState(nameof(IncludeSubfolder));
            // this call will update the validation message, if visible
            UpdateState(nameof(TypeOfSearch));
            // this call will update the Folder/Everything label
            UpdateState(nameof(TypeOfFileSearch));
            // this call will update the window title
            UpdateState(nameof(FileOrFolderPath));

            base.OnPropertyChanged(nameof(IsBookmarkedTooltip));
            base.OnPropertyChanged(nameof(IsFolderBookmarkedTooltip));
            base.OnPropertyChanged(nameof(ResultOptionsButtonTooltip));

            StatusMessage = string.Empty;
            ClearMatchCountStatus();
            SearchResults.Clear();
            UpdateReplaceButtonTooltip(true);
        }

        internal bool Closing()
        {
            if (bookmarkWindow != null)
            {
                bookmarkWindow.UseBookmark -= BookmarkForm_UseBookmark;
                bookmarkWindow.Close();
            }

            while (scriptEditorWindows.Count > 0)
            {
                var wnd = scriptEditorWindows[scriptEditorWindows.Count - 1];
                if (!wnd.ConfirmSave())
                {
                    return false;
                }
                wnd.Close();
            }

            return true;
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
            if (!e.FormattedGrepResult.GrepResult.IsHexFile)
                PreviewFile(e.FormattedGrepResult);
        }

        void SearchResults_PreviewFileLineRequest(object sender, GrepLineEventArgs e)
        {
            if (!e.FormattedGrepLine.GrepLine.IsHexFile)
                PreviewFile(e.FormattedGrepLine);
        }

        private void SearchResults_GrepLineSelected(object sender, GrepLineSelectEventArgs e)
        {
            if (e.FormattedGrepLine == null)
            {
                StatusMessage2 = string.Empty;
                StatusMessage3 = string.Empty;
                StatusMessage4 = string.Empty;
            }
            else
            {

                StatusMessage2 = $"({e.LineMatchCount})";
                StatusMessage3 = $"{e.MatchOrdinal}/{e.FileMatchCount}";
                StatusMessage2Tooltip = Resources.Main_StatusTooltip_MatchCountOnLine;
                StatusMessage3Tooltip = Resources.Main_StatusTooltip_MatchNumberMatchCountInFile;

                if (totalMatchCount > 0)
                {
                    int preceedingMatchCount = GetPreceedingMatchCount(e.FormattedGrepLine.Parent);
                    StatusMessage4 = $"{e.MatchOrdinal + preceedingMatchCount}/{totalMatchCount}";
                    StatusMessage4Tooltip = Resources.Main_StatusTooltip_MatchNumberMatchCountOverall;
                }
                else
                {
                    StatusMessage4 = string.Empty;
                    StatusMessage4Tooltip = string.Empty;
                }

            }
        }

        private int GetPreceedingMatchCount(FormattedGrepResult formattedGrepResult)
        {
            if (formattedGrepResult == null || formattedGrepResult.GrepResult == null)
            {
                return 0;
            }

            if (!preceedingMatches.TryGetValue(formattedGrepResult.GrepResult.Id, out int count))
            {
                count = 0;
                foreach (var item in SearchResults)
                {
                    if (item == formattedGrepResult)
                    {
                        break;
                    }
                    count += item.Matches;
                }
                preceedingMatches.Add(formattedGrepResult.GrepResult.Id, count);
            }
            return count;
        }

        private void SearchResults_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SearchResults.Count == 0 && PreviewFileContent)
            {
                // clear the preview
                PreviewModel.FilePath = string.Empty;
                PreviewTitle = string.Empty;
            }
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
        private readonly Dictionary<string, int> preceedingMatches = new Dictionary<string, int>();
        private int totalMatchCount;
        private string latestStatusMessage;

        #endregion

        #region Properties

        public Window ParentWindow { get; set; }

        public PreviewViewModel PreviewModel { get; internal set; }

        #endregion

        #region Presentation Properties

        private double mainFormfontSize;
        public double MainFormFontSize
        {
            get { return mainFormfontSize; }
            set
            {
                if (mainFormfontSize == value)
                    return;

                mainFormfontSize = value;
                base.OnPropertyChanged(nameof(MainFormFontSize));
            }
        }

        public ObservableCollection<MenuItemViewModel> ScriptMenuItems { get; } = new ObservableCollection<MenuItemViewModel>();

        private bool isBookmarked;
        public bool IsBookmarked
        {
            get { return isBookmarked; }
            set
            {
                if (value == isBookmarked)
                    return;

                isBookmarked = value;

                base.OnPropertyChanged(nameof(IsBookmarked));
                base.OnPropertyChanged(nameof(IsBookmarkedTooltip));
            }
        }

        public string IsBookmarkedTooltip
        {
            get
            {
                if (!IsBookmarked)
                    return Resources.Main_AddSearchPatternToBookmarks;
                else
                    return Resources.Main_ClearBookmark;
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

                base.OnPropertyChanged(nameof(IsFolderBookmarked));
                base.OnPropertyChanged(nameof(IsFolderBookmarkedTooltip));
            }
        }

        public string IsFolderBookmarkedTooltip
        {
            get
            {
                if (!IsFolderBookmarked)
                    return Resources.Main_AssociateBookmarkWithFolder;
                else
                    return Resources.Main_RemoveFolderFromBookmarkAssociation;
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
                base.OnPropertyChanged(nameof(PreviewTitle));
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
                base.OnPropertyChanged(nameof(PreviewWindowBounds));
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
                base.OnPropertyChanged(nameof(PreviewWindowState));
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
                base.OnPropertyChanged(nameof(IsPreviewDocked));
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
                base.OnPropertyChanged(nameof(PreviewAutoPosition));
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
                base.OnPropertyChanged(nameof(PreviewDockSide));
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
                base.OnPropertyChanged(nameof(IsPreviewHidden));

                if (IsPreviewHidden)
                {
                    PreviewFileContent = false;
                }
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
                base.OnPropertyChanged(nameof(PreviewDockedWidth));
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
                base.OnPropertyChanged(nameof(PreviewDockedHeight));
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
                    base.OnPropertyChanged(nameof(SortType));
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
                    base.OnPropertyChanged(nameof(SortDirection));
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
                base.OnPropertyChanged(nameof(HighlightsOn));
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

                base.OnPropertyChanged(nameof(ShowLinesInContext));
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

                base.OnPropertyChanged(nameof(ContextLinesBefore));
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

                base.OnPropertyChanged(nameof(ContextLinesAfter));
            }
        }


        private bool isResultTreeMaximized = false;
        public bool IsResultTreeMaximized
        {
            get { return isResultTreeMaximized; }
            set
            {
                if (isResultTreeMaximized == value)
                {
                    return;
                }

                isResultTreeMaximized = value;
                OnPropertyChanged(nameof(IsResultTreeMaximized));
                OnPropertyChanged(nameof(MaximizeResultsTreeButtonTooltip));
            }
        }


        public string MaximizeResultsTreeButtonTooltip
        {
            get
            {
                if (IsResultTreeMaximized)
                {
                    return Resources.Main_RestoreResults;
                }
                else
                {
                    return Resources.Main_MaximizeResults;
                }
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

                base.OnPropertyChanged(nameof(IsResultOptionsExpanded));
                base.OnPropertyChanged(nameof(ResultOptionsButtonTooltip));
            }
        }

        public string ResultOptionsButtonTooltip
        {
            get
            {
                if (IsResultOptionsExpanded)
                    return Resources.Main_HideResultOptions;
                else
                    return Resources.Main_ShowResultOptions;
            }
        }

        private string replaceButtonToolTip = string.Empty;
        public string ReplaceButtonToolTip
        {
            get { return replaceButtonToolTip; }
            set
            {
                if (replaceButtonToolTip == value)
                {
                    return;
                }

                replaceButtonToolTip = value;
                OnPropertyChanged(nameof(ReplaceButtonToolTip));
            }
        }


        private bool replaceButtonToolTipVisible = false;
        public bool ReplaceButtonToolTipVisible
        {
            get { return replaceButtonToolTipVisible; }
            set
            {
                if (replaceButtonToolTipVisible == value)
                {
                    return;
                }

                replaceButtonToolTipVisible = value;
                OnPropertyChanged(nameof(ReplaceButtonToolTipVisible));
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

        private string statusMessage2;
        public string StatusMessage2
        {
            get { return statusMessage2; }
            set
            {
                if (value == statusMessage2)
                    return;

                statusMessage2 = value;
                base.OnPropertyChanged(nameof(StatusMessage2));
            }
        }

        private string statusMessage2tooltip;
        public string StatusMessage2Tooltip
        {
            get { return statusMessage2tooltip; }
            set
            {
                if (value == statusMessage2tooltip)
                    return;

                statusMessage2tooltip = value;
                base.OnPropertyChanged(nameof(StatusMessage2Tooltip));
            }
        }

        private string statusMessage3;
        public string StatusMessage3
        {
            get { return statusMessage3; }
            set
            {
                if (value == statusMessage3)
                    return;

                statusMessage3 = value;
                base.OnPropertyChanged(nameof(StatusMessage3));
            }
        }

        private string statusMessage3tooltip;
        public string StatusMessage3Tooltip
        {
            get { return statusMessage3tooltip; }
            set
            {
                if (value == statusMessage3tooltip)
                    return;

                statusMessage3tooltip = value;
                base.OnPropertyChanged(nameof(StatusMessage3Tooltip));
            }
        }

        private string statusMessage4;
        public string StatusMessage4
        {
            get { return statusMessage4; }
            set
            {
                if (value == statusMessage4)
                    return;

                statusMessage4 = value;
                base.OnPropertyChanged(nameof(StatusMessage4));
            }
        }

        private string statusMessage4tooltip;
        public string StatusMessage4Tooltip
        {
            get { return statusMessage4tooltip; }
            set
            {
                if (value == statusMessage4tooltip)
                    return;

                statusMessage4tooltip = value;
                base.OnPropertyChanged(nameof(StatusMessage4Tooltip));
            }
        }

        private void ClearMatchCountStatus()
        {
            StatusMessage2 = string.Empty;
            StatusMessage3 = string.Empty;
            StatusMessage4 = string.Empty;

            StatusMessage2Tooltip = string.Empty;
            StatusMessage3Tooltip = string.Empty;
            StatusMessage4Tooltip = string.Empty;

            preceedingMatches.Clear();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Returns an undo command
        /// </summary>
        public ICommand UndoCommand => new RelayCommand(
            param => Undo(),
            param => CanUndo);

        /// <summary>
        /// Returns an options command
        /// </summary>
        public ICommand OptionsCommand => new RelayCommand(
            param => ShowOptions());

        /// <summary>
        /// Returns a help command
        /// </summary>
        public ICommand HelpCommand => new RelayCommand(
            param => ShowHelp());

        /// <summary>
        /// Returns an about command
        /// </summary>
        public ICommand AboutCommand => new RelayCommand(
            param => ShowAbout());

        public ICommand CheckForUpdatesCommand => new RelayCommand(
            param => CheckForUpdates(true));

        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BrowseCommand => new RelayCommand(
            param => Browse());

        /// <summary>
        /// Returns a command that starts a search.
        /// </summary>
        public ICommand SearchCommand => new RelayCommand(
            param => Search(),
            param => CanSearch);

        /// <summary>
        /// Returns a command that starts a search in results.
        /// </summary>
        public ICommand ReplaceCommand => new RelayCommand(
            param => Replace(),
            param => CanReplace);

        /// <summary>
        /// Returns a command that sorts the results.
        /// </summary>
        public ICommand SortCommand => new RelayCommand(
            param => SortResults(),
            param => CanSortResults);

        /// <summary>
        /// Returns a command that copies files
        /// </summary>
        public ICommand CopyFilesCommand => new RelayCommand(
            param => CopyFiles(param));

        /// <summary>
        /// Returns a command that moves files
        /// </summary>
        public ICommand MoveFilesCommand => new RelayCommand(
            param => MoveFiles(param));

        /// <summary>
        /// Returns a command that deletes files
        /// </summary>
        public ICommand DeleteFilesCommand => new RelayCommand(
            param => DeleteFiles());

        /// <summary>
        /// Returns a command that copies content to clipboard
        /// </summary>
        public ICommand CopyToClipboardCommand => new RelayCommand(
            param => CopyToClipboard());

        /// <summary>
        /// Returns a command that opens the report options window
        /// </summary>
        public ICommand ReportOptions => new RelayCommand(
            p => ShowReportOptions());

        /// <summary>
        /// Returns a command that copies content to clipboard
        /// </summary>
        public ICommand SaveResultsCommand => new RelayCommand(
            param => SaveResultsToFile(param as string));

        /// <summary>
        /// Returns a command that copies matching lines to clipboard
        /// </summary>
        public ICommand CopyMatchingLinesCommand => new RelayCommand(
            param => CopyResults());

        /// <summary>
        /// Returns a command that cancels search
        /// </summary>
        public ICommand CancelCommand => new RelayCommand(
            param => Cancel(),
            param => CanCancel);

        /// <summary>
        /// Returns a command that toggles match highlights
        /// </summary>
        public ICommand HighlightsCommand => new RelayCommand(
            param => ToggleHighlights());

        /// <summary>
        /// Returns a command that opens test view
        /// </summary>
        public ICommand TestCommand => new RelayCommand(
            param => Test());

        public ICommand BookmarkAddCommand => new RelayCommand(
            param => BookmarkAddRemove(false));

        public ICommand FolderBookmarkAddCommand => new RelayCommand(
            param => BookmarkAddRemove(true));

        /// <summary>
        /// Returns a command that opens the bookmarks window
        /// </summary>
        public ICommand OpenBookmarksWindowCommand => new RelayCommand(
            param => OpenBookmarksWindow());

        /// <summary>
        /// Returns a command that resets the search options.
        /// </summary>
        public ICommand ResetOptionsCommand => new RelayCommand(
            param => ResetOptions());

        /// <summary>
        /// Returns a command that resets the search options.
        /// </summary>
        public ICommand ToggleFileOptionsCommand => new RelayCommand(
            param => IsFiltersExpanded = !IsFiltersExpanded);

        /// <summary>
        /// Returns a command that reloads the current theme file.
        /// </summary>
        public ICommand ReloadThemeCommand => new RelayCommand(
            param => AppTheme.Instance.ReloadCurrentTheme());

        public ICommand ToggleResultsMaximizeCommand => new RelayCommand(
            p => IsResultTreeMaximized = !IsResultTreeMaximized);

        public ICommand OpenAppDataCommand => new RelayCommand(
            p => OpenAppDataFolder(),
            q => true);

        #endregion

        #region Public Methods

        public void OnFileDrop(bool append, string[] filePaths)
        {
            string paths = append ? FileOrFolderPath : string.Empty;

            bool everythingSearch = TypeOfFileSearch == FileSearchType.Everything;
            if (append && everythingSearch && FileOrFolderPath.Trim('\"') != PathSearchText.BaseFolder)
            {
                // for anything but a simple path, do not append in Everything mode
                paths = string.Empty;
            }

            string separator = everythingSearch ? " | " : ";";

            foreach (string path in filePaths)
            {
                if (!string.IsNullOrEmpty(paths))
                    paths += separator;

                var part = UiUtils.QuoteIfNeeded(path);
                if (everythingSearch)
                {
                    part = UiUtils.QuoteIfIncludesSpaces(part);
                }
                paths += part;
            }

            SetFileOrFolderPath(paths);
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
                    else if (IsPreviewHidden && !IsPreviewDocked)
                    {
                        PreviewShow?.Invoke(this, EventArgs.Empty);
                    }
                }
                else if (!IsPreviewHidden)
                {
                    PreviewHide?.Invoke(this, EventArgs.Empty);
                }
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
            ResultsFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ResultsFontFamily);
            ResultsFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.ResultsFontSize);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);

            PersonalizationOn = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PersonalizationOn);
            UpdatePersonalization();

            if (PreviewModel != null)
            {
                PreviewModel.ApplicationFontFamily = ApplicationFontFamily;
                PreviewModel.MainFormFontSize = MainFormFontSize;
                PreviewModel.ResultsFontFamily = ResultsFontFamily;
                PreviewModel.UpdatePersonalization(PersonalizationOn);
            }
        }

        public override void SaveSettings()
        {
            CopyBookmarksToSettings();

            Settings.Set(GrepSettings.Key.SortDirection, SortDirection);
            Settings.Set(GrepSettings.Key.TypeOfSort, SortType);
            Settings.Set(GrepSettings.Key.ShowResultOptions, IsResultOptionsExpanded);
            Settings.Set(GrepSettings.Key.ResultsTreeScale, SearchResults.ResultsScale);
            Settings.Set(GrepSettings.Key.ResultsTreeWrap, SearchResults.WrapText);
            Settings.Set(GrepSettings.Key.HighlightMatches, HighlightsOn);
            Settings.Set(GrepSettings.Key.ShowLinesInContext, ShowLinesInContext);
            Settings.Set(GrepSettings.Key.ContextLinesBefore, ContextLinesBefore);
            Settings.Set(GrepSettings.Key.ContextLinesAfter, ContextLinesAfter);
            Settings.Set(GrepSettings.Key.PersonalizationOn, PersonalizationOn);

            LayoutProperties.PreviewBounds = PreviewWindowBounds;
            LayoutProperties.PreviewWindowState = PreviewWindowState;
            LayoutProperties.PreviewDocked = IsPreviewDocked;
            LayoutProperties.PreviewDockSide = PreviewDockSide.ToString();
            LayoutProperties.PreviewDockedWidth = PreviewDockedWidth;
            LayoutProperties.PreviewDockedHeight = PreviewDockedHeight;
            LayoutProperties.PreviewHidden = IsPreviewHidden;

            Settings.Set(GrepSettings.Key.PreviewAutoPosition, PreviewAutoPosition);

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
                    useCustomEditor, Settings.Get<string>(GrepSettings.Key.CustomEditor),
                    Settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                bool isInArchive = Utils.IsArchive(result.GrepResult.FileNameReal);
                if (isInArchive)
                {
                    ArchiveDirectory.OpenFile(fileArg);
                }
                else
                {
                    IGrepEngine engine = GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, GrepEngineInitParams.Default, new FileFilter(), TypeOfSearch);
                    if (engine != null)
                    {
                        engine.OpenFile(fileArg);
                        GrepEngineFactory.ReturnToPool(result.GrepResult.FileNameReal, engine);
                    }
                    if (fileArg.UseBaseEngine)
                        Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, matchText, columnNumber,
                            useCustomEditor, Settings.Get<string>(GrepSettings.Key.CustomEditor),
                            Settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to open file.");
                if (useCustomEditor)
                {
                    MessageBox.Show(Resources.MessageBox_CustomEditorFileOpenError + Environment.NewLine +
                        Resources.MessageBox_CheckEditorPath,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
                else
                {
                    MessageBox.Show(Resources.MessageBox_ErrorOpeningFile + App.LogDir,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
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
                    useCustomEditor, Settings.Get<string>(GrepSettings.Key.CustomEditor),
                    Settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                if (Utils.IsArchive(result.GrepResult.FileNameReal))
                {
                    ArchiveDirectory.OpenFile(fileArg);
                }
                else
                {
                    IGrepEngine engine = GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, GrepEngineInitParams.Default, new FileFilter(), TypeOfSearch);
                    if (engine != null)
                    {
                        engine.OpenFile(fileArg);
                        GrepEngineFactory.ReturnToPool(result.GrepResult.FileNameReal, engine);
                    }
                    if (fileArg.UseBaseEngine)
                        Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, matchText, columnNumber,
                            useCustomEditor, Settings.Get<string>(GrepSettings.Key.CustomEditor),
                            Settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to open file.");
                if (useCustomEditor)
                {
                    MessageBox.Show(Resources.MessageBox_CustomEditorFileOpenError + Environment.NewLine +
                        Resources.MessageBox_CheckEditorPath,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
                else
                {
                    MessageBox.Show(Resources.MessageBox_ErrorOpeningFile + App.LogDir,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
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
            if (e.Argument is SearchReplaceCriteria param && !workerSearchReplace.CancellationPending)
            {
                try
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

                        IEnumerable<FileData> fileInfos = null;
                        IEnumerable<string> files = null;

                        Utils.CancelSearch = false;

                        FileFilter fileParams = new FileFilter(FileOrFolderPath, filePatternInclude, filePatternExclude,
                            param.TypeOfFileSearch == FileSearchType.Regex, param.UseGitIgnore, param.TypeOfFileSearch == FileSearchType.Everything,
                            param.IncludeSubfolder, param.MaxSubfolderDepth, param.IncludeHidden, param.IncludeBinary, param.IncludeArchive,
                            param.FollowSymlinks, sizeFrom, sizeTo, param.UseFileDateFilter, startTime, endTime,
                            SkipRemoteCloudStorageFiles);

                        if (string.IsNullOrEmpty(SearchFor) &&
                            Settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern))
                        {
                            fileInfos = Utils.GetFileListIncludingArchives(fileParams);
                        }
                        else if (param.Operation == GrepOperation.SearchInResults)
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
                                if (IsScriptRunning)
                                {
                                    logger.Error(Resources.MessageBox_IncorrectPattern + regException.Message);
                                    AddScriptMessage(Resources.MessageBox_IncorrectPattern + regException.Message);
                                }
                                else
                                {
                                    MessageBox.Show(Resources.MessageBox_IncorrectPattern + regException.Message,
                                        Resources.MessageBox_DnGrep,
                                        MessageBoxButton.OK, MessageBoxImage.Warning,
                                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                                }
                                e.Result = null;
                                return;
                            }
                        }

                        GrepCore grep = new GrepCore();
                        grep.SearchParams = new GrepEngineInitParams(
                            Settings.Get<bool>(GrepSettings.Key.ShowLinesInContext),
                            Settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                            Settings.Get<int>(GrepSettings.Key.ContextLinesAfter),
                            Settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                            Settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
                            SearchParallel);

                        grep.FileFilter = new FileFilter(FileOrFolderPath, filePatternInclude, filePatternExclude,
                            param.TypeOfFileSearch == FileSearchType.Regex, param.UseGitIgnore, param.TypeOfFileSearch == FileSearchType.Everything,
                            param.IncludeSubfolder, param.MaxSubfolderDepth, param.IncludeHidden, param.IncludeBinary, param.IncludeArchive,
                            param.FollowSymlinks, sizeFrom, sizeTo, param.UseFileDateFilter, startTime, endTime,
                            SkipRemoteCloudStorageFiles);

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

                        if (UseGitignore)
                        {
                            // this will be the first search performed, and may take a long time
                            // the message will allow the user to see the cost of the operation
                            StatusMessage = Resources.Main_Status_SearchingForGitignore;
                        }

                        grep.ProcessedFile += GrepCore_ProcessedFile;

                        if (CaptureGroupSearch && param.TypeOfFileSearch == FileSearchType.Regex &&
                            !string.IsNullOrEmpty(param.SearchFor) && files != null)
                        {
                            e.Result = grep.CaptureGroupSearch(files, filePatternInclude, searchOptions, param.TypeOfSearch, param.SearchFor, param.CodePage);
                        }
                        else if (files != null)
                        {
                            e.Result = grep.Search(files, param.TypeOfSearch, param.SearchFor, searchOptions, param.CodePage);
                        }
                        else if (fileInfos != null)
                        {
                            e.Result = grep.ListFiles(fileInfos, searchOptions, param.CodePage);
                        }
                        grep.ProcessedFile -= GrepCore_ProcessedFile;
                    }
                    else
                    {
                        GrepCore grep = new GrepCore();
                        grep.SearchParams = new GrepEngineInitParams(
                            Settings.Get<bool>(GrepSettings.Key.ShowLinesInContext),
                            Settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                            Settings.Get<int>(GrepSettings.Key.ContextLinesAfter),
                            Settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                            Settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
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
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed in search/replace");
                    bool isSearch = true;

                    if (param.Operation == GrepOperation.Search || param.Operation == GrepOperation.SearchInResults)
                        isSearch = true;
                    else
                        isSearch = false;

                    if (isSearch)
                    {
                        if (IsScriptRunning)
                        {
                            AddScriptMessage(Resources.MessageBox_SearchFailedError + App.LogDir);
                        }
                        else
                        {
                            MessageBox.Show(Resources.MessageBox_SearchFailedError + App.LogDir,
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                    else
                    {
                        if (IsScriptRunning)
                        {
                            AddScriptMessage(Resources.MessageBox_ReplaceFailedError + App.LogDir);
                        }
                        else
                        {
                            MessageBox.Show(Resources.MessageBox_ReplaceFailedError + App.LogDir,
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                }
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
                if (IsScriptRunning)
                {
                    AddScriptMessage(Resources.MessageBox_SearchOrReplaceFailed + App.LogDir);
                }
                else
                {
                    MessageBox.Show(Resources.MessageBox_SearchOrReplaceFailed + App.LogDir,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
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
                    latestStatusMessage = TranslationSource.Format(Resources.Main_Status_Searched0FilesFound1MatchingFilesProcessing2,
                        progress.ProcessedFiles, progress.SuccessfulFiles, fileName);
                }
                else
                {
                    latestStatusMessage = TranslationSource.Format(Resources.Main_Status_Searched0FilesFound1MatchingFiles,
                        progress.ProcessedFiles, progress.SuccessfulFiles);
                }

                if (SearchResults.Count > 0 && GrepSettings.Instance.Get<bool>(GrepSettings.Key.MaximizeResultsTreeOnSearch))
                {
                    IsResultTreeMaximized = true;
                }
            }
        }

        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(latestStatusMessage))
                StatusMessage = latestStatusMessage;
        }

        private void SearchReplaceComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            latestStatusMessage = string.Empty;
            idleTimer.Stop();
            try
            {
                if (CurrentGrepOperation == GrepOperation.Search || CurrentGrepOperation == GrepOperation.SearchInResults)
                {
                    if (e.Result == null)
                    {
                        StatusMessage = Resources.Main_Status_SearchCanceledOrFailed;
                    }
                    else if (!e.Cancelled)
                    {
                        TimeSpan duration = DateTime.Now.Subtract(timer);
                        int successFileCount = 0;
                        totalMatchCount = 0;
                        if (e.Result is List<GrepSearchResult> results)
                        {
                            successFileCount = results.Where(r => r.IsSuccess).Count();
                            totalMatchCount = results.Where(r => r.IsSuccess).SelectMany(r => r.Matches).Count();
                        }

                        StatusMessage = TranslationSource.Format(Resources.Main_Status_SearchCompletedIn0_1MatchesFoundIn2FilesOf3Searched,
                            duration.GetPrettyString(), totalMatchCount, successFileCount, processedFiles);

                        if (IsEverythingSearchMode && Everything.EverythingSearch.CountMissingFiles > 0)
                        {
                            StatusMessage += enQuad + TranslationSource.Format(Resources.Main_Status_Excluded0MissingFiles, Everything.EverythingSearch.CountMissingFiles);
                        }
                        logger.Info($"{StatusMessage} {Resources.Main_SearchFor} {SearchFor}\t{duration.GetPrettyString()}\t{totalMatchCount}\t{successFileCount}\t{processedFiles}");
                    }
                    else
                    {
                        StatusMessage = Resources.Main_Status_SearchCanceled;
                    }

                    FilesFound = SearchResults.Count > 0;
                    CurrentGrepOperation = GrepOperation.None;
                    base.OnPropertyChanged(nameof(CurrentGrepOperation));
                    CanSearch = true;
                    UpdateReplaceButtonTooltip(false);

                    if (FilesFound && GrepSettings.Instance.Get<bool>(GrepSettings.Key.MaximizeResultsTreeOnSearch))
                    {
                        IsResultTreeMaximized = true;
                    }

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
                            StatusMessage = Resources.Main_Status_ReplaceFailed;

                            if (IsScriptRunning)
                            {
                                AddScriptMessage(Resources.MessageBox_ReplaceFailedError + App.LogDir);
                            }
                            else
                            {
                                MessageBox.Show(Resources.MessageBox_ReplaceFailedError + App.LogDir,
                                    Resources.MessageBox_DnGrep,
                                    MessageBoxButton.OK, MessageBoxImage.Error,
                                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                            }
                        }
                        else
                        {
                            StatusMessage = TranslationSource.Format(Resources.Main_Status_ReplaceComplete0FilesReplaced,
                                (int)e.Result);
                            CanUndo = undoList.Count > 0;
                        }
                    }
                    else
                    {
                        StatusMessage = Resources.Main_Status_ReplaceCanceled;
                    }
                    CurrentGrepOperation = GrepOperation.None;
                    base.OnPropertyChanged(nameof(CurrentGrepOperation));
                    CanSearch = true;
                    ClearMatchCountStatus();
                    SearchResults.Clear();
                    UpdateReplaceButtonTooltip(false);
                }

                string outdatedEngines = dnGREP.Engines.GrepEngineFactory.GetListOfFailedEngines();
                if (!string.IsNullOrEmpty(outdatedEngines))
                {
                    if (IsScriptRunning)
                    {
                        logger.Error(Resources.MessageBox_TheFollowingPluginsFailedToLoad +
                            Environment.NewLine + Environment.NewLine +
                            outdatedEngines + Environment.NewLine + Environment.NewLine +
                            Resources.MessageBox_DefaultEngineWasUsedInstead);
                        AddScriptMessage(Resources.MessageBox_TheFollowingPluginsFailedToLoad +
                            Environment.NewLine + Environment.NewLine +
                            outdatedEngines + Environment.NewLine + Environment.NewLine +
                            Resources.MessageBox_DefaultEngineWasUsedInstead);
                    }
                    else
                    {
                        MessageBox.Show(Resources.MessageBox_TheFollowingPluginsFailedToLoad +
                            Environment.NewLine + Environment.NewLine +
                            outdatedEngines + Environment.NewLine + Environment.NewLine +
                            Resources.MessageBox_DefaultEngineWasUsedInstead,
                            Resources.MessageBox_DnGrep + " " + Resources.MessageBox_PluginErrors,
                            MessageBoxButton.OK, MessageBoxImage.Warning,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failure in search complete update");
                if (IsScriptRunning)
                {
                    AddScriptMessage(Resources.MessageBox_SearchOrReplaceFailed + App.LogDir);
                }
                else
                {
                    MessageBox.Show(Resources.MessageBox_SearchOrReplaceFailed + App.LogDir,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
            }
            finally
            {
                if (Utils.CancelSearch && IsScriptRunning)
                {
                    CancelScript();
                }

                Utils.CancelSearch = false;
                currentSearchFiles.Clear();

                // try to move on to next script statement
                ContinueScript();
            }
        }

        void GrepCore_ProcessedFile(object sender, ProgressStatus progress)
        {
            workerSearchReplace.ReportProgress((int)progress.ProcessedFiles, progress);
        }

        private void Browse()
        {
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
                if (fileFolderDialog.HasMultiSelectedFiles)
                {
                    newPath = fileFolderDialog.GetSelectedPaths(
                        TypeOfFileSearch == FileSearchType.Everything ? " | " : ";");
                }
                else
                {
                    newPath = fileFolderDialog.SelectedPath;
                }

                SetFileOrFolderPath(newPath);
            }
        }

        private void Search()
        {
            if (CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
            {
                // first, check for valid path
                if (!PathSearchText.IsValidPath)
                {
                    if (IsScriptRunning)
                    {
                        logger.Error(TranslationSource.Format(Resources.MessageBox_SearchPathInTheFieldIsNotValid, SearchTextBoxLabel));
                        AddScriptMessage(TranslationSource.Format(Resources.MessageBox_SearchPathInTheFieldIsNotValid, SearchTextBoxLabel));

                        CancelScript();
                    }
                    else
                    {
                        MessageBox.Show(TranslationSource.Format(Resources.MessageBox_SearchPathInTheFieldIsNotValid, SearchTextBoxLabel),
                            Resources.MessageBox_DnGrep,
                            MessageBoxButton.OK, MessageBoxImage.Warning,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                    return;
                }

                if (!IsValidPattern)
                {
                    if (IsScriptRunning)
                    {
                        logger.Error(ValidationMessage + Environment.NewLine + ValidationToolTip);
                        AddScriptMessage(ValidationMessage + Environment.NewLine + ValidationToolTip);

                        CancelScript();
                    }
                    else
                    {
                        MessageBox.Show(ValidationMessage + Environment.NewLine + ValidationToolTip,
                            Resources.MessageBox_DnGrep,
                            MessageBoxButton.OK, MessageBoxImage.Warning,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                    return;
                }

                SaveSettings();

                if (TypeOfFileSearch == FileSearchType.Regex)
                {
                    if (!ValidateFilePatterns())
                    {
                        if (IsScriptRunning)
                        {
                            CancelScript();
                        }
                        return;
                    }
                }

                // set base folder for results display
                SearchResults.FolderPath = PathSearchText.BaseFolder;
                SearchResults.TypeOfSearch = TypeOfSearch;

                if (SearchInResultsContent && CanSearchInResults)
                    CurrentGrepOperation = GrepOperation.SearchInResults;
                else
                    CurrentGrepOperation = GrepOperation.Search;
                StatusMessage = Resources.Main_Status_Searching;
                totalMatchCount = 0;

                PreviewModel.FilePath = string.Empty;
                PreviewTitle = string.Empty;
                // clear temp files from the previous search
                Utils.DeleteTempFolder();

                SearchReplaceCriteria workerParams = new SearchReplaceCriteria(this);
                if (SearchInResultsContent && CanSearchInResults)
                {
                    List<string> foundFiles = new List<string>();
                    foreach (FormattedGrepResult n in SearchResults) foundFiles.Add(n.GrepResult.FileNameReal);
                    workerParams.AddSearchFiles(foundFiles);
                }

                UpdateBookmarks();
                SearchParametersChanged = false;

                ClearMatchCountStatus();
                SearchResults.Clear();
                UpdateReplaceButtonTooltip(true);
                processedFiles = 0;
                idleTimer.Start();
                workerSearchReplace.RunWorkerAsync(workerParams);
                // toggle value to move focus to the results tree, and enable keyboard actions on the tree
                SearchResults.IsResultsTreeFocused = false;
                SearchResults.IsResultsTreeFocused = true;
            }
            else if (IsScriptRunning)
            {
                // in a bad state, do not continue
                CancelScript();
                AddScriptMessage("Search busy, script run stopped.");
            }
        }

        private bool ValidateFilePatterns()
        {
            if (!string.IsNullOrWhiteSpace(FilePattern))
            {
                foreach (string pattern in UiUtils.SplitPattern(FilePattern))
                {
                    string msg = GetValidateRegexMsg(pattern);
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        if (IsScriptRunning)
                        {
                            logger.Error(TranslationSource.Format(Resources.MessageBox_TheFilePattern0IsNotAValidRegularExpression12, pattern, Environment.NewLine, msg));
                            AddScriptMessage(TranslationSource.Format(Resources.MessageBox_TheFilePattern0IsNotAValidRegularExpression12, pattern, Environment.NewLine, msg));
                        }
                        else
                        {
                            MessageBox.Show(TranslationSource.Format(Resources.MessageBox_TheFilePattern0IsNotAValidRegularExpression12, pattern, Environment.NewLine, msg),
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(FilePatternIgnore))
            {
                foreach (string pattern in UiUtils.SplitPattern(FilePatternIgnore))
                {
                    string msg = GetValidateRegexMsg(pattern);
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        if (IsScriptRunning)
                        {
                            logger.Error(TranslationSource.Format(Resources.MessageBox_TheFilePattern0IsNotAValidRegularExpression12, pattern, Environment.NewLine, msg));
                            AddScriptMessage(TranslationSource.Format(Resources.MessageBox_TheFilePattern0IsNotAValidRegularExpression12, pattern, Environment.NewLine, msg));
                        }
                        else
                        {
                            MessageBox.Show(TranslationSource.Format(Resources.MessageBox_TheFilePattern0IsNotAValidRegularExpression12, pattern, Environment.NewLine, msg),
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        private string GetValidateRegexMsg(string pattern)
        {
            try
            {
                Regex regex = new Regex(pattern);
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
                    if (!IsScriptRunning)
                    {
                        if (MessageBox.Show(Resources.MessageBox_AreYouSureYouWantToReplaceSearchPatternWithEmptyString,
                            Resources.MessageBox_DnGrep + " " + Resources.MessageBox_Replace,
                            MessageBoxButton.YesNo, MessageBoxImage.Question,
                            MessageBoxResult.Yes, TranslationSource.Instance.FlowDirection) != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }
                }

                List<string> roFiles = Utils.GetReadOnlyFiles(SearchResults.GetList());
                if (!IsScriptRunning && roFiles.Count > 0)
                {
                    StringBuilder sb = new StringBuilder(Resources.MessageBox_SomeOfTheFilesCannotBeModifiedIfYouContinueTheseFilesWillBeSkipped);
                    sb.Append(Environment.NewLine)
                      .Append(Resources.MessageBox_WouldYouLikeToContinue)
                      .Append(Environment.NewLine).Append(Environment.NewLine);
                    foreach (string fileName in roFiles)
                    {
                        sb.AppendLine(" - " + new FileInfo(fileName).Name);
                    }
                    if (MessageBox.Show(sb.ToString(), Resources.MessageBox_DnGrep + " " + Resources.MessageBox_Replace,
                        MessageBoxButton.YesNo, MessageBoxImage.Question,
                        MessageBoxResult.Yes, TranslationSource.Instance.FlowDirection) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                List<GrepSearchResult> replaceList = SearchResults.GetWritableList()
                    .Where(sr => sr.Matches.Any()).ToList(); // filter out files with errors shown in results tree
                foreach (var file in roFiles)
                {
                    var item = replaceList.FirstOrDefault(r => r.FileNameReal == file);
                    if (item != null)
                        replaceList.Remove(item);
                }

                bool doReplace = false;
                if (IsScriptRunning)
                {
                    // mark all matches for replace
                    foreach (GrepSearchResult gsr in replaceList)
                    {
                        foreach (var match in gsr.Matches)
                        {
                            match.ReplaceMatch = true;
                        }
                    }
                    doReplace = true;
                }
                else
                {
                    ReplaceWindow dlg = new ReplaceWindow();
                    dlg.ViewModel.SearchFor = SearchFor;
                    dlg.ViewModel.ReplaceWith = ReplaceWith;
                    dlg.ViewModel.SearchResults = replaceList;
                    var result = dlg.ShowDialog();
                    if (result.HasValue && result.Value)
                    {
                        doReplace = true;
                    }
                }

                if (doReplace)
                {
                    CanUndo = false;
                    Utils.DeleteUndoFolder();
                    undoList.Clear();
                    foreach (GrepSearchResult gsr in replaceList)
                    {
                        string filePath = gsr.FileNameReal;
                        if (!gsr.IsReadOnlyFileType && !undoList.Any(r => r.OrginalFile == filePath) && gsr.Matches.Any(m => m.ReplaceMatch))
                        {
                            undoList.Add(new ReplaceDef(filePath, gsr.Matches));
                        }
                    }

                    if (undoList.Count > 0)
                    {
                        StatusMessage = Resources.Main_Status_Replacing;

                        PreviewModel.FilePath = string.Empty;
                        PreviewTitle = string.Empty;

                        CurrentGrepOperation = GrepOperation.Replace;

                        SearchReplaceCriteria workerParams = new SearchReplaceCriteria(this);

                        workerParams.AddReplaceFiles(undoList);

                        ClearMatchCountStatus();
                        SearchResults.Clear();
                        idleTimer.Start();
                        workerSearchReplace.RunWorkerAsync(workerParams);
                        UpdateBookmarks();
                    }
                    else if (IsScriptRunning)
                    {
                        AddScriptMessage("Search list is empty, nothing to replace.");
                        Dispatcher.CurrentDispatcher.Invoke(() => ContinueScript());
                    }
                }
            }
            else if (IsScriptRunning)
            {
                // in a bad state, do not continue
                CancelScript();
                AddScriptMessage("Replace busy, script run stopped.");
            }
        }

        private void Undo()
        {
            if (CanUndo)
            {
                MessageBoxResult response = MessageBoxResult.Yes;

                if (!IsScriptRunning)
                {
                    response = MessageBox.Show(
                        Resources.MessageBox_UndoWillRevertModifiedFiles,
                        Resources.MessageBox_DnGrep + " " + Resources.MessageBox_Undo,
                        MessageBoxButton.YesNo, MessageBoxImage.Warning,
                        MessageBoxResult.No, TranslationSource.Instance.FlowDirection);
                }

                if (response == MessageBoxResult.Yes)
                {
                    GrepCore core = new GrepCore();
                    bool result = core.Undo(undoList);
                    if (result)
                    {
                        if (IsScriptRunning)
                        {
                            logger.Info(Resources.MessageBox_FilesHaveBeenSuccessfullyReverted);
                            AddScriptMessage(Resources.MessageBox_FilesHaveBeenSuccessfullyReverted);
                        }
                        else
                        {
                            MessageBox.Show(Resources.MessageBox_FilesHaveBeenSuccessfullyReverted,
                                Resources.MessageBox_DnGrep + " " + Resources.MessageBox_Undo,
                                MessageBoxButton.OK, MessageBoxImage.Information,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                        Utils.DeleteUndoFolder();
                        undoList.Clear();
                    }
                    else
                    {
                        if (IsScriptRunning)
                        {
                            AddScriptMessage(Resources.MessageBox_ThereWasAnErrorRevertingFiles + App.LogDir);
                        }
                        else
                        {
                            MessageBox.Show(Resources.MessageBox_ThereWasAnErrorRevertingFiles + App.LogDir,
                                Resources.MessageBox_DnGrep + " " + Resources.MessageBox_Undo,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                        CanUndo = false;
                    }
                }
            }
        }

        private bool inUpdateBookmarks;
        private void UpdateBookmarks()
        {
            inUpdateBookmarks = true;

            int maxSearchReplaceCount = Settings.Get<int>(GrepSettings.Key.MaxSearchBookmarks);
            int maxPathCount = Settings.Get<int>(GrepSettings.Key.MaxPathBookmarks);
            int maxExtCount = Settings.Get<int>(GrepSettings.Key.MaxExtensionBookmarks);

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
                if (workerSearchReplace.IsBusy)
                    workerSearchReplace.CancelAsync();
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
                MessageBox.Show(Resources.MessageBox_ThereWasAnErrorSavingOptions + App.LogDir,
                    Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
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
            nameof(FileOrFolderPath), // when in Everything mode
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
            nameof(SkipRemoteCloudStorageFiles),
            nameof(IncludeArchive),
            nameof(CodePage),
        };

        public Bookmark CurrentBookmarkSettings()
        {
            return new Bookmark()
            {
                IgnoreFilePattern = FilePatternIgnore,
                TypeOfFileSearch = TypeOfFileSearch,
                // when in Everything mode, save the Everything search in the bookmark's FileNames property
                FileNames = TypeOfFileSearch == FileSearchType.Everything ? FileOrFolderPath : FilePattern,
                TypeOfSearch = TypeOfSearch,
                SearchPattern = SearchFor,
                ReplacePattern = ReplaceWith,
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
                SkipRemoteCloudStorageFiles = SkipRemoteCloudStorageFiles,
                IncludeArchive = IncludeArchive,
                FollowSymlinks = FollowSymlinks,
                CodePage = CodePage,
                ApplyFileSourceFilters = true,
                ApplyFilePropertyFilters = true,
                ApplyContentSearchFilters = true,
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
                        current.Ordinal = BookmarkLibrary.Instance.Bookmarks.Count;
                        BookmarkLibrary.Instance.Bookmarks.Add(current);
                        bmk = current;
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
                    {
                        current.Ordinal = BookmarkLibrary.Instance.Bookmarks.Count;
                        BookmarkLibrary.Instance.Bookmarks.Add(current);
                        BookmarkLibrary.Instance.Bookmarks.Sort((x, y) => x.Ordinal.CompareTo(y.Ordinal));
                    }
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
                                message = Resources.MessageBox_ThisBookmarkIsAssociatedWithOneOtherFolder +
                                    Environment.NewLine + Resources.MessageBox_ClearingThisBookmarkWillAlsoClearThatBookmark;
                            }
                            else
                            {
                                message = TranslationSource.Format(Resources.MessageBox_ThisBookmarkIsAssociatedWith0OtherFolders, count) +
                                    Environment.NewLine + Resources.MessageBox_ClearingThisBookmarkWillAlsoRemoveThoseBookmarks;

                            }
                            var ans = MessageBox.Show(message + Environment.NewLine + Environment.NewLine +
                                Resources.MessageBox_DoYouWantToContinue, Resources.MessageBox_DnGrep,
                                MessageBoxButton.YesNo, MessageBoxImage.Question,
                                MessageBoxResult.Yes, TranslationSource.Instance.FlowDirection);

                            if (ans == MessageBoxResult.No)
                            {
                                IsBookmarked = true;
                                return;
                            }
                        }

                        BookmarkLibrary.Instance.Bookmarks.Remove(bmk);
                        BookmarkLibrary.Instance.UpdateOrdinals();

                        IsBookmarked = false;
                        IsFolderBookmarked = false;
                    }
                }
            }
            BookmarkLibrary.Save();

            if (bookmarkWindow != null)
            {
                bookmarkWindow.ViewModel.SynchToLibrary();
            }
        }

        private void AddBookmark(string bookmarkName, bool assocateWithFolder)
        {
            bool modified = false;

            // if the named bookmark exists, replace it
            if (!string.IsNullOrEmpty(bookmarkName))
            {
                var bmk = BookmarkLibrary.Instance.Bookmarks
                    .FirstOrDefault(b => bookmarkName.Equals(b.BookmarkName, StringComparison.OrdinalIgnoreCase));

                if (bmk != null)
                {
                    BookmarkLibrary.Instance.Bookmarks.Remove(bmk);
                    BookmarkLibrary.Instance.UpdateOrdinals();
                    modified = true;
                }
            }

            Bookmark current = CurrentBookmarkSettings();
            if (!string.IsNullOrEmpty(bookmarkName))
            {
                current.BookmarkName = bookmarkName;
            }
            current.Ordinal = BookmarkLibrary.Instance.Bookmarks.Count;
            BookmarkLibrary.Instance.Bookmarks.Add(current);
            BookmarkLibrary.Instance.Bookmarks.Sort((x, y) => x.Ordinal.CompareTo(y.Ordinal));
            IsBookmarked = true;
            modified = true;

            if (assocateWithFolder && !string.IsNullOrWhiteSpace(FileOrFolderPath))
            {
                BookmarkLibrary.Instance.AddFolderReference(current, FileOrFolderPath);
                IsFolderBookmarked = true;
            }

            if (modified)
            {
                BookmarkLibrary.Save();

                if (bookmarkWindow != null)
                {
                    bookmarkWindow.ViewModel.SynchToLibrary();
                }
            }
        }

        private void RemoveBookmark(string bookmarkName, bool disassocateWithFolder)
        {
            bool modified = false;

            if (!string.IsNullOrEmpty(bookmarkName))
            {
                var bmk = BookmarkLibrary.Instance.Bookmarks
                    .FirstOrDefault(b => bookmarkName.Equals(b.BookmarkName, StringComparison.OrdinalIgnoreCase));

                if (bmk != null)
                {
                    if (disassocateWithFolder)
                    {
                        if (bmk.FolderReferences.Contains(FileOrFolderPath))
                        {
                            bmk.FolderReferences.Remove(FileOrFolderPath);
                            modified = true;
                        }
                    }
                    else
                    {
                        BookmarkLibrary.Instance.Bookmarks.Remove(bmk);
                        BookmarkLibrary.Instance.UpdateOrdinals();
                        modified = true;
                    }
                }
            }
            else // no name supplied, use current settings
            {
                Bookmark current = CurrentBookmarkSettings();
                Bookmark bmk = BookmarkLibrary.Instance.Find(current);
                if (bmk != null)
                {
                    if (disassocateWithFolder && IsFolderBookmarked && bmk.FolderReferences.Contains(FileOrFolderPath))
                    {
                        bmk.FolderReferences.Remove(FileOrFolderPath);
                        modified = true;
                    }
                    else if (IsBookmarked)
                    {
                        BookmarkLibrary.Instance.Bookmarks.Remove(bmk);
                        BookmarkLibrary.Instance.UpdateOrdinals();
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                UpdateState(nameof(SearchFor)); // to update the bookmark indicator check boxes

                BookmarkLibrary.Save();

                if (bookmarkWindow != null)
                {
                    bookmarkWindow.ViewModel.SynchToLibrary();
                }
            }
        }

        private void UseBookmark(string bookmarkName)
        {
            var bmk = BookmarkLibrary.Instance.Bookmarks
                .FirstOrDefault(b => bookmarkName.Equals(b.BookmarkName, StringComparison.OrdinalIgnoreCase));
            if (bmk != null)
            {
                if (bmk.ApplyFileSourceFilters)
                {
                    // set type of search first to handle Everything mode
                    TypeOfFileSearch = bmk.TypeOfFileSearch;

                    if (TypeOfFileSearch == FileSearchType.Everything)
                    {
                        FileOrFolderPath = bmk.FileNames;
                    }
                    else
                    {
                        FilePattern = bmk.FileNames;
                    }
                    FilePatternIgnore = bmk.IgnoreFilePattern;
                    IncludeArchive = bmk.IncludeArchive;
                    UseGitignore = bmk.UseGitignore;
                    SkipRemoteCloudStorageFiles = bmk.SkipRemoteCloudStorageFiles;
                    CodePage = bmk.CodePage;
                }

                if (bmk.ApplyFilePropertyFilters)
                {
                    IncludeSubfolder = bmk.IncludeSubfolders;
                    MaxSubfolderDepth = bmk.MaxSubfolderDepth;
                    IncludeHidden = bmk.IncludeHiddenFiles;
                    IncludeBinary = bmk.IncludeBinaryFiles;
                    FollowSymlinks = bmk.FollowSymlinks;
                }

                if (bmk.ApplyContentSearchFilters)
                {
                    TypeOfSearch = bmk.TypeOfSearch;
                    SearchFor = bmk.SearchPattern;
                    ReplaceWith = bmk.ReplacePattern;

                    CaseSensitive = bmk.CaseSensitive;
                    WholeWord = bmk.WholeWord;
                    Multiline = bmk.Multiline;
                    Singleline = bmk.Singleline;
                    BooleanOperators = bmk.BooleanOperators;
                }
            }
        }

        private void OpenBookmarksWindow()
        {
            if (bookmarkWindow == null)
            {
                bookmarkWindow = new BookmarksWindow(bk =>
                {
                    var current = BookmarkLibrary.Instance.Find(CurrentBookmarkSettings());
                    if (current != null && current.Id == bk.Id)
                    {
                        IsBookmarked = false;
                        IsFolderBookmarked = false;
                    }
                });
                bookmarkWindow.UseBookmark += BookmarkForm_UseBookmark;
            }

            if (bookmarkWindow.IsVisible)
            {
                bookmarkWindow.Activate();
                bookmarkWindow.Focus();
            }
            else
            {
                var wnd = Application.Current.MainWindow;
                Point pt = Mouse.GetPosition(wnd);
                pt.Offset(-bookmarkWindow.Width + 100, 20);
                bookmarkWindow.SetWindowPosition(pt, wnd);
                bookmarkWindow.Show();
            }
        }

        private void CopyFiles(object argument)
        {
            if (FilesFound)
            {
                string selectedPath = null;
                if (argument is string path && !string.IsNullOrEmpty(path))
                {
                    selectedPath = path;
                }
                else if (fileFolderDialog.ShowDialog() == true)
                {
                    selectedPath = fileFolderDialog.SelectedPath;
                }

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    try
                    {
                        var fileList = SearchResults.GetList();
                        string destinationFolder = UiUtils.GetBaseFolder(selectedPath);
                        bool hasSingleBaseFolder = UiUtils.HasSingleBaseFolder(PathSearchText.FileOrFolderPath);
                        string baseFolder = PathSearchText.BaseFolder;

                        if (!Utils.CanCopyFiles(fileList, destinationFolder))
                        {
                            if (IsScriptRunning)
                            {
                                logger.Error(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain);
                                AddScriptMessage(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain);
                            }
                            else
                            {
                                MessageBox.Show(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory + Environment.NewLine +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain,
                                    Resources.MessageBox_DnGrep + " " + Resources.MessageBox_CopyFiles,
                                    MessageBoxButton.OK, MessageBoxImage.Warning,
                                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                            }
                            return;
                        }

                        var overwritePref = Settings.Get<OverwriteFile>(GrepSettings.Key.OverwriteFilesOnCopy);
                        if (IsScriptRunning && overwritePref == OverwriteFile.Prompt)
                        {
                            overwritePref = OverwriteFile.No;
                        }

                        int count = 0;
                        if (hasSingleBaseFolder && !string.IsNullOrWhiteSpace(baseFolder))
                        {
                            count = Utils.CopyFiles(fileList, baseFolder, destinationFolder, overwritePref);
                        }
                        else
                        {
                            // without a common base path, copy all files to a single directory 
                            count = Utils.CopyFiles(fileList, destinationFolder, overwritePref);
                        }

                        if (IsScriptRunning)
                        {
                            logger.Info(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyCopied, count) +
                                " " + selectedPath);
                            AddScriptMessage(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyCopied, count) +
                                " " + selectedPath);
                        }
                        else
                        {
                            MessageBox.Show(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyCopied, count),
                                Resources.MessageBox_DnGrep + " " + Resources.MessageBox_CopyFiles,
                                MessageBoxButton.OK, MessageBoxImage.Information,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error copying files");

                        if (IsScriptRunning)
                        {
                            AddScriptMessage(Resources.Scripts_CopyFilesFailed + ex.Message);
                            CancelScript();
                        }
                        else
                        {
                            MessageBox.Show(Resources.MessageBox_ThereWasAnErrorCopyingFiles + App.LogDir,
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                    CanUndo = false;
                }
            }
        }

        private void MoveFiles(object argument)
        {
            if (FilesFound)
            {
                string selectedPath = null;
                if (argument is string path && !string.IsNullOrEmpty(path))
                {
                    selectedPath = path;
                }
                else if (fileFolderDialog.ShowDialog() == true)
                {
                    selectedPath = fileFolderDialog.SelectedPath;
                }

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    try
                    {
                        var fileList = SearchResults.GetList();
                        string destinationFolder = UiUtils.GetBaseFolder(selectedPath);
                        bool hasSingleBaseFolder = UiUtils.HasSingleBaseFolder(PathSearchText.FileOrFolderPath);
                        string baseFolder = PathSearchText.BaseFolder;

                        if (!Utils.CanCopyFiles(fileList, destinationFolder))
                        {
                            if (IsScriptRunning)
                            {
                                logger.Error(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain);
                                AddScriptMessage(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain);
                            }
                            else
                            {
                                MessageBox.Show(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory + Environment.NewLine +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain,
                                    Resources.MessageBox_DnGrep + " " + Resources.MessageBox_MoveFiles,
                                    MessageBoxButton.OK, MessageBoxImage.Warning,
                                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                            }
                            return;
                        }

                        var overwritePref = Settings.Get<OverwriteFile>(GrepSettings.Key.OverwriteFilesOnMove);
                        if (IsScriptRunning && overwritePref == OverwriteFile.Prompt)
                        {
                            overwritePref = OverwriteFile.No;
                        }

                        int count = 0;
                        if (hasSingleBaseFolder && !string.IsNullOrWhiteSpace(baseFolder))
                        {
                            count = Utils.MoveFiles(fileList, baseFolder, destinationFolder, overwritePref);
                        }
                        else
                        {
                            // without a common base path, move all files to a single directory 
                            count = Utils.MoveFiles(fileList, destinationFolder, overwritePref);
                        }

                        if (IsScriptRunning)
                        {
                            logger.Info(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyMoved, count) +
                                " " + selectedPath);
                            AddScriptMessage(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyMoved, count) +
                                " " + selectedPath);
                        }
                        else
                        {
                            MessageBox.Show(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyMoved, count),
                                Resources.MessageBox_DnGrep + " " + Resources.MessageBox_MoveFiles,
                                MessageBoxButton.OK, MessageBoxImage.Information,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error moving files");
                        if (IsScriptRunning)
                        {
                            AddScriptMessage(Resources.Scripts_MoveFilesFailed + ex.Message);
                            CancelScript();
                        }
                        else
                        {
                            MessageBox.Show(Resources.MessageBox_ThereWasAnErrorMovingFiles + App.LogDir,
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                    CanUndo = false;
                    ClearMatchCountStatus();
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
                    if (!IsScriptRunning)
                    {
                        if (MessageBox.Show(Resources.MessageBox_YouAreAboutToDeleteFilesFoundDuringSearch + Environment.NewLine +
                                Resources.MessageBox_AreYouSureYouWantToContinue,
                                Resources.MessageBox_DnGrep + " " + Resources.MessageBox_DeleteFiles,
                                MessageBoxButton.YesNo, MessageBoxImage.Warning,
                                MessageBoxResult.No, TranslationSource.Instance.FlowDirection) != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }

                    int count;
                    if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.DeleteToRecycleBin))
                    {
                        count = Utils.SendToRecycleBin(SearchResults.GetList());
                    }
                    else
                    {
                        count = Utils.DeleteFiles(SearchResults.GetList());
                    }

                    if (IsScriptRunning)
                    {
                        logger.Info(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyDeleted, count));
                        AddScriptMessage(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyDeleted, count));
                    }
                    else
                    {
                        MessageBox.Show(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyDeleted, count),
                            Resources.MessageBox_DnGrep + " " + Resources.MessageBox_DeleteFiles,
                            MessageBoxButton.OK, MessageBoxImage.Information,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error deleting files");
                    if (IsScriptRunning)
                    {
                        AddScriptMessage(Resources.Scripts_DeleteFilesFailed + ex.Message);
                        CancelScript();
                    }
                    else
                    {
                        MessageBox.Show(Resources.MessageBox_ThereWasAnErrorDeletingFiles + App.LogDir,
                            Resources.MessageBox_DnGrep,
                            MessageBoxButton.OK, MessageBoxImage.Error,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                }
                CanUndo = false;
                ClearMatchCountStatus();
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
            NativeMethods.SetClipboardText(ReportWriter.GetResultsAsText(SearchResults.GetList(), SearchResults.TypeOfSearch));
        }

        private void ShowReportOptions()
        {
            ReportOptionsViewModel vm = new ReportOptionsViewModel(SearchResults);
            ReportOptionsWindow dlg = new ReportOptionsWindow(vm);
            dlg.Owner = Application.Current.MainWindow;
            dlg.ShowDialog();
        }

        private async void SaveResultsToFile(string reportType)
        {
            if (FilesFound)
            {
                SaveFileDialog dlg = new SaveFileDialog();

                switch (reportType)
                {
                    case "Report":
                        dlg.Filter = Resources.Main_ReportOption_ReportFileFormat + "|*.txt";
                        dlg.DefaultExt = "*.txt";
                        break;
                    case "Text":
                        dlg.Filter = Resources.Main_ReportOption_ResultsFileFormat + "|*.txt";
                        dlg.DefaultExt = "*.txt";
                        break;
                    case "CSV":
                        dlg.Filter = Resources.Main_ReportOption_CSVFileFormat + "|*.csv";
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
                                    ReportWriter.SaveResultsReport(SearchResults.GetList(), BooleanOperators, SearchFor, GetSearchOptions(), dlg.FileName);
                                    break;
                                case "Text":
                                    ReportWriter.SaveResultsAsText(SearchResults.GetList(), SearchResults.TypeOfSearch, dlg.FileName);
                                    break;
                                case "CSV":
                                    ReportWriter.SaveResultsAsCSV(SearchResults.GetList(), SearchResults.TypeOfSearch, dlg.FileName);
                                    break;
                            }
                        });

                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error creating results file");
                        MessageBox.Show(Resources.MessageBox_ThereWasAnErrorCreatingTheFile + App.LogDir,
                            Resources.MessageBox_DnGrep,
                            MessageBoxButton.OK, MessageBoxImage.Error,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
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
                ReportWriter.SaveResultsReport(SearchResults.GetList(), BooleanOperators, SearchFor, GetSearchOptions(), args.ReportPath);
            }
            if (!string.IsNullOrWhiteSpace(args.TextPath))
            {
                ReportWriter.SaveResultsAsText(SearchResults.GetList(), SearchResults.TypeOfSearch, args.TextPath);
            }
            if (!string.IsNullOrWhiteSpace(args.CsvPath))
            {
                ReportWriter.SaveResultsAsCSV(SearchResults.GetList(), SearchResults.TypeOfSearch, args.CsvPath);
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
                var writableFiles = SearchResults.GetWritableList();
                var hasWritableFiles = writableFiles.Any() &&
                    writableFiles.SelectMany(m => m.Matches).Any();

                bool enabled = FilesFound && CurrentGrepOperation == GrepOperation.None &&
                        !IsSaveInProgress && !string.IsNullOrEmpty(SearchFor) &&
                        hasWritableFiles &&
                        // can only replace using the same parameters as was used for the search
                        !SearchParametersChanged &&
                        // if using boolean operators, only allow replace for plain text searches (not implemented for regex)
                        (BooleanOperators ? TypeOfSearch == SearchType.PlainText : true);

                return enabled;
            }
        }

        private void UpdateReplaceButtonTooltip(bool clear)
        {
            ReplaceButtonToolTip = string.Empty;
            if (!clear && !CanReplace && FilesFound)
            {
                var writableFiles = SearchResults.GetWritableList();
                var hasWritableFiles = writableFiles.Any();
                var hasMatches = writableFiles.SelectMany(m => m.Matches).Any();

                if (!hasWritableFiles)
                {
                    ReplaceButtonToolTip = Resources.Main_ReplaceTooltip_NoWritableFilesInResults;
                }
                else if (string.IsNullOrEmpty(SearchFor) || !hasMatches)
                {
                    ReplaceButtonToolTip = Resources.Main_ReplaceTooltip_NoMatchesToReplace;
                }
                else if (SearchParametersChanged)
                {
                    ReplaceButtonToolTip = Resources.Main_ReplaceTooltip_SearchParametersHaveChanged;
                }
                else if (BooleanOperators && TypeOfSearch != SearchType.PlainText)
                {
                    ReplaceButtonToolTip = Resources.Main_ReplaceTooltip_ReplaceUsingBooleanOperators;
                }
            }
            ReplaceButtonToolTipVisible = !string.IsNullOrEmpty(ReplaceButtonToolTip);
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
                ClearMatchCountStatus();
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

            sb.Append(Resources.ReportSummary_SearchFor).Append(" '").Append(SearchFor).AppendLine("'")
              .AppendFormat(Resources.ReportSummary_UsingTypeOfSeach, TypeOfSearch.ToLocalizedString());

            if (CaseSensitive) options.Add(Resources.ReportSummary_CaseSensitive);
            if (WholeWord) options.Add(Resources.ReportSummary_WholeWord);
            if (Multiline) options.Add(Resources.ReportSummary_Multiline);
            if (Singleline) options.Add(Resources.ReportSummary_DotAsNewline);
            if (BooleanOperators) options.Add(Resources.ReportSummary_BooleanOperators);
            if (SearchInResultsContent) options.Add(Resources.ReportSummary_SearchInResults);
            if (StopAfterFirstMatch) options.Add(Resources.ReportSummary_StopAfterFirstMatch);
            if (options.Count > 0)
                sb.AppendLine(string.Join(", ", options.ToArray()));
            sb.AppendLine();

            sb.Append(Resources.ReportSummary_SearchIn).Append(" ").AppendLine(FileOrFolderPath)
              .Append(Resources.ReportSummary_FilePattern).Append(" ").AppendLine(FilePattern);
            if (!string.IsNullOrWhiteSpace(FilePatternIgnore))
                sb.Append(Resources.ReportSummary_ExcludePattern).Append(" ").AppendLine(FilePatternIgnore);
            if (TypeOfFileSearch == FileSearchType.Regex)
                sb.AppendLine(Resources.ReportSummary_UsingRegexFilePattern);
            else if (TypeOfFileSearch == FileSearchType.Everything)
                sb.AppendLine(Resources.ReportSummary_UsingEverythingIndexSearch);

            options.Clear();
            if (!IncludeSubfolder || (IncludeSubfolder && MaxSubfolderDepth == 0)) options.Add(Resources.ReportSummary_NoSubfolders);
            if (IncludeSubfolder && MaxSubfolderDepth > 0) options.Add(TranslationSource.Format(Resources.ReportSummary_MaxFolderDepth, MaxSubfolderDepth));
            if (!IncludeHidden) options.Add(Resources.ReportSummary_NoHiddenFiles);
            if (!IncludeBinary) options.Add(Resources.ReportSummary_NoBinaryFiles);
            if (!IncludeArchive) options.Add(Resources.ReportSummary_NoArchives);
            if (!FollowSymlinks) options.Add(Resources.ReportSummary_NoSymlinks);
            if (options.Count > 0)
                sb.AppendLine(string.Join(", ", options.ToArray()));

            if (UseFileSizeFilter == FileSizeFilter.Yes)
                sb.AppendFormat(Resources.ReportSummary_SizeFrom0To1KB, SizeFrom, SizeTo).AppendLine();

            if (UseFileDateFilter != FileDateFilter.None && TypeOfTimeRangeFilter == FileTimeRange.Dates)
                sb.AppendFormat(Resources.ReportSummary_Type0DateFrom1To2, UseFileDateFilter.ToLocalizedString(),
                    StartDate.HasValue ? StartDate.Value.ToShortDateString() : "*",
                    EndDate.HasValue ? EndDate.Value.ToShortDateString() : "*").AppendLine();

            if (UseFileDateFilter != FileDateFilter.None && TypeOfTimeRangeFilter == FileTimeRange.Hours)
                sb.AppendFormat(Resources.ReportSummary_Type0DateInPast1To2Hours, UseFileDateFilter.ToLocalizedString(), HoursFrom, HoursTo)
                  .AppendLine();

            if (CodePage != -1)
            {
                string encoding = Encodings.Where(r => r.Value == CodePage).Select(r => r.Key).FirstOrDefault();
                sb.Append(Resources.ReportSummary_Encoding).Append(" ").AppendLine(encoding);
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
                logger.Error(ex, "Error running test pattern view");
                MessageBox.Show(Resources.MessageBox_ThereWasAnErrorRunningRegexTest + App.LogDir,
                    Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            }
        }

        private void CheckVersion()
        {
            if (Settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking))
            {
                DateTime lastCheck = Settings.Get<DateTime>(GrepSettings.Key.LastCheckedVersion);
                TimeSpan duration = DateTime.Now.Subtract(lastCheck);
                if (duration.TotalDays >= Settings.Get<int>(GrepSettings.Key.UpdateCheckInterval))
                {
                    CheckForUpdates(false);
                }
            }
        }

        private async void CheckForUpdates(bool fromCommand)
        {
            try
            {
                var versionChk = new PublishedVersionExtractor();
                string version = await versionChk.QueryLatestVersion();

                if (!string.IsNullOrEmpty(version))
                {
                    string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    if (PublishedVersionExtractor.IsUpdateNeeded(currentVersion, version))
                    {
                        if (MessageBox.Show(TranslationSource.Format(Resources.MessageBox_NewVersionOfDnGREP0IsAvailableForDownload, version) +
                            Environment.NewLine + Resources.MessageBox_WouldYouLikeToDownloadItNow,
                            Resources.MessageBox_DnGrep + " " + Resources.MessageBox_NewVersion,
                            MessageBoxButton.YesNo, MessageBoxImage.Information,
                            MessageBoxResult.Yes, TranslationSource.Instance.FlowDirection) == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start("https://github.com/dnGrep/dnGrep/releases/latest");
                        }
                    }
                    else if (fromCommand)
                    {
                        MessageBox.Show(Resources.MessageBox_DnGrepIsUpToDate,
                            Resources.MessageBox_DnGrep, MessageBoxButton.OK, MessageBoxImage.Information,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                }

                Settings.Set(GrepSettings.Key.LastCheckedVersion, DateTime.Now);
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
            this.workerSearchReplace.RunWorkerCompleted += SearchReplaceComplete;
            this.workerSearchReplace.ProgressChanged += SearchProgressChanged;

            DiginesisHelpProvider.HelpNamespace = @"https://github.com/dnGrep/dnGrep/wiki/";
            DiginesisHelpProvider.ShowHelp = true;
        }

        private void PopulateEncodings()
        {
            KeyValuePair<string, int> defaultValue = new KeyValuePair<string, int>(Resources.Main_EncodingAutoDetection, -1);

            List<KeyValuePair<string, int>> tempUni = new List<KeyValuePair<string, int>>();
            List<KeyValuePair<string, int>> tempEnc = new List<KeyValuePair<string, int>>();
            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                Encoding e = ei.GetEncoding();
                if (e.EncodingName.Contains("Unicode", StringComparison.OrdinalIgnoreCase))
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
                if (bmk.ApplyFileSourceFilters)
                {
                    // set type of search first to handle Everything mode
                    TypeOfFileSearch = bmk.TypeOfFileSearch;

                    if (TypeOfFileSearch == FileSearchType.Everything)
                    {
                        FileOrFolderPath = bmk.FilePattern;
                    }
                    else
                    {
                        FilePattern = bmk.FilePattern;
                    }
                    FilePatternIgnore = bmk.IgnoreFilePattern;
                    IncludeArchive = bmk.IncludeArchive;
                    UseGitignore = bmk.UseGitignore;
                    SkipRemoteCloudStorageFiles = bmk.SkipRemoteCloudStorageFiles;
                    CodePage = bmk.CodePage;
                }

                if (bmk.ApplyFilePropertyFilters)
                {
                    IncludeSubfolder = bmk.IncludeSubfolders;
                    MaxSubfolderDepth = bmk.MaxSubfolderDepth;
                    IncludeHidden = bmk.IncludeHidden;
                    IncludeBinary = bmk.IncludeBinary;
                    FollowSymlinks = bmk.FollowSymlinks;
                }

                if (bmk.ApplyContentSearchFilters)
                {
                    TypeOfSearch = bmk.TypeOfSearch;
                    SearchFor = bmk.SearchFor;
                    ReplaceWith = bmk.ReplaceWith;

                    CaseSensitive = bmk.CaseSensitive;
                    WholeWord = bmk.WholeWord;
                    Multiline = bmk.Multiline;
                    Singleline = bmk.Singleline;
                    BooleanOperators = bmk.BooleanOperators;
                }
            }
        }

        private void ApplyBookmark(Bookmark bmk)
        {
            if (bmk != null)
            {
                if (bmk.ApplyFileSourceFilters)
                {
                    // set type of search first to handle Everything mode
                    TypeOfFileSearch = bmk.TypeOfFileSearch;

                    if (TypeOfFileSearch == FileSearchType.Everything)
                    {
                        FileOrFolderPath = bmk.FileNames;
                    }
                    else
                    {
                        FilePattern = bmk.FileNames;
                    }
                    FilePatternIgnore = bmk.IgnoreFilePattern;
                    IncludeArchive = bmk.IncludeArchive;
                    UseGitignore = bmk.UseGitignore;
                    SkipRemoteCloudStorageFiles = bmk.SkipRemoteCloudStorageFiles;
                    CodePage = bmk.CodePage;
                }

                if (bmk.ApplyFilePropertyFilters)
                {
                    IncludeSubfolder = bmk.IncludeSubfolders;
                    MaxSubfolderDepth = bmk.MaxSubfolderDepth;
                    IncludeHidden = bmk.IncludeHiddenFiles;
                    IncludeBinary = bmk.IncludeBinaryFiles;
                    FollowSymlinks = bmk.FollowSymlinks;
                }

                if (bmk.ApplyContentSearchFilters)
                {
                    TypeOfSearch = bmk.TypeOfSearch;
                    SearchFor = bmk.SearchPattern;
                    ReplaceWith = bmk.ReplacePattern;

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
            Settings.Set(GrepSettings.Key.FastSearchBookmarks, fsb);
            List<string> frb = new List<string>();
            for (int i = 0; i < FastReplaceBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                frb.Add(FastReplaceBookmarks[i]);
            }
            Settings.Set(GrepSettings.Key.FastReplaceBookmarks, frb);
            List<string> ffmb = new List<string>();
            for (int i = 0; i < FastFileMatchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                ffmb.Add(FastFileMatchBookmarks[i]);
            }
            Settings.Set(GrepSettings.Key.FastFileMatchBookmarks, ffmb);
            List<string> ffnmb = new List<string>();
            for (int i = 0; i < FastFileNotMatchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                ffnmb.Add(FastFileNotMatchBookmarks[i]);
            }
            Settings.Set(GrepSettings.Key.FastFileNotMatchBookmarks, ffnmb);
            List<string> fpb = new List<string>();
            for (int i = 0; i < FastPathBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                fpb.Add(FastPathBookmarks[i]);
            }
            Settings.Set(GrepSettings.Key.FastPathBookmarks, fpb);
        }

        private void OpenAppDataFolder()
        {
            string dataFolder = Utils.GetDataFolderPath();
            if (!dataFolder.EndsWith(Path.DirectorySeparator))
            {
                dataFolder += Path.DirectorySeparator;
            }
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "/open, \"" + dataFolder + "\"",
                UseShellExecute = false,
            };

            Process.Start(startInfo);
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
                        if (IsScriptRunning)
                        {
                            AddScriptMessage(Resources.MessageBox_FailedToExtractFileFromArchive + App.LogDir);
                        }
                        else
                        {
                            MessageBox.Show(Resources.MessageBox_FailedToExtractFileFromArchive + App.LogDir,
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                        return;
                    }
                    else
                    {
                        displayfileName = result.FileNameDisplayed;
                        filePath = tempFile;
                    }
                }

                if (!string.IsNullOrEmpty(result.FileInfo.TempFile))
                {
                    filePath = result.FileInfo.TempFile;
                    displayfileName = result.FileNameDisplayed + " " + Resources.Preview_Title_AsText;
                }

                string basePath = PathSearchText.BaseFolder;
                if (!string.IsNullOrWhiteSpace(basePath) &&
                    displayfileName.Contains(basePath, StringComparison.CurrentCultureIgnoreCase))
                {
                    displayfileName = displayfileName.Substring(basePath.Length).TrimStart('\\');
                }

                PreviewTitle = displayfileName;

                // order of property setting matters here:
                PreviewModel.GrepResult = result;
                PreviewModel.LineNumber = line;
                PreviewModel.Encoding = result.Encoding;
                PreviewModel.FilePath = filePath;

                if (!IsPreviewDocked)
                    PreviewShow?.Invoke(this, EventArgs.Empty);
            }
        }
        #endregion
    }
}
