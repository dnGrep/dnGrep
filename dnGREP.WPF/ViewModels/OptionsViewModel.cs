using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using dnGREP.Common;
using dnGREP.Engines;
using dnGREP.Localization;
using dnGREP.WPF.MVHelpers;
using Microsoft.Win32;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public class OptionsViewModel : CultureAwareViewModel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly string ellipsis = char.ConvertFromUtf32(0x2026);

        public OptionsViewModel()
        {
            TaskLimit = Environment.ProcessorCount * 4;

            LoadSettings();

            foreach (string name in AppTheme.Instance.ThemeNames)
                ThemeNames.Add(name);

            CultureNames = TranslationSource.Instance.AppCultures
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
                CustomEditorHelp = TranslationSource.Format(Resources.Options_CustomEditorHelp,
                    File, Line, Pattern, Match, Column);
                PanelTooltip = IsAdministrator ? string.Empty : Resources.Options_ToChangeThisSettingRunDnGREPAsAdministrator;
                WindowsIntegrationTooltip = IsAdministrator ? Resources.Options_EnablesStartingDnGrepFromTheWindowsExplorerRightClickContextMenu : string.Empty;

                foreach (var item in VisibilityOptions)
                {
                    item.UpdateLabel();
                }
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
        private const string Line = "%line";
        private const string Pattern = "%pattern";
        private const string Match = "%match";
        private const string Column = "%column";
        private const string ArchiveNameKey = "Archive";
        private const string Enabledkey = "Enabled";
        private const string PreviewTextKey = "PreviewText";
        private GrepSettings Settings
        {
            get { return GrepSettings.Instance; }
        }
        #endregion

        #region Properties
        public bool CanSave
        {
            get
            {
                if (EnableWindowsIntegration != IsShellRegistered("Directory") ||
                EnableRunAtStartup != IsStartupRegistered() ||
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

        private bool IsChanged(IList<PluginOptions> plugins)
        {
            return plugins.Any(p => p.IsChanged);
        }

        private bool IsChanged(IList<VisibilityOption> visibilityOptions)
        {
            return visibilityOptions.Any(p => p.IsChanged);
        }

        public ObservableCollection<VisibilityOption> VisibilityOptions { get; } = new ObservableCollection<VisibilityOption>();


        private bool enableWindowsIntegration;
        public bool EnableWindowsIntegration
        {
            get { return enableWindowsIntegration; }
            set
            {
                if (value == enableWindowsIntegration)
                    return;

                enableWindowsIntegration = value;
                base.OnPropertyChanged(nameof(EnableWindowsIntegration));
            }
        }

        private string windowsIntegrationTooltip;
        public string WindowsIntegrationTooltip
        {
            get { return windowsIntegrationTooltip; }
            set
            {
                if (value == windowsIntegrationTooltip)
                    return;

                windowsIntegrationTooltip = value;
                base.OnPropertyChanged(nameof(WindowsIntegrationTooltip));
            }
        }

        private string panelTooltip;
        public string PanelTooltip
        {
            get { return panelTooltip; }
            set
            {
                if (value == panelTooltip)
                    return;

                panelTooltip = value;
                base.OnPropertyChanged(nameof(PanelTooltip));
            }
        }

        private bool isAdministrator;
        public bool IsAdministrator
        {
            get { return isAdministrator; }
            set
            {
                if (value == isAdministrator)
                    return;

                isAdministrator = value;
                base.OnPropertyChanged(nameof(IsAdministrator));
            }
        }

        private bool enableCheckForUpdates;
        public bool EnableCheckForUpdates
        {
            get { return enableCheckForUpdates; }
            set
            {
                if (value == enableCheckForUpdates)
                    return;

                enableCheckForUpdates = value;
                base.OnPropertyChanged(nameof(EnableCheckForUpdates));

                if (!enableCheckForUpdates)
                    EnableRunAtStartup = false;
            }
        }

        private int checkForUpdatesInterval;
        public int CheckForUpdatesInterval
        {
            get { return checkForUpdatesInterval; }
            set
            {
                if (value == checkForUpdatesInterval)
                    return;

                checkForUpdatesInterval = value;
                base.OnPropertyChanged(nameof(CheckForUpdatesInterval));
            }
        }

        private bool enableRunAtStartup;
        public bool EnableRunAtStartup
        {
            get { return enableRunAtStartup; }
            set
            {
                if (value == enableRunAtStartup)
                    return;

                enableRunAtStartup = value;
                base.OnPropertyChanged(nameof(EnableRunAtStartup));
            }
        }

        private bool followWindowsTheme = true;
        public bool FollowWindowsTheme
        {
            get { return followWindowsTheme; }
            set
            {
                if (followWindowsTheme == value)
                    return;

                followWindowsTheme = value;
                OnPropertyChanged(nameof(FollowWindowsTheme));

                AppTheme.Instance.FollowWindowsThemeChanged(followWindowsTheme, CurrentTheme);

                CurrentTheme = AppTheme.Instance.CurrentThemeName;
            }
        }


        private bool hasWindowsThemes = true;
        public bool HasWindowsThemes
        {
            get { return hasWindowsThemes; }
            set
            {
                if (hasWindowsThemes == value)
                    return;

                hasWindowsThemes = value;
                OnPropertyChanged(nameof(HasWindowsThemes));
            }
        }

        private string currentTheme = "Light";
        public string CurrentTheme
        {
            get { return currentTheme; }
            set
            {
                if (currentTheme == value)
                    return;

                currentTheme = value;
                OnPropertyChanged(nameof(CurrentTheme));

                AppTheme.Instance.CurrentThemeName = currentTheme;
            }
        }

        public ObservableCollection<string> ThemeNames { get; } = new ObservableCollection<string>();

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

        public KeyValuePair<string, string>[] CultureNames { get; }

        public KeyValuePair<string, ConfigurationTemplate>[] CustomEditorTemplates { get; }

        private void ApplyCustomEditorTemplate(ConfigurationTemplate template)
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

        private ConfigurationTemplate customEditorTemplate = null;

        public ConfigurationTemplate CustomEditorTemplate
        {
            get { return customEditorTemplate; }
            set
            {
                if (value == customEditorTemplate)
                    return;

                customEditorTemplate = value;
                ApplyCustomEditorTemplate(customEditorTemplate);

                base.OnPropertyChanged(nameof(CustomEditorTemplate));
            }
        }

        private string customEditorPath;
        public string CustomEditorPath
        {
            get { return customEditorPath; }
            set
            {
                if (value == customEditorPath)
                    return;

                customEditorPath = value;
                base.OnPropertyChanged(nameof(CustomEditorPath));
            }
        }

        private string customEditorArgs;
        public string CustomEditorArgs
        {
            get { return customEditorArgs; }
            set
            {
                if (value == customEditorArgs)
                    return;

                customEditorArgs = value;
                base.OnPropertyChanged(nameof(CustomEditorArgs));
            }
        }

        private string customEditorHelp;
        public string CustomEditorHelp
        {
            get { return customEditorHelp; }
            set
            {
                if (value == customEditorHelp)
                    return;

                customEditorHelp = value;
                base.OnPropertyChanged(nameof(CustomEditorHelp));
            }
        }

        public KeyValuePair<string, ConfigurationTemplate>[] CompareApplicationTemplates { get; }

        private void ApplyCompareApplicationTemplate(ConfigurationTemplate template)
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

        private ConfigurationTemplate compareApplicationTemplate = null;

        public ConfigurationTemplate CompareApplicationTemplate
        {
            get { return compareApplicationTemplate; }
            set
            {
                if (value == compareApplicationTemplate)
                    return;

                compareApplicationTemplate = value;
                ApplyCompareApplicationTemplate(compareApplicationTemplate);
                base.OnPropertyChanged(nameof(CompareApplicationTemplate));
            }
        }

        private string compareApplicationPath;
        public string CompareApplicationPath
        {
            get { return compareApplicationPath; }
            set
            {
                if (value == compareApplicationPath)
                    return;

                compareApplicationPath = value;
                base.OnPropertyChanged(nameof(CompareApplicationPath));
            }
        }

        private string compareApplicationArgs;
        public string CompareApplicationArgs
        {
            get { return compareApplicationArgs; }
            set
            {
                if (value == compareApplicationArgs)
                    return;

                compareApplicationArgs = value;
                base.OnPropertyChanged(nameof(CompareApplicationArgs));
            }
        }

        private bool showFilePathInResults;
        public bool ShowFilePathInResults
        {
            get { return showFilePathInResults; }
            set
            {
                if (value == showFilePathInResults)
                    return;

                showFilePathInResults = value;
                base.OnPropertyChanged(nameof(ShowFilePathInResults));
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

        private bool allowSearchWithEmptyPattern;
        public bool AllowSearchWithEmptyPattern
        {
            get { return allowSearchWithEmptyPattern; }
            set
            {
                if (value == allowSearchWithEmptyPattern)
                    return;

                allowSearchWithEmptyPattern = value;
                base.OnPropertyChanged(nameof(AllowSearchWithEmptyPattern));
            }
        }

        private bool detectEncodingForFileNamePattern;
        public bool DetectEncodingForFileNamePattern
        {
            get { return detectEncodingForFileNamePattern; }
            set
            {
                if (value == detectEncodingForFileNamePattern)
                    return;

                detectEncodingForFileNamePattern = value;
                base.OnPropertyChanged(nameof(DetectEncodingForFileNamePattern));
            }
        }

        private bool autoExpandSearchTree;
        public bool AutoExpandSearchTree
        {
            get { return autoExpandSearchTree; }
            set
            {
                if (value == autoExpandSearchTree)
                    return;

                autoExpandSearchTree = value;
                base.OnPropertyChanged(nameof(AutoExpandSearchTree));
            }
        }

        private bool showVerboseMatchCount;
        public bool ShowVerboseMatchCount
        {
            get { return showVerboseMatchCount; }
            set
            {
                if (value == showVerboseMatchCount)
                    return;

                showVerboseMatchCount = value;
                base.OnPropertyChanged(nameof(ShowVerboseMatchCount));
            }
        }

        private bool showFileInfoTooltips;
        public bool ShowFileInfoTooltips
        {
            get { return showFileInfoTooltips; }
            set
            {
                if (value == showFileInfoTooltips)
                    return;

                showFileInfoTooltips = value;
                base.OnPropertyChanged(nameof(ShowFileInfoTooltips));
            }
        }

        private double matchTimeout;
        public double MatchTimeout
        {
            get { return matchTimeout; }
            set
            {
                if (value == matchTimeout)
                    return;

                matchTimeout = value;
                base.OnPropertyChanged(nameof(MatchTimeout));
            }
        }

        private double matchThreshold;
        public double MatchThreshold
        {
            get { return matchThreshold; }
            set
            {
                if (value == matchThreshold)
                    return;

                matchThreshold = value;
                base.OnPropertyChanged(nameof(MatchThreshold));
            }
        }

        private int maxPathBookmarks;
        public int MaxPathBookmarks
        {
            get { return maxPathBookmarks; }
            set
            {
                if (value == maxPathBookmarks)
                    return;

                maxPathBookmarks = value;
                base.OnPropertyChanged(nameof(MaxPathBookmarks));
            }
        }

        private int maxSearchBookmarks;
        public int MaxSearchBookmarks
        {
            get { return maxSearchBookmarks; }
            set
            {
                if (value == maxSearchBookmarks)
                    return;

                maxSearchBookmarks = value;
                base.OnPropertyChanged(nameof(MaxSearchBookmarks));
            }
        }

        private int maxExtensionBookmarks;
        public int MaxExtensionBookmarks
        {
            get { return maxExtensionBookmarks; }
            set
            {
                if (value == maxExtensionBookmarks)
                    return;

                maxExtensionBookmarks = value;
                base.OnPropertyChanged(nameof(MaxExtensionBookmarks));
            }
        }

        public enum PanelSelection { MainPanel = 0, OptionsExpander }

        private PanelSelection optionsLocation;
        public PanelSelection OptionsLocation
        {
            get { return optionsLocation; }
            set
            {
                if (value == optionsLocation)
                    return;

                optionsLocation = value;
                base.OnPropertyChanged(nameof(OptionsLocation));
            }
        }

        private bool maximizeResultsTreeOnSearch;
        public bool MaximizeResultsTreeOnSearch
        {
            get { return maximizeResultsTreeOnSearch; }
            set
            {
                if (maximizeResultsTreeOnSearch == value)
                {
                    return;
                }

                maximizeResultsTreeOnSearch = value;
                OnPropertyChanged(nameof(MaximizeResultsTreeOnSearch));
            }
        }


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

        private int taskLimit = 1;
        public int TaskLimit
        {
            get { return taskLimit; }
            set
            {
                if (taskLimit == value)
                {
                    return;
                }

                taskLimit = value;
                OnPropertyChanged(nameof(TaskLimit));
            }
        }

        private int parallelismCount = 1;
        public int ParallelismCount
        {
            get { return parallelismCount; }
            set
            {
                if (parallelismCount == value)
                {
                    return;
                }

                parallelismCount = value;
                OnPropertyChanged(nameof(ParallelismCount));
            }
        }


        private bool parallelismUnlimited = true;
        public bool ParallelismUnlimited
        {
            get { return parallelismUnlimited; }
            set
            {
                if (parallelismUnlimited == value)
                {
                    return;
                }

                parallelismUnlimited = value;
                OnPropertyChanged(nameof(ParallelismUnlimited));
            }
        }


        public enum ReplaceDialogConfiguration { FullDialog = 0, FilesOnly }

        private ReplaceDialogConfiguration replaceDialogLayout;
        public ReplaceDialogConfiguration ReplaceDialogLayout
        {
            get { return replaceDialogLayout; }
            set
            {
                if (replaceDialogLayout == value)
                    return;

                replaceDialogLayout = value;
                OnPropertyChanged(nameof(ReplaceDialogLayout));
            }
        }

        public enum DeleteFilesDestination { Recycle, Permanent }


        private DeleteFilesDestination deleteOption = DeleteFilesDestination.Recycle;
        public DeleteFilesDestination DeleteOption
        {
            get { return deleteOption; }
            set
            {
                if (deleteOption == value)
                {
                    return;
                }

                deleteOption = value;
                OnPropertyChanged(nameof(DeleteOption));
            }
        }


        private OverwriteFile copyOverwriteFileOption = OverwriteFile.Prompt;
        public OverwriteFile CopyOverwriteFileOption
        {
            get { return copyOverwriteFileOption; }
            set
            {
                if (copyOverwriteFileOption == value)
                {
                    return;
                }

                copyOverwriteFileOption = value;
                OnPropertyChanged(nameof(CopyOverwriteFileOption));
            }
        }


        private OverwriteFile moveOverwriteFileOption = OverwriteFile.Prompt;
        public OverwriteFile MoveOverwriteFileOption
        {
            get { return moveOverwriteFileOption; }
            set
            {
                if (moveOverwriteFileOption == value)
                {
                    return;
                }

                moveOverwriteFileOption = value;
                OnPropertyChanged(nameof(MoveOverwriteFileOption));
            }
        }

        public List<int> HexLengthOptions { get; }

        private int hexResultByteLength = 16;
        public int HexResultByteLength
        {
            get { return hexResultByteLength; }
            set
            {
                if (hexResultByteLength == value)
                    return;

                hexResultByteLength = value;
                OnPropertyChanged(nameof(HexResultByteLength));
            }
        }

        private string pdfToTextOptions = string.Empty;
        public string PdfToTextOptions
        {
            get { return pdfToTextOptions; }
            set
            {
                if (pdfToTextOptions == value)
                    return;

                pdfToTextOptions = value;
                OnPropertyChanged(nameof(PdfToTextOptions));
            }
        }

        private PdfNumberType pdfNumberStyle = PdfNumberType.PageNumber;
        public PdfNumberType PdfNumberStyle
        {
            get { return pdfNumberStyle; }
            set
            {
                if (pdfNumberStyle == value)
                {
                    return;
                }

                pdfNumberStyle = value;
                OnPropertyChanged(nameof(PdfNumberStyle));
            }
        }

        private PluginOptions archiveOptions;
        public PluginOptions ArchiveOptions
        {
            get { return archiveOptions; }
            set
            {
                if (archiveOptions == value)
                    return;

                archiveOptions = value;
                base.OnPropertyChanged(nameof(ArchiveOptions));
            }
        }

        public ObservableCollection<PluginOptions> Plugins { get; } = new ObservableCollection<PluginOptions>();

        public IList<FontInfo> FontFamilies
        {
            get { return Fonts.SystemFontFamilies.Select(r => new FontInfo(r.Source)).ToList(); }
        }

        private bool useDefaultFont = true;
        public bool UseDefaultFont
        {
            get { return useDefaultFont; }
            set
            {
                if (useDefaultFont == value)
                    return;

                useDefaultFont = value;

                if (useDefaultFont)
                {
                    EditApplicationFontFamily = SystemFonts.MessageFontFamily.Source;
                    EditMainFormFontSize = SystemFonts.MessageFontSize;
                    EditReplaceFormFontSize = SystemFonts.MessageFontSize;
                    EditDialogFontSize = SystemFonts.MessageFontSize;
                    EditResultsFontFamily = GrepSettings.DefaultMonospaceFontFamily;
                    EditResultsFontSize = SystemFonts.MessageFontSize;
                }

                base.OnPropertyChanged(nameof(UseDefaultFont));
            }
        }

        private string applicationFontFamily;
        public string ApplicationFontFamily
        {
            get { return applicationFontFamily; }
            set
            {
                if (applicationFontFamily == value)
                    return;

                applicationFontFamily = value;
                base.OnPropertyChanged(nameof(ApplicationFontFamily));
            }
        }

        private string editApplicationFontFamily;
        public string EditApplicationFontFamily
        {
            get { return editApplicationFontFamily; }
            set
            {
                if (editApplicationFontFamily == value)
                    return;

                editApplicationFontFamily = value;
                base.OnPropertyChanged(nameof(EditApplicationFontFamily));
            }
        }

        private double mainFormFontSize;
        public double MainFormFontSize
        {
            get { return mainFormFontSize; }
            set
            {
                if (mainFormFontSize == value)
                    return;

                mainFormFontSize = value;
                base.OnPropertyChanged(nameof(MainFormFontSize));
            }
        }

        private double editMainFormFontSize;
        public double EditMainFormFontSize
        {
            get { return editMainFormFontSize; }
            set
            {
                if (editMainFormFontSize == value)
                    return;

                editMainFormFontSize = value;
                base.OnPropertyChanged(nameof(EditMainFormFontSize));
            }
        }

        private double replaceFormFontSize;
        public double ReplaceFormFontSize
        {
            get { return replaceFormFontSize; }
            set
            {
                if (replaceFormFontSize == value)
                    return;

                replaceFormFontSize = value;
                base.OnPropertyChanged(nameof(ReplaceFormFontSize));
            }
        }

        private double editReplaceFormFontSize;
        public double EditReplaceFormFontSize
        {
            get { return editReplaceFormFontSize; }
            set
            {
                if (editReplaceFormFontSize == value)
                    return;

                editReplaceFormFontSize = value;
                base.OnPropertyChanged(nameof(EditReplaceFormFontSize));
            }
        }

        private double dialogFontSize;
        public double DialogFontSize
        {
            get { return dialogFontSize; }
            set
            {
                if (dialogFontSize == value)
                    return;

                dialogFontSize = value;
                base.OnPropertyChanged(nameof(DialogFontSize));
            }
        }

        private double editDialogFontSize;
        public double EditDialogFontSize
        {
            get { return editDialogFontSize; }
            set
            {
                if (editDialogFontSize == value)
                    return;

                editDialogFontSize = value;
                base.OnPropertyChanged(nameof(EditDialogFontSize));
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

        private string editResultsFontFamily;
        public string EditResultsFontFamily
        {
            get { return editResultsFontFamily; }
            set
            {
                if (editResultsFontFamily == value)
                    return;

                editResultsFontFamily = value;
                base.OnPropertyChanged(nameof(EditResultsFontFamily));
            }
        }

        private double resultsFontSize;
        public double ResultsFontSize
        {
            get { return resultsFontSize; }
            set
            {
                if (resultsFontSize == value)
                    return;

                resultsFontSize = value;
                base.OnPropertyChanged(nameof(ResultsFontSize));
            }
        }

        private double editResultsFontSize;
        public double EditResultsFontSize
        {
            get { return editResultsFontSize; }
            set
            {
                if (editResultsFontSize == value)
                    return;

                editResultsFontSize = value;
                base.OnPropertyChanged(nameof(EditResultsFontSize));
            }
        }

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
            q => !PdfToTextOptions.Equals(defaultPdfToText));

        private const string defaultPdfToText = "-layout -enc UTF-8 -bom";

        #endregion

        #region Public Methods

        /// <summary>
        /// Saves the settings to file.  This method is invoked by the SaveCommand.
        /// </summary>
        public void Save()
        {
            SaveSettings();
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

        public void ClearSearches()
        {
            Settings.Set(GrepSettings.Key.FastFileMatchBookmarks, new List<string>());
            Settings.Set(GrepSettings.Key.FastFileNotMatchBookmarks, new List<string>());
            Settings.Set(GrepSettings.Key.FastPathBookmarks, new List<string>());
            Settings.Set(GrepSettings.Key.FastReplaceBookmarks, new List<string>());
            Settings.Set(GrepSettings.Key.FastSearchBookmarks, new List<string>());
        }

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

            CustomEditorHelp = TranslationSource.Format(Resources.Options_CustomEditorHelp,
                File, Line, Pattern, Match, Column);

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

                ArchiveOptions = new PluginOptions(ArchiveNameKey, true, false,
                    extensionList, string.Join(", ", ArchiveDirectory.DefaultExtensions));
            }

            Plugins.Clear();
            foreach (var plugin in GrepEngineFactory.AllPlugins.OrderBy(p => p.Name))
            {
                string nameKey = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(plugin.Name);
                string enabledkey = nameKey + Enabledkey;
                string previewTextKey = nameKey + PreviewTextKey;
                string extensionList = string.Join(", ", Settings.GetExtensionList(nameKey,
                    plugin.DefaultExtensions));

                bool isEnabled = true;
                if (GrepSettings.Instance.ContainsKey(enabledkey))
                    isEnabled = GrepSettings.Instance.Get<bool>(enabledkey);

                bool previewTextEnabled = true;
                if (GrepSettings.Instance.ContainsKey(previewTextKey))
                    previewTextEnabled = GrepSettings.Instance.Get<bool>(previewTextKey);

                var pluginOptions = new PluginOptions(
                    plugin.Name, isEnabled, previewTextEnabled,
                    extensionList, string.Join(", ", plugin.DefaultExtensions));

                Plugins.Add(pluginOptions);
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
                    if (location == "here")
                    {
                        string regPath = $@"SOFTWARE\Classes\Directory\Background\shell\{SHELL_KEY_NAME}";

                        // add context menu to the registry
                        using (RegistryKey key = Registry.LocalMachine.CreateSubKey(regPath))
                        {
                            key.SetValue(null, SHELL_MENU_TEXT);
                            key.SetValue("Icon", Assembly.GetAssembly(typeof(OptionsView)).Location);
                        }

                        // add command that is invoked to the registry
                        string menuCommand = string.Format("\"{0}\" \"%V\"",
                                               Assembly.GetAssembly(typeof(OptionsView)).Location);
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
                            key.SetValue("Icon", Assembly.GetAssembly(typeof(OptionsView)).Location);
                        }

                        // add command that is invoked to the registry
                        string menuCommand = string.Format("\"{0}\" \"%1\"",
                                               Assembly.GetAssembly(typeof(OptionsView)).Location);
                        using (RegistryKey key = Registry.ClassesRoot.CreateSubKey($@"{regPath}\command"))
                        {
                            key.SetValue(null, menuCommand);
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

        private bool IsStartupRegistered()
        {
            string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath))
                {
                    return key.GetValue(SHELL_KEY_NAME) != null;
                }
            }
            catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private void StartupRegister()
        {
            if (!IsStartupRegistered())
            {
                try
                {
                    string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath, true))
                    {
                        key.SetValue(SHELL_KEY_NAME, string.Format("\"{0}\" /warmUp", Assembly.GetAssembly(typeof(OptionsView)).Location), RegistryValueKind.ExpandString);
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

        private void StartupUnregister()
        {
            if (IsStartupRegistered())
            {
                try
                {
                    string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath, true))
                    {
                        key.DeleteValue(SHELL_KEY_NAME);
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
                using (WindowsIdentity wi = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal wp = new WindowsPrincipal(wi);

                    IsAdministrator = wp.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                IsAdministrator = false;
            }
        }

        #endregion
    }

    public class PluginOptions : CultureAwareViewModel
    {
        public PluginOptions(string name, bool enabled, bool previewTextEnabled,
            string extensions, string defaultExtensions)
        {
            Name = name;
            IsEnabled = origIsEnabled = enabled;
            PreviewTextEnabled = origPreviewTextEnabled = previewTextEnabled;
            MappedExtensions = origMappedExtensions = extensions;
            DefaultExtensions = defaultExtensions ?? string.Empty;
        }

        public bool IsChanged => isEnabled != origIsEnabled ||
            previewTextEnabled != origPreviewTextEnabled ||
            mappedExtensions != origMappedExtensions;

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                if (value == name)
                    return;

                name = value;
                base.OnPropertyChanged(nameof(Name));
            }
        }

        private bool origIsEnabled;
        private bool isEnabled;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (value == isEnabled)
                    return;

                isEnabled = value;
                base.OnPropertyChanged(nameof(IsEnabled));
            }
        }


        private bool origPreviewTextEnabled;
        private bool previewTextEnabled;
        public bool PreviewTextEnabled
        {
            get { return previewTextEnabled; }
            set
            {
                if (previewTextEnabled == value)
                {
                    return;
                }

                previewTextEnabled = value;
                OnPropertyChanged(nameof(PreviewTextEnabled));
            }
        }

        private string origMappedExtensions = string.Empty;
        private string mappedExtensions = string.Empty;
        public string MappedExtensions
        {
            get { return mappedExtensions; }
            set
            {
                if (value == mappedExtensions)
                    return;

                mappedExtensions = value;
                base.OnPropertyChanged(nameof(MappedExtensions));
            }
        }

        public string DefaultExtensions { get; private set; }

        public ICommand ResetExtensions => new RelayCommand(
            p => MappedExtensions = DefaultExtensions,
            q => !MappedExtensions.Equals(DefaultExtensions));


        internal void SetUnchanged()
        {
            origIsEnabled = isEnabled;
            origPreviewTextEnabled = previewTextEnabled;
            origMappedExtensions = mappedExtensions;
        }
    }

    public class VisibilityOption : CultureAwareViewModel
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

        public bool IsChanged => isVisible != origIsVisible;

        public void UpdateOption()
        {
            GrepSettings.Instance.Set(OptionKey, isVisible);
            origIsVisible = isVisible;
        }

        internal void UpdateLabel()
        {
            OnPropertyChanged(nameof(Label));
        }

        private bool origIsVisible;
        private bool isVisible;
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible == value)
                {
                    return;
                }

                isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
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
            Typeface typeface = new Typeface(new FontFamily(familyName), SystemFonts.MessageFontStyle,
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
