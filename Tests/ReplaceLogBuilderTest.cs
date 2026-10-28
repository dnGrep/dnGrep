using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;
using dnGREP.Engines;
using Xunit;

namespace Tests
{
    // Phase 1 tests for the replacement log builder and its model.
    // Written without being able to compile or run them; first run on Windows may need fixes.
    public class ReplaceLogBuilderTest : TestBase
    {
        private static readonly UTF8Encoding utf8 = new(false);

        private static GrepEnginePlainText NewEngine()
        {
            GrepEnginePlainText engine = new();
            engine.Initialize(GrepEngineInitParams.Default, FileFilter.Default, null);
            return engine;
        }

        /// <summary>
        /// Runs a real search over the text, so the matches look like the ones the UI would hold.
        /// </summary>
        private static List<GrepMatch> Search(string text, string pattern, SearchType type, GrepSearchOption options)
        {
            using MemoryStream stream = new(utf8.GetBytes(text));
            List<GrepSearchResult> results = NewEngine().Search(stream, new FileData("test.txt"), pattern, type, options, utf8);
            return results.Count == 0 ? [] : results[0].Matches;
        }

        /// <summary>
        /// Runs the engine's replace over the whole text, as GrepCore does for a file.
        /// </summary>
        private static string ReplaceWholeFile(string text, string pattern, string replace, SearchType type,
            GrepSearchOption options, List<GrepMatch> matches)
        {
            using MemoryStream input = new(utf8.GetBytes(text));
            using MemoryStream output = new();
            NewEngine().Replace(input, output, pattern, Utils.ReplaceSpecialCharacters(replace), type, options, utf8,
                matches.Select(m => m.Copy(m.ReplaceMatch)).ToList());
            return utf8.GetString(output.ToArray());
        }

        private static string TextOf(ReplaceLogLine line, ReplaceLogSpan span)
        {
            return line.Text.Substring(span.Start, span.Length);
        }

        [Fact]
        public void SplitHandlesLineEndingsAndSpans()
        {
            var spans = new[] { new ReplaceLogSpan(1, 4, ReplaceLogSpanKind.Replaced) };

            // span "b\r\nc" crosses a line break and is clipped to each side
            List<ReplaceLogLine> lines = ReplaceLogLine.Split("ab\r\ncd\n", spans, 7);
            Assert.Equal(2, lines.Count);
            Assert.Equal(7, lines[0].LineNumber);
            Assert.Equal("ab", lines[0].Text);
            Assert.Equal(0, lines[1].LineNumber);
            Assert.Equal("cd", lines[1].Text);
            Assert.Equal("b", TextOf(lines[0], lines[0].Spans[0]));
            Assert.Equal("c", TextOf(lines[1], lines[1].Spans[0]));
        }

        [Fact]
        public void SplitOfEmptyTextGivesOneEmptyLine()
        {
            List<ReplaceLogLine> lines = ReplaceLogLine.Split("", [], 3);
            Assert.Single(lines);
            Assert.Equal(3, lines[0].LineNumber);
            Assert.Equal("", lines[0].Text);
        }

        [Fact]
        public void SplitOfBlankLineKeepsIt()
        {
            List<ReplaceLogLine> lines = ReplaceLogLine.Split("\n", [], 3);
            Assert.Single(lines);
            Assert.Equal("", lines[0].Text);
        }

        [Fact]
        public void ReplacesSingleMatchWithSpans()
        {
            string text = "int a;\r\nbool gdi_plus::build_pargb(void)\r\nint b;\r\n";
            List<GrepMatch> matches = Search(text, "pargb", SearchType.PlainText, GrepSearchOption.Global);
            Assert.Single(matches);
            matches[0].ReplaceMatch = true;

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, "pargb", "pre_argb",
                SearchType.PlainText, GrepSearchOption.Global);

            ReplaceLogEntry entry = Assert.Single(log.Entries);
            Assert.Equal(2, entry.LineNumber);
            Assert.True(entry.IsReplaced);
            Assert.Equal(1, entry.ReplacementCount);

            ReplaceLogLine oldLine = Assert.Single(entry.OldLines);
            Assert.Equal("bool gdi_plus::build_pargb(void)", oldLine.Text);
            ReplaceLogSpan oldSpan = Assert.Single(oldLine.Spans);
            Assert.Equal(ReplaceLogSpanKind.Replaced, oldSpan.Kind);
            Assert.Equal("pargb", TextOf(oldLine, oldSpan));

            ReplaceLogLine newLine = Assert.Single(entry.NewLines);
            Assert.Equal(2, newLine.LineNumber);
            Assert.Equal("bool gdi_plus::build_pre_argb(void)", newLine.Text);
            ReplaceLogSpan newSpan = Assert.Single(newLine.Spans);
            Assert.Equal("pre_argb", TextOf(newLine, newSpan));
        }

        [Fact]
        public void SeveralMatchesOnOneLineWithOneSkipped()
        {
            string text = "foo bar foo baz foo\n";
            List<GrepMatch> matches = Search(text, "foo", SearchType.PlainText, GrepSearchOption.Global);
            Assert.Equal(3, matches.Count);
            matches[0].ReplaceMatch = true;
            matches[1].ReplaceMatch = false;
            matches[2].ReplaceMatch = true;

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, "foo", "quux!",
                SearchType.PlainText, GrepSearchOption.Global);

            ReplaceLogEntry entry = Assert.Single(log.Entries);
            Assert.Equal(2, entry.ReplacementCount);
            Assert.Equal(1, entry.SkippedCount);
            ReplaceLogLine newLine = Assert.Single(entry.NewLines);
            Assert.Equal("quux! bar foo baz quux!", newLine.Text);
            Assert.Equal(3, newLine.Spans.Count);
            Assert.Equal(ReplaceLogSpanKind.Replaced, newLine.Spans[0].Kind);
            Assert.Equal("quux!", TextOf(newLine, newLine.Spans[0]));
            Assert.Equal(ReplaceLogSpanKind.Skipped, newLine.Spans[1].Kind);
            Assert.Equal("foo", TextOf(newLine, newLine.Spans[1]));
            Assert.Equal(ReplaceLogSpanKind.Replaced, newLine.Spans[2].Kind);
            Assert.Equal("quux!", TextOf(newLine, newLine.Spans[2]));

            ReplaceLogLine oldLine = Assert.Single(entry.OldLines);
            Assert.Equal("foo bar foo baz foo", oldLine.Text);
            Assert.Equal(new[] { 0, 8, 16 }, oldLine.Spans.Select(s => s.Start).ToArray());
        }

        [Fact]
        public void AllSkippedGivesNoNewLines()
        {
            string text = "foo\nbar foo\n";
            List<GrepMatch> matches = Search(text, "foo", SearchType.PlainText, GrepSearchOption.Global);
            Assert.Equal(2, matches.Count);

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, "foo", "x",
                SearchType.PlainText, GrepSearchOption.Global);

            Assert.Equal(2, log.Entries.Count);
            Assert.All(log.Entries, e =>
            {
                Assert.False(e.IsReplaced);
                Assert.Empty(e.NewLines);
                Assert.Equal(ReplaceLogSpanKind.Skipped, Assert.Single(Assert.Single(e.OldLines).Spans).Kind);
            });
            Assert.Equal(0, log.ReplacementCount);
        }

        [Fact]
        public void RegexCaptureGroupsAreExpanded()
        {
            string text = "x = get_value();\nget_other();\n";
            List<GrepMatch> matches = Search(text, @"get_(\w+)\(", SearchType.Regex, GrepSearchOption.Global);
            Assert.Equal(2, matches.Count);
            matches.ForEach(m => m.ReplaceMatch = true);

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, @"get_(\w+)\(", "fetch_$1(",
                SearchType.Regex, GrepSearchOption.Global);

            Assert.Equal("x = fetch_value();", log.Entries[0].NewLines[0].Text);
            Assert.Equal("fetch_value(", TextOf(log.Entries[0].NewLines[0], log.Entries[0].NewLines[0].Spans[0]));
            Assert.Equal("fetch_other();", log.Entries[1].NewLines[0].Text);
        }

        [Fact]
        public void RegexPartialMarksOnOneLine()
        {
            // the regex engine pairs marks by position in the line's full match list
            string text = "a1 a2 a3\n";
            List<GrepMatch> matches = Search(text, @"a\d", SearchType.Regex, GrepSearchOption.Global);
            Assert.Equal(3, matches.Count);
            matches[1].ReplaceMatch = true;

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, @"a\d", "bb",
                SearchType.Regex, GrepSearchOption.Global);

            ReplaceLogEntry entry = Assert.Single(log.Entries);
            Assert.Equal("a1 bb a3", entry.NewLines[0].Text);
            Assert.Equal(1, entry.ReplacementCount);
        }

        [Fact]
        public void ReplacementWithLineBreakGivesContinuationLine()
        {
            string text = "one;two\nthree\n";
            List<GrepMatch> matches = Search(text, ";", SearchType.PlainText, GrepSearchOption.Global);
            matches.ForEach(m => m.ReplaceMatch = true);

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, ";", @"\\n",
                SearchType.PlainText, GrepSearchOption.Global);

            ReplaceLogEntry entry = Assert.Single(log.Entries);
            Assert.Equal(2, entry.NewLines.Count);
            Assert.Equal(1, entry.NewLines[0].LineNumber);
            Assert.Equal("one", entry.NewLines[0].Text);
            Assert.Equal(0, entry.NewLines[1].LineNumber);
            Assert.Equal("two", entry.NewLines[1].Text);
        }

        [Fact]
        public void GuidTokenAddsNote()
        {
            string text = "id\n";
            List<GrepMatch> matches = Search(text, "id", SearchType.PlainText, GrepSearchOption.Global);
            matches[0].ReplaceMatch = true;

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, "id", "$(guid)",
                SearchType.PlainText, GrepSearchOption.Global);

            ReplaceLogEntry entry = Assert.Single(log.Entries);
            Assert.NotNull(entry.Note);
            Assert.Contains("GUID", entry.Note);
        }

        [Fact]
        public void LogAgreesWithWholeFileReplace()
        {
            // rebuild the file from the log and compare with the engine's own whole-file replace
            string text = "alpha beta\nbeta beta\ngamma\nbeta\n";
            List<GrepMatch> matches = Search(text, "beta", SearchType.PlainText, GrepSearchOption.Global);
            Assert.Equal(4, matches.Count);
            matches[0].ReplaceMatch = true;
            matches[2].ReplaceMatch = true;
            matches[3].ReplaceMatch = true;

            string expected = ReplaceWholeFile(text, "beta", "DELTA", SearchType.PlainText, GrepSearchOption.Global, matches);

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, "beta", "DELTA",
                SearchType.PlainText, GrepSearchOption.Global);

            string[] sourceLines = text.Split('\n');
            StringBuilder rebuilt = new();
            for (int i = 0; i < sourceLines.Length - 1; i++)
            {
                ReplaceLogEntry? entry = log.Entries.FirstOrDefault(e => e.LineNumber == i + 1);
                if (entry != null && entry.IsReplaced)
                {
                    rebuilt.Append(string.Join("\n", entry.NewLines.Select(l => l.Text)));
                }
                else
                {
                    rebuilt.Append(sourceLines[i]);
                }
                rebuilt.Append('\n');
            }

            Assert.Equal(expected, rebuilt.ToString());
        }

        [Fact]
        public void MultilineSearchFallsBackToOriginalLinesWithNote()
        {
            string text = "start\nmiddle\nend\n";
            List<GrepMatch> matches = Search(text, @"start\nmiddle", SearchType.Regex,
                GrepSearchOption.Global | GrepSearchOption.Multiline);
            Assert.Single(matches);
            matches[0].ReplaceMatch = true;

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, @"start\nmiddle", "x",
                SearchType.Regex, GrepSearchOption.Global | GrepSearchOption.Multiline);

            ReplaceLogEntry entry = Assert.Single(log.Entries);
            Assert.Equal(2, entry.OldLines.Count);
            Assert.Equal("start", entry.OldLines[0].Text);
            Assert.Equal("middle", entry.OldLines[1].Text);
            Assert.Equal(1, entry.OldLines[0].LineNumber);
            Assert.Equal(2, entry.OldLines[1].LineNumber);
            Assert.Empty(entry.NewLines);
            Assert.NotNull(entry.Note);
        }

        [Fact]
        public void NoMatchesGivesEmptyLog()
        {
            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", "abc\n", [], "x", "y",
                SearchType.PlainText, GrepSearchOption.Global);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public void WholeFileModeCountsDistinctLines()
        {
            // the Multiline option makes the builder use one entry per match, so the two matches
            // on line 1 are two entries, but only one line
            string text = "foo foo\nbar\n";
            GrepSearchOption options = GrepSearchOption.Global | GrepSearchOption.Multiline;
            List<GrepMatch> matches = Search(text, "foo", SearchType.PlainText, options);
            Assert.Equal(2, matches.Count);
            matches.ForEach(m => m.ReplaceMatch = true);

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, "foo", "x",
                SearchType.PlainText, options);

            Assert.Equal(2, log.Entries.Count);
            Assert.Equal(2, log.ReplacementCount);
            Assert.Equal(1, log.ReplacedLineCount);
            Assert.Equal(1, log.LineCount);
        }

        [Fact]
        public void LineCountsIncludeSkippedLinesButReplacedCountDoesNot()
        {
            string text = "foo\nbar foo\nbaz\n";
            List<GrepMatch> matches = Search(text, "foo", SearchType.PlainText, GrepSearchOption.Global);
            Assert.Equal(2, matches.Count);
            matches[0].ReplaceMatch = true;

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, "foo", "x",
                SearchType.PlainText, GrepSearchOption.Global);

            Assert.Equal(1, log.ReplacedLineCount);
            Assert.Equal(2, log.LineCount);
        }

        [Fact]
        public void FileEncodingIsCarriedOnTheLog()
        {
            string text = "foo\n";
            List<GrepMatch> matches = Search(text, "foo", SearchType.PlainText, GrepSearchOption.Global);

            ReplaceLogFile withEncoding = ReplaceLogBuilder.Build("test.txt", text, matches, "foo", "x",
                SearchType.PlainText, GrepSearchOption.Global, encoding: Encoding.Latin1);
            // encoding is determined from the file or falls back to UTF-8
            ReplaceLogFile without = ReplaceLogBuilder.Build("test.txt", text, matches, "foo", "x",
                SearchType.PlainText, GrepSearchOption.Global);

            Assert.Same(Encoding.Latin1, withEncoding.Encoding);
            Assert.Same(Encoding.UTF8, without.Encoding);
        }

        [Fact]
        public void UniformReplacementSpansCoverTheInsertedText()
        {
            // two of three matches replaced with text longer than the match: the new spans must
            // sit on the inserted text and on the skipped match, not drift
            string text = "foo bar foo baz foo\n";
            List<GrepMatch> matches = Search(text, "foo", SearchType.PlainText, GrepSearchOption.Global);
            Assert.Equal(3, matches.Count);
            matches[0].ReplaceMatch = true;
            matches[2].ReplaceMatch = true;

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, "foo", "longer text",
                SearchType.PlainText, GrepSearchOption.Global);

            ReplaceLogLine newLine = Assert.Single(Assert.Single(log.Entries).NewLines);
            Assert.Equal("longer text bar foo baz longer text", newLine.Text);
            Assert.Equal(new[] { "longer text", "foo", "longer text" },
                newLine.Spans.Select(s => TextOf(newLine, s)).ToArray());
            Assert.Equal(new[] { ReplaceLogSpanKind.Replaced, ReplaceLogSpanKind.Skipped, ReplaceLogSpanKind.Replaced },
                newLine.Spans.Select(s => s.Kind).ToArray());
        }

        [Fact]
        public void RegexReplacementsOfDifferentLengthGetTheirOwnSpans()
        {
            // regex replacements depend on the capture groups, so each match has its own length
            string text = "a1 bb22 c333\n";
            List<GrepMatch> matches = Search(text, @"[a-z]+(\d+)", SearchType.Regex, GrepSearchOption.Global);
            Assert.Equal(3, matches.Count);
            matches.ForEach(m => m.ReplaceMatch = true);

            ReplaceLogFile log = ReplaceLogBuilder.Build("test.txt", text, matches, @"[a-z]+(\d+)", "<$1>",
                SearchType.Regex, GrepSearchOption.Global);

            ReplaceLogLine newLine = Assert.Single(Assert.Single(log.Entries).NewLines);
            Assert.Equal("<1> <22> <333>", newLine.Text);
            Assert.Equal(new[] { "<1>", "<22>", "<333>" }, newLine.Spans.Select(s => TextOf(newLine, s)).ToArray());
        }
    }
}
