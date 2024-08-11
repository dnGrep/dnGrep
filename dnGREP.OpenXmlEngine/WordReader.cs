using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NLog;

namespace dnGREP.Engines.OpenXml
{
    internal static class WordReader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        internal static void Initialize()
        {
            WordExtractFootnotes = GrepSettings.Instance.Get<bool>(GrepSettings.Key.WordExtractFootnotes);
            WordExtractComments = GrepSettings.Instance.Get<bool>(GrepSettings.Key.WordExtractComments);
            WordFootnoteReference = GrepSettings.Instance.Get<FootnoteRefType>(GrepSettings.Key.WordFootnoteReference);
            WordCommentReference = GrepSettings.Instance.Get<CommentRefType>(GrepSettings.Key.WordCommentReference);
            WordExtractHeaders = GrepSettings.Instance.Get<bool>(GrepSettings.Key.WordExtractHeaders);
            WordExtractFooters = GrepSettings.Instance.Get<bool>(GrepSettings.Key.WordExtractFooters);
            WordHeaderFooterPosition = GrepSettings.Instance.Get<HeaderFooterPosition>(GrepSettings.Key.WordHeaderFooterPosition);
        }

        internal static bool WordExtractFootnotes { get; private set; }
        internal static bool WordExtractComments { get; private set; }
        internal static bool WordExtractHeaders { get; private set; }
        internal static bool WordExtractFooters { get; private set; }
        internal static FootnoteRefType WordFootnoteReference { get; private set; }
        internal static CommentRefType WordCommentReference { get; private set; }
        internal static HeaderFooterPosition WordHeaderFooterPosition { get; private set; }
        internal static FootnoteRefType WordFootnoteNumber => FootnoteRefType.Parenthesis;

        public static string ExtractWordText(Stream stream,
            PauseCancelToken pauseCancelToken)
        {
            StringBuilder sb = new();

            // Open a given Word document as readonly
            using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, false))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                var docStyles = doc.MainDocumentPart?.StyleDefinitionsPart?.Styles?
                    .Where(r => r is Style).Select(r => r as Style);
                var sectionMap = GetSectionMap(body);
                SectionProperties sectionProperties = sectionMap.Peek().Item2 ?? new();
                SectionProperties lastSectionProps = sectionMap.Last().Item2;
                HashSet<string> completedReferences = [];

                WordListManager wlm = WordListManager.Empty;
                if (doc.MainDocumentPart?.NumberingDefinitionsPart != null && doc.MainDocumentPart.NumberingDefinitionsPart.Numbering != null)
                {
                    wlm = new WordListManager(doc.MainDocumentPart.NumberingDefinitionsPart.Numbering);
                }

                if (body != null && docStyles != null)
                {
                    ExtractText(doc, body, docStyles, sectionMap, ref sectionProperties, completedReferences, wlm, sb, pauseCancelToken);
                }

                if (doc.MainDocumentPart != null && WordExtractFootnotes)
                {
                    ExtractFootnotes(doc.MainDocumentPart, lastSectionProps, sb, pauseCancelToken);
                }

                if (WordExtractComments)
                {
                    ExtractComments(doc.MainDocumentPart, lastSectionProps, sb, pauseCancelToken);
                }

                if ((WordExtractHeaders || WordExtractFooters) && WordHeaderFooterPosition == HeaderFooterPosition.DocumentEnd)
                {
                    ExtractHeadersAndFooters(doc.MainDocumentPart, lastSectionProps, sb, pauseCancelToken);
                }
            }

            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

            return sb.ToString();
        }

        private static Queue<Tuple<Paragraph?, SectionProperties>> GetSectionMap(Body? body)
        {
            Queue<Tuple<Paragraph?, SectionProperties>> results = new();
            if (body == null)
            {
                results.Enqueue(new Tuple<Paragraph?, SectionProperties>(null, new()));
                return results;
            }

            var paragraphs = body.Elements<Paragraph>();
            foreach (var para in paragraphs)
            {
                var props = para.Descendants<SectionProperties>().FirstOrDefault();
                if (props != null)
                {
                    results.Enqueue(new Tuple<Paragraph?, SectionProperties>(para, props));
                }
            }

            var lastSectionProperties = body.Elements<SectionProperties>().FirstOrDefault();
            if (lastSectionProperties != null)
            {
                results.Enqueue(new Tuple<Paragraph?, SectionProperties>(null, lastSectionProperties));
            }
            else
            {
                results.Enqueue(new Tuple<Paragraph?, SectionProperties>(null, new()));
            }

            return results;
        }

        private static bool isInTableRow;
        private static void ExtractText(WordprocessingDocument doc, OpenXmlElement elem,
            IEnumerable<Style?> docStyles, Queue<Tuple<Paragraph?, SectionProperties>> sectionMap,
            ref SectionProperties sectionProperties, HashSet<string> completedReferences,
            WordListManager wlm, StringBuilder sb,
            PauseCancelToken pauseCancelToken)
        {
            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

            var lastSectionProps = sectionMap.Last().Item2;

            if (elem is Paragraph para)
            {
                if (WordExtractHeaders && WordHeaderFooterPosition == HeaderFooterPosition.SectionStart)
                {
                    AddSectionHeaders(doc, sectionProperties, lastSectionProps, completedReferences, sb, pauseCancelToken);
                }
                if (WordExtractFooters && WordHeaderFooterPosition == HeaderFooterPosition.SectionStart)
                {
                    AddSectionFooters(doc, sectionProperties, lastSectionProps, completedReferences, sb, pauseCancelToken);
                }

                string indent = GetIndent(para, docStyles);
                string fmtNum = wlm.GetFormattedNumber(para);

                if (isInTableRow)
                    sb.Append(indent).Append(fmtNum).Append(GetText(para, lastSectionProps, 0, pauseCancelToken)).Append('\t');
                else
                    sb.Append(indent).Append(fmtNum).AppendLine(GetText(para, lastSectionProps, 0, pauseCancelToken));

                if (sectionMap.TryPeek(out var sm) && sm.Item1 == para)
                {
                    _ = sectionMap.Dequeue();
                    sectionProperties = sectionMap.Peek().Item2;
                }
            }
            else if (elem is TableRow)
            {
                isInTableRow = true;

                sb.Append('\t');

                foreach (var child in elem)
                {
                    ExtractText(doc, child, docStyles, sectionMap, ref sectionProperties,
                        completedReferences, wlm, sb, pauseCancelToken);
                }

                sb.AppendLine();

                isInTableRow = false;
            }
            else
            {
                foreach (var child in elem)
                {
                    ExtractText(doc, child, docStyles, sectionMap, ref sectionProperties,
                        completedReferences, wlm, sb, pauseCancelToken);
                }
            }
        }

        private static void AddSectionHeaders(WordprocessingDocument doc, SectionProperties sectionProperties,
            SectionProperties lastSectionProps, HashSet<string> completedReferences, StringBuilder sb,
            PauseCancelToken pauseCancelToken)
        {
            foreach (HeaderReference headerRef in sectionProperties.Descendants<HeaderReference>())
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                try
                {
                    string? id = headerRef.Id?.Value;
                    if (id != null && !completedReferences.Contains(id))
                    {
                        if (doc.MainDocumentPart?.GetPartById(id) is HeaderPart headerPart)
                        {
                            completedReferences.Add(id);

                            if (headerPart.Header.Descendants<Run>().Any())
                            {
                                sb.AppendLine(@"▲───────────");
                                foreach (Paragraph hp in headerPart.Header.Elements<Paragraph>())
                                {
                                    sb.AppendLine(GetText(hp, lastSectionProps, 0, pauseCancelToken));
                                }
                                sb.AppendLine(@"──");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Add section headers");
                }
            }
        }

        private static void AddSectionFooters(WordprocessingDocument doc, SectionProperties sectionProperties,
            SectionProperties lastSectionProps, HashSet<string> completedReferences, StringBuilder sb,
            PauseCancelToken pauseCancelToken)
        {
            foreach (FooterReference footerRef in sectionProperties.Descendants<FooterReference>())
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                try
                {
                    string? id = footerRef.Id?.Value;
                    if (id != null && !completedReferences.Contains(id))
                    {
                        if (doc.MainDocumentPart?.GetPartById(id) is FooterPart footerPart)
                        {
                            completedReferences.Add(id);

                            if (footerPart.Footer.Descendants<Run>().Any())
                            {
                                sb.AppendLine(@"▼───────────");
                                foreach (Paragraph hp in footerPart.Footer.Elements<Paragraph>())
                                {
                                    sb.AppendLine(GetText(hp, lastSectionProps, 0, pauseCancelToken));
                                }
                                sb.AppendLine(@"──");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Add section footers");
                }
            }
        }

        private static string GetText(Paragraph para, SectionProperties sectionProperties,
            long noteId, PauseCancelToken pauseCancelToken)
        {
            StringBuilder sb = new();
            foreach (Run run in para.Descendants<Run>())
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                GetText(run, sectionProperties, noteId, sb, pauseCancelToken);
            }
            //foreach (var child in para.ChildElements)
            //{
            //    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

            //    Debug.WriteLine(child.GetType().Name);

            //    if (child is Run run)
            //    {
            //        GetText(run, sectionProperties, noteId, sb, pauseCancelToken);
            //    }
            //    else if (child is CommentRangeEnd ce)
            //    {
            //        sb.Append($"[{ce.Id}]");
            //    }
            //}
            return sb.ToString();
        }

        private static void GetText(Run run, SectionProperties sectionProperties, long noteId,
            StringBuilder sb, PauseCancelToken pauseCancelToken)
        {
            foreach (var child in run.ChildElements)
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                Debug.WriteLine(child.GetType().Name);
                if (child is Text text)
                {
                    sb.Append(text.Text);
                }
                else if (child is FootnoteReference fn)
                {
                    var props = sectionProperties.Elements<FootnoteProperties>().FirstOrDefault();
                    NumberFormatValues format = props?.NumberingFormat?.Val?.Value ??
                        NumberFormatValues.Decimal;
                    sb.Append(ValueFormatter.FormatValue(fn.Id?.Value ?? 0, format, WordFootnoteReference));
                }
                else if (child is FootnoteReferenceMark && noteId > 0)
                {
                    var props = sectionProperties.Elements<FootnoteProperties>().FirstOrDefault();
                    NumberFormatValues format = props?.NumberingFormat?.Val?.Value ??
                        NumberFormatValues.Decimal;
                    sb.Append(ValueFormatter.FormatValue(noteId, format, WordFootnoteNumber));
                }
                else if (child is EndnoteReference en)
                {
                    var props = sectionProperties.Elements<EndnoteProperties>().FirstOrDefault();
                    NumberFormatValues format = props?.NumberingFormat?.Val?.Value ??
                        NumberFormatValues.LowerRoman;
                    sb.Append(ValueFormatter.FormatValue(en.Id?.Value ?? 0, format, WordFootnoteReference));
                }
                else if (child is EndnoteReferenceMark && noteId > 0)
                {
                    var props = sectionProperties.Elements<EndnoteProperties>().FirstOrDefault();
                    NumberFormatValues format = props?.NumberingFormat?.Val?.Value ??
                        NumberFormatValues.LowerRoman;
                    sb.Append(ValueFormatter.FormatValue(noteId, format, WordFootnoteNumber));
                }
                else if (child is CommentReference cr && cr.Id != null && cr.Id.Value != null)
                {
                    sb.Append(ValueFormatter.FormatValue(cr.Id.Value, WordCommentReference));
                }
                else if (child is RunProperties)
                {
                    // skip
                }
                else
                {
                    foreach (var rn in child.Descendants<Run>())
                    {
                        GetText(rn, sectionProperties, noteId, sb, pauseCancelToken);
                    }
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

        private static void ExtractFootnotes(MainDocumentPart mainDoc, SectionProperties lastSectionProps,
            StringBuilder sb, PauseCancelToken pauseCancelToken)
        {
            if (mainDoc.FootnotesPart != null &&
                mainDoc.FootnotesPart.Footnotes.Cast<Footnote>().Any(fn => fn.Id?.Value > 0))
            {
                sb.AppendLine(@"────────────");
                foreach (Footnote note in mainDoc.FootnotesPart.Footnotes.Cast<Footnote>())
                {
                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                    if (note.Id?.Value > 0)
                    {
                        foreach (Paragraph para in note.Elements<Paragraph>())
                        {
                            sb.AppendLine(GetText(para, lastSectionProps, note.Id.Value, pauseCancelToken));
                        }
                    }
                }
            }

            if (mainDoc.EndnotesPart != null &&
                mainDoc.EndnotesPart.Endnotes.Cast<Endnote>().Any(en => en.Id?.Value > 0))
            {
                sb.AppendLine(@"────────────");
                foreach (Endnote note in mainDoc.EndnotesPart.Endnotes.Cast<Endnote>())
                {
                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                    if (note.Id?.Value > 0)
                    {
                        foreach (Paragraph para in note.Elements<Paragraph>())
                        {
                            sb.AppendLine(GetText(para, lastSectionProps, note.Id.Value, pauseCancelToken));
                        }
                    }
                }
            }
        }

        private static void ExtractComments(MainDocumentPart? mainDoc, SectionProperties lastSectionProps, StringBuilder sb, PauseCancelToken pauseCancelToken)
        {
            if (mainDoc != null &&
                mainDoc.WordprocessingCommentsPart != null)
            {
                if (mainDoc.WordprocessingCommentsPart.Comments.Any())
                {
                    sb.AppendLine(@"────────────");
                    foreach (Comment comment in mainDoc.WordprocessingCommentsPart.Comments.Cast<Comment>())
                    {
                        sb.AppendLine($"({comment.Id}) {comment.Author}:");
                        foreach (Paragraph para in comment.Elements<Paragraph>())
                        {
                            sb.AppendLine(GetText(para, lastSectionProps, 0, pauseCancelToken));
                        }
                    }
                }
            }
        }

        private static void ExtractHeadersAndFooters(MainDocumentPart? mainDocumentPart,
            SectionProperties lastSectionProps, StringBuilder sb, PauseCancelToken pauseCancelToken)
        {
            if (mainDocumentPart != null)
            {
                if (WordExtractHeaders)
                {
                    foreach (HeaderPart headerPart in mainDocumentPart.HeaderParts)
                    {
                        pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                        if (headerPart.Header.Descendants<Run>().Any())
                        {
                            sb.AppendLine(@"▲───────────");
                            foreach (Paragraph hp in headerPart.Header.Elements<Paragraph>())
                            {
                                sb.AppendLine(GetText(hp, lastSectionProps, 0, pauseCancelToken));
                            }
                            sb.AppendLine(@"──");
                        }
                    }
                }

                if (WordExtractFooters)
                {
                    foreach (FooterPart footerPart in mainDocumentPart.FooterParts)
                    {
                        pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                        if (footerPart.Footer.Descendants<Run>().Any())
                        {
                            sb.AppendLine(@"▼───────────");
                            foreach (Paragraph hp in footerPart.Footer.Elements<Paragraph>())
                            {
                                sb.AppendLine(GetText(hp, lastSectionProps, 0, pauseCancelToken));
                            }
                            sb.AppendLine(@"──");
                        }
                    }
                }
            }
        }
    }
}
