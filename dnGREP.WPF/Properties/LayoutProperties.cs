using System.Windows;
using dnGREP.Common;

namespace dnGREP.WPF.Properties
{
    /// <summary>
    /// Changing the application to store the window layout properties in the GrepSettings
    /// class and file instead of user.config so all settings can be stored in the
    /// application directory, if it is run from a writable directory (not installed).
    /// This class is used to move values from Properties.Settings.Default to GrepSettings.
    /// Note this will also get the default layout properties from Properties.Settings
    /// for new installs.
    /// </summary>
    internal static class LayoutProperties
    {
        public static void Save()
        {
            GrepSettings.Instance.Save();
        }

        private static Rect? mainWindowBounds;
        private static WindowState? mainWindowState;
        private static Rect? replaceBounds;
        private static Rect? previewBounds;
        private static WindowState? previewWindowState;
        private static bool? previewDocked;
        private static double? previewDockedWidth;
        private static bool? previewHidden;

        public static Rect MainWindowBounds
        {
            get
            {
                if (!mainWindowBounds.HasValue)
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.MainWindowBounds))
                    {
                        mainWindowBounds = GrepSettings.Instance.Get<Rect>(GrepSettings.Key.MainWindowBounds);
                    }
                    else
                    {
                        mainWindowBounds = Properties.Settings.Default.MainFormExBounds;
                    }
                }
                return mainWindowBounds.Value;
            }
            set
            {
                mainWindowBounds = value;
                GrepSettings.Instance.Set(GrepSettings.Key.MainWindowBounds, mainWindowBounds.Value);
            }
        }

        public static WindowState MainWindowState
        {
            get
            {
                if (!mainWindowState.HasValue)
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.MainWindowState))
                    {
                        mainWindowState = GrepSettings.Instance.Get<WindowState>(GrepSettings.Key.MainWindowState);
                    }
                    else
                    {
                        mainWindowState = Properties.Settings.Default.MainWindowState;
                    }
                }
                return mainWindowState.Value;
            }
            set
            {
                mainWindowState = value;
                GrepSettings.Instance.Set(GrepSettings.Key.MainWindowState, mainWindowState.Value);
            }
        }

        public static Rect ReplaceBounds
        {
            get
            {
                if (!replaceBounds.HasValue)
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.ReplaceBounds))
                    {
                        replaceBounds = GrepSettings.Instance.Get<Rect>(GrepSettings.Key.ReplaceBounds);
                    }
                    else
                    {
                        replaceBounds = Properties.Settings.Default.ReplaceBounds;
                    }
                }
                return replaceBounds.Value;
            }
            set
            {
                replaceBounds = value;
                GrepSettings.Instance.Set(GrepSettings.Key.ReplaceBounds, replaceBounds.Value);
            }
        }

        public static Rect PreviewBounds
        {
            get
            {
                if (!previewBounds.HasValue)
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.PreviewBounds))
                    {
                        previewBounds = GrepSettings.Instance.Get<Rect>(GrepSettings.Key.PreviewBounds);
                    }
                    else
                    {
                        previewBounds = Properties.Settings.Default.PreviewBounds;
                    }
                }
                return previewBounds.Value;
            }
            set
            {
                previewBounds = value;
                GrepSettings.Instance.Set(GrepSettings.Key.PreviewBounds, previewBounds.Value);
            }
        }

        public static WindowState PreviewWindowState
        {
            get
            {
                if (!previewWindowState.HasValue)
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.PreviewWindowState))
                    {
                        previewWindowState = GrepSettings.Instance.Get<WindowState>(GrepSettings.Key.PreviewWindowState);
                    }
                    else
                    {
                        previewWindowState = Properties.Settings.Default.PreviewWindowState;
                    }
                }
                return previewWindowState.Value;
            }
            set
            {
                previewWindowState = value;
                GrepSettings.Instance.Set(GrepSettings.Key.PreviewWindowState, previewWindowState.Value);
            }
        }

        public static bool PreviewDocked
        {
            get
            {
                if (!previewDocked.HasValue)
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.PreviewDocked))
                    {
                        previewDocked = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreviewDocked);
                    }
                    else
                    {
                        previewDocked = Properties.Settings.Default.PreviewDocked;
                    }
                }
                return previewDocked.Value;
            }
            set
            {
                previewDocked = value;
                GrepSettings.Instance.Set(GrepSettings.Key.PreviewDocked, previewDocked.Value);
            }
        }


        public static double PreviewDockedWidth
        {
            get
            {
                if (!previewDockedWidth.HasValue)
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.PreviewDockedWidth))
                    {
                        previewDockedWidth = GrepSettings.Instance.Get<double>(GrepSettings.Key.PreviewDockedWidth);
                    }
                    else
                    {
                        previewDockedWidth = Properties.Settings.Default.PreviewDockedWidth;
                    }
                }
                return previewDockedWidth.Value;
            }
            set
            {
                previewDockedWidth = value;
                GrepSettings.Instance.Set(GrepSettings.Key.PreviewDockedWidth, previewDockedWidth.Value);
            }
        }

        public static bool PreviewHidden
        {
            get
            {
                if (!previewHidden.HasValue)
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.PreviewHidden))
                    {
                        previewHidden = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreviewHidden);
                    }
                    else
                    {
                        previewHidden = Properties.Settings.Default.PreviewHidden;
                    }
                }
                return previewHidden.Value;
            }
            set
            {
                previewHidden = value;
                GrepSettings.Instance.Set(GrepSettings.Key.PreviewHidden, previewHidden.Value);
            }
        }
    }
}
