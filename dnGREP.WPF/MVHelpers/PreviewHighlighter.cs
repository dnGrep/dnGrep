using System.Windows;
using System.Windows.Media;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnGREP.WPF
{
    public class PreviewHighlighter : DocumentColorizingTransformer
    {
        private GrepSearchResult result;
        private int[] lineNumbers;
        public PreviewHighlighter(GrepSearchResult result, int[] lineNumbers = null)
        {
            this.result = result;
            this.lineNumbers = lineNumbers;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            if (result == null || result.Matches == null || result.Matches.Count == 0)
                return;

            int lineNumber = line.LineNumber;
            if (lineNumbers != null && lineNumbers.Length > line.LineNumber - 1)
                lineNumber = lineNumbers[line.LineNumber - 1];

            var lineResult = result.SearchResults.Find(sr => sr.LineNumber == lineNumber && sr.IsContext == false);

            if (lineResult != null)
            {
                Brush background = Application.Current.Resources["Match.Highlight.Background"] as Brush;
                Brush foreground = Application.Current.Resources["Match.Highlight.Foreground"] as Brush;

                for (int i = 0; i < lineResult.Matches.Count; i++)
                {
                    try
                    {
                        var grepMatch = lineResult.Matches[i];

                        base.ChangeLinePart(
                            lineStartOffset + grepMatch.StartLocation, // startOffset
                            lineStartOffset + grepMatch.StartLocation + grepMatch.Length, // endOffset
                            (VisualLineElement element) =>
                            {
                                // This lambda gets called once for every VisualLineElement
                                // between the specified offsets.
                                element.TextRunProperties.SetBackgroundBrush(background);
                                element.TextRunProperties.SetForegroundBrush(foreground);
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
