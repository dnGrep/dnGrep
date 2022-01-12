using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace dnGREP.WPF
{
    public class IntToImageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && 0 <= intValue && intValue <= 7)
            {
                var uriString = $@"pack://application:,,,/dnGREP;component/Images/checks{intValue}.png";
                return new BitmapImage(new Uri(uriString));
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
