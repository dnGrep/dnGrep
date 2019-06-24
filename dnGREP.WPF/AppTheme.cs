using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using dnGREP.Common;
using Microsoft.Win32;
using NLog;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.WPF
{
    public enum WindowsTheme
    {
        Light,
        Dark
    }

    public class AppTheme
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        public static AppTheme Instance { get; } = new AppTheme();

        public event EventHandler CurrentThemeChanged;

        private AppTheme() { }

        public WindowsTheme WindowsTheme { get; private set; } = WindowsTheme.Light;

        public static bool HasWindowsThemes { get; private set; }

        // current value may not equal the saved settings value
        public bool FollowWindowsTheme { get; private set; }

        private readonly List<string> themeNames = new List<string> { "Light", "Dark" };
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

                currentThemeName = value;

                if (CurrentThemeName == "Light" || CurrentThemeName == "Dark")
                {
                    Application.Current.Resources.MergedDictionaries[0].Source = new Uri($"/Themes/{CurrentThemeName}Brushes.xaml", UriKind.Relative);
                }
                else
                {
                    string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnGrep");
                    string path = Path.Combine(dataFolder, CurrentThemeName + ".xaml");
                    if (File.Exists(path))
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
            string src = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Themes", "Sunset.xaml");
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
                            logger.Error(ex);
                        }
                    }
                }
            }

            if (!File.Exists(dest))
            {
                try
                {
                    File.Copy(src, dest);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }

            LoadExternalThemes();

            HasWindowsThemes = false;
            if (Environment.OSVersion.Version.Major >= 10)
            {
                string releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
                if (int.TryParse(releaseId, out int id) && id >= 1803)
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

            string appTheme = "Light";
            if (HasWindowsThemes && FollowWindowsTheme)
            {
                appTheme = WindowsTheme == WindowsTheme.Dark ? "Dark" : "Light";
            }
            else
            {
                appTheme = GrepSettings.Instance.Get<string>(GrepSettings.Key.CurrentTheme);
            }

            CurrentThemeName = appTheme;

            ThemedHighlightingManager.Instance.Initialize();
        }

        private bool FileChanged(string filePath1, string filePath2)
        {
            return GetSHA(filePath1) != GetSHA(filePath2);
        }

        private string GetSHA(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                using (var sha = SHA256.Create())
                {
                    return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public void WatchTheme()
        {
            if (!HasWindowsThemes)
                return;

            var currentUser = WindowsIdentity.GetCurrent();
            string query = string.Format(
                CultureInfo.InvariantCulture,
                @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                currentUser.User.Value,
                RegistryKeyPath.Replace(@"\", @"\\"),
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

        private static WindowsTheme GetWindowsTheme()
        {
            if (!HasWindowsThemes)
                return WindowsTheme.Light;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                object registryValueObject = key?.GetValue(RegistryValueName);
                if (registryValueObject == null)
                {
                    return WindowsTheme.Light;
                }

                int registryValue = (int)registryValueObject;

                return registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;
            }
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
            string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnGrep");
            foreach (string fileName in Directory.GetFiles(dataFolder, "*.xaml"))
            {
                try
                {
                    using (FileStream s = new FileStream(fileName, FileMode.Open))
                    {
                        string name = Path.GetFileNameWithoutExtension(fileName);
                        object obj = XamlReader.Load(s);
                        if (obj is ResourceDictionary dict && IsValid(dict, name) && !themeNames.Contains(name))
                        {
                            themeNames.Add(name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                }
            }
        }

        private bool IsValid(ResourceDictionary dict2, string name)
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
                    if (!(value2 is Brush))
                    {
                        valid = false;
                        logger.Error($"Theme '{name}', key '{key}' should be a Brush");
                    }
                }
                else if (value1 is DropShadowEffect)
                {
                    if (!(value2 is DropShadowEffect))
                    {
                        valid = false;
                        logger.Error($"Theme '{name}', key '{key}' should be a DropShadowEffect");
                    }
                }
                else if (value1 is bool)
                {
                    if (!(value2 is bool))
                    {
                        valid = false;
                        logger.Error($"Theme '{name}', key '{key}' should be a Boolean");
                    }
                }
            }

            if (!valid)
            {
                MessageBox.Show($"Could not load theme '{name}', see log file for more information.", "Load Theme", MessageBoxButton.OK);
            }

            return valid;
        }
    }
}
