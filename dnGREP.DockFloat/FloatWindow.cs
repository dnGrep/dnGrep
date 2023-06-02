using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using dnGREP.Common.UI;

namespace dnGREP.DockFloat
{
    [TemplatePart(Name = "PART_CloseButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_RestoreButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_MinimizeButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_DockButton", Type = typeof(ButtonBase))]
    public class FloatWindow : Window
    {
        private bool forceClose;

        static FloatWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FloatWindow), new FrameworkPropertyMetadata(typeof(FloatWindow)));
        }

        public FloatWindow(FrameworkElement content)
        {
            ConfigureContentForFloating(content);
            Content = content;

            Closing += FloatWindow_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                hwndSource.AddHook(HookProc);
            }
        }

        private IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_NCHITTEST)
            {
                try
                {
                    // Support snap layouts for desktop apps on Windows 11
                    // https://docs.microsoft.com/en-us/windows/apps/desktop/modernize/apply-snap-layout-menu
                    var point = PointFromScreen(Native.GetPoint(lParam));
                    if (GetTemplateChild("PART_RestoreButton") is Button restoreButton)
                    {
                        var buttonRect = restoreButton.TransformToVisual(this)
                            .TransformBounds(new Rect(restoreButton.RenderSize));

                        if (buttonRect.Contains(point))
                        {
                            handled = true;
                            return Native.HTMAXBUTTON;
                        }
                    }

                    // This prevents a crash in WindowChromeWorker._HandleNCHitTest
                    lParam.ToInt32();
                }
                catch (OverflowException)
                {
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_CloseButton") is Button closeButton)
                closeButton.Click += (s, e) => Close();

            if (GetTemplateChild("PART_RestoreButton") is Button restoreButton)
                restoreButton.Click += (s, e) => WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;

            if (GetTemplateChild("PART_MinimizeButton") is Button minimizeButton)
                minimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;

            if (GetTemplateChild("PART_DockButton") is Button dockInButton)
                dockInButton.Click += (s, e) => ForceClose();

            PreviewKeyDown += FloatWindow_PreviewKeyDown;
        }

        private void FloatWindow_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        public void ForceClose()
        {
            forceClose = true;

            Close();
        }

        private void FloatWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (!forceClose)
            {
                Hide();
                e.Cancel = true;
            }
        }

        static void ConfigureContentForFloating(FrameworkElement content)
        {
            content.Width = double.NaN;
            content.Height = double.NaN;
            content.HorizontalAlignment = HorizontalAlignment.Stretch;
            content.VerticalAlignment = VerticalAlignment.Stretch;
        }
    }
}
