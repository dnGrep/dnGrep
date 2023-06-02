using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace dnGREP.WPF
{
    public class IntToImageValueConverter : IValueConverter
    {
        private static readonly Dictionary<string, BitmapImage> lightImageCache = new();
        private static readonly Dictionary<string, BitmapImage> darkImageCache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int)
            {
                throw new ArgumentException("Value must be an int");
            }

            int intValue = (int)value;

            if (intValue is < 0 or > 7)
            {
                throw new ArgumentException("Integer value must be 0..7 inclusive");
            }

            string key = $"checks{intValue}";
            bool invertColors = (bool)Application.Current.Resources["AvalonEdit.SyntaxColor.Invert"];

            var cache = invertColors ? darkImageCache : lightImageCache;

            if (!cache.TryGetValue(key, out BitmapImage? image))
            {
                var uriString = $@"pack://application:,,,/dnGREP;component/Images/checks{intValue}.png";
                image = new BitmapImage(new Uri(uriString));

                if (invertColors)
                {
                    image = ColorInverter.Convert(ColorInverter.Invert(image));
                }

                cache.Add(key, image);
            }

            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
