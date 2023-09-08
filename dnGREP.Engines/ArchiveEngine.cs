using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using dnGREP.Common;
using dnGREP.Common.IO;
using NLog;
using SevenZip;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Engines
{
    public class ArchiveEngine : GrepEngineBase, IGrepEngine
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<DataEventArgs<string>>? StartingFileSearch;

        public IList<string> DefaultFileExtensions => ArchiveDirectory.DefaultExtensions;

        public bool IsSearchOnly => true;

        private bool precacheResults;
        private FileFilter fileFilter = FileFilter.Default;
        private GrepEngineInitParams searchParams = GrepEngineInitParams.Default;
        private readonly List<string> includeSearchPatterns = new();
        private readonly List<Regex> includeRegexPatterns = new();
        private readonly List<Regex> excludeRegexPatterns = new();
        private readonly List<Regex> includeShebangPatterns = new();
        private readonly HashSet<string> hiddenDirectories = new();

        public void SetSearchOptions(FileFilter filter, GrepEngineInitParams initParams)
        {
            precacheResults = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ExpandResults);
            fileFilter = filter;
            searchParams = initParams;
            includeSearchPatterns.Clear();
            includeRegexPatterns.Clear();
            excludeRegexPatterns.Clear();
            includeShebangPatterns.Clear();

            bool hasSearchPattern = Utils.PrepareSearchPatterns(filter, includeSearchPatterns);
            Utils.PrepareFilters(filter, includeRegexPatterns, excludeRegexPatterns, includeShebangPatterns, hasSearchPattern);

            hiddenDirectories.Clear();
        }

        public Version? FrameworkVersion => Assembly.GetAssembly(typeof(IGrepEngine))?.GetName()?.Version;

        List<GrepSearchResult> IGrepEngine.Search(string file, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            // not used, just here to implement interface
            return new List<GrepSearchResult>();
        }

        List<GrepSearchResult> IGrepEngine.Search(Stream input, string fileName, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            // not used, just here to implement interface
            return new List<GrepSearchResult>();
        }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems,
            PauseCancelToken pauseCancelToken)
        {
            // should not get here, replace is not allowed in an archive
            throw new NotImplementedException();
        }

        public void Unload()
        {
        }

        public IEnumerable<List<GrepSearchResult>> Search(string file, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding,
            PauseCancelToken pauseCancelToken)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (file.Length > 260 && !file.StartsWith(PathEx.LongPathPrefix, StringComparison.Ordinal))
            {
                file = PathEx.GetLongPath(file);
            }

            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            foreach (var item in Search(fileStream, file, searchPattern, searchType, searchOptions, encoding, pauseCancelToken))
            {
                yield return item;
            }
        }

        public IEnumerable<List<GrepSearchResult>> Search(Stream input, string fileName,
            string searchPattern, SearchType searchType, GrepSearchOption searchOptions,
            Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            var enumerator = SearchInsideArchive(input, fileName, searchPattern, searchType,
                searchOptions, encoding, pauseCancelToken).GetEnumerator();
            while (true)
            {
                List<GrepSearchResult> ret;
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                    ret = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    // expected for stop after first match or user cancel
                    yield break;
                }
                catch (Exception ex)
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedToSearchInsideArchive0, fileName);
                    logger.Error(ex, msg);

                    FileData fileData = new(fileName)
                    {
                        ErrorMsg = msg + ": " + ex.Message
                    };
                    ret = new List<GrepSearchResult> { new GrepSearchResult(fileData, encoding) };
                }
                if (ret != null)
                {
                    yield return ret;
                }
            }
        }

        private IEnumerable<List<GrepSearchResult>> SearchInsideArchive(Stream input, string fileName,
            string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding,
            PauseCancelToken pauseCancelToken)
        {
            using SevenZipExtractor extractor = new(input, true);
            foreach (var fileInfo in extractor.ArchiveFileData)
            {
                FileData fileData = new(fileName, fileInfo);

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
                    if (!fileFilter.IncludeHidden && attr.HasFlag(FileAttributes.Hidden) &&
                        !hiddenDirectories.Contains(innerFileName))
                    {
                        hiddenDirectories.Add(innerFileName + Path.DirectorySeparatorChar);
                    }

                    continue;
                }

                if (!fileFilter.IncludeHidden)
                {
                    if (attr.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }

                    bool excludeFile = false;
                    foreach (string dir in hiddenDirectories)
                    {
                        if (innerFileName.StartsWith(dir, StringComparison.Ordinal))
                        {
                            excludeFile = true;
                            break;
                        }
                    }

                    if (excludeFile)
                    {
                        continue;
                    }
                }

                if (Utils.IsArchive(innerFileName))
                {
                    using Stream stream = new MemoryStream(4096);
                    extractor.ExtractFile(index, stream);

                    var enumerator = SearchInsideArchive(stream, fileName + ArchiveDirectory.ArchiveSeparator + innerFileName,
                        searchPattern, searchType, searchOptions, encoding, pauseCancelToken).GetEnumerator();

                    while (true)
                    {
                        List<GrepSearchResult>? ret = null;
                        try
                        {
                            if (!enumerator.MoveNext())
                            {
                                break;
                            }
                            ret = enumerator.Current;
                        }
                        catch (OperationCanceledException)
                        {
                            // expected for stop after first match or user cancel
                            yield break;
                        }
                        catch (Exception ex)
                        {
                            string msg = string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedToSearchInsideArchive0, fileName + ArchiveDirectory.ArchiveSeparator + innerFileName);
                            logger.Error(ex, msg);

                            fileData.ErrorMsg = msg + ": " + ex.Message;
                            ret = new List<GrepSearchResult> { new GrepSearchResult(fileData, encoding) };
                        }
                        if (ret != null)
                        {
                            yield return ret;
                        }
                    }
                }
                else
                {
                    if (ArchiveDirectory.IncludeFile(innerFileName,
                        fileName + ArchiveDirectory.ArchiveSeparator + innerFileName,
                        fileFilter, fileData, includeSearchPatterns,
                        includeRegexPatterns, excludeRegexPatterns))
                    {
                        var res = SearchInnerFile(extractor, index, fileFilter, fileData,
                            fileName + ArchiveDirectory.ArchiveSeparator + innerFileName,
                            searchPattern, searchType, searchOptions, encoding, pauseCancelToken);

                        if (res != null)
                        {
                            yield return res;
                        }
                    }
                }
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
            }
        }

        private List<GrepSearchResult>? SearchInnerFile(SevenZipExtractor extractor, int index,
            FileFilter fileFilter, FileData fileData, string innerFileName, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding,
            PauseCancelToken pauseCancelToken)
        {
            List<GrepSearchResult>? innerFileResults = null;
            try
            {
                using Stream stream = new MemoryStream(4096);
                extractor.ExtractFile(index, stream);
                stream.Seek(0, SeekOrigin.Begin);

                if (!ArchiveDirectory.IncludeFileStream(stream,
                    fileFilter, fileData, true, includeShebangPatterns))
                {
                    return innerFileResults;
                }

                // The IncludeFileStream method determined the encoding of the file in the archive.
                // If the encoding parameter is not default then it is the user-specified code page.
                // If the encoding parameter *is* the default, then it most likely not been set, so
                // use the encoding of the extracted text file
                if (encoding == Encoding.Default && !fileData.IsBinary)
                {
                    encoding = fileData.Encoding;
                }

                StartingFileSearch?.Invoke(this, new DataEventArgs<string>(innerFileName));

                IGrepEngine engine = GrepEngineFactory.GetSearchEngine(innerFileName, searchParams, fileFilter, searchType);
                innerFileResults = engine.Search(stream, innerFileName, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);

                if (innerFileResults.Any())
                {
                    if (precacheResults)
                    {
                        // pre-cache the search results since the text is available.
                        // user has set the option to auto-expand the results tree, so all the
                        // search results data will be needed, and this will save reopening the
                        // archive for each file
                        stream.Seek(0, SeekOrigin.Begin);
                        using StreamReader streamReader = new(stream, encoding, false, 4096, true);
                        foreach (var result in innerFileResults)
                        {
                            // save the temp file if set by the search engine
                            string tempFile = result.FileInfo.TempFile;

                            // file info is known, set it now
                            result.FileInfo = new(fileData)
                            {
                                TempFile = tempFile
                            };

                            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                            result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, engine.LinesBefore, engine.LinesAfter);
                        }
                    }
                    else
                    {
                        foreach (var result in innerFileResults)
                        {
                            // save the temp file if set by the search engine
                            string tempFile = result.FileInfo.TempFile;

                            // file info is known, set it now
                            result.FileInfo = new(fileData)
                            {
                                TempFile = tempFile
                            };

                            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
                        }
                    }
                }
                else
                {
                    // short circuit this file
                    innerFileResults = null;
                }
                GrepEngineFactory.ReturnToPool(innerFileName, engine);
            }
            catch (OperationCanceledException)
            {
                // expected for stop after first match or user cancel
                innerFileResults = null;
            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedToSearchInsideArchive0, innerFileName));
            }

            return innerFileResults;
        }
    }
}
