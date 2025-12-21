using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [ValueConversion(typeof(string), typeof(string))]
    public class SpecialCharacterToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text.Replace("\t", string.Empty, StringComparison.Ordinal);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
