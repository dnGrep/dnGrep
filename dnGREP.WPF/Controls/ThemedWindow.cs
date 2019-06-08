using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

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
    }
}
