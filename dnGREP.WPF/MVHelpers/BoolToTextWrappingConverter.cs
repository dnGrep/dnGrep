using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [ValueConversion(typeof(bool), typeof(ScrollBarVisibility))]
    public class BoolToTextWrappingConverter : IValueConverter
    {
        public TextWrapping TrueValue { get; set; }
        public TextWrapping FalseValue { get; set; }

        public BoolToTextWrappingConverter()
        {
            // set defaults
            FalseValue = TextWrapping.NoWrap;
            TrueValue = TextWrapping.Wrap;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
