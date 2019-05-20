using System;
using System.Globalization;
using System.Management;
using System.Security.Principal;
using System.Windows;
using dnGREP.Common;
using Microsoft.Win32;

namespace dnGREP.WPF
{
    public enum WindowsTheme
    {
        Light,
        Dark
    }

    public class AppTheme
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        public static AppTheme Instance { get; } = new AppTheme();

        public event EventHandler CurrentThemeChanged;

        private AppTheme() { }

        public WindowsTheme WindowsTheme { get; private set; } = WindowsTheme.Light;

        public static bool HasWindowsThemes { get; private set; }

        // current value may not equal the saved settings value
        public bool FollowWindowsTheme { get; private set; }

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

                Application.Current.Resources.MergedDictionaries[0].Source = new Uri($"/Themes/{CurrentThemeName}Brushes.xaml", UriKind.Relative);

                //var accentColor = SystemParameters.WindowGlassColor;
                //Current.Resources["ControlAccentBrush"] = new SolidColorBrush(accentColor);

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
    }
}
