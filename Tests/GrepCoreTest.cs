using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Extensions;
using dnGREP;
using System.IO;
using dnGREP.Common;
using System.Data.Linq;
using System.Collections;
using System.Text.RegularExpressions;

namespace Tests
{
	public class GrepCoreTest : TestBase, IDisposable
	{
		string sourceFolder;
		string destinationFolder;

        public GrepCoreTest()
		{
			sourceFolder = GetDllPath() + "\\Files";
            destinationFolder = Path.GetTempPath() + Guid.NewGuid().ToString();
            Directory.CreateDirectory(destinationFolder);
		}


        public void Dispose()
        {
            if (Directory.Exists(destinationFolder))
                Utils.DeleteFolder(destinationFolder);
        }

		[Fact]
		public void TestSearchRegexReturnsCorrectNumber()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
			GrepCore core = new GrepCore();
			List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dnGR\\wP", GrepSearchOption.CaseSensitive, -1);
			Assert.Equal(results.Count, 1);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.CaseSensitive, -1);
			Assert.Equal(results.Count, 0);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
			Assert.Equal(results.Count, 0);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.None, -1);
			Assert.Equal(results.Count, 1);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.None, -1);
			Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
			Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
			Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);			

            Assert.Empty(core.Search(null, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
			Assert.Empty(core.Search(new string[] { }, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
		}

		[Fact]
		public void TestSearchPlainReturnsCorrectNumber()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
			GrepCore core = new GrepCore();
			List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dnGREP", GrepSearchOption.CaseSensitive, -1);
			Assert.Equal(results.Count, 1);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive, -1);
			Assert.Equal(results.Count, 0);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, - 1);
			Assert.Equal(results.Count, 0);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.None, -1);
			Assert.Equal(results.Count, 1);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.None, -1);
			Assert.Equal(results.Count, 2);
			Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
			Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
			Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive, -1);
			Assert.Equal(results.Count, 0);

			Assert.Empty(core.Search(null, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
			Assert.Empty(core.Search(new string[] { }, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
		}

        [Fact]
        public void TestSearchContainsValidPattern()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dnGREP", GrepSearchOption.CaseSensitive, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal("dnGREP", results[0].Pattern);          
        }

        [Fact]
        public void TestSearchWithStopAfterFirstMatch()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "public", GrepSearchOption.CaseSensitive, -1);
            Assert.True(results.Count > 1);
            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "public", GrepSearchOption.StopAfterFirstMatch, -1);
            Assert.Equal(1, results.Count);
            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "", GrepSearchOption.CaseSensitive, -1);
            Assert.True(results.Count > 1);
            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "", GrepSearchOption.StopAfterFirstMatch, -1);
            Assert.Equal(1, results.Count);
        }

        [Fact]
        public void TestResultSequence()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "test-file-plain-big.txt"), SearchType.PlainText, "string", GrepSearchOption.CaseSensitive, -1);
            Assert.Equal(results.Count, 1);
            var resultLines = results[0].GetLinesWithContext(3, 3);
            int lastLine = 0;
            foreach (var line in resultLines)
            {
                if (line.LineNumber <= lastLine)
                    Assert.True(false, "Lines are not sequential");
                lastLine = line.LineNumber;
            }
        }

		[Fact]
		public void TestSearchXPathReturnsCorrectMatchCount()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase4", destinationFolder + "\\TestCase4", null, null);
			GrepCore core = new GrepCore();
			List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase4", "app.config"), SearchType.XPath, "//setting", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
			Assert.Equal(results.Count, 1);
			Assert.Equal(results[0].Matches.Count, 28);
		}

        [Fact]
        public void TestSearchXPathReturnsCorrectResultsCount()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase4", destinationFolder + "\\TestCase4", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase4", "app.config"), SearchType.XPath, "//setting", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 84);
        }

        [Fact]
        public void TestSearchWholeWord_Issue_114_Regex()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase10", destinationFolder + "\\TestCase10", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase10", "issue-114.txt"), SearchType.Regex, "protected", GrepSearchOption.WholeWord, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 1);            
        }

        [Fact]
        public void TestSearchWholeWord_Issue_114_Plain()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase10", destinationFolder + "\\TestCase10", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase10", "issue-114.txt"), SearchType.PlainText, "protected", GrepSearchOption.WholeWord, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 1);            
        }

        [Fact]
        public void TestReplaceWithNewLineWorks()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase8", destinationFolder + "\\TestCase8", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase8", "test.txt"), SearchType.Regex, "here", GrepSearchOption.None, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 1);
            core.Replace(Directory.GetFiles(destinationFolder + "\\TestCase8", "test.txt"), SearchType.Regex, destinationFolder + "\\TestCase8", "here", "\\n", GrepSearchOption.None, -1);
            Assert.Equal(File.ReadAllText(destinationFolder + "\\TestCase8\\test.txt", Encoding.ASCII).Trim().Split('\n').Length, 2);
        }

        private Regex guidPattern = new Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        
        [Theory]
        [InlineData(SearchType.Regex)]
        [InlineData(SearchType.PlainText)]
        [InlineData(SearchType.Soundex)]
        public void TestReplaceWithPattern(SearchType type)
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase9", destinationFolder + "\\TestCase9", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase9", "test.txt"), type, "here", GrepSearchOption.None, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 6);
            core.Replace(Directory.GetFiles(destinationFolder + "\\TestCase9", "test.txt"), type, destinationFolder + "\\TestCase9", "here", "$(guid)", GrepSearchOption.None, -1);
            string fileContent = File.ReadAllText(destinationFolder + "\\TestCase9\\test.txt", Encoding.ASCII).Trim();
            Assert.Equal(6, guidPattern.Matches(fileContent).Count);
            HashSet<string> uniqueGuids = new HashSet<string>();
            foreach (Match match in guidPattern.Matches(fileContent))
            {
                if (!uniqueGuids.Contains(match.Value))
                    uniqueGuids.Add(match.Value);
                else
                    Assert.True(false, "All guides should be unique.");
            }
        }

        [Fact(Skip = "Functionality being tested not implemented yet.")]
        public void TestGuidxReplaceWithPatternRegex()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase9", destinationFolder + "\\TestCase9", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase9", "guidx.txt"), SearchType.Regex, "h\\wre", GrepSearchOption.None, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 6);
            core.Replace(Directory.GetFiles(destinationFolder + "\\TestCase9", "guidx.txt"), SearchType.Regex, destinationFolder + "\\TestCase9", "h\\wre", "$(guidx)", GrepSearchOption.None, -1);
            string fileContent = File.ReadAllText(destinationFolder + "\\TestCase9\\guidx.txt", Encoding.ASCII).Trim();
            Assert.Equal(6, guidPattern.Matches(fileContent).Count);
            Dictionary<string, int> uniqueGuids = new Dictionary<string, int>();
            foreach (Match match in guidPattern.Matches(fileContent))
            {
                if (!uniqueGuids.ContainsKey(match.Value))
                    uniqueGuids[match.Value] = 1;
                else
                    uniqueGuids[match.Value]++;
            }
            Assert.Equal(2, uniqueGuids.Keys.Count);
        }

        [Theory]
        [InlineData(SearchType.Regex)]
        [InlineData(SearchType.PlainText)]
        public void SearchWIthMultipleLines(SearchType type)
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase12", destinationFolder + "\\TestCase12", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase12", "issue-165.txt"), type, "asdf\r\nqwer", GrepSearchOption.Multiline, -1);
            Assert.Equal(results[0].Matches.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 5);
        }
    }
}
