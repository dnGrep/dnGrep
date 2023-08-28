using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;
using dnGREP.Common.IO;
using dnGREP.Common.UI;
using dnGREP.WPF;
using Xunit;

namespace Tests
{
    public class UtilsTest : TestBase, IDisposable
    {
        private readonly string sourceFolder;
        private string destinationFolder;

        public UtilsTest()
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

        [Fact]
        public void TestGetContextLines()
        {
            string test = "Hi\r\nmy\r\nWorld\r\nMy name is Denis\r\nfor\r\nloop";

            List<GrepMatch> bodyMatches = new();
            List<GrepLine> lines = new();
            using (StringReader reader = new(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 9, 2));
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


            bodyMatches = new();
            using (StringReader reader = new(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 9, 2));
                lines = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }
            Assert.Single(lines);

            bodyMatches = new();
            using (StringReader reader = new(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 4, 1));
                lines = Utils.GetLinesEx(reader, bodyMatches, 10, 0);
            }
            Assert.Equal(2, lines.Count);
            Assert.Equal(1, lines[0].LineNumber);
            Assert.Equal("Hi", lines[0].LineText);
            Assert.True(lines[0].IsContext);
            Assert.Equal("my", lines[1].LineText);
            Assert.False(lines[1].IsContext);

            bodyMatches = new();
            using (StringReader reader = new(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 34, 1));
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

            // test added for github issue 417: the 'before' context lines were missing
            // from multiline regex match
            bodyMatches = new();
            using (StringReader reader = new(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 4, 15, 21));
                lines = Utils.GetLinesEx(reader, bodyMatches, 2, 3);
            }

            Assert.Equal(5, lines.Count);
            Assert.Equal(2, lines[0].LineNumber);
            Assert.Equal(3, lines[1].LineNumber);
            Assert.Equal("World", lines[1].LineText);
            Assert.True(lines[1].IsContext);
            Assert.Equal(4, lines[2].LineNumber);
            Assert.Equal("My name is Denis", lines[2].LineText);
            Assert.False(lines[2].IsContext);
        }

        [Fact]
        public void TestGetCaptureGroups()
        {
            string test = "a1 b2 c3 d4";

            List<GrepMatch> bodyMatches = new();
            List<GrepLine> lines = new();
            using (StringReader reader = new(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", @"\w(\d)", 0, 0, 2, new[] { new GrepCaptureGroup("1", 1, 1, "1") }));
                bodyMatches.Add(new GrepMatch("", @"\w(\d)", 0, 3, 2, new[] { new GrepCaptureGroup("2", 4, 1, "2") }));
                bodyMatches.Add(new GrepMatch("", @"\w(\d)", 0, 6, 2, new[] { new GrepCaptureGroup("3", 7, 1, "3") }));
                bodyMatches.Add(new GrepMatch("", @"\w(\d)", 0, 9, 2, new[] { new GrepCaptureGroup("4", 10, 1, "4") }));
                lines = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Single(lines);
            GrepLine line = lines[0];
            Assert.Equal(1, line.LineNumber);
            Assert.Equal(test, line.LineText);
            Assert.Equal(4, line.Matches.Count);
            GrepMatch match = line.Matches[1];
            Assert.Equal("b2", line.LineText.Substring(match.StartLocation, match.Length));
            Assert.Single(match.Groups);
            GrepCaptureGroup group = match.Groups[0];
            Assert.Equal("2", line.LineText.Substring(group.StartLocation, group.Length));
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
        [InlineData("{0}\\TestCase1\\test-file-code.cs", "{0}\\TestCase1")]
        [InlineData("{0}\\TestCase1", "{0}\\TestCase1")]
        [InlineData("{0}\\TestCas\\", "")]
        [InlineData("{0}\\TestCase1\\test-file-code.cs;{0}\\TestCase2\\test-file-plain.txt", "{0}")]
        [InlineData("{0}\\Test*", "{0}")]
        [InlineData("{0}\\T*e7", "{0}\\TestCase7")]
        public void TestGetBaseFolder(string relativePath, string expected)
        {
            string path = string.Format(relativePath, sourceFolder);

            string result = string.Format(expected, sourceFolder);
            Assert.Equal(result, UiUtils.GetBaseFolder(path));
        }

        [Theory]
        [InlineData("{0}\\TestCase7\\Test;Folder", "{0}\\TestCase7\\Test;Folder")]
        [InlineData("{0}\\TestCase7\\Test,Folder", "{0}\\TestCase7\\Test,Folder")]
        [InlineData("{0}\\TestCase7\\Test,Folder\\logA", "{0}\\TestCase7\\Test,Folder\\logA")]
        [InlineData("{0}\\TestCase7\\path.1 final", "{0}\\TestCase7\\path.1 final")]
        [InlineData("\"{0}\\TestCase7\\path.1 final, server\"", "{0}\\TestCase7\\path.1 final, server")] // tests for issue 184, needs to be quoted
        [InlineData("\"{0}\\TestCase7\\path.1 final; server\"", "{0}\\TestCase7\\path.1 final; server")]
        public void TestGetBaseFolderWithColons(string relativePath, string expected)
        {
            string path = string.Format(relativePath, sourceFolder);
            string result = string.Format(expected, sourceFolder);

            Assert.Equal(result, UiUtils.GetBaseFolder(path));
        }

        [Theory]
        [InlineData("{0}\\TestCase1\\test-file-code.cs", true)]
        [InlineData("{0}\\TestCase1", true)]
        [InlineData("{0}\\TestCas\\", false)]
        [InlineData("{0}\\TestCase1\\test-file-code.cs;{0}\\TestCase2\\test-file-plain.txt", true)]
        [InlineData("{0}\\Test*", true)]
        [InlineData("{0}\\T*e7", true)]
        [InlineData("{0}\\TestCase7\\Test;Folder", true)]
        [InlineData("{0}\\TestCase7\\Test,Folder\\log*", true)]
        [InlineData("{0}\\TestCase7\\Test,Folder\\log?", true)]
        [InlineData("{0}\\TestCase7\\path.1 final, server", false)] // tests for issue 184, needs to be quoted
        [InlineData("\"{0}\\TestCase7\\path.1 final, server\"", true)]
        public void TestHasSingleBaseFolder(string relativePath, bool expected)
        {
            string path = string.Format(relativePath, sourceFolder);
            Assert.Equal(expected, UiUtils.HasSingleBaseFolder(path));
        }

        [Theory]
        [InlineData("{0}\\TestCase1\\test-file-code.cs", true)]
        [InlineData("{0}\\TestCase1\\test-file-code2.cs", false)]
        [InlineData("{0}\\TestCase1\\", true)]
        [InlineData("{0}\\TestCase1", true)]
        [InlineData("{0}\\TestCas\\", false)]
        [InlineData("{0}\\TestCase1\\test-file-code.cs;{0}\\TestCase1\\test-file-plain.txt", true)]
        [InlineData("{0}\\TestCase1\\test-file-code.cs;{0}\\TestCase1\\test-file-plain.txt;{0}\\TestCase1", true)]
        [InlineData("{0}\\TestCase1\\test11-file-code.cs;{0}\\TestCase1\\test-file-plain.txt;{0}\\TestCase1", false)]
        [InlineData("{0}\\TestCase1\\test-file-code.cs;{0}\\TestCase1\\test-file-plain.txt;{0}1\\TestCase1", false)]
        [InlineData("{0}\\TestCase*", true)]
        [InlineData("{0}\\TestCase*\\", false)]
        [InlineData("{0}\\TestCase1\\*.txt", true)]
        [InlineData("{0}\\TestCase1\\*.cpp", false)]
        [InlineData("{0}\\TestCase1\\test*", true)]
        public void TestIsPathValid(string relativePath, bool expected)
        {
            string path = string.Format(relativePath, sourceFolder);
            Assert.Equal(expected, Utils.IsPathValid(path));
        }

        [Theory]
        [InlineData("{0}\\TestCase7\\Test;Folder\\issue-10.txt", true)]
        [InlineData("{0}\\TestCase7\\Test,Folder\\issue-10.txt", true)]
        [InlineData("{0}\\TestCase7\\Test,Folder\\log?", true)]
        [InlineData("{0}\\TestCase7\\Test;Folder\\*.txt", true)]
        [InlineData("{0}\\TestCase7\\Test,Folder\\*.txt", true)]
        [InlineData("{0}\\TestCase7\\path.1 final, server", false)] // special case issue 184: needs to quoted
        [InlineData("\"{0}\\TestCase7\\path.1 final, server\"", true)]
        [InlineData("\"{0}\\TestCase7\\path.1 final; server\"", true)]
        public void TestIsPathValidWithColon(string relativePath, bool expected)
        {
            string path = string.Format(relativePath, sourceFolder);
            Assert.Equal(expected, Utils.IsPathValid(path));
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
            List<int> lineNumbers = new();
            List<GrepMatch> bodyMatches = new();
            List<GrepLine> results = new();
            using (StringReader reader = new(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 3, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Equal(1, results.Count(l => l.IsContext == false));
            Assert.Equal("Hello world", results[0].LineText);
            Assert.Single(results[0].Matches);
            Assert.Equal(1, results[0].LineNumber);

            using (StringReader reader = new(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 14, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }


            Assert.Single(results);
            Assert.Equal("My tests are good", results[0].LineText);
            Assert.Single(results[0].Matches);
            Assert.Equal(2, results[0].LineNumber);

            using (StringReader reader = new(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 3, 11));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Equal(2, results.Count);
            Assert.Equal("Hello world", results[0].LineText);
            Assert.Equal("My tests are good", results[1].LineText);
            Assert.Single(results[0].Matches);
            Assert.Single(results[1].Matches);
            Assert.Equal(1, results[0].LineNumber);
            Assert.Equal(2, results[1].LineNumber);

            using (StringReader reader = new(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 3, 30));
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

            using (StringReader reader = new("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 2, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Single(results);
            Assert.Equal("test", results[0].LineText);
            Assert.Single(results[0].Matches);
            Assert.Equal(1, results[0].LineNumber);

            using (StringReader reader = new("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 0, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Single(results);
            Assert.Equal("test", results[0].LineText);
            Assert.Single(results[0].Matches);
            Assert.Equal(1, results[0].LineNumber);

            using (StringReader reader = new("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 10, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Empty(results);

            using (StringReader reader = new("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 2, 10));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.Empty(results);

            using (StringReader reader = new(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepMatch("", 0, 3, 2));
                bodyMatches.Add(new GrepMatch("", 0, 6, 2));
                bodyMatches.Add(new GrepMatch("", 0, 14, 2));
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
        public void TestTextReaderReadLine()
        {
            string text = "Hello world" + Environment.NewLine + "My tests are good\nHow about \ryours?\n";
            int lineNumber = 0;
            using (StringReader baseReader = new(text))
            {
                using EolReader reader = new(baseReader);
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
            Assert.Equal(4, lineNumber);
            text = "Hello world";
            lineNumber = 0;
            using (StringReader baseReader = new(text))
            {
                using EolReader reader = new(baseReader);
                while (!reader.EndOfStream)
                {
                    lineNumber++;
                    var line = reader.ReadLine();
                    Assert.Equal("Hello world", line);
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
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase3\\SubFolder\\test-file-plain-hidden.txt", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase3\\test-file-code.cs", "", new(), Encoding.Default)
            };
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
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase3\\SubFolder\\test-file-plain-hidden.txt", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase3\\test-file-code.cs", "", new(), Encoding.Default)
            };
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
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase3\\SubFolder\\test-file-plain-hidden.txt", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase3\\test-file-code.cs", "", new(), Encoding.Default)
            };
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
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase3\\SubFolder\\test-file-plain-hidden.txt", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase3\\test-file-code.cs", "", new(), Encoding.Default)
            };
            Utils.CopyFiles(source, longDestinationFolder + "\\TestCase3", OverwriteFile.Yes);
            Assert.Equal(2, Directory.GetFiles(longDestinationFolder + "\\TestCase3", "*.*", SearchOption.TopDirectoryOnly).Length);
            Assert.False(Directory.Exists(longDestinationFolder + "\\TestCase3\\SubFolder"));
        }

        [Fact]
        public void TestCopyResults()
        {
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };
            Utils.CopyFiles(source, sourceFolder, destinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(destinationFolder + "\\TestCase1").Length);
            source.Add(new(sourceFolder + "\\issue-10.txt", "", new(), Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, destinationFolder, OverwriteFile.Yes);
            Assert.Equal(3, Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length);
            try
            {
                Utils.CopyFiles(source, sourceFolder, destinationFolder, OverwriteFile.No);
                Assert.Fail("Not supposed to get here");
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
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };
            Utils.CopyFiles(source, sourceFolder, longDestinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(longDestinationFolder + "\\TestCase1").Length);
            source.Add(new(sourceFolder + "\\issue-10.txt", "", new(), Encoding.Default));
            Utils.CopyFiles(source, sourceFolder, longDestinationFolder, OverwriteFile.Yes);
            Assert.Equal(3, Directory.GetFiles(longDestinationFolder, "*.*", SearchOption.AllDirectories).Length);
            try
            {
                Utils.CopyFiles(source, sourceFolder, longDestinationFolder, OverwriteFile.No);
                Assert.Fail("Not supposed to get here");
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
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\TestCase1\\test-file-plain2.txt", "", new(), Encoding.Default)
            };
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
            GrepCore core = new();
            var results = core.Search(Directory.GetFiles(destinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.None, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);
            ReportWriter.SaveResultsAsCSV(results, SearchType.PlainText, destinationFolder + "\\test.csv");
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
            GrepCore core = new();
            var results = core.Search(Directory.GetFiles(longDestinationFolder + "\\TestCase3", "*.*"), SearchType.PlainText, "string", GrepSearchOption.None, -1);
            Assert.Equal(2, results.Count);
            Assert.Equal(3, results[0].Matches.Count);
            Assert.Equal(282, results[1].Matches.Count);
            ReportWriter.SaveResultsAsCSV(results, SearchType.PlainText, longDestinationFolder + "\\test.csv");
            string[] stringLines = File.ReadAllLines(longDestinationFolder + "\\test.csv");
            Assert.Equal(177, stringLines.Length);
            Assert.Equal("File Name", stringLines[0].Split(',')[0].Trim());
            Assert.Equal("1", stringLines[1].Split(',')[1].Trim());
            Assert.Equal("\"\tstring returnedLine = Utils.GetLine(body", stringLines[2].Split(',')[2].Trim());
        }

        [Fact]
        public void DeleteFilesTest()
        {
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };
            Utils.CopyFiles(source, sourceFolder, destinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(destinationFolder + "\\TestCase1\\").Length);
            List<GrepSearchResult> source2 = new()
            {
                new(destinationFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default)
            };
            Utils.DeleteFiles(source2);
            Assert.Single(Directory.GetFiles(destinationFolder + "\\TestCase1\\"));
            source2.Add(new(destinationFolder + "\\test-file-code.cs", "", new(), Encoding.Default));
            Utils.DeleteFiles(source2);
            Assert.Single(Directory.GetFiles(destinationFolder + "\\TestCase1\\"));
            source2.Add(new(destinationFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default));
            Utils.DeleteFiles(source2);
            Assert.Empty(Directory.GetFiles(destinationFolder + "\\TestCase1\\"));
        }

        [Fact]
        public void DeleteFilesTestLongPath()
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };
            Utils.CopyFiles(source, sourceFolder, longDestinationFolder, OverwriteFile.No);
            Assert.Equal(2, Directory.GetFiles(longDestinationFolder + "\\TestCase1\\").Length);
            List<GrepSearchResult> source2 = new()
            {
                new(longDestinationFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default)
            };
            Utils.DeleteFiles(source2);
            Assert.Single(Directory.GetFiles(longDestinationFolder + "\\TestCase1\\"));
            source2.Add(new(longDestinationFolder + "\\test-file-code.cs", "", new(), Encoding.Default));
            Utils.DeleteFiles(source2);
            Assert.Single(Directory.GetFiles(longDestinationFolder + "\\TestCase1\\"));
            source2.Add(new(longDestinationFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default));
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
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };
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
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };
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
        [InlineData("", true, true, true, 0, 0, 5)]
        public void GetFileListTest(string namePattern, bool isRegex, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo, int expected)
        {
            string testCase2 = Path.Combine(sourceFolder, @"TestCase2");
            string destFolder = Path.Combine(destinationFolder, @"TestCase2");
            string hiddenFolder = Path.Combine(destinationFolder, @"TestCase2", @"HiddenFolder");
            DirectoryInfo di = new(destFolder);
            if (!di.Exists)
            {
                di.Create();
                DirectoryEx.Copy(testCase2, destFolder);
            }
            di = new(hiddenFolder);
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(Path.Combine(di.FullName, "test-file-plain-hidden.txt"), "Hello world");
            }
            di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

            string[] files = Utils.GetFileList(destFolder, namePattern, string.Empty, isRegex, false, includeSubfolders, includeHidden,
                true, false, false, sizeFrom, sizeTo, FileDateFilter.None, null, null, false, -1);
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
        [InlineData("", true, true, true, 0, 0, 5)]
        public void GetFileListTestLongPath(string namePattern, bool isRegex, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo, int expected)
        {
            string longDestinationFolder = GetLongPathDestination(Guid.NewGuid().ToString());
            string testCase2 = Path.Combine(sourceFolder, @"TestCase2");
            string destFolder = Path.Combine(longDestinationFolder, @"TestCase2");
            string hiddenFolder = Path.Combine(longDestinationFolder, @"TestCase2", @"HiddenFolder");
            DirectoryInfo di = new(destFolder);
            if (!di.Exists)
            {
                di.Create();
                DirectoryEx.Copy(testCase2, destFolder);
            }
            di = new(hiddenFolder);
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(Path.Combine(di.FullName, "test-file-plain-hidden.txt"), "Hello world");
            }
            di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

            string[] files = Utils.GetFileList(destFolder, namePattern, string.Empty, isRegex, false, includeSubfolders, includeHidden,
                true, false, false, sizeFrom, sizeTo, FileDateFilter.None, null, null, false, -1);
            Assert.Equal(expected, files.Length);
        }

        [Theory]
        [InlineData("*.txt", 17)]
        [InlineData("*.c", 2)]
        public void GetArchiveFileListTest(string namePattern, int expected)
        {
            string testCase17 = Path.Combine(sourceFolder, @"TestCase17");
            string destFolder = Path.Combine(destinationFolder, @"TestCase17");
            DirectoryInfo di = new(destFolder);
            if (!di.Exists)
            {
                di.Create();
                DirectoryEx.Copy(testCase17, destFolder);
            }

            FileFilter filter = new(destFolder, namePattern, string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);

            var files = Utils.GetFileListIncludingArchives(filter).ToArray();

            Assert.Equal(expected, files.Length);
        }

        [Theory]
        [InlineData("*.txt", 17)]
        [InlineData("*.c", 2)]
        public void GetArchiveFileListTestLongPath(string namePattern, int expected)
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

            FileFilter filter = new(destFolder, namePattern, string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);

            var files = Utils.GetFileListIncludingArchives(filter).ToArray();

            Assert.Equal(expected, files.Length);
        }

        [Fact]
        public void TestArchiveFilters()
        {
            // this test is repeated from the same in GrepCoreTest because
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

            // all files
            FileFilter filter = new(destFolder, "*.*", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            var files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(18, files.Length);

            // all .ttt files
            filter = new(destFolder, "*.ttt", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(13, files.Length);

            // all but .ttt files
            filter = new(destFolder, "*.*", "*.ttt", false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(5, files.Length);

            // all .md files
            filter = new(destFolder, "*.md", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Single(files);

            // exclude below depth2
            filter = new(destFolder, "*.ttt", @"depth2\*", false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(8, files.Length);

            // regex filter
            filter = new(destFolder, @"\bl.*", string.Empty, true, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(2, files.Length);

            // regex exclude filter
            filter = new(destFolder, ".*", @"\bshe", true, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(16, files.Length);

            // exclude hidden
            filter = new(destFolder, "*.ttt", string.Empty, false, false, false, true, -1, false,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(10, files.Length);

            // exclude binary
            filter = new(destFolder, "*.*", string.Empty, false, false, false, true, -1, true,
                false, true, false, 0, 0, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(17, files.Length);

            // size filter
            filter = new(destFolder, "*.ttt", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 10, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(12, files.Length);

            // date filter
            filter = new(destFolder, "*.ttt", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.Modified, null, new DateTime(2019, 1, 1));
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(4, files.Length);

            // shebang filter
            filter = new(destFolder, "#!*python", string.Empty, false, false, false, true, -1, true,
                true, true, false, 0, 0, FileDateFilter.None, null, null);
            files = Utils.GetFileListIncludingArchives(filter).ToArray();
            Assert.Equal(2, files.Length);
        }

        [IgnoreIfNotAdministratorTheory] // must run as Administrator to create symbolic link
        [InlineData(false)]
        [InlineData(true)]
        public void GetSymlinkFilesTest(bool useLongPathLink)
        {
            string testCase1 = Path.Combine(sourceFolder, @"TestCase1");

            string targetFolder = destinationFolder;
            targetFolder = Path.Combine(targetFolder, @"TestCase1");
            string targetFile = Path.Combine(targetFolder, @"test-file-plain.txt");

            DirectoryInfo di = new(targetFolder);
            if (!di.Exists) di.Create();
            DirectoryEx.Copy(testCase1, targetFolder);

            string linkFolder = useLongPathLink ? GetLongPathDestination(Guid.NewGuid().ToString()) : destinationFolder;
            linkFolder = Path.Combine(linkFolder, @"TestSymlink");
            string linkFile = Path.Combine(linkFolder, @"myfile.txt");
            if (useLongPathLink)
                linkFile = PathEx.GetLongPath(linkFile);

            di = new(linkFolder);
            if (!di.Exists) di.Create();

            FileEx.CreateSymbolicLink(linkFile, targetFile);

            bool followSymlinks = false;
            string[] files = Utils.GetFileList(linkFolder, "*.txt", string.Empty, false, false, true, true,
                true, true, followSymlinks, 0, 0, FileDateFilter.None, null, null, false, -1);
            Assert.Empty(files);

            followSymlinks = true;
            files = Utils.GetFileList(linkFolder, "*.txt", string.Empty, false, false, true, true,
                true, true, followSymlinks, 0, 0, FileDateFilter.None, null, null, false, -1);
            Assert.Single(files);
            Assert.Equal(@"myfile.txt", Path.GetFileName(files[0]));
            Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(files[0])));
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
            DirectoryInfo di = new(sourceFolder + "\\TestCaseDates");
            string testFile = Path.Combine(di.FullName, "test-file.txt");
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(testFile, "Hello world");
            }
            di.Attributes = FileAttributes.Directory;
            FileInfo fi = new(testFile);

            DateTime fileTime = new(2017, 01, fileDay, 10, 12, 14, DateTimeKind.Local);
            DateTime? startTime = null, endTime = null;

            if (startDay.HasValue)
                startTime = new(2017, 01, startDay.Value, 0, 0, 0, DateTimeKind.Local);

            if (endDay.HasValue)
                endTime = new(2017, 01, endDay.Value, 0, 0, 0, DateTimeKind.Local);

            fi.CreationTime = fileTime;
            Assert.Equal(expected, Utils.GetFileList(di.FullName, "*", string.Empty, false, false, false, false, false, false, false, 0, 0, FileDateFilter.Created, startTime, endTime, false, -1).Length);

            fi.LastWriteTime = fileTime;
            Assert.Equal(expected, Utils.GetFileList(di.FullName, "*", string.Empty, false, false, false, false, false, false, false, 0, 0, FileDateFilter.Modified, startTime, endTime, false, -1).Length);
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
            DirectoryInfo di = new(sourceFolder + "\\TestCaseDates");
            string testFile = Path.Combine(di.FullName, "test-file.txt");
            if (!di.Exists)
            {
                di.Create();
                File.WriteAllText(testFile, "Hello world");
            }
            di.Attributes = FileAttributes.Directory;
            FileInfo fi = new(testFile);

            DateTime now = DateTime.Now;

            DateTime fileTime = now.AddHours(-1 * filePast);
            DateTime startTime = now.AddHours(-1 * toHoursPast);
            DateTime endTime = now.AddHours(-1 * fromHoursPast);

            fi.CreationTime = fileTime;
            Assert.Equal(expected, Utils.GetFileList(di.FullName, "*", string.Empty, false, false, false, false, false, false, false, 0, 0, FileDateFilter.Created, startTime, endTime, false, -1).Length);

            fi.LastWriteTime = fileTime;
            Assert.Equal(expected, Utils.GetFileList(di.FullName, "*", string.Empty, false, false, false, false, false, false, false, 0, 0, FileDateFilter.Modified, startTime, endTime, false, -1).Length);
        }

        [Fact]
        public void GetFileListTestWithMultiplePaths()
        {
            string path = sourceFolder + "\\TestCase2;" + sourceFolder + "\\TestCase2\\excel-file.xls";
            Assert.Equal(4, Utils.GetFileList(path, "*.*", "", false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);

            path = sourceFolder + "\\TestCase2;" + sourceFolder + "\\TestCase3\\test-file-code.cs";
            Assert.Equal(5, Utils.GetFileList(path, "*.*", "", false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase2";
            Assert.Equal(5, Utils.GetFileList(path, "*.*", "", false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);

            path = sourceFolder + "\\TestCase2;" + sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase3\\test-file-plain.txt";
            Assert.Equal(6, Utils.GetFileList(path, "*.*", string.Empty, false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase3\\test-file-plain.txt";
            Assert.Equal(2, Utils.GetFileList(path, "*.*", string.Empty, false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase3\\test-file-plain.txt;";
            Assert.Equal(2, Utils.GetFileList(path, "*.*", string.Empty, false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs," + sourceFolder + "\\TestCase3\\test-file-plain.txt,";
            Assert.Equal(2, Utils.GetFileList(path, "*.*", string.Empty, false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);

            path = sourceFolder + "\\TestCase3\\test-file-code.cs," + sourceFolder + "\\TestCase3\\test-file-plain.txt";
            Assert.Equal(2, Utils.GetFileList(path, "*.*", string.Empty, false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);
        }

        [Theory]
        [InlineData("{0}\\TestCase7\\log*", 2)]
        [InlineData("{0}\\TestCase7\\l?g*", 2)]
        [InlineData("{0}\\TestCase7\\log?", 2)]
        [InlineData("{0}\\TestCase7\\Test*", 3)]
        [InlineData("{0}\\TestCase7\\*Folder", 2)]
        [InlineData("{0}\\TestCase7\\T*Folder", 2)]
        [InlineData("{0}\\TestCase7\\Test,Folder\\log?", 2)]
        [InlineData("{0}\\TestCase7\\logA\\*.txt", 1)]
        [InlineData("{0}\\TestCase7\\logA;{0}\\TestCase7\\Test*", 4)]
        [InlineData("{0}\\TestCase7\\Test*;{0}\\TestCase7\\LogB", 4)]
        [InlineData("{0}\\TestCase7\\log?;{0}\\TestCase7\\Test*", 5)]
        public void GetFileListTestWithPathWildcards(string pathPattern, int expected)
        {
            string path = string.Format(pathPattern, sourceFolder);
            Assert.Equal(expected, Utils.GetFileList(path, "*.*", "", false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);
        }

        [Fact]
        public void GetFileListWithExcludes()
        {
            string path = sourceFolder + "\\TestCase2";
            Assert.Equal(3, Utils.GetFileList(path, "*.*", "*.xls", false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);
            Assert.Empty(Utils.GetFileList(path, "excel*.*", "*.xls", false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1));
            Assert.Single(Utils.GetFileList(path, "excel*.*", "*.xs", false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1));
            Assert.Equal(2, Utils.GetFileList(path, "t*st-file-*.*", "*.cs", false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);
            Assert.Equal(2, Utils.GetFileList(path, "t?st-file-*.*", "*.cs", false, false, false, false, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1).Length);
        }

        [Fact]
        public void GetFileListFromNonExistingFolderReturnsEmptyString()
        {
            Assert.Empty(Utils.GetFileList(sourceFolder + "\\NonExisting", "*.*", string.Empty, false, false, true, true, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1));
        }

        [Fact]
        public void GetReadOnlyFilesTest()
        {
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };

            List<GrepSearchResult> destination = new()
            {
                new(destinationFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(destinationFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };

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
            List<GrepSearchResult> source = new()
            {
                new(sourceFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(sourceFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };

            List<GrepSearchResult> destination = new()
            {
                new(longDestinationFolder + "\\TestCase1\\test-file-code.cs", "", new(), Encoding.Default),
                new(longDestinationFolder + "\\TestCase1\\test-file-plain.txt", "", new(), Encoding.Default)
            };

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

        public static IEnumerable<object?[]> TestGetPaths_Source
        {
            get
            {
                yield return new object?[] { "{0}\\TestCase5\\big-word-document.doc", 1 };
                yield return new object?[] { "{0}\\TestCase7;{0}\\TestCase7", 2 };
                yield return new object?[] { "{0}\\TestCase5;{0}\\TestCase7", 2 };
                yield return new object?[] { "{0}\\TestCase7\\Test,Folder\\;{0}\\TestCase7", 2 };
                yield return new object?[] { "{0}\\TestCase7\\Test;Folder\\;{0}\\TestCase7", 2 };
                yield return new object?[] { "{0}\\TestCase7\\Test;Folder\\;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder\\", 3 };
                yield return new object?[] { "{0}\\TestCase7\\Test;Folder;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder", 3 };
                yield return new object?[] { "{0}\\TestCase7\\Test;Folder ;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder", 3 };
                yield return new object?[] { "{0}\\TestCase7\\Test;Folder;{0}\\TestCase7 ;{0}\\TestCase7\\Test;Folder", 3 };
                yield return new object?[] { "{0}\\TestCase7\\Test;Folder;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder ", 3 };
                yield return new object?[] { null, 0 };
                yield return new object?[] { "", 0 };
                yield return new object?[] { "{0}\\TestCase7\\log*", 2 };
                yield return new object?[] { "{0}\\TestCase7\\log?", 2 };
                yield return new object?[] { "{0}\\TestCase7\\Test*", 3 };
                yield return new object?[] { "{0}\\TestCase7\\logA\\*.txt", 1 };
                yield return new object?[] { "{0}\\TestCase7\\Test,Folder\\log?", 2 };
                yield return new object?[] { "{0}\\TestCase7\\Test,Folder\\logA\\*.txt", 1 };
            }
        }

        [Theory]
        [MemberData(nameof(TestGetPaths_Source))]
        public void TestGetPathsCount(string? source, int? expected)
        {
            if (source != null && source.Contains("{0}"))
                source = string.Format(source, sourceFolder);

            string[] result = UiUtils.SplitPath(source, false);
            Assert.NotNull(result);
            Assert.Equal(expected, result.Length);
        }

        [Theory]
        [InlineData("\"{0}\\TestCase7\\path.1 final, server\",{0}\\TestCase7\\Test,Folder\\;{0}\\TestCase7", 3)]
        [InlineData("\"{0}\\TestCase7\\path.1 final, server\";{0}\\TestCase7\\Test,Folder\\;{0}\\TestCase7", 3)]
        [InlineData("\"{0}\\TestCase7\\path.1 final; server\",{0}\\TestCase7\\Test,Folder\\;{0}\\TestCase7", 3)]
        [InlineData("\"{0}\\TestCase7\\path.1 final; server\";{0}\\TestCase7\\Test,Folder\\;{0}\\TestCase7", 3)]
        [InlineData("{0}\\TestCase7\\Test,Folder\\;\"{0}\\TestCase7\\path.1 final; server\";{0}\\TestCase7", 3)]
        [InlineData("{0}\\TestCase7\\Test,Folder\\;{0}\\TestCase7;\"{0}\\TestCase7\\path.1 final; server\"", 3)]
        [InlineData("\"{0}\\TestCase7\\Test,Folder\\\";{0}\\TestCase7;\"{0}\\TestCase7\\path.1 final; server\"", 3)]
        [InlineData("\"{0}\\TestCase7\\Test,Folder\";{0}\\TestCase7 ;\"{0}\\TestCase7\\path.1 final; server \"", 3)]
        public void TestGetPathsCountQuoted(string path, int expected) // tests for issue 184
        {
            path = string.Format(path, sourceFolder);

            string[] result = UiUtils.SplitPath(path, false);
            Assert.NotNull(result);
            Assert.Equal(expected, result.Length);
        }


        [Fact]
        public void TestGetPathsContent()
        {
            string[] result = UiUtils.SplitPath(sourceFolder + "\\TestCase7\\Test;Folder\\;" + sourceFolder + "\\TestXXXX;" + sourceFolder + "\\TestCase7\\Test;Fo;lder\\;" + sourceFolder + "\\TestCase7\\Test,Folder\\;", false);
            Assert.Equal(sourceFolder + "\\TestCase7\\Test;Folder\\", result[0]);
            Assert.Equal(sourceFolder + "\\TestXXXX", result[1]);
            Assert.Equal(sourceFolder + "\\TestCase7\\Test;Fo;lder\\", result[2]);
            Assert.Equal(sourceFolder + "\\TestCase7\\Test,Folder\\", result[3]);
        }

        [Theory]
        [InlineData("{0}\\TestCase7\\Test;Folder", "{0}\\TestCase7\\Test;Folder")]
        [InlineData("{0}\\TestCase7\\Test;Folder ", "{0}\\TestCase7\\Test;Folder")]
        [InlineData("{0}\\TestCase7\\Test,Folder", "{0}\\TestCase7\\Test,Folder")]
        [InlineData("{0}\\TestCase7\\log*", "{0}\\TestCase7\\log*")]
        [InlineData("{0}\\TestCase7\\log* ", "{0}\\TestCase7\\log*")]
        [InlineData("{0}\\TestCase7\\log* ;{0}\\TestCase7\\log*", "{0}\\TestCase7\\log*;{0}\\TestCase7\\log*")]
        [InlineData("{0}\\TestCase7\\log* ;{0}\\TestCase7\\log? ", "{0}\\TestCase7\\log*;{0}\\TestCase7\\log?")]
        [InlineData("{0}\\TestCase7\\Test;Folder ;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder", "{0}\\TestCase7\\Test;Folder;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder")]
        [InlineData("{0}\\TestCase7\\Test;Folder;{0}\\TestCase7 ;{0}\\TestCase7\\Test;Folder", "{0}\\TestCase7\\Test;Folder;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder")]
        [InlineData("{0}\\TestCase7\\Test;Folder;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder ", "{0}\\TestCase7\\Test;Folder;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder")]
        [InlineData("\"{0}\\TestCase7\\Test;Folder \";{0}\\TestCase7;{0}\\TestCase7\\Test;Folder", "{0}\\TestCase7\\Test;Folder;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder")]
        [InlineData("\"{0}\\TestCase7\\Test;Folder \";\"{0}\\TestCase7\";{0}\\TestCase7\\Test;Folder", "{0}\\TestCase7\\Test;Folder;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder")]
        [InlineData("\"{0}\\TestCase7\\Test;Folder \";\"{0}\\TestCase7\";\"{0}\\TestCase7\\Test;Folder\"", "{0}\\TestCase7\\Test;Folder;{0}\\TestCase7;{0}\\TestCase7\\Test;Folder")]
        [InlineData("\"{0}\\TestCase7\\path.1 final, server\"", "{0}\\TestCase7\\path.1 final, server")] // tests for issue 184, needs to be quoted
        [InlineData("\"{0}\\TestCase7\\path.1 final; server\"", "{0}\\TestCase7\\path.1 final; server")]
        public void TestCleanPath(string path, string result)
        {
            string input = string.Format(path, sourceFolder);
            string expected = string.Format(result, sourceFolder);
            string cleaned = string.Join(";", UiUtils.SplitPath(input, true));
            Assert.Equal(expected, cleaned);
        }

        [Theory]
        [InlineData("*.*", 1)]
        [InlineData("*.cpp;*.h", 2)]
        [InlineData("*.cpp,*.h", 2)]
        [InlineData("*.cpp ,*.h", 2)]
        [InlineData("*.cpp, *.h", 2)]
        [InlineData("**/test/*,**bin/*", 2)]
        [InlineData(".git\\*;*.resx;*.aip;bin\\*;packages\\*;", 5)]
        [InlineData(";.git\\*;*.resx;*.aip;bin\\*;packages\\*;", 5)]
        [InlineData(".git\\*;*.resx;;;*.aip;;bin\\*;packages\\*;", 5)]
        public void TestSplitPattern(string pattern, int expected)
        {
            Assert.Equal(expected, UiUtils.SplitPattern(pattern).Length);
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

        [Fact]
        public void TestRoundTripDateTimeToString()
        {
            var input = new[]
            {
                new DateTime(2022, 9, 1, 13, 40, 16, DateTimeKind.Local),
                new DateTime(2022, 9, 1, 6, 40, 16, DateTimeKind.Utc),
            };

            foreach (var srcDateTime in input)
            {
                string dateStr = srcDateTime.ToIso8601Date();
                DateTime? date = dateStr.FromIso8601Date();
                Assert.NotNull(date);
                Assert.Equal(srcDateTime.Date, date);

                string dateTimeStr = srcDateTime.ToIso8601DateTime();
                DateTime? dateTime = dateTimeStr.FromIso8601DateTime();
                Assert.NotNull(dateTime);
                Assert.Equal(srcDateTime, dateTime);

                string dateTimeZoneStr = srcDateTime.ToIso8601DateTimeWithZone();
                DateTime? dateTimeZone = dateTimeZoneStr.FromIso8601DateTimeWithZone();
                Assert.NotNull(dateTimeZone);
                Assert.Equal(srcDateTime, dateTimeZone);
            }
        }

        [Theory]
        [InlineData("\\TestCase2", "*.txt", 2)]
        [InlineData("\\TestCase2", "*.txt;*.xls", 3)]
        [InlineData("\\TestCase2", null, 0)]
        [InlineData("\\TestCase11", "#!*python", 2)]
        [InlineData("\\TestCase11", "#!*python;#!*sh", 3)]
        public void TestAsteriskGetFilesWithoutExclude(string folder, string pattern, int expectedCount)
        {
            var result = Utils.GetFileListEx(new FileFilter(sourceFolder + folder, pattern, string.Empty, false, false, false, false, -1, true, true, false, false, 0, 0, FileDateFilter.None, null, null)).ToArray();
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
            var result = Utils.GetFileListEx(new FileFilter(sourceFolder + folder, pattern, excludePattern, false, false, false, true, -1, true, true, false, false, 0, 0, FileDateFilter.None, null, null)).ToArray();
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
            TimeSpan duration = new(days, hours, minutes, seconds, milliseconds);
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

        [Theory]
        [InlineData(@"", 0, false, false, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" /warmUp", 1, false, true, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" ""c:\temp\test data\""", 1, false, false, @"c:\temp\test data\", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)] // old style search directory without flag
        [InlineData(@" ""c:\temp\test data\"" p\w*", 2, false, false, @"c:\temp\test data\", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]  // old style search directory and regex without flags
        [InlineData(@" ""c:\temp\test data"" ""p\w*""", 2, false, false, @"c:\temp\test data", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]  // old style search directory and regex without flags
        [InlineData(@" c:\temp\testData\ ""p\w*""", 2, false, false, @"c:\temp\testData\", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]  // old style search directory and regex without flags
        [InlineData(@" c:\temp\testData ""p\w*""", 2, false, false, @"c:\temp\testData", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]  // old style search directory and regex without flags
        [InlineData(@" -f ""c:\temp\test data\""", 2, false, false, @"c:\temp\test data\", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f ""c:\temp\testData\""", 2, false, false, @"c:\temp\testData\", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f ""c:\temp\testData"";""c:\temp\test files""", 2, false, false, @"c:\temp\testData;c:\temp\test files", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f ""c:\temp\test files"";""c:\temp\testData""", 2, false, false, @"c:\temp\test files;c:\temp\testData", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f ""c:\temp\test files"";""c:\temp\testData"" -s p\w*", 4, false, false, @"c:\temp\test files;c:\temp\testData", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f ""c:\temp\test files"";""c:\temp\testData"" -s ""p\w*""", 4, false, false, @"c:\temp\test files;c:\temp\testData", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData\", 2, false, false, @"c:\temp\testData\", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData", 2, false, false, @"c:\temp\testData", null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f ""c:\temp\test data\"" -s p\w*", 4, false, false, @"c:\temp\test data\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f ""c:\temp\test data\"" -s ""p\w*""", 4, false, false, @"c:\temp\test data\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f ""c:\temp\testData\"" -s p\w*", 4, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData\ -s p\w*", 4, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData -s p\w*", 4, false, false, @"c:\temp\testData", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData -s ""p\w*""", 4, false, false, @"c:\temp\testData", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData -s p""\w*", 4, false, false, @"c:\temp\testData", @"p""\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData -s ""\w*", 4, false, false, @"c:\temp\testData", @"""\w*", null, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData -st Regex -s ""p\w*""", 6, false, false, @"c:\temp\testData", @"p\w*", SearchType.Regex, null, null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData -pm *.txt;*.xml -s ""p\w*""", 6, false, false, @"c:\temp\testData", @"p\w*", null, "*.txt;*.xml", null, null, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData -pt Asterisk -pm *.* -pi *.pdf -s ""p\w*""", 10, false, false, @"c:\temp\testData", @"p\w*", null, "*.*", "*.pdf", FileSearchType.Asterisk, null, null, null, null, null, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData -s p\w* /cs true /ww True /ml false /dn false /bo False", 14, false, false, @"c:\temp\testData", @"p\w*", null, null, null, null, true, true, false, false, false, true, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData /cs true /ww True /ml false", 8, false, false, @"c:\temp\testData", null, null, null, null, null, true, true, false, null, null, false, null, null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData\ -s p\w* -rpt c:\temp\report.txt", 6, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, @"c:\temp\report.txt", null, null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData\ -s p\w* -txt c:\temp\report.txt", 6, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, @"c:\temp\report.txt", null, null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData\ -s p\w* -csv c:\temp\report.csv", 6, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, @"c:\temp\report.csv", null, null, null, null, null, null, null, null, false)]
        [InlineData(@" -f c:\temp\testData\ -s p\w* -csv c:\temp\report.csv -x", 7, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, @"c:\temp\report.csv", null, null, null, null, null, null, null, null, true)]
        [InlineData(@" -f c:\temp\testData\ -s p\w* -x -csv c:\temp\report.csv", 7, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, null, null, @"c:\temp\report.csv", null, null, null, null, null, null, null, null, true)]
        [InlineData(@" -f c:\temp\testData\ -s p\w* -mode Groups -fi false -unique true -scope Global -sl true -sep "" "" -rpt c:\temp\report.txt", 18, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, @"c:\temp\report.txt", null, null, ReportMode.Groups, false, null, true, UniqueScope.Global, true, " ", null, false)]
        [InlineData(@" -f c:\temp\testData\ -s p\w* -mode FullLine -fi true -trim true -rpt c:\temp\report.txt", 12, false, false, @"c:\temp\testData\", @"p\w*", null, null, null, null, null, null, null, null, null, true, @"c:\temp\report.txt", null, null, ReportMode.FullLine, true, true, null, null, null, null, null, false)]
        [InlineData(@" -sep "" """, 2, false, false, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, " ", null, false)]
        [InlineData(@" -script scriptName", 2, false, false, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, null, null, null, null, "scriptName", false)]
        public void SplitCommandLineTest(string commandLine, int argCount,
            bool expInvalidArgument, bool expIsWarmUp, string expSearchPath, string expSearchFor,
            SearchType? expSearchType, string expPatternToInclude, string expPatternToExclude,
            FileSearchType? expTypeOfFileSearch, bool? expCaseSensitive, bool? expWholeWord,
            bool? expMultiline, bool? expDotAsNewLine, bool? expBooleanOperators, bool expExecuteSearch,
            string expReportPath, string expTextPath, string expCsvPath, ReportMode? reportMode,
            bool? includeFileInformation, bool? trimWhitespace, bool? filterUniqueValues,
            UniqueScope? uniqueScope, bool? outputOnSeparateLines, string listItemSeparator,
            string script, bool expExit)
        {
            const string program = @"""C:\\Program Files\\dnGREP\\dnGREP.exe""";
            CommandLineArgs args = new(program + commandLine);

            Assert.Equal(argCount, args.Count);
            Assert.Equal(expInvalidArgument, args.InvalidArgument);
            Assert.Equal(expIsWarmUp, args.WarmUp);
            Assert.Equal(expSearchPath, args.SearchPath);
            Assert.Equal(expSearchFor, args.SearchFor);
            Assert.Equal(expSearchType, args.TypeOfSearch);
            Assert.Equal(expPatternToInclude, args.NamePatternToInclude);
            Assert.Equal(expPatternToExclude, args.NamePatternToExclude);
            Assert.Equal(expTypeOfFileSearch, args.TypeOfFileSearch);
            Assert.Equal(expCaseSensitive, args.CaseSensitive);
            Assert.Equal(expWholeWord, args.WholeWord);
            Assert.Equal(expMultiline, args.Multiline);
            Assert.Equal(expDotAsNewLine, args.DotAsNewline);
            Assert.Equal(expBooleanOperators, args.BooleanOperators);
            Assert.Equal(expExecuteSearch, args.ExecuteSearch);
            Assert.Equal(expReportPath, args.ReportPath);
            Assert.Equal(expTextPath, args.TextPath);
            Assert.Equal(expCsvPath, args.CsvPath);
            Assert.Equal(reportMode, args.ReportMode);
            Assert.Equal(includeFileInformation, args.IncludeFileInformation);
            Assert.Equal(trimWhitespace, args.TrimWhitespace);
            Assert.Equal(filterUniqueValues, args.FilterUniqueValues);
            Assert.Equal(uniqueScope, args.UniqueScope);
            Assert.Equal(outputOnSeparateLines, args.OutputOnSeparateLines);
            Assert.Equal(listItemSeparator, args.ListItemSeparator);
            Assert.Equal(script, args.Script);
            Assert.Equal(expExit, args.Exit);

        }

        [Theory]
        [InlineData(@"a$b\$c", @"azb\$c", "$", "z")]
        [InlineData(@"$ab\$c", @"zab\$c", "$", "z")]
        [InlineData(@"\$ab\$c", @"\$ab\$c", "$", "z")]
        [InlineData(@"a$b\$c", @"azyzzxb\$c", "$", "zyzzx")]
        [InlineData(@"\$", @"\$", "$", "z")]
        [InlineData(@"$", @"z", "$", "z")]
        [InlineData(@"abcdef", @"abcdef", "$", "z")]
        public void TestReplaceUnescaped(string input, string expected, string oldValue, string newValue)
        {
            string actual = input.ReplaceIfNotEscaped(oldValue, newValue);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(@"a$b\$c", true, "$")]
        [InlineData(@"$ab\$c", true, "$")]
        [InlineData(@"a\$b$c", true, "$")]
        [InlineData(@"\$ab$c", true, "$")]
        [InlineData(@"\$ab\$c", false, "$")]
        [InlineData(@"\$", false, "$")]
        [InlineData(@"$", true, "$")]
        [InlineData(@"abcdef", false, "$")]
        public void TestContainsNotUnescaped(string input, bool expected, string toCheck)
        {
            bool actual = input.ConstainsNotEscaped(toCheck);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(@"one two AND three", @"a AND b")]
        [InlineData(@"one two NAND three", @"a NAND b")]
        [InlineData(@"one AND two two AND three", @"a AND b AND c")]
        [InlineData(@"one AND two AND NOT three four", @"a AND b AND NOT c")]
        [InlineData(@"one OR two", @"a OR b")]
        [InlineData(@"one NOR two", @"a NOR b")]
        [InlineData(@"one OR two OR three", @"a OR b OR c")]
        [InlineData(@"one OR two OR NOT three", @"a OR b OR NOT c")]
        [InlineData(@"(one OR two) AND NOT three", @"( a OR b ) AND NOT c")]
        [InlineData(@"false OR true AND (false OR false)", @"a OR b AND ( c OR d )")]
        [InlineData(@"(false OR true) AND false OR false", @"( a OR b ) AND c OR d")]
        [InlineData(@"\w+\s+\w* AND `\p{Sc}*(\s?\d+[.,]?\d*)\p{Sc}*`", @"a AND b")]
        [InlineData(@"`\w+\s+\w*` AND `\p{Sc}*(\s?\d+[.,]?\d*)\p{Sc}*`", @"a AND b")]
        [InlineData(@"`((\"".+?\"")|('.+?'))` AND `test`", @"a AND b")]
        [InlineData(@"<((\"".+?\"")|('.+?'))> AND <test>", @"a AND b")]
        [InlineData(@"sand AND floor", @"a AND b")]
        [InlineData(@"<sand > AND < floor>", @"a AND b")]
        [InlineData(@"`(?<double>\w)\k<double>` AND ` floor`", @"a AND b")]
        public void TestParseBooleanOperators(string input, string expected)
        {
            BooleanExpression exp = new();
            bool success = exp.TryParse(input);
            Assert.True(success);
            Assert.Equal(expected, exp.Expression);
        }

        [Theory]
        [InlineData("not a", "a not", false, true)]
        [InlineData("not a", "a not", true, false)]

        //[InlineData("a (and) b", "a b and", false, false, false)]

        [InlineData("a and b", "a b and", false, false, false)]
        [InlineData("a and b", "a b and", false, true, false)]
        [InlineData("a and b", "a b and", true, false, false)]
        [InlineData("a and b", "a b and", true, true, true)]

        [InlineData("a nand b", "a b nand", false, false, true)]
        [InlineData("a nand b", "a b nand", false, true, true)]
        [InlineData("a nand b", "a b nand", true, false, true)]
        [InlineData("a nand b", "a b nand", true, true, false)]

        [InlineData("a or b", "a b or", false, false, false)]
        [InlineData("a or b", "a b or", false, true, true)]
        [InlineData("a or b", "a b or", true, false, true)]
        [InlineData("a or b", "a b or", true, true, true)]

        [InlineData("a nor b", "a b nor", false, false, true)]
        [InlineData("a nor b", "a b nor", false, true, false)]
        [InlineData("a nor b", "a b nor", true, false, false)]
        [InlineData("a nor b", "a b nor", true, true, false)]

        [InlineData("a and b or c", "a b and c or", false, false, false, false)]
        [InlineData("a and b or c", "a b and c or", false, false, true, true)]
        [InlineData("a and b or c", "a b and c or", false, true, false, false)]
        [InlineData("a and b or c", "a b and c or", false, true, true, true)]
        [InlineData("a and b or c", "a b and c or", true, false, false, false)]
        [InlineData("a and b or c", "a b and c or", true, false, true, true)]
        [InlineData("a and b or c", "a b and c or", true, true, false, true)]
        [InlineData("a and b or c", "a b and c or", true, true, true, true)]

        [InlineData("a or b and c", "a b c and or", false, false, false, false)]
        [InlineData("a or b and c", "a b c and or", false, false, true, false)]
        [InlineData("a or b and c", "a b c and or", false, true, false, false)]
        [InlineData("a or b and c", "a b c and or", false, true, true, true)]
        [InlineData("a or b and c", "a b c and or", true, false, false, true)]
        [InlineData("a or b and c", "a b c and or", true, false, true, true)]
        [InlineData("a or b and c", "a b c and or", true, true, false, true)]
        [InlineData("a or b and c", "a b c and or", true, true, true, true)]

        [InlineData("a and (b or c)", "a b c or and", false, false, false, false)]
        [InlineData("a and (b or c)", "a b c or and", false, false, true, false)]
        [InlineData("a and (b or c)", "a b c or and", false, true, false, false)]
        [InlineData("a and (b or c)", "a b c or and", false, true, true, false)]
        [InlineData("a and (b or c)", "a b c or and", true, false, false, false)]
        [InlineData("a and (b or c)", "a b c or and", true, false, true, true)]
        [InlineData("a and (b or c)", "a b c or and", true, true, false, true)]
        [InlineData("a and (b or c)", "a b c or and", true, true, true, true)]

        [InlineData("( a or b ) and c", "a b or c and", false, false, false, false)]
        [InlineData("( a or b ) and c", "a b or c and", false, false, true, false)]
        [InlineData("( a or b ) and c", "a b or c and", false, true, false, false)]
        [InlineData("( a or b ) and c", "a b or c and", false, true, true, true)]
        [InlineData("( a or b ) and c", "a b or c and", true, false, false, false)]
        [InlineData("( a or b ) and c", "a b or c and", true, false, true, true)]
        [InlineData("( a or b ) and c", "a b or c and", true, true, false, false)]
        [InlineData("( a or b ) and c", "a b or c and", true, true, true, true)]

        [InlineData("not a and b or c", "a not b and c or", false, false, false, false)]
        [InlineData("not a and b or c", "a not b and c or", false, false, true, true)]
        [InlineData("not a and b or c", "a not b and c or", false, true, false, true)]
        [InlineData("not a and b or c", "a not b and c or", false, true, true, true)]
        [InlineData("not a and b or c", "a not b and c or", true, false, false, false)]
        [InlineData("not a and b or c", "a not b and c or", true, false, true, true)]
        [InlineData("not a and b or c", "a not b and c or", true, true, false, false)]
        [InlineData("not a and b or c", "a not b and c or", true, true, true, true)]

        [InlineData("a or b and not c", "a b c not and or", false, false, false, false)]
        [InlineData("a or b and not c", "a b c not and or", false, false, true, false)]
        [InlineData("a or b and not c", "a b c not and or", false, true, false, true)]
        [InlineData("a or b and not c", "a b c not and or", false, true, true, false)]
        [InlineData("a or b and not c", "a b c not and or", true, false, false, true)]
        [InlineData("a or b and not c", "a b c not and or", true, false, true, true)]
        [InlineData("a or b and not c", "a b c not and or", true, true, false, true)]
        [InlineData("a or b and not c", "a b c not and or", true, true, true, true)]

        [InlineData("a or b or not c", "a b c not or or", false, false, false, true)]
        [InlineData("a or b or not c", "a b c not or or", false, false, true, false)]
        [InlineData("a or b or not c", "a b c not or or", false, true, false, true)]
        [InlineData("a or b or not c", "a b c not or or", false, true, true, true)]
        [InlineData("a or b or not c", "a b c not or or", true, false, false, true)]
        [InlineData("a or b or not c", "a b c not or or", true, false, true, true)]
        [InlineData("a or b or not c", "a b c not or or", true, true, false, true)]
        [InlineData("a or b or not c", "a b c not or or", true, true, true, true)]

        [InlineData("a and b and not c", "a b c not and and", false, false, false, false)]
        [InlineData("a and b and not c", "a b c not and and", false, false, true, false)]
        [InlineData("a and b and not c", "a b c not and and", false, true, false, false)]
        [InlineData("a and b and not c", "a b c not and and", false, true, true, false)]
        [InlineData("a and b and not c", "a b c not and and", true, false, false, false)]
        [InlineData("a and b and not c", "a b c not and and", true, false, true, false)]
        [InlineData("a and b and not c", "a b c not and and", true, true, false, true)]
        [InlineData("a and b and not c", "a b c not and and", true, true, true, false)]

        [InlineData("( a and b ) and not c", "a b and c not and", false, false, false, false)]
        [InlineData("( a and b ) and not c", "a b and c not and", false, false, true, false)]
        [InlineData("( a and b ) and not c", "a b and c not and", false, true, false, false)]
        [InlineData("( a and b ) and not c", "a b and c not and", false, true, true, false)]
        [InlineData("( a and b ) and not c", "a b and c not and", true, false, false, false)]
        [InlineData("( a and b ) and not c", "a b and c not and", true, false, true, false)]
        [InlineData("( a and b ) and not c", "a b and c not and", true, true, false, true)]
        [InlineData("( a and b ) and not c", "a b and c not and", true, true, true, false)]

        [InlineData("( a or b ) and not c", "a b or c not and", false, false, false, false)]
        [InlineData("( a or b ) and not c", "a b or c not and", false, false, true, false)]
        [InlineData("( a or b ) and not c", "a b or c not and", false, true, false, true)]
        [InlineData("( a or b ) and not c", "a b or c not and", false, true, true, false)]
        [InlineData("( a or b ) and not c", "a b or c not and", true, false, false, true)]
        [InlineData("( a or b ) and not c", "a b or c not and", true, false, true, false)]
        [InlineData("( a or b ) and not c", "a b or c not and", true, true, false, true)]
        [InlineData("( a or b ) and not c", "a b or c not and", true, true, true, false)]

        [InlineData("a or ( b and not c )", "a b c not and or", false, false, false, false)]
        [InlineData("a or ( b and not c )", "a b c not and or", false, false, true, false)]
        [InlineData("a or ( b and not c )", "a b c not and or", false, true, false, true)]
        [InlineData("a or ( b and not c )", "a b c not and or", false, true, true, false)]
        [InlineData("a or ( b and not c )", "a b c not and or", true, false, false, true)]
        [InlineData("a or ( b and not c )", "a b c not and or", true, false, true, true)]
        [InlineData("a or ( b and not c )", "a b c not and or", true, true, false, true)]
        [InlineData("a or ( b and not c )", "a b c not and or", true, true, true, true)]

        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", false, false, false, false, false)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", false, false, false, true, false)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", false, false, true, false, false)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", false, false, true, true, false)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", false, true, false, false, false)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", false, true, false, true, true)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", false, true, true, false, true)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", false, true, true, true, true)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", true, false, false, false, false)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", true, false, false, true, true)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", true, false, true, false, true)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", true, false, true, true, true)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", true, true, false, false, false)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", true, true, false, true, true)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", true, true, true, false, true)]
        [InlineData("( a or b ) and ( c or d )", "a b or c d or and", true, true, true, true, true)]

        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", false, false, false, false, false)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", false, false, false, true, false)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", false, false, true, false, false)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", false, false, true, true, true)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", false, true, false, false, false)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", false, true, false, true, false)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", false, true, true, false, false)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", false, true, true, true, true)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", true, false, false, false, false)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", true, false, false, true, false)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", true, false, true, false, false)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", true, false, true, true, true)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", true, true, false, false, true)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", true, true, false, true, true)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", true, true, true, false, true)]
        [InlineData("( a and b ) or ( c and d )", "a b and c d and or", true, true, true, true, true)]

        [InlineData("a and b and c or d", "a b c and and d or", false, false, false, false, false)]
        [InlineData("a and b and c or d", "a b c and and d or", false, false, false, true, true)]
        [InlineData("a and b and c or d", "a b c and and d or", false, false, true, false, false)]
        [InlineData("a and b and c or d", "a b c and and d or", false, false, true, true, true)]
        [InlineData("a and b and c or d", "a b c and and d or", false, true, false, false, false)]
        [InlineData("a and b and c or d", "a b c and and d or", false, true, false, true, true)]
        [InlineData("a and b and c or d", "a b c and and d or", false, true, true, false, false)]
        [InlineData("a and b and c or d", "a b c and and d or", false, true, true, true, true)]
        [InlineData("a and b and c or d", "a b c and and d or", true, false, false, false, false)]
        [InlineData("a and b and c or d", "a b c and and d or", true, false, false, true, true)]
        [InlineData("a and b and c or d", "a b c and and d or", true, false, true, false, false)]
        [InlineData("a and b and c or d", "a b c and and d or", true, false, true, true, true)]
        [InlineData("a and b and c or d", "a b c and and d or", true, true, false, false, false)]
        [InlineData("a and b and c or d", "a b c and and d or", true, true, false, true, true)]
        [InlineData("a and b and c or d", "a b c and and d or", true, true, true, false, true)]
        [InlineData("a and b and c or d", "a b c and and d or", true, true, true, true, true)]
        public void TestEvaluateBooleanExpressions(string input, string postfixExpression, params bool[] values)
        {
            BooleanExpression exp = new();
            bool success = exp.TryParse(input);
            Assert.True(success);

            Assert.Equal(postfixExpression, exp.PostfixExpression);

            var operands = exp.Operands.ToList();
            // the last value is the expected result for the input values
            Assert.Equal(values.Length - 1, operands.Count);
            for (int i = 0; i < values.Length - 1; i++)
            {
                operands[i].EvaluatedResult = values[i];
            }

            EvaluationResult expected = values.Last() ? EvaluationResult.True : EvaluationResult.False;

            Assert.Equal(expected, exp.Evaluate());
        }

        [Theory]
        [InlineData("a and b", true, false, null)]
        [InlineData("a and b", true, null, false)]
        [InlineData("a or b", false, true, null)]
        [InlineData("a or b", false, null, true)]
        [InlineData("a and (b or c)", false, true, null, null)]
        [InlineData("a and (b or c)", false, true, false, null)]
        [InlineData("a and (b or c)", false, true, true, null)]
        [InlineData("a and (b or c)", true, false, null, null)]
        [InlineData("(a or b) and c", false, true, null, null)]
        [InlineData("(a or b) and c", false, true, true, null)]
        [InlineData("(a or b) and c", false, true, false, null)]
        public void TestShortCircuitResult(string input, bool expectedResult, params bool?[] values)
        {
            BooleanExpression exp = new();
            bool success = exp.TryParse(input);
            Assert.True(success);

            var operands = exp.Operands.ToList();
            for (int i = 0; i < values.Length; i++)
            {
                operands[i].EvaluatedResult = values[i];
            }

            var result = exp.IsShortCircuitFalse();
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("not a", true)]
        [InlineData("a nand b", true)]
        [InlineData("a nand (b or c)", true)]
        [InlineData("a nor (b or c)", true)]
        public void TestForNegativeExpression(string input, bool expectedResult)
        {
            BooleanExpression exp = new();
            bool success = exp.TryParse(input);
            Assert.True(success);

            Assert.Equal(expectedResult, exp.IsNegativeExpression());
        }

        [Theory]
        [InlineData("a (and) b", ParserErrorState.MissingOperand)]
        [InlineData("a (b and) c", ParserErrorState.MismatchedParentheses)]
        [InlineData("a or (b and) c", ParserErrorState.MismatchedParentheses)]
        [InlineData("a and b and", ParserErrorState.MissingOperand)]
        [InlineData("a not b", ParserErrorState.MissingOperator)]
        [InlineData("a and b not c", ParserErrorState.MissingOperator)]
        [InlineData("not a or b not c", ParserErrorState.MissingOperator)]
        [InlineData("a and or b", ParserErrorState.MissingOperand)]
        [InlineData("a and or not b", ParserErrorState.MissingOperand)]
        [InlineData("a and (b or c", ParserErrorState.MismatchedParentheses)]
        [InlineData("(a and b) or c)", ParserErrorState.MismatchedParentheses)]
        [InlineData("a and b) or c", ParserErrorState.MismatchedParentheses)]
        public void TestInvalidExpression(string input, ParserErrorState expectedResult)
        {
            BooleanExpression exp = new();
            bool success = exp.TryParse(input);
            Assert.False(success);

            Assert.Equal(expectedResult, exp.ParserState);
        }
    }
}
