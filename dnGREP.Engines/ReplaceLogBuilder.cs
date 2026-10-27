using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;

namespace dnGREP.Engines
{
    /// <summary>
    /// Builds the replacement log (see ReplaceLog.cs) for one file, without touching the disk.
    ///
    /// The replaced text always comes from the engine's own replace code
    /// (GrepEnginePlainText.Replace), run in memory on one line at a time, so the log
    /// cannot disagree with what is written.  Spans (the highlighted old and new text) are
    /// derived from the length of each engine run's output, so they do not depend on any
    /// knowledge of how the engine builds replacement text.
    ///
    /// Not supported by the line-by-line approach, and handled by the positional fallback
    /// (original lines plus a note, no new text): the Multiline option, XPath and Hex search
    /// types, where the engine replaces in the whole file rather than line by line.
    /// </summary>
    public static class ReplaceLogBuilder
    {
        private const string NoteGuid = "replacement contains a GUID token; the GUID written to the file differs from any shown here";
        private const string NoteNotShown = "replaced text is not shown for multi-line or XPath replace";

        /// <summary>
        /// Builds the log for one file.
        /// </summary>
        /// <param name="fileName">name shown in the file header</param>
        /// <param name="fileText">the decoded file text, as the search saw it (no BOM character)</param>
        /// <param name="matches">all matches the search found in this file, with ReplaceMatch set
        /// to the user's decision for each. Must be the unfiltered list for the file: the regex
        /// engine pairs matches by their position in the line's full match list</param>
        /// <param name="searchPattern">the search pattern as used by the search</param>
        /// <param name="replacePattern">the raw text of the "Replace with" box; the special-character
        /// conversion that GrepCore.Replace applies is applied here as well</param>
        /// <param name="searchType">search type used for the search</param>
        /// <param name="searchOptions">search options used for the search</param>
        /// <param name="initParams">engine settings (fuzzy threshold); defaults if null</param>
        /// <param name="isDecided">if given, only lines that have at least one match for which this
        /// returns true get an entry (used for the live log, which shows only decided matches);
        /// all matches are still passed to the engine. Null means every line with a match</param>
        /// <returns>the log for the file; no entries if there are no matches</returns>
        public static ReplaceLogFile Build(string fileName, string fileText, IEnumerable<GrepMatch> matches,
            string searchPattern, string replacePattern, SearchType searchType, GrepSearchOption searchOptions,
            GrepEngineInitParams? initParams = null, Func<GrepMatch, bool>? isDecided = null)
        {
            ReplaceLogFile file = new(fileName);
            List<GrepMatch> all = [.. matches.OrderBy(m => m.StartLocation)];
            if (all.Count == 0) {
                return file;
            }

            bool wholeFile = searchOptions.HasFlag(GrepSearchOption.Multiline) ||
                searchType == SearchType.XPath || searchType == SearchType.Hex;

            if (wholeFile) {
                BuildPositional(file, fileText, all, isDecided);
                return file;
            }

            replacePattern = Utils.ReplaceSpecialCharacters(replacePattern);

            GrepEnginePlainText engine = new();
            engine.Initialize(initParams ?? GrepEngineInitParams.Default, FileFilter.Default, null);
            LineRunner runner = new(engine, searchPattern, replacePattern, searchType, searchOptions);

            string? guidNote = replacePattern.Contains("$(guid", StringComparison.OrdinalIgnoreCase) ? NoteGuid : null;

            Dictionary<int, List<GrepMatch>> byLine = [];
            foreach (GrepMatch m in all) {
                if (!byLine.TryGetValue(m.LineNumber, out List<GrepMatch>? list)) {
                    list = [];
                    byLine[m.LineNumber] = list;
                }
                list.Add(m);
            }

            // same line splitting as the engine: lines keep their line endings,
            // line numbers start at 1, file position is the running character count
            using StringReader stringReader = new(fileText);
            using EolReader reader = new(stringReader);
            int lineNumber = 1;
            int filePosition = 0;
            int remaining = byLine.Count;
            while (remaining > 0 && !reader.EndOfStream) {
                string? line = reader.ReadLine();
                if (line == null) {
                    continue;
                }

                if (byLine.TryGetValue(lineNumber, out List<GrepMatch>? lineMatches)) {
                    if (isDecided == null || lineMatches.Any(isDecided)) {
                        file.Entries.Add(BuildEntry(runner, lineNumber, filePosition, line, lineMatches, guidNote));
                    }
                    remaining--;
                }

                filePosition += line.Length;
                lineNumber++;
            }

            return file;
        }

        /// <summary>
        /// Builds the entry for one line: the old line with its spans, and, if any match on the line
        /// is marked, the new line(s) with their spans.
        /// </summary>
        /// <param name="runner">runs the engine on a single line</param>
        /// <param name="lineNumber">1-based line number</param>
        /// <param name="filePosition">file offset of the first character of the line</param>
        /// <param name="line">the line, including its line ending</param>
        /// <param name="lineMatches">all matches of the file that are on this line, sorted by start</param>
        /// <param name="guidNote">note to attach to replaced entries, or null</param>
        /// <returns>the entry</returns>
        private static ReplaceLogEntry BuildEntry(LineRunner runner, int lineNumber, int filePosition,
            string line, List<GrepMatch> lineMatches, string? guidNote)
        {
            // copies in line coordinates: the engine run below sees the line as line 1 at offset 0
            List<GrepMatch> local = [.. lineMatches.Select(m => new GrepMatch(m.FileMatchId, m.SearchPattern, 1,
                m.StartLocation - filePosition, m.Length, m.Groups, m.RegexMatchValue) { ReplaceMatch = m.ReplaceMatch })];

            List<int> marked = [.. Enumerable.Range(0, local.Count).Where(i => local[i].ReplaceMatch)];

            List<ReplaceLogSpan> oldSpans = [.. local.Select(m => new ReplaceLogSpan(m.StartLocation, m.Length,
                m.ReplaceMatch ? ReplaceLogSpanKind.Replaced : ReplaceLogSpanKind.Skipped))];
            List<ReplaceLogLine> oldLines = ReplaceLogLine.Split(line, oldSpans, lineNumber);

            if (marked.Count == 0) {
                return new ReplaceLogEntry(lineNumber, oldLines, [], 0, null, local.Count);
            }

            // one engine pass over the line with every marked match; the engine reports the
            // length of each replacement it wrote
            string newText = runner.Run(line, local, marked, out List<(int Start, int NewLength)> captured);
            Dictionary<int, int> newLengthByStart = [];
            foreach ((int start, int length) in captured) {
                newLengthByStart.TryAdd(start, length);
            }

            List<ReplaceLogSpan> newSpans = [];
            int delta = 0;
            for (int i = 0; i < local.Count; i++) {
                GrepMatch m = local[i];
                if (m.ReplaceMatch) {
                    int newLength = newLengthByStart.TryGetValue(m.StartLocation, out int len) ? len : m.Length;
                    newSpans.Add(new ReplaceLogSpan(m.StartLocation + delta, newLength, ReplaceLogSpanKind.Replaced));
                    delta += newLength - m.Length;
                }
                else {
                    newSpans.Add(new ReplaceLogSpan(m.StartLocation + delta, m.Length, ReplaceLogSpanKind.Skipped));
                }
            }

            List<ReplaceLogLine> newLines = ReplaceLogLine.Split(newText, newSpans, lineNumber);
            return new ReplaceLogEntry(lineNumber, oldLines, newLines, marked.Count, guidNote, local.Count - marked.Count);
        }

        /// <summary>
        /// Fallback for searches the engine replaces over the whole file (Multiline, XPath, Hex):
        /// one entry per match showing the original line(s) the match covers, with the match
        /// highlighted, and a note instead of the new text.  Matches whose position falls outside
        /// the text are left out.
        /// </summary>
        /// <param name="file">receives the entries</param>
        /// <param name="fileText">the file text</param>
        /// <param name="matches">matches sorted by start</param>
        /// <param name="isDecided">if given, matches for which it returns false are left out</param>
        private static void BuildPositional(ReplaceLogFile file, string fileText, List<GrepMatch> matches,
            Func<GrepMatch, bool>? isDecided)
        {
            List<(int Start, string Text)> lines = [];
            using (StringReader stringReader = new(fileText))
            using (EolReader reader = new(stringReader)) {
                int position = 0;
                while (!reader.EndOfStream) {
                    string? line = reader.ReadLine();
                    if (line == null) {
                        continue;
                    }
                    lines.Add((position, line));
                    position += line.Length;
                }
            }

            foreach (GrepMatch m in matches) {
                if (isDecided != null && !isDecided(m)) {
                    continue;
                }
                int first = FindLine(lines, m.StartLocation);
                if (first < 0) {
                    continue;
                }
                int last = FindLine(lines, Math.Max(m.StartLocation, m.EndPosition - 1));
                if (last < first) {
                    last = first;
                }

                ReplaceLogSpanKind kind = m.ReplaceMatch ? ReplaceLogSpanKind.Replaced : ReplaceLogSpanKind.Skipped;
                List<ReplaceLogLine> oldLines = [];
                for (int i = first; i <= last; i++) {
                    int s = Math.Max(m.StartLocation, lines[i].Start) - lines[i].Start;
                    int e = Math.Min(m.EndPosition, lines[i].Start + lines[i].Text.Length) - lines[i].Start;
                    List<ReplaceLogSpan> spans = e > s ? [new ReplaceLogSpan(s, e - s, kind)] : [];
                    // each source line has its own number, so split one line at a time
                    oldLines.AddRange(ReplaceLogLine.Split(lines[i].Text, spans, i + 1));
                }

                file.Entries.Add(new ReplaceLogEntry(first + 1, oldLines, [], m.ReplaceMatch ? 1 : 0,
                    m.ReplaceMatch ? NoteNotShown : null, m.ReplaceMatch ? 0 : 1));
            }
        }

        /// <summary>
        /// Finds the index of the line that contains the file offset.
        /// </summary>
        /// <param name="lines">line start offsets and texts (texts include line endings)</param>
        /// <param name="position">file offset</param>
        /// <returns>line index, or -1 if the offset is outside the text</returns>
        private static int FindLine(List<(int Start, string Text)> lines, int position)
        {
            for (int i = 0; i < lines.Count; i++) {
                if (position >= lines[i].Start && position < lines[i].Start + lines[i].Text.Length) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Runs the plain-text engine's replace on a single line held in memory.
        /// </summary>
        private sealed class LineRunner(GrepEnginePlainText engine, string searchPattern, string replacePattern,
            SearchType searchType, GrepSearchOption searchOptions)
        {
            // no BOM, and not Encoding.UTF8, so the engine neither writes nor looks for a BOM
            private static readonly UTF8Encoding encoding = new(false);

            /// <summary>
            /// Runs the engine on the line with only the matches at the given indexes marked.
            /// </summary>
            /// <param name="line">the line including its line ending</param>
            /// <param name="local">all matches on the line in line coordinates (line number 1)</param>
            /// <param name="marked">indexes into <paramref name="local"/> of the matches to replace</param>
            /// <returns>the line after replacement</returns>
            public string Run(string line, List<GrepMatch> local, IReadOnlyCollection<int> marked,
                out List<(int Start, int NewLength)> captured)
            {
                HashSet<int> markedSet = [.. marked];
                List<GrepMatch> items = [.. local.Select((m, i) => m.Copy(markedSet.Contains(i)))];

                captured = [];
                engine.CapturedReplaceSpans = captured;
                try {
                    using MemoryStream input = new(encoding.GetBytes(line));
                    using MemoryStream output = new();
                    engine.Replace(input, output, searchPattern, replacePattern, searchType, searchOptions, encoding, items);
                    return encoding.GetString(output.ToArray());
                }
                finally {
                    engine.CapturedReplaceSpans = null;
                }
            }
        }
    }
}
