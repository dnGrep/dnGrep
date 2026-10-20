using System.Collections.Generic;
using System.Linq;

namespace dnGREP.Common
{
    // Data model for the replacement log: for each file that was (or would be)
    // changed by a search/replace, one entry per affected source line, showing the
    // line as it was and as it became.  These types hold no UI or engine code, so
    // the builder (dnGREP.Engines.ReplaceLogBuilder) and the text/html writers
    // can be tested without the UI.

    /// <summary>
    /// What a highlighted span of text in a log line represents.
    /// </summary>
    public enum ReplaceLogSpanKind
    {
        /// <summary>
        /// A match that was replaced.  In an old line this is the text that was removed,
        /// in a new line it is the text that was inserted.
        /// </summary>
        Replaced,

        /// <summary>
        /// A match that was left alone.  Shown in both the old and the new line.
        /// </summary>
        Skipped
    }

    /// <summary>
    /// What a line in the Results pane represents when it belongs to a replacement log.
    /// </summary>
    public enum ReplaceLogLineKind
    {
        /// <summary>An ordinary search result line.</summary>
        None,

        /// <summary>The line as it was.</summary>
        Old,

        /// <summary>The line as it became.</summary>
        New,

        /// <summary>An explanatory line, for example "replacement skipped".</summary>
        Note
    }

    /// <summary>
    /// A highlighted range inside <see cref="ReplaceLogLine.Text"/>.
    /// </summary>
    public record ReplaceLogSpan(int Start, int Length, ReplaceLogSpanKind Kind);

    /// <summary>
    /// One displayed line of the log: a line number, the text without any
    /// line ending, and the highlighted spans within that text.
    /// </summary>
    public class ReplaceLogLine
    {
        public ReplaceLogLine(int lineNumber, string text, IReadOnlyList<ReplaceLogSpan> spans)
        {
            LineNumber = lineNumber;
            Text = text;
            Spans = spans;
        }

        /// <summary>
        /// The source line number, or 0 for a continuation line (the number field is shown blank).
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// The line text, without a line ending.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Highlighted ranges, sorted by start, never overlapping, always within <see cref="Text"/>.
        /// </summary>
        public IReadOnlyList<ReplaceLogSpan> Spans { get; }

        /// <summary>
        /// Splits text into display lines (on \r\n, \n or \r) and distributes the spans over them,
        /// clipping any span that crosses a line break.  Used for original lines (which carry their
        /// own line ending) and for replaced text (which may contain line breaks the replacement added).
        /// The first returned line gets <paramref name="lineNumber"/>, any further lines get 0.
        /// A trailing line ending does not produce an extra empty line.
        /// </summary>
        /// <param name="text">text to split; may be empty</param>
        /// <param name="spans">spans in coordinates of <paramref name="text"/></param>
        /// <param name="lineNumber">line number for the first line</param>
        /// <returns>at least one line</returns>
        public static List<ReplaceLogLine> Split(string text, IEnumerable<ReplaceLogSpan> spans, int lineNumber)
        {
            List<ReplaceLogSpan> sorted = [.. spans.OrderBy(s => s.Start)];
            List<ReplaceLogLine> lines = [];

            int segStart = 0;
            int i = 0;
            while (i < text.Length) {
                char ch = text[i];
                if (ch == '\r' || ch == '\n') {
                    lines.Add(MakeLine(text, segStart, i - segStart, sorted, lines.Count == 0 ? lineNumber : 0));
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') {
                        i++;
                    }
                    i++;
                    segStart = i;
                }
                else {
                    i++;
                }
            }

            if (segStart < text.Length || lines.Count == 0) {
                lines.Add(MakeLine(text, segStart, text.Length - segStart, sorted, lines.Count == 0 ? lineNumber : 0));
            }

            return lines;
        }

        private static ReplaceLogLine MakeLine(string text, int start, int length, List<ReplaceLogSpan> sorted, int lineNumber)
        {
            List<ReplaceLogSpan> clipped = [];
            int end = start + length;
            foreach (ReplaceLogSpan span in sorted) {
                int s = System.Math.Max(span.Start, start);
                int e = System.Math.Min(span.Start + span.Length, end);
                if (e > s) {
                    clipped.Add(span with { Start = s - start, Length = e - s });
                }
            }
            return new ReplaceLogLine(lineNumber, text.Substring(start, length), clipped);
        }
    }

    /// <summary>
    /// The log entry for one affected source line.  A line with several matches is one
    /// entry that shows the combined result of all its marks.
    /// </summary>
    public class ReplaceLogEntry
    {
        public ReplaceLogEntry(int lineNumber, IReadOnlyList<ReplaceLogLine> oldLines,
            IReadOnlyList<ReplaceLogLine> newLines, int replacementCount, string? note = null, int skippedCount = 0)
        {
            SkippedCount = skippedCount;
            LineNumber = lineNumber;
            OldLines = oldLines;
            NewLines = newLines;
            ReplacementCount = replacementCount;
            Note = note;
        }

        /// <summary>
        /// The (first) source line number of this entry.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// The line(s) as they were; more than one only when a match spans lines.
        /// </summary>
        public IReadOnlyList<ReplaceLogLine> OldLines { get; }

        /// <summary>
        /// The line(s) as they became; more than one when the replacement contains line breaks.
        /// Empty when no match on the line was replaced, or when the new text could not be
        /// reproduced (see <see cref="Note"/>).
        /// </summary>
        public IReadOnlyList<ReplaceLogLine> NewLines { get; }

        /// <summary>
        /// The number of matches on the line that are marked for replacement.
        /// </summary>
        public int ReplacementCount { get; }

        /// <summary>
        /// The number of matches on the line that are not replaced.
        /// </summary>
        public int SkippedCount { get; }

        /// <summary>
        /// True if at least one match on the line is replaced.
        /// </summary>
        public bool IsReplaced => ReplacementCount > 0;

        /// <summary>
        /// A short explanation to show with the entry, or null.
        /// </summary>
        public string? Note { get; }
    }

    /// <summary>
    /// The log for one file.
    /// </summary>
    public class ReplaceLogFile
    {
        public ReplaceLogFile(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }

        /// <summary>
        /// Entries in file order.
        /// </summary>
        public List<ReplaceLogEntry> Entries { get; } = [];

        /// <summary>
        /// A note for the whole file (for example a write error), or null.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Total matches replaced in the file.
        /// </summary>
        public int ReplacementCount => Entries.Sum(e => e.ReplacementCount);

        /// <summary>
        /// Total matches left alone on the lines shown in the log.
        /// </summary>
        public int SkippedCount => Entries.Sum(e => e.SkippedCount);

        /// <summary>
        /// Number of lines with at least one replacement.
        /// </summary>
        public int ReplacedLineCount => Entries.Count(e => e.IsReplaced);
    }
}
