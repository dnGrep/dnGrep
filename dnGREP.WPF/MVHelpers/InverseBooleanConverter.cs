using System;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                return !(bool)value;
            }
            else
            {
                throw new InvalidOperationException("The target must be a boolean");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (null == value)
            {
                return true;
            }
            return !(bool)value;
        }
    }
}
