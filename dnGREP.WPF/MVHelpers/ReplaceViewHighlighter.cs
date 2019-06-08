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
    /// show which are selected for replacement; also underlines the currently selected match
    /// </summary>
    public class ReplaceViewHighlighter : DocumentColorizingTransformer
    {
        private GrepSearchResult result;

        public ReplaceViewHighlighter(GrepSearchResult result)
        {
            this.result = result;
        }

        public GrepMatch SelectedGrepMatch { get; set; }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (result.Matches == null || result.Matches.Count == 0)
                return;

            int lineStartOffset = line.Offset;
            int lineNumber = line.LineNumber;

            var lineResult = result.SearchResults.FirstOrDefault(sr => sr.ClippedFileLineNumber == lineNumber && sr.IsContext == false);
            if (lineResult != null)
            {
                Brush skipBackground = Application.Current.Resources["Match.Skip.Background"] as Brush;
                Brush skipForeground = Application.Current.Resources["Match.Skip.Foreground"] as Brush;
                Brush replBackground = Application.Current.Resources["Match.Replace.Background"] as Brush;
                Brush replForeground = Application.Current.Resources["Match.Replace.Foreground"] as Brush;

                foreach (var grepMatch in lineResult.Matches)
                {
                    try
                    {
                        // get the global file match corresponding to this line match
                        // only the file match has a valid ReplaceMatch flag
                        GrepMatch fileMatch = result.Matches.FirstOrDefault(m => m.FileMatchId == grepMatch.FileMatchId);
                        Brush foreground = fileMatch == null ? Brushes.Black : fileMatch.ReplaceMatch ? replForeground : skipForeground;
                        Brush background = fileMatch == null ? Brushes.LightGray : fileMatch.ReplaceMatch ? replBackground : skipBackground;

                        bool isSelected = grepMatch.FileMatchId.Equals(SelectedGrepMatch.FileMatchId);

                        base.ChangeLinePart(
                            lineStartOffset + grepMatch.StartLocation, // startOffset
                            lineStartOffset + grepMatch.StartLocation + grepMatch.Length, // endOffset
                            (VisualLineElement element) =>
                            {
                                // This lambda gets called once for every VisualLineElement
                                // between the specified offsets.
                                element.TextRunProperties.SetBackgroundBrush(background);
                                element.TextRunProperties.SetForegroundBrush(foreground);

                                if (isSelected)
                                {
                                    TextDecorationCollection coll = new TextDecorationCollection
                                    {
                                        new TextDecoration(
                                            TextDecorationLocation.Underline,
                                            new Pen(foreground, 2), 0,
                                            TextDecorationUnit.Pixel,
                                            TextDecorationUnit.Pixel)
                                    };
                                    element.TextRunProperties.SetTextDecorations(coll);
                                }
                            });
                    }
                    catch
                    {
                        // Do nothing
                    }
                }
            }
        }
    }
}
