using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dnGREP.Common;
using dnGREP.Engines;
using Xunit;

namespace Tests
{
    // Phase 4 tests for the replacement log text, csv and html writers.
    // Written without being able to compile or run them; first run on Windows may need fixes.
    public class ReplaceLogWriterTest : TestBase
    {
        private static readonly UTF8Encoding utf8 = new(false);

        private static readonly ReplaceLogInfo info = new("foo", "quux", "Search for 'foo'\r\nUsing type of search: Plain text\r\n",
            new DateTime(2026, 10, 15, 16, 0, 0));

        private static readonly ReplaceLogColors colors = new("#101010", "#e0e0e0", "#8a0000", "#006400", "#444444", "#ffff00", "#808080");

        private static List<GrepMatch> Search(string text, string pattern)
        {
            GrepEnginePlainText engine = new();
            engine.Initialize(GrepEngineInitParams.Default, FileFilter.Default, null);
            using MemoryStream stream = new(utf8.GetBytes(text));
            List<GrepSearchResult> results = engine.Search(stream, new FileData("test.txt"), pattern,
                SearchType.PlainText, GrepSearchOption.Global, utf8);
            return results.Count == 0 ? [] : results[0].Matches;
        }

        private static ReplaceLogFile Build(string text, string pattern, string replacement, params int[] marked)
        {
            List<GrepMatch> matches = Search(text, pattern);
            foreach (int i in marked) {
                matches[i].ReplaceMatch = true;
            }
            return ReplaceLogBuilder.Build("c:\\src\\test.txt", text, matches, pattern, replacement,
                SearchType.PlainText, GrepSearchOption.Global);
        }

        [Fact]
        public void TextShowsOldAndNewLinesAndCounts()
        {
            // line 2 has two matches, only the first is replaced
            ReplaceLogFile log = Build("one foo\ntwo foo bar foo\nthree\n", "foo", "quux", 0, 1);

            string text = ReplaceLogWriter.ToText([log], info);

            Assert.Contains("dnGrep Replace Log", text);
            Assert.Contains("Replace with: 'quux'", text);
            Assert.Contains("Replaced 2 matches on 2 lines in 1 files (1 unchanged)", text);
            Assert.Contains("c:\\src\\test.txt", text);
            Assert.Contains("has 2 replacements on 2 lines (1 unchanged):", text);
            Assert.Contains("- 1: one foo", text);
            Assert.Contains("+ 1: one quux", text);
            Assert.Contains("- 2: two foo bar foo", text);
            Assert.Contains("+ 2: two quux bar foo", text);
            Assert.DoesNotContain("other match", text);
            Assert.DoesNotContain("\t", text);
        }

        private static ReplaceLogEntry Entry(int lineNumber, int oldLineCount, int replacements)
        {
            List<ReplaceLogLine> oldLines = [];
            for (int i = 0; i < oldLineCount; i++) {
                oldLines.Add(new ReplaceLogLine(lineNumber + i, "x", []));
            }
            return new ReplaceLogEntry(lineNumber, oldLines, [], replacements);
        }

        [Fact]
        public void ReplacedLineCountCountsSameLineOnce()
        {
            ReplaceLogFile file = new("test.txt");
            file.Entries.Add(Entry(3, 1, 1));
            file.Entries.Add(Entry(3, 1, 1));

            Assert.Equal(1, file.ReplacedLineCount);
        }

        [Fact]
        public void ReplacedLineCountCountsEveryLineOfSpanningMatch()
        {
            ReplaceLogFile file = new("test.txt");
            file.Entries.Add(Entry(5, 3, 1));
            file.Entries.Add(Entry(9, 1, 0));

            Assert.Equal(3, file.ReplacedLineCount);
        }

        [Fact]
        public void TextShowsSkippedLineAsBeforeAndAfter()
        {
            ReplaceLogFile log = Build("one foo\n", "foo", "quux");

            string text = ReplaceLogWriter.ToText([log], info);

            Assert.Contains("- 1: one foo", text);
            Assert.Contains("+ 1: one foo", text);
            Assert.DoesNotContain("replacement skipped", text);
        }

        [Fact]
        public void CsvQuotesTextAndRepeatsOldTextWhenSkipped()
        {
            ReplaceLogFile replaced = Build("say \"hi\", foo\n", "foo", "quux", 0);
            ReplaceLogFile skipped = Build("one foo\n", "foo", "quux");

            string csv = ReplaceLogWriter.ToCsv([replaced, skipped]);
            string[] rows = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal("File,Line,Replacements,Unchanged,Old text,New text,Note", rows[0]);
            Assert.Equal("\"c:\\src\\test.txt\",1,1,0,\"say \"\"hi\"\", foo\",\"say \"\"hi\"\", quux\",\"\"", rows[1]);
            Assert.Equal("\"c:\\src\\test.txt\",1,0,1,\"one foo\",\"one foo\",\"\"", rows[2]);
        }

        [Fact]
        public void HtmlWrapsOldAndNewTextAndEncodesSource()
        {
            ReplaceLogFile log = Build("<b>foo</b> & foo\n", "foo", "quux", 0);

            string html = ReplaceLogWriter.ToHtml([log], info, colors);

            Assert.StartsWith("<!DOCTYPE html>", html);
            Assert.Contains("white-space: pre", html);
            Assert.DoesNotContain("&nbsp;", html);
            Assert.Contains("&lt;b&gt;<span class=\"old\">foo</span>&lt;/b&gt; &amp; <span class=\"skip\">foo</span>", html);
            Assert.Contains("&lt;b&gt;<span class=\"new\">quux</span>&lt;/b&gt; &amp; <span class=\"skip\">foo</span>", html);
            Assert.Contains(".old { background: #8a0000; }", html);
            Assert.DoesNotContain("<script", html);
            Assert.DoesNotContain("http", html);
        }
    }
}
