using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace dnGREP.WPF
{
    public class DockSiteVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 && values[1] is Dock side && values[2] is Dock dock)
            {
                bool isDocked = (bool)values[0];

                return isDocked && side == dock;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
