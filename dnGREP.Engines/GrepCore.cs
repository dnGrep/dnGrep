using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using dnGREP.Common.IO;
using dnGREP.Engines;
using NLog;

namespace dnGREP.Common
{
    public class GrepCore
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public delegate void SearchProgressHandler(object sender, ProgressStatus files);
        public event SearchProgressHandler? ProcessedFile;

        public GrepCore()
        {
        }

        public GrepEngineInitParams SearchParams { get; set; } = GrepEngineInitParams.Default;
        public FileFilter FileFilter { get; set; } = FileFilter.Default;
        public int SearchAutoStopCount { get; set; } = 1;
        public int SearchAutoPauseCount { get; set; } = 5;


        private readonly List<GrepSearchResult> searchResults = [];
        private readonly object lockObj = new();
        private int processedFilesCount;
        private int foundFilesCount;
        private bool autoPauseExecuted;

        public static TimeSpan MatchTimeout { get; private set; } = TimeSpan.FromSeconds(4);

        public static void Initialize()
        {
            double seconds = GrepSettings.Instance.Get<double>(GrepSettings.Key.MatchTimeout);
            if (seconds > 0)
            {
                MatchTimeout = TimeSpan.FromSeconds(seconds);
            }
        }


        public List<GrepSearchResult> ListFiles(IEnumerable<FileData> files, GrepSearchOption searchOptions,
            int codePage, PauseCancelToken pauseCancelToken = default)
        {
            searchResults.Clear();

            if (files == null)
                return searchResults;

            int successful = 0;
            processedFilesCount = 0;
            foreach (FileData fileInfo in files)
            {
                ProcessedFile?.Invoke(this, new ProgressStatus(true, searchResults.Count, successful, null, fileInfo.FullName));

                Encoding encoding = Encoding.Default;
                if (codePage > -1)
                    encoding = Encoding.GetEncoding(codePage);

                try
                {
                    if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.DetectEncodingForFileNamePattern))
                    {
                        if (codePage == -1 && !fileInfo.FullName.Contains(ArchiveDirectory.ArchiveSeparator, StringComparison.Ordinal) &&
                            !Utils.IsArchive(fileInfo.FullName) && !Utils.IsPluginFile(fileInfo.FullName) &&
                            !Utils.IsBinary(fileInfo.FullName))
                        {
                            encoding = Utils.GetFileEncoding(fileInfo.FullName);
                        }
                    }

                    GrepSearchResult grepSearchResult = new(fileInfo, encoding);
                    searchResults.Add(grepSearchResult);
                    successful++;
                    Interlocked.Increment(ref processedFilesCount);

                    if (searchOptions.HasFlag(GrepSearchOption.StopAfterNumMatches) && successful >= SearchAutoStopCount)
                    {
                        pauseCancelToken.Cancel();
                    }
                    if (searchOptions.HasFlag(GrepSearchOption.PauseAfterNumMatches) && successful >= SearchAutoPauseCount && !autoPauseExecuted)
                    {
                        autoPauseExecuted = true;
                        pauseCancelToken.Pause();
                    }

                    ProcessedFile?.Invoke(this, new ProgressStatus(false, processedFilesCount, successful, [grepSearchResult], fileInfo.FullName));

                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    // expected for stop after first match or user cancel
                    break;
                }
                catch (Exception ex)
                {
                    // will catch file not found errors (Everything search):
                    logger.Error(ex, "Failed in ListFiles");
                    fileInfo.ErrorMsg = ex.Message;
                    AddSearchResult(new GrepSearchResult(fileInfo, encoding));
                }
            }

            return [.. searchResults];
        }

        /// <summary>
        /// Searches folder for files whose content matches regex
        /// </summary>
        /// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
        /// <param name="searchRegex">Regex pattern</param>
        /// <returns>List of results. If nothing is found returns empty list</returns>
        public List<GrepSearchResult> Search(IEnumerable<string>? files, SearchType searchType,
            string searchPattern, GrepSearchOption searchOptions, int codePage,
            PauseCancelToken pauseCancelToken = default)
        {
            searchResults.Clear();

            if (files == null)
                return searchResults;

            if (string.IsNullOrEmpty(searchPattern))
                return searchResults;

            Initialize();

            processedFilesCount = 0;
            foundFilesCount = 0;

            int maxParallel = GrepSettings.Instance.Get<int>(GrepSettings.Key.MaxDegreeOfParallelism);
            int counter = 0, highWater = 0;

            try
            {
                if (SearchParams.SearchParallel)
                {
                    ParallelOptions po = new()
                    {
                        MaxDegreeOfParallelism = maxParallel == -1 ? -1 : Math.Max(1, maxParallel),
                        CancellationToken = pauseCancelToken.CancellationToken
                    };
                    Parallel.ForEach(files, po, f => Search(f, searchType, searchPattern, searchOptions,
                        codePage, ref counter, ref highWater, pauseCancelToken));
                }
                else
                {
                    foreach (var file in files)
                    {
                        Search(file, searchType, searchPattern, searchOptions,
                            codePage, ref counter, ref highWater, pauseCancelToken);

                        pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
                    }
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle(ex =>
                {
                    if (ex is OperationCanceledException)
                    {
                        // expected for stop after first match or user cancel
                        return true;
                    }
                    else
                    {
                        logger.Error(ex, "Failed in search in files");
                        return true;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // expected for stop after first match or user cancel
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed in search in files");
            }
            finally
            {
                logger.Info($"Maximum concurrent tasks used in search: {highWater}");

                GrepEngineFactory.UnloadEngines();
            }

            return [.. searchResults];
        }

        public List<GrepSearchResult> CaptureGroupSearch(IEnumerable<string> files, string filePatternInclude,
            GrepSearchOption searchOptions, SearchType searchType, string searchPattern, int codePage,
            PauseCancelToken pauseCancelToken = default)
        {
            searchResults.Clear();

            if (files == null || !files.Any())
                return searchResults;

            Initialize();

            try
            {
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string modSearchPattern = Regex.Replace(fileName, filePatternInclude, searchPattern,
                        RegexOptions.IgnoreCase, TimeSpan.FromSeconds(4.0));

                    if (string.IsNullOrEmpty(modSearchPattern))
                    {
                        continue;
                    }
                    else if (searchType == SearchType.Regex && !Utils.ValidateRegex(modSearchPattern))
                    {
                        logger.Error($"Capture group search pattern is not a valid regular expression: '{modSearchPattern}'");
                        continue;
                    }

                    int counter = 0, highWater = 0;
                    Search(filePath, searchType, modSearchPattern, searchOptions, codePage,
                        ref counter, ref highWater, pauseCancelToken);

                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                // expected for stop after first match or user cancel
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed in capture group search");
            }
            finally
            {
                GrepEngineFactory.UnloadEngines();
            }
            return [.. searchResults];
        }

        private void AddSearchResult(GrepSearchResult result)
        {
            lock (lockObj)
            {
                searchResults.Add(result);
            }
        }

        private void AddSearchResults(IEnumerable<GrepSearchResult> results)
        {
            lock (lockObj)
            {
                if (results.Any())
                {
                    searchResults.AddRange(results);
                }
            }
        }

        private void Search(string file, SearchType searchType, string searchPattern, GrepSearchOption searchOptions, int codePage,
            ref int counter, ref int highWater, PauseCancelToken pauseCancelToken = default)
        {
            try
            {
                InterlockedMax(ref highWater, Interlocked.Increment(ref counter));

                ProcessedFile?.Invoke(this, new ProgressStatus(true, processedFilesCount, foundFilesCount, null, file));

                bool isArchive = Utils.IsArchive(file);

                Encoding encoding = Encoding.Default;
                if (codePage > -1)
                    encoding = Encoding.GetEncoding(codePage);
                else if (!isArchive && !Utils.IsPluginFile(file) && !Utils.IsBinary(file))
                    encoding = Utils.GetFileEncoding(file);

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                IGrepEngine engine = GrepEngineFactory.GetSearchEngine(file, SearchParams, FileFilter, searchType);

                if (isArchive && engine is ArchiveEngine archiveEngine)
                {
                    archiveEngine.SetSearchOptions(FileFilter, SearchParams);
                    archiveEngine.StartingFileSearch += ArchiveEngine_StartingFileSearch;

                    foreach (var fileSearchResults in archiveEngine.Search(file, searchPattern, searchType,
                        searchOptions, encoding, pauseCancelToken))
                    {
                        if (fileSearchResults.Count > 0)
                        {
                            AddSearchResults(fileSearchResults);
                        }
                        int hits = fileSearchResults.Where(r => r.IsSuccess).Count();
                        Interlocked.Add(ref foundFilesCount, hits);

                        if (searchOptions.HasFlag(GrepSearchOption.StopAfterNumMatches) && foundFilesCount >= SearchAutoStopCount)
                        {
                            pauseCancelToken.Cancel();
                        }
                        if (searchOptions.HasFlag(GrepSearchOption.PauseAfterNumMatches) && foundFilesCount >= SearchAutoPauseCount && !autoPauseExecuted)
                        {
                            autoPauseExecuted = true;
                            pauseCancelToken.Pause();
                        }

                        ProcessedFile?.Invoke(this, new ProgressStatus(false, processedFilesCount, foundFilesCount, fileSearchResults, file));
                    }
                    archiveEngine.StartingFileSearch -= ArchiveEngine_StartingFileSearch;
                }
                else
                {
                    Interlocked.Increment(ref processedFilesCount);

                    var fileSearchResults = engine.Search(file, searchPattern, searchType, searchOptions, encoding, pauseCancelToken).ToList();

                    if (fileSearchResults.Count > 0)
                    {
                        AddSearchResults(fileSearchResults);
                    }
                    int hits = fileSearchResults.Where(r => r.IsSuccess).Count();
                    Interlocked.Add(ref foundFilesCount, hits);

                    if (searchOptions.HasFlag(GrepSearchOption.StopAfterNumMatches) && foundFilesCount >= SearchAutoStopCount)
                    {
                        pauseCancelToken.Cancel();
                    }
                    if (searchOptions.HasFlag(GrepSearchOption.PauseAfterNumMatches) && foundFilesCount >= SearchAutoPauseCount && !autoPauseExecuted)
                    {
                        autoPauseExecuted = true;
                        pauseCancelToken.Pause();
                    }
                    ProcessedFile?.Invoke(this, new ProgressStatus(false, processedFilesCount, foundFilesCount, fileSearchResults, file));
                }

                GrepEngineFactory.ReturnToPool(file, engine);
            }
            catch (OperationCanceledException)
            {
                // expected for stop after first match or user cancel
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error searching file: '{0}'", file);
                AddSearchResult(new GrepSearchResult(file, searchPattern, ex.Message, false));
                if (ProcessedFile != null)
                {
                    List<GrepSearchResult> _results =
                    [
                        new GrepSearchResult(file, searchPattern, ex.Message, false)
                    ];
                    ProcessedFile?.Invoke(this, new ProgressStatus(false, processedFilesCount, foundFilesCount, _results, file));
                }
            }
            finally
            {
                Interlocked.Decrement(ref counter);
            }
        }

        // from Raymond Chen:
        // https://devblogs.microsoft.com/oldnewthing/20140516-00/?p=973
        // https://devblogs.microsoft.com/oldnewthing/20040915-00/?p=37863
        private static int InterlockedMax(ref int location, int value)
        {
            int initialValue, newValue;
            do
            {
                initialValue = location;
                newValue = Math.Max(initialValue, value);
            }
            while (Interlocked.CompareExchange(ref location, newValue,
                                               initialValue) != initialValue);
            return initialValue;
        }

        private void ArchiveEngine_StartingFileSearch(object? sender, DataEventArgs<string> e)
        {
            Interlocked.Increment(ref processedFilesCount);
            ProcessedFile?.Invoke(this, new ProgressStatus(true, processedFilesCount, foundFilesCount, null, e.Data));
        }

        public int Replace(IEnumerable<ReplaceDef> files, SearchType searchType, string searchPattern,
            string replacePattern, GrepSearchOption searchOptions, int codePage, PauseCancelToken pauseCancelToken = default)
        {
            string undoFolder = Utils.GetUndoFolder();

            if (files == null || !files.Any() || !Directory.Exists(undoFolder))
                return 0;

            GrepEngineBase.ResetGuidxCache();

            replacePattern = Utils.ReplaceSpecialCharacters(replacePattern);

            bool restoreLastModifiedDate = GrepSettings.Instance.Get<bool>(GrepSettings.Key.RestoreLastModifiedDate);

            int processedFiles = 0;

            try
            {
                foreach (var item in files)
                {
                    ProcessedFile?.Invoke(this, new ProgressStatus(true, processedFiles, processedFiles, null, item.OriginalFile));

                    // the value in the files dictionary is the temp file name assigned by
                    // the caller for any possible Undo operation
                    string undoFileName = Path.Combine(undoFolder, item.BackupName);
                    IGrepEngine engine = GrepEngineFactory.GetReplaceEngine(item.OriginalFile, SearchParams, FileFilter);

                    try
                    {
                        processedFiles++;
                        // Copy file					
                        Utils.CopyFile(item.OriginalFile, undoFileName, true);
                        Utils.DeleteFile(item.OriginalFile);

                        Encoding encoding = Encoding.Default;
                        if (codePage > -1)
                            encoding = Encoding.GetEncoding(codePage);
                        else if (!Utils.IsBinary(undoFileName))
                            encoding = Utils.GetFileEncoding(undoFileName);

                        // The UTF-8 encoding returned from Encoding.GetEncoding("utf-8") includes the BOM - see Encoding.GetPreamble()
                        // If this file does not have the BOM, then change to an encoder without the BOM so the BOM is not added in 
                        // the replace operation
                        if (encoding is UTF8Encoding && !Utils.HasUtf8ByteOrderMark(undoFileName))
                        {
                            encoding = new UTF8Encoding(false);
                        }

                        pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                        if (!engine.Replace(undoFileName, item.OriginalFile, searchPattern, replacePattern, searchType, searchOptions,
                            encoding, item.ReplaceItems, pauseCancelToken))
                        {
                            throw new ApplicationException("Replace failed for file: " + item.OriginalFile);
                        }


                        ProcessedFile?.Invoke(this, new ProgressStatus(false, processedFiles, processedFiles, null, item.OriginalFile));


                        File.SetAttributes(item.OriginalFile, File.GetAttributes(undoFileName));

                        if (restoreLastModifiedDate)
                        {
                            try
                            {
                                FileInfo info = new(item.OriginalFile)
                                {
                                    LastWriteTime = item.LastWriteTime
                                };
                            }
                            catch (IOException ex)
                            {
                                throw new ApplicationException("Failed to reset last write time for file: " + item.OriginalFile, ex);
                            }
                        }

                        GrepEngineFactory.ReturnToPool(item.OriginalFile, engine);
                    }
                    catch (OperationCanceledException)
                    {
                        // Replace the file
                        Utils.DeleteFile(item.OriginalFile);
                        Utils.CopyFile(undoFileName, item.OriginalFile, true);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failure in Replace");
                        try
                        {
                            // Replace the file
                            if (File.Exists(undoFileName) && File.Exists(item.OriginalFile))
                            {
                                Utils.DeleteFile(item.OriginalFile);
                                Utils.CopyFile(undoFileName, item.OriginalFile, true);
                            }
                        }
                        catch
                        {
                            // DO NOTHING
                        }
                        return -1;
                    }
                }
            }
            finally
            {
                GrepEngineFactory.UnloadEngines();
            }

            return processedFiles;
        }

        public static bool Undo(IEnumerable<ReplaceDef> undoMap)
        {
            string undoFolder = Utils.GetUndoFolder();
            if (!Directory.Exists(undoFolder) || DirectoryEx.IsEmpty(undoFolder))
            {
                logger.Error("Failed to undo replacement as temporary directory was removed.");
                return false;
            }
            try
            {
                foreach (var item in undoMap)
                {
                    string sourceFile = Path.Combine(undoFolder, item.BackupName);
                    if (File.Exists(sourceFile))
                        Utils.CopyFile(sourceFile, item.OriginalFile, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to undo replacement");
                return false;
            }
        }
    }

    public class ProgressStatus(bool beginSearch, int processed, int successful, List<GrepSearchResult>? results, string fileName)
    {
        public bool BeginSearch { get; private set; } = beginSearch;
        public int ProcessedFiles { get; private set; } = processed;
        public int SuccessfulFiles { get; private set; } = successful;
        public List<GrepSearchResult>? SearchResults { get; private set; } = results;
        public string FileName { get; private set; } = fileName;
    }
}
