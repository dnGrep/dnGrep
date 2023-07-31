using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using dnGREP.Common;

namespace dnGREP.Engines
{
    public class GrepEnginePlainText : GrepEngineBase, IGrepEngine
    {
        public GrepEnginePlainText() : base() { }

        public IList<string> DefaultFileExtensions
        {
            get { return Array.Empty<string>(); }
        }

        public bool IsSearchOnly
        {
            get { return false; }
        }

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken = default)
        {
            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            return Search(fileStream, file, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);
        }

        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken = default)
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
                return SearchMultiline(input, fileName, searchPattern, searchOptions, searchMethod, encoding, pauseCancelToken);
            else
                return Search(input, fileName, searchPattern, searchOptions, searchMethod, encoding, pauseCancelToken);
        }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken = default)
        {
            using FileStream readStream = File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using FileStream writeStream = File.OpenWrite(destinationFile);
            return Replace(readStream, writeStream, searchPattern, replacePattern, searchType, searchOptions, encoding, replaceItems, pauseCancelToken);
        }

        public bool Replace(Stream readStream, Stream writeStream, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken = default)
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
                return ReplaceMultiline(readStream, writeStream, searchPattern, replacePattern, searchOptions, replaceMethod, encoding, replaceItems, pauseCancelToken);
            else
                return Replace(readStream, writeStream, searchPattern, replacePattern, searchOptions, replaceMethod, encoding, replaceItems, pauseCancelToken);
        }

        public void Unload()
        {
            // Do nothing
        }

        public Version? FrameworkVersion => Assembly.GetAssembly(typeof(IGrepEngine))?.GetName()?.Version;

        #region Actual Implementation

        private static List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern,

            GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod, Encoding encoding,
            PauseCancelToken pauseCancelToken)
        {
            List<GrepSearchResult> searchResults = new();

            using (StreamReader baseReader = new(input, encoding, false, 4096, true))
            {
                using EolReader readStream = new(baseReader);
                string? line = null;
                int lineNumber = 1;
                int filePosition = 0;
                List<GrepMatch> matches = new();
                while (!readStream.EndOfStream)
                {
                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                    line = readStream.ReadLine();
                    if (line == null)
                    {
                        continue;  // ? or break;
                    }

                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                    List<GrepMatch> results = searchMethod(lineNumber, filePosition, line, searchPattern, searchOptions, false, pauseCancelToken);
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
            return searchResults;
        }

        private static List<GrepSearchResult> SearchMultiline(Stream input, string fileName, string searchPattern,
            GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            List<GrepSearchResult> searchResults = new();

            using (StreamReader readStream = new(input, encoding, false, 4096, true))
            {
                string fileBody = readStream.ReadToEnd();
                var matches = searchMethod(-1, 0, fileBody, searchPattern, searchOptions, true, pauseCancelToken);
                if (matches.Count > 0)
                {
                    searchResults.Add(new GrepSearchResult(fileName, searchPattern, matches, encoding));
                }
            }

            return searchResults;
        }

        private static bool Replace(Stream inputStream, Stream outputStream, string searchPattern, string replacePattern, GrepSearchOption searchOptions,
            SearchDelegates.DoReplace replaceMethod, Encoding encoding, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken)
        {
            using StreamReader readStream = new(inputStream, encoding, false, 4096, true);
            bool hasUtf8bom = encoding == Encoding.UTF8 && Utils.HasUtf8ByteOrderMark(inputStream);
            var outputEncoding = encoding;
            if (hasUtf8bom)
            {
                outputEncoding = new UTF8Encoding(true);
            }

            StreamWriter writeStream = new(outputStream, outputEncoding, -1, true);

            string? line = null;
            int lineNumber = 1;
            int filePosition = 0;

            // read with eol character(s);
            using (EolReader eolReader = new(readStream))
            {
                while (!eolReader.EndOfStream)
                {
                    line = eolReader.ReadLine();
                    if (line == null)
                    {
                        continue; // ? or break;
                    }

                    if (lineNumber == 1 && hasUtf8bom)
                        line = line.Replace("\ufeff", "", StringComparison.Ordinal); // remove BOM
                    int lineLength = line.Length;

                    line = replaceMethod(lineNumber, filePosition, line, searchPattern, replacePattern, searchOptions, replaceItems, pauseCancelToken);
                    writeStream.Write(line);  // keep original eol

                    lineNumber++;
                    filePosition += lineLength;
                }
            }

            writeStream.Flush();
            writeStream.Dispose();

            return true;
        }

        private static bool ReplaceMultiline(Stream inputStream, Stream outputStream, string searchPattern, string replacePattern, GrepSearchOption searchOptions,
            SearchDelegates.DoReplace replaceMethod, Encoding encoding, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken)
        {
            using StreamReader readStream = new(inputStream, encoding, false, 4096, true);
            StreamWriter writeStream = new(outputStream, encoding);

            string fileBody = readStream.ReadToEnd();

            fileBody = replaceMethod(-1, 0, fileBody, searchPattern, replacePattern, searchOptions, replaceItems, pauseCancelToken);
            writeStream.Write(fileBody);

            writeStream.Flush();

            return true;
        }

        #endregion
    }
}
