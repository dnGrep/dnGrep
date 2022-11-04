using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnGREP.Common.UI;

namespace dnGREP.WPF
{
    public class LeadingTextTrimming
    {
        private static readonly string ellipses = char.ConvertFromUtf32(0x2026);
        private static readonly string ltrMark = char.ConvertFromUtf32(0x200e);

        public static string GetFullText(DependencyObject obj)
        {
            return (string)obj.GetValue(FullTextProperty);
        }

        public static void SetFullText(DependencyObject obj, string value)
        {
            obj.SetValue(FullTextProperty, value);
        }


        public static readonly DependencyProperty FullTextProperty =
                DependencyProperty.RegisterAttached("FullText",
                    typeof(string), typeof(LeadingTextTrimming),
                    new FrameworkPropertyMetadata(string.Empty,
                        FrameworkPropertyMetadataOptions.AffectsMeasure |
                        FrameworkPropertyMetadataOptions.AffectsRender,
                        OnTextSet));

        private static void OnTextSet(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
                UpdateText(textBlock);
                textBlock.SizeChanged -= TextBlock_SizeChanged;
                textBlock.SizeChanged += TextBlock_SizeChanged;
            }
        }

        private static void TextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                UpdateText(textBlock);
            }
        }

        private static void UpdateText(TextBlock textBlock)
        {
            var text = GetFullText(textBlock);
            var actualWidth = textBlock.ActualWidth;

            if (actualWidth == 0)
                return;

            if (!string.IsNullOrWhiteSpace(text))
            {
                var typeFace = new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch);

                var sz = text.MeasureString(typeFace, textBlock.FontSize, textBlock);

                if (sz.Width > actualWidth)
                {
                    while (sz.Width > actualWidth && text.Length > 0)
                    {
                        text = text.Substring(1);
                        sz = AddPrefix(text).MeasureString(typeFace, textBlock.FontSize, textBlock);
                    }

                    text = AddPrefix(text);
                }
            }
            textBlock.Text = text;
        }

        private static string AddPrefix(string text)
        {
            if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
            {
                return string.Concat(ltrMark, ellipses, text);
            }
            else
            {
                return string.Concat(ellipses, text);
            }
        }
    }
}
