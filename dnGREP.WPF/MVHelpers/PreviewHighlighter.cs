using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnGREP.WPF
{
    public class PreviewHighlighter(GrepSearchResult result) : IBackgroundRenderer
    {
        private readonly GrepSearchResult grepSearchResult = result;
        private readonly Brush? markerBrush = Application.Current.Resources["Match.Highlight.Background"] as Brush;
        private readonly Pen? markerPen = null;
        private readonly double markerCornerRadius = 3;
        private readonly double markerPenThickness = 0;

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

            var lineResult = grepSearchResult.SearchResults.Find(sr => sr.LineNumber == lineNumber && sr.IsContext == false);
            if (lineResult != null)
            {
                for (int i = 0; i < lineResult.Matches.Count; i++)
                {
                    var grepMatch = lineResult.Matches[i];

                    // Convert document text offsets to visual columns.
                    // This accounts for elements like TabTextElement that have
                    // a visual length (2) different from their document length (1).
                    int startVC = visLine.GetVisualColumn(grepMatch.StartLocation);
                    int endVC = Math.Min(visLine.VisualLength,
                        visLine.GetVisualColumn(Math.Min(grepMatch.StartLocation + grepMatch.Length,
                            line.Length)));

                    var rects = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, visLine, startVC, endVC);
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