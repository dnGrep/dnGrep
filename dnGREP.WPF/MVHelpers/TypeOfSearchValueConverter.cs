using System;
using System.Globalization;
using System.Windows.Data;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class TypeOfSearchValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 1 &&
                values[0] is SearchType typeOfSearch &&
                values[1] is bool visible)
            {
                if (visible)
                    return typeOfSearch.ToString();
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
