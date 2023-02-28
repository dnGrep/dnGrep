using System;
using System.Windows;
using dnGREP.Common;

namespace dnGREP.WPF.Properties
{
    /// <summary>
    /// Changing the application to store the window layout properties in the GrepSettings
    /// class and file instead of user.config so all settings can be stored in the
    /// application directory, if it is run from a writable directory (not installed).
    /// This class was used to move values from Properties.Settings.Default to GrepSettings,
    /// but Properties.Settings has now been removed. Note this will also set the default layout 
    /// properties for new installs.
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
        private static string? previewDockSide;
        private static double? previewDockedWidth;
        private static double? previewDockedHeight;
        private static bool? previewHidden;

        private static Rect Validate(Rect rect)
        {
            // width and height must be non-negative
            if (rect.Width < 0 || rect.Height < 0)
                return new Rect(rect.Location,
                    new Size(Math.Max(rect.Width, 0), Math.Max(rect.Height, 0)));

            return rect;
        }

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
                        mainWindowBounds = new(20, 20, 1200, 800);
                    }
                    mainWindowBounds = Validate(mainWindowBounds.Value);
                }
                return mainWindowBounds.Value;
            }
            set
            {
                mainWindowBounds = Validate(value);
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
                        mainWindowState = WindowState.Normal;
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
                        replaceBounds = new(0, 0, 0, 0);
                    }
                    replaceBounds = Validate(replaceBounds.Value);
                }
                return replaceBounds.Value;
            }
            set
            {
                replaceBounds = Validate(value);
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
                        previewBounds = new(0, 0, 0, 0);
                    }
                    previewBounds = Validate(previewBounds.Value);
                }
                return previewBounds.Value;
            }
            set
            {
                previewBounds = Validate(value);
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
                        previewWindowState = WindowState.Normal;
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
                        previewDocked = true;
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

        public static string PreviewDockSide
        {
            get
            {
                if (string.IsNullOrWhiteSpace(previewDockSide))
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.PreviewDockSide))
                    {
                        previewDockSide = GrepSettings.Instance.Get<string>(GrepSettings.Key.PreviewDockSide) ?? "Right";
                    }
                    else
                    {
                        previewDockSide = "Right";
                    }
                }
                return previewDockSide;
            }
            set
            {
                // validate value
                if (value == "Right" || value == "Bottom")
                {
                    previewDockSide = value;
                    GrepSettings.Instance.Set(GrepSettings.Key.PreviewDockSide, previewDockSide);
                }
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
                        previewDockedWidth = 360;
                    }
                    previewDockedWidth = Math.Max(previewDockedWidth.Value, 25);
                }
                return previewDockedWidth.Value;
            }
            set
            {
                previewDockedWidth = Math.Max(value, 25);
                GrepSettings.Instance.Set(GrepSettings.Key.PreviewDockedWidth, previewDockedWidth.Value);
            }
        }

        public static double PreviewDockedHeight
        {
            get
            {
                if (!previewDockedHeight.HasValue)
                {
                    if (GrepSettings.Instance.IsSet(GrepSettings.Key.PreviewDockedHeight))
                    {
                        previewDockedHeight = GrepSettings.Instance.Get<double>(GrepSettings.Key.PreviewDockedHeight);
                    }
                    else
                    {
                        previewDockedHeight = 200;
                    }
                    previewDockedHeight = Math.Max(previewDockedHeight.Value, 25);
                }
                return previewDockedHeight.Value;
            }
            set
            {
                previewDockedHeight = Math.Max(value, 25);
                GrepSettings.Instance.Set(GrepSettings.Key.PreviewDockedHeight, previewDockedHeight.Value);
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
                        previewHidden = false;
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
