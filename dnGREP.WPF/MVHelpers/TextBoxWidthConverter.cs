using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace dnGREP.WPF
{
    public class TextBoxWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 && values[0] is Control ctl &&
                values[1] is string fontFamily && values[2] is double &&
                parameter is string width && double.TryParse(width, out double stdWidth))
            {
                const string candiate = "Abcd 1234";

                double fontSize = (double)values[2];
                // when initializing, the size may be 0 (which throws an exception)
                if (fontSize < 8) fontSize = 12;

                Size defaultSize = MeasureString(candiate,
                    new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Normal),
                        SystemFonts.MessageFontSize, ctl);

                Size currentSize = MeasureString(candiate,
                    new Typeface(new FontFamily(fontFamily), SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Normal),
                        fontSize, ctl);

                double newWidth = stdWidth * currentSize.Width / defaultSize.Width;
                return newWidth;
            }
            return 80.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private Size MeasureString(string candidate, Typeface typeface, double fontSize, FrameworkElement control)
        {
            var formattedText = new FormattedText(candidate,
                                                  CultureInfo.CurrentUICulture,
                                                  FlowDirection.LeftToRight,
                                                  typeface,
                                                  fontSize,
                                                  Brushes.Black,
                                                  VisualTreeHelper.GetDpi(control).PixelsPerDip);

            return new Size(formattedText.Width, formattedText.Height);
        }
    }
}
