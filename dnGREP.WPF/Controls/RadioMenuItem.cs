using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace dnGREP.WPF
{
    public class RadioMenuItem : MenuItem
    {
        /// <summary>
        /// Identifies the <see cref="GroupName" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty GroupNameProperty = DependencyProperty.Register(
            "GroupName", typeof(string), typeof(RadioMenuItem),
            new UIPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the value of the <see cref="CaptionBackground" />
        /// property. This is a dependency property.
        /// </summary>
        public string GroupName
        {
            get
            {
                return (string)GetValue(GroupNameProperty);
            }
            set
            {
                SetValue(GroupNameProperty, value);
            }
        }

        protected override void OnClick()
        {
            if (VisualParent is Panel panel)
            {
                var rmi = panel.Children.OfType<RadioMenuItem>().FirstOrDefault(i =>
                    i.GroupName == GroupName && i.IsChecked);
                if (null != rmi) rmi.IsChecked = false;

                IsChecked = true;
            }
            base.OnClick();
        }
    }
}
