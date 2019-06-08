using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dnGREP.WPF
{
    public class ResizeModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ResizeMode mode)
            {
                if (mode == ResizeMode.NoResize)
                    return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
