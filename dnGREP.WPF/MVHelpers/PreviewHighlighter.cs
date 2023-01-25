using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnGREP.WPF
{
    public class PreviewHighlighter : IBackgroundRenderer
    {
        private readonly GrepSearchResult grepSearchResult;
        private readonly int[] lineNumbers;
        public PreviewHighlighter(GrepSearchResult result, int[] lineNumbers = null)
        {
            this.grepSearchResult = result;
            this.lineNumbers = lineNumbers;

            MarkerBrush = Application.Current.Resources["Match.Highlight.Background"] as Brush;
            MarkerPen = null;
            MarkerCornerRadius = 3.0;
        }

        /// <summary>Gets the layer on which this background renderer should draw.</summary>
        public KnownLayer Layer => KnownLayer.Selection; // draw behind selection
        public Brush MarkerBrush { get; set; }
        public Pen MarkerPen { get; set; }
        public double MarkerCornerRadius { get; set; }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException("textView");
            if (drawingContext == null)
                throw new ArgumentNullException("drawingContext");

            if (grepSearchResult == null || !textView.VisualLinesValid)
                return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            Brush markerBrush = MarkerBrush;
            Pen markerPen = MarkerPen;
            double markerCornerRadius = MarkerCornerRadius;
            double markerPenThickness = markerPen != null ? markerPen.Thickness : 0;

            foreach (VisualLine visLine in textView.VisualLines)
            {
                DocumentLine line = visLine.FirstDocumentLine;
                int lineNumber = line.LineNumber;
                if (lineNumbers != null && lineNumbers.Length > line.LineNumber - 1)
                    lineNumber = lineNumbers[line.LineNumber - 1];

                var lineResult = grepSearchResult.SearchResults.Find(sr => sr.LineNumber == lineNumber && sr.IsContext == false);
                if (lineResult != null)
                {
                    for (int i = 0; i < lineResult.Matches.Count; i++)
                    {
                        var grepMatch = lineResult.Matches[i];

                        int startOffset = grepMatch.StartLocation;
                        // match may include the non-printing newline chars at the end of the line, don't overflow the length
                        int endOffset = Math.Min(visLine.VisualLength, grepMatch.StartLocation + grepMatch.Length);

                        var rects = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, visLine, startOffset, endOffset);
                        if (rects.Any())
                        {
                            BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
                            geoBuilder.AlignToWholePixels = true;
                            geoBuilder.BorderThickness = markerPenThickness;
                            geoBuilder.CornerRadius = markerCornerRadius;
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
}
