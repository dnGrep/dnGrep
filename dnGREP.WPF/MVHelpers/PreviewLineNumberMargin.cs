using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnGREP.WPF
{
    /// <summary>
    /// The PreviewLineNumberMargin class is used to map line numbers from the 
    /// original source file to the page number.  If there are no page numbers, 
    /// then this class returns the base class implementation.
    /// </summary>
    public class PreviewLineNumberMargin : LineNumberMargin
    {
        public PreviewLineNumberMargin()
            : base()
        {
            // override Property Value Inheritance, and always render
            // the line number margin left-to-right
            FlowDirection = FlowDirection.LeftToRight;
        }

        /// <summary>
        /// The map of line numbers to page numbers from the original source file
        /// </summary>
        public Dictionary<int, int> LineToPageMap { get; } = new Dictionary<int, int>();

        protected override Size MeasureOverride(Size availableSize)
        {
            // always call the base class, it creates the typeface and enSize
            var size = base.MeasureOverride(availableSize);

            if (LineToPageMap.Count > 0)
            {
                double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                int digits = (int)Math.Floor(Math.Log10(LineToPageMap.Values.Max()) + 1);

                FormattedText text = new(
                    new('9', digits), CultureInfo.CurrentCulture, TextView.FlowDirection,
                    typeface, emSize, Brushes.Black, null,
                    TextOptions.GetTextFormattingMode(this), pixelsPerDip);
                return new Size(text.Width, 0);
            }
            else
            {
                return size;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (LineToPageMap.Count > 0)
            {
                TextView textView = TextView;
                Size renderSize = RenderSize;
                if (textView != null && textView.VisualLinesValid)
                {
                    var foreground = (Brush)GetValue(Control.ForegroundProperty);
                    double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

                    foreach (VisualLine line in textView.VisualLines)
                    {
                        int lineNumber = line.FirstDocumentLine.LineNumber;

                        if (LineToPageMap.TryGetValue(lineNumber, out int value))
                        {
                            FormattedText text = new(
                                            value.ToString(),
                                            CultureInfo.CurrentCulture,
                                            FlowDirection.LeftToRight,
                                            typeface,
                                            emSize,
                                            foreground,
                                            null,
                                            TextOptions.GetTextFormattingMode(this),
                                            pixelsPerDip);
                            double y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                            drawingContext.DrawText(text, new Point(renderSize.Width - text.Width, y - textView.VerticalOffset));
                        }
                    }
                }
            }
            else
            {
                base.OnRender(drawingContext);
            }
        }
    }
}
