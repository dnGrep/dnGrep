using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using dnGREP.Common;
using NLog;
using SevenZip;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Engines
{
    public class ArchiveEngine : GrepEngineBase, IGrepEngine
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<DataEventArgs<string>> StartingFileSearch;

        public IList<string> DefaultFileExtensions => ArchiveDirectory.DefaultExtensions;

        public bool IsSearchOnly => true;

        private FileFilter fileFilter;
        private GrepEngineInitParams searchParams;
        private List<string> includeSearchPatterns;
        private List<Regex> includeRegexPatterns;
        private List<Regex> excludeRegexPatterns;
        private readonly HashSet<string> hiddenDirectories = new HashSet<string>();

        public void SetSearchOptions(FileFilter filter, GrepEngineInitParams initParams)
        {
            fileFilter = filter;
            searchParams = initParams;
            includeSearchPatterns = new List<string>();
            bool hasSearchPattern = Utils.PrepareSearchPatterns(filter, includeSearchPatterns);

            includeRegexPatterns = new List<Regex>();
            excludeRegexPatterns = new List<Regex>();
            Utils.PrepareFilters(filter, includeRegexPatterns, excludeRegexPatterns, hasSearchPattern);

            hiddenDirectories.Clear();
        }

        public Version FrameworkVersion => Assembly.GetAssembly(typeof(IGrepEngine)).GetName().Version;

        List<GrepSearchResult> IGrepEngine.Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            // not used, just here to implement interface
            return new List<GrepSearchResult>();
        }

        List<GrepSearchResult> IGrepEngine.Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            // not used, just here to implement interface
            return new List<GrepSearchResult>();
        }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems)
        {
            // should not get here, replace is not allowed in an archive
            throw new NotImplementedException();
        }

        public void Unload()
        {
        }

        public IEnumerable<List<GrepSearchResult>> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (file.Length > 260 && !file.StartsWith(@"\\?\", StringComparison.InvariantCulture))
            {
                file = @"\\?\" + file;
            }

            using (FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                foreach (var item in Search(fileStream, file, searchPattern, searchType, searchOptions, encoding))
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<List<GrepSearchResult>> Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            var enumerator = SearchInsideArchive(input, fileName, searchPattern, searchType,
                searchOptions, encoding).GetEnumerator();
            while (true)
            {
                List<GrepSearchResult> ret = null;
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                    ret = enumerator.Current;
                }
                catch (Exception ex)
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, "Failed to search inside archive '{0}'", fileName);
                    logger.Error(ex, msg);

                    FileData fileData = new FileData(fileName);
                    fileData.ErrorMsg = msg + ": " + ex.Message;
                    GrepSearchResult result = new GrepSearchResult(fileData, encoding);
                    ret = new List<GrepSearchResult> { result };
                }
                if (ret != null)
                {
                    yield return ret;
                }
            }
        }

        private IEnumerable<List<GrepSearchResult>> SearchInsideArchive(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            using (SevenZipExtractor extractor = new SevenZipExtractor(input, true))
            {
                if (extractor.IsSolid)
                {
                    System.Diagnostics.Debug.WriteLine("Solid: " + fileName + " is Solid");
                }

                foreach (var fileInfo in extractor.ArchiveFileData)
                {
                    FileData fileData = new FileData(fileName, fileInfo);

                    var attr = (FileAttributes)fileInfo.Attributes;
                    string innerFileName = fileInfo.FileName;

                    int index = fileInfo.Index;
                    if (innerFileName == "[no name]" && extractor.ArchiveFileData.Count == 1)
                    {
                        index = 0;
                        innerFileName = Path.GetFileNameWithoutExtension(fileName);
                        ArchiveFileInfo temp = ArchiveDirectory.Copy(fileInfo);
                        temp.FileName = innerFileName;
                        fileData = new FileData(fileName, temp);
                    }

                    if (fileInfo.IsDirectory)
                    {
                        if (!fileFilter.IncludeHidden && attr.HasFlag(FileAttributes.Hidden) && !hiddenDirectories.Contains(innerFileName))
                            hiddenDirectories.Add(innerFileName);

                        continue;
                    }

                    if (!fileFilter.IncludeHidden)
                    {
                        string path = Path.GetDirectoryName(innerFileName);
                        if (hiddenDirectories.Contains(path))
                        {
                            continue;
                        }
                    }


                    if (Utils.IsArchive(innerFileName))
                    {
                        using (Stream stream = new MemoryStream())
                        {
                            extractor.ExtractFile(index, stream);

                            var enumerator = SearchInsideArchive(stream, fileName + ArchiveDirectory.ArchiveSeparator + innerFileName,
                                searchPattern, searchType, searchOptions, encoding).GetEnumerator();

                            while (true)
                            {
                                List<GrepSearchResult> ret = null;
                                try
                                {
                                    if (!enumerator.MoveNext())
                                    {
                                        break;
                                    }
                                    ret = enumerator.Current;
                                }
                                catch (Exception ex)
                                {
                                    string msg = string.Format(CultureInfo.CurrentCulture, "Failed to search inside archive '{0}'", fileName + ArchiveDirectory.ArchiveSeparator + innerFileName);
                                    logger.Error(ex, msg);

                                    fileData.ErrorMsg = msg + ": " + ex.Message;
                                    GrepSearchResult result = new GrepSearchResult(fileData, encoding);
                                    ret = new List<GrepSearchResult> { result };
                                }
                                if (ret != null)
                                {
                                    yield return ret;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (includeSearchPatterns != null && includeSearchPatterns.Count > 0)
                        {
                            foreach (string pattern in includeSearchPatterns)
                            {
                                if (SafeDirectory.WildcardMatch(innerFileName, pattern, true))
                                {
                                    if (Utils.IncludeFile(innerFileName, fileFilter, fileData, true,
                                        includeSearchPatterns, includeRegexPatterns, excludeRegexPatterns))
                                    {
                                        yield return SearchInnerFile(extractor, index, fileData,
                                            fileName + ArchiveDirectory.ArchiveSeparator + innerFileName,
                                            searchPattern, searchType, searchOptions, encoding);
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (Utils.IncludeFile(innerFileName, fileFilter, fileData, false,
                                includeSearchPatterns, includeRegexPatterns, excludeRegexPatterns))
                            {
                                yield return SearchInnerFile(extractor, index, fileData,
                                    fileName + ArchiveDirectory.ArchiveSeparator + innerFileName,
                                    searchPattern, searchType, searchOptions, encoding);
                            }
                        }
                    }
                    if (Utils.CancelSearch)
                        break;
                }
            }
        }

        private List<GrepSearchResult> SearchInnerFile(SevenZipExtractor extractor, int index,
            FileData fileInfo, string innerFileName, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding)
        {
            List<GrepSearchResult> innerFileResults = new List<GrepSearchResult>();
            try
            {
                using (Stream stream = new MemoryStream((int)fileInfo.Length))
                {
                    extractor.ExtractFile(index, stream);

                    // the isBinary flag is needed for the Encoding check below
                    fileInfo.IsBinary = Utils.IsBinary(stream);
                    if (!fileFilter.IncludeBinary && fileInfo.IsBinary)
                    {
                        return innerFileResults;
                    }

                    // Need to check the encoding of each file in the archive. If the encoding parameter is not default
                    // then it is the user-specified code page.  If the encoding parameter *is* the default,
                    // then it most likely not been set, so get the encoding of the extracted text file:
                    if (encoding == Encoding.Default && !fileInfo.IsBinary)
                    {
                        encoding = Utils.GetFileEncoding(stream);
                    }

                    StartingFileSearch?.Invoke(this, new DataEventArgs<string>(innerFileName));

                    IGrepEngine engine = GrepEngineFactory.GetSearchEngine(innerFileName, searchParams, fileFilter, searchType);
                    innerFileResults = engine.Search(stream, innerFileName, searchPattern, searchType, searchOptions, encoding);

                    if (innerFileResults.Any())
                    {
                        //stream.Seek(0, SeekOrigin.Begin);
                        //using (StreamReader streamReader = new StreamReader(stream, encoding, false, 4096, true))
                        //{
                        foreach (var result in innerFileResults)
                        {
                            // file info is known, set it now
                            result.FileInfo = fileInfo;

                            if (Utils.CancelSearch)
                                break;

                            //    // pre-cache the search results since the text is available
                            //    // this could create too much memory use, but will save reopening the archive
                            //    result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, engine.LinesBefore, engine.LinesAfter);
                        }
                        //}
                    }
                    GrepEngineFactory.ReturnToPool(innerFileName, engine);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format(CultureInfo.CurrentCulture, "Failed to search inside archive '{0}'", innerFileName));
            }

            return innerFileResults;
        }
    }
}
