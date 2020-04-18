using System;
using System.Globalization;
using System.Windows.Data;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class TypeOfSearchValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SearchType typeOfSearch)
            {
                return typeOfSearch.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
