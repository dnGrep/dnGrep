using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using NetDiff;

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

            outlinePen = new Pen(penBrush, 1.5);
            outlinePen.Freeze();

            transparentPen = new Pen(Brushes.Transparent, 0.0);
            transparentPen.Freeze();
        }

        public List<DiffPiece> DiffLines { get; } = [];

        /// <summary>
        /// The ordered list of line numbers from the original source file
        /// </summary>
        public List<int> LineNumbers { get; } = [];

        public GrepMatch? SelectedGrepMatch { get; set; }

        private readonly Brush? insertedLineBackground = Application.Current.Resources["DiffText.Inserted.Line.Background"] as Brush;
        private readonly Brush? deletedLineBackground = Application.Current.Resources["DiffText.Deleted.Line.Background"] as Brush;
        private readonly Brush? insertedWordBackground = Application.Current.Resources["DiffText.Inserted.Word.Background"] as Brush;
        private readonly Brush? deletedWordBackground = Application.Current.Resources["DiffText.Deleted.Word.Background"] as Brush;
        private readonly Brush? skipBackground = Application.Current.Resources["Match.Skip.Background"] as Brush;
        private readonly Brush? replBackground = Application.Current.Resources["Match.Replace.Background"] as Brush;
        private readonly Brush? penBrush = Application.Current.Resources["Match.Skip.Foreground"] as Brush;
        private readonly Pen outlinePen;
        private readonly Pen transparentPen;

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
                var lineNum = visLine.FirstDocumentLine.LineNumber - 1;

                if (DiffLines.Count > 0 && lineNum < DiffLines.Count)
                {
                    var diffLine = DiffLines[lineNum];
                    if (diffLine.Operation == DiffStatus.Inserted || diffLine.Operation == DiffStatus.Deleted)
                    {
                        DrawDiffBackground(diffLine, visLine, textView, drawingContext);
                    }
                }

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

            if (lineNumber < 1)
                return;

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

                    int startOffset = grepMatch.DisplayStartLocation;
                    // match may include the non-printing newline chars at the end of the line, don't overflow the length
                    int endOffset = Math.Min(visLine.VisualLength, grepMatch.DisplayStartLocation + grepMatch.Length);

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
                            rect.Inflate(1, -1);
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

        private void DrawDiffBackground(DiffPiece diffLine, VisualLine visLine,
            TextView textView, DrawingContext drawingContext)
        {
            var brush = diffLine.Operation == DiffStatus.Inserted ? insertedLineBackground : deletedLineBackground;

            foreach (var rc in BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, visLine, 0, 1000))
            {
                drawingContext.DrawRectangle(brush, transparentPen,
                    new Rect(0, rc.Top, textView.ActualWidth, rc.Height));
            }

            int startOffset = 0;
            int endOffset = 0;
            foreach (var piece in diffLine.SubPieces)
            {
                endOffset += string.IsNullOrEmpty(piece.Text) ? 0 : piece.Text.Length;

                if (piece.Operation != DiffStatus.Inserted && piece.Operation != DiffStatus.Deleted)
                {
                    startOffset = endOffset;
                    continue;
                }

                brush = piece.Operation == DiffStatus.Inserted ? insertedWordBackground : deletedWordBackground;

                var rects = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, visLine, startOffset, endOffset);
                if (rects.Any())
                {
                    BackgroundGeometryBuilder geoBuilder = new()
                    {
                        AlignToWholePixels = true,
                    };
                    foreach (var rect in rects)
                    {
                        geoBuilder.AddRectangle(textView, rect);
                    }
                    Geometry geometry = geoBuilder.CreateGeometry();
                    if (geometry != null)
                    {
                        drawingContext.DrawGeometry(brush, transparentPen, geometry);
                    }
                }

                startOffset = endOffset;
            }
        }
    }
}
