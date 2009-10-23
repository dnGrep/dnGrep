using System;
using System.Collections.Generic;
using System.Text;
using MbUnit.Framework;
using dnGREP;
using System.IO;

namespace Tests
{
	[TestFixture]
	public class GrepCoreTest : TestBase
	{
		string sourceFolder;
		string destinationFolder;

		[TestFixtureSetUp]
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
			core.ShowLinesInContext = false;
			GrepSearchResult[] results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "dnGR\\wP", true, false, -1);
			Assert.AreEqual(results.Length, 1);

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "dngr\\wp", true, false, -1);
			Assert.AreEqual(results.Length, 0);

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "dngr\\wp", true, true, -1);
			Assert.AreEqual(results.Length, 0);

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "dngr\\wp", false, false, -1);
			Assert.AreEqual(results.Length, 1);

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, false, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			core.ShowLinesInContext = true;

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*", SearchOption.AllDirectories), "", false, true, -1);
			Assert.AreEqual(results.Length, 4);
			Assert.IsNull(results[0].SearchResults);
			
			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			core.LinesBefore = 2;
			core.LinesAfter = 2;

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 6);
			Assert.AreEqual(results[1].SearchResults.Count, 624);

			results = core.SearchRegex(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 6);
			Assert.AreEqual(results[1].SearchResults.Count, 624);

			Assert.IsNull(core.SearchRegex(null, "string", false, true, -1));
			Assert.IsNull(core.SearchRegex(new string[] { }, "string", false, true, -1));
		}

		[Test]
		public void TestSearchPlainReturnsCorrectNumber()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
			GrepCore core = new GrepCore();
			core.ShowLinesInContext = false;
			GrepSearchResult[] results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "dnGREP", true, false, -1);
			Assert.AreEqual(results.Length, 1);

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "dngrep", true, false, -1);
			Assert.AreEqual(results.Length, 0);

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "dngrep", true, true, -1);
			Assert.AreEqual(results.Length, 0);

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "dngrep", false, false, -1);
			Assert.AreEqual(results.Length, 1);

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, false, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "dngrep", true, false, -1);
			Assert.AreEqual(results.Length, 0);

			core.ShowLinesInContext = true;

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*", SearchOption.AllDirectories), "", false, true, -1);
			Assert.AreEqual(results.Length, 4);
			Assert.IsNull(results[0].SearchResults);

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 2);
			Assert.AreEqual(results[1].SearchResults.Count, 174);

			core.LinesBefore = 2;
			core.LinesAfter = 2;

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 6);
			Assert.AreEqual(results[1].SearchResults.Count, 624);

			results = core.SearchText(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), "string", false, true, -1);
			Assert.AreEqual(results.Length, 2);
			Assert.AreEqual(results[0].SearchResults.Count, 6);
			Assert.AreEqual(results[1].SearchResults.Count, 624);

			Assert.IsNull(core.SearchText(null, "string", false, true, -1));
			Assert.IsNull(core.SearchText(new string[] { }, "string", false, true, -1));
		}

		[Test]
		public void TestSearchXPathReturnsCorrectNumber()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase4", destinationFolder + "\\TestCase4", null, null);
			GrepCore core = new GrepCore();
			core.ShowLinesInContext = false;
			GrepSearchResult[] results = core.SearchXPath(Directory.GetFiles(destinationFolder + "\\TestCase4", "app.config"), "//setting", -1);
			Assert.AreEqual(results.Length, 1);
			Assert.AreEqual(results[0].SearchResults.Count, 28);
		}
	}
}
