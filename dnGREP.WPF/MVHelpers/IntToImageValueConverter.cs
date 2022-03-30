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
        private static readonly Dictionary<string, BitmapImage> lightImageCache = new Dictionary<string, BitmapImage>();
        private static readonly Dictionary<string, BitmapImage> darkImageCache = new Dictionary<string, BitmapImage>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue && 0 <= intValue && intValue <= 7)
            {
                string key = $"checks{intValue}";
                bool invertColors = (bool)Application.Current.Resources["AvalonEdit.SyntaxColor.Invert"];

                var cache = invertColors ? darkImageCache : lightImageCache;

                if (!cache.TryGetValue(key, out BitmapImage image))
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
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
