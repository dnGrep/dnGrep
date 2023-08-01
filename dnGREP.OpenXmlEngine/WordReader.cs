using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace dnGREP.Engines.OpenXml
{
    internal static class WordReader
    {
        public static string ExtractWordText(Stream stream, PauseCancelToken pauseCancelToken)
        {
            StringBuilder sb = new();

            // Open a given Word document as readonly
            using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, false))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                var docStyles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles?
                    .Where(r => r is Style).Select(r => r as Style);

                WordListManager wlm = WordListManager.Empty;
                if (doc.MainDocumentPart?.NumberingDefinitionsPart != null && doc.MainDocumentPart.NumberingDefinitionsPart.Numbering != null)
                {
                    wlm = new WordListManager(doc.MainDocumentPart.NumberingDefinitionsPart.Numbering);
                }
                if (body != null && docStyles != null)
                {
                    ExtractText(body, docStyles, wlm, sb, pauseCancelToken);
                }
            }

            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

            return sb.ToString();
        }

        private static bool isInTableRow;
        private static void ExtractText(OpenXmlElement elem, IEnumerable<Style?> docStyles,
            WordListManager wlm, StringBuilder sb, PauseCancelToken pauseCancelToken)
        {
            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

            if (elem is Paragraph para)
            {
                string indent = GetIndent(para, docStyles);
                string fmtNum = wlm.GetFormattedNumber(para);

                if (isInTableRow)
                    sb.Append(indent).Append(fmtNum).Append(elem.InnerText).Append('\t');
                else
                    sb.Append(indent).Append(fmtNum).AppendLine(elem.InnerText);
            }
            else if (elem is TableRow)
            {
                isInTableRow = true;

                sb.Append('\t');

                foreach (var child in elem)
                {
                    ExtractText(child, docStyles, wlm, sb, pauseCancelToken);
                }

                sb.AppendLine();

                isInTableRow = false;
            }
            else
            {
                foreach (var child in elem)
                {
                    ExtractText(child, docStyles, wlm, sb, pauseCancelToken);
                }
            }
        }

        private static string GetIndent(Paragraph para, IEnumerable<Style?> docStyles)
        {
            string indent = string.Empty;
            if (para != null && para.ParagraphProperties != null && para.ParagraphProperties.Indentation != null)
            {
                var indentation = para.ParagraphProperties.Indentation;
                if (indentation.Left != null && indentation.Left.HasValue)
                {
                    indent = WordListManager.TwipsToSpaces(indentation.Left);
                }
                else if (indentation.Start != null && indentation.Start.HasValue)
                {
                    indent = WordListManager.TwipsToSpaces(indentation.Start);
                }
            }
            if (para != null && para.ParagraphProperties != null && para.ParagraphProperties.ParagraphStyleId != null &&
                para.ParagraphProperties.ParagraphStyleId.Val != null &&
                para.ParagraphProperties.ParagraphStyleId.Val.HasValue)
            {
                var style = docStyles.Where(r => r?.StyleId == para.ParagraphProperties.ParagraphStyleId.Val.Value)
                    .Select(r => r).FirstOrDefault();

                if (style != null)
                {
                    var pp = style.Where(r => r is StyleParagraphProperties)
                        .Select(r => r as StyleParagraphProperties).FirstOrDefault();

                    if (pp != null && pp.Indentation != null)
                    {
                        if (pp.Indentation.Left != null && pp.Indentation.Left.HasValue)
                        {
                            indent = WordListManager.TwipsToSpaces(pp.Indentation.Left);
                        }
                        else if (pp.Indentation.Start != null && pp.Indentation.Start.HasValue)
                        {
                            indent = WordListManager.TwipsToSpaces(pp.Indentation.Start);
                        }
                    }
                }
            }

            return indent;
        }
    }
}
