using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows;

namespace dnGREP.WPF
{
    public class TotalValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double total = 0;
            foreach (object o in values)
            {
                double i;
                bool parsed = double.TryParse(o.ToString(), out i);
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
