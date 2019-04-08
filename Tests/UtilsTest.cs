using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;
using Xunit;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace Tests
{
    public class UtilsTest : TestBase, IDisposable
    {
        static string sourceFolder;
        static string destinationFolder;

        public UtilsTest()
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

        [Fact]
        public void GetIntArray()
        {
            int[] r = Utils.GetIntArray(3, 3);
            Assert.Equal(r, new int[] { 3, 4, 5 });

            r = Utils.GetIntArray(0, 3);
            Assert.Equal(r, new int[] { 0, 1, 2 });

            r = Utils.GetIntArray(0, 0);
            Assert.Equal(r, new int[] { });
        }

        [Fact]
        public void TestGetContextLines()
        {
            string test = "Hi\r\nmy\r\nWorld\r\nMy name is Denis\r\nfor\r\nloop";

            List<GrepMatch> bodyMatches = new List<GrepMatch>();
            List<GrepLine> lines = new List<GrepLine>();
            using (StringReader reader = new StringReader(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 9, 2));
                lines = Utils.GetLinesEx(reader, bodyMatches, 2, 2);
            }
            Assert.Equal(5, lines.Count);
            Assert.Equal(1, lines[0].LineNumber);
            Assert.Equal("Hi", lines[0].LineText);
            Assert.True(lines[0].IsContext);
            Assert.True(lines[1].IsContext);
            Assert.False(lines[2].IsContext);
            Assert.Equal(4, lines[3].LineNumber);
            Assert.Equal("My name is Denis", lines[3].LineText);
            Assert.True(lines[3].IsContext);
            Assert.Equal(5, lines[4].LineNumber);
            Assert.Equal("for", lines[4].LineText);
            Assert.True(lines[4].IsContext);


            bodyMatches = new List<GrepMatch>();
            using (StringReader reader = new StringReader(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 9, 2));
                lines = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }
            Assert.Single(lines);

            bodyMatches = new List<GrepMatch>();
            using (StringReader reader = new StringReader(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 4, 1));
                lines = Utils.GetLinesEx(reader, bodyMatches, 10, 0);
            }
            Assert.Equal(2, lines.Count);
            Assert.Equal(1, lines[0].LineNumber);
            Assert.Equal("Hi", lines[0].LineText);
            Assert.True(lines[0].IsContext);
            Assert.Equal("my", lines[1].LineText);
            Assert.False(lines[1].IsContext);

            bodyMatches = new List<GrepMatch>();
            using (StringReader reader = new StringReader(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 34, 1));
                lines = Utils.GetLinesEx(reader, bodyMatches, 1, 10);
            }

            Assert.Equal(3, lines.Count);
            Assert.Equal(4, lines[0].LineNumber);
            Assert.Equal(5, lines[1].LineNumber);
            Assert.Equal("for", lines[1].LineText);
            Assert.False(lines[1].IsContext);
            Assert.Equal(6, lines[2].LineNumber);
            Assert.Equal("loop", lines[2].LineText);
            Assert.True(lines[2].IsContext);
        }

        [Fact]
        public void TestDefaultSettings()
        {
            var type = GrepSettings.Instance.Get<SearchType>(GrepSettings.Key.TypeOfSearch);
            Assert.Equal(SearchType.Regex, type);
        }

        [Theory]
        [InlineData("hello\rworld", "hello\r\nworld")]
        [InlineData("hello\nworld", "hello\r\nworld")]
        [InlineData("hello\rworld\r", "hello\r\nworld\r")]
        public void TestCleanLineBreaks(string input, string expected)
        {
            string result = Utils.CleanLineBreaks(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("\\Files\\TestCase1\\test-file-code.cs", "\\Files\\TestCase1")]
        [InlineData("\\Files\\TestCase1", "\\Files\\TestCase1")]
        [InlineData("\\Files\\TestCas\\", null)]
        [InlineData("\\Files\\TestCase1\\test-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt", "\\Files\\TestCase1")]
        public void TestGetBaseFolder(string relativePath, string expected)
        {
            StringBuilder sb = new StringBuilder();
            string pathToDll = GetDllPath();
            string[] rPaths = relativePath.Split(';');
            foreach (string rPath in rPaths)
                sb.Append(pathToDll + rPath + ";");

            if (expected == null)
                Assert.Null(Utils.GetBaseFolder(sb.ToString()));
            else
                Assert.Equal(pathToDll + expected, Utils.GetBaseFolder(sb.ToString()));
        }

        [Theory]
        [InlineData("\\Files\\TestCase7\\Test;Folder", "\\Files\\TestCase7\\Test;Folder")]
        [InlineData("\\Files\\TestCase7\\Test,Folder", "\\Files\\TestCase7\\Test,Folder")]
        public void TestGetBaseFolderWithColons(string relativePath, string expected)
        {
            string pathToDll = GetDllPath();
            relativePath = pathToDll + relativePath;

            if (expected == null)
                Assert.Null(Utils.GetBaseFolder(relativePath));
            else
                Assert.Equal(pathToDll + expected, Utils.GetBaseFolder(relativePath));
        }

        [Theory]
        [InlineData("\\Files\\TestCase1\\test-file-code.cs", true)]
        [InlineData("\\Files\\TestCase1\\test-file-code2.cs", false)]
        [InlineData("\\Files\\TestCase1\\", true)]
        [InlineData("\\Files\\TestCase1", true)]
        [InlineData("\\Files\\TestCas\\", false)]
        [InlineData("\\Files\\TestCase1\\test-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt", true)]
        [InlineData("\\Files\\TestCase1\\test-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt;\\Files\\TestCase1", true)]
        [InlineData("\\Files\\TestCase1\\test11-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt;\\Files\\TestCase1", false)]
        [InlineData("\\Files\\TestCase1\\test-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt;\\Files1\\TestCase1", false)]
        public void TestIsPathValied(string relativePath, bool expected)
        {
            StringBuilder sb = new StringBuilder();
            string pathToDll = GetDllPath();
            string[] rPaths = relativePath.Split(';');
            foreach (string rPath in rPaths)
                sb.Append(pathToDll + rPath + ";");

            Assert.Equal(expected, Utils.IsPathValid(sb.ToString()));

            foreach (string rPath in rPaths)
                sb.Append(pathToDll + rPath + ",");

            Assert.Equal(expected, Utils.IsPathValid(sb.ToString()));
        }

        [Theory]
        [InlineData("\\Files\\TestCase7\\Test;Folder\\issue-10.txt", true)]
        [InlineData("\\Files\\TestCase7\\Test,Folder\\issue-10.txt", true)]
        [InlineData("\\Files\\TestCase1\\test-file-code.cs", true)]
        public void TestIsPathValiedWithColon(string relativePath, bool expected)
        {
            string pathToDll = GetDllPath();
            relativePath = pathToDll + relativePath;

            Assert.Equal(expected, Utils.IsPathValid(relativePath));
        }

        [Fact]
        public void TestIsPathValidWithoutCollon()
        {
            StringBuilder sb = new StringBuilder();
            string pathToDll = GetDllPath();
            sb.Append(pathToDll + "\\Files\\TestCase1");

            Assert.True(Utils.IsPathValid(sb.ToString()));
        }

        [Fact]
        public void TestCleanResults()
        {
            List<GrepLine> results = new List<GrepLine>();
            results.Add(new GrepLine(1, "test", true, null));
            results.Add(new GrepLine(3, "test3", false, null));
            results.Add(new GrepLine(2, "test2", false, null));
            results.Add(new GrepLine(1, "test1", false, null));
            Utils.CleanResults(ref results);

            Assert.Equal(3, results.Count);
            Assert.False(results[0].IsContext);
            Assert.Equal(1, results[0].LineNumber);
            Assert.False(results[2].IsContext);
            Assert.Equal(3, results[2].LineNumber);

            results = null;
            Utils.CleanResults(ref results);
            results = new List<GrepLine>();
            Utils.CleanResults(ref results);
        }


        [Theory]
        [InlineData("0.9.1", "0.9.2", true)]
        [InlineData("0.9.1", "0.9.2.5556", true)]
        [InlineData("0.9.1.5554", "0.9.1.5556", true)]
        [InlineData("0.9.0.5557", "0.9.1.5550", true)]
        [InlineData("0.9.1", "0.9.0.5556", false)]
        [InlineData("0.9.5.5000", "0.9.0.5556", false)]
        [InlineData(null, "0.9.0.5556", false)]
        [InlineData("0.9.5.5000", "", false)]
        [InlineData("0.9.5.5000", null, false)]
        [InlineData("xyz", "abc", false)]
        public void CompareVersions(string v1, string v2, bool expected)
        {
            Assert.Equal(expected, PublishedVersionExtractor.IsUpdateNeeded(v1, v2));
        }

        [Fact]
        public void GetLinesEx_Returns_Correct_Line()
        {
            string text = "Hello world" + Environment.NewLine + "My tests are good" + Environment.NewLine + "How about yours?";
            List<int> lineNumbers = new List<int>();
            List<GrepMatch> bodyMatches = new List<GrepMatch>();
            List<GrepLine> results = new List<GrepLine>();
            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 3, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Equal(1, results.Count(l => l.IsContext == false));
            Assert.Equal("Hello world", results[0].LineText);
            Assert.Single(results[0].Matches);
            Assert.Equal(1, results[0].LineNumber);

            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 14, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }


            Assert.Single(results);
            Assert.Equal("My tests are good", results[0].LineText);
            Assert.Single(results[0].Matches);
            Assert.Equal(2, results[0].LineNumber);

            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 3, 11));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Equal(2, results.Count);
            Assert.Equal("Hello world", results[0].LineText);
            Assert.Equal("My tests are good", results[1].LineText);
            Assert.Single(results[0].Matches);
            Assert.Single(results[1].Matches);
            Assert.Equal(1, results[0].LineNumber);
            Assert.Equal(2, results[1].LineNumber);

            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 3, 30));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Equal(3, results.Count);
            Assert.Equal("Hello world", results[0].LineText);
            Assert.Equal("My tests are good", results[1].LineText);
            Assert.Equal("How about yours?", results[2].LineText);
            Assert.Single(results[0].Matches);
            Assert.Single(results[1].Matches);
            Assert.Single(results[2].Matches);
            Assert.Equal(1, results[0].LineNumber);
            Assert.Equal(2, results[1].LineNumber);
            Assert.Equal(3, results[2].LineNumber);

            using (StringReader reader = new StringReader("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 2, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Single(results);
            Assert.Equal("test", results[0].LineText);
            Assert.Single(results[0].Matches);
            Assert.Equal(1, results[0].LineNumber);

            using (StringReader reader = new StringReader("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 0, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Single(results);
            Assert.Equal("test", results[0].LineText);
            Assert.Single(results[0].Matches);
            Assert.Equal(1, results[0].LineNumber);

            using (StringReader reader = new StringReader("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 10, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Empty(results);

            using (StringReader reader = new StringReader("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 2, 10));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Empty(results);

            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch(0, 3, 2));
                bodyMatches.Add(new GrepMatch(0, 6, 2));
                bodyMatches.Add(new GrepMatch(0, 14, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Equal(2, results.Count);
            Assert.Equal("Hello world", results[0].LineText);
            Assert.Equal("My tests are good", results[1].LineText);
            Assert.Equal(2, results[0].Matches.Count);
            Assert.Single(results[1].Matches);
            Assert.Equal(1, results[0].LineNumber);
            Assert.Equal(2, results[1].LineNumber);
        }

        [Fact]
        public void TestMergeResultsHappyPath()
        {
            List<GrepLine> results = new List<GrepLine>();
            List<GrepLine> context = new List<GrepLine>();
            results.Add(new GrepLine(3, "text3", false, null));
            results.Add(new GrepLine(5, "text5", false, null));
            results.Add(new GrepLine(6, "text6", false, new List<GrepMatch>()));
            context.Add(new GrepLine(1, "text1", true, null));
            context.Add(new GrepLine(2, "text2", true, null));
            context.Add(new GrepLine(3, "text3", true, null));
            Utils.MergeResults(ref results, context);
            Assert.Equal(5, results.Count);
            Assert.Equal("text1", results[0].LineText);
            Assert.Equal("text2", results[1].LineText);
            Assert.True(results[1].IsContext);
            Assert.Equal("text3", results[2].LineText);
            Assert.False(results[2].IsContext);
        }

        [Fact]
        public void TestMergeResultsBorderCases()
        {
            List<GrepLine> results = new List<GrepLine>();
            Utils.MergeResults(ref results, null);
            Assert.Empty(results);

            List<GrepLine> context = new List<GrepLine>();
            context.Add(new GrepLine(1, "text1", true, null));
            context.Add(new GrepLine(2, "text2", true, null));
            context.Add(new GrepLine(3, "text3", true, null));

            Utils.MergeResults(ref results, context);
            Assert.Equal(3, results.Count);

            results.Add(new GrepLine(3, "text3", false, null));
            results.Add(new GrepLine(5, "text5", false, null));
            results.Add(new GrepLine(6, "text6", false, new List<GrepMatch>()));

            results = new List<GrepLine>();
            results.Add(new GrepLine(3, "text3", false, null));
            results.Add(new GrepLine(5, "text5", false, null));
            results.Add(new GrepLine(6, "text6", false, new List<GrepMatch>()));
            Utils.MergeResults(ref results, null);
            Assert.Equal(3, results.Count);

            results = new List<GrepLine>();
            results.Add(new GrepLine(3, "text3", false, null));
            results.Add(new GrepLine(5, "text5", false, null));
            context.Add(new GrepLine(20, "text20", true, null));
            context.Add(new GrepLine(30, "text30", true, null));

            Utils.MergeResults(ref results, context);
            Assert.Equal(6, results.Count);
            Assert.Equal("text30", results[5].LineText);
        }

        [Fact]
        public void TestTextReaderReadLine()
        {
            string text = "Hello world" + Environment.NewLine + "My tests are good\nHow about \ryours?\n";
            int lineNumber = 0;
            using (StringReader baseReader = new StringReader(text))
            {
                using (EolReader reader = new EolReader(baseReader))
                {
                    while (!reader.EndOfStream)
                    {
                        lineNumber++;
                        var line = reader.ReadLine();
                        if (lineNumber == 1)
                            Assert.Equal("Hello world" + Environment.NewLine, line);
                        if (lineNumber == 2)
                            Assert.Equal("My tests are good\n", line);
                        if (lineNumber == 3)
                            Assert.Equal("How about \r", line);
                        if (lineNumber == 4)
                            Assert.Equal("yours?\n", line);
                    }
                }
            }
            Assert.Equal(4, lineNumber);
            text = "Hello world";
            lineNumber = 0;
            using (StringReader baseReader = new StringReader(text))
            {
                using (EolReader reader = new EolReader(baseReader))
                {
                    while (!reader.EndOfStream)
                    {
                        lineNumber++;
                        var line = reader.ReadLine();
                        Assert.Equal("Hello world", line);
                    }
                }
            }
            Assert.Equal(1, lineNumber);
        }

        [Theory]
        [InlineData(null, null, 2)]
        [InlineData("", "", 2)]
        [InlineData(null, ".*\\.cs", 1)]
        [InlineData(".*\\.txt", null, 1)]
        public void TestCopyFiles(string includePattern, string excludePattern, int expected)
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase1", destinationFolder, includePattern, excludePattern);
            Assert.Equal(expected, Directory.GetFiles(destinationFolder).Length);
        }

        [Theory]
        [InlineData(null, null, 2)]
        [InlineData("", "", 2)]
        [InlineData(null, ".*\\.cs", 1)]
        [InlineData(".*\\.txt", null, 1)]
        public void TestCopyFilesLongPath(string includePattern, string excludePattern, int expected)
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            Utils.CopyFiles(sourceFolder + "\\TestCase1", longDestinationFolder, includePattern, excludePattern);
            Assert.Equal(expected, Directory.GetFiles(longDestinationFolder).Length);
        }

        [Theory]
        [InlineData(null, null, 2)]
        public void TestCopyFilesToNonExistingFolder(string includePattern, string excludePattern, int expected)
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase1", destinationFolder + "\\123", includePattern, excludePattern);
            Assert.Equal(expected, Directory.GetFiles(destinationFolder + "\\123").Length);
        }

        [Theory]
        [InlineData(null, null, 2)]
        public void TestCopyFilesToNonExistingFolderLongPath(string includePattern, string excludePattern, int expected)
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            Utils.CopyFiles(sourceFolder + "\\TestCase1", longDestinationFolder + "\\123", includePattern, excludePattern);
            Assert.Equal(expected, Directory.GetFiles(longDestinationFolder + "\\123").Length);
        }

        [Fact]
        public void TestCopyFilesWithSubFolders()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", ".*", null);
            Assert.Equal(4, Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length);
            Assert.True(Directory.Exists(destinationFolder + "\\TestCase3\\SubFolder"));
            Utils.DeleteFolder(destinationFolder + "\\TestCase3");
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\SubFolder\\test-file-plain-hidden.txt", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\test-file-code.cs", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", OverwriteFile.Yes);
            Assert.Equal(2, Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length);
            Assert.True(Directory.Exists(destinationFolder + "\\TestCase3\\SubFolder"));
        }

        [Fact]
        public void TestCopyFilesWithSubFoldersLongPath()
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            Utils.CopyFiles(sourceFolder + "\\TestCase3", longDestinationFolder + "\\TestCase3", ".*", null);
            Assert.Equal(4, Directory.GetFiles(longDestinationFolder, "*.*", SearchOption.AllDirectories).Length);
            Assert.True(Directory.Exists(longDestinationFolder + "\\TestCase3\\SubFolder"));
            Utils.DeleteFolder(longDestinationFolder + "\\TestCase3");
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\SubFolder\\test-file-plain-hidden.txt", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\test-file-code.cs", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder + "\\TestCase3", longDestinationFolder + "\\TestCase3", OverwriteFile.Yes);
            Assert.Equal(2, Directory.GetFiles(longDestinationFolder, "*.*", SearchOption.AllDirectories).Length);
            Assert.True(Directory.Exists(longDestinationFolder + "\\TestCase3\\SubFolder"));
        }

        [Fact]
        public void TestCopyFilesWithSubFoldersToSingleDestination()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", ".*", null);
            Assert.Equal(4, Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length);
            Assert.True(Directory.Exists(destinationFolder + "\\TestCase3\\SubFolder"));
            Utils.DeleteFolder(destinationFolder + "\\TestCase3");
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\SubFolder\\test-file-plain-hidden.txt", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\test-file-code.cs", "", null, Encoding.Default));
            Utils.CopyFiles(source, destinationFolder + "\\TestCase3", OverwriteFile.Yes);
            Assert.Equal(2, Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*", SearchOption.TopDirectoryOnly).Length);
            Assert.False(Directory.Exists(destinationFolder + "\\TestCase3\\SubFolder"));
        }

        [Fact]
        public void TestCopyFilesWithSubFoldersToSingleDestinationLongPath()
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            Utils.CopyFiles(sourceFolder + "\\TestCase3", longDestinationFolder + "\\TestCase3", ".*", null);
            Assert.Equal(4, Directory.GetFiles(longDestinationFolder, "*.*", SearchOption.AllDirectories).Length);
            Assert.True(Directory.Exists(longDestinationFolder + "\\TestCase3\\SubFolder"));
            Utils.DeleteFolder(longDestinationFolder + "\\TestCase3");
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\SubFolder\\test-file-plain-hidden.txt", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\test-file-code.cs", "", null, Encoding.Default));
            Utils.CopyFiles(source, longDestinationFolder + "\\TestCase3", OverwriteFile.Yes);
            Assert.Equal(2, Directory.GetFiles(longDestinationFolder + "\\TestCase3", "*.*", SearchOption.TopDirectoryOnly).Length);
            Assert.False(Directory.Exists(longDestinationFolder + "\\TestCase3\\SubFolder"));
        }

        [Fact]
        public void TestCopyResults()
        {
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, destinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(destinationFolder + "\\TestCase1").Length);
            source.Add(new GrepSearchResult(sourceFolder + "\\issue-10.txt", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, destinationFolder, OverwriteFile.Yes);
            Assert.Equal(3, Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length);
            try
            {
                Utils.CopyFiles(source, sourceFolder, destinationFolder, OverwriteFile.No);
                Assert.True(false, "Not supposed to get here");
            }
            catch
            {
                //OK
            }
            Assert.Equal(3, Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length);
            Utils.CopyFiles(source, sourceFolder, destinationFolder + "\\123", OverwriteFile.No);
            Assert.Equal(6, Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length);
        }

        [Fact]
        public void TestCopyResultsLongPath()
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, longDestinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(longDestinationFolder + "\\TestCase1").Length);
            source.Add(new GrepSearchResult(sourceFolder + "\\issue-10.txt", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, longDestinationFolder, OverwriteFile.Yes);
            Assert.Equal(3, Directory.GetFiles(longDestinationFolder, "*.*", SearchOption.AllDirectories).Length);
            try
            {
                Utils.CopyFiles(source, sourceFolder, longDestinationFolder, OverwriteFile.No);
                Assert.True(false, "Not supposed to get here");
            }
            catch
            {
                //OK
            }
            Assert.Equal(3, Directory.GetFiles(longDestinationFolder, "*.*", SearchOption.AllDirectories).Length);
            Utils.CopyFiles(source, sourceFolder, longDestinationFolder + "\\123", OverwriteFile.No);
            Assert.Equal(6, Directory.GetFiles(longDestinationFolder, "*.*", SearchOption.AllDirectories).Length);
        }

        [Fact]
        public void TestCanCopy()
        {
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\TestCase1\\test-file-plain2.txt", "", null, Encoding.Default));
            Assert.False(Utils.CanCopyFiles(source, sourceFolder + "\\TestCase1"));
            Assert.False(Utils.CanCopyFiles(source, sourceFolder + "\\TestCase1\\"));
            Assert.True(Utils.CanCopyFiles(source, sourceFolder));
            Assert.False(Utils.CanCopyFiles(source, sourceFolder + "\\TestCase1\\TestCase1"));
            Assert.False(Utils.CanCopyFiles(null, null));
            Assert.False(Utils.CanCopyFiles(source, null));
            Assert.False(Utils.CanCopyFiles(null, sourceFolder));
        }

        [Fact]
        public void WriteToCsvTest()
        {
            Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", null, null);
            File.WriteAllText(destinationFolder + "\\test.csv", "hello");
            var core = new GrepCore();
            var results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.None, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);
            Utils.SaveResultsAsCSV(results, destinationFolder + "\\test.csv");
            string[] stringLines = File.ReadAllLines(destinationFolder + "\\test.csv");
            Assert.Equal(177, stringLines.Length);
            Assert.Equal("File Name", stringLines[0].Split(',')[0].Trim());
            Assert.Equal("1", stringLines[1].Split(',')[1].Trim());
            Assert.Equal("\"\tstring returnedLine = Utils.GetLine(body", stringLines[2].Split(',')[2].Trim());
        }

        [Fact]
        public void WriteToCsvTestLongPath()
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            Utils.CopyFiles(sourceFolder + "\\TestCase3", longDestinationFolder + "\\TestCase3", null, null);
            File.WriteAllText(longDestinationFolder + "\\test.csv", "hello");
            var core = new GrepCore();
            var results = core.Search(Directory.GetFiles(longDestinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.None, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);
            Utils.SaveResultsAsCSV(results, longDestinationFolder + "\\test.csv");
            string[] stringLines = File.ReadAllLines(longDestinationFolder + "\\test.csv");
            Assert.Equal(177, stringLines.Length);
            Assert.Equal("File Name", stringLines[0].Split(',')[0].Trim());
            Assert.Equal("1", stringLines[1].Split(',')[1].Trim());
            Assert.Equal("\"\tstring returnedLine = Utils.GetLine(body", stringLines[2].Split(',')[2].Trim());
        }

        [Fact]
        public void DeleteFilesTest()
        {
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, destinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(destinationFolder + "\\TestCase1\\").Length);
            List<GrepSearchResult> source2 = new List<GrepSearchResult>();
            source2.Add(new GrepSearchResult(destinationFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            Utils.DeleteFiles(source2);
            Assert.Single(Directory.GetFiles(destinationFolder + "\\TestCase1\\"));
            source2.Add(new GrepSearchResult(destinationFolder + "\\test-file-code.cs", "", null, Encoding.Default));
            Utils.DeleteFiles(source2);
            Assert.Single(Directory.GetFiles(destinationFolder + "\\TestCase1\\"));
            source2.Add(new GrepSearchResult(destinationFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));
            Utils.DeleteFiles(source2);
            Assert.Empty(Directory.GetFiles(destinationFolder + "\\TestCase1\\"));
        }

        [Fact]
        public void DeleteFilesTestLongPath()
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, longDestinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(longDestinationFolder + "\\TestCase1\\").Length);
            List<GrepSearchResult> source2 = new List<GrepSearchResult>();
            source2.Add(new GrepSearchResult(longDestinationFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            Utils.DeleteFiles(source2);
            Assert.Single(Directory.GetFiles(longDestinationFolder + "\\TestCase1\\"));
            source2.Add(new GrepSearchResult(longDestinationFolder + "\\test-file-code.cs", "", null, Encoding.Default));
            Utils.DeleteFiles(source2);
            Assert.Single(Directory.GetFiles(longDestinationFolder + "\\TestCase1\\"));
            source2.Add(new GrepSearchResult(longDestinationFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));
            Utils.DeleteFiles(source2);
            Assert.Empty(Directory.GetFiles(longDestinationFolder + "\\TestCase1\\"));
        }

        [Fact]
        public void TestCopyFileInNonExistingFolder()
        {
            Utils.CopyFile(sourceFolder + "\\TestCase1\\test-file-code.cs", destinationFolder + "\\Test\\test-file-code2.cs", false);
            Assert.True(File.Exists(destinationFolder + "\\Test\\test-file-code2.cs"));
        }

        [Fact]
        public void TestCopyFileInNonExistingFolderLongPath()
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            Utils.CopyFile(sourceFolder + "\\TestCase1\\test-file-code.cs", longDestinationFolder + "\\Test\\test-file-code2.cs", false);
            Assert.True(File.Exists(longDestinationFolder + "\\Test\\test-file-code2.cs"));
        }

        [Fact]
        public void DeleteFolderTest()
        {
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, destinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(destinationFolder + "\\TestCase1").Length);
            File.SetAttributes(destinationFolder + "\\TestCase1\\test-file-code.cs", FileAttributes.ReadOnly);
            Utils.DeleteFolder(destinationFolder);
            Assert.False(Directory.Exists(destinationFolder));
        }

        [Fact]
        public void DeleteFolderTestLongPath()
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, longDestinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(longDestinationFolder + "\\TestCase1").Length);
            File.SetAttributes(longDestinationFolder + "\\TestCase1\\test-file-code.cs", FileAttributes.ReadOnly);
            Utils.DeleteFolder(longDestinationFolder);
            Assert.False(Directory.Exists(longDestinationFolder));
        }

        [Theory]
        [InlineData("*.*", false, true, true, 0, 0, 5)]
        [InlineData("*.*", false, true, false, 0, 0, 4)]
        [InlineData("*.*", false, true, false, 0, 40, 3)]
        [InlineData("*.*", false, true, false, 1, 40, 1)]
        [InlineData(".*\\.txt", true, true, true, 0, 0, 3)]
        [InlineData(".*\\.txt", true, false, true, 0, 0, 2)]
        [InlineData(null, true, false, true, 0, 0, 0)]
        [InlineData("", true, true, true, 0, 0, 0)]
        public void GetFileListTest(string namePattern, bool isRegex, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo, int expected)
        {
            string testCase2 = Path.Combine(sourceFolder, @"TestCase2");
            string destFolder = Path.Combine(destinationFolder, @"TestCase2");
            string hiddenFolder = Path.Combine(destinationFolder, @"TestCase2", @"HiddenFolder");
            DirectoryInfo di = new DirectoryInfo(destFolder);
            if (!di.Exists)
            {
                di.Create();
                Directory.Copy(testCase2, destFolder);
            }
            di = new DirectoryInfo(hiddenFolder);
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(Path.Combine(di.FullName, "test-file-plain-hidden.txt"), "Hello world");
            }
            di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

            string[] files = Utils.GetFileList(destFolder, namePattern, null, isRegex, false, includeSubfolders, includeHidden,
                true, false, sizeFrom, sizeTo, FileDateFilter.None, null, null);
            Assert.Equal(expected, files.Length);
        }

        [Theory]
        [InlineData("*.*", false, true, true, 0, 0, 5)]
        [InlineData("*.*", false, true, false, 0, 0, 4)]
        [InlineData("*.*", false, true, false, 0, 40, 3)]
        [InlineData("*.*", false, true, false, 1, 40, 1)]
        [InlineData(".*\\.txt", true, true, true, 0, 0, 3)]
        [InlineData(".*\\.txt", true, false, true, 0, 0, 2)]
        [InlineData(null, true, false, true, 0, 0, 0)]
        [InlineData("", true, true, true, 0, 0, 0)]
        public void GetFileListTestLongPath(string namePattern, bool isRegex, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo, int expected)
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            string testCase2 = Path.Combine(sourceFolder, @"TestCase2");
            string destFolder = Path.Combine(longDestinationFolder, @"TestCase2");
            string hiddenFolder = Path.Combine(longDestinationFolder, @"TestCase2", @"HiddenFolder");
            DirectoryInfo di = new DirectoryInfo(destFolder);
            if (!di.Exists)
            {
                di.Create();
                Directory.Copy(testCase2, destFolder);
            }
            di = new DirectoryInfo(hiddenFolder);
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(Path.Combine(di.FullName, "test-file-plain-hidden.txt"), "Hello world");
            }
            di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

            string[] files = Utils.GetFileList(destFolder, namePattern, null, isRegex, false, includeSubfolders, includeHidden,
                true, false, sizeFrom, sizeTo, FileDateFilter.None, null, null);
            Assert.Equal(expected, files.Length);
        }

        [Theory]
        [InlineData(12, 10, null, 1)]
        [InlineData(12, 11, null, 1)]
        [InlineData(12, 12, null, 1)]
        [InlineData(12, 13, null, 0)]
        [InlineData(12, null, 10, 0)]
        [InlineData(12, null, 11, 0)]
        [InlineData(12, null, 12, 0)]
        [InlineData(12, null, 13, 1)]
        [InlineData(12, 10, 11, 0)]
        [InlineData(12, 10, 12, 0)]
        [InlineData(12, 10, 13, 1)]
        [InlineData(12, 11, 12, 0)]
        [InlineData(12, 11, 13, 1)]
        [InlineData(12, 11, 14, 1)]
        [InlineData(12, 12, 13, 1)]
        [InlineData(12, 12, 14, 1)]
        [InlineData(12, 13, 14, 0)]
        public void GetFileListDateFilterTest(int fileDay, int? startDay, int? endDay, int expected)
        {
            DirectoryInfo di = new DirectoryInfo(sourceFolder + "\\TestCaseDates");
            string testFile = Path.Combine(di.FullName, "test-file.txt");
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(testFile, "Hello world");
            }
            di.Attributes = FileAttributes.Directory;
            FileInfo fi = new FileInfo(testFile);

            DateTime fileTime = new DateTime(2017, 01, fileDay, 10, 12, 14, DateTimeKind.Local);
            DateTime? startTime = null, endTime = null;

            if (startDay.HasValue)
                startTime = new DateTime(2017, 01, startDay.Value, 0, 0, 0, DateTimeKind.Local);

            if (endDay.HasValue)
                endTime = new DateTime(2017, 01, endDay.Value, 0, 0, 0, DateTimeKind.Local);

            fi.CreationTime = fileTime;
            Assert.Equal(expected, Utils.GetFileList(di.FullName, "*", string.Empty, false, false, false, false, false, false, 0, 0, FileDateFilter.Created, startTime, endTime).Length);

            fi.LastWriteTime = fileTime;
            Assert.Equal(expected, Utils.GetFileList(di.FullName, "*", string.Empty, false, false, false, false, false, false, 0, 0, FileDateFilter.Modified, startTime, endTime).Length);
        }

        [Theory]
        [InlineData(2, 0, 4, 1)]
        [InlineData(2, 0, 3, 1)]
        [InlineData(2, 0, 2, 1)]
        [InlineData(2, 0, 1, 0)]
        [InlineData(0.5, 0, 2, 1)]
        [InlineData(0.5, 1, 2, 0)]
        [InlineData(1.5, 0, 4, 1)]
        [InlineData(1.5, 1, 4, 1)]
        [InlineData(1.5, 2, 4, 0)]
        public void GetFileListHourFilterTest(double filePast, int fromHoursPast, int toHoursPast, int expected)
        {
            DirectoryInfo di = new DirectoryInfo(sourceFolder + "\\TestCaseDates");
            string testFile = Path.Combine(di.FullName, "test-file.txt");
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(testFile, "Hello world");
            }
            di.Attributes = FileAttributes.Directory;
            FileInfo fi = new FileInfo(testFile);

            DateTime now = DateTime.Now;

            DateTime fileTime = now.AddHours(-1 * filePast);
            DateTime startTime = now.AddHours(-1 * toHoursPast);
            DateTime endTime = now.AddHours(-1 * fromHoursPast);

            fi.CreationTime = fileTime;
            Assert.Equal(expected, Utils.GetFileList(di.FullName, "*", string.Empty, false, false, false, false, false, false, 0, 0, FileDateFilter.Created, startTime, endTime).Length);

            fi.LastWriteTime = fileTime;
            Assert.Equal(expected, Utils.GetFileList(di.FullName, "*", string.Empty, false, false, false, false, false, false, 0, 0, FileDateFilter.Modified, startTime, endTime).Length);
        }

        [Fact]
        public void GetFileListTestWithMultiplePaths()
        {
            string dllPath = GetDllPath();
            string path = sourceFolder + "\\TestCase2;" + sourceFolder + "\\TestCase2\\excel-file.xls";
            Assert.Equal(4, Utils.GetFileList(path, "*.*", "", false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);

            path = sourceFolder + "\\TestCase2;" + sourceFolder + "\\TestCase3\\test-file-code.cs";
            Assert.Equal(5, Utils.GetFileList(path, "*.*", "", false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase2";
            Assert.Equal(5, Utils.GetFileList(path, "*.*", "", false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);

            path = sourceFolder + "\\TestCase2;" + sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase3\\test-file-plain.txt";
            Assert.Equal(6, Utils.GetFileList(path, "*.*", null, false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase3\\test-file-plain.txt";
            Assert.Equal(2, Utils.GetFileList(path, "*.*", null, false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase3\\test-file-plain.txt;";
            Assert.Equal(2, Utils.GetFileList(path, "*.*", null, false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs," + sourceFolder + "\\TestCase3\\test-file-plain.txt,";
            Assert.Equal(2, Utils.GetFileList(path, "*.*", null, false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs," + sourceFolder + "\\TestCase3\\test-file-plain.txt";
            Assert.Equal(2, Utils.GetFileList(path, "*.*", null, false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);
        }

        [Fact]
        public void GetFileListWithExcludes()
        {
            string dllPath = GetDllPath();
            string path = sourceFolder + "\\TestCase2";
            Assert.Equal(3, Utils.GetFileList(path, "*.*", "*.xls", false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);
            Assert.Empty(Utils.GetFileList(path, "excel*.*", "*.xls", false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null));
            Assert.Single(Utils.GetFileList(path, "excel*.*", "*.xs", false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null));
            Assert.Equal(2, Utils.GetFileList(path, "t*st-file-*.*", "*.cs", false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);
            Assert.Equal(2, Utils.GetFileList(path, "t?st-file-*.*", "*.cs", false, false, false, false, true, false, 0, 0, FileDateFilter.None, null, null).Length);
        }

        [Fact]
        public void GetFileListFromNonExistingFolderReturnsEmptyString()
        {
            Assert.Empty(Utils.GetFileList(sourceFolder + "\\NonExisting", "*.*", null, false, false, true, true, true, false, 0, 0, FileDateFilter.None, null, null));
        }

        [Theory]
        [InlineData("", 1, 1)]
        [InlineData("5", 0, 5)]
        [InlineData(" 12", 1, 12)]
        [InlineData("", int.MinValue, int.MinValue)]
        [InlineData(null, int.MinValue, int.MinValue)]
        [InlineData(" 22 ", int.MinValue, 22)]
        public void ParseIntTest(string text, int defaultValue, int expected)
        {
            if (defaultValue != int.MinValue)
                Assert.Equal(expected, Utils.ParseInt(text, defaultValue));
            else
                Assert.Equal(expected, Utils.ParseInt(text));
        }

        [Fact]
        public void GetReadOnlyFilesTest()
        {
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));

            List<GrepSearchResult> destination = new List<GrepSearchResult>();
            destination.Add(new GrepSearchResult(destinationFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            destination.Add(new GrepSearchResult(destinationFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));

            Utils.CopyFiles(source, sourceFolder + "\\TestCase1", destinationFolder + "\\TestCase1", OverwriteFile.Yes);
            File.SetAttributes(destinationFolder + "\\TestCase1\\test-file-code.cs", FileAttributes.ReadOnly);
            Assert.Single(Utils.GetReadOnlyFiles(destination));
            File.SetAttributes(destinationFolder + "\\TestCase1\\test-file-plain.txt", FileAttributes.ReadOnly);
            Assert.Equal(2, Utils.GetReadOnlyFiles(destination).Count);

            Assert.Empty(Utils.GetReadOnlyFiles(null));
            Assert.Empty(Utils.GetReadOnlyFiles(new List<GrepSearchResult>()));
        }

        [Fact]
        public void GetReadOnlyFilesTestLongPath()
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            List<GrepSearchResult> source = new List<GrepSearchResult>();
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));

            List<GrepSearchResult> destination = new List<GrepSearchResult>();
            destination.Add(new GrepSearchResult(longDestinationFolder + "\\TestCase1\\test-file-code.cs", "", null, Encoding.Default));
            destination.Add(new GrepSearchResult(longDestinationFolder + "\\TestCase1\\test-file-plain.txt", "", null, Encoding.Default));

            Utils.CopyFiles(source, sourceFolder + "\\TestCase1", longDestinationFolder + "\\TestCase1", OverwriteFile.Yes);
            File.SetAttributes(longDestinationFolder + "\\TestCase1\\test-file-code.cs", FileAttributes.ReadOnly);
            Assert.Single(Utils.GetReadOnlyFiles(destination));
            File.SetAttributes(longDestinationFolder + "\\TestCase1\\test-file-plain.txt", FileAttributes.ReadOnly);
            Assert.Equal(2, Utils.GetReadOnlyFiles(destination).Count);

            Assert.Empty(Utils.GetReadOnlyFiles(null));
            Assert.Empty(Utils.GetReadOnlyFiles(new List<GrepSearchResult>()));
        }

        [Theory]
        [InlineData("\\TestCase6\\test.rar", true)]
        [InlineData("\\TestCase6\\test_file.txt", false)]
        [InlineData("\\TestCase5\\big-word-document.doc", true)]
        public void TestIsBinaryFile(string file, bool expected)
        {
            Assert.Equal(expected, Utils.IsBinary(sourceFolder + file));
        }

        public static IEnumerable<object[]> TestGetPaths_Source
        {
            get
            {
                yield return new object[] { "{0}\\TestCase5\\big-word-document.doc", 1 };
                yield return new object[] { "{0}\\TestCase7;{0}\\TestCase7", 2 };
                yield return new object[] { "{0}\\TestCase5;{0}\\TestCase7", 2 };
                yield return new object[] { "{0}\\TestCase7\\Test,Folder\\;{0}\\TestCase7", 2 };
                yield return new object[] { "{0}\\TestCase7\\Test;Folder\\;{0}\\TestCase7", 2 };
                yield return new object[] { "{0}\\TestCase7\\Test;Folder\\;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder\\", 3 };
                yield return new object[] { ".git\\*;*.resx;*.aip;bin\\*;packages\\*;", 5 };
                yield return new object[] { ";.git\\*;*.resx;*.aip;bin\\*;packages\\*;", 5 };
                yield return new object[] { ".git\\*;*.resx;;;*.aip;;bin\\*;packages\\*;", 5 };
                yield return new object[] { null, 0 };
                yield return new object[] { "", 0 };
            }
        }

        [Theory]
        [MemberData(nameof(TestGetPaths_Source))]
        public void TestGetPathsCount(string source, int? expected)
        {
            if (source != null && source.Contains("{0}"))
                source = string.Format(source, sourceFolder);

            string[] result = Utils.SplitPath(source);
            Assert.NotNull(result);
            Assert.Equal(expected, result.Length);
        }

        [Fact]
        public void TestGetPathsContent()
        {
            string[] result = Utils.SplitPath(sourceFolder + "\\TestCase7\\Test;Folder\\;" + sourceFolder + "\\TestXXXX;" + sourceFolder + "\\TestCase7\\Test;Fo;lder\\;" + sourceFolder + "\\TestCase7\\Test,Folder\\;");
            Assert.Equal(sourceFolder + "\\TestCase7\\Test;Folder\\", result[0]);
            Assert.Equal(sourceFolder + "\\TestXXXX", result[1]);
            Assert.Equal(sourceFolder + "\\TestCase7\\Test;Fo;lder\\", result[2]);
            Assert.Equal(sourceFolder + "\\TestCase7\\Test,Folder\\", result[3]);
        }

        [Fact]
        public void TestTrimEndOfString()
        {
            string text = "test\r\n";
            Assert.Equal("test", text.TrimEndOfLine());
            text = "test\r";
            Assert.Equal("test", text.TrimEndOfLine());
            text = "test\n";
            Assert.Equal("test", text.TrimEndOfLine());
            text = "test";
            Assert.Equal("test", text.TrimEndOfLine());
            text = "";
            Assert.Equal("", text.TrimEndOfLine());
        }

        [Theory]
        //[InlineData("\\TestCase1", "*.cs", 1)]
        [InlineData("\\TestCase2", "*.txt", 2)]
        [InlineData("\\TestCase2", "*.txt;*.xls", 3)]
        [InlineData("\\TestCase2", null, 0)]
        [InlineData("\\TestCase11", "#!*python", 2)]
        [InlineData("\\TestCase11", "#!*python;#!*sh", 3)]
        public void TestAsteriskGetFilesWithoutExclude(string folder, string pattern, int expectedCount)
        {
            var result = Utils.GetFileListEx(new FileFilter(sourceFolder + folder, pattern, null, false, false, false, true, true, false, 0, 0, FileDateFilter.None, null, null)).ToArray();
            Assert.Equal(expectedCount, result.Length);
        }

        [Theory]
        [InlineData("\\TestCase13", "*.*", "Obj\\*", 10)]
        [InlineData("\\TestCase13", "*.*", ".svn\\*", 11)]
        [InlineData("\\TestCase13", "*.*", ".svn\\*;obj\\*", 6)]
        [InlineData("\\TestCase13", "*.*", "*.*", 0)]
        [InlineData("\\TestCase13", "*.*", "", 15)]
        public void TestAsteriskGetFilesWithExclude(string folder, string pattern, String excludePattern, int expectedCount)
        {
            // This recurses to subfolders
            var result = Utils.GetFileListEx(new FileFilter(sourceFolder + folder, pattern, excludePattern, false, false, true, true, true, false, 0, 0, FileDateFilter.None, null, null)).ToArray();
            Assert.Equal(expectedCount, result.Length);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, "0.0s")]
        [InlineData(1, 30, 15, 7, 123, "54h 15m 7.123s")]
        [InlineData(0, 10, 0, 1, 234, "10h 0m 1.234s")]
        [InlineData(0, 0, 13, 1, 234, "13m 1.234s")]
        [InlineData(0, 0, 0, 1, 234, "1.234s")]
        [InlineData(0, 0, 0, 0, 123456789, "34h 17m 36.789s")]
        public void TestDurationGetPrettyString(int days, int hours, int minutes, int seconds, int milliseconds, String expectedString)
        {
            var duration = new TimeSpan(days, hours, minutes, seconds, milliseconds);
            Assert.Equal(expectedString, duration.GetPrettyString());
        }

        [Theory]
        [InlineData("*.txt", "test.txt", true)]
        [InlineData("*.cs", "test.txt", false)]
        [InlineData("*.*", "test.txt", true)]
        [InlineData("*t", "test", true)]
        [InlineData("*.", "test", true)]
        [InlineData("*.", "test.txt", false)]
        [InlineData(".*", "test.txt", false)]
        [InlineData(".*", ".gitignore", true)]
        [InlineData("foo*.*", "test.txt", false)]
        [InlineData("foo*.*", "footest.txt", true)]
        [InlineData("foo??.*", "footest.txt", false)]
        [InlineData("foo??.*", "foote.txt", true)]
        public void TestWildcardMatch(string pattern, string fileName, bool expected)
        {
            bool result = SafeDirectory.WildcardMatch(fileName, pattern, true);
            Assert.Equal(expected, result);
        }
    }
}
