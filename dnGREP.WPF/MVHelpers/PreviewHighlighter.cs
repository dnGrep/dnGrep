using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class PreviewHighlighter : DocumentColorizingTransformer
    {
        private GrepSearchResult result;
        private int firstLineNumber;
        public PreviewHighlighter(GrepSearchResult result, int firstLineNumber = 1)
        {
            this.result = result;
            this.firstLineNumber = firstLineNumber;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            if (result.Matches == null || result.Matches.Count == 0)
                return;

            var lineResult = result.SearchResults.Find(sr => (sr.LineNumber - firstLineNumber + 1) == line.LineNumber);

            if (lineResult != null)
            {
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
                                Brush br = element.TextRunProperties.BackgroundBrush;
                                // Replace the typeface with a modified version of
                                // the same typeface
                                element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Colors.Yellow));
                            });
                    }
                    catch (Exception ex)
                    {
                        // Do nothing
                    }
                }
            }
        }
    }
}
