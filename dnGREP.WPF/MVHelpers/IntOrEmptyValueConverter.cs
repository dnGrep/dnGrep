using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    public class IntOrEmptyValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int && (int)value < 0)
            {
                return null;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (value is string && string.IsNullOrWhiteSpace((string)value)))
            {
                return -1;
            }

            return value;
        }
    }
}
