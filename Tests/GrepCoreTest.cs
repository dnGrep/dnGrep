using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using dnGREP.Common;
using dnGREP.Engines;
using Xunit;
using Xunit.Extensions;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace Tests
{
    public class GrepCoreTest : TestBase, IDisposable
    {
        private readonly string sourceFolder;
        private string destinationFolder;

        public GrepCoreTest()
        {
            sourceFolder = Path.Combine(GetDllPath(), "Files");
            destinationFolder = Path.Combine(Path.GetTempPath(), "dnGrepTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(destinationFolder);
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
            // if long path, delete folder from the top of the long path
            string folder = destinationFolder;
            while (folder.Contains("aaaaaaaaaaaaaaaaaaaa"))
                folder = Path.GetDirectoryName(folder);

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
            GrepCore core = new GrepCore();
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
            Assert.Empty(core.Search(new string[] { }, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchPlainReturnsCorrectNumber(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dnGREP", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive, -1);
            Assert.Empty(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
            Assert.Empty(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.None, -1);
            Assert.Single(results);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "string", GrepSearchOption.None, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);

            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
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
            Assert.Empty(core.Search(new string[] { }, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchContainsValidPattern(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new GrepCore();
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
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "public", GrepSearchOption.CaseSensitive, -1);
            Assert.True(results.Count > 1);
            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "public", GrepSearchOption.StopAfterFirstMatch, -1);
            Assert.Single(results);
            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "", GrepSearchOption.CaseSensitive, -1);
            Assert.True(results.Count > 1);
            results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "*.*"), SearchType.PlainText, "", GrepSearchOption.StopAfterFirstMatch, -1);
            Assert.Single(results);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestResultSequence(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;
            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destFolder, "TestCase3"), null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase3"), "test-file-plain-big.txt"), SearchType.PlainText, "string", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);
            var resultLines = results[0].GetLinesWithContext(3, 3);
            int lastLine = 0;
            foreach (var line in resultLines)
            {
                if (line.LineNumber <= lastLine)
                    Assert.True(false, "Lines are not sequential");
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
            GrepCore core = new GrepCore();
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
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase4"), "app.config"), SearchType.XPath,
                "//setting", GrepSearchOption.CaseSensitive, -1);
            Assert.Single(results);
            Assert.Equal(84, results[0].SearchResults.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchXPathWithMissingXmlDeclaration(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase4"), Path.Combine(destFolder, "TestCase4"), null, null);
            GrepCore core = new GrepCore();
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
            GrepCore core = new GrepCore();
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
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
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
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase4"), "books_no_decl.xml"),
                SearchType.XPath, "(//@currency)", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Equal(5, results[0].Matches.Count);

            // mark 2nd and 4th matches for replace
            results[0].Matches[1].ReplaceMatch = true;
            results[0].Matches[3].ReplaceMatch = true;

            string testFile = Path.Combine(destinationFolder, "TestCase4", "books_no_decl.xml");
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
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
            GrepCore core = new GrepCore();
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
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
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
            GrepCore core = new GrepCore();
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
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
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
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchWholeWord_Issue_114_Regex(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase10"), Path.Combine(destFolder, "TestCase10"), null, null);
            GrepCore core = new GrepCore();
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
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase10"), "issue-114.txt"), SearchType.PlainText, "protected", GrepSearchOption.WholeWord, -1);
            Assert.Single(results);
            Assert.Single(results[0].SearchResults);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestReplaceSpecialChars(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

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
        [InlineData(true)]
        [InlineData(false)]
        public void TestReplaceWithNewLineWorks(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase8"), Path.Combine(destFolder, "TestCase8"), null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destFolder, "TestCase8"), "test.txt"), SearchType.Regex, "here", GrepSearchOption.None, -1);
            Assert.Single(results);
            Assert.Single(results[0].SearchResults);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destFolder, @"TestCase8\test.txt");
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.Regex, "here", "\\\\n", GrepSearchOption.None, -1);
            Assert.Equal(2, File.ReadAllText(testFile, Encoding.ASCII).Trim().Split('\n').Length);
        }

        private Regex guidPattern = new Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

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
            GrepCore core = new GrepCore();
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
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
            };

            core.Replace(files, type, "here", "$(guid)", GrepSearchOption.None, -1);
            string fileContent = File.ReadAllText(testFile, Encoding.UTF8).Trim();
            Assert.Equal(6, guidPattern.Matches(fileContent).Count);
            HashSet<string> uniqueGuids = new HashSet<string>();
            foreach (Match match in guidPattern.Matches(fileContent))
            {
                if (!uniqueGuids.Contains(match.Value))
                    uniqueGuids.Add(match.Value);
                else
                    Assert.True(false, "All GUIDs should be unique.");
            }
        }

        [Fact]
        public void TestGuidxReplaceWithPatternRegex()
        {
            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase9"), Path.Combine(destinationFolder, "TestCase9"), null, null);
            GrepCore core = new GrepCore();
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
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
            };

            // all instances of the same string matched will get the same guid
            core.Replace(files, SearchType.Regex, "h\\wre", "$(guidx)", GrepSearchOption.None, -1);
            string fileContent = File.ReadAllText(testFile, Encoding.UTF8).Trim();
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
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destinationFolder, "TestCase15"), "books.xml"),
                type, searchFor, option, -1);
            Assert.Single(results);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destinationFolder, "TestCase15", "books.xml");
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
            };
            core.Replace(files, type, searchFor, replaceWith, option, -1);

            using (FileStream stream = File.Open(testFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(stream, true))
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
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destinationFolder, "TestCase15"), "books_bom.xml"),
                type, searchFor, option, -1);
            Assert.Single(results);

            // mark all matches for replace
            foreach (var match in results[0].Matches)
            {
                match.ReplaceMatch = true;
            }

            string testFile = Path.Combine(destinationFolder, "TestCase15", "books_bom.xml");
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
            };
            core.Replace(files, type, searchFor, replaceWith, option, -1);

            using (FileStream stream = File.Open(testFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(stream, true))
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
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(Path.Combine(destinationFolder, "TestCase15"), "books.xml"),
                type, searchFor, option, -1);
            Assert.Single(results);
            Assert.Equal(2, results[0].Matches.Count);

            // mark only the second match for replace
            results[0].Matches[1].ReplaceMatch = true;

            string testFile = Path.Combine(destinationFolder, "TestCase15", "books.xml");
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
            };
            core.Replace(files, type, searchFor, replaceWith, option, -1);

            var fileContent = File.ReadAllText(testFile, Encoding.UTF8);
            Assert.Contains("<year>2003</year>", fileContent);
            Assert.Single(Regex.Matches(fileContent, "2002"));
            Assert.Single(Regex.Matches(fileContent, "2003"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestReplaceAndUndoWorks(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(Path.Combine(sourceFolder, "TestCase3"), Path.Combine(destinationFolder, "TestCase3"), null, null);
            GrepCore core = new GrepCore();
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
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.PlainText, "body", "text", GrepSearchOption.None, -1);
            string content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.DoesNotContain("body", content);
            Assert.Contains("text", content);

            core.Undo(files);
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
            GrepCore core = new GrepCore();
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
            GrepCore core = new GrepCore
            {
                SearchParams = new GrepEngineInitParams(false, 0, 0, 0.5, verbose, false)
            };
            Stopwatch sw = new Stopwatch();
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

            GrepCore core = new GrepCore();
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

            GrepCore core = new GrepCore();
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
            GrepCore core = new GrepCore();
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
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.Regex, @"\w*y", @"$&Text", GrepSearchOption.None, -1);
            string content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.Contains("bodyText", content);

            core.Undo(files);
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
            GrepCore core = new GrepCore();
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
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
            };

            core.Replace(files, SearchType.Regex, @"-(\d)", @"$1", GrepSearchOption.None, -1);
            string content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.DoesNotContain("= -1;", content);
            Assert.Contains("= 1;", content);

            core.Undo(files);
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
            GrepCore core = new GrepCore();
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
            List<ReplaceDef> files = new List<ReplaceDef>
            {
                new ReplaceDef(testFile, results[0].Matches)
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
    }
}
