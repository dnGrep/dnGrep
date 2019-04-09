using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Input;
using dnGREP.Common;
using Microsoft.Win32;

namespace dnGREP.WPF
{
    public class OptionsViewModel : ViewModelBase, IDataErrorInfo
    {
        public OptionsViewModel()
        {
            LoadSetting();
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
                EnableStartupAcceleration != IsStartupRegistered() ||
                EnableCheckForUpdates != Settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking) ||
                CheckForUpdatesInterval != Settings.Get<int>(GrepSettings.Key.UpdateCheckInterval) ||
                ShowLinesInContext != Settings.Get<bool>(GrepSettings.Key.ShowLinesInContext) ||
                ContextLinesBefore != Settings.Get<int>(GrepSettings.Key.ContextLinesBefore) ||
                ContextLinesAfter != Settings.Get<int>(GrepSettings.Key.ContextLinesAfter) ||
                CustomEditorPath != Settings.Get<string>(GrepSettings.Key.CustomEditor) ||
                CustomEditorArgs != Settings.Get<string>(GrepSettings.Key.CustomEditorArgs) ||
                ShowFilePathInResults != Settings.Get<bool>(GrepSettings.Key.ShowFilePathInResults) ||
                AllowSearchWithEmptyPattern != Settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern) ||
                AutoExpandSearchTree != Settings.Get<bool>(GrepSettings.Key.ExpandResults) ||
                ShowVerboseMatchCount != Settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount) ||
                MatchTimeout != Settings.Get<double>(GrepSettings.Key.MatchTimeout) ||
                MatchThreshold != Settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold) ||
                MaxSearchBookmarks != Settings.Get<int>(GrepSettings.Key.MaxSearchBookmarks) ||
                MaxPathBookmarks != Settings.Get<int>(GrepSettings.Key.MaxPathBookmarks) ||
                MaxExtensionBookmarks != Settings.Get<int>(GrepSettings.Key.MaxExtensionBookmarks) ||
                OptionsLocation != (Settings.Get<bool>(GrepSettings.Key.OptionsOnMainPanel) ?
                    PanelSelection.MainPanel : PanelSelection.OptionsExpander))
                    return true;
                else
                    return false;
            }
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

        private bool enableStartupAcceleration;
        public bool EnableStartupAcceleration
        {
            get { return enableStartupAcceleration; }
            set
            {
                if (value == enableStartupAcceleration)
                    return;

                enableStartupAcceleration = value;

                base.OnPropertyChanged(() => EnableStartupAcceleration);
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

        private string startupAccelerationTooltip;
        public string StartupAccelerationTooltip
        {
            get { return startupAccelerationTooltip; }
            set
            {
                if (value == startupAccelerationTooltip)
                    return;

                startupAccelerationTooltip = value;

                base.OnPropertyChanged(() => StartupAccelerationTooltip);
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

                base.OnPropertyChanged(() => CheckForUpdatesInterval);
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


        #endregion

        #region Presentation Properties

        RelayCommand _saveCommand;
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
                        param => Browse()
                        );
                }
                return _browseCommand;
            }
        }

        RelayCommand _clearSearchesCommand;
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

        public void Browse()
        {
            var dlg = new OpenFileDialog();
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                CustomEditorPath = dlg.FileName;
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

        private void LoadSetting()
        {
            CheckIfAdmin();
            if (!IsAdministrator)
            {
                WindowsIntegrationTooltip = "To set shell integration run dnGREP with elevated priveleges.";
                StartupAccelerationTooltip = "To enable startup acceleration run dnGREP with elevated priveleges.";
                PanelTooltip = "To change shell integration and startup acceleration options run dnGREP with elevated priveleges.";
            }
            else
            {
                WindowsIntegrationTooltip = "Shell integration enables running an application from shell context menu.";
                StartupAccelerationTooltip = "Startup acceleration loads application libraries on machine start to improve application startup time.";
                PanelTooltip = "Shell integration enables running an application from shell context menu.";
            }
            EnableWindowsIntegration = IsShellRegistered("Directory");
            EnableStartupAcceleration = IsStartupRegistered();
            EnableCheckForUpdates = Settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking);
            CheckForUpdatesInterval = Settings.Get<int>(GrepSettings.Key.UpdateCheckInterval);
            CustomEditorPath = Settings.Get<string>(GrepSettings.Key.CustomEditor);
            CustomEditorArgs = Settings.Get<string>(GrepSettings.Key.CustomEditorArgs);
            ShowFilePathInResults = Settings.Get<bool>(GrepSettings.Key.ShowFilePathInResults);
            AllowSearchWithEmptyPattern = Settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern);
            AutoExpandSearchTree = Settings.Get<bool>(GrepSettings.Key.ExpandResults);
            showVerboseMatchCount = Settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount);
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

            if (EnableStartupAcceleration)
            {
                StartupRegister();
            }
            else
            {
                StartupUnregister();
            }

            Settings.Set(GrepSettings.Key.EnableUpdateChecking, EnableCheckForUpdates);
            Settings.Set(GrepSettings.Key.UpdateCheckInterval, CheckForUpdatesInterval);
            Settings.Set(GrepSettings.Key.CustomEditor, CustomEditorPath);
            Settings.Set(GrepSettings.Key.CustomEditorArgs, CustomEditorArgs);
            Settings.Set(GrepSettings.Key.ShowFilePathInResults, ShowFilePathInResults);
            Settings.Set(GrepSettings.Key.AllowSearchingForFileNamePattern, AllowSearchWithEmptyPattern);
            Settings.Set(GrepSettings.Key.ExpandResults, AutoExpandSearchTree);
            Settings.Set(GrepSettings.Key.ShowVerboseMatchCount, showVerboseMatchCount);
            Settings.Set(GrepSettings.Key.MatchTimeout, MatchTimeout);
            Settings.Set(GrepSettings.Key.FuzzyMatchThreshold, MatchThreshold);
            Settings.Set(GrepSettings.Key.ShowLinesInContext, ShowLinesInContext);
            Settings.Set(GrepSettings.Key.ContextLinesBefore, ContextLinesBefore);
            Settings.Set(GrepSettings.Key.ContextLinesAfter, ContextLinesAfter);
            Settings.Set(GrepSettings.Key.MaxSearchBookmarks, MaxSearchBookmarks);
            Settings.Set(GrepSettings.Key.MaxPathBookmarks, MaxPathBookmarks);
            Settings.Set(GrepSettings.Key.MaxExtensionBookmarks, MaxExtensionBookmarks);
            Settings.Set(GrepSettings.Key.OptionsOnMainPanel, OptionsLocation == PanelSelection.MainPanel);
            Settings.Save();
        }

        private bool IsShellRegistered(string location)
        {
            if (!isAdministrator)
                return false;

            if (location == "here")
            {
                string regPath = string.Format(@"SOFTWARE\Classes\Directory\Background\shell\{0}",
                                           SHELL_KEY_NAME);
                try
                {
                    return Registry.LocalMachine.OpenSubKey(regPath) != null;
                }
                catch (UnauthorizedAccessException)
                {
                    isAdministrator = false;
                    return false;
                }
            }
            else
            {
                string regPath = string.Format(@"{0}\shell\{1}",
                                           location, SHELL_KEY_NAME);
                try
                {
                    return Registry.ClassesRoot.OpenSubKey(regPath) != null;
                }
                catch (UnauthorizedAccessException)
                {
                    isAdministrator = false;
                    return false;
                }
            }
        }

        private void ShellRegister(string location)
        {
            if (!isAdministrator)
                return;

            if (!IsShellRegistered(location))
            {

                if (location == "here")
                {
                    string regPath = string.Format(@"SOFTWARE\Classes\Directory\Background\shell\{0}", SHELL_KEY_NAME);

                    // add context menu to the registry
                    using (RegistryKey key =
                           Registry.LocalMachine.CreateSubKey(regPath))
                    {
                        key.SetValue(null, SHELL_MENU_TEXT);
                        key.SetValue("Icon", Assembly.GetAssembly(typeof(OptionsView)).Location);
                    }

                    // add command that is invoked to the registry
                    string menuCommand = string.Format("\"{0}\" \"%V\"",
                                           Assembly.GetAssembly(typeof(OptionsView)).Location);
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(
                        string.Format(@"{0}\command", regPath)))
                    {
                        key.SetValue(null, menuCommand);
                    }
                }
                else
                {
                    string regPath = string.Format(@"{0}\shell\{1}", location, SHELL_KEY_NAME);

                    // add context menu to the registry
                    using (RegistryKey key =
                           Registry.ClassesRoot.CreateSubKey(regPath))
                    {
                        key.SetValue(null, SHELL_MENU_TEXT);
                        key.SetValue("Icon", Assembly.GetAssembly(typeof(OptionsView)).Location);
                    }

                    // add command that is invoked to the registry
                    string menuCommand = string.Format("\"{0}\" \"%1\"",
                                           Assembly.GetAssembly(typeof(OptionsView)).Location);
                    using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(
                        string.Format(@"{0}\command", regPath)))
                    {
                        key.SetValue(null, menuCommand);
                    }
                }
            }
        }

        private void ShellUnregister(string location)
        {
            if (!isAdministrator)
                return;

            if (IsShellRegistered(location))
            {
                if (location == "here")
                {
                    string regPath = string.Format(@"SOFTWARE\Classes\Directory\Background\shell\{0}", SHELL_KEY_NAME);
                    Registry.LocalMachine.DeleteSubKeyTree(regPath);
                }
                else
                {
                    string regPath = string.Format(@"{0}\shell\{1}", location, SHELL_KEY_NAME);
                    Registry.ClassesRoot.DeleteSubKeyTree(regPath);
                }
            }
        }

        private bool IsStartupRegistered()
        {
            if (!isAdministrator)
                return false;

            string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath))
                {
                    return key.GetValue(SHELL_KEY_NAME) != null;
                }
            }
            catch (UnauthorizedAccessException)
            {
                isAdministrator = false;
                return false;
            }
        }

        private void StartupRegister()
        {
            if (!isAdministrator)
                return;

            if (!IsStartupRegistered())
            {
                string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath, true))
                {
                    key.SetValue(SHELL_KEY_NAME, string.Format("\"{0}\" /warmUp", Assembly.GetAssembly(typeof(OptionsView)).Location), RegistryValueKind.ExpandString);
                }
            }
        }

        private void StartupUnregister()
        {
            if (!isAdministrator)
                return;

            if (IsStartupRegistered())
            {
                string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath, true))
                {
                    key.DeleteValue(SHELL_KEY_NAME);
                }
            }
        }

        private void CheckIfAdmin()
        {
            try
            {
                WindowsIdentity wi = WindowsIdentity.GetCurrent();
                WindowsPrincipal wp = new WindowsPrincipal(wi);

                if (wp.IsInRole("Administrators"))
                {
                    IsAdministrator = true;
                }
                else
                {
                    IsAdministrator = false;
                }
            }
            catch
            {
                IsAdministrator = false;
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
                if (IsProperty(() => MatchThreshold, propertyName))
                {
                    if (MatchThreshold < 0 || MatchThreshold > 1.0)
                        error = "Error: Match threshold should be a number between 0 and 1.0";
                }
                else if (IsProperty(() => MatchTimeout, propertyName))
                {
                    if (MatchTimeout <= 0 || MatchTimeout > 60 * 60)
                        error = "Error: Match Timeout should be a number 0 and 3600";
                }
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
