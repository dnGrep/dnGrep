using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnGREP.WPF
{
    public class BigEllipsisColorizer : DocumentColorizingTransformer
    {
        public static readonly string ellipsis = "•••";

        protected override void ColorizeLine(DocumentLine line)
        {
            Brush foreground = Application.Current.Resources["AvalonEdit.BigEllipsis"] as Brush ?? Brushes.DeepSkyBlue;
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            int start = 0;
            int index;
            while ((index = text.IndexOf(ellipsis, start, StringComparison.Ordinal)) >= 0)
            {
                ChangeLinePart(
                    lineStartOffset + index, // startOffset
                    lineStartOffset + index + ellipsis.Length, // endOffset
                    (VisualLineElement element) =>
                    {
                        // This lambda gets called once for every VisualLineElement
                        // between the specified offsets.
                        element.TextRunProperties.SetForegroundBrush(foreground);
                    });
                start = index + 1; // search for next occurrence
            }
        }
    }
}
