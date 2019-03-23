using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DockFloat
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

        private void FloatWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        public void ForceClose()
        {
            forceClose = true;

            Close();
        }

        private void FloatWindow_Closing(object sender, CancelEventArgs e)
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
