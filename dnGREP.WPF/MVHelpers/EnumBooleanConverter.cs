using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dnGREP.WPF
{
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string ParameterString)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object paramvalue = Enum.Parse(value.GetType(), ParameterString);
            if (paramvalue.Equals(value))
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string ParameterString || value.Equals(false))
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, ParameterString);
        }
    }
}
