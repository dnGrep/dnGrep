using System;
using System.Globalization;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [ValueConversion(typeof(long), typeof(string))]
    public class LongToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long num)
            {
                return num.ToString(CultureInfo.CurrentCulture);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                var style = NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign;

                if (long.TryParse(text, style, CultureInfo.CurrentCulture, out long num))
                {
                    return num;
                }
            }
            return value;
        }
    }
}
