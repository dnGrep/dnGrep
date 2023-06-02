using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    public class IntOrEmptyValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int num && num < 0)
            {
                return string.Empty;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                return -1;
            }

            return value;
        }
    }
}
