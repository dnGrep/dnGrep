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
    /// The ReplaceViewLineNumberMargin class is used to map line numbers from the 
    /// original source file to the clipped file view showing matching lines and context
    /// lines only.  If the whole file is loaded in the text editor, then this class
    /// returns the base class implementation.
    /// </summary>
    public class ReplaceViewLineNumberMargin : LineNumberMargin
    {
        /// <summary>
        /// The ordered list of line numbers from the original source file
        /// </summary>
        public List<int> LineNumbers { get; } = new List<int>();

        protected override Size MeasureOverride(Size availableSize)
        {
            // always call the base class, it creates the typeface and enSize
            var size = base.MeasureOverride(availableSize);

            if (LineNumbers.Count > 0)
            {
                int digits = (int)Math.Floor(Math.Log10(LineNumbers.Max()) + 1);

                FormattedText text = new FormattedText(
                    new string('9', digits), CultureInfo.CurrentUICulture, TextView.FlowDirection,
                    typeface, emSize, Brushes.Black, 1.0);
                return new Size(text.Width, 0);
            }
            else
            {
                return size;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (LineNumbers.Count > 0)
            {
                TextView textView = TextView;
                Size renderSize = RenderSize;
                if (textView != null && textView.VisualLinesValid)
                {
                    var foreground = (Brush)GetValue(Control.ForegroundProperty);
                    double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

                    foreach (VisualLine line in textView.VisualLines)
                    {
                        int lineIndex = line.FirstDocumentLine.LineNumber - 1;

                        if (lineIndex >= 0 && lineIndex < LineNumbers.Count)
                        {
                            FormattedText text = new FormattedText(LineNumbers[lineIndex].ToString(),
                                CultureInfo.CurrentUICulture, textView.FlowDirection,
                                typeface, emSize, foreground, pixelsPerDip);

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
