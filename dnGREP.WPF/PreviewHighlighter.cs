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
        public PreviewHighlighter(GrepSearchResult result)
        {
            this.result = result;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);

            var lineResult = result.SearchResults.Find(sr => sr.LineNumber == line.LineNumber);

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
