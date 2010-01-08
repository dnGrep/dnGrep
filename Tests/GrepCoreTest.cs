using System;
using System.Collections.Generic;
using System.Text;
using MbUnit.Framework;
using dnGREP;
using System.IO;
using dnGREP.Common;

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
			core.SearchParams.ShowLinesInContext = false;
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

			core.SearchParams.ShowLinesInContext = true;

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*", SearchOption.AllDirectories), SearchType.Regex, "", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 4);
			Assert.IsNull(results[0].SearchResults);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			core.SearchParams.LinesBefore = 2;
			core.SearchParams.LinesAfter = 2;

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 6);
			Assert.AreEqual(results[1].SearchResults.Count, 624);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 6);
			Assert.AreEqual(results[1].SearchResults.Count, 624);

            Assert.IsNull(core.Search(null, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
            Assert.IsNull(core.Search(new string[] { }, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
		}

		[Test]
		public void TestSearchPlainReturnsCorrectNumber()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
			GrepCore core = new GrepCore();
			core.SearchParams.ShowLinesInContext = false;
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

			core.SearchParams.ShowLinesInContext = true;

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*", SearchOption.AllDirectories), SearchType.PlainText, "", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 4);
			Assert.IsNull(results[0].SearchResults);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			core.SearchParams.LinesBefore = 2;
			core.SearchParams.LinesAfter = 2;

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 6);
			Assert.AreEqual(results[1].SearchResults.Count, 624);

            results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 6);
			Assert.AreEqual(results[1].SearchResults.Count, 624);

            Assert.IsNull(core.Search(null, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
            Assert.IsNull(core.Search(new string[] { }, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
		}

		[Test]
		public void TestSearchReturnsCorrectNumber()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase4", destinationFolder + "\\TestCase4", null, null);
			GrepCore core = new GrepCore();
			core.SearchParams.ShowLinesInContext = false;
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase4", "app.config"), SearchType.XPath, "//setting", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
			Assert.AreEqual(results.Count, 1);
			Assert.AreEqual(results[0].SearchResults.Count, 28);
		}
	}
}
