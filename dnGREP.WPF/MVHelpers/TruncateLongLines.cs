using System.Windows;
using System.Windows.Media;
using dnGREP.Localization;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnGREP.WPF
{
    public class TruncateLongLines : VisualLineElementGenerator
    {
        private const int maxLength = 8000;
        private const int charactersAfterEllipsis = 10;

        public override int GetFirstInterestedOffset(int startOffset)
        {
            DocumentLine line = CurrentContext.VisualLine.LastDocumentLine;
            if (line.Length > maxLength)
            {
                int ellipsisOffset = line.Offset + maxLength - charactersAfterEllipsis - BigEllipsisColorizer.ellipsis.Length;
                if (startOffset <= ellipsisOffset)
                    return ellipsisOffset;
            }
            return -1;
        }


        public override VisualLineElement ConstructElement(int offset)
        {
            FormattedText formattedText = new(BigEllipsisColorizer.ellipsis,
                TranslationSource.Instance.CurrentCulture,
                CurrentContext.TextView.FlowDirection,
                CurrentContext.GlobalTextRunProperties.Typeface,
                CurrentContext.GlobalTextRunProperties.FontRenderingEmSize,
                Application.Current.Resources["AvalonEdit.BigEllipsis"] as Brush ?? Brushes.DeepSkyBlue,
                CurrentContext.GlobalTextRunProperties.PixelsPerDip);

            return new FormattedTextElement(formattedText, CurrentContext.VisualLine.LastDocumentLine.EndOffset - offset - charactersAfterEllipsis);
        }
    }
}
