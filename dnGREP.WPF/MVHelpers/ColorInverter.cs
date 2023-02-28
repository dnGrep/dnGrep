using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit.Highlighting;

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
                        var hex = item.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Foreground = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                    else if (item.Background != null)
                    {
                        var hex = item.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Background = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                }
            }
            foreach (var item in hl.MainRuleSet.Rules)
            {
                if (item.Color != null && !item.Color.IsFrozen && string.IsNullOrWhiteSpace(item.Color.Name))
                {
                    if (item.Color.Foreground != null)
                    {
                        var hex = item.Color.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Color.Foreground = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                    else if (item.Color.Background != null)
                    {
                        var hex = item.Color.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.Color.Background = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                }
            }
            foreach (var item in hl.MainRuleSet.Spans)
            {
                if (item.SpanColor != null && !item.SpanColor.IsFrozen && string.IsNullOrWhiteSpace(item.SpanColor.Name))
                {
                    if (item.SpanColor.Foreground != null)
                    {
                        var hex = item.SpanColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.SpanColor.Foreground = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                    else if (item.SpanColor.Background != null)
                    {
                        var hex = item.SpanColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.SpanColor.Background = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                }
                if (item.StartColor != null && !item.StartColor.IsFrozen && string.IsNullOrWhiteSpace(item.StartColor.Name))
                {
                    if (item.StartColor.Foreground != null)
                    {
                        var hex = item.StartColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.StartColor.Foreground = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                    else if (item.StartColor.Background != null)
                    {
                        var hex = item.StartColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.StartColor.Background = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                }
                if (item.EndColor != null && !item.EndColor.IsFrozen && string.IsNullOrWhiteSpace(item.EndColor.Name))
                {
                    if (item.EndColor.Foreground != null)
                    {
                        var hex = item.EndColor.Foreground.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.EndColor.Foreground = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                    else if (item.EndColor.Background != null)
                    {
                        var hex = item.EndColor.Background.ToString();
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        item.EndColor.Background = new SimpleHighlightingBrush(ShiftAndInvert(c));
                    }
                }
            }
        }

        public static Color ShiftAndInvert(Color color)
        {
            return Invert(ShiftFromBlue(color));
        }

        /// <summary>
        /// Shifts colors away from blue toward cyan or magenta
        /// </summary>
        /// <remarks>
        /// Blue on a dark background has a very low contrast. To make it more
        /// legible, shift blues toward cyan or magenta.
        /// </remarks>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color ShiftFromBlue(Color color)
        {
            var hsv = ConvertToHSV(color);
            double hue = hsv.Item1;
            const double maxHue = 360;
            const double low = 180 / maxHue;
            const double mid = 240 / maxHue;
            const double hi = 300 / maxHue;
            const double factor = 2;

            if (hue > low && hue <= mid)
            {
                hue = low + (hue - low) / factor;
                return ConvertToColor(hue, hsv.Item2, hsv.Item3);
            }
            else if (hue > mid && hue < hi)
            {
                hue = hi - (hi - hue) / factor;
                return ConvertToColor(hue, hsv.Item2, hsv.Item3);
            }
            return color;
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

        /// <summary>
        /// Inverts a text color for a white background so it is legible on a black background
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color Invert(Color color)
        {
            byte shift = (byte)(byte.MaxValue - Math.Min(color.R, Math.Min(color.G, color.B)) - Math.Max(color.R, Math.Max(color.G, color.B)));
            Color result = new()
            {
                A = color.A,
                R = (byte)(shift + color.R),
                G = (byte)(shift + color.G),
                B = (byte)(shift + color.B),
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
            if (bitmapSource is not BitmapImage bitmapImage)
            {
                bitmapImage = new BitmapImage();

                PngBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using MemoryStream memoryStream = new();
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }

        public static Color ConvertToColor(double h, double s, double v)
        {
            double r, g, b;

            r = v;   // default to gray
            g = v;
            b = v;

            int hi = (int)(h * 6.0);
            double f = (h * 6.0) - hi;
            double p = v * (1 - s);
            double q = v * (1 - f * s);
            double t = v * (1 - (1 - f) * s);

            switch (hi)
            {
                case 0:
                    r = v;
                    g = t;
                    b = p;
                    break;

                case 1:
                    r = q;
                    g = v;
                    b = p;
                    break;

                case 2:
                    r = p;
                    g = v;
                    b = t;
                    break;

                case 3:
                    r = p;
                    g = q;
                    b = v;
                    break;

                case 4:
                    r = t;
                    g = p;
                    b = v;
                    break;

                case 5:
                    r = v;
                    g = p;
                    b = q;
                    break;
            }

            Color rgb = Color.FromArgb(255,
                System.Convert.ToByte(r * 255.0f),
                System.Convert.ToByte(g * 255.0f),
                System.Convert.ToByte(b * 255.0f));

            return rgb;
        }

        public static Tuple<double, double, double> ConvertToHSV(Color c)
        {
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double hue, sat, val;
            if (max == min)
                hue = 0.0;
            else if (max == r)
                hue = ((60 * ((g - b) / delta)) + 360) % 360;
            else if (max == g)
                hue = (60 * ((b - r) / delta)) + 120;
            else
                hue = (60 * ((r - g) / delta)) + 240;

            hue /= 360;

            if (max == 0.0)
                sat = 0.0;
            else
                sat = delta / max;

            val = max;

            return new Tuple<double, double, double>(hue, sat, val);
        }
    }
}
