using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using dnGREP.Common;
using dnGREP.Common.IO;
using dnGREP.Engines;
using Xunit;

namespace Tests
{
    public partial class GrepCoreTest : TestBase, IDisposable
    {
#pragma warning disable SYSLIB1045
        private readonly string sourceFolder;
        private string destinationFolder;

        public GrepCoreTest()
        {
            sourceFolder = Path.Combine(GetDllPath(), "Files");
            destinationFolder = Path.Combine(Path.GetTempPath(), "dnGrepTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(destinationFolder);

            if (Environment.Is64BitProcess)
                SevenZip.SevenZipBase.SetLibraryPath(Path.Combine(GetDllPath(), @"7z64.dll"));
            else
                SevenZip.SevenZipBase.SetLibraryPath(Path.Combine(GetDllPath(), @"7z32.dll"));
        }

        public string GetLongPathDestination(string leafFolder)
        {
            var parts = new string[]
            {
                destinationFolder,
                new string('a', 50),
                new string('b', 50),
                new string('c', 50),
                new string('d', 50),
                new string('e', 50),
                new string('f', 50),
                new string('g', 50),
                new string('h', 50),
                leafFolder
            };
            destinationFolder = Path.Combine(parts);

            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            return destinationFolder;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            // if long path, delete folder from the top of the long path
            string folder = destinationFolder;
            while (folder.Contains("aaaaaaaaaaaaaaaaaaaa"))
                folder = Path.GetDirectoryName(folder) ?? string.Empty;

            if (Directory.Exists(folder))
                Utils.DeleteFolder(folder);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchRegexReturnsCorrectNumber(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"),
                SearchType.Regex, "dnGR\\wP", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.CaseSensitive, -1);
            Assert.Empty(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
            Assert.Empty(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.None, -1);
            Assert.Single(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, "string", GrepSearchOption.None, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);

            Assert.Empty(core.Search(null, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
            Assert.Empty(core.Search(Array.Empty<string>(), SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchPlainReturnsCorrectNumber(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dnGREP", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);
            Assert.Equal(3, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive, -1);
            Assert.Empty(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
            Assert.Empty(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Equal(3, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "string", GrepSearchOption.None, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive, -1);
            Assert.Empty(results);

            Assert.Empty(core.Search(null, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
            Assert.Empty(core.Search(Array.Empty<string>(), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchPlainBooleanOpAndReturnsCorrectNumber(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "Regex AND pattern", GrepSearchOption.CaseSensitive | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(24, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "regex AND pattern", GrepSearchOption.CaseSensitive | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(12, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep AND asterisk", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Empty(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dnGREP AND asterisk", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(6, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "regex AND asterisk", GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(9, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "body AND lineNumber", GrepSearchOption.BooleanOperators, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(4, results[0].Matches.Count);
            Assert.Equal(12, results[1].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "body AND lineNumber", GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(7, results[0].Matches.Count);
            Assert.Equal(120, results[1].Matches.Count);

            Assert.Empty(core.Search(null, SearchType.PlainText, "body AND lineNumber", GrepSearchOption.BooleanOperators, -1));
            Assert.Empty(core.Search(Array.Empty<string>(), SearchType.PlainText, "body AND lineNumber", GrepSearchOption.BooleanOperators, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchPlainBooleanOpOrReturnsCorrectNumber(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dnGREP OR asterisk", GrepSearchOption.CaseSensitive | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(6, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep OR asterisk", GrepSearchOption.CaseSensitive | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(3, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dnGREP OR asterisk", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(6, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep OR asterisk", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(3, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dnGrep OR Asterisk", GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(6, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "alpha OR hello", GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "alpha OR hello", GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);

            Assert.Empty(core.Search(null, SearchType.PlainText, "body OR lineNumber", GrepSearchOption.BooleanOperators, -1));
            Assert.Empty(core.Search(Array.Empty<string>(), SearchType.PlainText, "body OR lineNumber", GrepSearchOption.BooleanOperators, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchRegexBooleanOpAndReturnsCorrectNumber(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, @"r\w+ly AND Pa\w+n", GrepSearchOption.CaseSensitive | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(12, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, @"r\w+ly AND pa\w+n", GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(12, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, @"\w+GREP AND as.+k", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(6, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, @"\w+grep AND as.+k", GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(9, results[0].Matches.Count);

            Assert.Empty(core.Search(null, SearchType.Regex, @"\w+grep AND as.+k", GrepSearchOption.BooleanOperators, -1));
            Assert.Empty(core.Search(Array.Empty<string>(), SearchType.Regex, @"\w+grep AND as.+k", GrepSearchOption.BooleanOperators, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchRegexBooleanOpOrReturnsCorrectNumber(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, @"r\w+ly OR H...o", GrepSearchOption.CaseSensitive | GrepSearchOption.BooleanOperators, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(6, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, @"unn\w+ or Ast\w+", GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(6, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, @"unn\w+ OR As.+k", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(3, results[0].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.Regex, @"unn\w+ OR As.+k", GrepSearchOption.Multiline | GrepSearchOption.BooleanOperators, -1);
            Assert.Single(results);
            Assert.Equal(9, results[0].Matches.Count);

            Assert.Empty(core.Search(null, SearchType.Regex, @"r\w+ly OR pa\w+n", GrepSearchOption.BooleanOperators, -1));
            Assert.Empty(core.Search(Array.Empty<string>(), SearchType.Regex, @"r\w+ly OR pa\w+n", GrepSearchOption.BooleanOperators, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchContainsValidPattern(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dnGREP", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);
            Assert.Equal("dnGREP", results[0].Pattern);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchWithStopAfterFirstMatch(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "public", GrepSearchOption.CaseSensitive, -1);
            Assert.True(results.Count > 1);
            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "public", GrepSearchOption.StopAfterFirstMatch, -1);
            Assert.Single(results);

            var fileData = Utils.GetFileListIncludingArchives(new FileFilter(destFolder, "*.*", string.Empty, false, false,
                false, true, -1, true, true, true, false, 0, 0, FileDateFilter.None, null, null));
            results = core.ListFiles(fileData, GrepSearchOption.CaseSensitive, -1);
            Assert.True(results.Count > 1);
            results = core.ListFiles(fileData, GrepSearchOption.StopAfterFirstMatch, -1);
            Assert.Single(results);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestResultSequence(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;
            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "test-file-plain-big.txt"), SearchType.PlainText, "string", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);
            var resultLines = results[0].GetLinesWithContext(3, 3);
            int lastLine = 0;
            foreach (var line in resultLines)
            {
                if (line.LineNumber <= lastLine)
                    Assert.Fail("Lines are not sequential");
                lastLine = line.LineNumber;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchXPathReturnsCorrectMatchCount(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase4"), Path.Combine(destFolder, "TestCase4"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase4"), "app.config"), SearchType.XPath,
                "//setting", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);
            Assert.Equal(28, results[0].Matches.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchXPathReturnsCorrectResultsCount(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase4"), Path.Combine(destFolder, "TestCase4"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase4"), "app.config"), SearchType.XPath,
                "//setting", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);
            var lines = results[0].GetLinesWithContext(0, 0);
            Assert.Equal(84, lines.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchXPathWithMissingXmlDeclaration(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase4"), Path.Combine(destFolder, "TestCase4"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase4"), "books_no_decl.xml"),
                SearchType.XPath, "//book/price", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);
            Assert.Equal(5, results[0].Matches.Count);

            // test the results have returned the correct string
            foreach (var item in results[0].SearchResults.Where(r => !r.IsContext))
            {
                Assert.StartsWith("<price", item.LineText.Trim());
                Assert.EndsWith("</price>", item.LineText.Trim());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchAndReplaceXPathWithMissingXmlDeclaration(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase4"), Path.Combine(destFolder, "TestCase4"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase4"), "books_no_decl.xml"),
                SearchType.XPath, "(//@category)[2]", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destinationFolder, "TestCase4", "books_no_decl.xml");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };
            core.Replace(files, SearchType.XPath, "(//@category)[2]", "general", GrepSearchOption.None, -1);

            var fileContent = File.ReadAllLines(testFile, Encoding.UTF8);
            Assert.Equal(37, fileContent.Length);
            Assert.Equal("<bookstore>", fileContent[0]);
            Assert.Equal("  <book category=\"general\">", fileContent[7]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchAndFilteredReplaceXPathWithMissingXmlDeclaration(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase4"), Path.Combine(destFolder, "TestCase4"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase4"), "books_no_decl.xml"),
                SearchType.XPath, "(//@currency)", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Equal(5, results[0].Matches.Count);

            // mark 2nd and 4th matches for replace
            results[0].Matches[1].ReplaceMatch = true;
            results[0].Matches[3].ReplaceMatch = true;

            string testFile = Path.Combine(destinationFolder, "TestCase4", "books_no_decl.xml");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };
            core.Replace(files, SearchType.XPath, "(//@currency)", "EUR", GrepSearchOption.None, -1);

            var fileContent = File.ReadAllText(testFile, Encoding.UTF8);
            Assert.Equal(2, Regex.Matches(fileContent, "EUR").Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchAndReplaceXPath(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase4"), Path.Combine(destFolder, "TestCase4"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase4"), "app.config"), SearchType.XPath,
                "//appSettings", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);

            // mark all matches for replace
            foreach (var result in results)
            {
                foreach (var match in result.Matches)
                {
                    match.ReplaceMatch = true;
                }
            }

            string testFile = Path.Combine(destFolder, @"TestCase4\app.config");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            string replaceText = "<add key=\"test\" value=\"true\" />";
            core.Replace(files, SearchType.XPath, "//appSettings", replaceText, GrepSearchOption.None, -1);

            results = core.Search(new string[] { testFile }, SearchType.XPath, "//add", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);
            var hit = results[0].SearchResults.Where(r => !r.IsContext).ToArray();
            Assert.Single(hit);
            Assert.Contains("key=\"test\"", hit[0].LineText);
            Assert.Contains("value=\"true\"", hit[0].LineText);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchAndReplaceXPathAttribute(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase15"), Path.Combine(destFolder, "TestCase15"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase15"), "books.xml"), SearchType.XPath,
                "/bookstore/book/title[1]/@lang", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Equal(5, results[0].Matches.Count);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destFolder, @"TestCase15\books.xml");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            string replaceText = "dnGrep";
            core.Replace(files, SearchType.XPath, "/bookstore/book/title[1]/@lang", replaceText, GrepSearchOption.None, -1);

            results = core.Search(new string[] { testFile }, SearchType.XPath, "/bookstore/book/title[1]/@lang", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Equal(5, results[0].Matches.Count);
            var hits = results[0].SearchResults.Where(r => !r.IsContext).ToArray();
            Assert.Equal(5, hits.Length);
            foreach (var line in hits)
                Assert.Contains("lang=\"dnGrep\"", line.LineText);
        }

        [Theory]
        [InlineData("test1.xml", 3, false)]
        [InlineData("test2.xml", 3, false)]
        [InlineData("test3.xml", 3, false)]
        [InlineData("test4.xml", 4, true)]
        [InlineData("test5.xml", 4, true)]
        [InlineData("test6.xml", 3, false)]
        [InlineData("test7.xml", 3, false)]
        [InlineData("test8.xml", 3, true)]
        [InlineData("test9.xml", 3, false)]
        [InlineData("test10.xml", 8, false)]
        public void TestSearchXPathWithCommentsReturnsCorrectString(string fileName, int matchLineCount, bool matchContainsComment)
        {
            string destFolder = destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase18"), Path.Combine(destFolder, "TestCase18"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase18"), fileName), SearchType.XPath,
                "/bookstore/book[1]", GrepSearchOption.None, -1);
            Assert.Single(results);
            var lines = results[0].SearchResults.Where(r => !r.IsContext);
            Assert.Equal(matchLineCount, lines.Count());
            Assert.Equal(matchContainsComment, lines.Where(l => l.LineText.Contains("<!--")).Any());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchWholeWord_Issue_114_Regex(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase10"), Path.Combine(destFolder, "TestCase10"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase10"), "issue-114.txt"), SearchType.Regex, "protected", GrepSearchOption.WholeWord, -1);
            Assert.Single(results);
            Assert.Single(results[0].SearchResults);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchWholeWord_Issue_114_Plain(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase10"), Path.Combine(destFolder, "TestCase10"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase10"), "issue-114.txt"), SearchType.PlainText, "protected", GrepSearchOption.WholeWord, -1);
            Assert.Single(results);
            Assert.Single(results[0].SearchResults);
        }

        [Theory]
        [InlineData(@"b\w+k", "book shop", 1, 1, 4)]
        [InlineData(@"b\w+k", "bank shop", 1, 1, 4)]
        [InlineData(@"b\w+k", " book shop", 1, 1, 4)]
        [InlineData(@"b\w+k", "the book shop", 1, 1, 4)]
        [InlineData(@"b\w+k", "cook book", 1, 1, 4)]
        [InlineData(@"b\w+k", "cook b00k", 1, 1, 4)]
        [InlineData(@"b\w+k", "\tbook shop", 1, 1, 4)]
        [InlineData(@"b\w+k", "\tbook\tshop", 1, 1, 4)]
        [InlineData(@"b\w+k", " bookmark shop", 1, 1, 8)]
        [InlineData(@"b\w+k", " cookbook shop", 0, 0, 0)]
        [InlineData(@"b\w+k", "cookbook shop", 0, 0, 0)]
        [InlineData(@"b\w+k", "1cookbook shop", 0, 0, 0)]
        [InlineData(@"book", "abc1book shop", 0, 0, 0)]
        [InlineData(@"book", "abc#book shop", 1, 1, 4)]
        [InlineData(@"book shop", "the book shop store", 1, 1, 9)]
        [InlineData(@"b.*\ss.*p", "the book shop store", 1, 1, 9)]
        [InlineData(@"\bb\w+k", "book shop", 1, 1, 4)]
        [InlineData(@"\bb\w+k\ss\w+\b", "the book shop", 1, 1, 9)]
        [InlineData(@"b\w+k", "\r\nbook shop", 1, 1, 4)]
        [InlineData(@"b\w+k", "book\r\nshop", 1, 1, 4)]
        [InlineData(@"b\w+k", "\r\nbook\r\nshop", 1, 1, 4)]
        [InlineData(@"book ", "book shop", 1, 1, 5)]
        [InlineData(@"book ", "the book shop", 1, 1, 5)]
        [InlineData(@" book", "book shop", 0, 0, 0)]
        [InlineData(@" book", "the book shop", 1, 1, 5)]
        [InlineData(@" book ", "the book shop", 1, 1, 6)]
        [InlineData(@"shop\p{P}", "the book shop, the book shop. the book shop? the book shop", 1, 3, 5)]
        [InlineData(@"shop\p{P}", "the book shop.", 1, 1, 5)]
        [InlineData(@"shop\p{P}", "the book shop.a", 1, 1, 5)]
        [InlineData(@"shop\p{P}", "the book \"shop\"", 1, 1, 5)]
        [InlineData(@"shop\p{P}", "the book \"shop\"a", 1, 1, 5)]
        [InlineData(@"%temp%", "write to the %temp% directory", 1, 1, 6)]
        [InlineData(@"i\w+e", " include ", 1, 1, 7)]
        [InlineData(@"i\w+e", " included", 0, 0, 0)]
        [InlineData(@"#include", "#include", 1, 1, 8)]
        [InlineData(@"#include", "abc#include", 1, 1, 8)]// I think this should fail, but does not
        [InlineData(@"#include", "#include#", 1, 1, 8)]
        [InlineData(@"#include", "#include \"header.h\"", 1, 1, 8)]
        [InlineData(@"#include", "\\\\s*#include\\\\s*", 1, 1, 8)]
        [InlineData(@"#include", "pragma message(\"#include \" __FILE__)", 1, 1, 8)]
        [InlineData(@"#\w+", "#include", 1, 1, 8)]
        [InlineData(@"#\w+", "#include#", 1, 1, 8)]
        [InlineData(@"#\w+", "#include \"header.h\"", 1, 1, 8)]
        [InlineData(@"#\w+", "\\\\s*#include\\\\s*", 1, 1, 8)]
        [InlineData(@"#\w+", "pragma message(\"#include \" __FILE__)", 1, 1, 8)]
        [InlineData(@"#\w+e", "#include's", 1, 1, 8)]
        [InlineData(@"#\w+e", "#includes", 0, 0, 0)]
        [InlineData(@"red|green|blue", "DarkRed Red GreenYellow LightGreen Green SpringGreen RoyalBlue", 1, 2, 3)]
        public void TestRegexWholeWord(string pattern, string text, int expectedResultCount, int expectedMatchCount, int expectedMatchLength)
        {
            // Issue #813
            GrepEnginePlainText engine = new();
            var encoding = Encoding.UTF8;
            using Stream inputStream = new MemoryStream(encoding.GetBytes(text));
            var results = engine.Search(inputStream, "test.txt", pattern, SearchType.Regex, GrepSearchOption.WholeWord, encoding);

            Assert.Equal(expectedResultCount, results.Count);

            if (expectedResultCount > 0)
            {
                Assert.Equal(expectedMatchCount, results[0].Matches.Count);
                Assert.Equal(expectedMatchLength, results[0].Matches[0].Length);
            }
        }

        [Fact]
        public void TestReplaceSpecialChars()
        {
            string result = Utils.ReplaceSpecialCharacters("\\\\t");
            Assert.Equal("\t", result);

            result = Utils.ReplaceSpecialCharacters("\\\\n");
            Assert.Equal("\n", result);

            result = Utils.ReplaceSpecialCharacters("\\\\r\\\\n");
            Assert.Equal("\r\n", result);

            result = Utils.ReplaceSpecialCharacters("\\\\a");
            Assert.Equal("\a", result);
        }

        [Theory]
        [InlineData(@"\b(\w+[.])\b", "$1 ", "first.second. third", "first. second")]
        [InlineData(@"\b\w+(?=\sis\b)", "Saturday", "Sunday is a weekend day.", "Saturday")]
        [InlineData(@"\b(?!un)\w+\b", "understood", "untie, unite, misunderstood, under, unknown", ", understood, ")]
        [InlineData(@"(?<=[$])(\d+)", "4", "The price is $1.99 for two.", "$4.99")]
        [InlineData(@"(?<!USD)\d{3}", "200", "JPY100 USD100", "JPY200 USD100")]
        [InlineData(@"(?<=_(?=\d{2}_))\d+", "15", "10 _16_ 20", "10 _15_ 20")]
        public void TestLookAroundReplace(string pattern, string replace, string input, string expected)
        {
            string path = Path.Combine(destinationFolder, @"Issue437");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);

            string testFile = Path.Combine(path, @"test.txt");
            File.WriteAllText(testFile, input);

            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(path, "test.txt"), SearchType.Regex, pattern, GrepSearchOption.None, -1);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.Regex, pattern, replace, GrepSearchOption.None, -1);

            Assert.True(File.ReadAllText(testFile).Contains(expected, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestReplaceWithNewLineWorks(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase8"), Path.Combine(destFolder, "TestCase8"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase8"), "test.txt"), SearchType.Regex, "here", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Single(results[0].SearchResults);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destFolder, @"TestCase8\test.txt");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.Regex, "here", "\\\\n", GrepSearchOption.None, -1);
            Assert.Equal(2, File.ReadAllText(testFile, Encoding.ASCII).Trim().Split('\n').Length);
        }

        [Theory]
        [InlineData(SearchType.Regex, true)]
        [InlineData(SearchType.Regex, false)]
        [InlineData(SearchType.PlainText, true)]
        [InlineData(SearchType.PlainText, false)]
        [InlineData(SearchType.Soundex, true)]
        [InlineData(SearchType.Soundex, false)]
        public void TestReplaceWithPattern(SearchType type, bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase9"), Path.Combine(destFolder, "TestCase9"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase9"), "test.txt"),
                type, "here", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Equal(6, results[0].SearchResults.Count);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destFolder, @"TestCase9\test.txt");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            core.Replace(files, type, "here", "$(guid)", GrepSearchOption.None, -1);
            string fileContent = File.ReadAllText(testFile, Encoding.UTF8).Trim();
            Assert.Equal(6, GuidRegex().Matches(fileContent).Count);
            HashSet<string> uniqueGuids = new();
            foreach (Match match in GuidRegex().Matches(fileContent).Cast<Match>())
            {
                if (!uniqueGuids.Contains(match.Value))
                    uniqueGuids.Add(match.Value);
                else
                    Assert.Fail("All GUIDs should be unique.");
            }
        }

        [Fact]
        public void TestGuidxReplaceWithPatternRegex()
        {
            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase9"), Path.Combine(destinationFolder, "TestCase9"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destinationFolder, "TestCase9"), "guidx.txt"),
                SearchType.Regex, "h\\wre", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Equal(6, results[0].SearchResults.Count);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destinationFolder, @"TestCase9\guidx.txt");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            // all instances of the same string matched will get the same guid
            core.Replace(files, SearchType.Regex, "h\\wre", "$(guidx)", GrepSearchOption.None, -1);
            string fileContent = File.ReadAllText(testFile, Encoding.UTF8).Trim();
            Assert.Equal(6, GuidRegex().Matches(fileContent).Count);
            Dictionary<string, int> uniqueGuids = new();
            foreach (Match match in GuidRegex().Matches(fileContent).Cast<Match>())
            {
                if (!uniqueGuids.ContainsKey(match.Value))
                    uniqueGuids[match.Value] = 1;
                else
                    uniqueGuids[match.Value]++;
            }
            Assert.Equal(2, uniqueGuids.Keys.Count);
        }

        [Theory]
        [InlineData(SearchType.PlainText, GrepSearchOption.None, "children", "general")]
        [InlineData(SearchType.PlainText, GrepSearchOption.Multiline, "children", "general")]
        [InlineData(SearchType.Regex, GrepSearchOption.None, "child.*\"\\>", "general\">")]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, "child.*\"\\>", "general\">")]
        [InlineData(SearchType.Regex, GrepSearchOption.None, "child.*\"\\>$", "general\">")]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, "child.*\"\\>$", "general\">")]
        [InlineData(SearchType.XPath, GrepSearchOption.None, "(//@category)[2]", "general")]
        public void TestReplaceOnFileWithout_UTF8_BOM(SearchType type, GrepSearchOption option, string searchFor, string replaceWith)
        {
            // Test for Issue #227
            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase15"), Path.Combine(destinationFolder, "TestCase15"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destinationFolder, "TestCase15"), "books.xml"),
                type, searchFor, option, -1);
            Assert.Single(results);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destinationFolder, "TestCase15", "books.xml");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };
            core.Replace(files, type, searchFor, replaceWith, option, -1);

            using (FileStream stream = File.Open(testFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new(stream, true))
            {
                Assert.Equal(Encoding.UTF8, reader.CurrentEncoding);
                // check there is no BOM
                int bb = reader.BaseStream.ReadByte();
                Assert.NotEqual(0xEF, bb);
                Assert.Equal('<', bb);
                bb = reader.BaseStream.ReadByte();
                Assert.NotEqual(0xBB, bb);
                bb = reader.BaseStream.ReadByte();
                Assert.NotEqual(0xBF, bb);
            }
            var fileContent = File.ReadAllLines(Path.Combine(destinationFolder, "TestCase15", "books.xml"), Encoding.UTF8);

            Assert.Equal(38, fileContent.Length);
            Assert.Equal("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", fileContent[0]);
            Assert.Equal("  <book category=\"general\">", fileContent[8]);
        }

        [Theory]
        [InlineData(SearchType.PlainText, GrepSearchOption.None, "children", "general")]
        [InlineData(SearchType.PlainText, GrepSearchOption.Multiline, "children", "general")]
        [InlineData(SearchType.Regex, GrepSearchOption.None, "child.*\"\\>", "general\">")]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, "child.*\"\\>", "general\">")]
        [InlineData(SearchType.Regex, GrepSearchOption.None, "child.*\"\\>$", "general\">")]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, "child.*\"\\>$", "general\">")]
        [InlineData(SearchType.XPath, GrepSearchOption.None, "(//@category)[2]", "general")]
        public void TestReplaceOnFileWith_UTF8_BOM(SearchType type, GrepSearchOption option, string searchFor, string replaceWith)
        {
            // Test for Issue #227 dnGrep inserting extra BOM, and scrambling file
            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase15"), Path.Combine(destinationFolder, "TestCase15"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destinationFolder, "TestCase15"), "books_bom.xml"),
                type, searchFor, option, -1);
            Assert.Single(results);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destinationFolder, "TestCase15", "books_bom.xml");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };
            core.Replace(files, type, searchFor, replaceWith, option, -1);

            using (FileStream stream = File.Open(testFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new(stream, true))
            {
                Assert.Equal(Encoding.UTF8, reader.CurrentEncoding);
                // check there is a BOM
                int bb = reader.BaseStream.ReadByte();
                Assert.Equal(0xEF, bb);
                bb = reader.BaseStream.ReadByte();
                Assert.Equal(0xBB, bb);
                bb = reader.BaseStream.ReadByte();
                Assert.Equal(0xBF, bb);
                // check that there are not two BOMs
                bb = reader.BaseStream.ReadByte();
                Assert.NotEqual(0xEF, bb);
                Assert.Equal('<', bb);
            }
            var fileContent = File.ReadAllLines(Path.Combine(destinationFolder, "TestCase15", "books_bom.xml"), Encoding.UTF8);

            Assert.Equal(38, fileContent.Length);
            string line1 = fileContent[0].Replace("\ufeff", ""); // remove BOM
            Assert.Equal("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", line1);
            Assert.Equal("  <book category=\"general\">", fileContent[8]);
        }

        [Theory]
        [InlineData(SearchType.PlainText, GrepSearchOption.None, "2003", "2002")]
        [InlineData(SearchType.PlainText, GrepSearchOption.Multiline, "2003", "2002")]
        [InlineData(SearchType.Regex, GrepSearchOption.None, "(<\\w+>)\\d+3(</\\w+>)", "${1}2002${2}")]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, "(<\\w+>)\\d+3(</\\w+>)", "${1}2002${2}")]
        [InlineData(SearchType.Regex, GrepSearchOption.None, "(<\\w+>)\\d+3(</\\w+>)$", "${1}2002${2}")]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, "(<\\w+>)\\d+3(</\\w+>)$", "${1}2002${2}")]
        [InlineData(SearchType.XPath, GrepSearchOption.None, "//book[year = 2003]/year", "2002")]
        [InlineData(SearchType.Soundex, GrepSearchOption.None, "03", "02")]
        public void TestSearchAndFilteredReplace(SearchType type, GrepSearchOption option, string searchFor, string replaceWith)
        {
            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase15"), Path.Combine(destinationFolder, "TestCase15"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destinationFolder, "TestCase15"), "books.xml"),
                type, searchFor, option, -1);
            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);

            // mark only the second match for replace
            results[0].Matches[1].ReplaceMatch = true;

            string testFile = Path.Combine(destinationFolder, "TestCase15", "books.xml");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };
            core.Replace(files, type, searchFor, replaceWith, option, -1);

            var fileContent = File.ReadAllText(testFile, Encoding.UTF8);
            Assert.Contains("<year>2003</year>", fileContent);
            Assert.Single(Regex.Matches(fileContent, "2002").Cast<Match>());
            Assert.Single(Regex.Matches(fileContent, "2003").Cast<Match>());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestReplaceAndUndoWorks(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destinationFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "test-file-code.cs"),
                SearchType.PlainText, "body", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Equal(2, results[0].SearchResults.Where(r => !r.IsContext).Count());

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destFolder, @"TestCase3\test-file-code.cs");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.PlainText, "body", "text", GrepSearchOption.None, -1);
            string content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.DoesNotContain("body", content);
            Assert.Contains("text", content);

            GrepCore.Undo(files);
            content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.DoesNotContain("text", content);
            Assert.Contains("body", content);
        }

        [Theory]
        [InlineData(SearchType.Regex, true)]
        [InlineData(SearchType.Regex, false)]
        [InlineData(SearchType.PlainText, true)]
        [InlineData(SearchType.PlainText, false)]
        public void SearchWithMultipleLines(SearchType type, bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase12"), Path.Combine(destFolder, "TestCase12"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase12"), "issue-165.txt"),
                type, "asdf\r\nqwer", GrepSearchOption.Multiline, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal(5, results[0].SearchResults.Count);
        }

        [Theory]
        [InlineData(SearchType.Regex, GrepSearchOption.None, true)]
        [InlineData(SearchType.Regex, GrepSearchOption.None, false)]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, true)]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, false)]
        [InlineData(SearchType.PlainText, GrepSearchOption.None, true)]
        [InlineData(SearchType.PlainText, GrepSearchOption.None, false)]
        [InlineData(SearchType.PlainText, GrepSearchOption.Multiline, true)]
        [InlineData(SearchType.PlainText, GrepSearchOption.Multiline, false)]
        public void TestSearchLongLineWithManyMatches(SearchType type, GrepSearchOption option, bool verbose)
        {
            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase14"), Path.Combine(destinationFolder, "TestCase14"), null, null);
            GrepCore core = new()
            {
                SearchParams = new(false, 0, 0, 0.5, verbose, false)
            };
            Stopwatch sw = new();
            sw.Start();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destinationFolder, "TestCase14"), "*.txt"),
                type, "1234", option, -1);
            sw.Stop();
            Assert.Equal(2, results.Count);
            Assert.Equal(102456, results[0].Matches.Count);
            Assert.Equal(102456, results[1].Matches.Count);
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2.0));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRegexEolToken_Issue_210_SingleLine(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            string path = Path.Combine(destFolder, @"Issue210");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);

            string line = @"24.4.2014 11:45:21.435;T1;End runSingle Job Wfd ID:1895 JobId:166078 JobType:RunWorkFlow Runtime:5.375 Queued:0.015 Total:5.390";

            string content = line + '\r' + line + '\r' + line + '\r';
            File.WriteAllText(Path.Combine(path, @"Issue210mac.txt"), content);
            File.WriteAllText(Path.Combine(path, @"Issue210oneline.txt"), line);
            content = line + '\n' + line + '\n' + line + '\n';
            File.WriteAllText(Path.Combine(path, @"Issue210unix.txt"), content);
            content = line + "\r\n" + line + "\r\n" + line + "\r\n";
            File.WriteAllText(Path.Combine(path, @"Issue210win.txt"), content);

            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(path, "*.txt"), SearchType.Regex, @"[3-9]\d*?.\d\d\d$", GrepSearchOption.None, -1);
            // should be four test files with no EOL, Windows, Unix, and Mac EOL
            Assert.Equal(4, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Single(results[1].Matches);
            Assert.Equal(3, results[2].Matches.Count);
            Assert.Equal(3, results[3].Matches.Count);

            // verify the matches are recorded at the correct location in the files
            // with \r EOL
            Assert.Equal(378, results[0].Matches[2].StartLocation);
            Assert.Equal(383, results[0].Matches[2].EndPosition);

            // with \n EOL
            Assert.Equal(378, results[2].Matches[2].StartLocation);
            Assert.Equal(383, results[2].Matches[2].EndPosition);

            // with \r\n EOL
            Assert.Equal(380, results[3].Matches[2].StartLocation);
            Assert.Equal(385, results[3].Matches[2].EndPosition);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRegexEolToken_Issue_210_MultiLine(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            string path = Path.Combine(destFolder, @"Issue210");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);

            string line = @"24.4.2014 11:45:21.435;T1;End runSingle Job Wfd ID:1895 JobId:166078 JobType:RunWorkFlow Runtime:5.375 Queued:0.015 Total:5.390";

            string content = line + '\r' + line + '\r' + line + '\r';
            File.WriteAllText(Path.Combine(path, @"Issue210mac.txt"), content);
            File.WriteAllText(Path.Combine(path, @"Issue210oneline.txt"), line);
            content = line + '\n' + line + '\n' + line + '\n';
            File.WriteAllText(Path.Combine(path, @"Issue210unix.txt"), content);
            content = line + "\r\n" + line + "\r\n" + line + "\r\n";
            File.WriteAllText(Path.Combine(path, @"Issue210win.txt"), content);

            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(path, "*.txt"), SearchType.Regex, @"[3-9]\d*?.\d\d\d$", GrepSearchOption.Multiline, -1);
            // should be four test files with no EOL, Windows, Unix, and Mac EOL
            Assert.Equal(4, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Single(results[1].Matches);
            Assert.Equal(3, results[2].Matches.Count);
            Assert.Equal(3, results[3].Matches.Count);

            // verify the matches are recorded at the correct location in the files
            // with \r EOL
            Assert.Equal(378, results[0].Matches[2].StartLocation);
            Assert.Equal(383, results[0].Matches[2].EndPosition);

            // with \n EOL
            Assert.Equal(378, results[2].Matches[2].StartLocation);
            Assert.Equal(383, results[2].Matches[2].EndPosition);

            // with \r\n EOL
            Assert.Equal(380, results[3].Matches[2].StartLocation);
            Assert.Equal(385, results[3].Matches[2].EndPosition);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRegexMatchReplace(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destinationFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "test-file-code.cs"),
                SearchType.Regex, @"\w*y", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Equal(2, results[0].SearchResults.Where(r => !r.IsContext).Count());

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destFolder, @"TestCase3\test-file-code.cs");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.Regex, @"\w*y", @"$&Text", GrepSearchOption.None, -1);
            string content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.Contains("bodyText", content);

            GrepCore.Undo(files);
            content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.DoesNotContain("bodyText", content);
            Assert.Contains("body", content);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRegexCaptureReplace(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destinationFolder, "TestCase3"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "test-file-code.cs"),
                SearchType.Regex, @"-(\d)", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Single(results[0].SearchResults.Where(r => !r.IsContext));

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destFolder, @"TestCase3\test-file-code.cs");
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.Regex, @"-(\d)", @"$1", GrepSearchOption.None, -1);
            string content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.DoesNotContain("= -1;", content);
            Assert.Contains("= 1;", content);

            GrepCore.Undo(files);
            content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.DoesNotContain("= 1;", content);
            Assert.Contains("= -1;", content);
        }

        [Theory]
        [InlineData("lorem_unix.txt", @"\\n", false)]
        [InlineData("lorem_win.txt", @"\\r\\n", false)]
        public void TestMultilineSearchAndReplace(string fileName, string newLine, bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase16"), Path.Combine(destinationFolder, "TestCase16"), null, null);
            GrepCore core = new();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase16"), fileName),
                SearchType.Regex, @"\w*\.$.P\w*", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, -1);
            Assert.Single(results);
            var hits = results[0].SearchResults.Where(r => !r.IsContext).ToList();
            Assert.Equal(2, hits.Count);
            Assert.Single(hits[0].Matches);
            Assert.Equal("hendrerit.", hits[0].LineText.Substring(hits[0].Matches[0].StartLocation, hits[0].Matches[0].Length));

            Assert.Single(hits[1].Matches);
            Assert.Equal("Phasellus", hits[1].LineText.Substring(hits[1].Matches[0].StartLocation, hits[1].Matches[0].Length));


            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destFolder, @"TestCase16", fileName);
            List<ReplaceDef> files = new()
            {
                new(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.Regex, @"\w*\.$.P\w*", $"end.{newLine}Start", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, -1);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase16"), fileName),
                            SearchType.Regex, @"\w*\.$.S\w*", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, -1);
            Assert.Single(results);
            hits = results[0].SearchResults.Where(r => !r.IsContext).ToList();
            Assert.Equal(2, hits.Count);
            Assert.Single(hits[0].Matches);
            Assert.Equal("end.", hits[0].LineText.Substring(hits[0].Matches[0].StartLocation, hits[0].Matches[0].Length));

            Assert.Single(hits[1].Matches);
            Assert.Equal("Start", hits[1].LineText.Substring(hits[1].Matches[0].StartLocation, hits[1].Matches[0].Length));
        }

        [Theory]
        [InlineData("*.txt", "test", 15)]
        [InlineData("*.txt", "flash", 1)]
        [InlineData("*.c", "hello", 1)]
        public void TestSearchArchiveFiles(string namePattern, string searchText, int expected)
        {
            string testCase17 = Path.Combine(sourceFolder, @"TestCase17");
            string destFolder = Path.Combine(destinationFolder, @"TestCase17");
            DirectoryInfo di = new(destFolder);
            if (!di.Exists)
            {
                di.Create();
                DirectoryEx.Copy(testCase17, destFolder);
            }

            GrepCore core = new();
            var files = Utils.GetFileList(destFolder, namePattern, string.Empty, false, false, true, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null, false, -1);
            List<GrepSearchResult> results = core.Search(files, SearchType.PlainText, searchText, GrepSearchOption.None, -1);
            Assert.Equal(expected, results.Count);
        }

        [Theory]
        [InlineData("*.txt", "test", 15)]
        [InlineData("*.txt", "flash", 1)]
        [InlineData("*.c", "hello", 1)]
        public void TestSearchArchiveFilesLongPath(string namePattern, string searchText, int expected)
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            string testCase17 = Path.Combine(sourceFolder, @"TestCase17");
            string destFolder = Path.Combine(longDestinationFolder, @"TestCase17");
            DirectoryInfo di = new(destFolder);
            if (!di.Exists)
            {
                di.Create();
                DirectoryEx.Copy(testCase17, destFolder);
            }

            GrepCore core = new();
            var files = Utils.GetFileList(destFolder, namePattern, string.Empty, false, false, true, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null, false, -1);
            List<GrepSearchResult> results = core.Search(files, SearchType.PlainText, searchText, GrepSearchOption.None, -1);
            Assert.Equal(expected, results.Count);
        }

        [Theory]
        [InlineData("plum apple\norange\n", "p..", GrepSearchOption.None, 0, 3, 6, 3)]
        [InlineData("plum apple\r\norange\r\n", "p..", GrepSearchOption.None, 0, 3, 6, 3)]
        [InlineData("plum apple\norange\n", "p..", GrepSearchOption.Multiline, 0, 3, 6, 3)]
        [InlineData("plum apple\r\norange\r\n", "p..", GrepSearchOption.Multiline, 0, 3, 6, 3)]
        [InlineData("plum apple\norange\n", "p..", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, 0, 3, 6, 3)]
        [InlineData("plum apple\r\norange\r\n", "p..", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, 0, 3, 6, 3)]
        [InlineData("plum apple\norange\n", "l.", GrepSearchOption.None, 1, 2, 8, 2)]
        [InlineData("plum apple\r\norange\r\n", "l.", GrepSearchOption.None, 1, 2, 8, 2)]
        [InlineData("plum apple\norange\n", "l.", GrepSearchOption.Multiline, 1, 2, 8, 2)]
        [InlineData("plum apple\r\norange\r\n", "l.", GrepSearchOption.Multiline, 1, 2, 8, 2)]
        public void TestRegexPatternEndingInDot(string content, string pattern, GrepSearchOption regexOption, int start1, int len1, int start2, int len2)
        {
            string path = Path.Combine(destinationFolder, @"TestNewlines");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, @"test.txt"), content);

            GrepCore core = new();
            var results = core.Search(Directory.GetFiles(path, "*.txt"), SearchType.Regex, pattern, regexOption, -1);
            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);
            Assert.Equal(start1, results[0].Matches[0].StartLocation);
            Assert.Equal(len1, results[0].Matches[0].Length);
            Assert.Equal(start2, results[0].Matches[1].StartLocation);
            Assert.Equal(len2, results[0].Matches[1].Length);
        }

        [Theory]
        [InlineData("plum apple\norange\n", "l.", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, 2, 1, 2, "lu", 8, 2, "le")]
        [InlineData("plum apple\r\norange\r\n", "l.", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, 2, 1, 2, "lu", 8, 2, "le")]
        [InlineData("plum apple\norange\n", "l..", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, 2, 1, 3, "lum", 8, 3, "le\n")]
        [InlineData("plum apple\r\norange\r\n", "l..", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, 2, 1, 3, "lum", 8, 4, "le\r\n")]
        [InlineData("plum apple\norange\n", "l...", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, 2, 1, 4, "lum ", 8, 4, "le\no")]
        [InlineData("plum apple\r\norange\r\n", "l...", GrepSearchOption.Multiline | GrepSearchOption.SingleLine, 2, 1, 4, "lum ", 8, 5, "le\r\no")]
        [InlineData("plum apple\norange\n", "l.", GrepSearchOption.SingleLine, 2, 1, 2, "lu", 8, 2, "le")]
        [InlineData("plum apple\r\norange\r\n", "l.", GrepSearchOption.SingleLine, 2, 1, 2, "lu", 8, 2, "le")]
        [InlineData("plum apple\norange\n", "l..", GrepSearchOption.SingleLine, 2, 1, 3, "lum", 8, 3, "le\n")]
        [InlineData("plum apple\r\norange\r\n", "l..", GrepSearchOption.SingleLine, 2, 1, 3, "lum", 8, 4, "le\r\n")]
        [InlineData("plum apple\norange\n", "l...", GrepSearchOption.SingleLine, 1, 1, 4, "lum ", 0, 0, null)]
        [InlineData("plum apple\r\norange\r\n", "l...", GrepSearchOption.SingleLine, 1, 1, 4, "lum ", 0, 0, null)]
        public void TestRegexPatternSinglelineEndingInDot(string content, string pattern, GrepSearchOption regexOption, int matches, int start1, int len1, string text1, int start2, int len2, string text2)
        {
            string path = Path.Combine(destinationFolder, @"TestNewlines");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, @"test.txt"), content);

            GrepCore core = new();
            var results = core.Search(Directory.GetFiles(path, "*.txt"), SearchType.Regex, pattern, regexOption, -1);
            Assert.Single(results);
            Assert.Equal(matches, results[0].Matches.Count);
            Assert.Equal(start1, results[0].Matches[0].StartLocation);
            Assert.Equal(len1, results[0].Matches[0].Length);
            Assert.Equal(text1, content.Substring(results[0].Matches[0].StartLocation, results[0].Matches[0].Length));
            if (matches > 1)
            {
                Assert.Equal(start2, results[0].Matches[1].StartLocation);
                Assert.Equal(len2, results[0].Matches[1].Length);
                Assert.Equal(text2, content.Substring(results[0].Matches[1].StartLocation, results[0].Matches[1].Length));
            }
        }

        [Theory]
        [InlineData("abcd", "a.*", 0, 4)]
        [InlineData("abcd", "a.*$", 0, 4)]
        [InlineData("abcd\rline2\rline3\r", "a.*", 0, 4)]
        [InlineData("abcd\nline2\nline3\n", "a.*", 0, 4)]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*", 0, 4)]
        [InlineData("abcd\rline2\rline3\r", "a.*$", 0, 4)]
        [InlineData("abcd\nline2\nline3\n", "a.*$", 0, 4)]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*$", 0, 4)]
        [InlineData("line1\rabcd\rline3\r", "a.*", 6, 4)]
        [InlineData("line1\nabcd\n\nline3", "a.*", 6, 4)]
        [InlineData("line1\r\nabcd\r\nline3\r\n", "a.*", 7, 4)]
        [InlineData("line1\rabcd\rline3\r", "a.*$", 6, 4)]
        [InlineData("line1\nabcd\nline3\n", "a.*$", 6, 4)]
        [InlineData("line1\r\nabcd\r\nline3\r\n", "a.*$", 7, 4)]
        [InlineData("line1\rline2\rabcd\r", "a.*", 12, 4)]
        [InlineData("line1\nline2\nabcd\n", "a.*", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd\r\n", "a.*", 14, 4)]
        [InlineData("line1\rline2\rabcd\r", "a.*$", 12, 4)]
        [InlineData("line1\nline2\nabcd\n", "a.*$", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd\r\n", "a.*$", 14, 4)]
        [InlineData("line1\rline2\rabcd", "a.*", 12, 4)]
        [InlineData("line1\nline2\nabcd", "a.*", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd", "a.*", 14, 4)]
        [InlineData("line1\rline2\rabcd", "a.*$", 12, 4)]
        [InlineData("line1\nline2\nabcd", "a.*$", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd", "a.*$", 14, 4)]
        [InlineData("line1\r\nline2\r\nline3\r\nabcd", "a.*$", 21, 4)]
        [InlineData("line1\r\nline2\r\nline3\r\nline4\r\nabcd", "a.*$", 28, 4)]
        [InlineData("line1\r\nline2\nline3\r\nline4\r\nabcd", "a.*$", 27, 4)] //mixed newlines
        [InlineData("line1\r\nline2\r\nline3\r\n\r\nabcd\r\n", "a.*$", 23, 4)] // empty line
        [InlineData("line1\r\nline2\r\nline3\r\n\r\n\r\nabcd\r\n", "a.*$", 25, 4)] // empty line
        [InlineData("line1\r\nline2\r\nline3\r\n\n\r\nabcd\r\n", "a.*$", 24, 4)] // mixed empty line
        public void TestRegexShouldNotReturnNewlineChar(string content, string pattern, int start, int length)
        {
            string path = Path.Combine(destinationFolder, @"TestNewlines");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, @"test.txt"), content);

            GrepCore core = new();
            var results = core.Search(Directory.GetFiles(path, "*.txt"), SearchType.Regex, pattern, GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal(start, results[0].Matches[0].StartLocation);
            Assert.Equal(length, results[0].Matches[0].Length);
        }

        [Theory]
        [InlineData("abcd", "a.*", 0, 4)]
        [InlineData("abcd", "a.*$", 0, 4)]
        [InlineData("abcd\rline2\rline3\r", "a.*", 0, 5)]
        [InlineData("abcd\nline2\nline3\n", "a.*", 0, 5)]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*", 0, 6)]
        [InlineData("abcd\rline2\rline3\r", "a.*$", 0, 5)]
        [InlineData("abcd\nline2\nline3\n", "a.*$", 0, 5)]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*$", 0, 6)]
        [InlineData("line1\rline2\rabcd\r", "a.*", 12, 5)]
        [InlineData("line1\nline2\nabcd\n", "a.*", 12, 5)]
        [InlineData("line1\r\nline2\r\nabcd\r\n", "a.*", 14, 6)]
        [InlineData("line1\rline2\rabcd\r", "a.*$", 12, 5)]
        [InlineData("line1\nline2\nabcd\n", "a.*$", 12, 5)]
        [InlineData("line1\r\nline2\r\nabcd\r\n", "a.*$", 14, 6)]
        [InlineData("line1\rline2\rabcd", "a.*", 12, 4)]
        [InlineData("line1\nline2\nabcd", "a.*", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd", "a.*", 14, 4)]
        [InlineData("line1\rline2\rabcd", "a.*$", 12, 4)]
        [InlineData("line1\nline2\nabcd", "a.*$", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd", "a.*$", 14, 4)]
        [InlineData("abcd\r\nline2\nline3\r\n", "a.*", 0, 6)]  // mixed newlines
        [InlineData("abcd\nline2\r\nline3\r\n", "a.*", 0, 5)]  // mixed newlines
        [InlineData("line1\nline2\r\nabcd\r\n", "a.*", 13, 6)] // mixed newlines
        [InlineData("line1\nline2\r\nabcd", "a.*", 13, 4)]     // mixed newlines
        [InlineData("line1\rline2\r\nabcd\r\n", "a.*", 13, 6)] // mixed newlines
        [InlineData("line1\rline2\r\nabcd", "a.*", 13, 4)]     // mixed newlines
        public void TestRegexSinglelineShouldReturnNewlineChar(string content, string pattern, int start, int length)
        {
            string path = Path.Combine(destinationFolder, @"TestNewlines");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, @"test.txt"), content);

            GrepCore core = new();
            var results = core.Search(Directory.GetFiles(path, "*.txt"), SearchType.Regex, pattern, GrepSearchOption.SingleLine, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal(start, results[0].Matches[0].StartLocation);
            Assert.Equal(length, results[0].Matches[0].Length);
        }

        [Theory]
        [InlineData("abcd", "a.*", 0, 4)]
        [InlineData("abcd", "a.*$", 0, 4)]
        [InlineData("abcd\rline2\rline3\r", "a.*", 0, 4)]
        [InlineData("abcd\nline2\nline3\n", "a.*", 0, 4)]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*", 0, 4)]
        [InlineData("abcd\rline2\rline3\r", "a.*$", 0, 4)]
        [InlineData("abcd\nline2\nline3\n", "a.*$", 0, 4)]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*$", 0, 4)]
        [InlineData("line1\rline2\rabcd\r", "a.*", 12, 4)]
        [InlineData("line1\nline2\nabcd\n", "a.*", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd\r\n", "a.*", 14, 4)]
        [InlineData("line1\rline2\rabcd\r", "a.*$", 12, 4)]
        [InlineData("line1\nline2\nabcd\n", "a.*$", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd\r\n", "a.*$", 14, 4)]
        [InlineData("line1\rline2\rabcd", "a.*", 12, 4)]
        [InlineData("line1\nline2\nabcd", "a.*", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd", "a.*", 14, 4)]
        [InlineData("line1\rline2\rabcd", "a.*$", 12, 4)]
        [InlineData("line1\nline2\nabcd", "a.*$", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd", "a.*$", 14, 4)]
        [InlineData("abcd\r\nline2\nline3\r\n", "a.*", 0, 4)]  // mixed newlines
        [InlineData("abcd\nline2\r\nline3\r\n", "a.*", 0, 4)]  // mixed newlines
        [InlineData("line1\nline2\r\nabcd\r\n", "a.*", 13, 4)] // mixed newlines
        [InlineData("abcd\rline2\r\nline3\r\n", "a.*", 0, 4)]  // mixed newlines
        [InlineData("line1\rline2\r\nabcd\r\n", "a.*", 13, 4)] // mixed newlines
        public void TestRegexMultilineShouldNotReturnNewlineChar(string content, string pattern, int start, int length)
        {
            string path = Path.Combine(destinationFolder, @"TestNewlines");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, @"test.txt"), content);

            GrepCore core = new();
            var results = core.Search(Directory.GetFiles(path, "*.txt"), SearchType.Regex, pattern, GrepSearchOption.Multiline, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal(start, results[0].Matches[0].StartLocation);
            Assert.Equal(length, results[0].Matches[0].Length);
        }

        [Theory]
        [InlineData("abcd", "a.*", 0, 4)]
        [InlineData("abcd", "a.*$", 0, 4)]
        [InlineData("abcd\rline2\rline3\r", "a.*", 0, 17)]
        [InlineData("abcd\nline2\nline3\n", "a.*", 0, 17)]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*", 0, 20)]
        [InlineData("abcd\rline2\rline3\r", "a.*$", 0, 17)]
        [InlineData("abcd\nline2\nline3\n", "a.*$", 0, 17)]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*$", 0, 20)]
        [InlineData("line1\rline2\rabcd\r", "a.*", 12, 5)]
        [InlineData("line1\nline2\nabcd\n", "a.*", 12, 5)]
        [InlineData("line1\r\nline2\r\nabcd\r\n", "a.*", 14, 6)]
        [InlineData("line1\rline2\rabcd\r", "a.*$", 12, 5)]
        [InlineData("line1\nline2\nabcd\n", "a.*$", 12, 5)]
        [InlineData("line1\r\nline2\r\nabcd\r\n", "a.*$", 14, 6)]
        [InlineData("line1\rline2\rabcd", "a.*", 12, 4)]
        [InlineData("line1\nline2\nabcd", "a.*", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd", "a.*", 14, 4)]
        [InlineData("line1\rline2\rabcd", "a.*$", 12, 4)]
        [InlineData("line1\nline2\nabcd", "a.*$", 12, 4)]
        [InlineData("line1\r\nline2\r\nabcd", "a.*$", 14, 4)]
        public void TestRegexMultilineAndSinglelineShouldReturnNewlineChars(string content, string pattern, int start, int length)
        {
            string path = Path.Combine(destinationFolder, @"TestNewlines");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, @"test.txt"), content);

            GrepCore core = new();
            var results = core.Search(Directory.GetFiles(path, "*.txt"), SearchType.Regex, pattern, GrepSearchOption.Multiline | GrepSearchOption.SingleLine, -1);
            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal(start, results[0].Matches[0].StartLocation);
            Assert.Equal(length, results[0].Matches[0].Length);
        }

        [Theory]
        [InlineData("abcd\rline2\rline3\r", "a.*", 5, "joinline2\rline3\r")]
        [InlineData("abcd\nline2\nline3\n", "a.*", 5, "joinline2\nline3\n")]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*", 6, "joinline2\r\nline3\r\n")]
        [InlineData("abcd\rline2\rline3\r", "a.*$", 5, "joinline2\rline3\r")]
        [InlineData("abcd\nline2\nline3\n", "a.*$", 5, "joinline2\nline3\n")]
        [InlineData("abcd\r\nline2\r\nline3\r\n", "a.*$", 6, "joinline2\r\nline3\r\n")]
        public void TestReplaceWithSingleLineFlag(string content, string pattern, int matchLength, string expected)
        {
            // with the SingleLine flag, the first newline char(s) should be captured in the match and replaced

            string path = Path.Combine(destinationFolder, @"TestNewlines");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);

            string file = Path.Combine(path, @"test.txt");
            File.WriteAllText(file, content);
            var files = Directory.GetFiles(path, "*.txt");

            GrepCore core = new();
            var results = core.Search(files, SearchType.Regex, pattern, GrepSearchOption.SingleLine, -1);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal(matchLength, results[0].Matches[0].Length);

            // mark for replace
            List<ReplaceDef> replaceFiles = new();
            results[0].Matches[0].ReplaceMatch = true;
            replaceFiles.Add(new(file, results[0].Matches));

            core.Replace(replaceFiles, SearchType.Regex, pattern, "join", GrepSearchOption.SingleLine, -1);
            string fileContent = File.ReadAllText(file, Encoding.UTF8);

            Assert.Equal(expected, fileContent);
        }

        [Theory]
        [InlineData(@"{$R *.dfm}", @"\{\$", 2, @"{&", @"{&R *.dfm}")]
        public void TestIssue503EscapedDollar(string content, string pattern, int matchLength, string replace, string expected)
        {
            string path = Path.Combine(destinationFolder, @"TestEscapedDollar");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);

            string file = Path.Combine(path, @"test.txt");
            File.WriteAllText(file, content);
            var files = Directory.GetFiles(path, "*.txt");

            GrepCore core = new();
            var results = core.Search(files, SearchType.Regex, pattern, GrepSearchOption.None, -1);

            Assert.Single(results);
            Assert.Single(results[0].Matches);
            Assert.Equal(matchLength, results[0].Matches[0].Length);

            // mark for replace
            List<ReplaceDef> replaceFiles = new();
            results[0].Matches[0].ReplaceMatch = true;
            replaceFiles.Add(new(file, results[0].Matches));

            core.Replace(replaceFiles, SearchType.Regex, pattern, replace, GrepSearchOption.SingleLine, -1);
            string fileContent = File.ReadAllText(file, Encoding.UTF8);

            Assert.Equal(expected, fileContent);
        }

        [Fact]
        public void TestArchiveFilters()
        {
            // this test is repeated from the same in UtilsTest because
            // ArchiveEngine and ArchiveDirectory each have code to enumerate 
            // and filter files in archives

            string testCase19 = Path.Combine(sourceFolder, @"TestCase19");
            string destFolder = Path.Combine(destinationFolder, @"TestCase19");
            DirectoryInfo di = new(destFolder);
            if (!di.Exists)
            {
                di.Create();
                DirectoryEx.Copy(testCase19, destFolder);
            }

            GrepCore core = new()
            {
                // all files
                FileFilter = new(destFolder, "*.*", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null)
            };
            var results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(18, results.Count);

            // all .ttt files
            core.FileFilter = new(destFolder, "*.ttt", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(13, results.Count);

            // all but .ttt files
            core.FileFilter = new(destFolder, "*.*", "*.ttt", false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(5, results.Count);

            // all .md files
            core.FileFilter = new(destFolder, "*.md", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Single(results);

            // exclude below depth2
            core.FileFilter = new(destFolder, "*.ttt", @"depth2\*", false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(8, results.Count);

            // regex filter
            core.FileFilter = new(destFolder, @"\bl.*", string.Empty, true, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(2, results.Count);

            // regex exclude filter
            core.FileFilter = new(destFolder, ".*", @"\b[abl]", true, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(13, results.Count);

            // exclude hidden
            core.FileFilter = new(destFolder, "*.ttt", string.Empty, false, false, false, true, -1, false,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(10, results.Count);

            // exclude binary
            core.FileFilter = new(destFolder, "*.*", string.Empty, false, false, false, true, -1, true,
                false, true, false, 0, 0, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(17, results.Count);

            // size filter
            core.FileFilter = new(destFolder, "*.ttt", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 10, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(12, results.Count);

            // date filter
            core.FileFilter = new(destFolder, "*.ttt", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.Modified, null, new DateTime(2019, 1, 1));
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(4, results.Count);

            // shebang filter
            core.FileFilter = new(destFolder, "#!*python", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            results = core.Search(Directory.GetFiles(destFolder, "*.*"), SearchType.Regex, @"\w+",
                GrepSearchOption.None, -1);
            Assert.Equal(2, results.Count);
        }

        [Theory]
        [InlineData("a1111 b2222\r\nc3333 d4444", true)]
        [InlineData(" a1111 b2222\r\n c3333 d4444", true)]
        [InlineData("aa1111 b2222\r\ncc3333 d4444", true)]
        [InlineData("a1111 b2222 \r\nc3333 d4444 ", true)]
        [InlineData("zz a1111 b2222\r\nzz c3333 d4444", true)]
        [InlineData("a1111 z b2222\r\nc3333 z d4444", true)]
        [InlineData("zz a1111 zz b2222\r\nzz c3333 zz d4444", true)]
        [InlineData("a1111 b2222\r\nc3333 d4444", false)]
        [InlineData(" a1111 b2222\r\n c3333 d4444", false)]
        [InlineData("aa1111 b2222\r\ncc3333 d4444", false)]
        [InlineData("a1111 b2222 \r\nc3333 d4444 ", false)]
        [InlineData("zz a1111 b2222\r\nzz c3333 d4444", false)]
        [InlineData("a1111 z b2222\r\nc3333 z d4444", false)]
        [InlineData("zz a1111 zz b2222\r\nzz c3333 zz d4444", false)]
        public void TestCaptureGroupHighlightMutlipleMatches1(string content, bool verboseMatchCount)
        {
            string pattern = @"\w(\d+)";
            string[] textLines = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string path = Path.Combine(destinationFolder, @"TestCaptureGroupHighlight");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);

            string file = Path.Combine(path, @"test.txt");
            File.WriteAllText(file, content);
            var files = Directory.GetFiles(path, "*.txt");

            GrepCore core = new()
            {
                SearchParams = new(false, 0, 0, 0, verboseMatchCount, false)
            };
            var results = core.Search(files, SearchType.Regex, pattern, GrepSearchOption.None, -1);

            Assert.Single(results);
            Assert.Equal(4, results[0].Matches.Count);

            List<GrepLine> lines = new();
            using (StringReader reader = new(content))
            {
                lines = Utils.GetLinesEx(reader, results[0].Matches, 0, 0);
            }

            Assert.Equal(2, lines.Count);

            GrepLine line = lines[0];
            Assert.Equal(1, line.LineNumber);
            Assert.Equal(textLines[0], line.LineText);
            Assert.Equal(2, line.Matches.Count);

            GrepMatch match = line.Matches[0];
            Assert.Equal("a1111", line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Single(match.Groups);
            GrepCaptureGroup group = match.Groups[0];
            Assert.Equal("1111", line.LineText.Substring(group.StartLocation, group.Length));

            match = line.Matches[1];
            Assert.Equal("b2222", line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Single(match.Groups);
            group = match.Groups[0];
            Assert.Equal("2222", line.LineText.Substring(group.StartLocation, group.Length));

            line = lines[1];
            Assert.Equal(2, line.LineNumber);
            Assert.Equal(textLines[1], line.LineText);
            Assert.Equal(2, line.Matches.Count);

            match = line.Matches[0];
            Assert.Equal("c3333", line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Single(match.Groups);
            group = match.Groups[0];
            Assert.Equal("3333", line.LineText.Substring(group.StartLocation, group.Length));

            match = line.Matches[1];
            Assert.Equal("d4444", line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Single(match.Groups);
            group = match.Groups[0];
            Assert.Equal("4444", line.LineText.Substring(group.StartLocation, group.Length));
        }

        [Theory]
        [InlineData("a1111 b2222\r\nc3333 d4444", true)]
        [InlineData(" a1111 b2222\r\n c3333 d4444", true)]
        [InlineData("aa1111 b2222\r\ncc3333 d4444", true)]
        [InlineData("a1111 b2222 \r\nc3333 d4444 ", true)]
        [InlineData("zz a1111 b2222\r\nzz c3333 d4444", true)]
        [InlineData("a1111 z b2222\r\nc3333 z d4444", true)]
        [InlineData("zz a1111 zz b2222\r\nzz c3333 zz d4444", true)]
        [InlineData("a1111 b2222\r\nc3333 d4444", false)]
        [InlineData(" a1111 b2222\r\n c3333 d4444", false)]
        [InlineData("aa1111 b2222\r\ncc3333 d4444", false)]
        [InlineData("a1111 b2222 \r\nc3333 d4444 ", false)]
        [InlineData("zz a1111 b2222\r\nzz c3333 d4444", false)]
        [InlineData("a1111 z b2222\r\nc3333 z d4444", false)]
        [InlineData("zz a1111 zz b2222\r\nzz c3333 zz d4444", false)]
        public void TestCaptureGroupHighlightMutlipleMatches2(string content, bool verboseMatchCount)
        {
            string pattern = @"\w(\d+)";
            string[] textLines = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string path = Path.Combine(destinationFolder, @"TestCaptureGroupHighlight");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);

            string file = Path.Combine(path, @"test.txt");
            File.WriteAllText(file, content);
            var files = Directory.GetFiles(path, "*.txt");

            GrepCore core = new()
            {
                SearchParams = new(false, 0, 0, 0, verboseMatchCount, false)
            };
            var results = core.Search(files, SearchType.Regex, pattern, GrepSearchOption.Multiline | GrepSearchOption.SingleLine, -1);

            Assert.Single(results);
            Assert.Equal(4, results[0].Matches.Count);

            List<GrepLine> lines = new();
            using (StringReader reader = new(content))
            {
                lines = Utils.GetLinesEx(reader, results[0].Matches, 0, 0);
            }

            Assert.Equal(2, lines.Count);

            GrepLine line = lines[0];
            Assert.Equal(1, line.LineNumber);
            Assert.Equal(textLines[0], line.LineText);
            Assert.Equal(2, line.Matches.Count);

            GrepMatch match = line.Matches[0];
            Assert.Equal("a1111", line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Single(match.Groups);
            GrepCaptureGroup group = match.Groups[0];
            Assert.Equal("1111", line.LineText.Substring(group.StartLocation, group.Length));

            match = line.Matches[1];
            Assert.Equal("b2222", line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Single(match.Groups);
            group = match.Groups[0];
            Assert.Equal("2222", line.LineText.Substring(group.StartLocation, group.Length));

            line = lines[1];
            Assert.Equal(2, line.LineNumber);
            Assert.Equal(textLines[1], line.LineText);
            Assert.Equal(2, line.Matches.Count);

            match = line.Matches[0];
            Assert.Equal("c3333", line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Single(match.Groups);
            group = match.Groups[0];
            Assert.Equal("3333", line.LineText.Substring(group.StartLocation, group.Length));

            match = line.Matches[1];
            Assert.Equal("d4444", line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Single(match.Groups);
            group = match.Groups[0];
            Assert.Equal("4444", line.LineText.Substring(group.StartLocation, group.Length));
        }

        [Theory]
        [InlineData("a1111 b2222\r\nc3333 d4444", true)]
        [InlineData(" a1111 b2222\r\n c3333 d4444", true)]
        [InlineData("aa1111 b2222\r\ncc3333 d4444", true)]
        [InlineData("a1111 b2222 \r\nc3333 d4444 ", true)]
        [InlineData("zz a1111 b2222\r\nzz c3333 d4444", true)]
        [InlineData("a1111 z b2222\r\nc3333 z d4444", true)]
        [InlineData("zz a1111 zz b2222\r\nzz c3333 zz d4444", true)]
        [InlineData("a1111 b2222\r\nc3333 d4444", false)]
        [InlineData(" a1111 b2222\r\n c3333 d4444", false)]
        [InlineData("aa1111 b2222\r\ncc3333 d4444", false)]
        [InlineData("a1111 b2222 \r\nc3333 d4444 ", false)]
        [InlineData("zz a1111 b2222\r\nzz c3333 d4444", false)]
        [InlineData("a1111 z b2222\r\nc3333 z d4444", false)]
        [InlineData("zz a1111 zz b2222\r\nzz c3333 zz d4444", false)]
        public void TestCaptureGroupHighlightSingleMatchesMultipleGroups(string content, bool verboseMatchCount)
        {
            string pattern = @"a(\d+).+b(\d+).+c(\d+).+d(\d+)\s?$";
            string[] textLines = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string path = Path.Combine(destinationFolder, @"TestCaptureGroupHighlight");
            if (Directory.Exists(path))
                Utils.DeleteFolder(path);
            Directory.CreateDirectory(path);

            string file = Path.Combine(path, @"test.txt");
            File.WriteAllText(file, content);
            var files = Directory.GetFiles(path, "*.txt");

            GrepCore core = new()
            {
                SearchParams = new(false, 0, 0, 0, verboseMatchCount, false)
            };
            var results = core.Search(files, SearchType.Regex, pattern, GrepSearchOption.Multiline | GrepSearchOption.SingleLine, -1);

            Assert.Single(results);
            Assert.Single(results[0].Matches);

            List<GrepLine> lines = new();
            using (StringReader reader = new(content))
            {
                lines = Utils.GetLinesEx(reader, results[0].Matches, 0, 0);
            }

            Assert.Equal(2, lines.Count);

            GrepLine line = lines[0];
            Assert.Equal(1, line.LineNumber);
            Assert.Equal(textLines[0], line.LineText);
            Assert.Single(line.Matches);

            GrepMatch match = line.Matches[0];
            //Assert.Equal(textLines[0], line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Equal(2, match.Groups.Count);
            GrepCaptureGroup group = match.Groups[0];
            Assert.Equal("1111", line.LineText.Substring(group.StartLocation, group.Length));
            group = match.Groups[1];
            Assert.Equal("2222", line.LineText.Substring(group.StartLocation, group.Length));

            line = lines[1];
            Assert.Equal(2, line.LineNumber);
            Assert.Equal(textLines[1], line.LineText);
            Assert.Single(line.Matches);

            match = line.Matches[0];
            //Assert.Equal(textLines[1], line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Equal(2, match.Groups.Count);
            group = match.Groups[0];
            Assert.Equal("3333", line.LineText.Substring(group.StartLocation, group.Length));
            group = match.Groups[1];
            Assert.Equal("4444", line.LineText.Substring(group.StartLocation, group.Length));
        }

        [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
        private static partial Regex GuidRegex();

#pragma warning restore SYSLIB1045

    }
}
