using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using dnGREP.Common;
using dnGREP.Localization;
using Microsoft.Win32;
using NLog;

namespace dnGREP.WPF
{
    public enum WindowsTheme
    {
        Light,
        Dark
    }

    public class AppTheme
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        public static AppTheme Instance { get; } = new();

        public event EventHandler? CurrentThemeChanging;
        public event EventHandler? CurrentThemeChanged;

        private AppTheme() { }

        public WindowsTheme WindowsTheme { get; private set; } = WindowsTheme.Light;

        public static bool HasWindowsThemes { get; private set; }

        // current value may not equal the saved settings value
        public bool FollowWindowsTheme { get; private set; }

        private readonly List<string> themeNames = new() { "Light", "Dark" };
        public IEnumerable<string> ThemeNames { get => themeNames; }

        internal void ReloadCurrentTheme()
        {
            string name = currentThemeName;
            currentThemeName = string.Empty;
            CurrentThemeName = name;
        }

        // current value may not equal the saved settings value
        private string currentThemeName = "Light";
        public string CurrentThemeName
        {
            get { return currentThemeName; }
            set
            {
                if (currentThemeName == value)
                    return;

                CurrentThemeChanging?.Invoke(this, EventArgs.Empty);

                currentThemeName = value;

                if (CurrentThemeName == "Light" || CurrentThemeName == "Dark")
                {
                    Application.Current.Resources.MergedDictionaries[0].Source = new Uri($"/Themes/{CurrentThemeName}Brushes.xaml", UriKind.Relative);
                }
                else
                {
                    string dataFolder = Utils.GetDataFolderPath();
                    string fileName = CurrentThemeName + ".xaml";
                    string? path = Directory.GetFiles(dataFolder, "*.xaml", SearchOption.AllDirectories)
                        .Where(p => Path.GetFileName(p).Equals(fileName, StringComparison.Ordinal))
                        .FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        Application.Current.Resources.MergedDictionaries[0].Source = new Uri(path, UriKind.Absolute);
                    }
                }

                CurrentThemeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal void FollowWindowsThemeChanged(bool followWindowsTheme, string currentTheme)
        {
            FollowWindowsTheme = followWindowsTheme;
            if (followWindowsTheme)
            {
                CurrentThemeName = WindowsTheme == WindowsTheme.Dark ? "Dark" : "Light";
            }
            else
            {
                CurrentThemeName = currentTheme;
            }
        }

        public void Initialize()
        {
            if (!Utils.IsPortableMode)
            {
                // Copy Sunset.xaml from Program Files\Themes to AppData\dnGrep
                // If the file exists and is different, back up the old file first, then copy
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
                string src = Path.Combine(dir, "Themes", "Sunset.xaml");
                string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnGrep", "Sunset.xaml");
                if (File.Exists(dest) && FileChanged(src, dest))
                {
                    for (int idx = 1; idx < 40; idx++)
                    {
                        string temp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnGrep", $"Sunset_{idx}.xaml.bak");
                        if (!File.Exists(temp))
                        {
                            try
                            {
                                File.Move(dest, temp);
                                break;
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, "Failure in initialize themes, backup Sunset.xaml");
                            }
                        }
                    }
                }

                if (Directory.Exists(Path.GetDirectoryName(dest)) && !File.Exists(dest))
                {
                    try
                    {
                        File.Copy(src, dest);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failure in initialize themes, copy Sunset.xaml");
                    }
                }
            }

            LoadExternalThemes();

            HasWindowsThemes = false;
            if (Environment.OSVersion.Version.Major >= 10)
            {
                string? releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "")?.ToString();
                if (!string.IsNullOrEmpty(releaseId) && int.TryParse(releaseId, out int id) && id >= 1803)
                {
                    HasWindowsThemes = true;
                }
            }

            if (HasWindowsThemes)
            {
                WindowsTheme = GetWindowsTheme();
                WatchTheme();
            }

            FollowWindowsTheme = HasWindowsThemes && GrepSettings.Instance.Get<bool>(GrepSettings.Key.FollowWindowsTheme);

            string appTheme;
            if (HasWindowsThemes && FollowWindowsTheme)
            {
                appTheme = WindowsTheme == WindowsTheme.Dark ? "Dark" : "Light";
            }
            else
            {
                var setting = GrepSettings.Instance.Get<string>(GrepSettings.Key.CurrentTheme);
                appTheme = setting ?? "Light";
            }

            CurrentThemeName = appTheme;

            ThemedHighlightingManager.Instance.Initialize();
        }

        private static bool FileChanged(string filePath1, string filePath2)
        {
            return GetSHA(filePath1) != GetSHA(filePath2);
        }

        private static string GetSHA(string filename)
        {
            using var stream = File.OpenRead(filename);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
        }

        public void WatchTheme()
        {
            if (!HasWindowsThemes)
                return;

            var currentUser = WindowsIdentity.GetCurrent();
            if (currentUser != null && currentUser.User != null)
            {
                string query = string.Format(
                    CultureInfo.InvariantCulture,
                    @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                    currentUser.User.Value,
                    RegistryKeyPath.Replace(@"\", @"\\", StringComparison.Ordinal),
                    RegistryValueName);

                try
                {
                    var watcher = new ManagementEventWatcher(query);
                    watcher.EventArrived += (sender, args) =>
                    {
                        AppThemeChanged();
                    };

                    // Start listening for events
                    watcher.Start();
                }
                catch (Exception)
                {
                    // This can fail on Windows 7
                }
            }
        }

        private static WindowsTheme GetWindowsTheme()
        {
            if (!HasWindowsThemes)
                return WindowsTheme.Light;

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            object? registryValueObject = key?.GetValue(RegistryValueName);
            if (registryValueObject == null)
            {
                return WindowsTheme.Light;
            }

            int registryValue = (int)registryValueObject;

            return registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;
        }

        private void AppThemeChanged()
        {
            WindowsTheme = GetWindowsTheme();

            if (FollowWindowsTheme)
            {
                CurrentThemeName = WindowsTheme == WindowsTheme.Dark ? "Dark" : "Light";
            }
        }

        private void LoadExternalThemes()
        {
            string dataFolder = Utils.GetDataFolderPath();
            foreach (string fileName in Directory.GetFiles(dataFolder, "*.xaml", SearchOption.AllDirectories))
            {
                try
                {
                    using FileStream s = new(fileName, FileMode.Open);
                    string name = Path.GetFileNameWithoutExtension(fileName);
                    object obj = XamlReader.Load(s);
                    if (obj is ResourceDictionary dict && IsValid(dict, name) && !themeNames.Contains(name))
                    {
                        themeNames.Add(name);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to load external theme file: '{fileName}'");
                }
            }
        }

        private static bool IsValid(ResourceDictionary dict2, string name)
        {
            bool valid = true;
            var dict1 = Application.Current.Resources.MergedDictionaries[0];

            if (dict1.Count > dict2.Count)
            {
                valid = false;
                logger.Error($"Theme '{name}' is missing {dict1.Count - dict2.Count} keys");
            }

            foreach (var key in dict1.Keys)
            {
                if (!dict2.Contains(key))
                {
                    valid = false;
                    logger.Error($"Theme '{name}' is missing key '{key}'");
                }

                object value1 = dict1[key];
                object value2 = dict2[key];

                if (value1 is Brush)
                {
                    if (value2 is not Brush)
                    {
                        valid = false;
                        logger.Error($"Theme '{name}', key '{key}' should be a Brush");
                    }
                }
                else if (value1 is DropShadowEffect)
                {
                    if (value2 is not DropShadowEffect)
                    {
                        valid = false;
                        logger.Error($"Theme '{name}', key '{key}' should be a DropShadowEffect");
                    }
                }
                else if (value1 is bool)
                {
                    if (value2 is not bool)
                    {
                        valid = false;
                        logger.Error($"Theme '{name}', key '{key}' should be a Boolean");
                    }
                }
            }

            if (!valid)
            {
                MessageBox.Show(TranslationSource.Format(Localization.Properties.Resources.MessageBox_CouldNotLoadTheme, name) + App.LogDir,
                    Localization.Properties.Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
            }

            return valid;
        }
    }
}
