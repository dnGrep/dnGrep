using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiffPlex.DiffBuilder.Model;
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
        private static readonly Brush? InsertedBackground;
        private static readonly Brush? DeletedBackground;
        private static readonly Pen BorderlessPen;

        private const double TextHorizontalMargin = 4.0;
        private double lineNumberFtWidth = 100;
        private double plusMinusFtWidth = 40;

        static ReplaceViewLineNumberMargin()
        {
            InsertedBackground = Application.Current.Resources["DiffText.Inserted.Line.Background"] as Brush;
            DeletedBackground = Application.Current.Resources["DiffText.Deleted.Line.Background"] as Brush;

            var transparentBrush = new SolidColorBrush(Colors.Transparent);
            transparentBrush.Freeze();

            BorderlessPen = new Pen(transparentBrush, 0.0);
            BorderlessPen.Freeze();
        }

        public ReplaceViewLineNumberMargin()
            : base()
        {
            // override Property Value Inheritance, and always render
            // the line number margin left-to-right
            FlowDirection = FlowDirection.LeftToRight;
        }

        /// <summary>
        /// The ordered list of line numbers from the original source file
        /// </summary>
        public List<int> LineNumbers { get; } = [];

        public List<DiffPiece> DiffLines { get; } = [];

        protected override Size MeasureOverride(Size availableSize)
        {
            // always call the base class, it creates the typeface and enSize
            var size = base.MeasureOverride(availableSize);

            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            if (DiffLines.Count > 0)
            {
                var textToUse = DiffLines.Last(l => l.Position.HasValue).Position.ToString();

                FormattedText lineFt = new(textToUse,
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, (double)GetValue(TextBlock.FontSizeProperty),
                    Brushes.Black, pixelsPerDip);

                FormattedText plusMinusFt = new("+ ",
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, (double)GetValue(TextBlock.FontSizeProperty),
                    Brushes.Black, pixelsPerDip);

                lineNumberFtWidth = lineFt.Width;
                plusMinusFtWidth = plusMinusFt.WidthIncludingTrailingWhitespace;

                // NB: This is a bit tricky. We use the margin control to actually
                // draw the diff "+/-" prefix, so that it's not selectable. So, looking
                // at this from the perspective of a single line, the arrangement is:
                //
                // margin-lineFt-margin-plusMinusFt
                return new Size(
                    lineNumberFtWidth + plusMinusFtWidth + (TextHorizontalMargin * 2.0),
                    0.0);
            }
            else if (LineNumbers.Count > 0)
            {
                int digits = (int)Math.Floor(Math.Log10(LineNumbers.Max()) + 1);

                FormattedText text = new(
                    new('9', digits), CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
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
            var foreground = (Brush)GetValue(Control.ForegroundProperty);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            if (DiffLines.Count > 0)
            {
                var lineNumberWidth = Math.Round(lineNumberFtWidth + TextHorizontalMargin * 2.0);

                var fontSize = (double)GetValue(TextBlock.FontSizeProperty);
                Size renderSize = RenderSize;

                var visualLines = TextView.VisualLinesValid ? TextView.VisualLines : Enumerable.Empty<VisualLine>();
                foreach (var line in visualLines)
                {
                    var rcs = BackgroundGeometryBuilder.GetRectsFromVisualSegment(TextView, line, 0, 1000).ToArray();
                    var lineNum = line.FirstDocumentLine.LineNumber - 1;
                    if (lineNum >= DiffLines.Count) continue;

                    var diffLine = DiffLines[lineNum];

                    FormattedText ft;

                    if (diffLine.Type == ChangeType.Inserted || diffLine.Type == ChangeType.Deleted)
                    {
                        var brush = diffLine.Type == ChangeType.Inserted ? InsertedBackground : DeletedBackground;

                        foreach (var rc in rcs)
                        {
                            drawingContext.DrawRectangle(brush, BorderlessPen, new Rect(0, rc.Top, ActualWidth, rc.Height));
                        }
                    }

                    ft = new FormattedText(diffLine.Position?.ToString() ?? string.Empty,
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, fontSize, foreground, pixelsPerDip);

                    drawingContext.DrawText(ft, new Point(renderSize.Width - ft.Width - plusMinusFtWidth, rcs[0].Top));

                    if (diffLine.Type == ChangeType.Inserted || diffLine.Type == ChangeType.Deleted)
                    {
                        var prefix = diffLine.Type == ChangeType.Inserted ? "+" : "-";
                        ft = new FormattedText(prefix,
                            CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                            typeface, fontSize, foreground, pixelsPerDip);

                        drawingContext.DrawText(ft, new Point(lineNumberWidth + TextHorizontalMargin, rcs[0].Top));
                    }
                }
            }
            else if (LineNumbers.Count > 0)
            {
                TextView textView = TextView;
                Size renderSize = RenderSize;
                if (textView != null && textView.VisualLinesValid)
                {
                    foreach (VisualLine line in textView.VisualLines)
                    {
                        int lineIndex = line.FirstDocumentLine.LineNumber - 1;

                        if (lineIndex >= 0 && lineIndex < LineNumbers.Count)
                        {
                            FormattedText text = new(
                                            LineNumbers[lineIndex].ToString(),
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
