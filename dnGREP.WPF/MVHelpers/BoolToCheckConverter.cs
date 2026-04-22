using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToCheckConverter : IValueConverter
    {
        public BoolToCheckConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? char.ConvertFromUtf32(0x2713) : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
