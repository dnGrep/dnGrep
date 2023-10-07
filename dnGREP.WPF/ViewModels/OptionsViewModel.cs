using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Engines;
using dnGREP.Localization;
using dnGREP.WPF.MVHelpers;
using Microsoft.Win32;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public partial class OptionsViewModel : CultureAwareViewModel
    {
        public event EventHandler? RequestClose;

        public enum PanelSelection { MainPanel = 0, OptionsExpander }

        public enum ReplaceDialogConfiguration { FullDialog = 0, FilesOnly }

        public enum DeleteFilesDestination { Recycle, Permanent }

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly string ellipsis = char.ConvertFromUtf32(0x2026);

        public OptionsViewModel()
        {
            TaskLimit = Environment.ProcessorCount * 4;

            LoadSettings();

            foreach (string name in AppTheme.Instance.ThemeNames)
                ThemeNames.Add(name);

            CultureNames = TranslationSource.AppCultures
                .OrderBy(kv => kv.Value, StringComparer.CurrentCulture).ToArray();

            CustomEditorTemplates = ConfigurationTemplate.EditorConfigurationTemplates.ToArray();
            CompareApplicationTemplates = ConfigurationTemplate.CompareConfigurationTemplates.ToArray();

            HexLengthOptions = new List<int> { 8, 16, 32, 64, 128 };

            hasWindowsThemes = AppTheme.HasWindowsThemes;
            AppTheme.Instance.CurrentThemeChanged += (s, e) =>
            {
                CurrentTheme = AppTheme.Instance.CurrentThemeName;
            };

            TranslationSource.Instance.CurrentCultureChanged += (s, e) =>
            {
                int count = TranslationSource.CountPlaceholders(Resources.Options_CustomEditorHelp);
                if (count == 5)
                {
                    CustomEditorHelp = TranslationSource.Format(Resources.Options_CustomEditorHelp,
                        File, Line, Pattern, Match, Column);
                }
                else
                {
                    CustomEditorHelp = TranslationSource.Format(Resources.Options_CustomEditorHelp,
                        File, Line, Pattern, Match, Column, Page);
                }
                PanelTooltip = IsAdministrator ? string.Empty : Resources.Options_ToChangeThisSettingRunDnGREPAsAdministrator;
                WindowsIntegrationTooltip = IsAdministrator ? Resources.Options_EnablesStartingDnGrepFromTheWindowsExplorerRightClickContextMenu : string.Empty;

                foreach (var item in VisibilityOptions)
                {
                    item.UpdateLabel();
                }

                // call these to reformat decimal separators
                OnPropertyChanged(nameof(EditMainFormFontSize));
                OnPropertyChanged(nameof(EditReplaceFormFontSize));
                OnPropertyChanged(nameof(EditDialogFontSize));
                OnPropertyChanged(nameof(EditResultsFontSize));
                OnPropertyChanged(nameof(MatchTimeout));
                OnPropertyChanged(nameof(MatchThreshold));
            };


            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_Features, nameof(Resources.Main_Menu_Bookmarks), GrepSettings.Key.BookmarksVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_Features, nameof(Resources.Main_TestExpression), GrepSettings.Key.TestExpressionVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_Features, nameof(Resources.Main_ReplaceButton), GrepSettings.Key.ReplaceVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_Features, nameof(Resources.Main_SortButton), GrepSettings.Key.SortVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_Features, nameof(Resources.Main_MoreArrowButton), GrepSettings.Key.MoreVisible));

            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_FileFilter, nameof(Resources.Main_SearchInArchives), GrepSettings.Key.SearchInArchivesVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_FileFilter, nameof(Resources.Main_AllSizes), GrepSettings.Key.SizeFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_FileFilter, nameof(Resources.Main_IncludeSubfolders), GrepSettings.Key.SubfoldersFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_FileFilter, nameof(Resources.Main_IncludeHiddenFolders), GrepSettings.Key.HiddenFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_FileFilter, nameof(Resources.Main_IncludeBinaryFiles), GrepSettings.Key.BinaryFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_FileFilter, nameof(Resources.Main_FollowSymbolicLinks), GrepSettings.Key.SymbolicLinkFilterVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_FileFilter, nameof(Resources.Main_AllDates), GrepSettings.Key.DateFilterVisible));

            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SpecialOptions, nameof(Resources.Main_SearchParallel), GrepSettings.Key.SearchParallelVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SpecialOptions, nameof(Resources.Main_UseGitignore), GrepSettings.Key.UseGitIgnoreVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SpecialOptions, nameof(Resources.Main_SkipRemoteCloudStorageFiles), GrepSettings.Key.SkipCloudStorageVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SpecialOptions, nameof(Resources.Main_Encoding), GrepSettings.Key.EncodingVisible));

            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SearchType, nameof(Resources.Main_SearchType_Regex), GrepSettings.Key.SearchTypeRegexVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SearchType, nameof(Resources.Main_SearchType_XPath), GrepSettings.Key.SearchTypeXPathVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SearchType, nameof(Resources.Main_SearchType_Text), GrepSettings.Key.SearchTypeTextVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SearchType, nameof(Resources.Main_SearchType_Phonetic), GrepSettings.Key.SearchTypePhoneticVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SearchType, nameof(Resources.Main_SearchType_Hex), GrepSettings.Key.SearchTypeByteVisible));

            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SearchOptions, nameof(Resources.Main_BooleanOperators), GrepSettings.Key.BooleanOperatorsVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_SearchOptions, nameof(Resources.Main_CaptureGroupSearch), GrepSettings.Key.CaptureGroupSearchVisible));

            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_ResultOptions, nameof(Resources.Main_SearchInResults), GrepSettings.Key.SearchInResultsVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_ResultOptions, nameof(Resources.Main_PreviewFile), GrepSettings.Key.PreviewFileVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_ResultOptions, nameof(Resources.Main_StopAfterFirstMatch), GrepSettings.Key.StopAfterFirstMatchVisible));

            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_ResultsTree, nameof(Resources.Main_HighlightMatches), GrepSettings.Key.HighlightMatchesVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_ResultsTree, nameof(Resources.Main_HighlightGroups), GrepSettings.Key.HighlightGroupsVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_ResultsTree, nameof(Resources.Main_ContextShowLines), GrepSettings.Key.ShowContextLinesVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_ResultsTree, nameof(Resources.Main_Zoom), GrepSettings.Key.ZoomResultsTreeVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_ResultsTree, nameof(Resources.Main_WrapText), GrepSettings.Key.WrapTextResultsTreeVisible));

            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_PreviewWindow, nameof(Resources.Preview_Zoom), GrepSettings.Key.PreviewZoomWndVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_PreviewWindow, nameof(Resources.Preview_WrapText), GrepSettings.Key.WrapTextPreviewWndVisible));
            VisibilityOptions.Add(new VisibilityOption(Resources.Options_Personalize_PreviewWindow, nameof(Resources.Preview_Syntax), GrepSettings.Key.SyntaxPreviewWndVisible));
        }

        #region Private Variables and Properties
        private static readonly string SHELL_KEY_NAME = "dnGREP";
        private static readonly string SHELL_MENU_TEXT = "dnGrep...";
        private const string File = "%file";
        private const string Page = "%page";
        private const string Line = "%line";
        private const string Pattern = "%pattern";
        private const string Match = "%match";
        private const string Column = "%column";
        private const string ArchiveNameKey = "Archive";
        private const string CustomNameKey = " Custom";
        private const string Enabledkey = "Enabled";
        private const string PreviewTextKey = "PreviewText";
        private static GrepSettings Settings => GrepSettings.Instance;
        #endregion

        #region Properties
        public bool CanSave
        {
            get
            {
                if (EnableWindowsIntegration != IsShellRegistered("Directory") ||
                EnableRunAtStartup != IsStartupRegistered() ||
                IsSingletonInstance != Settings.Get<bool>(GrepSettings.Key.IsSingletonInstance) ||
                ConfirmExitScript != Settings.Get<bool>(GrepSettings.Key.ConfirmExitScript) ||
                ConfirmExitSearch != Settings.Get<bool>(GrepSettings.Key.ConfirmExitSearch) ||
                ConfirmExitSearchDuration != Settings.Get<double>(GrepSettings.Key.ConfirmExitSearchDuration) ||
                PassSearchFolderToSingleton != Settings.Get<bool>(GrepSettings.Key.PassSearchFolderToSingleton) ||
                EnableCheckForUpdates != Settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking) ||
                CheckForUpdatesInterval != Settings.Get<int>(GrepSettings.Key.UpdateCheckInterval) ||
                ShowLinesInContext != Settings.Get<bool>(GrepSettings.Key.ShowLinesInContext) ||
                ContextLinesBefore != Settings.Get<int>(GrepSettings.Key.ContextLinesBefore) ||
                ContextLinesAfter != Settings.Get<int>(GrepSettings.Key.ContextLinesAfter) ||
                CustomEditorPath != Settings.Get<string>(GrepSettings.Key.CustomEditor) ||
                CustomEditorArgs != Settings.Get<string>(GrepSettings.Key.CustomEditorArgs) ||
                CompareApplicationPath != Settings.Get<string>(GrepSettings.Key.CompareApplication) ||
                CompareApplicationArgs != Settings.Get<string>(GrepSettings.Key.CompareApplicationArgs) ||
                ShowFilePathInResults != Settings.Get<bool>(GrepSettings.Key.ShowFilePathInResults) ||
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
                MaximizeResultsTreeOnSearch != Settings.Get<bool>(GrepSettings.Key.MaximizeResultsTreeOnSearch) ||
                MaxDegreeOfParallelism != Settings.Get<int>(GrepSettings.Key.MaxDegreeOfParallelism) ||
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
                HexResultByteLength != Settings.Get<int>(GrepSettings.Key.HexResultByteLength) ||
                PdfToTextOptions != Settings.Get<string>(GrepSettings.Key.PdfToTextOptions) ||
                PdfNumberStyle != Settings.Get<PdfNumberType>(GrepSettings.Key.PdfNumberStyle) ||
                ArchiveOptions.IsChanged ||
                IsChanged(Plugins) ||
                IsChanged(VisibilityOptions)
                )
                {
                    return CurrentCulture != null;
                }
                else
                {
                    return false;
                }
            }
        }

        private static bool IsChanged(IList<PluginOptions> plugins)
        {
            return plugins.Any(p => p.IsChanged);
        }

        private static bool IsChanged(IList<VisibilityOption> visibilityOptions)
        {
            return visibilityOptions.Any(p => p.IsChanged);
        }

        public KeyValuePair<string, string>[] CultureNames { get; }

        public KeyValuePair<string, ConfigurationTemplate?>[] CompareApplicationTemplates { get; }

        public KeyValuePair<string, ConfigurationTemplate?>[] CustomEditorTemplates { get; }

        public List<int> HexLengthOptions { get; }

        public ObservableCollection<PluginOptions> Plugins { get; } = new();

        public static IList<FontInfo> FontFamilies
        {
            get { return Fonts.SystemFontFamilies.Select(r => new FontInfo(r.Source)).ToList(); }
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

        private void ApplyCustomEditorTemplate(ConfigurationTemplate? template)
        {
            if (template != null)
            {
                CustomEditorPath = ellipsis + template.ExeFileName;
                CustomEditorArgs = template.Arguments;

                Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    UIServices.SetBusyState();
                    string fullPath = ConfigurationTemplate.FindExePath(template);
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        CustomEditorPath = fullPath;
                        CustomEditorArgs = template.Arguments;
                    }
                }, DispatcherPriority.ApplicationIdle);
            }
        }

        public ObservableCollection<VisibilityOption> VisibilityOptions { get; } = new();

        public ObservableCollection<string> ThemeNames { get; } = new();


        [ObservableProperty]
        private bool enableWindowsIntegration;

        [ObservableProperty]
        private bool isSingletonInstance;

        [ObservableProperty]
        private bool confirmExitScript;

        [ObservableProperty]
        private bool confirmExitSearch;

        [ObservableProperty]
        private double confirmExitSearchDuration;

        [ObservableProperty]
        private bool passSearchFolderToSingleton;

        [ObservableProperty]
        private string? windowsIntegrationTooltip;

        [ObservableProperty]
        private string? panelTooltip;

        [ObservableProperty]
        private bool isAdministrator;

        [ObservableProperty]
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
        private ConfigurationTemplate? customEditorTemplate = null;
        partial void OnCustomEditorTemplateChanged(ConfigurationTemplate? value)
        {
            ApplyCustomEditorTemplate(value);
        }

        [ObservableProperty]
        private string customEditorPath = string.Empty;

        [ObservableProperty]
        private string customEditorArgs = string.Empty;

        [ObservableProperty]
        private string customEditorHelp = string.Empty;

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
        private bool maximizeResultsTreeOnSearch;

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
        private int hexResultByteLength = 16;

        [ObservableProperty]
        private string pdfToTextOptions = string.Empty;

        [ObservableProperty]
        private PdfNumberType pdfNumberStyle = PdfNumberType.PageNumber;

        [ObservableProperty]
        private PluginOptions archiveOptions;

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

        #endregion

        #region Commands

        /// <summary>
        /// Returns a command that saves the form
        /// </summary>
        public ICommand SaveCommand => new RelayCommand(
            param => Save(),
            param => CanSave);

        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BrowseEditorCommand => new RelayCommand(
            param => BrowseToEditor());

        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BrowseCompareCommand => new RelayCommand(
            param => BrowseToCompareApp());

        /// <summary>
        /// Returns a command that clears old searches.
        /// </summary>
        public ICommand ClearSearchesCommand => new RelayCommand(
            param => ClearSearches());

        /// <summary>
        /// Returns a command that reloads the current theme file.
        /// </summary>
        public ICommand ReloadThemeCommand => new RelayCommand(
            param => AppTheme.Instance.ReloadCurrentTheme());

        /// <summary>
        /// Returns a command that loads an external resx file.
        /// </summary>
        public ICommand LoadResxCommand => new RelayCommand(
            param => LoadResxFile());

        public ICommand ResetPdfToTextOptionCommand => new RelayCommand(
            p => PdfToTextOptions = defaultPdfToText,
            q => !PdfToTextOptions.Equals(defaultPdfToText, StringComparison.Ordinal));

        private const string defaultPdfToText = "-layout -enc UTF-8 -bom";

        #endregion

        #region Public Methods

        /// <summary>
        /// Saves the settings to file.  This method is invoked by the SaveCommand.
        /// </summary>
        public void Save()
        {
            SaveSettings();
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

        public void BrowseToEditor()
        {
            var dlg = new OpenFileDialog();
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                CustomEditorPath = dlg.FileName;
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

        private static void ClearSearches()
        {
            // keep the pinned bookmarks
            Settings.Set(GrepSettings.Key.FastPathBookmarks,
                Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastPathBookmarks).Where(r => r.IsPinned));

            Settings.Set(GrepSettings.Key.FastFileMatchBookmarks,
                Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastFileMatchBookmarks).Where(r => r.IsPinned));

            Settings.Set(GrepSettings.Key.FastFileNotMatchBookmarks,
                Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastFileNotMatchBookmarks).Where(r => r.IsPinned));

            Settings.Set(GrepSettings.Key.FastSearchBookmarks,
                Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastSearchBookmarks).Where(r => r.IsPinned));

            Settings.Set(GrepSettings.Key.FastReplaceBookmarks,
                Settings.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastReplaceBookmarks).Where(r => r.IsPinned));
        }

#pragma warning disable MVVMTK0034
        [MemberNotNull(nameof(archiveOptions))]
        private void LoadSettings()
        {
            CheckIfAdmin();
            if (!IsAdministrator)
            {
                PanelTooltip = Resources.Options_ToChangeThisSettingRunDnGREPAsAdministrator;
            }
            else
            {
                WindowsIntegrationTooltip = Resources.Options_EnablesStartingDnGrepFromTheWindowsExplorerRightClickContextMenu;
            }
            EnableWindowsIntegration = IsShellRegistered("Directory");
            EnableRunAtStartup = IsStartupRegistered();
            IsSingletonInstance = Settings.Get<bool>(GrepSettings.Key.IsSingletonInstance);
            ConfirmExitScript = Settings.Get<bool>(GrepSettings.Key.ConfirmExitScript);
            ConfirmExitSearch = Settings.Get<bool>(GrepSettings.Key.ConfirmExitSearch);
            ConfirmExitSearchDuration = Settings.Get<double>(GrepSettings.Key.ConfirmExitSearchDuration);
            PassSearchFolderToSingleton = Settings.Get<bool>(GrepSettings.Key.PassSearchFolderToSingleton);
            EnableCheckForUpdates = Settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking);
            CheckForUpdatesInterval = Settings.Get<int>(GrepSettings.Key.UpdateCheckInterval);
            CustomEditorPath = Settings.Get<string>(GrepSettings.Key.CustomEditor);
            CustomEditorArgs = Settings.Get<string>(GrepSettings.Key.CustomEditorArgs);
            CompareApplicationPath = Settings.Get<string>(GrepSettings.Key.CompareApplication);
            CompareApplicationArgs = Settings.Get<string>(GrepSettings.Key.CompareApplicationArgs);
            ShowFilePathInResults = Settings.Get<bool>(GrepSettings.Key.ShowFilePathInResults);
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
            MaximizeResultsTreeOnSearch = Settings.Get<bool>(GrepSettings.Key.MaximizeResultsTreeOnSearch);
            MaxDegreeOfParallelism = Settings.Get<int>(GrepSettings.Key.MaxDegreeOfParallelism);

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

            int count = TranslationSource.CountPlaceholders(Resources.Options_CustomEditorHelp);
            if (count == 5)
            {
                CustomEditorHelp = TranslationSource.Format(Resources.Options_CustomEditorHelp,
                    File, Line, Pattern, Match, Column);
            }
            else
            {
                CustomEditorHelp = TranslationSource.Format(Resources.Options_CustomEditorHelp,
                    File, Line, Pattern, Match, Column, Page);
            }

            // current values may not equal the saved settings value
            CurrentTheme = AppTheme.Instance.CurrentThemeName;
            FollowWindowsTheme = AppTheme.Instance.FollowWindowsTheme;
            CurrentCulture = TranslationSource.Instance.CurrentCulture.Name;

            HexResultByteLength = Settings.Get<int>(GrepSettings.Key.HexResultByteLength);
            PdfToTextOptions = Settings.Get<string>(GrepSettings.Key.PdfToTextOptions);
            PdfNumberStyle = Settings.Get<PdfNumberType>(GrepSettings.Key.PdfNumberStyle);

            {
                string extensionList = string.Join(", ", Settings.GetExtensionList(ArchiveNameKey,
                    ArchiveDirectory.DefaultExtensions));

                string customExtensionList = string.Join(", ", Settings.GetExtensionList(ArchiveNameKey + CustomNameKey,
                    new List<string>()));

                ArchiveOptions = new PluginOptions(ArchiveNameKey, true, false,
                    extensionList, string.Join(", ", ArchiveDirectory.DefaultExtensions),
                    customExtensionList);
            }

            Plugins.Clear();
            foreach (var plugin in GrepEngineFactory.AllPlugins.OrderBy(p => p.Name))
            {
                string nameKey = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(plugin.Name);
                string enabledkey = nameKey + Enabledkey;
                string previewTextKey = nameKey + PreviewTextKey;
                string extensionList = string.Join(", ", Settings.GetExtensionList(nameKey,
                    plugin.DefaultExtensions));
                string customExtensionList = string.Join(", ", Settings.GetExtensionList(nameKey + CustomNameKey,
                    new List<string>()));

                bool isEnabled = true;
                if (GrepSettings.Instance.ContainsKey(enabledkey))
                    isEnabled = GrepSettings.Instance.Get<bool>(enabledkey);

                bool previewTextEnabled = true;
                if (GrepSettings.Instance.ContainsKey(previewTextKey))
                    previewTextEnabled = GrepSettings.Instance.Get<bool>(previewTextKey);

                var pluginOptions = new PluginOptions(
                    plugin.Name, isEnabled, previewTextEnabled,
                    extensionList, string.Join(", ", plugin.DefaultExtensions),
                    customExtensionList);

                Plugins.Add(pluginOptions);
            }
        }
#pragma warning restore MVVMTK0034

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

        private void SaveSettings()
        {
            if (EnableWindowsIntegration)
            {
                ShellRegister("Directory");
                ShellRegister("Drive");
                ShellRegister("*");
                ShellRegister("here");
            }
            else
            {
                ShellUnregister("Directory");
                ShellUnregister("Drive");
                ShellUnregister("*");
                ShellUnregister("here");
            }

            if (EnableRunAtStartup)
            {
                StartupRegister();
            }
            else
            {
                StartupUnregister();
            }

            ApplicationFontFamily = EditApplicationFontFamily;
            MainFormFontSize = EditMainFormFontSize;
            ReplaceFormFontSize = EditReplaceFormFontSize;
            DialogFontSize = EditDialogFontSize;
            ResultsFontFamily = EditResultsFontFamily;
            ResultsFontSize = EditResultsFontSize;

            Settings.Set(GrepSettings.Key.IsSingletonInstance, IsSingletonInstance);
            Settings.Set(GrepSettings.Key.ConfirmExitScript, ConfirmExitScript);
            Settings.Set(GrepSettings.Key.ConfirmExitSearch, ConfirmExitSearch);
            Settings.Set(GrepSettings.Key.ConfirmExitSearchDuration, ConfirmExitSearchDuration);
            Settings.Set(GrepSettings.Key.PassSearchFolderToSingleton, PassSearchFolderToSingleton);
            Settings.Set(GrepSettings.Key.EnableUpdateChecking, EnableCheckForUpdates);
            Settings.Set(GrepSettings.Key.UpdateCheckInterval, CheckForUpdatesInterval);
            Settings.Set(GrepSettings.Key.CustomEditor, CustomEditorPath);
            Settings.Set(GrepSettings.Key.CustomEditorArgs, CustomEditorArgs);
            Settings.Set(GrepSettings.Key.CompareApplication, CompareApplicationPath);
            Settings.Set(GrepSettings.Key.CompareApplicationArgs, CompareApplicationArgs);
            Settings.Set(GrepSettings.Key.ShowFilePathInResults, ShowFilePathInResults);
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
            Settings.Set(GrepSettings.Key.MaximizeResultsTreeOnSearch, MaximizeResultsTreeOnSearch);
            Settings.Set(GrepSettings.Key.MaxDegreeOfParallelism, MaxDegreeOfParallelism);
            Settings.Set(GrepSettings.Key.ShowFullReplaceDialog, ReplaceDialogLayout == ReplaceDialogConfiguration.FullDialog);
            Settings.Set(GrepSettings.Key.DeleteToRecycleBin, DeleteOption == DeleteFilesDestination.Recycle);
            Settings.Set(GrepSettings.Key.OverwriteFilesOnCopy, CopyOverwriteFileOption);
            Settings.Set(GrepSettings.Key.OverwriteFilesOnMove, MoveOverwriteFileOption);
            Settings.Set(GrepSettings.Key.PreserveFolderLayoutOnCopy, PreserveFolderLayoutOnCopy);
            Settings.Set(GrepSettings.Key.PreserveFolderLayoutOnMove, PreserveFolderLayoutOnMove);
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
            Settings.Set(GrepSettings.Key.HexResultByteLength, HexResultByteLength);
            Settings.Set(GrepSettings.Key.PdfToTextOptions, PdfToTextOptions);
            Settings.Set(GrepSettings.Key.PdfNumberStyle, PdfNumberStyle);

            foreach (var visOpt in VisibilityOptions)
            {
                if (visOpt.IsChanged)
                {
                    visOpt.UpdateOption();
                }
            }

            if (ArchiveOptions.IsChanged)
            {
                Settings.SetExtensions(ArchiveNameKey, ArchiveOptions.MappedExtensions);
                Settings.SetExtensions(ArchiveNameKey + CustomNameKey, ArchiveOptions.CustomExtensions);

                ArchiveOptions.SetUnchanged();
                ArchiveDirectory.Reinitialize();
            }

            bool pluginsChanged = Plugins.Any(p => p.IsChanged);
            foreach (var plugin in Plugins)
            {
                string nameKey = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(plugin.Name);
                string enabledkey = nameKey + Enabledkey;
                string previewTextKey = nameKey + PreviewTextKey;

                Settings.Set(enabledkey, plugin.IsEnabled);
                Settings.Set(previewTextKey, plugin.PreviewTextEnabled);
                Settings.SetExtensions(nameKey, plugin.MappedExtensions);
                Settings.SetExtensions(nameKey + CustomNameKey, plugin.CustomExtensions);

                plugin.SetUnchanged();
            }

            Settings.Save();

            if (pluginsChanged)
                GrepEngineFactory.ReloadPlugins();
        }

        private bool IsShellRegistered(string location)
        {
            if (location == "here")
            {
                string regPath = $@"SOFTWARE\Classes\Directory\Background\shell\{SHELL_KEY_NAME}";
                try
                {
                    return Registry.LocalMachine.OpenSubKey(regPath) != null;
                }
                catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    IsAdministrator = false;
                    return false;
                }
            }
            else
            {
                string regPath = $@"{location}\shell\{SHELL_KEY_NAME}";
                try
                {
                    return Registry.ClassesRoot.OpenSubKey(regPath) != null;
                }
                catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    IsAdministrator = false;
                    return false;
                }
            }
        }

        private void ShellRegister(string location)
        {
            if (!IsAdministrator)
                return;

            if (!IsShellRegistered(location))
            {
                try
                {
                    var assemblyPath = Assembly.GetAssembly(typeof(OptionsView))?.Location
                        .Replace("dll", "exe", StringComparison.OrdinalIgnoreCase);
                    if (assemblyPath != null)
                    {
                        if (location == "here")
                        {
                            string regPath = $@"SOFTWARE\Classes\Directory\Background\shell\{SHELL_KEY_NAME}";

                            // add context menu to the registry
                            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(regPath))
                            {
                                key.SetValue(null, SHELL_MENU_TEXT);
                                key.SetValue("Icon", assemblyPath);
                            }

                            // add command that is invoked to the registry
                            string menuCommand = string.Format("\"{0}\" \"%V\"", assemblyPath);
                            using (RegistryKey key = Registry.LocalMachine.CreateSubKey($@"{regPath}\command"))
                            {
                                key.SetValue(null, menuCommand);
                            }
                        }
                        else
                        {
                            string regPath = $@"{location}\shell\{SHELL_KEY_NAME}";

                            // add context menu to the registry
                            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(regPath))
                            {
                                key.SetValue(null, SHELL_MENU_TEXT);
                                key.SetValue("Icon", assemblyPath);
                            }

                            // add command that is invoked to the registry
                            string menuCommand = string.Format("\"{0}\" \"%1\"", assemblyPath);
                            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey($@"{regPath}\command"))
                            {
                                key.SetValue(null, menuCommand);
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    IsAdministrator = false;
                    MessageBox.Show(Resources.MessageBox_RunDnGrepAsAdministrator,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to register dnGrep with Explorer context menu");
                    MessageBox.Show(Resources.MessageBox_ThereWasAnErrorAddingDnGrepToExplorerRightClickMenu + App.LogDir,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
            }
        }

        private void ShellUnregister(string location)
        {
            if (!IsAdministrator)
                return;

            try
            {
                if (IsShellRegistered(location))
                {
                    if (location == "here")
                    {
                        string regPath = $@"SOFTWARE\Classes\Directory\Background\shell\{SHELL_KEY_NAME}";
                        Registry.LocalMachine.DeleteSubKeyTree(regPath);
                    }
                    else
                    {
                        string regPath = $@"{location}\shell\{SHELL_KEY_NAME}";
                        Registry.ClassesRoot.DeleteSubKeyTree(regPath);
                    }
                }
            }
            catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
            {
                IsAdministrator = false;
                MessageBox.Show(Resources.MessageBox_RunDnGrepAsAdministrator,
                    Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to remove dnGrep from Explorer context menu");
                MessageBox.Show(Resources.MessageBox_ThereWasAnErrorRemovingDnGrepFromTheExplorerRightClickMenu + App.LogDir,
                    Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            }
        }

        private static bool IsStartupRegistered()
        {
            string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(regPath);
                return key?.GetValue(SHELL_KEY_NAME) != null;
            }
            catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static void StartupRegister()
        {
            if (!IsStartupRegistered())
            {
                try
                {
                    string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

                    var assemblyPath = Assembly.GetAssembly(typeof(OptionsView))?.Location
                        .Replace("dll", "exe", StringComparison.OrdinalIgnoreCase);
                    if (assemblyPath != null)
                    {
                        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(regPath, true);
                        key?.SetValue(SHELL_KEY_NAME, string.Format("\"{0}\" /warmUp", assemblyPath), RegistryValueKind.ExpandString);
                    }
                }
                catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(Resources.MessageBox_RunDnGrepAsAdministratorToChangeStartupRegister,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to register auto startup");
                    MessageBox.Show(Resources.MessageBox_ThereWasAnErrorRegisteringAutoStartup + App.LogDir,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
            }
        }

        private static void StartupUnregister()
        {
            if (IsStartupRegistered())
            {
                try
                {
                    string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(regPath, true);
                    key?.DeleteValue(SHELL_KEY_NAME);
                }
                catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(Resources.MessageBox_RunDnGrepAsAdministratorToChangeStartupRegister,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to unregister auto startup");
                    MessageBox.Show(Resources.MessageBox_ThereWasAnErrorUnregisteringAutoStartup + App.LogDir,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
            }
        }

        private void CheckIfAdmin()
        {
            try
            {
                using WindowsIdentity wi = WindowsIdentity.GetCurrent();
                WindowsPrincipal wp = new(wi);

                IsAdministrator = wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                IsAdministrator = false;
            }
        }

        #endregion
    }

    public partial class PluginOptions : CultureAwareViewModel
    {
        public PluginOptions(string name, bool enabled, bool previewTextEnabled,
            string extensions, string defaultExtensions, string customExtensions)
        {
            Name = name;
            IsEnabled = origIsEnabled = enabled;
            PreviewTextEnabled = origPreviewTextEnabled = previewTextEnabled;
            MappedExtensions = origMappedExtensions = extensions;
            DefaultExtensions = defaultExtensions ?? string.Empty;
            CustomExtensions = origCustomExtensions = customExtensions ?? string.Empty;
        }

        public bool IsChanged => IsEnabled != origIsEnabled ||
            PreviewTextEnabled != origPreviewTextEnabled ||
            MappedExtensions != origMappedExtensions ||
            CustomExtensions != origCustomExtensions;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private bool isEnabled;
        private bool origIsEnabled;


        [ObservableProperty]
        private bool previewTextEnabled;
        private bool origPreviewTextEnabled;

        [ObservableProperty]
        private string mappedExtensions = string.Empty;
        private string origMappedExtensions = string.Empty;

        public string DefaultExtensions { get; private set; }


        [ObservableProperty]
        private string customExtensions = string.Empty;
        private string origCustomExtensions = string.Empty;


        public ICommand ResetExtensions => new RelayCommand(
            p => MappedExtensions = DefaultExtensions,
            q => !MappedExtensions.Equals(DefaultExtensions, StringComparison.Ordinal));


        internal void SetUnchanged()
        {
            origIsEnabled = IsEnabled;
            origPreviewTextEnabled = PreviewTextEnabled;
            origMappedExtensions = MappedExtensions;
            origCustomExtensions = CustomExtensions;
        }
    }

    public partial class VisibilityOption : CultureAwareViewModel
    {
        public VisibilityOption(string group, string labelKey, string optionKey)
        {
            Group = group;
            LabelKey = labelKey;
            OptionKey = optionKey;

            isVisible = origIsVisible = GrepSettings.Instance.Get<bool>(OptionKey);
        }

        public string Group { get; private set; }
        public string LabelKey { get; private set; }
        public string OptionKey { get; private set; }

        public string Label => TranslationSource.Instance[LabelKey].TrimEnd(':', '…');

        public bool IsChanged => IsVisible != origIsVisible;

        public void UpdateOption()
        {
            GrepSettings.Instance.Set(OptionKey, IsVisible);
            origIsVisible = IsVisible;
        }

        internal void UpdateLabel()
        {
            OnPropertyChanged(nameof(Label));
        }

        [ObservableProperty]
        private bool isVisible;
        private bool origIsVisible;
    }


    public class FontInfo
    {
        public FontInfo(string familyName)
        {
            FamilyName = familyName;
            IsMonospaced = GetIsMonospaced(familyName);
        }
        public string FamilyName { get; private set; }
        public bool IsMonospaced { get; private set; }

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
