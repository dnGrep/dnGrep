using System.Windows.Media;
using dnGREP.Localization;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnGREP.WPF
{
    public class TruncateLongLines : VisualLineElementGenerator
    {
        private const int maxLength = 8000;
        private const int charactersAfterEllipsis = 0;
        private readonly string ellipsis = "•••";

        public override int GetFirstInterestedOffset(int startOffset)
        {
            DocumentLine line = CurrentContext.VisualLine.LastDocumentLine;
            if (line.Length > maxLength)
            {
                int ellipsisOffset = line.Offset + maxLength - charactersAfterEllipsis - ellipsis.Length;
                if (startOffset <= ellipsisOffset)
                    return ellipsisOffset;
            }
            return -1;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            FormattedText formattedText = new(ellipsis,
                TranslationSource.Instance.CurrentCulture,
                CurrentContext.TextView.FlowDirection,
                CurrentContext.GlobalTextRunProperties.Typeface,
                CurrentContext.GlobalTextRunProperties.FontRenderingEmSize,
                Brushes.Firebrick,
                CurrentContext.GlobalTextRunProperties.PixelsPerDip);

            return new FormattedTextElement(formattedText, CurrentContext.VisualLine.LastDocumentLine.EndOffset - offset - charactersAfterEllipsis);
        }
    }
}
