using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double num)
            {
                return num.ToString(CultureInfo.CurrentCulture);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                var style = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

                if (double.TryParse(text, style, CultureInfo.CurrentCulture, out double num))
                {
                    return num;
                }
            }
            return value;
        }
    }
}
