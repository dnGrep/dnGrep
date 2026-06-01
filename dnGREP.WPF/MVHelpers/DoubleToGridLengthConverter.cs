using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dnGREP.WPF
{
    /// <summary>
    /// Converts a double value to a GridLength (pixel) value for use with column width binding.
    /// </summary>
    public class DoubleToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && !double.IsNaN(width) && width >= 0)
            {
                return new GridLength(width, GridUnitType.Pixel);
            }
            return new GridLength(1, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GridLength gridLength)
            {
                return gridLength.Value;
            }
            return double.NaN;
        }
    }
}
