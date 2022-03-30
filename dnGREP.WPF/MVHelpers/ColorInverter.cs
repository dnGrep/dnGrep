using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace dnGREP.WPF
{
    public static class ColorInverter
    {

        public static void TranslateThemeColors(IHighlightingDefinition hl)
        {
            foreach (var item in hl.NamedHighlightingColors)
            {
                if (!item.IsFrozen)
                {
                    if (item.Foreground != null)
                    {
                        string hex = item.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.Background != null)
                    {
                        string hex = item.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
            }
            foreach (var item in hl.MainRuleSet.Rules)
            {
                if (item.Color != null && !item.Color.IsFrozen && string.IsNullOrWhiteSpace(item.Color.Name))
                {
                    if (item.Color.Foreground != null)
                    {
                        string hex = item.Color.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Color.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.Color.Background != null)
                    {
                        string hex = item.Color.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Color.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
            }
            foreach (var item in hl.MainRuleSet.Spans)
            {
                if (item.SpanColor != null && !item.SpanColor.IsFrozen && string.IsNullOrWhiteSpace(item.SpanColor.Name))
                {
                    if (item.SpanColor.Foreground != null)
                    {
                        string hex = item.SpanColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.SpanColor.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.SpanColor.Background != null)
                    {
                        string hex = item.SpanColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.SpanColor.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
                if (item.StartColor != null && !item.StartColor.IsFrozen && string.IsNullOrWhiteSpace(item.StartColor.Name))
                {
                    if (item.StartColor.Foreground != null)
                    {
                        string hex = item.StartColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.StartColor.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.StartColor.Background != null)
                    {
                        string hex = item.StartColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.StartColor.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
                if (item.EndColor != null && !item.EndColor.IsFrozen && string.IsNullOrWhiteSpace(item.EndColor.Name))
                {
                    if (item.EndColor.Foreground != null)
                    {
                        string hex = item.EndColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.EndColor.Foreground = new SimpleHighlightingBrush(Invert(c));
                    }
                    else if (item.EndColor.Background != null)
                    {
                        string hex = item.EndColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.EndColor.Background = new SimpleHighlightingBrush(Invert(c));
                    }
                }
            }
        }

        private static HighlightingBrush Invert(HighlightingBrush brush)
        {
            if (brush != null)
            {
                Color c = (Color)ColorConverter.ConvertFromString(brush.ToString());
                return new SimpleHighlightingBrush(Invert(c));
            }
            return null;
        }

        //public static Color Invert(Color c)
        //{
        //    double white_bias = .08;
        //    double m = 1.0 + white_bias;
        //    double shift = white_bias + (byte.MaxValue - Math.Min(c.R, Math.Min(c.G, c.B)) - Math.Max(c.R, Math.Max(c.G, c.B)));
        //    Color result = new Color
        //    {
        //        A = c.A,
        //        R = (byte)((shift + c.R) / m),
        //        G = (byte)((shift + c.G) / m),
        //        B = (byte)((shift + c.B) / m),
        //    };
        //    return result;
        //}

        public static Color Invert(Color c)
        {
            byte shift = (byte)(byte.MaxValue - Math.Min(c.R, Math.Min(c.G, c.B)) - Math.Max(c.R, Math.Max(c.G, c.B)));
            Color result = new Color
            {
                A = c.A,
                R = (byte)(shift + c.R),
                G = (byte)(shift + c.G),
                B = (byte)(shift + c.B),
            };
            return result;
        }

        public static BitmapSource Invert(BitmapImage source)
        {
            // Calculate stride of source
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;

            // Create data array to hold source pixel data
            int length = stride * source.PixelHeight;
            byte[] data = new byte[length];

            // Copy source image pixels to the data array
            source.CopyPixels(data, stride, 0);

            // Change this loop for other formats
            for (int i = 0; i < length; i += 4)
            {
                data[i] = (byte)(255 - data[i]); //R
                data[i + 1] = (byte)(255 - data[i + 1]); //G
                data[i + 2] = (byte)(255 - data[i + 2]); //B
                //data[i + 3] = (byte)(255 - data[i + 3]); //A
            }

            // Create a new BitmapSource from the inverted pixel buffer
            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY, source.Format,
                null, data, stride);
        }

        public static BitmapImage Convert(BitmapSource bitmapSource)
        {
            if (!(bitmapSource is BitmapImage bitmapImage))
            {
                bitmapImage = new BitmapImage();

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    encoder.Save(memoryStream);
                    memoryStream.Position = 0;

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }
            }

            return bitmapImage;
        }
    }
}
