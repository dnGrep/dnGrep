using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;
using dnGREP.Engines;
using Xunit;

namespace Tests
{
    /// <summary>
    /// Non-multiline searches call the text and regex search for every line with the same pattern.
    /// </summary>
    public class GrepEngineLineSearchTest
    {
        private static List<GrepMatch> Search(string text, string pattern, SearchType type, GrepSearchOption options)
        {
            GrepEnginePlainText engine = new();
            Encoding encoding = Encoding.UTF8;
            using MemoryStream stream = new(encoding.GetBytes(text));
            var results = engine.Search(stream, new FileData("test.txt"), pattern, type, options, encoding);
            return results.SelectMany(r => r.Matches).ToList();
        }

        [Theory]
        [InlineData(SearchType.PlainText)]
        [InlineData(SearchType.Regex)]
        public void ReportsLineNumbersAndFilePositions(SearchType type)
        {
            string text = "no match\r\nfirst needle here\nneedle needle\rlast line\r\nneedle";
            var matches = Search(text, "needle", type, GrepSearchOption.Global | GrepSearchOption.CaseSensitive);

            Assert.Equal([2, 3, 3, 5], matches.Select(m => m.LineNumber));
            Assert.All(matches, m => Assert.Equal("needle", text.Substring(m.StartLocation, m.Length)));
        }

        [Theory]
        [InlineData(SearchType.PlainText)]
        [InlineData(SearchType.Regex)]
        public void NonGlobalStopsAtFirstMatch(SearchType type)
        {
            var matches = Search("needle needle\nneedle", "needle", type, GrepSearchOption.CaseSensitive);

            Assert.Single(matches);
            Assert.Equal(0, matches[0].StartLocation);
        }

        [Theory]
        [InlineData(SearchType.PlainText)]
        [InlineData(SearchType.Regex)]
        public void WholeWordSkipsEmbeddedMatches(SearchType type)
        {
            string text = "needles needle_ xneedle needle, (needle) 1needle";
            var matches = Search(text, "needle", type, GrepSearchOption.Global | GrepSearchOption.CaseSensitive | GrepSearchOption.WholeWord);

            Assert.Equal([text.IndexOf("needle,"), text.IndexOf("(needle)") + 1], matches.Select(m => m.StartLocation));
        }

        [Theory]
        [InlineData(SearchType.PlainText)]
        [InlineData(SearchType.Regex)]
        public void WholeWordWithManyEmbeddedMatchesInLongLine(SearchType type)
        {
            string text = string.Concat(Enumerable.Repeat("needles ", 20_000)) + "needle";
            var matches = Search(text, "needle", type, GrepSearchOption.Global | GrepSearchOption.CaseSensitive | GrepSearchOption.WholeWord);

            Assert.Single(matches);
            Assert.Equal(text.Length - 6, matches[0].StartLocation);
        }

        // the compiled regex is reused between lines and searches, but never between different options
        [Fact]
        public void RegexCacheDistinguishesOptionsForTheSamePattern()
        {
            string text = "Needle needle needles";

            var caseSensitive = Search(text, "needle", SearchType.Regex, GrepSearchOption.Global | GrepSearchOption.CaseSensitive);
            var ignoreCase = Search(text, "needle", SearchType.Regex, GrepSearchOption.Global);
            var wholeWord = Search(text, "needle", SearchType.Regex, GrepSearchOption.Global | GrepSearchOption.CaseSensitive | GrepSearchOption.WholeWord);
            var caseSensitiveAgain = Search(text, "needle", SearchType.Regex, GrepSearchOption.Global | GrepSearchOption.CaseSensitive);

            Assert.Equal(2, caseSensitive.Count);
            Assert.Equal(3, ignoreCase.Count);
            Assert.Single(wholeWord);
            Assert.Equal(2, caseSensitiveAgain.Count);
        }

        [Fact]
        public void RegexCapturesGroupsOnEveryLine()
        {
            var matches = Search("id=12\nid=345\nname=x", @"(?<key>id)=(?<num>\d+)", SearchType.Regex, GrepSearchOption.Global | GrepSearchOption.CaseSensitive);

            Assert.Equal(2, matches.Count);
            Assert.Equal(["12", "345"], matches.Select(m => m.Groups.Single(g => g.Name == "num").Value));
        }

        [Theory]
        [InlineData(SearchType.PlainText)]
        [InlineData(SearchType.Regex)]
        public void BooleanOperatorsStillEvaluatePerLine(SearchType type)
        {
            var matches = Search("alpha beta\nalpha only\nbeta only", "alpha AND beta", type,
                GrepSearchOption.Global | GrepSearchOption.CaseSensitive | GrepSearchOption.BooleanOperators);

            Assert.All(matches, m => Assert.Equal(1, m.LineNumber));
            Assert.Equal(2, matches.Count);
        }
    }
}
