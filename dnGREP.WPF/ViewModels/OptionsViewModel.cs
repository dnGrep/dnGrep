using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Engines;
using dnGREP.Localization;
using dnGREP.WPF.MVHelpers;
using Microsoft.Win32;
using NLog;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public partial class OptionsViewModel : CultureAwareViewModel
    {
        public event EventHandler? RequestClose;

        public static readonly Messenger OptionsMessenger = new();

        public enum PanelSelection { MainPanel = 0, OptionsExpander }

        public enum ReplaceDialogConfiguration { FullDialog = 0, FilesOnly }

        public enum DeleteFilesDestination { Recycle, Permanent }

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly string ellipsis = char.ConvertFromUtf32(0x2026);

        public bool SearchListsCleared { get; private set; }

        public OptionsViewModel()
        {
            TaskLimit = Environment.ProcessorCount * 4;

            OptionsMessenger.Register("DeleteCustomEditor",
                (Action<CustomEditorViewModel>)(vm => DeleteCustomEditor(vm)));

            OptionsMessenger.Register("SetAsDefault",
                (Action<CustomEditorViewModel>)(vm => SetAsDefault(vm)));

            LoadSettings();

            // reuse the same settings as the main window
            foreach (KeyBindingInfo kbi in KeyBindingManager.GetCommandGestures(KeyCategory.Main))
            {
                PropertyInfo? pi = GetType().GetProperty(kbi.CommandName, BindingFlags.Instance | BindingFlags.Public);
                if (pi != null && pi.GetValue(this) is RelayCommand cmd)
                {
                    InputBindings.Add(KeyBindingManager.CreateKeyBinding(cmd, kbi.KeyGesture));
                }
            }

            foreach (string name in AppTheme.Instance.ThemeNames)
                ThemeNames.Add(name);

            CultureNames =
            [
                .. TranslationSource.AppCultures.OrderBy(kv => kv.Value, StringComparer.CurrentCulture),
            ];

            CompareApplicationTemplates = [.. ConfigurationTemplate.CompareConfigurationTemplates];

            HexLengthOptions = [8, 16, 32, 64, 128];

            hasWindowsThemes = AppTheme.HasWindowsThemes;
            AppTheme.Instance.CurrentThemeChanged += (s, e) =>
            {
                CurrentTheme = AppTheme.Instance.CurrentThemeName;
            };

            TranslationSource.Instance.CurrentCultureChanged += (s, e) =>
            {
                PanelTooltip = IsAdministrator ? null : Resources.Options_ToChangeThisSettingRunDnGREPAsAdministrator;
                WindowsIntegrationTooltip = IsAdministrator ? Resources.Options_EnablesStartingDnGrepFromTheWindowsExplorerRightClickContextMenu : string.Empty;

                // reload to reset the group and item text
                LoadVisibilityOptions();

                // call these to reformat decimal separators
                OnPropertyChanged(nameof(EditMainFormFontSize));
                OnPropertyChanged(nameof(EditReplaceFormFontSize));
                OnPropertyChanged(nameof(EditDialogFontSize));
                OnPropertyChanged(nameof(EditResultsFontSize));
                OnPropertyChanged(nameof(MatchTimeout));
                OnPropertyChanged(nameof(MatchThreshold));
            };

            LoadVisibilityOptions();
        }

        private void LoadVisibilityOptions()
        {
            VisibilityOptions.Clear();
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_Features), nameof(Resources.Main_Menu_Bookmarks), GrepSettings.Key.BookmarksVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_Features), nameof(Resources.Main_TestExpression), GrepSettings.Key.TestExpressionVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_Features), nameof(Resources.Main_ReplaceButton), GrepSettings.Key.ReplaceVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_Features), nameof(Resources.Main_SortButton), GrepSettings.Key.SortVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_Features), nameof(Resources.Main_MoreArrowButton), GrepSettings.Key.MoreVisible));

            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_FileFilter), nameof(Resources.Main_SearchInArchives), GrepSettings.Key.SearchInArchivesVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_FileFilter), nameof(Resources.Main_AllSizes), GrepSettings.Key.SizeFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_FileFilter), nameof(Resources.Main_IncludeSubfolders), GrepSettings.Key.SubfoldersFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_FileFilter), nameof(Resources.Main_IncludeHiddenFolders), GrepSettings.Key.HiddenFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_FileFilter), nameof(Resources.Main_IncludeBinaryFiles), GrepSettings.Key.BinaryFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_FileFilter), nameof(Resources.Main_FollowSymbolicLinks), GrepSettings.Key.SymbolicLinkFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_FileFilter), nameof(Resources.Main_AllDates), GrepSettings.Key.DateFilterVisible));

            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SpecialOptions), nameof(Resources.Main_SearchParallel), GrepSettings.Key.SearchParallelVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SpecialOptions), nameof(Resources.Main_UseGitignore), GrepSettings.Key.UseGitIgnoreVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SpecialOptions), nameof(Resources.Main_SkipRemoteCloudStorageFiles), GrepSettings.Key.SkipCloudStorageVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SpecialOptions), nameof(Resources.Main_Encoding), GrepSettings.Key.EncodingVisible));

            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SearchType), nameof(Resources.Main_SearchType_Regex), GrepSettings.Key.SearchTypeRegexVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SearchType), nameof(Resources.Main_SearchType_XPath), GrepSettings.Key.SearchTypeXPathVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SearchType), nameof(Resources.Main_SearchType_Text), GrepSettings.Key.SearchTypeTextVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SearchType), nameof(Resources.Main_SearchType_Phonetic), GrepSettings.Key.SearchTypePhoneticVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SearchType), nameof(Resources.Main_SearchType_Hex), GrepSettings.Key.SearchTypeByteVisible));

            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SearchOptions), nameof(Resources.Main_BooleanOperators), GrepSettings.Key.BooleanOperatorsVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_SearchOptions), nameof(Resources.Main_CaptureGroupSearch), GrepSettings.Key.CaptureGroupSearchVisible));

            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_ResultOptions), nameof(Resources.Main_SearchInResults), GrepSettings.Key.SearchInResultsVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_ResultOptions), nameof(Resources.Main_PreviewFile), GrepSettings.Key.PreviewFileVisible));

            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_ResultsTree), nameof(Resources.Main_HighlightMatches), GrepSettings.Key.HighlightMatchesVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_ResultsTree), nameof(Resources.Main_HighlightGroups), GrepSettings.Key.HighlightGroupsVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_ResultsTree), nameof(Resources.Main_ContextShowLines), GrepSettings.Key.ShowContextLinesVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_ResultsTree), nameof(Resources.Main_Zoom), GrepSettings.Key.ZoomResultsTreeVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_ResultsTree), nameof(Resources.Main_WrapText), GrepSettings.Key.WrapTextResultsTreeVisible));

            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_PreviewWindow), nameof(Resources.Preview_Zoom), GrepSettings.Key.PreviewZoomWndVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_PreviewWindow), nameof(Resources.Preview_WrapText), GrepSettings.Key.WrapTextPreviewWndVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_PreviewWindow), nameof(Resources.Preview_ViewWhitespace), GrepSettings.Key.ViewWhitespacePreviewWndVisible));
            VisibilityOptions.Add(new VisibilityOption(nameof(Resources.Options_Personalize_PreviewWindow), nameof(Resources.Preview_Syntax), GrepSettings.Key.SyntaxPreviewWndVisible));
        }

        #region Private Variables and Properties
        private static GrepSettings Settings => GrepSettings.Instance;
        #endregion

        #region Properties
        public KeyValuePair<string, string>[] CultureNames { get; }

        public KeyValuePair<string, ConfigurationTemplate?>[] CompareApplicationTemplates { get; }

        public List<int> HexLengthOptions { get; }

        public ObservableCollection<PluginViewModel> Plugins { get; } = [];

        public ObservableCollection<CustomEditorViewModel> CustomEditors { get; } = [];

        private List<CustomEditor> originalCustomEditors = [];

        private void AddCustomEditor()
        {
            CustomEditor ed = new(string.Empty, string.Empty, string.Empty, false, string.Empty);
            CustomEditorViewModel vm = new(ed, false);
            if (vm.EditCustomEditor())
            {
                CustomEditors.Add(vm);
            }
        }

        private void DeleteCustomEditor(CustomEditorViewModel vm)
        {
            CustomEditors.Remove(vm);
        }

        private void SetAsDefault(CustomEditorViewModel vm)
        {
            if (CustomEditors.Count > 1 && CustomEditors.First() != vm)
            {
                CustomEditors.Remove(vm);
                CustomEditors.Insert(0, vm);

                for (int idx = 0; idx < CustomEditors.Count; idx++)
                {
                    CustomEditors[idx].IsDefault = 0 == idx;
                }
            }
        }

        public static IList<FontInfo> FontFamilies
        {
            get
            {
                return Fonts.SystemFontFamilies.Select(r => new FontInfo(r.Source))
                    .OrderBy(r => r.FamilyName).ToList();
            }
        }

        public static IList<FontWeight> FontWeightList
        {
            get { return [FontWeights.Normal, FontWeights.SemiBold, FontWeights.Bold, FontWeights.Black]; }
        }

        private void ApplyCompareApplicationTemplate(ConfigurationTemplate? template)
        {
            if (template != null)
            {
                CompareApplicationPath = ellipsis + template.ExeFileName;
                CompareApplicationArgs = template.Arguments;

                Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    UIServices.SetBusyState();
                    string fullPath = ConfigurationTemplate.FindExePath(template);
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        CompareApplicationPath = fullPath;
                        CompareApplicationArgs = template.Arguments;
                    }
                }, DispatcherPriority.ApplicationIdle);
            }
        }

        public ObservableCollectionEx<InputBinding> InputBindings { get; } = [];

        public ObservableCollection<VisibilityOption> VisibilityOptions { get; } = [];

        public ObservableCollection<string> ThemeNames { get; } = [];

        [ObservableProperty]
        private bool pathsAreDefault = true;

        [ObservableProperty]
        private string dataDirectoryPath = string.Empty;

        [ObservableProperty]
        private string logDirectoryPath = string.Empty;

        [ObservableProperty]
        private bool enableWindowsIntegration;

        [ObservableProperty]
        private bool enableWindows11ShellMenu;
        private bool enableWindows11ShellMenuOriginalValue;

        [ObservableProperty]
        private bool canModifyWindows11ShellMenu;

        [ObservableProperty]
        private string? enableWindows11ShellMenuTooltip;

        [ObservableProperty]
        private bool isSingletonInstance;

        [ObservableProperty]
        private bool minimizeToNotificationArea;

        [ObservableProperty]
        private string restoreWindowKeyboardShortcut = string.Empty;

        partial void OnRestoreWindowKeyboardShortcutChanged(string value)
        {
            LocalizedRestoreWindowKeyboardShortcut = string.Empty;

            if (!string.IsNullOrEmpty(value) &&
                HotKey.TryParse(value, out var hotKey))
            {
                List<string> modifiers = [];
                if (hotKey.KeyModifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_WIN))
                {
                    modifiers.Add(Resources.Keyboard_WindowsLogoKey);
                }
                if (hotKey.KeyModifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_CONTROL))
                {
                    modifiers.Add(Resources.Keyboard_ControlKey);
                }
                if (hotKey.KeyModifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_SHIFT))
                {
                    modifiers.Add(Resources.Keyboard_ShiftKey);
                }
                if (hotKey.KeyModifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_ALT))
                {
                    modifiers.Add(Resources.Keyboard_AltKey);
                }

                LocalizedRestoreWindowKeyboardShortcut = string.Format("{0}+{1}", string.Join("+", modifiers), hotKey.Key);
            }
        }

        [ObservableProperty]
        private string localizedRestoreWindowKeyboardShortcut = string.Empty;

        [ObservableProperty]
        private bool confirmExitScript;

        [ObservableProperty]
        private bool confirmExitSearch;

        [ObservableProperty]
        private double confirmExitSearchDuration;

        [ObservableProperty]
        private bool passCommandLineToSingleton;

        [ObservableProperty]
        private string? windowsIntegrationTooltip;

        [ObservableProperty]
        private string? panelTooltip;

        [ObservableProperty]
        private bool isAdministrator;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanChangeRunAtStartup))]
        private bool enableCheckForUpdates;
        partial void OnEnableCheckForUpdatesChanged(bool value)
        {
            if (!value)
                EnableRunAtStartup = false;
        }

        [ObservableProperty]
        private int checkForUpdatesInterval;

        [ObservableProperty]
        private bool enableRunAtStartup;

        public bool CanChangeRunAtStartup => EnableCheckForUpdates && IsAdministrator;

        [ObservableProperty]
        private bool followWindowsTheme = true;
        partial void OnFollowWindowsThemeChanged(bool value)
        {
            AppTheme.Instance.FollowWindowsThemeChanged(value, CurrentTheme);
            CurrentTheme = AppTheme.Instance.CurrentThemeName;
        }

        [ObservableProperty]
        private bool hasWindowsThemes = true;

        [ObservableProperty]
        private string currentTheme = "Light";
        partial void OnCurrentThemeChanged(string value)
        {
            AppTheme.Instance.CurrentThemeName = value;
        }

        [ObservableProperty]
        private string? currentCulture;
        partial void OnCurrentCultureChanged(string? value)
        {
            if (value != null)
            {
                TranslationSource.Instance.SetCulture(value);
            }
        }

        [ObservableProperty]
        private ConfigurationTemplate? compareApplicationTemplate = null;
        partial void OnCompareApplicationTemplateChanged(ConfigurationTemplate? value)
        {
            ApplyCompareApplicationTemplate(value);
        }

        [ObservableProperty]
        private string compareApplicationPath = string.Empty;

        [ObservableProperty]
        private string compareApplicationArgs = string.Empty;

        [ObservableProperty]
        private bool showFilePathInResults;

        [ObservableProperty]
        private bool showRelativeFilePath;

        [ObservableProperty]
        private bool showFileErrorsInResults;

        [ObservableProperty]
        private bool navigationButtonsVisible;

        partial void OnNavigationButtonsVisibleChanged(bool value)
        {
            NavigationToolsViewModel.Instance.ChangeNavigationToolsPosition(value, NavigationToolsPosition);
        }

        [ObservableProperty]
        private NavigationToolsPosition navigationToolsPosition;

        partial void OnNavigationToolsPositionChanged(NavigationToolsPosition value)
        {
            NavigationToolsViewModel.Instance.ChangeNavigationToolsPosition(NavigationButtonsVisible, value);
        }

        [ObservableProperty]
        private ToolSize navigationToolsSize;

        partial void OnNavigationToolsSizeChanged(ToolSize value)
        {
            NavigationToolsViewModel.Instance.ChangeNavigationToolsSize(EditMainFormFontSize, value);
        }

        [ObservableProperty]
        private bool showLinesInContext;

        [ObservableProperty]
        private int contextLinesBefore;

        [ObservableProperty]
        private int contextLinesAfter;

        [ObservableProperty]
        private bool allowSearchWithEmptyPattern;

        [ObservableProperty]
        private bool detectEncodingForFileNamePattern;

        [ObservableProperty]
        private bool autoExpandSearchTree;

        [ObservableProperty]
        private bool showVerboseMatchCount;

        [ObservableProperty]
        private bool showFileInfoTooltips;

        [ObservableProperty]
        private double matchTimeout;

        [ObservableProperty]
        private double matchThreshold;

        [ObservableProperty]
        private int maxPathBookmarks;

        [ObservableProperty]
        private int maxSearchBookmarks;

        [ObservableProperty]
        private int maxExtensionBookmarks;

        [ObservableProperty]
        private PanelSelection optionsLocation;

        [ObservableProperty]
        private bool stickyScroll;

        [ObservableProperty]
        private bool sortAutomaticallyOnSearch;

        [ObservableProperty]
        private bool maximizeResultsTreeOnSearch;

        [ObservableProperty]
        private bool autoCompleteEnabled;

        public int MaxDegreeOfParallelism
        {
            get
            {
                return ParallelismUnlimited ? -1 : ParallelismCount;
            }
            set
            {
                if (value == -1)
                {
                    ParallelismUnlimited = true;
                    ParallelismCount = TaskLimit;
                }
                else
                {
                    ParallelismUnlimited = false;
                    ParallelismCount = value;
                }
            }
        }

        [ObservableProperty]
        private int taskLimit = 1;

        [ObservableProperty]
        private int parallelismCount = 1;

        [ObservableProperty]
        private bool parallelismUnlimited = true;

        [ObservableProperty]
        private int searchAutoStopCount = 1;

        [ObservableProperty]
        private int searchAutoPauseCount = 5;

        [ObservableProperty]
        private FocusElement focusElement;

        [ObservableProperty]
        private ReplaceDialogConfiguration replaceDialogLayout;

        [ObservableProperty]
        private DeleteFilesDestination deleteOption = DeleteFilesDestination.Recycle;

        [ObservableProperty]
        private OverwriteFile copyOverwriteFileOption = OverwriteFile.Prompt;

        [ObservableProperty]
        private OverwriteFile moveOverwriteFileOption = OverwriteFile.Prompt;

        [ObservableProperty]
        private bool preserveFolderLayoutOnCopy = true;

        [ObservableProperty]
        private bool preserveFolderLayoutOnMove = true;

        [ObservableProperty]
        private ArchiveCopyMoveDelete archiveCopyOption = ArchiveCopyMoveDelete.CopyFile;

        [ObservableProperty]
        private ArchiveCopyMoveDelete archiveMoveOption = ArchiveCopyMoveDelete.CopyFile;

        [ObservableProperty]
        private ArchiveCopyMoveDelete archiveDeleteOption = ArchiveCopyMoveDelete.DoNothing;

        [ObservableProperty]
        private int hexResultByteLength = 16;

        [ObservableProperty]
        private long previewLargeFileLimit = 4000L;

        [ObservableProperty]
        private bool cacheExtractedFiles;

        [ObservableProperty]
        private HashOption cacheFileHashType = HashOption.FullFile;

        [ObservableProperty]
        private bool cacheFilesInTempFolder;

        partial void OnCacheFilesInTempFolderChanged(bool value)
        {
            if (value)
                CacheFilePath = string.Empty;
        }

        [ObservableProperty]
        private string cacheFilePath = string.Empty;

        [ObservableProperty]
        private bool cacheFilesClean;

        partial void OnCacheFilesCleanChanged(bool value)
        {
            if (!value)
                CacheFilesCleanDays = 0;
        }

        [ObservableProperty]
        private int cacheFilesCleanDays;

        [ObservableProperty]
        private string pdfToTextOptions = string.Empty;

        [ObservableProperty]
        private bool pdfJoinLines = true;

        [ObservableProperty]
        private PdfNumberType pdfNumberStyle = PdfNumberType.PageNumber;

        [ObservableProperty]
        private bool wordExtractComments = false;

        [ObservableProperty]
        private FootnoteRefType wordFootnoteReference = FootnoteRefType.None;

        [ObservableProperty]
        private bool wordExtractFootnotes = false;

        [ObservableProperty]
        private CommentRefType wordCommentReference = CommentRefType.None;

        [ObservableProperty]
        private bool wordExtractHeaders = false;

        [ObservableProperty]
        private bool wordExtractFooters = false;

        [ObservableProperty]
        private HeaderFooterPosition wordHeaderFooterPosition = HeaderFooterPosition.SectionStart;

        [ObservableProperty]
        private string archiveExtensions = string.Empty;
        private string defaultArchiveExtensions = string.Empty;

        [ObservableProperty]
        private string archiveCustomExtensions = string.Empty;

        [ObservableProperty]
        private bool useDefaultFont = true;
        partial void OnUseDefaultFontChanged(bool value)
        {
            if (value)
            {
                EditApplicationFontFamily = SystemFonts.MessageFontFamily.Source;
                EditMainFormFontSize = SystemFonts.MessageFontSize;
                EditReplaceFormFontSize = SystemFonts.MessageFontSize;
                EditDialogFontSize = SystemFonts.MessageFontSize;
                EditResultsFontFamily = GrepSettings.DefaultMonospaceFontFamily;
                EditResultsFontSize = SystemFonts.MessageFontSize;
            }
        }

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private string editApplicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double mainFormFontSize;

        [ObservableProperty]
        private double editMainFormFontSize;

        partial void OnEditMainFormFontSizeChanged(double value)
        {
            NavigationToolsViewModel.Instance.ChangeNavigationToolsSize(value, NavigationToolsSize);
        }

        [ObservableProperty]
        private double replaceFormFontSize;

        [ObservableProperty]
        private double editReplaceFormFontSize;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private double editDialogFontSize;

        [ObservableProperty]
        private string resultsFontFamily = GrepSettings.DefaultMonospaceFontFamily;

        [ObservableProperty]
        private string editResultsFontFamily = GrepSettings.DefaultMonospaceFontFamily;

        [ObservableProperty]
        private double resultsFontSize;

        [ObservableProperty]
        private double editResultsFontSize;

        [ObservableProperty]
        private FontWeight resultsFileNameWeight;

        public bool CanSave
        {
            get
            {
                if (AreDirectoriesChanged || AreSettingsChanged)
                {
                    return CurrentCulture != null;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool AreDirectoriesChanged
        {
            get
            {
                return PathsAreDefault != DirectoryConfiguration.Instance.PathsAreDefault ||
                    DataDirectoryPath != DirectoryConfiguration.Instance.DataDirectory ||
                    LogDirectoryPath != DirectoryConfiguration.Instance.LogDirectory;
            }
        }

        private bool AreSettingsChanged
        {
            get
            {
                return 
                EnableWindowsIntegration != RegistryOperations.IsShellRegistered("Directory") ||
                EnableWindows11ShellMenu != enableWindows11ShellMenuOriginalValue ||
                EnableRunAtStartup != RegistryOperations.IsStartupRegistered() ||
                IsSingletonInstance != Settings.Get<bool>(GrepSettings.Key.IsSingletonInstance) ||
                MinimizeToNotificationArea != Settings.Get<bool>(GrepSettings.Key.MinimizeToNotificationArea) ||
                RestoreWindowKeyboardShortcut != Settings.Get<string>(GrepSettings.Key.RestoreWindowKeyboardShortcut) ||
                ConfirmExitScript != Settings.Get<bool>(GrepSettings.Key.ConfirmExitScript) ||
                ConfirmExitSearch != Settings.Get<bool>(GrepSettings.Key.ConfirmExitSearch) ||
                ConfirmExitSearchDuration != Settings.Get<double>(GrepSettings.Key.ConfirmExitSearchDuration) ||
                PassCommandLineToSingleton != Settings.Get<bool>(GrepSettings.Key.PassCommandLineToSingleton) ||
                EnableCheckForUpdates != Settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking) ||
                CheckForUpdatesInterval != Settings.Get<int>(GrepSettings.Key.UpdateCheckInterval) ||
                ShowLinesInContext != Settings.Get<bool>(GrepSettings.Key.ShowLinesInContext) ||
                ContextLinesBefore != Settings.Get<int>(GrepSettings.Key.ContextLinesBefore) ||
                ContextLinesAfter != Settings.Get<int>(GrepSettings.Key.ContextLinesAfter) ||
                CompareApplicationPath != Settings.Get<string>(GrepSettings.Key.CompareApplication) ||
                CompareApplicationArgs != Settings.Get<string>(GrepSettings.Key.CompareApplicationArgs) ||
                ShowFilePathInResults != Settings.Get<bool>(GrepSettings.Key.ShowFilePathInResults) ||
                ShowRelativeFilePath != Settings.Get<bool>(GrepSettings.Key.ShowRelativeFilePath) ||
                ShowFileErrorsInResults != Settings.Get<bool>(GrepSettings.Key.ShowFileErrorsInResults) ||
                NavigationButtonsVisible != Settings.Get<bool>(GrepSettings.Key.NavigationButtonsVisible) ||
                NavigationToolsPosition != Settings.Get<NavigationToolsPosition>(GrepSettings.Key.NavToolsPosition) ||
                NavigationToolsSize != Settings.Get<ToolSize>(GrepSettings.Key.NavToolsSize) ||
                AllowSearchWithEmptyPattern != Settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern) ||
                DetectEncodingForFileNamePattern != Settings.Get<bool>(GrepSettings.Key.DetectEncodingForFileNamePattern) ||
                AutoExpandSearchTree != Settings.Get<bool>(GrepSettings.Key.ExpandResults) ||
                ShowVerboseMatchCount != Settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount) ||
                ShowFileInfoTooltips != Settings.Get<bool>(GrepSettings.Key.ShowFileInfoTooltips) ||
                MatchTimeout != Settings.Get<double>(GrepSettings.Key.MatchTimeout) ||
                MatchThreshold != Settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold) ||
                MaxSearchBookmarks != Settings.Get<int>(GrepSettings.Key.MaxSearchBookmarks) ||
                MaxPathBookmarks != Settings.Get<int>(GrepSettings.Key.MaxPathBookmarks) ||
                MaxExtensionBookmarks != Settings.Get<int>(GrepSettings.Key.MaxExtensionBookmarks) ||
                OptionsLocation != (Settings.Get<bool>(GrepSettings.Key.OptionsOnMainPanel) ?
                    PanelSelection.MainPanel : PanelSelection.OptionsExpander) ||
                ReplaceDialogLayout != (Settings.Get<bool>(GrepSettings.Key.ShowFullReplaceDialog) ?
                    ReplaceDialogConfiguration.FullDialog : ReplaceDialogConfiguration.FilesOnly) ||
                DeleteOption != (Settings.Get<bool>(GrepSettings.Key.DeleteToRecycleBin) ?
                    DeleteFilesDestination.Recycle : DeleteFilesDestination.Permanent) ||
                CopyOverwriteFileOption != Settings.Get<OverwriteFile>(GrepSettings.Key.OverwriteFilesOnCopy) ||
                MoveOverwriteFileOption != Settings.Get<OverwriteFile>(GrepSettings.Key.OverwriteFilesOnMove) ||
                PreserveFolderLayoutOnCopy != Settings.Get<bool>(GrepSettings.Key.PreserveFolderLayoutOnCopy) ||
                PreserveFolderLayoutOnMove != Settings.Get<bool>(GrepSettings.Key.PreserveFolderLayoutOnMove) ||
                ArchiveCopyOption != Settings.Get<ArchiveCopyMoveDelete>(GrepSettings.Key.ArchiveCopy) ||
                ArchiveMoveOption != Settings.Get<ArchiveCopyMoveDelete>(GrepSettings.Key.ArchiveMove) ||
                ArchiveDeleteOption != Settings.Get<ArchiveCopyMoveDelete>(GrepSettings.Key.ArchiveDelete) ||
                StickyScroll != Settings.Get<bool>(GrepSettings.Key.StickyScroll) ||
                SortAutomaticallyOnSearch != Settings.Get<bool>(GrepSettings.Key.SortAutomaticallyOnSearch) ||
                MaximizeResultsTreeOnSearch != Settings.Get<bool>(GrepSettings.Key.MaximizeResultsTreeOnSearch) ||
                AutoCompleteEnabled != Settings.Get<bool>(GrepSettings.Key.AutoCompleteEnabled) ||
                MaxDegreeOfParallelism != Settings.Get<int>(GrepSettings.Key.MaxDegreeOfParallelism) ||
                SearchAutoStopCount != Settings.Get<int>(GrepSettings.Key.SearchAutoStopCount) ||
                SearchAutoPauseCount != Settings.Get<int>(GrepSettings.Key.SearchAutoPauseCount) ||
                FocusElement != Settings.Get<FocusElement>(GrepSettings.Key.SetFocusElement) ||
                FollowWindowsTheme != Settings.Get<bool>(GrepSettings.Key.FollowWindowsTheme) ||
                CurrentTheme != Settings.Get<string>(GrepSettings.Key.CurrentTheme) ||
                CurrentCulture != Settings.Get<string>(GrepSettings.Key.CurrentCulture) ||
                UseDefaultFont != Settings.Get<bool>(GrepSettings.Key.UseDefaultFont) ||
                EditApplicationFontFamily != Settings.Get<string>(GrepSettings.Key.ApplicationFontFamily) ||
                EditMainFormFontSize != Settings.Get<double>(GrepSettings.Key.MainFormFontSize) ||
                EditReplaceFormFontSize != Settings.Get<double>(GrepSettings.Key.ReplaceFormFontSize) ||
                EditDialogFontSize != Settings.Get<double>(GrepSettings.Key.DialogFontSize) ||
                EditResultsFontFamily != Settings.Get<string>(GrepSettings.Key.ResultsFontFamily) ||
                EditResultsFontSize != Settings.Get<double>(GrepSettings.Key.ResultsFontSize) ||
                ResultsFileNameWeight != Settings.Get<FontWeight>(GrepSettings.Key.ResultsFileNameWeight) ||
                HexResultByteLength != Settings.Get<int>(GrepSettings.Key.HexResultByteLength) ||
                PreviewLargeFileLimit != Settings.Get<long>(GrepSettings.Key.PreviewLargeFileLimit) ||
                CacheExtractedFiles != Settings.Get<bool>(GrepSettings.Key.CacheExtractedFiles) ||
                CacheFileHashType != Settings.Get<HashOption>(GrepSettings.Key.CacheFileHashType) ||
                CacheFilesInTempFolder != Settings.Get<bool>(GrepSettings.Key.CacheFilesInTempFolder) ||
                CacheFilePath != Settings.Get<string>(GrepSettings.Key.CacheFilePath) ||
                CacheFilesCleanDays != Settings.Get<int>(GrepSettings.Key.CacheFilesCleanDays) ||
                PdfToTextOptions != Settings.Get<string>(GrepSettings.Key.PdfToTextOptions) ||
                PdfJoinLines != Settings.Get<bool>(GrepSettings.Key.PdfJoinLines) ||
                PdfNumberStyle != Settings.Get<PdfNumberType>(GrepSettings.Key.PdfNumberStyle) ||
                WordExtractFootnotes != Settings.Get<bool>(GrepSettings.Key.WordExtractFootnotes) ||
                WordFootnoteReference != Settings.Get<FootnoteRefType>(GrepSettings.Key.WordFootnoteReference) ||
                WordExtractComments != Settings.Get<bool>(GrepSettings.Key.WordExtractComments) ||
                WordCommentReference != Settings.Get<CommentRefType>(GrepSettings.Key.WordCommentReference) ||
                WordExtractHeaders != Settings.Get<bool>(GrepSettings.Key.WordExtractHeaders) ||
                WordExtractFooters != Settings.Get<bool>(GrepSettings.Key.WordExtractFooters) ||
                WordHeaderFooterPosition != Settings.Get<HeaderFooterPosition>(GrepSettings.Key.WordHeaderFooterPosition) ||
                GrepSettings.CleanExtensions(ArchiveExtensions) != Settings.Get<string>(GrepSettings.Key.ArchiveExtensions) ||
                GrepSettings.CleanExtensions(ArchiveCustomExtensions) != Settings.Get<string>(GrepSettings.Key.ArchiveCustomExtensions) ||
                IsChanged(Plugins) ||
                IsChanged(CustomEditors) ||
                IsChanged(VisibilityOptions);
            }
        }

        private static bool IsChanged(IList<PluginViewModel> plugins)
        {
            return plugins.Any(p => p.IsChanged);
        }

        private bool IsChanged(IList<CustomEditorViewModel> customEditors)
        {
            return originalCustomEditors.Count != customEditors.Count ||
                customEditors.Any(v => v.IsChanged);
        }

        private static bool IsChanged(IList<VisibilityOption> visibilityOptions)
        {
            return visibilityOptions.Any(p => p.IsChanged);
        }

        #endregion

        #region Commands

        private RelayCommand? saveCommand;
        public RelayCommand SaveCommand => saveCommand ??= new RelayCommand(
            p => Save(),
            p => CanSave);

        private RelayCommand? addCustomEditorCommand;
        public RelayCommand AddCustomEditorCommand => addCustomEditorCommand ??= new RelayCommand(
            p => AddCustomEditor());

        private RelayCommand? browseCompareCommand;
        public RelayCommand BrowseCompareCommand => browseCompareCommand ??= new RelayCommand(
            p => BrowseToCompareApp());

        private RelayCommand? browseCacheCommand;
        public RelayCommand BrowseCacheCommand => browseCacheCommand ??= new RelayCommand(
            p => BrowseToCacheFolder(),
            p => !CacheFilesInTempFolder);

        private RelayCommand? clearSearchesCommand;
        public RelayCommand ClearSearchesCommand => clearSearchesCommand ??= new RelayCommand(
            p => ClearSearches());

        private RelayCommand? reloadThemeCommand;
        public RelayCommand ReloadThemeCommand => reloadThemeCommand ??= new RelayCommand(
            p => AppTheme.Instance.ReloadCurrentTheme());

        private RelayCommand? showStringMapCommand;
        public RelayCommand ShowStringMapCommand => showStringMapCommand ??= new RelayCommand(
            p => ShowStringMap());

        public string ReloadThemeCommandTooltip
        {
            get
            {
                if (!string.IsNullOrEmpty(ReloadThemeCommand.KeyGestureText))
                {
                    return $"{Resources.Options_ReloadTheCurrentThemeFile} ({ReloadThemeCommand.KeyGestureText})";
                }
                return Resources.Options_ReloadTheCurrentThemeFile;
            }
        }

        private RelayCommand? loadResxCommand;
        public RelayCommand LoadResxCommand => loadResxCommand ??= new RelayCommand(
            p => LoadResxFile());

        private RelayCommand? resetArchiveExtensionsCommand;
        public RelayCommand ResetArchiveExtensionsCommand => resetArchiveExtensionsCommand ??= new RelayCommand(
            p => ArchiveExtensions = defaultArchiveExtensions,
            q => !ArchiveExtensions.Equals(defaultArchiveExtensions, StringComparison.Ordinal));

        private RelayCommand? resetPdfToTextOptionCommand;
        public RelayCommand ResetPdfToTextOptionCommand => resetPdfToTextOptionCommand ??= new RelayCommand(
            p => PdfToTextOptions = defaultPdfToText,
            q => !PdfToTextOptions.Equals(defaultPdfToText, StringComparison.Ordinal));

        private const string defaultPdfToText = "-layout -enc UTF-8 -bom";

        private RelayCommand? browseDataDirectory;
        public RelayCommand BrowseDataDirectory => browseDataDirectory ??= new RelayCommand(
            p => BrowseToDataDirectory());

        private RelayCommand? browseLogDirectory;
        public RelayCommand BrowseLogDirectory => browseLogDirectory ??= new RelayCommand(
            p => BrowseToLogDirectory());

        #endregion

        #region Public Methods

        /// <summary>
        /// Saves the settings to file.  This method is invoked by the SaveCommand.
        /// </summary>
        public void Save()
        {
            if (AreSettingsChanged)
            {
                SaveSettings();
            }

            if (AreDirectoriesChanged)
            {
                if (!SaveDirectories())
                    return;
            }

            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Private Methods

        private void LoadResxFile()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "resx files|*.resx",
                CheckFileExists = true,
                DefaultExt = "resx"
            };
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                if (TranslationSource.Instance.LoadResxFile(dlg.FileName))
                {
                    CurrentCulture = null;
                }
            }
        }

        public void BrowseToCompareApp()
        {
            var dlg = new OpenFileDialog();
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                CompareApplicationPath = dlg.FileName;
            }
        }

        public void BrowseToCacheFolder()
        {
            FileFolderDialogWin32 dlg = new();
            if (dlg.ShowDialog() == true)
            {
                CacheFilePath = dlg.SelectedPath;
            }
        }

        public void BrowseToDataDirectory()
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = Resources.MessageBox_DnGrep,
            };

            if (folderDialog.ShowDialog() == true)
            {
                DataDirectoryPath = folderDialog.FolderName;
            }
        }

        public void BrowseToLogDirectory()
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = Resources.MessageBox_DnGrep,
            };

            if (folderDialog.ShowDialog() == true)
            {
                LogDirectoryPath = folderDialog.FolderName;
            }
        }

        private static void ShowStringMap()
        {
            StringMapWindow form = new()
            {
                Owner = Application.Current.MainWindow
            };
            form.ShowDialog();
        }

        private void ClearSearches()
        {
            // keep the pinned bookmarks
            var list = Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastPathBookmarks).Where(r => r.IsPinned).ToList();
            Settings.Set(GrepSettings.Key.FastPathBookmarks, list);

            list = Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastFileMatchBookmarks).Where(r => r.IsPinned).ToList();
            Settings.Set(GrepSettings.Key.FastFileMatchBookmarks, list);

            list = Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastFileNotMatchBookmarks).Where(r => r.IsPinned).ToList();
            Settings.Set(GrepSettings.Key.FastFileNotMatchBookmarks, list);

            list = Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastSearchBookmarks).Where(r => r.IsPinned).ToList();
            Settings.Set(GrepSettings.Key.FastSearchBookmarks, list);

            list = Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastReplaceBookmarks).Where(r => r.IsPinned).ToList();
            Settings.Set(GrepSettings.Key.FastReplaceBookmarks, list);

            SearchListsCleared = true;
        }

        private void LoadSettings()
        {
            IsAdministrator = RegistryOperations.IsAdministrator;

            if (!IsAdministrator)
            {
                PanelTooltip = Resources.Options_ToChangeThisSettingRunDnGREPAsAdministrator;
            }
            else
            {
                WindowsIntegrationTooltip = Resources.Options_EnablesStartingDnGrepFromTheWindowsExplorerRightClickContextMenu;
            }

            DataDirectoryPath = DirectoryConfiguration.Instance.DataDirectory;
            LogDirectoryPath = DirectoryConfiguration.Instance.LogDirectory;
            PathsAreDefault = DirectoryConfiguration.Instance.PathsAreDefault;

            EnableWindowsIntegration = RegistryOperations.IsShellRegistered("Directory");
            EnableWindows11ShellMenu = enableWindows11ShellMenuOriginalValue = SparsePackage.IsRegistered;
            CanModifyWindows11ShellMenu = SparsePackage.CanRegisterPackage && !IsAdministrator;
            EnableWindows11ShellMenuTooltip = SparsePackage.CanRegisterPackage ? null : Resources.Options_RequiresWindows11;
            EnableRunAtStartup = RegistryOperations.IsStartupRegistered();
            IsSingletonInstance = Settings.Get<bool>(GrepSettings.Key.IsSingletonInstance);
            MinimizeToNotificationArea = Settings.Get<bool>(GrepSettings.Key.MinimizeToNotificationArea);
            RestoreWindowKeyboardShortcut = Settings.Get<string>(GrepSettings.Key.RestoreWindowKeyboardShortcut);
            ConfirmExitScript = Settings.Get<bool>(GrepSettings.Key.ConfirmExitScript);
            ConfirmExitSearch = Settings.Get<bool>(GrepSettings.Key.ConfirmExitSearch);
            ConfirmExitSearchDuration = Settings.Get<double>(GrepSettings.Key.ConfirmExitSearchDuration);
            PassCommandLineToSingleton = Settings.Get<bool>(GrepSettings.Key.PassCommandLineToSingleton);
            EnableCheckForUpdates = Settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking);
            CheckForUpdatesInterval = Settings.Get<int>(GrepSettings.Key.UpdateCheckInterval);
            CompareApplicationPath = Settings.Get<string>(GrepSettings.Key.CompareApplication);
            CompareApplicationArgs = Settings.Get<string>(GrepSettings.Key.CompareApplicationArgs);
            ShowFilePathInResults = Settings.Get<bool>(GrepSettings.Key.ShowFilePathInResults);
            ShowRelativeFilePath = Settings.Get<bool>(GrepSettings.Key.ShowRelativeFilePath);
            ShowFileErrorsInResults = Settings.Get<bool>(GrepSettings.Key.ShowFileErrorsInResults);
            NavigationButtonsVisible = Settings.Get<bool>(GrepSettings.Key.NavigationButtonsVisible);
            NavigationToolsPosition = Settings.Get<NavigationToolsPosition>(GrepSettings.Key.NavToolsPosition);
            NavigationToolsSize = Settings.Get<ToolSize>(GrepSettings.Key.NavToolsSize);
            AllowSearchWithEmptyPattern = Settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern);
            DetectEncodingForFileNamePattern = Settings.Get<bool>(GrepSettings.Key.DetectEncodingForFileNamePattern);
            AutoExpandSearchTree = Settings.Get<bool>(GrepSettings.Key.ExpandResults);
            ShowVerboseMatchCount = Settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount);
            ShowFileInfoTooltips = Settings.Get<bool>(GrepSettings.Key.ShowFileInfoTooltips);
            MatchTimeout = Settings.Get<double>(GrepSettings.Key.MatchTimeout);
            MatchThreshold = Settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold);
            ShowLinesInContext = Settings.Get<bool>(GrepSettings.Key.ShowLinesInContext);
            ContextLinesBefore = Settings.Get<int>(GrepSettings.Key.ContextLinesBefore);
            ContextLinesAfter = Settings.Get<int>(GrepSettings.Key.ContextLinesAfter);
            MaxSearchBookmarks = Settings.Get<int>(GrepSettings.Key.MaxSearchBookmarks);
            MaxPathBookmarks = Settings.Get<int>(GrepSettings.Key.MaxPathBookmarks);
            MaxExtensionBookmarks = Settings.Get<int>(GrepSettings.Key.MaxExtensionBookmarks);
            OptionsLocation = Settings.Get<bool>(GrepSettings.Key.OptionsOnMainPanel) ?
                PanelSelection.MainPanel : PanelSelection.OptionsExpander;
            ReplaceDialogLayout = Settings.Get<bool>(GrepSettings.Key.ShowFullReplaceDialog) ?
                ReplaceDialogConfiguration.FullDialog : ReplaceDialogConfiguration.FilesOnly;
            DeleteOption = Settings.Get<bool>(GrepSettings.Key.DeleteToRecycleBin) ?
                DeleteFilesDestination.Recycle : DeleteFilesDestination.Permanent;
            CopyOverwriteFileOption = Settings.Get<OverwriteFile>(GrepSettings.Key.OverwriteFilesOnCopy);
            MoveOverwriteFileOption = Settings.Get<OverwriteFile>(GrepSettings.Key.OverwriteFilesOnMove);
            PreserveFolderLayoutOnCopy = Settings.Get<bool>(GrepSettings.Key.PreserveFolderLayoutOnCopy);
            PreserveFolderLayoutOnMove = Settings.Get<bool>(GrepSettings.Key.PreserveFolderLayoutOnMove);
            ArchiveCopyOption = Settings.Get<ArchiveCopyMoveDelete>(GrepSettings.Key.ArchiveCopy);
            ArchiveMoveOption = Settings.Get<ArchiveCopyMoveDelete>(GrepSettings.Key.ArchiveMove);
            ArchiveDeleteOption = Settings.Get<ArchiveCopyMoveDelete>(GrepSettings.Key.ArchiveDelete);
            StickyScroll = Settings.Get<bool>(GrepSettings.Key.StickyScroll);
            SortAutomaticallyOnSearch = Settings.Get<bool>(GrepSettings.Key.SortAutomaticallyOnSearch);
            MaximizeResultsTreeOnSearch = Settings.Get<bool>(GrepSettings.Key.MaximizeResultsTreeOnSearch);
            AutoCompleteEnabled = Settings.Get<bool>(GrepSettings.Key.AutoCompleteEnabled);
            MaxDegreeOfParallelism = Settings.Get<int>(GrepSettings.Key.MaxDegreeOfParallelism);
            SearchAutoStopCount = Settings.Get<int>(GrepSettings.Key.SearchAutoStopCount);
            SearchAutoPauseCount = Settings.Get<int>(GrepSettings.Key.SearchAutoPauseCount);
            FocusElement = Settings.Get<FocusElement>(GrepSettings.Key.SetFocusElement);

            UseDefaultFont = Settings.Get<bool>(GrepSettings.Key.UseDefaultFont);
            ApplicationFontFamily = EditApplicationFontFamily =
                ValueOrDefault(GrepSettings.Key.ApplicationFontFamily, SystemFonts.MessageFontFamily.Source);
            MainFormFontSize = EditMainFormFontSize =
                ValueOrDefault(GrepSettings.Key.MainFormFontSize, SystemFonts.MessageFontSize);
            ReplaceFormFontSize = EditReplaceFormFontSize =
                ValueOrDefault(GrepSettings.Key.ReplaceFormFontSize, SystemFonts.MessageFontSize);
            DialogFontSize = EditDialogFontSize =
                ValueOrDefault(GrepSettings.Key.DialogFontSize, SystemFonts.MessageFontSize);
            ResultsFontFamily = EditResultsFontFamily =
                ValueOrDefault(GrepSettings.Key.ResultsFontFamily, GrepSettings.DefaultMonospaceFontFamily);
            ResultsFontSize = EditResultsFontSize =
                ValueOrDefault(GrepSettings.Key.ResultsFontSize, SystemFonts.MessageFontSize);
            ResultsFileNameWeight = Settings.Get<FontWeight>(GrepSettings.Key.ResultsFileNameWeight);

            // current values may not equal the saved settings value
            CurrentTheme = AppTheme.Instance.CurrentThemeName;
            FollowWindowsTheme = AppTheme.Instance.FollowWindowsTheme;
            CurrentCulture = TranslationSource.Instance.CurrentCulture.Name;

            HexResultByteLength = Settings.Get<int>(GrepSettings.Key.HexResultByteLength);
            PreviewLargeFileLimit = Settings.Get<long>(GrepSettings.Key.PreviewLargeFileLimit);
            CacheExtractedFiles = Settings.Get<bool>(GrepSettings.Key.CacheExtractedFiles);
            CacheFileHashType = Settings.Get<HashOption>(GrepSettings.Key.CacheFileHashType);
            CacheFilesInTempFolder = Settings.Get<bool>(GrepSettings.Key.CacheFilesInTempFolder);
            CacheFilePath = Settings.Get<string>(GrepSettings.Key.CacheFilePath);
            CacheFilesCleanDays = Settings.Get<int>(GrepSettings.Key.CacheFilesCleanDays);
            CacheFilesClean = CacheFilesCleanDays > 0;
            PdfToTextOptions = Settings.Get<string>(GrepSettings.Key.PdfToTextOptions);
            PdfJoinLines = Settings.Get<bool>(GrepSettings.Key.PdfJoinLines);
            PdfNumberStyle = Settings.Get<PdfNumberType>(GrepSettings.Key.PdfNumberStyle);

            WordExtractFootnotes = Settings.Get<bool>(GrepSettings.Key.WordExtractFootnotes);
            WordFootnoteReference = Settings.Get<FootnoteRefType>(GrepSettings.Key.WordFootnoteReference);
            WordExtractComments = Settings.Get<bool>(GrepSettings.Key.WordExtractComments);
            WordCommentReference = Settings.Get<CommentRefType>(GrepSettings.Key.WordCommentReference);
            WordExtractHeaders = Settings.Get<bool>(GrepSettings.Key.WordExtractHeaders);
            WordExtractFooters = Settings.Get<bool>(GrepSettings.Key.WordExtractFooters);
            WordHeaderFooterPosition = Settings.Get<HeaderFooterPosition>(GrepSettings.Key.WordHeaderFooterPosition);
            ArchiveExtensions = Settings.Get<string>(GrepSettings.Key.ArchiveExtensions);
            ArchiveCustomExtensions = Settings.Get<string>(GrepSettings.Key.ArchiveCustomExtensions);
            defaultArchiveExtensions = GrepSettings.CleanExtensions(ArchiveDirectory.DefaultExtensions);

            Plugins.Clear();
            List<PluginConfiguration> pluginConfigList = [];
            if (GrepSettings.Instance.ContainsKey(GrepSettings.Key.Plugins))
            {
                pluginConfigList = GrepSettings.Instance.Get<List<PluginConfiguration>>(GrepSettings.Key.Plugins);
            }

            foreach (var plugin in GrepEngineFactory.AllPlugins.OrderBy(p => p.Name))
            {
                string name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(plugin.Name)
                    .Replace(" ", "", StringComparison.Ordinal);

                PluginConfiguration cfg = pluginConfigList
                    .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
                    GrepSettings.Instance.AddNewPluginConfig(name, true, true, extensions: GrepSettings.CleanExtensions(plugin.DefaultExtensions));

                Plugins.Add(new(cfg, plugin.DefaultExtensions));
            }

            CustomEditors.Clear();
            if (Settings.ContainsKey(GrepSettings.Key.CustomEditors))
            {
                originalCustomEditors = GrepSettings.Instance.Get<List<CustomEditor>>(GrepSettings.Key.CustomEditors);

                for (int idx = 0; idx < originalCustomEditors.Count; idx++)
                {
                    var editor = originalCustomEditors[idx];
                    CustomEditors.Add(new CustomEditorViewModel(editor, idx == 0));
                }
            }
        }

        private string ValueOrDefault(string settingsKey, string defaultValue)
        {
            string value = Settings.Get<string>(settingsKey);
            return UseDefaultFont || string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private double ValueOrDefault(string settingsKey, double defaultValue)
        {
            double value = Settings.Get<double>(settingsKey);
            return UseDefaultFont || value == 0 ? defaultValue : value;
        }

        private bool SaveDirectories()
        {
            if (PathsAreDefault && PathsAreDefault != DirectoryConfiguration.Instance.PathsAreDefault)
            {
                DataDirectoryPath = DirectoryConfiguration.Instance.DefaultDataDirectory;
                LogDirectoryPath = DirectoryConfiguration.Instance.DefaultLogDirectory;
            }

            return ConfigurationManager.Instance.SaveDirectoryChanges(
                DirectoryConfiguration.Instance.DataDirectory,
                DataDirectoryPath,
                DirectoryConfiguration.Instance.LogDirectory,
                LogDirectoryPath);

        }

        private void SaveSettings()
        {
            if (IsAdministrator)
            {
                if (EnableWindowsIntegration)
                {
                    RegistryOperations.ShellRegisterContextMenu();
                }
                else
                {
                    RegistryOperations.ShellUnregisterContextMenu();
                }

                if (EnableRunAtStartup)
                {
                    RegistryOperations.StartupRegister();
                }
                else
                {
                    RegistryOperations.StartupUnregister();
                }
            }
            else
            {
                if (EnableWindows11ShellMenu)
                {
                    SparsePackage.RegisterSparsePackage(false);
                }
                else
                {
                    SparsePackage.RemoveSparsePackage();
                }
            }

            bool isCacheRemoved = !CacheExtractedFiles && CacheExtractedFiles != Settings.Get<bool>(GrepSettings.Key.CacheExtractedFiles);
            string oldUserCachePath = GrepSettings.Instance.Get<string>(GrepSettings.Key.CacheFilePath);
            string oldCachePath = !Utils.IsValidPath(oldUserCachePath) ||
                GrepSettings.Instance.Get<bool>(GrepSettings.Key.CacheFilesInTempFolder) ?
                Path.Combine(Path.GetTempPath(), Utils.defaultCacheFolderName) :
                oldUserCachePath;
            string newCachePath = !Utils.IsValidPath(CacheFilePath) ||
                CacheFilesInTempFolder ?
                Path.Combine(Path.GetTempPath(), Utils.defaultCacheFolderName) :
                CacheFilePath;
            bool isCachePathChanged = oldCachePath != newCachePath;
            bool isCacheHashTypeChanged = CacheFileHashType !=
                GrepSettings.Instance.Get<HashOption>(GrepSettings.Key.CacheFileHashType);

            string oldKeyboardShortcut = GrepSettings.Instance.Get<string>(GrepSettings.Key.RestoreWindowKeyboardShortcut);

            ApplicationFontFamily = EditApplicationFontFamily;
            MainFormFontSize = EditMainFormFontSize;
            ReplaceFormFontSize = EditReplaceFormFontSize;
            DialogFontSize = EditDialogFontSize;
            ResultsFontFamily = EditResultsFontFamily;
            ResultsFontSize = EditResultsFontSize;

            Settings.Set(GrepSettings.Key.IsSingletonInstance, IsSingletonInstance);
            Settings.Set(GrepSettings.Key.MinimizeToNotificationArea, MinimizeToNotificationArea);
            Settings.Set(GrepSettings.Key.RestoreWindowKeyboardShortcut, RestoreWindowKeyboardShortcut);
            Settings.Set(GrepSettings.Key.ConfirmExitScript, ConfirmExitScript);
            Settings.Set(GrepSettings.Key.ConfirmExitSearch, ConfirmExitSearch);
            Settings.Set(GrepSettings.Key.ConfirmExitSearchDuration, ConfirmExitSearchDuration);
            Settings.Set(GrepSettings.Key.PassCommandLineToSingleton, PassCommandLineToSingleton);
            Settings.Set(GrepSettings.Key.EnableUpdateChecking, EnableCheckForUpdates);
            Settings.Set(GrepSettings.Key.UpdateCheckInterval, CheckForUpdatesInterval);
            Settings.Set(GrepSettings.Key.CompareApplication, CompareApplicationPath);
            Settings.Set(GrepSettings.Key.CompareApplicationArgs, CompareApplicationArgs);
            Settings.Set(GrepSettings.Key.ShowFilePathInResults, ShowFilePathInResults);
            Settings.Set(GrepSettings.Key.ShowRelativeFilePath, ShowRelativeFilePath);
            Settings.Set(GrepSettings.Key.ShowFileErrorsInResults, ShowFileErrorsInResults);
            Settings.Set(GrepSettings.Key.NavigationButtonsVisible, NavigationButtonsVisible);
            Settings.Set(GrepSettings.Key.NavToolsPosition, NavigationToolsPosition);
            Settings.Set(GrepSettings.Key.NavToolsSize, NavigationToolsSize);
            Settings.Set(GrepSettings.Key.AllowSearchingForFileNamePattern, AllowSearchWithEmptyPattern);
            Settings.Set(GrepSettings.Key.DetectEncodingForFileNamePattern, DetectEncodingForFileNamePattern);
            Settings.Set(GrepSettings.Key.ExpandResults, AutoExpandSearchTree);
            Settings.Set(GrepSettings.Key.ShowVerboseMatchCount, ShowVerboseMatchCount);
            Settings.Set(GrepSettings.Key.ShowFileInfoTooltips, ShowFileInfoTooltips);
            Settings.Set(GrepSettings.Key.MatchTimeout, MatchTimeout);
            Settings.Set(GrepSettings.Key.FuzzyMatchThreshold, MatchThreshold);
            Settings.Set(GrepSettings.Key.ShowLinesInContext, ShowLinesInContext);
            Settings.Set(GrepSettings.Key.ContextLinesBefore, ContextLinesBefore);
            Settings.Set(GrepSettings.Key.ContextLinesAfter, ContextLinesAfter);
            Settings.Set(GrepSettings.Key.MaxSearchBookmarks, MaxSearchBookmarks);
            Settings.Set(GrepSettings.Key.MaxPathBookmarks, MaxPathBookmarks);
            Settings.Set(GrepSettings.Key.MaxExtensionBookmarks, MaxExtensionBookmarks);
            Settings.Set(GrepSettings.Key.OptionsOnMainPanel, OptionsLocation == PanelSelection.MainPanel);
            Settings.Set(GrepSettings.Key.StickyScroll, StickyScroll);
            Settings.Set(GrepSettings.Key.SortAutomaticallyOnSearch, SortAutomaticallyOnSearch);
            Settings.Set(GrepSettings.Key.MaximizeResultsTreeOnSearch, MaximizeResultsTreeOnSearch);
            Settings.Set(GrepSettings.Key.AutoCompleteEnabled, AutoCompleteEnabled);
            Settings.Set(GrepSettings.Key.MaxDegreeOfParallelism, MaxDegreeOfParallelism);
            Settings.Set(GrepSettings.Key.SearchAutoStopCount, SearchAutoStopCount);
            Settings.Set(GrepSettings.Key.SearchAutoPauseCount, SearchAutoPauseCount);
            Settings.Set(GrepSettings.Key.SetFocusElement, FocusElement);
            Settings.Set(GrepSettings.Key.ShowFullReplaceDialog, ReplaceDialogLayout == ReplaceDialogConfiguration.FullDialog);
            Settings.Set(GrepSettings.Key.DeleteToRecycleBin, DeleteOption == DeleteFilesDestination.Recycle);
            Settings.Set(GrepSettings.Key.OverwriteFilesOnCopy, CopyOverwriteFileOption);
            Settings.Set(GrepSettings.Key.OverwriteFilesOnMove, MoveOverwriteFileOption);
            Settings.Set(GrepSettings.Key.PreserveFolderLayoutOnCopy, PreserveFolderLayoutOnCopy);
            Settings.Set(GrepSettings.Key.PreserveFolderLayoutOnMove, PreserveFolderLayoutOnMove);
            Settings.Set(GrepSettings.Key.ArchiveCopy, ArchiveCopyOption);
            Settings.Set(GrepSettings.Key.ArchiveMove, ArchiveMoveOption);
            Settings.Set(GrepSettings.Key.ArchiveDelete, ArchiveDeleteOption);
            Settings.Set(GrepSettings.Key.FollowWindowsTheme, FollowWindowsTheme);
            Settings.Set(GrepSettings.Key.CurrentTheme, CurrentTheme);
            Settings.Set(GrepSettings.Key.CurrentCulture, CurrentCulture);
            Settings.Set(GrepSettings.Key.UseDefaultFont, UseDefaultFont);
            Settings.Set(GrepSettings.Key.ApplicationFontFamily, ApplicationFontFamily);
            Settings.Set(GrepSettings.Key.MainFormFontSize, MainFormFontSize);
            Settings.Set(GrepSettings.Key.ReplaceFormFontSize, ReplaceFormFontSize);
            Settings.Set(GrepSettings.Key.DialogFontSize, DialogFontSize);
            Settings.Set(GrepSettings.Key.ResultsFontFamily, ResultsFontFamily);
            Settings.Set(GrepSettings.Key.ResultsFontSize, ResultsFontSize);
            Settings.Set(GrepSettings.Key.ResultsFileNameWeight, ResultsFileNameWeight);
            Settings.Set(GrepSettings.Key.HexResultByteLength, HexResultByteLength);
            Settings.Set(GrepSettings.Key.PreviewLargeFileLimit, PreviewLargeFileLimit);
            Settings.Set(GrepSettings.Key.CacheExtractedFiles, CacheExtractedFiles);
            Settings.Set(GrepSettings.Key.CacheFileHashType, CacheFileHashType);
            Settings.Set(GrepSettings.Key.CacheFilesInTempFolder, CacheFilesInTempFolder);
            Settings.Set(GrepSettings.Key.CacheFilePath, CacheFilePath);
            Settings.Set(GrepSettings.Key.CacheFilesCleanDays, CacheFilesCleanDays);
            Settings.Set(GrepSettings.Key.PdfToTextOptions, PdfToTextOptions);
            Settings.Set(GrepSettings.Key.PdfJoinLines, PdfJoinLines);
            Settings.Set(GrepSettings.Key.PdfNumberStyle, PdfNumberStyle);
            Settings.Set(GrepSettings.Key.WordExtractFootnotes, WordExtractFootnotes);
            Settings.Set(GrepSettings.Key.WordFootnoteReference, WordFootnoteReference);
            Settings.Set(GrepSettings.Key.WordExtractComments, WordExtractComments);
            Settings.Set(GrepSettings.Key.WordCommentReference, WordCommentReference);
            Settings.Set(GrepSettings.Key.WordExtractHeaders, WordExtractHeaders);
            Settings.Set(GrepSettings.Key.WordExtractFooters, WordExtractFooters);
            Settings.Set(GrepSettings.Key.WordHeaderFooterPosition, WordHeaderFooterPosition);

            if (!oldKeyboardShortcut.Equals(RestoreWindowKeyboardShortcut, StringComparison.Ordinal) &&
                App.Current.MainWindow is MainForm mainWind)
            {
                if (!mainWind.InitializeKeyboardShortcut())
                {
                    MessageBox.Show(Resources.MessageBox_CouldNotRegisterTheNewKeyboardShortcut, Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK,
                    TranslationSource.Instance.FlowDirection);
                }
            }

            foreach (var visOpt in VisibilityOptions)
            {
                if (visOpt.IsChanged)
                {
                    visOpt.UpdateOption();
                }
            }

            if (GrepSettings.CleanExtensions(ArchiveExtensions) != Settings.Get<string>(GrepSettings.Key.ArchiveExtensions))
            {
                Settings.Set(GrepSettings.Key.ArchiveExtensions, GrepSettings.CleanExtensions(ArchiveExtensions));
                Settings.Set(GrepSettings.Key.ArchiveCustomExtensions, GrepSettings.CleanExtensions(ArchiveCustomExtensions));
                ArchiveDirectory.Reinitialize();
            }

            bool pluginsChanged = Plugins.Any(p => p.IsChanged);
            List<PluginConfiguration> plugins = Plugins.Select(v => v.Save()).ToList();
            Settings.Set(GrepSettings.Key.Plugins, plugins);

            List<CustomEditor> editors = CustomEditors.Select(v => v.Save()).ToList();
            bool editorsChanged = !editors.SequenceEqual(originalCustomEditors);
            Settings.Set(GrepSettings.Key.CustomEditors, editors);

            Settings.Save();

            if (pluginsChanged)
                GrepEngineFactory.ReloadPlugins();

            if (editorsChanged)
                GrepSearchResultsViewModel.SearchResultsMessenger.NotifyColleagues("EditorsChanged");

            if ((isCacheRemoved || isCachePathChanged || isCacheHashTypeChanged) &&
                Directory.Exists(oldCachePath))
            {
                if (MessageBoxResult.Yes == MessageBox.Show(
                    Resources.MessageBox_ThePlugInCacheSettingsHaveChanged, Resources.MessageBox_DnGrep,
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes,
                    TranslationSource.Instance.FlowDirection))
                {
                    PluginCacheCleared = true;
                    try
                    {
                        Directory.Delete(oldCachePath, true);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Failed to delete the plug-in cache folder '{oldCachePath}'");
                    }
                }
            }
        }

        public bool PluginCacheCleared { get; private set; }

        #endregion
    }

    public partial class VisibilityOption : CultureAwareViewModel
    {
        public VisibilityOption(string groupKey, string labelKey, string optionKey)
        {
            GroupKey = groupKey;
            LabelKey = labelKey;
            OptionKey = optionKey;

            isVisible = origIsVisible = GrepSettings.Instance.Get<bool>(OptionKey);
        }

        public string GroupKey { get; private set; }
        public string LabelKey { get; private set; }
        public string OptionKey { get; private set; }

        public string Group => TranslationSource.Instance[GroupKey];

        public string Label => TranslationSource.Instance[LabelKey].TrimEnd(':', '…');

        public bool IsChanged => IsVisible != origIsVisible;

        public void UpdateOption()
        {
            GrepSettings.Instance.Set(OptionKey, IsVisible);
            origIsVisible = IsVisible;
        }

        [ObservableProperty]
        private bool isVisible;
        private bool origIsVisible;
    }

    public class FontInfo(string familyName)
    {
        public string FamilyName { get; private set; } = familyName;
        public bool IsMonospaced { get; private set; } = GetIsMonospaced(familyName);

        private static bool GetIsMonospaced(string familyName)
        {
            Typeface typeface = new(new FontFamily(familyName), SystemFonts.MessageFontStyle,
                SystemFonts.MessageFontWeight, FontStretches.Normal);

            var narrowChar = new FormattedText("i", TranslationSource.Instance.CurrentCulture,
                TranslationSource.Instance.CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                typeface, 12, Brushes.Black, null, 1);
            var wideChar = new FormattedText("w", TranslationSource.Instance.CurrentCulture,
                TranslationSource.Instance.CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                typeface, 12, Brushes.Black, null, 1);

            return narrowChar.Width == wideChar.Width;
        }
    }
}
