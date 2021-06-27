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
using dnGREP.Common;
using dnGREP.Engines;
using Microsoft.Win32;
using NLog;

namespace dnGREP.WPF
{
    public class OptionsViewModel : ViewModelBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public OptionsViewModel()
        {
            LoadSettings();

            foreach (string name in AppTheme.Instance.ThemeNames)
                ThemeNames.Add(name);

            hasWindowsThemes = AppTheme.HasWindowsThemes;
            AppTheme.Instance.CurrentThemeChanged += (s, e) =>
            {
                CurrentTheme = AppTheme.Instance.CurrentThemeName;
            };
        }

        #region Private Variables and Properties
        private static readonly string SHELL_KEY_NAME = "dnGREP";
        private static readonly string SHELL_MENU_TEXT = "dnGREP...";
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
                FollowWindowsTheme != Settings.Get<bool>(GrepSettings.Key.FollowWindowsTheme) ||
                CurrentTheme != Settings.Get<string>(GrepSettings.Key.CurrentTheme) ||
                UseDefaultFont != Settings.Get<bool>(GrepSettings.Key.UseDefaultFont) ||
                EditApplicationFontFamily != Settings.Get<string>(GrepSettings.Key.ApplicationFontFamily) ||
                EditMainFormFontSize != Settings.Get<double>(GrepSettings.Key.MainFormFontSize) ||
                EditReplaceFormFontSize != Settings.Get<double>(GrepSettings.Key.ReplaceFormFontSize) ||
                EditDialogFontSize != Settings.Get<double>(GrepSettings.Key.DialogFontSize) ||
                PdfToTextOptions != Settings.Get<string>(GrepSettings.Key.PdfToTextOptions) ||
                ArchiveOptions.IsChanged ||
                IsChanged(Plugins)
                )
                    return true;
                else
                    return false;
            }
        }

        private bool IsChanged(IList<PluginOptions> plugins)
        {
            return plugins.Any(p => p.IsChanged);
        }

        private bool enableWindowsIntegration;
        public bool EnableWindowsIntegration
        {
            get { return enableWindowsIntegration; }
            set
            {
                if (value == enableWindowsIntegration)
                    return;

                enableWindowsIntegration = value;

                base.OnPropertyChanged(() => EnableWindowsIntegration);
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

                base.OnPropertyChanged(() => WindowsIntegrationTooltip);
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

                base.OnPropertyChanged(() => PanelTooltip);
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

                base.OnPropertyChanged(() => IsAdministrator);
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

                base.OnPropertyChanged(() => EnableCheckForUpdates);

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

                base.OnPropertyChanged("CheckForUpdatesInterval");
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

                base.OnPropertyChanged(() => EnableRunAtStartup);
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
                OnPropertyChanged("FollowWindowsTheme");

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
                OnPropertyChanged("HasWindowsThemes");
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
                OnPropertyChanged("CurrentTheme");

                AppTheme.Instance.CurrentThemeName = currentTheme;
            }
        }

        public ObservableCollection<string> ThemeNames { get; } = new ObservableCollection<string>();

        private string customEditorPath;
        public string CustomEditorPath
        {
            get { return customEditorPath; }
            set
            {
                if (value == customEditorPath)
                    return;

                customEditorPath = value;

                base.OnPropertyChanged(() => CustomEditorPath);
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

                base.OnPropertyChanged(() => CustomEditorArgs);
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

                base.OnPropertyChanged(() => CompareApplicationPath);
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

                base.OnPropertyChanged(() => CompareApplicationArgs);
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

                base.OnPropertyChanged(() => ShowFilePathInResults);
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

        private bool allowSearchWithEmptyPattern;
        public bool AllowSearchWithEmptyPattern
        {
            get { return allowSearchWithEmptyPattern; }
            set
            {
                if (value == allowSearchWithEmptyPattern)
                    return;

                allowSearchWithEmptyPattern = value;

                base.OnPropertyChanged(() => AllowSearchWithEmptyPattern);
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

                base.OnPropertyChanged(() => DetectEncodingForFileNamePattern);
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

                base.OnPropertyChanged(() => AutoExpandSearchTree);
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

                base.OnPropertyChanged(() => ShowVerboseMatchCount);
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

                base.OnPropertyChanged(() => ShowFileInfoTooltips);
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

                base.OnPropertyChanged(() => MatchTimeout);
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

                base.OnPropertyChanged(() => MatchThreshold);
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

                base.OnPropertyChanged(() => MaxPathBookmarks);
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

                base.OnPropertyChanged(() => MaxSearchBookmarks);
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

                base.OnPropertyChanged(() => MaxExtensionBookmarks);
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

                base.OnPropertyChanged(() => OptionsLocation);
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

        private PluginOptions archiveOptions;
        public PluginOptions ArchiveOptions
        {
            get { return archiveOptions; }
            set
            {
                archiveOptions = value;

                base.OnPropertyChanged(() => ArchiveOptions);
            }
        }

        public ObservableCollection<PluginOptions> Plugins { get; set; } = new ObservableCollection<PluginOptions>();

        public IList<string> FontFamilies
        {
            get { return Fonts.SystemFontFamilies.Select(r => r.Source).ToList(); }
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
                }

                base.OnPropertyChanged(() => UseDefaultFont);
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
                base.OnPropertyChanged(() => ApplicationFontFamily);
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
                base.OnPropertyChanged(() => EditApplicationFontFamily);
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
                base.OnPropertyChanged(() => MainFormFontSize);
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
                base.OnPropertyChanged(() => EditMainFormFontSize);
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
                base.OnPropertyChanged(() => ReplaceFormFontSize);
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
                base.OnPropertyChanged(() => EditReplaceFormFontSize);
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
                base.OnPropertyChanged(() => DialogFontSize);
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
                base.OnPropertyChanged(() => EditDialogFontSize);
            }
        }

        #endregion

        #region Presentation Properties

        private RelayCommand _saveCommand;
        /// <summary>
        /// Returns a command that saves the form
        /// </summary>
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(
                        param => Save(),
                        param => CanSave
                        );
                }
                return _saveCommand;
            }
        }

        private RelayCommand _browseEditiorCommand;
        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BrowseEditorCommand
        {
            get
            {
                if (_browseEditiorCommand == null)
                {
                    _browseEditiorCommand = new RelayCommand(
                        param => BrowseToEditor()
                        );
                }
                return _browseEditiorCommand;
            }
        }

        private RelayCommand _browseCompareCommand;
        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BrowseCompareCommand
        {
            get
            {
                if (_browseCompareCommand == null)
                {
                    _browseCompareCommand = new RelayCommand(
                        param => BrowseToCompareApp()
                        );
                }
                return _browseCompareCommand;
            }
        }

        private RelayCommand _clearSearchesCommand;
        /// <summary>
        /// Returns a command that clears old searches.
        /// </summary>
        public ICommand ClearSearchesCommand
        {
            get
            {
                if (_clearSearchesCommand == null)
                {
                    _clearSearchesCommand = new RelayCommand(
                        param => ClearSearches()
                        );
                }
                return _clearSearchesCommand;
            }
        }

        private RelayCommand _reloadThemeCommand;
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

        private RelayCommand _resetPdfToTextOptionCommand;
        public ICommand ResetPdfToTextOptionCommand
        {
            get
            {
                if (_resetPdfToTextOptionCommand == null)
                {
                    _resetPdfToTextOptionCommand = new RelayCommand(
                        param => PdfToTextOptions = "-layout -enc UTF-8 -bom"
                        );
                }
                return _resetPdfToTextOptionCommand;
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Saves the customer to the repository.  This method is invoked by the SaveCommand.
        /// </summary>
        public void Save()
        {
            SaveSettings();
        }

        #endregion // Public Methods

        #region Private Methods

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
                PanelTooltip = "To change this setting run dnGREP as Administrator.";
            }
            else
            {
                WindowsIntegrationTooltip = "Enables starting dnGrep from the Windows Explorer right-click context menu.";
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

            UseDefaultFont = Settings.Get<bool>(GrepSettings.Key.UseDefaultFont);
            ApplicationFontFamily = EditApplicationFontFamily =
                ValueOrDefault(GrepSettings.Key.ApplicationFontFamily, SystemFonts.MessageFontFamily.Source);
            MainFormFontSize = EditMainFormFontSize =
                ValueOrDefault(GrepSettings.Key.MainFormFontSize, SystemFonts.MessageFontSize);
            ReplaceFormFontSize = EditReplaceFormFontSize =
                ValueOrDefault(GrepSettings.Key.ReplaceFormFontSize, SystemFonts.MessageFontSize);
            DialogFontSize = EditDialogFontSize =
                ValueOrDefault(GrepSettings.Key.DialogFontSize, SystemFonts.MessageFontSize);

            // current values may not equal the saved settings value
            CurrentTheme = AppTheme.Instance.CurrentThemeName;
            FollowWindowsTheme = AppTheme.Instance.FollowWindowsTheme;

            PdfToTextOptions = Settings.Get<string>(GrepSettings.Key.PdfToTextOptions);

            {
                string nameKey = "Archive";
                string addKey = "Add" + nameKey + "Extensions";
                string remKey = "Rem" + nameKey + "Extensions";

                string addCsv = string.Empty;
                if (GrepSettings.Instance.ContainsKey(addKey))
                    addCsv = GrepSettings.Instance.Get<string>(addKey).Trim();

                string remCsv = string.Empty;
                if (GrepSettings.Instance.ContainsKey(remKey))
                    remCsv = GrepSettings.Instance.Get<string>(remKey).Trim();

                ArchiveOptions = new PluginOptions("Archive", true,
                    string.Join(", ", ArchiveDirectory.DefaultExtensions), addCsv, remCsv);
            }

            Plugins.Clear();
            foreach (var plugin in GrepEngineFactory.AllPlugins.OrderBy(p => p.Name))
            {
                string nameKey = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(plugin.Name);
                string enabledkey = nameKey + "Enabled";
                string addKey = "Add" + nameKey + "Extensions";
                string remKey = "Rem" + nameKey + "Extensions";

                bool isEnabled = true;
                if (GrepSettings.Instance.ContainsKey(enabledkey))
                    isEnabled = GrepSettings.Instance.Get<bool>(enabledkey);

                string addCsv = string.Empty;
                if (GrepSettings.Instance.ContainsKey(addKey))
                    addCsv = GrepSettings.Instance.Get<string>(addKey).Trim();

                string remCsv = string.Empty;
                if (GrepSettings.Instance.ContainsKey(remKey))
                    remCsv = GrepSettings.Instance.Get<string>(remKey).Trim();

                var pluginOptions = new PluginOptions(
                    plugin.Name, isEnabled, string.Join(", ", plugin.DefaultExtensions), addCsv, remCsv);
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
            Settings.Set(GrepSettings.Key.FollowWindowsTheme, FollowWindowsTheme);
            Settings.Set(GrepSettings.Key.CurrentTheme, CurrentTheme);
            Settings.Set(GrepSettings.Key.UseDefaultFont, UseDefaultFont);
            Settings.Set(GrepSettings.Key.ApplicationFontFamily, ApplicationFontFamily);
            Settings.Set(GrepSettings.Key.MainFormFontSize, MainFormFontSize);
            Settings.Set(GrepSettings.Key.ReplaceFormFontSize, ReplaceFormFontSize);
            Settings.Set(GrepSettings.Key.DialogFontSize, DialogFontSize);
            Settings.Set(GrepSettings.Key.PdfToTextOptions, PdfToTextOptions);

            if (ArchiveOptions.IsChanged)
            {
                string nameKey = "Archive";
                string addKey = "Add" + nameKey + "Extensions";
                string remKey = "Rem" + nameKey + "Extensions";

                Settings.Set(addKey, CleanExtensions(ArchiveOptions.AddExtensions));
                Settings.Set(remKey, CleanExtensions(ArchiveOptions.RemExtensions));

                ArchiveOptions.SetUnchanged();
                ArchiveDirectory.Reinitialize();
            }

            bool pluginsChanged = Plugins.Any(p => p.IsChanged);
            foreach (var plugin in Plugins)
            {
                string nameKey = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(plugin.Name);
                string enabledkey = nameKey + "Enabled";
                string addKey = "Add" + nameKey + "Extensions";
                string remKey = "Rem" + nameKey + "Extensions";

                Settings.Set(enabledkey, plugin.IsEnabled);
                Settings.Set(addKey, CleanExtensions(plugin.AddExtensions));
                Settings.Set(remKey, CleanExtensions(plugin.RemExtensions));

                plugin.SetUnchanged();
            }

            Settings.Save();

            if (pluginsChanged)
                GrepEngineFactory.ReloadPlugins();
        }

        private string CleanExtensions(string extensions)
        {
            if (string.IsNullOrWhiteSpace(extensions))
                return string.Empty;

            string[] split = extensions.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var cleaned = split.Select(s => s.TrimStart('.').Trim());
            return string.Join(", ", cleaned);
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
                    MessageBox.Show("Run dnGrep as Administrator to change showing dnGrep in Explorer right-click menu",
                        "dnGrep", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to register dnGrep with Explorer context menu");
                    MessageBox.Show("There was an error adding dnGrep to Explorer right-click menu. See the error log for details: " + App.LogDir,
                        "dnGrep", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Run dnGrep as Administrator to change showing dnGrep in Explorer right-click menu",
                    "dnGrep", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to remove dnGrep from Explorer context menu");
                MessageBox.Show("There was an error removing dnGrep from the Explorer right-click menu. See the error log for details: " + App.LogDir,
                    "dnGrep", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("Run dnGrep as Administrator to change Startup Register",
                        "dnGrep", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to register auto startup");
                    MessageBox.Show("There was an error registering auto startup. See the error log for details: " + App.LogDir,
                        "dnGrep", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("Run dnGrep as Administrator to change Startup Register",
                        "dnGrep", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to unregister auto startup");
                    MessageBox.Show("There was an error unregistering auto startup. See the error log for details: " + App.LogDir,
                        "dnGrep", MessageBoxButton.OK, MessageBoxImage.Error);
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

    public class PluginOptions : ViewModelBase
    {
        public PluginOptions(string name, bool enabled, string defExt, string addExt, string remExt)
        {
            Name = name;
            IsEnabled = origIsEnabled = enabled;
            DefaultExtensions = defExt;
            AddExtensions = origAddExtensions = addExt;
            RemExtensions = origRemExtensions = remExt;
        }

        public bool IsChanged => isEnabled != origIsEnabled || addExtensions != origAddExtensions || remExtensions != origRemExtensions;

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                if (value == name)
                    return;

                name = value;
                base.OnPropertyChanged(() => Name);
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

                base.OnPropertyChanged(() => IsEnabled);
            }
        }

        private string origAddExtensions = string.Empty;
        private string addExtensions = string.Empty;
        public string AddExtensions
        {
            get { return addExtensions; }
            set
            {
                if (value == addExtensions)
                    return;

                addExtensions = value;

                base.OnPropertyChanged(() => AddExtensions);
            }
        }

        private string origRemExtensions = string.Empty;
        private string remExtensions = string.Empty;
        public string RemExtensions
        {
            get { return remExtensions; }
            set
            {
                if (value == remExtensions)
                    return;

                remExtensions = value;

                base.OnPropertyChanged(() => RemExtensions);
            }
        }

        private string defaultExtensions = string.Empty;
        public string DefaultExtensions
        {
            get { return defaultExtensions; }
            set
            {
                if (value == defaultExtensions)
                    return;

                defaultExtensions = value;

                base.OnPropertyChanged(() => DefaultExtensions);
            }
        }

        internal void SetUnchanged()
        {
            origIsEnabled = isEnabled;
            origAddExtensions = addExtensions;
            origRemExtensions = remExtensions;
        }
    }
}
