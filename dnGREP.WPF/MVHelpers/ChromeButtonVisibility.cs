using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [Flags]
    public enum ChromeButtonVisibility
    {
        Visible = 0,
        MinimizeHidden = 1,
        MaximizeHidden = 2,
        MinimizeMaximizeHidden = 3,
    }

    public static class ChromeButtonExtension
    {
        public static ChromeButtonVisibility GetButtonVisibility(DependencyObject obj)
        {
            return (ChromeButtonVisibility)obj.GetValue(ButtonVisibilityProperty);
        }

        public static void SetButtonVisibility(DependencyObject obj, ChromeButtonVisibility value)
        {
            obj.SetValue(ButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty ButtonVisibilityProperty = DependencyProperty.RegisterAttached(
                "ButtonVisibility", typeof(ChromeButtonVisibility), typeof(ChromeButtonExtension),
                new PropertyMetadata(defaultValue: ChromeButtonVisibility.Visible));
    }

    public class ChromeButtonVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 &&
                values[0] is ResizeMode mode &&
                values[1] is ChromeButtonVisibility visibility &&
                parameter is string buttonName)
            {
                if (mode == ResizeMode.NoResize)
                    return Visibility.Collapsed;

                if (visibility == ChromeButtonVisibility.Visible)
                    return Visibility.Visible;

                if (visibility == ChromeButtonVisibility.MinimizeMaximizeHidden)
                    return Visibility.Collapsed;

                if (visibility.HasFlag(ChromeButtonVisibility.MinimizeHidden) && buttonName == "minimize")
                    return Visibility.Collapsed;

                if (visibility.HasFlag(ChromeButtonVisibility.MaximizeHidden) && buttonName == "maximize")
                    return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
