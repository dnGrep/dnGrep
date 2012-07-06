using System;
using System.Collections.Generic;
using System.Text;
using MbUnit.Framework;
using dnGREP;
using System.IO;
using dnGREP.Common;
using System.Data.Linq;
using System.Collections;
using System.Text.RegularExpressions;

namespace Tests
{
	[TestFixture]
	public class GrepCoreTest : TestBase
	{
		string sourceFolder;
		string destinationFolder;

		[FixtureSetUp]
		public void Initialize()
		{
			sourceFolder = GetDllPath() + "\\Files";
		}

		[SetUp]
		public void CreateTempFolder()
		{
			destinationFolder = Path.GetTempPath() + Guid.NewGuid().ToString();
			Directory.CreateDirectory(destinationFolder);
		}

		[TearDown]
		public void DeleteTempFolder()
		{
			if (Directory.Exists(destinationFolder))
				Utils.DeleteFolder(destinationFolder);
		}

		[Test]
		public void TestSearchRegexReturnsCorrectNumber()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
			GrepCore core = new GrepCore();
			List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dnGR\\wP", GrepSearchOption.CaseSensitive, -1);
			Assert.AreEqual(results.Count, 1);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.CaseSensitive, -1);
			Assert.AreEqual(results.Count, 0);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 0);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.None, -1);
			Assert.AreEqual(results.Count, 1);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.None, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);			

            Assert.IsEmpty(core.Search(null, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
			Assert.IsEmpty(core.Search(new string[] { }, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
		}

		[Test]
		public void TestSearchPlainReturnsCorrectNumber()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
			GrepCore core = new GrepCore();
			List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dnGREP", GrepSearchOption.CaseSensitive, -1);
			Assert.AreEqual(results.Count, 1);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive, -1);
			Assert.AreEqual(results.Count, 0);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, - 1);
			Assert.AreEqual(results.Count, 0);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.None, -1);
			Assert.AreEqual(results.Count, 1);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.None, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive, -1);
			Assert.AreEqual(results.Count, 0);

			Assert.IsEmpty(core.Search(null, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
			Assert.IsEmpty(core.Search(new string[] { }, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
		}

		[Test]
		public void TestSearchXPathReturnsCorrectNumber()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase4", destinationFolder + "\\TestCase4", null, null);
			GrepCore core = new GrepCore();
			List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase4", "app.config"), SearchType.XPath, "//setting", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 1);
			Assert.AreEqual(results[0].SearchResults.Count, 28);
		}

        [Test]
        public void TestSearchWholeWord_Issue_114_Regex()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase10", destinationFolder + "\\TestCase10", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase10", "issue-114.txt"), SearchType.Regex, "protected", GrepSearchOption.WholeWord, -1);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0].SearchResults.Count, 1);            
        }

        [Test]
        public void TestSearchWholeWord_Issue_114_Plain()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase10", destinationFolder + "\\TestCase10", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase10", "issue-114.txt"), SearchType.PlainText, "protected", GrepSearchOption.WholeWord, -1);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0].SearchResults.Count, 1);            
        }

        [Test]
        public void TestReplaceWithNewLineWorks()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase8", destinationFolder + "\\TestCase8", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase8", "test.txt"), SearchType.Regex, "here", GrepSearchOption.None, -1);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0].SearchResults.Count, 1);
            core.Replace(Directory.GetFiles(destinationFolder + "\\TestCase8", "test.txt"), SearchType.Regex, destinationFolder + "\\TestCase8", "here", "\\n", GrepSearchOption.None, -1);
            Assert.AreEqual(File.ReadAllText(destinationFolder + "\\TestCase8\\test.txt", Encoding.ASCII).Trim().Split('\n').Length, 2);
        }

        private Regex guidPattern = new Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        
        [Test]
        [Row(SearchType.Regex)]
        [Row(SearchType.PlainText)]
        [Row(SearchType.Soundex)]
        public void TestReplaceWithPattern(SearchType type)
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase9", destinationFolder + "\\TestCase9", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase9", "test.txt"), type, "here", GrepSearchOption.None, -1);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0].SearchResults.Count, 6);
            core.Replace(Directory.GetFiles(destinationFolder + "\\TestCase9", "test.txt"), type, destinationFolder + "\\TestCase9", "here", "$(guid)", GrepSearchOption.None, -1);
            string fileContent = File.ReadAllText(destinationFolder + "\\TestCase9\\test.txt", Encoding.ASCII).Trim();
            Assert.AreEqual(6, guidPattern.Matches(fileContent).Count);
            HashSet<string> uniqueGuids = new HashSet<string>();
            foreach (Match match in guidPattern.Matches(fileContent))
            {
                if (!uniqueGuids.Contains(match.Value))
                    uniqueGuids.Add(match.Value);
                else
                    Assert.Fail("All guides should be unique.");
            }
        }

        [Test]
        public void TestGuidxReplaceWithPatternRegex()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase9", destinationFolder + "\\TestCase9", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase9", "guidx.txt"), SearchType.Regex, "h\\wre", GrepSearchOption.None, -1);
            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0].SearchResults.Count, 6);
            core.Replace(Directory.GetFiles(destinationFolder + "\\TestCase9", "guidx.txt"), SearchType.Regex, destinationFolder + "\\TestCase9", "h\\wre", "$(guidx)", GrepSearchOption.None, -1);
            string fileContent = File.ReadAllText(destinationFolder + "\\TestCase9\\guidx.txt", Encoding.ASCII).Trim();
            Assert.AreEqual(6, guidPattern.Matches(fileContent).Count);
            Dictionary<string, int> uniqueGuids = new Dictionary<string, int>();
            foreach (Match match in guidPattern.Matches(fileContent))
            {
                if (!uniqueGuids.ContainsKey(match.Value))
                    uniqueGuids[match.Value] = 1;
                else
                    uniqueGuids[match.Value]++;
            }
            Assert.AreEqual(2, uniqueGuids.Keys.Count);
        }
	}
}
