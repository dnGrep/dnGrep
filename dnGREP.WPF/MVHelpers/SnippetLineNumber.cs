using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace dnGREP.WPF
{
    public class SnippetLineNumber : LineNumberMargin
    {
        private int[] lineNumbers;
        private Brush gray = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        private Brush lightGray = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        private System.Windows.Size size;

        public SnippetLineNumber() : this(null) { }
        public SnippetLineNumber(int[] lineNumbers)
        {
            this.lineNumbers = lineNumbers;
        }

        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            base.MeasureOverride(availableSize);

            typeface = createTypeface();
            emSize = (double)GetValue(TextBlock.FontSizeProperty);

            FormattedText text = createFormattedText(
                this,
                new string('9', maxLineNumberLength),
                typeface,
                emSize,
                gray
            );
            size = new Size(text.Width, 0);
            return size;
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            TextView textView = this.TextView;
            Size renderSize = this.RenderSize;
            if (textView != null && textView.VisualLinesValid)
            {
                foreach (VisualLine line in textView.VisualLines)
                {
                    int lineNumber = line.FirstDocumentLine.LineNumber;
                    if (lineNumbers != null && lineNumbers.Length > line.FirstDocumentLine.LineNumber - 1)
                        lineNumber = lineNumbers[line.FirstDocumentLine.LineNumber - 1];
                    FormattedText text = createFormattedText(
                        this,
                        lineNumber.ToString(CultureInfo.CurrentCulture),
                        typeface, emSize, gray
                    );
                    double y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                    drawingContext.DrawText(text, new Point(renderSize.Width - text.Width, y - textView.VerticalOffset));                    
                }
            }
        }

        protected override void OnDocumentChanged(TextDocument oldDocument, TextDocument newDocument)
        {
            base.OnDocumentChanged(oldDocument, newDocument);

            int documentLineCount = (lineNumbers == null? Document.LineCount : lineNumbers[lineNumbers.Length - 1]);
            if (documentLineCount <= 0)
                documentLineCount = lineNumbers[lineNumbers.Length - 2];

            int newLength = documentLineCount.ToString(CultureInfo.CurrentCulture).Length;

            // The margin looks too small when there is only one digit, so always reserve space for
            // at least two digits
            if (newLength < 2)
                newLength = 2;

            if (newLength != maxLineNumberLength)
            {
                maxLineNumberLength = newLength;
                InvalidateMeasure();
            }
        }

        private FormattedText createFormattedText(FrameworkElement element, string text, Typeface typeface, double? emSize, Brush foreground)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (text == null)
                throw new ArgumentNullException("text");
            if (typeface == null)
                typeface = createTypeface();
            if (emSize == null)
                emSize = TextBlock.GetFontSize(element);
            if (foreground == null)
                foreground = gray;
            return new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                emSize.Value,
                foreground,
                null,
                TextOptions.GetTextFormattingMode(element)
            );
        }

        private Typeface createTypeface()
        {
            return new Typeface((FontFamily)this.GetValue(TextBlock.FontFamilyProperty),
                                (FontStyle)this.GetValue(TextBlock.FontStyleProperty),
                                (FontWeight)this.GetValue(TextBlock.FontWeightProperty),
                                (FontStretch)this.GetValue(TextBlock.FontStretchProperty));
        }
    }
}
