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

        public IList<string> DefaultFileExtensions => ArchiveDirectory.DefaultExtensions;

        public bool IsSearchOnly => true;

        private FileFilter fileFilter;
        private GrepEngineInitParams searchParams;
        private List<string> includeSearchPatterns;
        //private List<string> excludeSearchPatterns;
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

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems)
        {
            // should not get here, replace is not allowed in an archive
            throw new NotImplementedException();
        }

        private static int fileOpenCount = 0;
        private static int extractorCount = 0;

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (file.Length > 260 && !file.StartsWith(@"\\?\", StringComparison.InvariantCulture))
            {
                file = @"\\?\" + file;
            }

            using (FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess))
            {
                //fileOpenCount++;
                //System.Diagnostics.Debug.WriteLine($"Opened {fileOpenCount} zip files.");
                return Search(fileStream, file, searchPattern, searchType, searchOptions, encoding);
            }
        }

        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            List<GrepSearchResult> results = new List<GrepSearchResult>();

            try
            {
                foreach (var item in SearchInsideArchive(input, fileName, searchPattern, searchType, searchOptions, encoding))
                {
                    results.Add(item);
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format(CultureInfo.CurrentCulture, "Failed to search inside archive '{0}'", fileName));
            }


            return results;
        }

        private IEnumerable<GrepSearchResult> SearchInsideArchive(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            //extractorCount++;
            //System.Diagnostics.Debug.WriteLine($"Extractor {extractorCount}.");
            using (SevenZipExtractor extractor = new SevenZipExtractor(input))
            {
                foreach (var fileInfo in extractor.ArchiveFileData)
                {
                    //FileData fileData = new FileData(fileName, fileInfo);

                    //var attr = (FileAttributes)fileInfo.Attributes;
                    string innerFileName = fileInfo.FileName;

                    int index = fileInfo.Index;
                    //if (innerFileName == "[no name]" && extractor.ArchiveFileData.Count == 1)
                    //{
                    //    index = 0;
                    //    innerFileName = Path.GetFileNameWithoutExtension(fileName);
                    //    ArchiveFileInfo temp = ArchiveDirectory.Copy(fileInfo);
                    //    temp.FileName = innerFileName;
                    //    fileData = new FileData(fileName, temp);
                    //}

                    //if (fileInfo.IsDirectory)
                    //{
                    //    if (!fileFilter.IncludeHidden && attr.HasFlag(FileAttributes.Hidden) && !hiddenDirectories.Contains(innerFileName))
                    //        hiddenDirectories.Add(innerFileName);

                    //    continue;
                    //}

                    //if (!fileFilter.IncludeHidden)
                    //{
                    //    string path = Path.GetDirectoryName(innerFileName);
                    //    if (hiddenDirectories.Contains(path))
                    //    {
                    //        continue;
                    //    }
                    //}

                    //if (!fileFilter.IncludeBinary)
                    //{
                    //    using (Stream stream = new MemoryStream())
                    //    {
                    //        extractor.ExtractFile(index, stream);
                    //        stream.Seek(0, SeekOrigin.Begin);

                    //        fileData.IsBinary = Utils.IsBinary(stream);
                    //    }
                    //}

                    if (includeSearchPatterns != null && includeSearchPatterns.Count > 0)
                    {
                        foreach (string pattern in includeSearchPatterns)
                        {
                            if (SafeDirectory.WildcardMatch(innerFileName, pattern, true))
                            {
                                //if (Utils.IncludeFile(innerFileName, fileFilter, fileData, true,
                                //    includeSearchPatterns, includeRegexPatterns, excludeRegexPatterns))
                                {
                                    foreach (var item in SearchInnerFile(extractor, index, innerFileName, searchPattern, searchType, searchOptions, encoding))
                                    {
                                        yield return item;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //if (Utils.IncludeFile(innerFileName, fileFilter, fileData, false,
                        //    includeSearchPatterns, includeRegexPatterns, excludeRegexPatterns))
                        {
                            foreach (var item in SearchInnerFile(extractor, index, innerFileName, searchPattern, searchType, searchOptions, encoding))
                            {
                                yield return item;

                            }
                        }
                    }

                    if (Utils.IsArchive(innerFileName))
                    {
                        using (Stream stream = new MemoryStream())
                        {
                            extractor.ExtractFile(index, stream);

                            foreach (var result in SearchInsideArchive(stream, fileName + ArchiveDirectory.ArchiveSeparator + innerFileName, searchPattern, searchType, searchOptions, encoding))
                            {
                                yield return result;
                            }
                        }
                    }

                    if (Utils.CancelSearch)
                        break;
                }
            }
        }

        private static int fileSearchCount = 0;
        private IEnumerable<GrepSearchResult> SearchInnerFile(SevenZipExtractor extractor, int index, string innerFileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            IGrepEngine engine = GrepEngineFactory.GetSearchEngine(innerFileName, searchParams, fileFilter, searchType);

            //fileSearchCount++;
            //System.Diagnostics.Debug.WriteLine($"File search {fileSearchCount}.");
            using (Stream stream = new MemoryStream())
            {
                extractor.ExtractFile(index, stream);
                stream.Seek(0, SeekOrigin.Begin);

                // Need to check the encoding of each file in the archive. If the encoding parameter is not default
                // then it is the user-specified code page.  If the encoding parameter *is* the default,
                // then it most likely not been set, so get the encoding of the extracted text file:
                if (encoding == Encoding.Default && !Utils.IsBinary(stream))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    encoding = Utils.GetFileEncoding(stream);
                }

                //var innerFileResults = engine.Search(stream, innerFileName, searchPattern, searchType, searchOptions, encoding);
                List<GrepSearchResult> innerFileResults = new List<GrepSearchResult>();
                if (innerFileResults.Count > 0)
                {
                    using (Stream readStream = new MemoryStream())
                    {
                        extractor.ExtractFile(index, readStream);
                        readStream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader streamReader = new StreamReader(readStream, encoding))
                        {
                            foreach (var result in innerFileResults)
                            {
                                if (Utils.CancelSearch)
                                    break;

                                if (!result.HasSearchResults)
                                    result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, engine.LinesBefore, engine.LinesAfter);
                            }
                        }
                    }

                    foreach (var result in innerFileResults)
                    {
                        yield return result;
                    }
                }
            }
        }

        //private List<GrepSearchResult> Search(IGrepEngine engine, string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        //{
        //    if (engine == null)
        //        throw new ArgumentNullException(nameof(engine));

        //    if (string.IsNullOrEmpty(file))
        //        throw new ArgumentNullException(nameof(file));

        //    string[] parts = file.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        //    if (!file.Contains(ArchiveDirectory.ArchiveSeparator) || parts.Length < 2)
        //    {
        //        return new List<GrepSearchResult>();
        //    }
        //    else
        //    {
        //        string diskFile = parts.First();
        //        string innerFileName = parts.Last();
        //        string[] intermediateFiles = parts.Skip(1).Take(parts.Length - 2).ToArray();

        //        if (diskFile.Length > 260 && !diskFile.StartsWith(@"\\?\", StringComparison.InvariantCulture))
        //        {
        //            diskFile = @"\\?\" + diskFile;
        //        }

        //        using (FileStream fileStream = File.Open(diskFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
        //        {
        //            return Search(engine, fileStream, file, diskFile, intermediateFiles, innerFileName, searchPattern, searchType, searchOptions, encoding);
        //        }
        //    }
        //}

        public static List<GrepSearchResult> Search(IGrepEngine engine, Stream input, string compositeFileName, string diskFile,
            string[] intermediateFiles, string innerFileName, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding)
        {
            List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

            try
            {
                using (SevenZipExtractor extractor = new SevenZipExtractor(input))
                {
                    if (intermediateFiles.Length > 0)
                    {
                        int index = -1;
                        if (extractor.ArchiveFileData.Count == 1)
                        {
                            index = 0;
                        }
                        else
                        {
                            string name = intermediateFiles.First();
                            var fd = extractor.ArchiveFileData.FirstOrDefault(f => string.Equals(f.FileName, name));
                            if (fd != null)
                                index = fd.Index;
                        }
                        if (index > -1)
                        {
                            using (Stream stream = new MemoryStream())
                            {
                                extractor.ExtractFile(index, stream);
                                string[] newIntermediateFiles = intermediateFiles.Skip(1).ToArray();

                                searchResults.AddRange(
                                    Search(engine, stream, compositeFileName, diskFile, newIntermediateFiles, innerFileName,
                                        searchPattern, searchType, searchOptions, encoding));
                            }
                        }
                    }
                    else
                    {
                        int index = -1;
                        var info = extractor.ArchiveFileData.FirstOrDefault(r => r.FileName == innerFileName);
                        if (info != null)
                        {
                            index = info.Index;
                        }
                        else if (extractor.ArchiveFileNames.Count == 1 && extractor.ArchiveFileNames[0] == "[no name]")
                        {
                            index = 0;
                        }

                        if (index > -1)
                        {
                            using (Stream stream = new MemoryStream())
                            {
                                extractor.ExtractFile(index, stream);
                                stream.Seek(0, SeekOrigin.Begin);

                                // Need to check the encoding of each file in the archive. If the encoding parameter is not default
                                // then it is the user-specified code page.  If the encoding parameter *is* the default,
                                // then it most likely not been set, so get the encoding of the extracted text file:
                                if (encoding == Encoding.Default && !Utils.IsBinary(stream))
                                {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    encoding = Utils.GetFileEncoding(stream);
                                }

                                var innerFileResults = engine.Search(stream, innerFileName, searchPattern, searchType, searchOptions, encoding);

                                if (innerFileResults.Count > 0)
                                {
                                    using (Stream readStream = new MemoryStream())
                                    {
                                        extractor.ExtractFile(index, readStream);
                                        readStream.Seek(0, SeekOrigin.Begin);
                                        using (StreamReader streamReader = new StreamReader(readStream, encoding))
                                        {
                                            foreach (var result in innerFileResults)
                                            {
                                                if (Utils.CancelSearch)
                                                    break;

                                                if (!result.HasSearchResults)
                                                    result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, engine.LinesBefore, engine.LinesAfter);
                                            }
                                        }
                                        searchResults.AddRange(innerFileResults);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (GrepSearchResult result in searchResults)
                {
                    result.InnerFileName = result.FileNameDisplayed;
                    result.FileNameDisplayed = compositeFileName;
                    result.FileNameReal = diskFile;
                    result.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format("Failed to search inside archive '{0}'", diskFile));
            }
            return searchResults;
        }

        public void Unload()
        {
        }
    }
}
