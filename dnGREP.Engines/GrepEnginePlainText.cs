using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using dnGREP.Common;
using NLog;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Engines
{
    public class GrepEnginePlainText : GrepEngineBase, IGrepEngine
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public GrepEnginePlainText() : base() { }

        public bool IsSearchOnly
        {
            get { return false; }
        }

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            using (FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan))
            {
                return Search(fileStream, file, searchPattern, searchType, searchOptions, encoding);
            }
        }

        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            SearchDelegates.DoSearch searchMethod = DoTextSearch;
            switch (searchType)
            {
                case SearchType.PlainText:
                    searchMethod = DoTextSearch;
                    break;
                case SearchType.Regex:
                    searchMethod = DoRegexSearch;
                    break;
                case SearchType.XPath:
                    searchMethod = DoXPathSearch;
                    break;
                case SearchType.Soundex:
                    searchMethod = DoFuzzySearch;
                    break;
            }

            if (searchOptions.HasFlag(GrepSearchOption.Multiline) || searchType == SearchType.XPath)
                return SearchMultiline(input, fileName, searchPattern, searchOptions, searchMethod, encoding);
            else
                return Search(input, fileName, searchPattern, searchOptions, searchMethod, encoding);
        }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems)
        {
            using (FileStream readStream = File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream writeStream = File.OpenWrite(destinationFile))
            {
                return Replace(readStream, writeStream, searchPattern, replacePattern, searchType, searchOptions, encoding, replaceItems);
            }
        }

        public bool Replace(Stream readStream, Stream writeStream, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems)
        {
            SearchDelegates.DoReplace replaceMethod = DoTextReplace;
            switch (searchType)
            {
                case SearchType.PlainText:
                    replaceMethod = DoTextReplace;
                    break;
                case SearchType.Regex:
                    replaceMethod = DoRegexReplace;
                    break;
                case SearchType.XPath:
                    replaceMethod = DoXPathReplace;
                    break;
                case SearchType.Soundex:
                    replaceMethod = DoFuzzyReplace;
                    break;
            }

            if (searchOptions.HasFlag(GrepSearchOption.Multiline) || searchType == SearchType.XPath)
                return ReplaceMultiline(readStream, writeStream, searchPattern, replacePattern, searchOptions, replaceMethod, encoding, replaceItems);
            else
                return Replace(readStream, writeStream, searchPattern, replacePattern, searchOptions, replaceMethod, encoding, replaceItems);
        }

        public void Unload()
        {
            // Do nothing
        }

        public Version FrameworkVersion
        {
            get
            {
                return Assembly.GetAssembly(typeof(IGrepEngine)).GetName().Version;
            }
        }

        #region Actual Implementation

        private List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod, Encoding encoding)
        {
            List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

            using (StreamReader baseReader = new StreamReader(input, encoding))
            {
                using (EolReader readStream = new EolReader(baseReader))
                {
                    string line = null;
                    int lineNumber = 1;
                    int filePosition = 0;
                    List<GrepMatch> matches = new List<GrepMatch>();
                    while (!readStream.EndOfStream)
                    {
                        line = readStream.ReadLine();

                        if (Utils.CancelSearch)
                        {
                            return searchResults;
                        }
                        List<GrepMatch> results = searchMethod(lineNumber, filePosition, line, searchPattern, searchOptions, false);
                        if (results.Count > 0)
                        {
                            foreach (GrepMatch m in results)
                            {
                                //matches.Add(new GrepMatch(lineNumber, m.StartLocation + filePosition, m.Length));
                                matches.Add(m);
                            }
                        }
                        filePosition += line.Length;
                        lineNumber++;
                    }
                    if (matches.Count > 0)
                    {
                        searchResults.Add(new GrepSearchResult(fileName, searchPattern, matches, encoding));
                    }
                }
            }
            return searchResults;
        }

        private List<GrepSearchResult> SearchMultiline(Stream input, string fileName, string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod, Encoding encoding)
        {
            List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

            using (StreamReader readStream = new StreamReader(input, encoding))
            {
                string fileBody = readStream.ReadToEnd();
                var matches = searchMethod(-1, 0, fileBody, searchPattern, searchOptions, true);
                if (matches.Count > 0)
                {
                    searchResults.Add(new GrepSearchResult(fileName, searchPattern, matches, encoding));
                }
            }

            return searchResults;
        }

        private bool Replace(Stream inputStream, Stream outputStream, string searchPattern, string replacePattern, GrepSearchOption searchOptions,
            SearchDelegates.DoReplace replaceMethod, Encoding encoding, IEnumerable<GrepMatch> replaceItems)
        {
            using (StreamReader readStream = new StreamReader(inputStream, encoding))
            {
                bool hasUtf8bom = encoding == Encoding.UTF8 && Utils.HasUtf8ByteOrderMark(inputStream);
                var outputEncoding = encoding;
                if (hasUtf8bom)
                {
                    outputEncoding = new UTF8Encoding(true);
                }

                StreamWriter writeStream = new StreamWriter(outputStream, outputEncoding);

                string line = null;
                int lineNumber = 1;
                int filePosition = 0;

                // read with eol character(s);
                using (EolReader eolReader = new EolReader(readStream))
                {
                    while (!eolReader.EndOfStream)
                    {
                        line = eolReader.ReadLine();
                        if (lineNumber == 1 && hasUtf8bom)
                            line = line.Replace("\ufeff", ""); // remove BOM
                        int lineLength = line.Length;

                        line = replaceMethod(lineNumber, filePosition, line, searchPattern, replacePattern, searchOptions, replaceItems);
                        writeStream.Write(line);  // keep original eol

                        lineNumber++;
                        filePosition += lineLength;
                    }
                }

                writeStream.Flush();
            }

            return true;
        }

        private bool ReplaceMultiline(Stream inputStream, Stream outputStream, string searchPattern, string replacePattern, GrepSearchOption searchOptions,
            SearchDelegates.DoReplace replaceMethod, Encoding encoding, IEnumerable<GrepMatch> replaceItems)
        {
            using (StreamReader readStream = new StreamReader(inputStream, encoding))
            {
                StreamWriter writeStream = new StreamWriter(outputStream, encoding);

                string fileBody = readStream.ReadToEnd();

                fileBody = replaceMethod(-1, 0, fileBody, searchPattern, replacePattern, searchOptions, replaceItems);
                writeStream.Write(fileBody);

                writeStream.Flush();
            }

            return true;
        }

        #endregion
    }
}
