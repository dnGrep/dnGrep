using System;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Windows;
using dnGREP.Localization;
using Microsoft.Win32;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public static class RegistryOperations
    {
        private static readonly string SHELL_KEY_NAME = "dnGREP";
        private static readonly string SHELL_MENU_TEXT = "dnGrep...";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static bool IsShellRegistered(string location)
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
                    isAdministrator = false;
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
                    isAdministrator = false;
                    return false;
                }
            }
        }

        public static bool ShellRegisterContextMenu(bool silent)
        {
            bool success = false;
            success |= ShellRegister("Directory", silent);
            success |= ShellRegister("Drive", silent);
            success |= ShellRegister("*", silent);
            success |= ShellRegister("here", silent);
            return success;
        }

        public static bool ShellRegister(string location, bool silent)
        {
            if (!IsAdministrator)
                return false;

            bool success = false;
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

                        success = true;
                    }
                }
                catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    isAdministrator = false;

                    if (!silent)
                    {
                        MessageBox.Show(Resources.MessageBox_RunDnGrepAsAdministrator,
                            Resources.MessageBox_DnGrep,
                            MessageBoxButton.OK, MessageBoxImage.Error,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to register dnGrep with Explorer context menu");

                    if (!silent)
                    {
                        MessageBox.Show(Resources.MessageBox_ThereWasAnErrorAddingDnGrepToExplorerRightClickMenu + App.LogDir,
                            Resources.MessageBox_DnGrep,
                            MessageBoxButton.OK, MessageBoxImage.Error,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                }
            }
            return success;
        }

        public static bool ShellUnregisterContextMenu(bool silent)
        {
            bool success = true;
            success |= ShellUnregister("Directory", silent);
            success |= ShellUnregister("Drive", silent);
            success |= ShellUnregister("*", silent);
            success |= ShellUnregister("here", silent);
            return success;
        }

        private static bool ShellUnregister(string location, bool silent)
        {
            if (!IsAdministrator)
                return false;

            bool success = false;
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

                    success = true;
                }
            }
            catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
            {
                isAdministrator = false;

                if (!silent)
                {
                    MessageBox.Show(Resources.MessageBox_RunDnGrepAsAdministrator,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to remove dnGrep from Explorer context menu");
                if (!silent)
                {
                    MessageBox.Show(Resources.MessageBox_ThereWasAnErrorRemovingDnGrepFromTheExplorerRightClickMenu + App.LogDir,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error,
                        MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                }
            }
            return success;
        }

        public static bool IsStartupRegistered()
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

        public static void StartupRegister()
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
        public static void StartupUnregister()
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

        private static bool? isAdministrator;
        public static bool IsAdministrator
        {
            get
            {
                if (isAdministrator == null)
                {
                    CheckIfAdmin();
                }

                return isAdministrator.HasValue ? isAdministrator.Value : false;
            }
        }

        private static void CheckIfAdmin()
        {
            try
            {
                using WindowsIdentity wi = WindowsIdentity.GetCurrent();
                WindowsPrincipal wp = new(wi);

                isAdministrator = wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                isAdministrator = false;
            }
        }

    }
}
