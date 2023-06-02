using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [ValueConversion(typeof(string), typeof(GridLength))]
    public class GridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (targetType != typeof(GridLength))
                throw new InvalidOperationException("The target must be a GridLength");

            string? text = value as string;
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (text.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                    return GridLength.Auto;

                if (text.Equals("*", StringComparison.Ordinal))
                    return new GridLength(1, GridUnitType.Star);

                if (text.EndsWith("*", StringComparison.Ordinal))
                {
                    text = text.TrimEnd('*');

                    if (double.TryParse(text, out double d))
                        return new GridLength(d, GridUnitType.Star);
                }

                else if (double.TryParse(text, out double d))
                {
                    return new GridLength(d, GridUnitType.Pixel);
                }
            }

            return GridLength.Auto;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
