using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using dnGREP.Common.UI;

namespace dnGREP.WPF
{
    [TemplatePart(Name = "PART_CloseButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_RestoreButton", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_MinimizeButton", Type = typeof(ButtonBase))]
    public class ThemedWindow : Window
    {
        static ThemedWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemedWindow), new FrameworkPropertyMetadata(typeof(ThemedWindow)));
        }

        public ThemedWindow()
            : base()
        {
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

        /// <summary>
        /// Identifies the <see cref="CaptionBackground" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty CaptionBackgroundProperty = DependencyProperty.Register(
            "CaptionBackground", typeof(Brush), typeof(ThemedWindow),
            new UIPropertyMetadata(Brushes.White));

        /// <summary>
        /// Gets or sets the value of the <see cref="CaptionBackground" />
        /// property. This is a dependency property.
        /// </summary>
        public Brush CaptionBackground
        {
            get
            {
                return (Brush)GetValue(CaptionBackgroundProperty);
            }
            set
            {
                SetValue(CaptionBackgroundProperty, value);
            }
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
        }

        public string WindowChromeFontFamily { get; set; } = SystemSymbols.WindowChromeFontFamily;
        public float WindowChromeFontSize { get; set; } = SystemSymbols.WindowChromeFontSize;
        public string MinimizeCharacter { get; set; } = SystemSymbols.MinimizeCharacter;
        public string MaximizeCharacter { get; set; } = SystemSymbols.MaximizeCharacter;
        public string RestoreCharacter { get; set; } = SystemSymbols.RestoreCharacter;
        public string CloseCharacter { get; set; } = SystemSymbols.CloseCharacter;
    }
}
