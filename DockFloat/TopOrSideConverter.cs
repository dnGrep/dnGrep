using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DockFloat
{
    /// <summary>
    /// Used so extra spacing is on the right of wide panels, but top of narrow ones.
    /// </summary>
    class TopOrSideConverter : IMultiValueConverter
    {
        public static TopOrSideConverter Instance { get; } = new TopOrSideConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var nullableHeight = values[0] as double?;
            var nullableWidth = values[1] as double?;
            var height = nullableHeight ?? 0.0;
            var width = nullableWidth ?? 0.0;

            return width > height ?
                System.Windows.Controls.Dock.Right :
                System.Windows.Controls.Dock.Top;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return default(object[]);
        }
    }
}
