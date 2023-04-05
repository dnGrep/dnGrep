using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnGREP.WPF
{
    /// <summary>
    /// The ReplaceViewHighlighter is used to color the background of match occurrences to
    /// show which are selected for replacement; also outlines the currently selected match
    /// </summary>
    public class ReplaceViewHighlighter : IBackgroundRenderer
    {
        private readonly GrepSearchResult grepSearchResult;

        public ReplaceViewHighlighter(GrepSearchResult result)
        {
            grepSearchResult = result;

            outlinePen = new Pen(penBrush, 1);
            outlinePen.Freeze();
        }

        public GrepMatch? SelectedGrepMatch { get; set; }

        /// <summary>
        /// The ordered list of line numbers from the original source file
        /// </summary>
        public List<int> LineNumbers { get; } = new List<int>();

        private readonly Brush? skipBackground = Application.Current.Resources["Match.Skip.Background"] as Brush;
        private readonly Brush? replBackground = Application.Current.Resources["Match.Replace.Background"] as Brush;
        private readonly Brush? penBrush = Application.Current.Resources["Match.Skip.Foreground"] as Brush;
        private readonly Pen? outlinePen;

        /// <summary>Gets the layer on which this background renderer should draw.</summary>
        public KnownLayer Layer => KnownLayer.Selection; // draw behind selection

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (!textView.VisualLinesValid)
                return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            foreach (VisualLine visLine in textView.VisualLines)
            {
                var currentDocumentLine = visLine.FirstDocumentLine;
                int firstLineStart = currentDocumentLine.Offset;
                int currentDocumentLineStartOffset = currentDocumentLine.Offset;
                int currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
                int currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;

                HighlightLine(textView, drawingContext, visLine, currentDocumentLine);

                if (visLine.FirstDocumentLine != visLine.LastDocumentLine)
                {
                    foreach (VisualLineElement e in visLine.Elements.ToArray())
                    {
                        int elementOffset = firstLineStart + e.RelativeTextOffset;
                        if (elementOffset >= currentDocumentLineTotalEndOffset)
                        {
                            currentDocumentLine = textView.Document.GetLineByOffset(elementOffset);
                            currentDocumentLineStartOffset = currentDocumentLine.Offset;
                            currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
                            currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;
                            HighlightLine(textView, drawingContext, visLine, currentDocumentLine);
                        }
                    }
                }
            }
        }

        private void HighlightLine(TextView textView, DrawingContext drawingContext, VisualLine visLine, DocumentLine line)
        {
            int lineNumber = line.LineNumber;
            int lineIndex = lineNumber - 1;
            if (lineIndex >= 0 && lineIndex < LineNumbers.Count)
            {
                lineNumber = LineNumbers[lineIndex];
            }

            var lineResult = grepSearchResult.SearchResults.Find(sr => sr.LineNumber == lineNumber && sr.IsContext == false);
            if (lineResult != null)
            {
                foreach (var grepMatch in lineResult.Matches)
                {
                    // get the global file match corresponding to this line match
                    // only the file match has a valid ReplaceMatch flag
                    GrepMatch? fileMatch = grepSearchResult.Matches.FirstOrDefault(m => m.FileMatchId == grepMatch.FileMatchId);
                    bool isSelected = grepMatch.FileMatchId.Equals(SelectedGrepMatch?.FileMatchId, StringComparison.Ordinal);

                    Brush? markerBrush = fileMatch == null ? Brushes.LightGray : fileMatch.ReplaceMatch ? replBackground : skipBackground;
                    double markerCornerRadius = 2;
                    Pen? markerPen = isSelected ? outlinePen : null;
                    double markerPenThickness = markerPen != null ? markerPen.Thickness : 0;

                    int startOffset = grepMatch.StartLocation;
                    // match may include the non-printing newline chars at the end of the line, don't overflow the length
                    int endOffset = Math.Min(visLine.VisualLength, grepMatch.StartLocation + grepMatch.Length);

                    var rects = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, visLine, startOffset, endOffset);
                    if (rects.Any())
                    {
                        BackgroundGeometryBuilder geoBuilder = new()
                        {
                            AlignToWholePixels = true,
                            BorderThickness = markerPenThickness,
                            CornerRadius = markerCornerRadius
                        };
                        foreach (var rect in rects)
                        {
                            rect.Inflate(0, -1);
                            geoBuilder.AddRectangle(textView, rect);
                        }
                        Geometry geometry = geoBuilder.CreateGeometry();
                        if (geometry != null)
                        {
                            drawingContext.DrawGeometry(markerBrush, markerPen, geometry);
                        }
                    }
                }
            }
        }
    }
}
