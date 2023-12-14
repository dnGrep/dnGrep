using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace dnGREP.WPF
{
    [ValueConversion(typeof(bool), typeof(ScrollBarVisibility))]
    public class BoolToScrollBarVisibilityConverter : IValueConverter
    {
        public ScrollBarVisibility TrueValue { get; set; }
        public ScrollBarVisibility FalseValue { get; set; }

        public BoolToScrollBarVisibilityConverter()
        {
            // set defaults
            FalseValue = ScrollBarVisibility.Auto;
            TrueValue = ScrollBarVisibility.Disabled;
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
