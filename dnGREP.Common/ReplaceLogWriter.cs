using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using dnGREP.Localization;
using dnGREP.Localization.Properties;

namespace dnGREP.Common
{
    /// <summary>
    /// What the saved log says about the replace that produced it.
    /// </summary>
    /// <param name="SearchFor">the search text</param>
    /// <param name="ReplaceWith">the replacement text, as typed</param>
    /// <param name="OptionsText">the report header block that describes the search (search type,
    /// options, folder, file patterns), the same text the search Report starts with</param>
    /// <param name="Time">when the replace was done</param>
    public record ReplaceLogInfo(string SearchFor, string ReplaceWith, string OptionsText, DateTime Time);

    /// <summary>
    /// Colors for the HTML log, as CSS color strings (for example "#8A0000").
    /// </summary>
    public record ReplaceLogColors(string PageBackground, string PageForeground, string OldBackground,
        string NewBackground, string SkipBackground, string SkipForeground, string NumberForeground);

    /// <summary>
    /// Turns the replacement log (see ReplaceLog.cs) into saved text, csv and html.  These are pure
    /// functions over the log, so they can be tested without the UI.  The wording is English only.
    /// </summary>
    public static class ReplaceLogWriter
    {
        private const string Rule = "--------------------------------------------------------------------------------";

        /// <summary>
        /// The log as a plain text report: a header block, then for each file its path and
        /// the original and new lines of every affected line ("-" = was, "+" = became).
        /// </summary>
        /// <param name="files">the log, one item per file</param>
        /// <param name="info">search and replace settings for the header</param>
        /// <returns>the report text</returns>
        public static string ToText(IReadOnlyList<ReplaceLogFile> files, ReplaceLogInfo info)
        {
            StringBuilder sb = new();
            sb.Append(Resources.Main_DnGREP_Title).Append(' ');
            sb.AppendLine(Resources.ReplaceLogWriter_ReplaceLog).AppendLine();
            sb.AppendLine(info.Time.ToString("F"));
            sb.AppendLine();
            sb.AppendLine(info.OptionsText.TrimEnd());
            sb.AppendLine(TranslationSource.Format(Resources.ReplaceLogWriter_ReplaceWith, info.ReplaceWith));
            sb.AppendLine(Totals(files));
            sb.AppendLine();

            foreach (ReplaceLogFile file in files) {
                sb.AppendLine(Rule).AppendLine();
                sb.AppendLine(file.FileName);
                sb.AppendLine(FileSummary(file)).AppendLine();

                int width = NumberWidth(file);
                foreach (ReplaceLogEntry entry in file.Entries) {
                    // an entry with nothing replaced shows the same line as "was" and as "became",
                    // as the Results pane does; the highlighting alone shows which matches were unchanged
                    AppendTextLines(sb, '-', entry.OldLines, width);
                    AppendTextLines(sb, '+', entry.IsReplaced ? entry.NewLines : entry.OldLines, width);

                    if (!string.IsNullOrEmpty(entry.Note)) {
                        sb.Append("      ").AppendLine(entry.Note);
                    }
                    sb.AppendLine();
                }
                if (!string.IsNullOrEmpty(file.Note)) {
                    sb.AppendLine(file.Note).AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// The log as csv, one row per affected line: file, line number, counts, the old text and
        /// the new text (the same as the old text when nothing on the line was replaced), and the
        /// entry note, which explains why the new text is unavailable (for example multiline replacements).  Text
        /// is quoted, with the lines joined by a line feed.
        /// </summary>
        /// <param name="files">the log, one item per file</param>
        /// <returns>the csv text, with a header row</returns>
        public static string ToCsv(IReadOnlyList<ReplaceLogFile> files)
        {
            StringBuilder sb = new();
            sb.AppendLine(Resources.ReplaceLogWriter_HeaderText);
            foreach (ReplaceLogFile file in files) {
                foreach (ReplaceLogEntry entry in file.Entries) {
                    string oldText = string.Join("\n", entry.OldLines.Select(l => l.Text));
                    string newText = string.Join("\n", (entry.IsReplaced ? entry.NewLines : entry.OldLines).Select(l => l.Text));
                    sb.Append(Quote(file.FileName)).Append(',')
                      .Append(entry.LineNumber).Append(',')
                      .Append(entry.ReplacementCount).Append(',')
                      .Append(entry.SkippedCount).Append(',')
                      .Append(Quote(oldText)).Append(',')
                      .Append(Quote(newText)).Append(',')
                      .AppendLine(Quote(entry.Note ?? string.Empty));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// The log as one standalone html page: a style block (no external files or scripts), the same
        /// header and per-file lines as the text report, and the old and new text wrapped in
        /// span elements of class "old" and "new" ("skip" for matches that were unchanged).
        /// Spacing is kept with white-space: pre, and all text is html-encoded.
        /// </summary>
        /// <param name="files">the log, one item per file</param>
        /// <param name="info">search and replace settings for the header</param>
        /// <param name="colors">colors for the page and the highlights</param>
        /// <returns>the html page</returns>
        public static string ToHtml(IReadOnlyList<ReplaceLogFile> files, ReplaceLogInfo info, ReplaceLogColors colors)
        {
            StringBuilder sb = new();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine($"<html lang=\"{TranslationSource.Instance.CurrentCulture.IetfLanguageTag}\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.Append("<title>")
                .Append(Resources.Main_DnGREP_Title).Append(' ')
                .Append(Resources.ReplaceLogWriter_ReplaceLog)
                .AppendLine("</title>");
            sb.AppendLine("<style>");
            sb.AppendLine($"body {{ background: {colors.PageBackground}; color: {colors.PageForeground}; margin: 1em 2em; }}");
            sb.AppendLine("pre, .line { font-family: Consolas, 'Courier New', monospace; white-space: pre; margin: 0; }");
            sb.AppendLine("h1 { font-family: sans-serif; font-size: 1.4em; }");
            sb.AppendLine(".file { font-family: Consolas, 'Courier New', monospace; font-weight: bold; margin-top: 1.5em; border-top: 1px solid gray; padding-top: 0.5em; }");
            sb.AppendLine(".entry { margin: 0.6em 0 0.6em 1em; }");
            sb.AppendLine($".num {{ color: {colors.NumberForeground}; }}");
            sb.AppendLine($".old {{ background: {colors.OldBackground}; }}");
            sb.AppendLine($".new {{ background: {colors.NewBackground}; }}");
            sb.AppendLine($".skip {{ background: {colors.SkipBackground}; color: {colors.SkipForeground}; }}");
            sb.AppendLine(".note { font-style: italic; margin-left: 4em; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.Append("<h1>")
                .Append(Resources.Main_DnGREP_Title).Append(' ')
                .Append(Resources.ReplaceLogWriter_ReplaceLog)
                .AppendLine("</h1>");
            sb.Append("<pre>").Append(Enc(info.Time.ToString("F"))).AppendLine("</pre>");
            sb.AppendLine("<pre>");
            sb.AppendLine(Enc(info.OptionsText.TrimEnd()));
            sb.AppendLine(Enc(TranslationSource.Format(Resources.ReplaceLogWriter_ReplaceWith, info.ReplaceWith)));
            sb.AppendLine(Enc(Totals(files)));
            sb.AppendLine("</pre>");

            foreach (ReplaceLogFile file in files) {
                sb.Append("<div class=\"file\">").Append(Enc(file.FileName)).AppendLine("</div>");
                sb.Append("<pre>").Append(Enc(FileSummary(file))).AppendLine("</pre>");

                int width = NumberWidth(file);
                foreach (ReplaceLogEntry entry in file.Entries) {
                    sb.AppendLine("<div class=\"entry\">");
                    AppendHtmlLines(sb, '-', entry.OldLines, width, "old");
                    AppendHtmlLines(sb, '+', entry.IsReplaced ? entry.NewLines : entry.OldLines, width, "new");

                    if (!string.IsNullOrEmpty(entry.Note)) {
                        sb.Append("<div class=\"note\">").Append(Enc(entry.Note)).AppendLine("</div>");
                    }
                    sb.AppendLine("</div>");
                }
                if (!string.IsNullOrEmpty(file.Note)) {
                    sb.Append("<div class=\"note\">").Append(Enc(file.Note)).AppendLine("</div>");
                }
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        /// <summary>
        /// "Replaced N matches on M lines in K files" plus the number unchanged, if any.
        /// </summary>
        private static string Totals(IReadOnlyList<ReplaceLogFile> files)
        {
            int replaced = files.Sum(f => f.ReplacementCount);
            int lines = files.Sum(f => f.ReplacedLineCount);
            int skipped = files.Sum(f => f.SkippedCount);
            int fileCount = files.Count(f => f.ReplacementCount > 0);
            string text = TranslationSource.Format(Resources.ReplaceLogWriter_ReplacedMatches,
                replaced.ToString("#,##0"), lines.ToString("#,##0"), fileCount.ToString("#,##0"));
            return skipped > 0 ?
                TranslationSource.Format(Resources.ReplaceLogWriter_ReplacedMatchesUnchanged, text, skipped.ToString("#,##0")) :
                text;
        }

        private static string FileSummary(ReplaceLogFile file)
        {
            string text = TranslationSource.Format(Resources.ReplaceLogWriter_HasReplacements,
                file.ReplacementCount, file.ReplacedLineCount);
            return (file.SkippedCount > 0 ?
                TranslationSource.Format(Resources.ReplaceLogWriter_ReplacedMatchesUnchanged, text, file.SkippedCount) :
                text) + ":";
        }

        /// <summary>
        /// Digits needed for the largest line number shown for the file, so the numbers line up.
        /// </summary>
        private static int NumberWidth(ReplaceLogFile file)
        {
            int max = file.Entries.SelectMany(e => e.OldLines.Concat(e.NewLines)).Select(l => l.LineNumber).DefaultIfEmpty(0).Max();
            return Math.Max(1, max.ToString().Length);
        }

        /// <summary>
        /// One line of text per log line: sign, line number (blank on a continuation line), text.
        /// </summary>
        private static void AppendTextLines(StringBuilder sb, char sign, IReadOnlyList<ReplaceLogLine> lines, int width)
        {
            foreach (ReplaceLogLine line in lines) {
                string number = line.LineNumber > 0 ? line.LineNumber.ToString().PadLeft(width) : new string(' ', width);
                sb.Append(sign).Append(' ').Append(number).Append(": ").AppendLine(line.Text);
            }
        }

        /// <summary>
        /// One div per log line, with the matched ranges wrapped in spans.  A replaced range is
        /// class "old" in the old lines and class "new" in the new lines; a range unchanged is "skip".
        /// </summary>
        private static void AppendHtmlLines(StringBuilder sb, char sign, IReadOnlyList<ReplaceLogLine> lines, int width,
            string replacedClass)
        {
            foreach (ReplaceLogLine line in lines) {
                string number = line.LineNumber > 0 ? line.LineNumber.ToString().PadLeft(width) : new string(' ', width);
                sb.Append("<div class=\"line\"><span class=\"num\">").Append(sign).Append(' ').Append(number).Append(": </span>");

                int position = 0;
                foreach (ReplaceLogSpan span in line.Spans) {
                    if (span.Start > position) {
                        sb.Append(Enc(line.Text[position..span.Start]));
                    }
                    string cssClass = span.Kind == ReplaceLogSpanKind.Skipped ? "skip" : replacedClass;
                    sb.Append("<span class=\"").Append(cssClass).Append("\">")
                      .Append(Enc(line.Text.Substring(span.Start, span.Length))).Append("</span>");
                    position = span.Start + span.Length;
                }
                if (position < line.Text.Length) {
                    sb.Append(Enc(line.Text[position..]));
                }
                sb.AppendLine("</div>");
            }
        }

        private static string Enc(string text)
        {
            return WebUtility.HtmlEncode(text);
        }

        private static string Quote(string text)
        {
            return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }
    }
}
