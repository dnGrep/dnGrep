using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace dnGREP.WPF
{
    public class TotalValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double total = 0;
            foreach (object o in values)
            {
                bool parsed = double.TryParse(o.ToString(), out double i);
                if (parsed)
                {
                    total += i;
                }
            }

            return new GridLength(total);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
