using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [ValueConversion(typeof(double), typeof(double))]
    public class ElementSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percentage = double.Parse(parameter.ToString());

            return double.Parse(value.ToString()) * percentage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
