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
        string sourceFolder;
        string destinationFolder;

        public GrepCoreTest()
        {
            sourceFolder = GetDllPath() + "\\Files";
            destinationFolder = Path.GetTempPath() + Guid.NewGuid().ToString();
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
            if (Directory.Exists(destinationFolder))
                Utils.DeleteFolder(destinationFolder);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchRegexReturnsCorrectNumber(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase3", destFolder + "\\TestCase3", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dnGR\\wP", GrepSearchOption.CaseSensitive, -1);
            Assert.Equal(results.Count, 1);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.CaseSensitive, -1);
            Assert.Equal(results.Count, 0);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
            Assert.Equal(results.Count, 0);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.Regex, "dngr\\wp", GrepSearchOption.None, -1);
            Assert.Equal(results.Count, 1);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.None, -1);
            Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
            Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.Regex, "string", GrepSearchOption.Multiline, -1);
            Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            Assert.Empty(core.Search(null, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
            Assert.Empty(core.Search(new string[] { }, SearchType.Regex, "string", GrepSearchOption.Multiline, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchPlainReturnsCorrectNumber(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase3", destFolder + "\\TestCase3", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dnGREP", GrepSearchOption.CaseSensitive, -1);
            Assert.Equal(results.Count, 1);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive, -1);
            Assert.Equal(results.Count, 0);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
            Assert.Equal(results.Count, 0);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.None, -1);
            Assert.Equal(results.Count, 1);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.None, -1);
            Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
            Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.Multiline, -1);
            Assert.Equal(results.Count, 2);
            Assert.Equal(results[0].Matches.Count, 3);
            Assert.Equal(results[1].Matches.Count, 282);

            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dngrep", GrepSearchOption.CaseSensitive, -1);
            Assert.Equal(results.Count, 0);

            Assert.Empty(core.Search(null, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
            Assert.Empty(core.Search(new string[] { }, SearchType.PlainText, "string", GrepSearchOption.Multiline, -1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchContainsValidPattern(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase3", destFolder + "\\TestCase3", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "dnGREP", GrepSearchOption.CaseSensitive, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal("dnGREP", results[0].Pattern);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchWithStopAfterFirstMatch(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase3", destFolder + "\\TestCase3", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "public", GrepSearchOption.CaseSensitive, -1);
            Assert.True(results.Count > 1);
            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "public", GrepSearchOption.StopAfterFirstMatch, -1);
            Assert.Equal(1, results.Count);
            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "", GrepSearchOption.CaseSensitive, -1);
            Assert.True(results.Count > 1);
            results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "", GrepSearchOption.StopAfterFirstMatch, -1);
            Assert.Equal(1, results.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestResultSequence(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;
            Utils.CopyFiles(sourceFolder + "\\TestCase3", destFolder + "\\TestCase3", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "test-file-plain-big.txt"), SearchType.PlainText, "string", GrepSearchOption.CaseSensitive, -1);
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchXPathReturnsCorrectMatchCount(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase4", destFolder + "\\TestCase4", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase4", "app.config"), SearchType.XPath, "//setting", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].Matches.Count, 28);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchXPathReturnsCorrectResultsCount(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase4", destFolder + "\\TestCase4", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase4", "app.config"), SearchType.XPath, "//setting", GrepSearchOption.CaseSensitive | GrepSearchOption.Multiline, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 84);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchWholeWord_Issue_114_Regex(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase10", destFolder + "\\TestCase10", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase10", "issue-114.txt"), SearchType.Regex, "protected", GrepSearchOption.WholeWord, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 1);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSearchWholeWord_Issue_114_Plain(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase10", destFolder + "\\TestCase10", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase10", "issue-114.txt"), SearchType.PlainText, "protected", GrepSearchOption.WholeWord, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 1);
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

            Utils.CopyFiles(sourceFolder + "\\TestCase8", destFolder + "\\TestCase8", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase8", "test.txt"), SearchType.Regex, "here", GrepSearchOption.None, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 1);

            string testFile = Path.Combine(destFolder, @"TestCase8\test.txt");
            Dictionary<string, string> files = new Dictionary<string, string>
            {
                { testFile, Guid.NewGuid().ToString() + ".txt" }
            };

            core.Replace(files, SearchType.Regex, "here", "\\\\n", GrepSearchOption.None, -1);
            Assert.Equal(File.ReadAllText(testFile, Encoding.ASCII).Trim().Split('\n').Length, 2);
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

            Utils.CopyFiles(sourceFolder + "\\TestCase9", destFolder + "\\TestCase9", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase9", "test.txt"), type, "here", GrepSearchOption.None, -1);
            Assert.Equal(results.Count, 1);
            Assert.Equal(results[0].SearchResults.Count, 6);

            string testFile = Path.Combine(destFolder, @"TestCase9\test.txt");
            Dictionary<string, string> files = new Dictionary<string, string>
            {
                { testFile, Guid.NewGuid().ToString() + ".txt" }
            };

            core.Replace(files, type, "here", "$(guid)", GrepSearchOption.None, -1);
            string fileContent = File.ReadAllText(destFolder + "\\TestCase9\\test.txt", Encoding.ASCII).Trim();
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

        // commented out so we can see all tests pass with green, not orange
        //[Fact(Skip = "Functionality being tested not implemented yet.")]
        //public void TestGuidxReplaceWithPatternRegex()
        //{
        //    Utils.CopyFiles(sourceFolder + "\\TestCase9", destinationFolder + "\\TestCase9", null, null);
        //    GrepCore core = new GrepCore();
        //    List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase9", "guidx.txt"), SearchType.Regex, "h\\wre", GrepSearchOption.None, -1);
        //    Assert.Equal(results.Count, 1);
        //    Assert.Equal(results[0].SearchResults.Count, 6);
        //    core.Replace(Directory.GetFiles(destinationFolder + "\\TestCase9", "guidx.txt"), SearchType.Regex, "h\\wre", "$(guidx)", GrepSearchOption.None, -1);
        //    string fileContent = File.ReadAllText(destinationFolder + "\\TestCase9\\guidx.txt", Encoding.ASCII).Trim();
        //    Assert.Equal(6, guidPattern.Matches(fileContent).Count);
        //    Dictionary<string, int> uniqueGuids = new Dictionary<string, int>();
        //    foreach (Match match in guidPattern.Matches(fileContent))
        //    {
        //        if (!uniqueGuids.ContainsKey(match.Value))
        //            uniqueGuids[match.Value] = 1;
        //        else
        //            uniqueGuids[match.Value]++;
        //    }
        //    Assert.Equal(2, uniqueGuids.Keys.Count);
        //}

        [Theory]
        [InlineData(SearchType.PlainText, GrepSearchOption.None, "children", "general")]
        [InlineData(SearchType.PlainText, GrepSearchOption.Multiline, "children", "general")]
        [InlineData(SearchType.Regex, GrepSearchOption.None, "child.*\"\\>", "general\">")]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, "child.*\"\\>", "general\">")]
        [InlineData(SearchType.Regex, GrepSearchOption.None, "child.*\"\\>$", "general\">")]
        [InlineData(SearchType.Regex, GrepSearchOption.Multiline, "child.*\"\\>$", "general\">")]
        public void TestReplaceOnFileWithout_UTF8_BOM(SearchType type, GrepSearchOption option, string searchFor, string replaceWith)
        {
            // Test for Issue #227
            Utils.CopyFiles(sourceFolder + "\\TestCase15", destinationFolder + "\\TestCase15", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase15", "books.xml"),
                type, searchFor, option, -1);
            Assert.Equal(1, results.Count);
            string testFile = Path.Combine(destinationFolder, @"TestCase15\books.xml");
            Dictionary<string, string> files = new Dictionary<string, string>
            {
                { testFile, Guid.NewGuid().ToString() + ".xml" }
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
            var fileContent = File.ReadAllLines(destinationFolder + "\\TestCase15\\books.xml", Encoding.UTF8);

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
        public void TestReplaceOnFileWith_UTF8_BOM(SearchType type, GrepSearchOption option, string searchFor, string replaceWith)
        {
            // Test for Issue #227 dnGrep inserting extra BOM, and scrambling file
            Utils.CopyFiles(sourceFolder + "\\TestCase15", destinationFolder + "\\TestCase15", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase15", "books_bom.xml"),
                type, searchFor, option, -1);
            Assert.Equal(1, results.Count);
            string testFile = Path.Combine(destinationFolder, @"TestCase15\books_bom.xml");
            Dictionary<string, string> files = new Dictionary<string, string>
            {
                { testFile, Guid.NewGuid().ToString() + ".xml" }
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
            var fileContent = File.ReadAllLines(destinationFolder + "\\TestCase15\\books_bom.xml", Encoding.UTF8);

            Assert.Equal(38, fileContent.Length);
            string line1 = fileContent[0].Replace("\ufeff", ""); // remove BOM
            Assert.Equal("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", line1);
            Assert.Equal("  <book category=\"general\">", fileContent[8]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestReplaceAndUndoWorks(bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase3", "test-file-code.cs"), SearchType.PlainText, "body", GrepSearchOption.None, -1);
            Assert.Equal(1, results.Count);
            Assert.Equal(2, results[0].SearchResults.Where(r => r.IsContext).Count());

            string testFile = Path.Combine(destFolder, @"TestCase3\test-file-code.cs");
            Dictionary<string, string> files = new Dictionary<string, string>
            {
                { testFile, Guid.NewGuid().ToString() + ".cs" }
            };

            core.Replace(files, SearchType.PlainText, "body", "text", GrepSearchOption.None, -1);
            string content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.False(content.Contains("body"));
            Assert.True(content.Contains("text"));

            core.Undo(files);
            content = File.ReadAllText(testFile, Encoding.ASCII);
            Assert.False(content.Contains("text"));
            Assert.True(content.Contains("body"));
        }

        [Theory]
        [InlineData(SearchType.Regex, true)]
        [InlineData(SearchType.Regex, false)]
        [InlineData(SearchType.PlainText, true)]
        [InlineData(SearchType.PlainText, false)]
        public void SearchWIthMultipleLines(SearchType type, bool useLongPath)
        {
            string destFolder = useLongPath ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;

            Utils.CopyFiles(sourceFolder + "\\TestCase12", destFolder + "\\TestCase12", null, null);
            GrepCore core = new GrepCore();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destFolder + "\\TestCase12", "issue-165.txt"), type, "asdf\r\nqwer", GrepSearchOption.Multiline, -1);
            Assert.Equal(1, results.Count);
            Assert.Equal(1, results[0].Matches.Count);
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
            Utils.CopyFiles(sourceFolder + "\\TestCase14", destinationFolder + "\\TestCase14", null, null);
            GrepCore core = new GrepCore();
            core.SearchParams = new GrepEngineInitParams(false, 0, 0, 0.5, verbose, false);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<GrepSearchResult> results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase14", "*.txt"), type, "1234", option, -1);
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
            Assert.Equal(1, results[1].Matches.Count);
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
            Assert.Equal(1, results[1].Matches.Count);
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
    }
}
