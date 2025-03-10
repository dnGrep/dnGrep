﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
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
        public event EventHandler? PreviewHide;
        public event EventHandler? PreviewShow;

        private Brush highlightForeground = Brushes.Yellow;
        private Brush highlightBackground = Brushes.Black;
        private PauseCancelTokenSource? pauseCancelTokenSource;

        private readonly string enQuad = char.ConvertFromUtf32(0x2000);
        public static readonly string IgnoreFilterFolder = "Filters";
        private static bool beenInitialized;

        static MainViewModel()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (beenInitialized) return;

            beenInitialized = true;
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ReloadThemeCommand), "Options_Reload", "Control+F5");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ToggleFileOptionsCommand), "", "Alt+E");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ToggleResultsMaximizeCommand), "", "F7");

            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(UndoCommand), "Main_Menu_Undo", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(PersonalizationCommand), "Main_Menu_Personalize", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(OptionsCommand), "Main_Menu_Options", "F8");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(KeyboardOptionsCommand), "Main_Menu_Keyboard", "F9");

            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(NewScriptCommand), "Main_Menu_NewScript", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(EditScriptCommand), "Main_Menu_EditScript", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CancelScriptCommand), "Main_Menu_CancelScript", string.Empty);

            foreach (var label in ScriptManager.GetScriptNames())
            {
                if (!string.IsNullOrEmpty(label))
                {
                    KeyBindingManager.RegisterScript(KeyCategory.Main, label);
                }
            }

            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(OpenBookmarksWindowCommand), "Main_Menu_Bookmarks", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CheckForUpdatesCommand), "Main_Menu_About_CheckForUpdates", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(OpenAppDataCommand), "Main_Menu_About_AppData", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(OpenAppLogsCommand), "Main_Menu_About_AppLogs", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(AboutCommand), "Main_Menu_About_AboutDnGrep", string.Empty);

            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(BrowseFolderCommand), "", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SearchCommand), "Main_SearchButton", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SearchAndStopCommand), "Main_StopAfterFirstMatch", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SearchAndPauseCommand), "Main_PauseAfterFirstMatch", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ReplaceCommand), "Main_ReplaceButton", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SortCommand), "Main_SortButton", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CancelCommand), "Main_CancelButton", "Escape");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(PauseResumeCommand), "Main_PauseButton", string.Empty);

            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CopyFilesCommand), "Main_MoreMenu_CopyFiles", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(MoveFilesCommand), "Main_MoreMenu_MoveFiles", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(DeleteFilesCommand), "Main_MoreMenu_DeleteFiles", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CopyToClipboardCommand), "Main_MoreMenu_CopyFileNames", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CopyFilesWithCountsCommand), "Main_MoreMenu_CopyFileNamesWithMatchCount", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CopyMatchingLinesCommand), "Main_MoreMenu_CopyResults", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SaveReportCommand), "Main_MoreMenu_Save_Report", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SaveTextResultsCommand), "Main_MoreMenu_Save_TextResults", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SaveCsvResultsCommand), "Main_MoreMenu_Save_CSVResults", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ReportOptionsCommand), "Main_MoreMenu_Report_Options", string.Empty);

            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(HighlightsCommand), "Main_HighlightMatches", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(TestExpressionCommand), "Main_TestExpression", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(BookmarkAddCommand), "", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(FolderBookmarkAddCommand), "", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ResetOptionsCommand), "Main_ResetOptions", string.Empty);
        }

        public MainViewModel()
            : base()
        {
            if (Application.Current != null)
            {
                double maxPreviewWidth = Application.Current.MainWindow.Width - DockPanelSplitter.Panel1MinSize;
                double maxPreviewHeight = Application.Current.MainWindow.Height - DockPanelSplitter.Panel1MinSize;

                PreviewWindowBounds = LayoutProperties.PreviewBounds;
                PreviewWindowState = LayoutProperties.PreviewWindowState;
                PreviewDockedWidth = Math.Min(LayoutProperties.PreviewDockedWidth, maxPreviewWidth);
                PreviewDockedHeight = Math.Min(LayoutProperties.PreviewDockedHeight, maxPreviewHeight);
                IsPreviewHidden = LayoutProperties.PreviewHidden;

                CanSearchArchives = Utils.ArchiveExtensions.Count > 0;

                ResultsViewModel.PathSearchText = PathSearchText;
                ResultsViewModel.GrepLineSelected += SearchResults_GrepLineSelected;
                ResultsViewModel.PreviewFileLineRequest += SearchResults_PreviewFileLineRequest;
                ResultsViewModel.PreviewFileRequest += SearchResults_PreviewFileRequest;
                ResultsViewModel.OpenFileLineRequest += SearchResults_OpenFileLineRequest;
                ResultsViewModel.OpenFileRequest += SearchResults_OpenFileRequest;
                ResultsViewModel.SearchResults.CollectionChanged += SearchResults_CollectionChanged;

                CheckVersion();
                ControlsInit();
                PopulateTimeIntervals();
                PopulateEncodings();
                PopulateScripts();
                PopulateIgnoreFilters(true);

                InitializeInputBindings();

                highlightBackground = Application.Current.Resources["Match.Highlight.Background"] as Brush ?? Brushes.Yellow;
                highlightForeground = Application.Current.Resources["Match.Highlight.Foreground"] as Brush ?? Brushes.Black;
                ToggleHighlights();

                AppTheme.Instance.CurrentThemeChanging += (s, e) =>
                {
                    Application.Current.Resources.Remove("Match.Highlight.Background");
                    Application.Current.Resources.Remove("Match.Highlight.Foreground");
                };

                AppTheme.Instance.CurrentThemeChanged += (s, e) =>
                {
                    highlightBackground = Application.Current.Resources["Match.Highlight.Background"] as Brush ?? Brushes.Yellow;
                    highlightForeground = Application.Current.Resources["Match.Highlight.Foreground"] as Brush ?? Brushes.Black;
                    ToggleHighlights();
                };

                TranslationSource.Instance.CurrentCultureChanged += CurrentCultureChanged;

                PropertyChanged += OnMainViewModel_PropertyChanged;

                idleTimer.Interval = TimeSpan.FromMilliseconds(250);
                idleTimer.Tick += IdleTimer_Tick;

                App.Messenger.Register<MRUViewModel>("IsPinnedChanged", OnMRUPinChanged);
                App.Messenger.Register<KeyCategory>("KeyGestureChanged", OnKeyGestureChanged);
            }
        }

        private void InitializeInputBindings()
        {
            foreach (KeyBindingInfo kbi in KeyBindingManager.GetCommandGestures(KeyCategory.Main))
            {
                PropertyInfo? pi = GetType().GetProperty(kbi.CommandName, BindingFlags.Instance | BindingFlags.Public);
                if (pi != null && pi.GetValue(this) is RelayCommand cmd)
                {
                    InputBindings.Add(KeyBindingManager.CreateKeyBinding(cmd, kbi.KeyGesture));
                }
            }
        }

        private void OnKeyGestureChanged(KeyCategory category)
        {
            if (category == KeyCategory.Main)
            {
                InputBindings.Clear();
                PopulateScripts();
                InitializeInputBindings();
                InputBindings.RaiseAfterCollectionChanged();
            }
        }

        internal void OnConfigurationFoldersChanged()
        {
            PopulateScripts();
            PopulateIgnoreFilters(firstTime: true);
            LoadSettings();
            CurrentCultureChanged(this, EventArgs.Empty);
        }

        private void CurrentCultureChanged(object? sender, EventArgs e)
        {
            if (PreviewModel != null)
            {
                PreviewModel.FilePath = string.Empty;
            }

            PreviewTitle = string.Empty;

            PopulateTimeIntervals();

            // reload the Encodings list, the "Auto" encoding name (at least) has changed languages
            int value = CodePage;
            CodePage = -2;
            PopulateEncodings();
            CodePage = value;
            PopulateScripts();

            // this call will repopulate the FileFiltersSummary
            // IncludeSubfolder didn't really change, but triggers the refresh
            UpdateState(nameof(IncludeSubfolder));
            // this call will update the validation message, if visible
            UpdateState(nameof(TypeOfSearch));
            // this call will update the Folder/Everything label
            UpdateState(nameof(TypeOfFileSearch));
            // this call will update the window title
            UpdateState(nameof(FileOrFolderPath));

            OnPropertyChanged(nameof(IsBookmarkedTooltip));
            OnPropertyChanged(nameof(IsFolderBookmarkedTooltip));
            OnPropertyChanged(nameof(ResultOptionsButtonTooltip));
            OnPropertyChanged(nameof(PauseResumeButtonLabel));

            StatusMessage = string.Empty;
            ClearMatchCountStatus();
            ResultsViewModel.Clear();
            UpdateReplaceButtonTooltip(true);

            App.Messenger.NotifyColleagues("CultureChanged");
        }

        internal bool Closing()
        {
            if (bookmarkWindow != null)
            {
                bookmarkWindow.UseBookmark -= BookmarkForm_UseBookmark;
                bookmarkWindow.ApplicationExit();
            }

            while (scriptEditorWindows.Count > 0)
            {
                var wnd = scriptEditorWindows[^1];
                if (!wnd.ConfirmSave())
                {
                    return false;
                }
                wnd.Close();
            }

            return true;
        }

        void SearchResults_OpenFileRequest(object? sender, GrepResultEventArgs e)
        {
            OpenFile(e.FormattedGrepResult, e.UseCustomEditor, e.CustomEditorName);
        }

        void SearchResults_OpenFileLineRequest(object? sender, GrepLineEventArgs e)
        {
            OpenFile(e.FormattedGrepLine, e.UseCustomEditor, e.CustomEditorName);
        }

        void SearchResults_PreviewFileRequest(object? sender, GrepResultEventArgs e)
        {
            if (e.FormattedGrepResult != null && !e.FormattedGrepResult.GrepResult.IsHexFile)
                PreviewFile(e.FormattedGrepResult);
        }

        void SearchResults_PreviewFileLineRequest(object? sender, GrepLineEventArgs e)
        {
            if (e.FormattedGrepLine != null && !e.FormattedGrepLine.GrepLine.IsHexFile)
                PreviewFile(e.FormattedGrepLine);
        }

        private void SearchResults_GrepLineSelected(object? sender, GrepLineSelectEventArgs e)
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
                    int precedingMatchCount = GetPrecedingMatchCount(e.FormattedGrepLine.Parent);
                    StatusMessage4 = $"{e.MatchOrdinal + precedingMatchCount}/{totalMatchCount}";
                    StatusMessage4Tooltip = Resources.Main_StatusTooltip_MatchNumberMatchCountOverall;
                }
                else
                {
                    StatusMessage4 = string.Empty;
                    StatusMessage4Tooltip = string.Empty;
                }

            }
        }

        private int GetPrecedingMatchCount(FormattedGrepResult formattedGrepResult)
        {
            if (formattedGrepResult == null || formattedGrepResult.GrepResult == null)
            {
                return 0;
            }

            if (!precedingMatches.TryGetValue(formattedGrepResult.GrepResult.Id, out int count))
            {
                count = 0;
                foreach (var item in ResultsViewModel.SearchResults)
                {
                    if (item == formattedGrepResult)
                    {
                        break;
                    }
                    count += item.Matches;
                }
                precedingMatches.Add(formattedGrepResult.GrepResult.Id, count);
            }
            return count;
        }

        private void SearchResults_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (ResultsViewModel.SearchResults.Count == 0 && PreviewFileContent && PreviewModel != null)
            {
                // clear the preview
                PreviewModel.FilePath = string.Empty;
                PreviewTitle = string.Empty;
            }
        }

        #region Private Variables and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private DateTime searchReplaceStartTime = DateTime.Now;
        private readonly BackgroundWorker workerSearchReplace = new();
        private BookmarksWindow? bookmarkWindow;
        private readonly HashSet<string> currentSearchFiles = [];
        private int processedFiles;
        private bool needsClearUndo;
        private readonly List<ReplaceDef> undoList = [];
        private readonly DispatcherTimer idleTimer = new(DispatcherPriority.ContextIdle);
        private readonly Dictionary<string, int> precedingMatches = [];
        private int totalMatchCount;
        private string latestStatusMessage = string.Empty;

        #endregion

        #region Properties

        public Window? ParentWindow { get; set; }

        public MainForm? MainForm => ParentWindow as MainForm;

        public PreviewViewModel? PreviewModel { get; internal set; } // will get set with the real view model

        public bool IsReplaceRunning => CurrentGrepOperation == GrepOperation.Replace;

        public TimeSpan CurrentSearchDuration
        {
            get
            {
                if (CurrentGrepOperation == GrepOperation.Search ||
                    CurrentGrepOperation == GrepOperation.SearchInResults)
                {
                    return DateTime.Now.Subtract(searchReplaceStartTime);
                }
                return TimeSpan.Zero;
            }
        }

        public TimeSpan LatestSearchDuration { get; private set; } = TimeSpan.Zero;
        #endregion

        #region Presentation Properties

        public static DockViewModel DockVM => DockViewModel.Instance;

        public static NavigationToolsViewModel NavTools => NavigationToolsViewModel.Instance;

        [ObservableProperty]
        private double mainFormFontSize;

        public ObservableCollection<TimeRangeMap> TimeRanges { get; } = [];

        public ObservableCollection<MenuItemViewModel> ScriptMenuItems { get; } = [];

        public ObservableCollection<IgnoreFilterFile> IgnoreFilterList { get; } = [];

        public ObservableCollectionEx<InputBinding> InputBindings { get; } = [];

        [ObservableProperty]
        private bool isSearchForFocused;

        [ObservableProperty]
        private IgnoreFilterFile ignoreFilter = IgnoreFilterFile.None;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PauseResumeButtonLabel))]
        private bool isSearchReplacePaused;

        public string PauseResumeButtonLabel => IsSearchReplacePaused ? Resources.Main_ResumeButton : Resources.Main_PauseButton;

        public static bool IsGitInstalled => Utils.IsGitInstalled;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CompositeSearchInArchivesVisible))]
        private bool canSearchArchives = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBookmarkedTooltip))]
        private bool isBookmarked;

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFolderBookmarkedTooltip))]
        private bool isFolderBookmarked;

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

        [ObservableProperty]
        private string _previewTitle = " ";

        [ObservableProperty]
        private Rect previewWindowBounds = Rect.Empty;

        [ObservableProperty]
        private WindowState previewWindowState = WindowState.Normal;

        [ObservableProperty]
        private bool isPreviewHidden = false;
        partial void OnIsPreviewHiddenChanged(bool value)
        {
            if (value)
            {
                // if the option to minimize to system tray is on,
                // then hiding the preview window does not reset the 
                // show preview flag
                if (Application.Current.MainWindow.IsVisible)
                {
                    PreviewFileContent = false;
                }
            }
        }

        [ObservableProperty]
        private double previewDockedWidth = 200;

        [ObservableProperty]
        private double previewDockedHeight = 200;

        [ObservableProperty]
        private ReplaceType replaceType;
        partial void OnReplaceTypeChanged(ReplaceType value)
        {
            ReplaceSwitch();
        }

        [ObservableProperty]
        private SortType sortType;
        partial void OnSortTypeChanged(SortType value)
        {
            SortResults();
        }

        [ObservableProperty]
        private ListSortDirection sortDirection;
        partial void OnSortDirectionChanged(ListSortDirection value)
        {
            SortResults();
        }

        [ObservableProperty]
        private bool naturalSort;
        partial void OnNaturalSortChanged(bool value)
        {
            SortResults();
        }

        [ObservableProperty]
        private bool highlightsOn;

        [ObservableProperty]
        private bool showLinesInContext;
        partial void OnShowLinesInContextChanged(bool value)
        {
            Settings.Set(GrepSettings.Key.ShowLinesInContext, value);
        }

        [ObservableProperty]
        private int contextLinesBefore;

        [ObservableProperty]
        private int contextLinesAfter;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaximizeResultsTreeButtonTooltip))]
        private bool isResultTreeMaximized = false;

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResultOptionsButtonTooltip))]
        private bool isResultOptionsExpanded;

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

        [ObservableProperty]
        private string replaceButtonToolTip = string.Empty;

        [ObservableProperty]
        private bool replaceButtonToolTipVisible = false;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private string statusMessage2 = string.Empty;

        [ObservableProperty]
        private string? statusMessage2Tooltip;

        [ObservableProperty]
        private string statusMessage3 = string.Empty;

        [ObservableProperty]
        private string? statusMessage3Tooltip;

        [ObservableProperty]
        private string statusMessage4 = string.Empty;

        [ObservableProperty]
        private string? statusMessage4Tooltip;

        [ObservableProperty]
        private string stopAfterNumMatchesText = string.Empty;

        [ObservableProperty]
        private string pauseAfterNumMatchesText = string.Empty;

        [ObservableProperty]
        private bool autoCompleteEnabled = false;

        private void ClearMatchCountStatus()
        {
            StatusMessage2 = string.Empty;
            StatusMessage3 = string.Empty;
            StatusMessage4 = string.Empty;

            StatusMessage2Tooltip = string.Empty;
            StatusMessage3Tooltip = string.Empty;
            StatusMessage4Tooltip = string.Empty;

            precedingMatches.Clear();
        }

        public string DropDownFontFamily { get; set; } = SystemSymbols.DropDownFontFamily;
        public string DropDownArrowCharacter { get; set; } = SystemSymbols.DropDownArrowCharacter;

        #endregion

        #region Commands

        private RelayCommand? undoCommand;
        public RelayCommand UndoCommand => undoCommand ??= new RelayCommand(
            param => Undo(),
            param => CanUndo);


        private RelayCommand? personalizationCommand;
        public RelayCommand PersonalizationCommand => personalizationCommand ??= new RelayCommand(
            p => PersonalizationOn = !PersonalizationOn,
            q => true);


        private RelayCommand? optionsCommand;
        public RelayCommand OptionsCommand => optionsCommand ??= new RelayCommand(
            param => ShowOptions());


        private RelayCommand? keyboardOptionsCommand;
        public RelayCommand KeyboardOptionsCommand => keyboardOptionsCommand ??= new RelayCommand(
            p => ShowKeyboardOptions(),
            q => true);


        private RelayCommand? helpCommand;
        public RelayCommand HelpCommand => helpCommand ??= new RelayCommand(
            param => ShowHelp());

        private RelayCommand? aboutCommand;
        public RelayCommand AboutCommand => aboutCommand ??= new RelayCommand(
            param => ShowAbout());

        private RelayCommand? checkForUpdatesCommand;
        public RelayCommand CheckForUpdatesCommand => checkForUpdatesCommand ??= new RelayCommand(
            param => CheckForUpdates(true));

        private RelayCommand? browseFolderCommand;
        public RelayCommand BrowseFolderCommand => browseFolderCommand ??= new RelayCommand(
            param => Browse());

        private RelayCommand? searchCommand;
        public RelayCommand SearchCommand => searchCommand ??= new RelayCommand(
            param =>
            {
                StopAfterNumMatches = false;
                PauseAfterNumMatches = false;
                Search();
            },
            param => CanSearch);

        private RelayCommand? searchAndStopCommand;
        public RelayCommand SearchAndStopCommand => searchAndStopCommand ??= new RelayCommand(
            param =>
            {
                StopAfterNumMatches = true;
                PauseAfterNumMatches = false;
                Search();
            },
            param => CanSearch);

        private RelayCommand? searchAndPauseCommand;
        public RelayCommand SearchAndPauseCommand => searchAndPauseCommand ??= new RelayCommand(
            param =>
            {
                StopAfterNumMatches = false;
                PauseAfterNumMatches = true;
                Search();
            },
            param => CanSearch);


        private RelayCommand? replaceCommand;
        public RelayCommand ReplaceCommand => replaceCommand ??= new RelayCommand(
            param => ReplaceSwitch(),
            param => CanReplace);

        private RelayCommand? sortCommand;
        public RelayCommand SortCommand => sortCommand ??= new RelayCommand(
            param => SortResults(),
            param => CanSortResults);

        private RelayCommand? copyFilesCommand;
        public RelayCommand CopyFilesCommand => copyFilesCommand ??= new RelayCommand(
            param => CopyFiles(param));

        private RelayCommand? moveFilesCommand;
        public RelayCommand MoveFilesCommand => moveFilesCommand ??= new RelayCommand(
            param => MoveFiles(param));

        private RelayCommand? deleteFilesCommand;
        public RelayCommand DeleteFilesCommand => deleteFilesCommand ??= new RelayCommand(
            param => DeleteFiles());

        private RelayCommand? copyToClipboardCommand;
        public RelayCommand CopyToClipboardCommand => copyToClipboardCommand ??= new RelayCommand(
            param => CopyToClipboard(false));

        private RelayCommand? copyFilesWithCountsCommand;
        public RelayCommand CopyFilesWithCountsCommand => copyFilesWithCountsCommand ??= new RelayCommand(
            param => CopyToClipboard(true));

        private RelayCommand? copyMatchingLinesCommand;
        public RelayCommand CopyMatchingLinesCommand => copyMatchingLinesCommand ??= new RelayCommand(
            param => CopyResults());

        private RelayCommand? saveReportCommand;
        public RelayCommand SaveReportCommand => saveReportCommand ??= new RelayCommand(
            param => SaveResultsToFile("Report"));

        private RelayCommand? saveTextResultsCommand;
        public RelayCommand SaveTextResultsCommand => saveTextResultsCommand ??= new RelayCommand(
            param => SaveResultsToFile("Text"));

        private RelayCommand? saveCsvResultsCommand;
        public RelayCommand SaveCsvResultsCommand => saveCsvResultsCommand ??= new RelayCommand(
            param => SaveResultsToFile("CSV"));

        private RelayCommand? reportOptionsCommand;
        public RelayCommand ReportOptionsCommand => reportOptionsCommand ??= new RelayCommand(
            p => ShowReportOptions());

        private RelayCommand? cancelCommand;
        public RelayCommand CancelCommand => cancelCommand ??= new RelayCommand(
            param => Cancel(),
            param => CanCancel);

        private RelayCommand? pauseResumeCommand;
        public RelayCommand PauseResumeCommand => pauseResumeCommand ??= new RelayCommand(
            param => PauseResume(),
            param => CanCancel);

        private RelayCommand? highlightsCommand;
        public RelayCommand HighlightsCommand => highlightsCommand ??= new RelayCommand(
            param => ToggleHighlights());

        private RelayCommand? testExpressionCommand;
        public RelayCommand TestExpressionCommand => testExpressionCommand ??= new RelayCommand(
            param => OpenTestPatternWindow());

        private RelayCommand? bookmarkAddCommand;
        public RelayCommand BookmarkAddCommand => bookmarkAddCommand ??= new RelayCommand(
            param => BookmarkAddRemove(false));

        private RelayCommand? folderBookmarkAddCommand;
        public RelayCommand FolderBookmarkAddCommand => folderBookmarkAddCommand ??= new RelayCommand(
            param => BookmarkAddRemove(true));

        private RelayCommand? openBookmarksWindowCommand;
        public RelayCommand OpenBookmarksWindowCommand => openBookmarksWindowCommand ??= new RelayCommand(
            param => OpenBookmarksWindow());

        private RelayCommand? resetOptionsCommand;
        public RelayCommand ResetOptionsCommand => resetOptionsCommand ??= new RelayCommand(
            param => ResetOptions());

        private RelayCommand? toggleFileOptionsCommand;
        public RelayCommand ToggleFileOptionsCommand => toggleFileOptionsCommand ??= new RelayCommand(
            param => IsFiltersExpanded = !IsFiltersExpanded);

        private RelayCommand? reloadThemeCommand;
        public RelayCommand ReloadThemeCommand => reloadThemeCommand ??= new RelayCommand(
            param => AppTheme.Instance.ReloadCurrentTheme());

        private RelayCommand? toggleResultsMaximizeCommand;
        public RelayCommand ToggleResultsMaximizeCommand => toggleResultsMaximizeCommand ??= new RelayCommand(
            p => IsResultTreeMaximized = !IsResultTreeMaximized);

        private RelayCommand? openAppDataCommand;
        public RelayCommand OpenAppDataCommand => openAppDataCommand ??= new RelayCommand(
            p => OpenAppDataFolder(),
            q => true);

        private RelayCommand? openAppLogsCommand;
        public RelayCommand OpenAppLogsCommand => openAppLogsCommand ??= new RelayCommand(
            p => OpenAppLogsFolder(),
            q => true);

        private RelayCommand? deleteMRUItemCommand;
        public RelayCommand DeleteMRUItemCommand => deleteMRUItemCommand ??= new RelayCommand(
            p => DeleteMRUItem(p as MRUViewModel),
            q => true);

        private RelayCommand? filterComboBoxDropDownCommand;
        public RelayCommand FilterComboBoxDropDownCommand => filterComboBoxDropDownCommand ??= new RelayCommand(
            p => PopulateIgnoreFilters(false));

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
                    IsFolderBookmarked = bmk.FolderReferences.Contains(Path.TrimEndingDirectorySeparator(FileOrFolderPath));
                }
                else
                {
                    IsBookmarked = false;
                    IsFolderBookmarked = false;
                }
            }

            if (name == nameof(FileOrFolderPath) && !inUpdateBookmarks)
            {
                var bmk = BookmarkLibrary.Instance.Bookmarks.FirstOrDefault(b => b.FolderReferences.Contains(Path.TrimEndingDirectorySeparator(FileOrFolderPath)));
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
                    if (ResultsViewModel.SelectedNodes.Count > 0)
                    {
                        var item = ResultsViewModel.SelectedNodes[0];

                        if (item is FormattedGrepLine grepLine)
                        {
                            PreviewFile(grepLine);
                        }
                        else if (item is FormattedGrepResult grepResult)
                        {
                            PreviewFile(grepResult);
                        }
                    }
                    else if (IsPreviewHidden && !DockVM.IsPreviewDocked)
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

#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            // changing the private field so as to not trigger sorting the results or replace when
            // the Options dialog is closed
            sortType = GrepSettings.Instance.Get<SortType>(GrepSettings.Key.TypeOfSort);
            sortDirection = GrepSettings.Instance.Get<ListSortDirection>(GrepSettings.Key.SortDirection);
            replaceType = GrepSettings.Instance.Get<ReplaceType>(GrepSettings.Key.TypeOfReplace);
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            NaturalSort = GrepSettings.Instance.Get<bool>(GrepSettings.Key.NaturalSort);
            ResultsViewModel.ResultsScale = GrepSettings.Instance.Get<double>(GrepSettings.Key.ResultsTreeScale);
            ResultsViewModel.WrapText = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ResultsTreeWrap);
            IsResultOptionsExpanded = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowResultOptions);
            HighlightsOn = GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightMatches);
            ShowLinesInContext = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext);
            ContextLinesBefore = GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore);
            ContextLinesAfter = GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter);
            AutoCompleteEnabled = GrepSettings.Instance.Get<bool>(GrepSettings.Key.AutoCompleteEnabled);

            StopAfterNumMatchesText = SearchAutoStopCount == 1 ? Resources.Main_StopAfterFirstMatch :
                TranslationSource.Format(Resources.Main_StopAfter0Matches, SearchAutoStopCount);

            PauseAfterNumMatchesText = SearchAutoPauseCount == 1 ? Resources.Main_PauseAfterFirstMatch :
                TranslationSource.Format(Resources.Main_PauseAfter0Matches, SearchAutoPauseCount);

            // archive extension count may change after Options dialog closes
            CanSearchArchives = Utils.ArchiveExtensions.Count > 0;
            if (!CanSearchArchives)
            {
                IncludeArchive = false;
            }

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
            Settings.Set(GrepSettings.Key.NaturalSort, NaturalSort);
            Settings.Set(GrepSettings.Key.TypeOfSort, SortType);
            Settings.Set(GrepSettings.Key.TypeOfReplace, ReplaceType);
            Settings.Set(GrepSettings.Key.ShowResultOptions, IsResultOptionsExpanded);
            Settings.Set(GrepSettings.Key.ResultsTreeScale, ResultsViewModel.ResultsScale);
            Settings.Set(GrepSettings.Key.ResultsTreeWrap, ResultsViewModel.WrapText);
            Settings.Set(GrepSettings.Key.HighlightMatches, HighlightsOn);
            Settings.Set(GrepSettings.Key.ShowLinesInContext, ShowLinesInContext);
            Settings.Set(GrepSettings.Key.ContextLinesBefore, ContextLinesBefore);
            Settings.Set(GrepSettings.Key.ContextLinesAfter, ContextLinesAfter);
            Settings.Set(GrepSettings.Key.PersonalizationOn, PersonalizationOn);
            Settings.Set(GrepSettings.Key.IgnoreFilter, IgnoreFilter.Name);

            LayoutProperties.PreviewBounds = PreviewWindowBounds;
            LayoutProperties.PreviewWindowState = PreviewWindowState;
            LayoutProperties.PreviewDockedWidth = PreviewDockedWidth;
            LayoutProperties.PreviewDockedHeight = PreviewDockedHeight;
            LayoutProperties.PreviewHidden = IsPreviewHidden;

            DockVM.SaveSettings();

            base.SaveSettings();
        }

        public void OpenFile(FormattedGrepLine? selectedNode, bool useCustomEditor, string customEditorName)
        {
            if (selectedNode == null)
                return;

            try
            {
                // Line was selected
                int pageNumber = selectedNode.GrepLine.PageNumber;
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
                OpenFileArgs fileArg = new(result.GrepResult, result.GrepResult.Pattern, pageNumber, lineNumber,
                    matchText, columnNumber, useCustomEditor, customEditorName);
                if (Utils.IsArchive(result.GrepResult.FileNameReal))
                {
                    var customExtensions = Settings.GetExtensionList("ArchiveCustom");
                    if (customExtensions.Contains(Path.GetExtension(result.GrepResult.FileNameReal).TrimStart('.').ToLowerInvariant()))
                    {
                        // open the archive, not the inner file
                        GrepSearchResult grepSearchResult = new(result.GrepResult.FileNameReal,
                            result.GrepResult.Pattern, string.Empty, true);

                        Utils.OpenFile(new(grepSearchResult, grepSearchResult.Pattern, pageNumber, lineNumber,
                            matchText, columnNumber, useCustomEditor, customEditorName));
                    }
                    else
                    {
                        ArchiveDirectory.OpenFile(fileArg);
                    }
                }
                else
                {
                    IGrepEngine engine = GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, GrepEngineInitParams.Default, FileFilter.Default, TypeOfSearch);
                    if (engine != null)
                    {
                        engine.OpenFile(fileArg);
                        GrepEngineFactory.ReturnToPool(result.GrepResult.FileNameReal, engine);
                    }
                    if (fileArg.UseBaseEngine)
                        Utils.OpenFile(new(result.GrepResult, result.GrepResult.Pattern, pageNumber, lineNumber,
                            matchText, columnNumber, useCustomEditor, customEditorName));
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

        public void OpenFile(FormattedGrepResult? result, bool useCustomEditor, string customEditorName)
        {
            if (result == null)
                return;

            try
            {
                // Line was selected
                int pageNumber = 0;
                int lineNumber = 0;

                int columnNumber = 1;
                string matchText = string.Empty;
                var firstLine = result.GrepResult.SearchResults.FirstOrDefault(r => !r.IsContext);
                if (firstLine != null)
                {
                    pageNumber = firstLine.PageNumber;
                    lineNumber = firstLine.LineNumber;

                    var firstMatch = firstLine.Matches.FirstOrDefault();
                    if (firstMatch != null)
                    {
                        columnNumber = firstMatch.StartLocation + 1;
                        matchText = firstLine.LineText.Substring(firstMatch.StartLocation, firstMatch.Length);
                    }
                }

                OpenFileArgs fileArg = new(result.GrepResult, result.GrepResult.Pattern, pageNumber, lineNumber,
                    matchText, columnNumber, useCustomEditor, customEditorName);
                if (Utils.IsArchive(result.GrepResult.FileNameReal))
                {
                    var customExtensions = Settings.GetExtensionList("ArchiveCustom");
                    if (customExtensions.Contains(Path.GetExtension(result.GrepResult.FileNameReal).TrimStart('.').ToLowerInvariant()))
                    {
                        // open the archive, not the inner file
                        GrepSearchResult grepSearchResult = new(result.GrepResult.FileNameReal,
                            result.GrepResult.Pattern, string.Empty, true);

                        Utils.OpenFile(new(grepSearchResult, grepSearchResult.Pattern, pageNumber, lineNumber,
                            matchText, columnNumber, useCustomEditor, customEditorName));
                    }
                    else
                    {
                        ArchiveDirectory.OpenFile(fileArg);
                    }
                }
                else
                {
                    IGrepEngine engine = GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, GrepEngineInitParams.Default, FileFilter.Default, TypeOfSearch);
                    if (engine != null)
                    {
                        engine.OpenFile(fileArg);
                        GrepEngineFactory.ReturnToPool(result.GrepResult.FileNameReal, engine);
                    }
                    if (fileArg.UseBaseEngine)
                        Utils.OpenFile(new(result.GrepResult, result.GrepResult.Pattern, pageNumber, lineNumber,
                            matchText, columnNumber, useCustomEditor, customEditorName));
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
            if (PreviewFileContent && !formattedGrepResult.GrepResult.IsHexFile)
            {
                int lineNumber = 0;
                if (formattedGrepResult.GrepResult.Matches.Count > 0)
                {
                    lineNumber = formattedGrepResult.GrepResult.Matches[0].LineNumber;
                }

                PreviewFile(formattedGrepResult.GrepResult.FileNameReal, formattedGrepResult.GrepResult, lineNumber);
            }
        }

        #endregion

        #region Private Methods

        protected override void ResetOptions()
        {
            base.ResetOptions();

            IgnoreFilter = IgnoreFilterFile.None;
            Global = true;
        }

        internal void CancelSearch()
        {
            pauseCancelTokenSource?.Cancel();
        }

        private void DoSearchReplace(object? sender, DoWorkEventArgs e)
        {
            if (e.Argument is SearchReplaceCriteria param && !workerSearchReplace.CancellationPending)
            {
                try
                {
                    searchReplaceStartTime = DateTime.Now;

                    if (param.Operation == GrepOperation.Search || param.Operation == GrepOperation.SearchInResults)
                    {
                        LatestSearchDuration = TimeSpan.Zero;

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
                            else if (param.TypeOfTimeRangeFilter != FileTimeRange.None)
                            {
                                var (start, end) = GetStartEndTimesFromRange(
                                    param.TypeOfTimeRangeFilter, param.TimeRangeFrom, param.TimeRangeTo);
                                startTime = start;
                                endTime = end;
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

                        IEnumerable<FileData>? fileInfos = null;
                        IEnumerable<string>? files = null;

                        FileFilter fileParams = new(FileOrFolderPath, filePatternInclude, filePatternExclude,
                            param.TypeOfFileSearch == FileSearchType.Regex, param.UseGitIgnore, param.TypeOfFileSearch == FileSearchType.Everything,
                            param.IncludeSubfolder, param.MaxSubfolderDepth, param.IncludeHidden, param.IncludeBinary, param.IncludeArchive,
                            param.FollowSymlinks, sizeFrom, sizeTo, param.UseFileDateFilter, startTime, endTime,
                            param.SkipRemoteCloudStorageFiles, param.IgnoreFilterFile);

                        if (string.IsNullOrEmpty(SearchFor) &&
                            Settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern))
                        {
                            fileInfos = Utils.GetFileListIncludingArchives(fileParams, param.PauseCancelToken);
                        }
                        else if (param.Operation == GrepOperation.SearchInResults)
                        {
                            files = param.SearchInFiles;
                        }
                        else
                        {
                            files = Utils.GetFileListEx(fileParams, param.PauseCancelToken);
                        }

                        string[] searchPatterns = [];
                        if (param.SearchFor.StartsWith("file://", StringComparison.Ordinal))
                        {
                            Uri uri = new(SearchFor);
                            if (File.Exists(uri.LocalPath))
                            {
                                searchPatterns = File.ReadAllLines(uri.LocalPath)
                                    .Where(l => !string.IsNullOrEmpty(l)).ToArray();
                            }
                        }

                        param.PauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                        if (param.TypeOfSearch == SearchType.Regex)
                        {
                            if (!ValidateRegex(searchPatterns.Length > 0 ? searchPatterns : [param.SearchFor]))
                            {
                                e.Result = null;
                                return;
                            }
                        }

                        GrepCore grep = new()
                        {
                            SearchParams = new(
                                Settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                                Settings.Get<int>(GrepSettings.Key.ContextLinesAfter),
                                Settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                                Settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
                                SearchParallel),

                            FileFilter = new(FileOrFolderPath, filePatternInclude, filePatternExclude,
                                param.TypeOfFileSearch == FileSearchType.Regex, param.UseGitIgnore, param.TypeOfFileSearch == FileSearchType.Everything,
                                param.IncludeSubfolder, param.MaxSubfolderDepth, param.IncludeHidden, param.IncludeBinary, param.IncludeArchive,
                                param.FollowSymlinks, sizeFrom, sizeTo, param.UseFileDateFilter, startTime, endTime,
                                param.SkipRemoteCloudStorageFiles, param.IgnoreFilterFile),

                            // copy current values from the view model to GrepCore
                            // may be different from GrepSettings when running a script
                            SearchAutoStopCount = SearchAutoStopCount,
                            SearchAutoPauseCount = SearchAutoPauseCount,
                        };

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
                            e.Result = grep.CaptureGroupSearch(files, filePatternInclude, SearchOptions, param.TypeOfSearch, param.SearchFor, param.CodePage, param.PauseCancelToken);
                        }
                        else if (files != null)
                        {
                            if (searchPatterns.Length > 0)
                            {
                                e.Result = grep.SearchMultiple(files, param.TypeOfSearch, searchPatterns, SearchOptions, param.CodePage, param.PauseCancelToken);
                            }
                            else
                            {
                                e.Result = grep.Search(files, param.TypeOfSearch, param.SearchFor, SearchOptions, param.CodePage, param.PauseCancelToken);
                            }
                        }
                        else if (fileInfos != null)
                        {
                            e.Result = grep.ListFiles(fileInfos, SearchOptions, param.CodePage, param.PauseCancelToken);
                        }
                        grep.ProcessedFile -= GrepCore_ProcessedFile;
                    }
                    else
                    {
                        GrepCore grep = new()
                        {
                            SearchParams = new(
                                Settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                                Settings.Get<int>(GrepSettings.Key.ContextLinesAfter),
                                Settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                                Settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
                                SearchParallel)
                        };

                        grep.ProcessedFile += GrepCore_ProcessedFile;
                        e.Result = grep.Replace(param.ReplaceFiles, param.TypeOfSearch, param.SearchFor, param.ReplaceWith, SearchOptions, param.CodePage, param.PauseCancelToken);
                        grep.ProcessedFile -= GrepCore_ProcessedFile;
                    }
                }
                catch (OperationCanceledException)
                {
                    e.Result = null;
                    return;
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

        private GrepSearchOption SearchOptions
        {
            get
            {
                GrepSearchOption searchOptions = GrepSearchOption.None;
                if (Global)
                    searchOptions |= GrepSearchOption.Global;
                if (CaseSensitive)
                    searchOptions |= GrepSearchOption.CaseSensitive;
                if (Multiline)
                    searchOptions |= GrepSearchOption.Multiline;
                if (Singleline)
                    searchOptions |= GrepSearchOption.SingleLine;
                if (WholeWord)
                    searchOptions |= GrepSearchOption.WholeWord;
                if (BooleanOperators)
                    searchOptions |= GrepSearchOption.BooleanOperators;
                if (StopAfterNumMatches)
                    searchOptions |= GrepSearchOption.StopAfterNumMatches;
                if (PauseAfterNumMatches)
                    searchOptions |= GrepSearchOption.PauseAfterNumMatches;

                return searchOptions;
            }
        }

        private bool ValidateRegex(string[] patterns)
        {
            bool result = true;
            foreach (string pattern in patterns)
            {
                try
                {
                    Regex regex = new(pattern);
                }
                catch (ArgumentException regException)
                {
                    result = false;
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
                }
            }
            return result;
        }

        private static (DateTime startTime, DateTime endTime) GetStartEndTimesFromRange(
            FileTimeRange typeOfTimeRangeFilter, int timeRangeFrom, int timeRangeTo)
        {
            int low = Math.Min(timeRangeFrom, timeRangeTo);
            int high = Math.Max(timeRangeFrom, timeRangeTo);

            DateTime startTime = GetPastTime(high, typeOfTimeRangeFilter);
            DateTime endTime = GetPastTime(low, typeOfTimeRangeFilter);

            return (startTime, endTime);
        }

        private static DateTime GetPastTime(int value, FileTimeRange type)
        {
            try
            {
                return type switch
                {
                    FileTimeRange.Minutes => DateTime.Now.AddMinutes(-1 * value),
                    FileTimeRange.Hours => DateTime.Now.AddHours(-1 * value),
                    FileTimeRange.Days => DateTime.Now.AddDays(-1 * value),
                    FileTimeRange.Weeks => DateTime.Now.AddDays(-7 * value),
                    FileTimeRange.Months => DateTime.Now.AddMonths(-1 * value),
                    FileTimeRange.Years => DateTime.Now.AddYears(-1 * value),
                    _ => DateTime.UnixEpoch,
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                return DateTime.UnixEpoch;
            }
        }

        private readonly object lockObjOne = new();
        private void SearchProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (e.UserState is ProgressStatus progress)
                {
                    if (StopAfterNumMatches && progress.SuccessfulFiles > SearchAutoStopCount)
                    {
                        pauseCancelTokenSource?.Cancel();
                    }

                    if (PauseAfterNumMatches && progress.SuccessfulFiles >= SearchAutoPauseCount)
                    {
                        pauseCancelTokenSource?.Pause();
                        IsSearchReplacePaused = true;
                        PauseAfterNumMatches = false;
                    }

                    if (!progress.BeginSearch && progress.SearchResults != null && progress.SearchResults.Count > 0)
                    {
                        lock (lockObjOne)
                        {
                            ResultsViewModel.AddRange(progress.SearchResults);
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

        private readonly object lockObjTwo = new();
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
                        currentSearchFiles.Add(fileName);
                    }
                    else
                    {
                        currentSearchFiles.Remove(fileName);

                        if (currentSearchFiles.Count > 0)
                            fileName = currentSearchFiles.FirstOrDefault() ?? string.Empty;
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

                if (ResultsViewModel.SearchResults.Count > 0 && GrepSettings.Instance.Get<bool>(GrepSettings.Key.MaximizeResultsTreeOnSearch))
                {
                    IsResultTreeMaximized = true;
                }
            }
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(latestStatusMessage))
                StatusMessage = latestStatusMessage;
        }

        private void SearchReplaceCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            IsSearchReplacePaused = false;
            latestStatusMessage = string.Empty;
            idleTimer.Stop();
            try
            {
                // pauseCancelTokenSource should be non-null unless a script was 
                // canceled and it's already been disposed.
                bool canceled = pauseCancelTokenSource?.IsCancellationRequested ?? true;

                if (CurrentGrepOperation == GrepOperation.Search || CurrentGrepOperation == GrepOperation.SearchInResults)
                {
                    if (e.Result == null)
                    {
                        if (canceled)
                        {
                            StatusMessage = Resources.Main_Status_SearchCanceled;
                        }
                        else
                        {
                            StatusMessage = Resources.Main_Status_SearchCanceledOrFailed;
                        }
                    }
                    else
                    {
                        TimeSpan duration = DateTime.Now.Subtract(searchReplaceStartTime);
                        if (!IsScriptRunning)
                        {
                            LatestSearchDuration = duration;
                        }
                        int successFileCount = 0;
                        totalMatchCount = 0;
                        if (e.Result is List<GrepSearchResult> results)
                        {
                            successFileCount = results.Where(r => r.IsSuccess).Count();
                            totalMatchCount = results.Where(r => r.IsSuccess).SelectMany(r => r.Matches).Count();
                        }

                        int filesSearched = processedFiles - ResultsViewModel.FailureCount;

                        if (canceled)
                        {
                            StatusMessage = TranslationSource.Format(Resources.Main_Status_SearchCanceledIn01MatchesFoundIn2FilesOf3Searched,
                                duration.GetPrettyString(), totalMatchCount, successFileCount, filesSearched);
                        }
                        else
                        {
                            StatusMessage = TranslationSource.Format(Resources.Main_Status_SearchCompletedIn0_1MatchesFoundIn2FilesOf3Searched,
                                duration.GetPrettyString(), totalMatchCount, successFileCount, filesSearched);
                        }

                        if (IsEverythingSearchMode && Everything.EverythingSearch.CountMissingFiles > 0)
                        {
                            StatusMessage += enQuad + TranslationSource.Format(Resources.Main_Status_Excluded0MissingFiles, Everything.EverythingSearch.CountMissingFiles);
                        }
                        logger.Info($"{StatusMessage} {Resources.Main_SearchFor.Replace("_", "", StringComparison.Ordinal)} {SearchFor}\t{duration.GetPrettyString()}\t{totalMatchCount}\t{successFileCount}\t{filesSearched}");
                    }

                    FilesFound = ResultsViewModel.SearchResults.Count > 0;
                    CurrentGrepOperation = GrepOperation.None;
                    OnPropertyChanged(nameof(CurrentGrepOperation));
                    CanSearch = true;
                    needsClearUndo = true;
                    UpdateReplaceButtonTooltip(false);

                    if (FilesFound && GrepSettings.Instance.Get<bool>(GrepSettings.Key.SortAutomaticallyOnSearch))
                    {
                        SortResults();
                    }

                    if (FilesFound && GrepSettings.Instance.Get<bool>(GrepSettings.Key.MaximizeResultsTreeOnSearch))
                    {
                        IsResultTreeMaximized = true;
                    }

                    if (Application.Current is App app && app.AppArgs != null && !IsScriptRunning)
                    {
                        ProcessCommands(app.AppArgs);
                    }
                }
                else if (CurrentGrepOperation == GrepOperation.Replace)
                {
                    if (e.Result == null || ((int)e.Result) == -1)
                    {
                        if (canceled)
                        {
                            StatusMessage = Resources.Main_Status_ReplaceCanceled;
                        }
                        else
                        {
                            StatusMessage = Resources.Main_Status_ReplaceFailed;
                        }

                        if (IsScriptRunning)
                        {
                            AddScriptMessage(Resources.MessageBox_ReplaceFailedError + App.LogDir);
                        }
                        else if (!canceled)
                        {
                            MessageBox.Show(Resources.MessageBox_ReplaceFailedError + App.LogDir,
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                    else
                    {
                        if (canceled)
                        {
                            StatusMessage = TranslationSource.Format(Resources.Main_Status_ReplaceCanceledReplacedTextIn0Files,
                                (int)e.Result);
                        }
                        else
                        {
                            StatusMessage = TranslationSource.Format(Resources.Main_Status_ReplaceComplete0FilesReplaced,
                                (int)e.Result);
                        }
                        CanUndo = undoList.Count > 0;
                    }

                    CurrentGrepOperation = GrepOperation.None;
                    OnPropertyChanged(nameof(CurrentGrepOperation));
                    CanSearch = true;
                    ClearMatchCountStatus();
                    if (ReplaceType != ReplaceType.SelectedFiles)
                    {
                        ResultsViewModel.Clear();
                    }
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
                if ((pauseCancelTokenSource?.IsCancellationRequested ?? false) && IsScriptRunning)
                {
                    CancelScript();
                }

                currentSearchFiles.Clear();

                if (!IsScriptRunning)
                {
                    pauseCancelTokenSource?.Dispose();
                    pauseCancelTokenSource = null;
                }
                else
                {
                    // try to move on to next script statement
                    ContinueScript(pauseCancelTokenSource?.Token ?? default);

                    if (pauseCancelTokenSource?.IsCancellationRequested ?? false)
                    {
                        pauseCancelTokenSource?.Dispose();
                        pauseCancelTokenSource = null;
                    }
                }
            }
        }

        void GrepCore_ProcessedFile(object sender, ProgressStatus progress)
        {
            workerSearchReplace.ReportProgress((int)progress.ProcessedFiles, progress);
        }

        private void Browse()
        {
            FileFolderDialogWin32 fileFolderDialog = new();
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
                string newPath;
                if (fileFolderDialog.HasMultiSelectedFiles)
                {
                    newPath = fileFolderDialog.GetSelectedPaths(
                        TypeOfFileSearch == FileSearchType.Everything ? " | " : ";") ?? string.Empty;
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
                        logger.Error(TranslationSource.Format(Resources.MessageBox_SearchPathInTheFieldIsNotValid, SearchTextBoxLabel.Replace("_", "", StringComparison.Ordinal)));
                        AddScriptMessage(TranslationSource.Format(Resources.MessageBox_SearchPathInTheFieldIsNotValid, SearchTextBoxLabel.Replace("_", "", StringComparison.Ordinal)));

                        CancelScript();
                    }
                    else
                    {
                        MessageBox.Show(TranslationSource.Format(Resources.MessageBox_SearchPathInTheFieldIsNotValid, SearchTextBoxLabel.Replace("_", "", StringComparison.Ordinal)),
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

                UpdateBookmarks();
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
                ResultsViewModel.FolderPath = PathSearchText.BaseFolder;
                ResultsViewModel.TypeOfSearch = TypeOfSearch;

                if (SearchInResultsContent && CanSearchInResults)
                    CurrentGrepOperation = GrepOperation.SearchInResults;
                else
                    CurrentGrepOperation = GrepOperation.Search;
                StatusMessage = Resources.Main_Status_Searching;
                totalMatchCount = 0;

                if (PreviewModel != null)
                {
                    PreviewModel.FilePath = string.Empty;
                }
                PreviewTitle = string.Empty;
                // clear temp files from the previous search
                Utils.DeleteTempFolder();

                pauseCancelTokenSource ??= new();
                SearchReplaceCriteria workerParams = new(this, pauseCancelTokenSource.Token);
                if (SearchInResultsContent && CanSearchInResults)
                {
                    List<string> foundFiles = [];
                    foreach (FormattedGrepResult n in ResultsViewModel.SearchResults)
                        foundFiles.Add(n.GrepResult.FileNameReal);
                    workerParams.AddSearchFiles(foundFiles);
                }

                SearchParametersChanged = false;

                ClearMatchCountStatus();
                ResultsViewModel.Clear();
                UpdateReplaceButtonTooltip(true);
                processedFiles = 0;
                idleTimer.Start();
                IsSearchReplacePaused = false;
                workerSearchReplace.RunWorkerAsync(workerParams);

                switch (Settings.Get<FocusElement>(GrepSettings.Key.SetFocusElement))
                {
                    case FocusElement.SearchFor:
                        // reset the keyboard focus back to the SearchFor box
                        IsSearchForFocused = false;
                        IsSearchForFocused = true;
                        break;
                    case FocusElement.ResultsTree:
                    default:
                        // toggle value to move focus to the results tree and enable keyboard actions on the tree
                        ResultsViewModel.IsResultsTreeFocused = false;
                        ResultsViewModel.IsResultsTreeFocused = true;
                        break;
                }
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
                foreach (string pattern in UiUtils.SplitPattern(FilePattern, TypeOfFileSearch == FileSearchType.Regex))
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
                foreach (string pattern in UiUtils.SplitPattern(FilePatternIgnore, TypeOfFileSearch == FileSearchType.Regex))
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

        private static string GetValidateRegexMsg(string pattern)
        {
            try
            {
                Regex regex = new(pattern);
                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void ReplaceSwitch()
        {
            switch (ReplaceType)
            {
                case ReplaceType.ReplaceDialog:
                    Replace();
                    break;
                case ReplaceType.SelectedFiles:
                    ReplaceSelectedFiles();
                    break;
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

                List<string> roFiles = Utils.GetReadOnlyFiles(ResultsViewModel.GetList());
                if (!IsScriptRunning && roFiles.Count > 0)
                {
                    StringBuilder sb = new(Resources.MessageBox_SomeOfTheFilesCannotBeModifiedIfYouContinueTheseFilesWillBeSkipped);
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

                List<GrepSearchResult> replaceList = ResultsViewModel.GetWritableList()
                    .Where(sr => sr.Matches.Count != 0).ToList(); // filter out files with errors shown in results tree

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
                    ReplaceWindow dlg = new();
                    dlg.ViewModel.TypeOfSearch = TypeOfSearch;
                    dlg.ViewModel.SearchOptions = SearchOptions;
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
                    ExecuteReplace(replaceList, true);
                }
            }
            else if (IsScriptRunning)
            {
                // in a bad state, do not continue
                CancelScript();
                AddScriptMessage("Replace busy, script run stopped.");
            }
        }

        private void ExecuteReplace(List<GrepSearchResult> replaceList, bool clearAll)
        {
            pauseCancelTokenSource ??= new();

            // clear the undo list only on the first replace after search
            if (needsClearUndo)
            {
                CanUndo = false;
                Utils.DeleteUndoFolder();
                undoList.Clear();
                needsClearUndo = false;
            }

            List<ReplaceDef> toReplace = [];
            foreach (GrepSearchResult gsr in replaceList)
            {
                string filePath = gsr.FileNameReal;
                if (!gsr.IsReadOnlyFileType && !undoList.Any(r => r.OriginalFile == filePath) && gsr.Matches.Any(m => m.ReplaceMatch))
                {
                    var item = new ReplaceDef(filePath, gsr.Matches);
                    toReplace.Add(item);
                    undoList.Add(item);
                }
            }

            if (toReplace.Count > 0)
            {
                StatusMessage = Resources.Main_Status_Replacing;

                if (PreviewModel != null)
                {
                    PreviewModel.FilePath = string.Empty;
                }
                PreviewTitle = string.Empty;

                CurrentGrepOperation = GrepOperation.Replace;

                SearchReplaceCriteria workerParams = new(this, pauseCancelTokenSource.Token);

                workerParams.AddReplaceFiles(toReplace);

                ClearMatchCountStatus();
                if (clearAll)
                {
                    ResultsViewModel.Clear();
                }
                else
                {
                    ResultsViewModel.Clear(replaceList);
                }
                idleTimer.Start();
                workerSearchReplace.RunWorkerAsync(workerParams);
                UpdateBookmarks();

                // reset the keyboard focus back to the SearchFor box
                IsSearchForFocused = false;
                IsSearchForFocused = true;
            }
            else if (IsScriptRunning)
            {
                AddScriptMessage("Search list is empty, nothing to replace.");
                Dispatcher.CurrentDispatcher.Invoke(() => ContinueScript(pauseCancelTokenSource.Token));
            }
        }

        private void ReplaceSelectedFiles()
        {
            if (!ResultsViewModel.HasSelection)
            {
                MessageBox.Show(Resources.MessageBox_ThereAreNoFilesSelectedForReplace,
                    Resources.MessageBox_DnGrep + " " + Resources.MessageBox_Replace,
                    MessageBoxButton.OK, MessageBoxImage.Information,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);

                return;
            }

            if (CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy &&
                ResultsViewModel.HasSelection)
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

                List<string> roFiles = Utils.GetReadOnlyFiles(ResultsViewModel.GetSelectedFiles());
                if (!IsScriptRunning && roFiles.Count > 0)
                {
                    StringBuilder sb = new(Resources.MessageBox_SomeOfTheFilesCannotBeModifiedIfYouContinueTheseFilesWillBeSkipped);
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

                List<GrepSearchResult> replaceList = ResultsViewModel.GetWritableSelectedFiles()
                    .Where(sr => sr.Matches.Count != 0).ToList(); // filter out files with errors shown in results tree

                // mark all matches for replace
                foreach (GrepSearchResult gsr in replaceList)
                {
                    foreach (var match in gsr.Matches)
                    {
                        match.ReplaceMatch = true;
                    }
                }

                ExecuteReplace(replaceList, false);
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
                    bool result = GrepCore.Undo(undoList);
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
                        CanUndo = false;
                        Utils.DeleteUndoFolder();
                        undoList.Clear();
                        needsClearUndo = false;
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

            int maxSearchBookmarks = Settings.Get<int>(GrepSettings.Key.MaxSearchBookmarks);
            int maxPathBookmarks = Settings.Get<int>(GrepSettings.Key.MaxPathBookmarks);
            int maxExtensionBookmarks = Settings.Get<int>(GrepSettings.Key.MaxExtensionBookmarks);

            // Update bookmarks, moving current to the top of the list
            SearchFor = UpdateMRUList(MRUType.SearchFor, FastSearchBookmarks, SearchFor, maxSearchBookmarks);
            ReplaceWith = UpdateMRUList(MRUType.ReplaceWith, FastReplaceBookmarks, ReplaceWith, maxSearchBookmarks);
            FilePattern = UpdateMRUList(MRUType.IncludePattern, FastFileMatchBookmarks, FilePattern, maxExtensionBookmarks);
            FilePatternIgnore = UpdateMRUList(MRUType.ExcludePattern, FastFileNotMatchBookmarks, FilePatternIgnore, maxExtensionBookmarks);
            FileOrFolderPath = UpdateMRUList(MRUType.SearchPath, FastPathBookmarks, FileOrFolderPath, maxPathBookmarks);

            inUpdateBookmarks = false;
        }

        private static string UpdateMRUList(MRUType valueType, ObservableCollection<MRUViewModel> list,
            string value, int maxCount)
        {
            string returnValue = value;
            // keep pinned items in order at the top of the list
            var item = list.FirstOrDefault(b => string.Equals(b.StringValue, value, StringComparison.Ordinal));
            int newIndex = IndexOfFirstUnpinned(list);
            if (item != null)
            {
                if (!item.IsPinned)
                {
                    int currentIndex = list.IndexOf(item);
                    if (list.IndexOf(item) != newIndex)
                    {
                        list.RemoveAt(currentIndex);
                        list.Insert(newIndex, item);
                        returnValue = item.StringValue;
                    }
                }
            }
            else
            {
                list.Insert(newIndex, new MRUViewModel(valueType, value));
            }

            while (list.Count > maxCount)
                list.RemoveAt(list.Count - 1);

            return returnValue;
        }

        private void OnMRUPinChanged(MRUViewModel item)
        {
            if (item != null)
            {
                ObservableCollection<MRUViewModel>? list = null;
                switch (item.ValueType)
                {
                    case MRUType.SearchPath:
                        list = FastPathBookmarks;
                        break;
                    case MRUType.IncludePattern:
                        list = FastFileMatchBookmarks;
                        break;
                    case MRUType.ExcludePattern:
                        list = FastFileNotMatchBookmarks;
                        break;
                    case MRUType.SearchFor:
                        list = FastSearchBookmarks;
                        break;
                    case MRUType.ReplaceWith:
                        list = FastReplaceBookmarks;
                        break;
                }

                if (list != null)
                {
                    int currentIndex = list.IndexOf(item);
                    if (currentIndex != -1)
                    {
                        list.RemoveAt(currentIndex);

                        int newIndex = IndexOfFirstUnpinned(list);
                        list.Insert(newIndex, item);
                    }
                }
            }
        }

        private static int IndexOfFirstUnpinned(IList<MRUViewModel> list)
        {
            for (int idx = 0; idx < list.Count; idx++)
            {
                if (!list[idx].IsPinned)
                    return idx;
            }
            return list.Count;
        }

        private void Cancel()
        {
            if (CurrentGrepOperation != GrepOperation.None)
            {
                pauseCancelTokenSource?.Cancel();
            }
        }

        private void PauseResume()
        {
            if ((IsScriptRunning || CurrentGrepOperation != GrepOperation.None) &&
                pauseCancelTokenSource != null)
            {
                if (pauseCancelTokenSource.IsPaused)
                {
                    pauseCancelTokenSource.Resume();
                    IsSearchReplacePaused = false;
                }
                else
                {
                    pauseCancelTokenSource.Pause();
                    IsSearchReplacePaused = true;
                }
            }
        }

        internal bool ConfirmScriptExit()
        {
            if (IsScriptRunning && GrepSettings.Instance.Get<bool>(GrepSettings.Key.ConfirmExitScript))
            {
                if (pauseCancelTokenSource != null && !pauseCancelTokenSource.IsPaused)
                {
                    pauseCancelTokenSource.Pause();
                    IsSearchReplacePaused = true;
                }

                MessageBoxCustoms customs = new()
                {
                    YesButtonText = Resources.MessageBox_YesExitAnyway,
                    DoNotAskAgainCheckboxText = Resources.MessageBox_DoNotShowThisMessageAgain,
                    ShowDoNotAskAgainCheckbox = true,
                };

                var answer = CustomMessageBox.Show(Resources.MessageBox_AScriptIsRunning + Environment.NewLine +
                    Resources.MessageBox_DoYouWantToStopTheScriptAndExit,
                    Resources.MessageBox_DnGrep,
                    MessageBoxButtonEx.YesNo, MessageBoxImage.Question,
                    MessageBoxResultEx.No, customs,
                    TranslationSource.Instance.FlowDirection);

                if (answer.Result == MessageBoxResultEx.No &&
                    pauseCancelTokenSource != null && pauseCancelTokenSource.IsPaused)
                {
                    pauseCancelTokenSource.Resume();
                    IsSearchReplacePaused = false;
                }

                if (answer.DoNotAskAgain)
                {
                    GrepSettings.Instance.Set(GrepSettings.Key.ConfirmExitScript, false);
                }

                return answer.Result == MessageBoxResultEx.Yes;
            }

            return true;
        }

        internal bool ConfirmSearchExit()
        {
            TimeSpan threshold = TimeSpan.FromMinutes(GrepSettings.Instance.Get<double>(GrepSettings.Key.ConfirmExitSearchDuration));
            bool pastThreshold = CurrentSearchDuration > threshold || LatestSearchDuration > threshold;

            if (pastThreshold && GrepSettings.Instance.Get<bool>(GrepSettings.Key.ConfirmExitSearch))
            {
                bool running = CurrentSearchDuration > TimeSpan.Zero;
                if (running && pauseCancelTokenSource != null && !pauseCancelTokenSource.IsPaused)
                {
                    pauseCancelTokenSource.Pause();
                    IsSearchReplacePaused = true;
                }

                MessageBoxCustoms customs = new()
                {
                    YesButtonText = Resources.MessageBox_YesExitAnyway,
                    DoNotAskAgainCheckboxText = Resources.MessageBox_DoNotShowThisMessageAgain,
                    ShowDoNotAskAgainCheckbox = true,
                };

                string msg = running ?
                    Resources.MessageBox_TheSearchIsRunning + Environment.NewLine +
                    Resources.MessageBox_DoYouWantToStopTheSearchAndExit :
                    Resources.MessageBox_SearchResultsWillBeClearedOnExit + Environment.NewLine +
                    Resources.MessageBox_DoYouWantToExit;

                var answer = CustomMessageBox.Show(msg,
                    Resources.MessageBox_DnGrep,
                    MessageBoxButtonEx.YesNo, MessageBoxImage.Question,
                    MessageBoxResultEx.No, customs,
                    TranslationSource.Instance.FlowDirection);

                if (answer.Result == MessageBoxResultEx.No &&
                    pauseCancelTokenSource != null && pauseCancelTokenSource.IsPaused)
                {
                    pauseCancelTokenSource.Resume();
                    IsSearchReplacePaused = false;
                }

                if (answer.DoNotAskAgain)
                {
                    GrepSettings.Instance.Set(GrepSettings.Key.ConfirmExitSearch, false);
                }

                return answer.Result == MessageBoxResultEx.Yes;
            }

            return true;
        }

        private void ShowOptions()
        {
            UIServices.SetBusyState();
            SaveSettings();
            OptionsView optionsForm = new();
            if (ParentWindow != null)
            {
                optionsForm.Owner = ParentWindow;
            }
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

            if (optionsForm.PluginCacheCleared)
            {
                StatusMessage = string.Empty;
                ClearMatchCountStatus();
                ResultsViewModel.Clear();
                UpdateReplaceButtonTooltip(true);
            }

            ResultsViewModel.RaiseSettingsPropertiesChanged();
        }

        private static void ShowKeyboardOptions()
        {
            UIServices.SetBusyState();
            GrepSearchResultsViewModel.Initialize();
            BookmarkListViewModel.Initialize();
            ReplaceViewModel.Initialize();
            ScriptViewModel.Initialize();

            KeyboardShortcutWindow keyboardForm = new()
            {
                Owner = Application.Current.MainWindow
            };
            keyboardForm.ShowDialog();
        }

        private static void ShowHelp()
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = @"https://github.com/dnGrep/dnGrep/wiki",
                UseShellExecute = true
            };
            using var proc = Process.Start(startInfo);
        }

        private static void ShowAbout()
        {
            AboutWindow aboutForm = new()
            {
                Owner = Application.Current.MainWindow
            };
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
        private static readonly HashSet<string> bookmarkParameters =
        [
            nameof(SearchFor),
            nameof(ReplaceWith),
            nameof(FileOrFolderPath), // when in Everything mode
            nameof(FilePattern),
            nameof(FilePatternIgnore),
            nameof(TypeOfFileSearch),
            nameof(TypeOfSearch),
            nameof(Global),
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
            nameof(IgnoreFilter),
            nameof(SkipRemoteCloudStorageFiles),
            nameof(IncludeArchive),
            nameof(CodePage),
        ];

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
                Global = Global,
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
                IgnoreFilterName = IgnoreFilter.Name,
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
                    Bookmark? bmk = BookmarkLibrary.Instance.Find(current);
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
                    Bookmark? bmk = BookmarkLibrary.Instance.Find(current);
                    string folder = Path.TrimEndingDirectorySeparator(FileOrFolderPath);
                    if (bmk != null && bmk.FolderReferences.Contains(folder))
                    {
                        bmk.FolderReferences.Remove(folder);
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
                    Bookmark? bmk = BookmarkLibrary.Instance.Find(current);
                    if (bmk != null)
                    {
                        int count = bmk.FolderReferences.Count(s => s != Path.TrimEndingDirectorySeparator(FileOrFolderPath));
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

            bookmarkWindow?.ViewModel.SynchToLibrary();
        }

        private void AddBookmark(string bookmarkName, bool associateWithFolder)
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

            if (associateWithFolder && !string.IsNullOrWhiteSpace(FileOrFolderPath))
            {
                BookmarkLibrary.Instance.AddFolderReference(current, FileOrFolderPath);
                IsFolderBookmarked = true;
            }

            if (modified)
            {
                BookmarkLibrary.Save();

                bookmarkWindow?.ViewModel.SynchToLibrary();
            }
        }

        private void RemoveBookmark(string bookmarkName, bool disassociateWithFolder)
        {
            bool modified = false;

            if (!string.IsNullOrEmpty(bookmarkName))
            {
                var bmk = BookmarkLibrary.Instance.Bookmarks
                    .FirstOrDefault(b => bookmarkName.Equals(b.BookmarkName, StringComparison.OrdinalIgnoreCase));

                if (bmk != null)
                {
                    if (disassociateWithFolder)
                    {
                        string folder = Path.TrimEndingDirectorySeparator(FileOrFolderPath);
                        if (bmk.FolderReferences.Contains(folder))
                        {
                            bmk.FolderReferences.Remove(folder);
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
                Bookmark? bmk = BookmarkLibrary.Instance.Find(current);
                if (bmk != null)
                {
                    string folder = Path.TrimEndingDirectorySeparator(FileOrFolderPath);
                    if (disassociateWithFolder && IsFolderBookmarked && bmk.FolderReferences.Contains(folder))
                    {
                        bmk.FolderReferences.Remove(folder);
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

                bookmarkWindow?.ViewModel.SynchToLibrary();
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
                    IgnoreFilter = SetFilter(bmk.IgnoreFilterName);
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

                    Global = bmk.Global;
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
                bookmarkWindow = new(bk =>
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
                string? selectedPath = null;
                if (argument is string path && !string.IsNullOrEmpty(path))
                {
                    selectedPath = path;
                }

                var (success, message) = FileOperations.CopyFiles(
                    ResultsViewModel.GetList(), PathSearchText, selectedPath, IsScriptRunning);

                if (!string.IsNullOrEmpty(message))
                {
                    AddScriptMessage(message);
                }

                if (IsScriptRunning && !success)
                {
                    CancelScript();
                }
            }
        }

        private void MoveFiles(object argument)
        {
            if (FilesFound)
            {
                string? selectedPath = null;
                if (argument is string path && !string.IsNullOrEmpty(path))
                {
                    selectedPath = path;
                }

                var (success, filesMoved, message) = FileOperations.MoveFiles(
                    ResultsViewModel.GetList(), PathSearchText, selectedPath, IsScriptRunning);

                if (!string.IsNullOrEmpty(message))
                {
                    AddScriptMessage(message);
                }

                if (IsScriptRunning && !success)
                {
                    CancelScript();
                }

                if (success)
                {
                    ClearMatchCountStatus();
                    ResultsViewModel.DeselectAllItems();
                    List<FormattedGrepResult> toRemove = [];
                    foreach (var gr in ResultsViewModel.SearchResults)
                    {
                        if (filesMoved.Contains(gr.GrepResult.FileNameReal))
                        {
                            toRemove.Add(gr);
                        }
                    }
                    foreach (var gr in toRemove)
                    {
                        ResultsViewModel.SearchResults.Remove(gr);
                    }

                    FilesFound = false;
                }
            }
        }

        private void DeleteFiles()
        {
            if (FilesFound)
            {
                var (success, filesDeleted, message) = FileOperations.DeleteFiles(
                    ResultsViewModel.GetList(), IsScriptRunning, true);

                if (!string.IsNullOrEmpty(message))
                {
                    AddScriptMessage(message);
                }

                if (IsScriptRunning && !success)
                {
                    CancelScript();
                }

                if (success)
                {
                    ClearMatchCountStatus();
                    ResultsViewModel.DeselectAllItems();
                    List<FormattedGrepResult> toRemove = [];
                    foreach (var gr in ResultsViewModel.SearchResults)
                    {
                        if (filesDeleted.Contains(gr.GrepResult.FileNameReal))
                        {
                            toRemove.Add(gr);
                        }
                    }
                    foreach (var gr in toRemove)
                    {
                        ResultsViewModel.SearchResults.Remove(gr);
                    }
                    FilesFound = false;
                }
            }
        }

        private void CopyToClipboard(bool includeMatchCounts)
        {
            StringBuilder sb = new();
            foreach (GrepSearchResult result in ResultsViewModel.GetList())
            {
                if (includeMatchCounts)
                {
                    int matchCount = result.Matches == null ? 0 : result.Matches.Count;
                    int lineCount = result.Matches?.Where(r => r.LineNumber > 0)
                       .Select(r => r.LineNumber).Distinct().Count() ?? 0;

                    sb.AppendLine(TranslationSource.Format(Resources.Main_ResultList_CountMatchesOnLines, result.FileNameReal, matchCount, lineCount));
                }
                else
                {
                    sb.AppendLine(result.FileNameReal);
                }
            }
            NativeMethods.SetClipboardText(sb.ToString());
        }

        private void CopyResults()
        {
            // can be a long process if the results are not yet cached
            UIServices.SetBusyState();
            NativeMethods.SetClipboardText(ReportWriter.GetResultsAsText(ResultsViewModel.GetList(), ResultsViewModel.TypeOfSearch));
        }

        private void ShowReportOptions()
        {
            ReportOptionsViewModel vm = new(ResultsViewModel);
            ReportOptionsWindow dlg = new(vm)
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();
        }

        private async void SaveResultsToFile(string? reportType)
        {
            if (FilesFound)
            {
                SaveFileDialog dlg = new();

                switch (reportType)
                {
                    default:
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
                                default:
                                case "Report":
                                    ReportWriter.SaveResultsReport(ResultsViewModel.GetList(), BooleanOperators, SearchFor, GetSearchOptions(), dlg.FileName);
                                    break;
                                case "Text":
                                    ReportWriter.SaveResultsAsText(ResultsViewModel.GetList(), ResultsViewModel.TypeOfSearch, dlg.FileName);
                                    break;
                                case "CSV":
                                    ReportWriter.SaveResultsAsCSV(ResultsViewModel.GetList(), ResultsViewModel.TypeOfSearch, dlg.FileName);
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
                ReportWriter.SaveResultsReport(ResultsViewModel.GetList(), BooleanOperators, SearchFor, GetSearchOptions(), args.ReportPath);
            }
            if (!string.IsNullOrWhiteSpace(args.TextPath))
            {
                ReportWriter.SaveResultsAsText(ResultsViewModel.GetList(), ResultsViewModel.TypeOfSearch, args.TextPath);
            }
            if (!string.IsNullOrWhiteSpace(args.CsvPath))
            {
                ReportWriter.SaveResultsAsCSV(ResultsViewModel.GetList(), ResultsViewModel.TypeOfSearch, args.CsvPath);
            }
            if (args.Exit)
            {
                Application.Current.MainWindow.Close();
            }
        }

#pragma warning disable IDE0075
        public bool CanReplace
        {
            get
            {
                var writableFiles = ResultsViewModel.GetWritableList();
                var hasWritableFiles = writableFiles.Count != 0 &&
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
#pragma warning restore IDE0075

        private void UpdateReplaceButtonTooltip(bool clear)
        {
            ReplaceButtonToolTip = string.Empty;
            if (!clear && !CanReplace && FilesFound)
            {
                var writableFiles = ResultsViewModel.GetWritableList();
                var hasWritableFiles = writableFiles.Count != 0;
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
                return ResultsViewModel.SearchResults.Count > 0 &&
                    CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy;
            }
        }

        private void SortResults()
        {
            if (!CanSortResults) return;

            var selections = ResultsViewModel.SelectedItems;
            using (var d = Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                ResultsViewModel.DeselectAllItems();
                var list = ResultsViewModel.SearchResults.ToList();
                ClearMatchCountStatus();
                ResultsViewModel.SearchResults.Clear();

                switch (SortType)
                {
                    case SortType.FileNameOnly:
                        list.Sort(new FileNameOnlyComparer(SortDirection, NaturalSort));
                        break;
                    case SortType.FileTypeAndName:
                        list.Sort(new FileTypeAndNameComparer(SortDirection, NaturalSort));
                        break;
                    case SortType.FileNameDepthFirst:
                    default:
                        list.Sort(new FileNameDepthFirstComparer(SortDirection, NaturalSort));
                        break;
                    case SortType.FileNameBreadthFirst:
                        list.Sort(new FileNameBreadthFirstComparer(SortDirection, NaturalSort));
                        break;
                    case SortType.Size:
                        list.Sort(new FileSizeComparer(SortDirection));
                        break;
                    case SortType.Date:
                        list.Sort(new FileDateComparer(SortDirection));
                        break;
                    case SortType.MatchCount:
                        list.Sort(new MatchCountComparer(SortDirection));
                        break;
                    case SortType.ReadOnly:
                        list.Sort(new ReadOnlyComparer(SortDirection));
                        break;
                }

                ResultsViewModel.AddRange(list);
            }
            ResultsViewModel.SelectItems(selections);
        }

        private string GetSearchOptions()
        {
            StringBuilder sb = new();

            List<string> options = [];

            var excludePatterns = Utils.GetCompositeIgnoreList(FileOrFolderPath, FilePatternIgnore,
                TypeOfFileSearch == FileSearchType.Regex, IgnoreFilter.FilePath);

            sb.Append(Resources.ReportSummary_SearchFor).Append(" '").Append(SearchFor).AppendLine("'")
              .AppendFormat(Resources.ReportSummary_UsingTypeOfSeach, TypeOfSearch.ToLocalizedString())
              .AppendLine();

            if (!Global) options.Add(Resources.ReportSummary_NotGlobal);
            if (CaseSensitive) options.Add(Resources.ReportSummary_CaseSensitive);
            if (WholeWord) options.Add(Resources.ReportSummary_WholeWord);
            if (Multiline) options.Add(Resources.ReportSummary_Multiline);
            if (Singleline) options.Add(Resources.ReportSummary_DotAsNewline);
            if (BooleanOperators) options.Add(Resources.ReportSummary_BooleanOperators);
            if (SearchInResultsContent) options.Add(Resources.ReportSummary_SearchInResults);
            if (StopAfterNumMatches)
            {
                if (SearchAutoStopCount == 1)
                    options.Add(Resources.ReportSummary_StopAfterFirstMatch);
                else
                    options.Add(TranslationSource.Format(Resources.Main_StopAfter0Matches, SearchAutoStopCount));
            }
            if (options.Count > 0)
                sb.AppendLine(string.Join(", ", options));
            sb.AppendLine();

            sb.Append(Resources.ReportSummary_SearchIn).Append(' ').AppendLine(FileOrFolderPath)
              .Append(Resources.ReportSummary_FilePattern).Append(' ').AppendLine(FilePattern);

            if (excludePatterns.Count == 1)
            {
                if (!string.IsNullOrEmpty(excludePatterns[0].pattern))
                {
                    sb.Append(Resources.ReportSummary_ExcludePattern).Append(' ').AppendLine(excludePatterns[0].pattern);
                }
            }
            else
            {
                sb.AppendLine(Resources.ReportSummary_ExcludePattern);
                foreach (var (path, pattern) in excludePatterns)
                {
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        sb.Append("  ").Append(path).Append(": ").AppendLine(pattern);
                    }
                }
            }

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
                sb.AppendLine(string.Join(", ", (ReadOnlySpan<string?>)[.. options]));

            if (UseFileSizeFilter == FileSizeFilter.Yes)
                sb.AppendFormat(Resources.ReportSummary_SizeFrom0To1KB, SizeFrom, SizeTo).AppendLine();

            if (UseFileDateFilter != FileDateFilter.None && TypeOfTimeRangeFilter == FileTimeRange.Dates)
            {
                sb.AppendFormat(Resources.ReportSummary_Type0DateFrom1To2, UseFileDateFilter.ToLocalizedString(),
                    StartDate.HasValue ? StartDate.Value.ToShortDateString() : "*",
                    EndDate.HasValue ? EndDate.Value.ToShortDateString() : "*").AppendLine();
            }

            if (UseFileDateFilter != FileDateFilter.None && TypeOfTimeRangeFilter != FileTimeRange.Dates &&
                TypeOfTimeRangeFilter != FileTimeRange.None)
            {
                sb.AppendFormat(Resources.ReportSummary_Type0DateInThePast1To23,
                    UseFileDateFilter.ToLocalizedString(),
                    TimeRangeFrom, TimeRangeTo,
                    TypeOfTimeRangeFilter.ToLocalizedString())
                  .AppendLine();
            }

            if (CodePage != -1)
            {
                string? encoding = Encodings.Where(r => r.Value == CodePage).Select(r => r.Key).FirstOrDefault();
                sb.Append(Resources.ReportSummary_Encoding).Append(' ').AppendLine(encoding);
            }

            return sb.ToString();
        }

        private void OpenTestPatternWindow()
        {
            try
            {
                SaveSettings();
                TestPattern testForm = new();
                Point pt = new(40, 40);
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

        private static void CheckVersion()
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

        private static async void CheckForUpdates(bool fromCommand)
        {
            try
            {
                PublishedVersionExtractor versionChk = new();
                string version = await PublishedVersionExtractor.QueryLatestVersion();

                if (!string.IsNullOrEmpty(version))
                {
                    string currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
                    if (PublishedVersionExtractor.IsUpdateNeeded(currentVersion, version))
                    {
                        if (MessageBox.Show(TranslationSource.Format(Resources.MessageBox_NewVersionOfDnGREP0IsAvailableForDownload, version) +
                            Environment.NewLine + Resources.MessageBox_WouldYouLikeToDownloadItNow,
                            Resources.MessageBox_DnGrep + " " + Resources.MessageBox_NewVersion,
                            MessageBoxButton.YesNo, MessageBoxImage.Information,
                            MessageBoxResult.Yes, TranslationSource.Instance.FlowDirection) == MessageBoxResult.Yes)
                        {
                            ProcessStartInfo startInfo = new()
                            {
                                FileName = "https://github.com/dnGrep/dnGrep/releases/latest",
                                UseShellExecute = true
                            };
                            using var proc = Process.Start(startInfo);
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
            workerSearchReplace.WorkerReportsProgress = true;
            workerSearchReplace.DoWork += DoSearchReplace;
            workerSearchReplace.RunWorkerCompleted += SearchReplaceCompleted;
            workerSearchReplace.ProgressChanged += SearchProgressChanged;

            DiginesisHelpProvider.HelpNamespace = @"https://github.com/dnGrep/dnGrep/wiki/";
            DiginesisHelpProvider.ShowHelp = true;
        }

        private void PopulateTimeIntervals()
        {
            TimeRanges.Clear();
            var selectedRange = PastTimeRangeFilter;
            PastTimeRangeFilter = FileTimeRange.None;
            TimeRanges.Add(new(FileTimeRange.Minutes, Resources.Main_DatePastMinutes));
            TimeRanges.Add(new(FileTimeRange.Hours, Resources.Main_DatePastHours));
            TimeRanges.Add(new(FileTimeRange.Days, Resources.Main_DatePastDays));
            TimeRanges.Add(new(FileTimeRange.Weeks, Resources.Main_DatePastWeeks));
            TimeRanges.Add(new(FileTimeRange.Months, Resources.Main_DatePastMonths));
            TimeRanges.Add(new(FileTimeRange.Years, Resources.Main_DatePastYears));

            PastTimeRangeFilter = selectedRange;
        }

        private void PopulateEncodings()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            KeyValuePair<string, int> defaultValue = new(Resources.Main_EncodingAutoDetection, -1);

            List<KeyValuePair<string, int>> tempUni = [];
            List<KeyValuePair<string, int>> tempEnc = [];
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

        private void PopulateIgnoreFilters(bool firstTime)
        {
            var selectedFilter = firstTime ?
                GrepSettings.Instance.Get<string>(GrepSettings.Key.IgnoreFilter) :
                IgnoreFilter.Name;

            if (IgnoreFilterList.Count == 0)
            {
                IgnoreFilterList.Add(IgnoreFilterFile.None);
            }
            else
            {
                IgnoreFilter = IgnoreFilterFile.None;
                // do not empty the list: the IgnoreFilter will be set to null
                while (IgnoreFilterList.Count > 1)
                {
                    IgnoreFilterList.RemoveAt(IgnoreFilterList.Count - 1);
                }
            }

            string dataFolder = Path.Combine(DirectoryConfiguration.Instance.DataDirectory, IgnoreFilterFolder);
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

#pragma warning disable CA1868
            HashSet<string> names = [];
            foreach (string fileName in Directory.GetFiles(dataFolder, "*.ignore", SearchOption.AllDirectories))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);

                if (!names.Contains(name))
                {
                    names.Add(name);
                    IgnoreFilterList.Add(new IgnoreFilterFile(name, fileName));
                }
            }
#pragma warning restore CA1868

            IgnoreFilter = SetFilter(selectedFilter);
        }

        private IgnoreFilterFile SetFilter(string filterName)
        {
            if (!string.IsNullOrEmpty(filterName))
            {
                var filter = IgnoreFilterList.FirstOrDefault(f => f.Name.Equals(filterName, StringComparison.OrdinalIgnoreCase));
                if (filter != null)
                {
                    return filter;
                }
            }
            return IgnoreFilterFile.None;
        }

        private void BookmarkForm_UseBookmark(object? sender, EventArgs e)
        {
            var bmk = bookmarkWindow?.ViewModel.SelectedBookmark;
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
                    IgnoreFilter = SetFilter(bmk.IgnoreFilterName);
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

                    Global = bmk.Global;
                    CaseSensitive = bmk.CaseSensitive;
                    WholeWord = bmk.WholeWord;
                    Multiline = bmk.Multiline;
                    Singleline = bmk.Singleline;
                    BooleanOperators = bmk.BooleanOperators;
                }
            }
            // edge case: if a bookmark is edited to match the current state
            // applying the bookmark doesn't change any properties and the
            // bookmark star indicator doesn't update.
            UpdateState(nameof(FilePattern));
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
                    IgnoreFilter = SetFilter(bmk.IgnoreFilterName);
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

                    Global = bmk.Global;
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
            int maxSearchBookmarks = Settings.Get<int>(GrepSettings.Key.MaxSearchBookmarks);
            int maxPathBookmarks = Settings.Get<int>(GrepSettings.Key.MaxPathBookmarks);
            int maxExtensionBookmarks = Settings.Get<int>(GrepSettings.Key.MaxExtensionBookmarks);

            //Saving bookmarks
            CopyMRUListToSettings(FastSearchBookmarks, maxSearchBookmarks, GrepSettings.Key.FastSearchBookmarks);
            CopyMRUListToSettings(FastReplaceBookmarks, maxSearchBookmarks, GrepSettings.Key.FastReplaceBookmarks);
            CopyMRUListToSettings(FastFileMatchBookmarks, maxExtensionBookmarks, GrepSettings.Key.FastFileMatchBookmarks);
            CopyMRUListToSettings(FastFileNotMatchBookmarks, maxExtensionBookmarks, GrepSettings.Key.FastFileNotMatchBookmarks);
            CopyMRUListToSettings(FastPathBookmarks, maxPathBookmarks, GrepSettings.Key.FastPathBookmarks);
        }

        private static void CopyMRUListToSettings(IList<MRUViewModel> list, int maxCount, string itemKey)
        {
            List<MostRecentlyUsed> items = [];
            for (int i = 0; i < list.Count && i < maxCount; i++)
            {
                items.Add(list[i].AsMostRecentlyUsed());
            }
            Settings.Set(itemKey, items);
        }

        private static void OpenAppDataFolder()
        {
            string dataFolder = DirectoryConfiguration.Instance.DataDirectory;
            if (!dataFolder.EndsWith(Path.DirectorySeparatorChar))
            {
                dataFolder += Path.DirectorySeparatorChar;
            }
            ProcessStartInfo startInfo = new()
            {
                FileName = "explorer.exe",
                Arguments = "/open, \"" + dataFolder + "\"",
                UseShellExecute = false,
            };

            Process.Start(startInfo);
        }

        private static void OpenAppLogsFolder()
        {
            string logFolder = DirectoryConfiguration.Instance.LogDirectory;
            if (!logFolder.EndsWith(Path.DirectorySeparatorChar))
            {
                logFolder += Path.DirectorySeparatorChar;
            }
            ProcessStartInfo startInfo = new()
            {
                FileName = "explorer.exe",
                Arguments = "/open, \"" + logFolder + "\"",
                UseShellExecute = false,
            };

            Process.Start(startInfo);
        }


        private void PreviewFile(string filePath, GrepSearchResult result, int line)
        {
            if (PreviewFileContent)
            {
                string displayFileName = filePath;

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
                        displayFileName = result.FileNameDisplayed;
                        filePath = tempFile;
                    }
                }

                if (!string.IsNullOrEmpty(result.FileInfo.TempFile))
                {
                    filePath = result.FileInfo.TempFile;
                    displayFileName = result.FileNameDisplayed + " " + Resources.Preview_Title_AsText;
                }

                string basePath = PathSearchText.BaseFolder;
                if (!string.IsNullOrWhiteSpace(basePath) &&
                    displayFileName.Contains(basePath, StringComparison.CurrentCultureIgnoreCase))
                {
                    displayFileName = displayFileName[basePath.Length..].TrimStart('\\');
                }

                PreviewTitle = displayFileName;

                if (PreviewModel != null)
                {
                    // order of property setting matters here:
                    PreviewModel.GrepResult = result;
                    PreviewModel.LineNumber = line;
                    PreviewModel.Encoding = result.Encoding;
                    PreviewModel.FilePath = filePath;
                }

                if (!DockVM.IsPreviewDocked)
                    PreviewShow?.Invoke(this, EventArgs.Empty);
            }
        }

        private void DeleteMRUItem(MRUViewModel? item)
        {
            if (item != null)
            {
                ObservableCollection<MRUViewModel>? list = null;
                string? itemKey = null;
                switch (item.ValueType)
                {
                    case MRUType.SearchPath:
                        list = FastPathBookmarks;
                        itemKey = GrepSettings.Key.FastPathBookmarks;
                        break;
                    case MRUType.IncludePattern:
                        list = FastFileMatchBookmarks;
                        itemKey = GrepSettings.Key.FastFileMatchBookmarks;
                        break;
                    case MRUType.ExcludePattern:
                        list = FastFileNotMatchBookmarks;
                        itemKey = GrepSettings.Key.FastFileNotMatchBookmarks;
                        break;
                    case MRUType.SearchFor:
                        list = FastSearchBookmarks;
                        itemKey = GrepSettings.Key.FastSearchBookmarks;
                        break;
                    case MRUType.ReplaceWith:
                        list = FastReplaceBookmarks;
                        itemKey = GrepSettings.Key.FastReplaceBookmarks;
                        break;
                }

                if (list != null && itemKey != null)
                {
                    list.Remove(item);

                    List<MostRecentlyUsed> items = [];
                    for (int i = 0; i < list.Count; i++)
                    {
                        items.Add(list[i].AsMostRecentlyUsed());
                    }
                    Settings.Set(itemKey, items);
                }
            }
        }

        public class IgnoreFilterFile(string name, string filePath) : IComparable<IgnoreFilterFile>, IComparable, IEquatable<IgnoreFilterFile>
        {
            public static IgnoreFilterFile None => new(string.Empty, string.Empty);

            public string Name { get; private set; } = name;
            public string FilePath { get; private set; } = filePath;

            public override int GetHashCode()
            {
                return string.GetHashCode(Name, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as IgnoreFilterFile);

            }

            public bool Equals(IgnoreFilterFile? other)
            {
                if (other == null) return false;

                return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
            }

            public int CompareTo(object? obj)
            {
                return CompareTo(obj as IgnoreFilterFile);
            }

            public int CompareTo(IgnoreFilterFile? other)
            {
                if (other == null)
                    return 1;
                else
                    return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
        #endregion
    }

    public record TimeRangeMap(FileTimeRange Range, string Label)
    {
    }
}
