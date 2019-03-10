using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DockFloat
{
    [TemplatePart(Name = "PART_DockButton", Type = typeof(ButtonBase))]
    public class FloatWindow : Window
    {
        static FloatWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FloatWindow), new FrameworkPropertyMetadata(typeof(FloatWindow)));
        }

        public FloatWindow(FrameworkElement content)
        {
            ConfigureContentForFloating(content);
            Content = content;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var dockInButton = GetTemplateChild("PART_DockButton") as Button;
            dockInButton.Click += (s, e) => Close();
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
