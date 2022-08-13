using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Engines;
using NLog;

namespace dnGREP.Common
{
    public class GrepCore
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public delegate void SearchProgressHandler(object sender, ProgressStatus files);
        public event SearchProgressHandler ProcessedFile = delegate { };

        public GrepCore()
        {
            SearchParams = GrepEngineInitParams.Default;
            FileFilter = new FileFilter();
        }

        public GrepEngineInitParams SearchParams { get; set; }
        public FileFilter FileFilter { get; set; }


        private readonly List<GrepSearchResult> searchResults = new List<GrepSearchResult>();
        private readonly object lockObj = new object();
        private CancellationTokenSource cancellationTokenSource;
        private int processedFilesCount;
        private int foundfilesCount;

        public List<GrepSearchResult> ListFiles(IEnumerable<FileData> files, GrepSearchOption searchOptions, int codePage)
        {
            searchResults.Clear();

            if (files == null)
                return searchResults;

            int successful = 0;
            foreach (FileData fileInfo in files)
            {
                ProcessedFile(this, new ProgressStatus(true, searchResults.Count, successful, null, fileInfo.FullName));

                Encoding encoding = Encoding.Default;
                if (codePage > -1)
                    encoding = Encoding.GetEncoding(codePage);

                try
                {
                    if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.DetectEncodingForFileNamePattern))
                    {
                        if (codePage == -1 && !fileInfo.FullName.Contains(ArchiveDirectory.ArchiveSeparator) &&
                            !Utils.IsArchive(fileInfo.FullName) && !Utils.IsBinary(fileInfo.FullName) &&
                            !Utils.IsPdfFile(fileInfo.FullName))
                        {
                            encoding = Utils.GetFileEncoding(fileInfo.FullName);
                        }
                    }

                    searchResults.Add(new GrepSearchResult(fileInfo, encoding));
                    successful++;
                }
                catch (Exception ex)
                {
                    // will catch file not found errors (Everything search):
                    logger.Error(ex, "Failed in ListFiles");
                    fileInfo.ErrorMsg = ex.Message;
                    AddSearchResult(new GrepSearchResult(fileInfo, encoding));
                }

                if (searchOptions.HasFlag(GrepSearchOption.StopAfterFirstMatch))
                    break;
                if (Utils.CancelSearch)
                    break;
            }

            ProcessedFile(this, new ProgressStatus(false, searchResults.Count, successful, searchResults, null));

            return new List<GrepSearchResult>(searchResults);
        }

        /// <summary>
        /// Searches folder for files whose content matches regex
        /// </summary>
        /// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
        /// <param name="searchRegex">Regex pattern</param>
        /// <returns>List of results. If nothing is found returns empty list</returns>
        public List<GrepSearchResult> Search(IEnumerable<string> files, SearchType searchType, string searchPattern, GrepSearchOption searchOptions, int codePage)
        {
            searchResults.Clear();

            if (files == null)
                return searchResults;

            if (string.IsNullOrEmpty(searchPattern))
                return searchResults;

            Utils.CancelSearch = false;

            processedFilesCount = 0;
            foundfilesCount = 0;

            try
            {
                if (SearchParams.SearchParallel)
                {
                    cancellationTokenSource = new CancellationTokenSource();

                    ParallelOptions po = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount * 4 / 5),
                        CancellationToken = cancellationTokenSource.Token
                    };
                    Parallel.ForEach(files, po, f => Search(f, searchType, searchPattern, searchOptions, codePage));
                }
                else
                {
                    foreach (var file in files)
                    {
                        Search(file, searchType, searchPattern, searchOptions, codePage);

                        if (searchOptions.HasFlag(GrepSearchOption.StopAfterFirstMatch) && searchResults.Count > 0)
                            break;

                        if (Utils.CancelSearch)
                            break;
                    }
                }
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
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
                GrepEngineFactory.UnloadEngines();
            }

            return new List<GrepSearchResult>(searchResults);
        }

        public List<GrepSearchResult> CaptureGroupSearch(IEnumerable<string> files, string filePatternInclude,
            GrepSearchOption searchOptions, SearchType searchType, string searchPattern, int codePage)
        {
            searchResults.Clear();

            if (files == null || !files.Any())
                return searchResults;

            Utils.CancelSearch = false;

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

                    Search(filePath, searchType, modSearchPattern, searchOptions, codePage);

                    if (searchOptions.HasFlag(GrepSearchOption.StopAfterFirstMatch) && searchResults.Count > 0)
                        break;

                    if (Utils.CancelSearch)
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed in capture group search");
            }
            finally
            {
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
                GrepEngineFactory.UnloadEngines();
            }
            return new List<GrepSearchResult>(searchResults);
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

        private void Search(string file, SearchType searchType, string searchPattern, GrepSearchOption searchOptions, int codePage)
        {
            try
            {
                ProcessedFile(this, new ProgressStatus(true, processedFilesCount, foundfilesCount, null, file));

                bool isArchive = Utils.IsArchive(file);

                Encoding encoding = Encoding.Default;
                if (codePage > -1)
                    encoding = Encoding.GetEncoding(codePage);
                else if (!isArchive && !Utils.IsBinary(file) && !Utils.IsPdfFile(file))
                    encoding = Utils.GetFileEncoding(file);

                if (Utils.CancelSearch)
                {
                    if (cancellationTokenSource != null)
                        cancellationTokenSource.Cancel();
                    return;
                }

                IGrepEngine engine = GrepEngineFactory.GetSearchEngine(file, SearchParams, FileFilter, searchType);

                if (isArchive && engine is ArchiveEngine archiveEngine)
                {
                    archiveEngine.SetSearchOptions(FileFilter, SearchParams);
                    archiveEngine.StartingFileSearch += ArchiveEngine_StartingFileSearch;

                    foreach (var fileSearchResults in archiveEngine.Search(file, searchPattern, searchType, searchOptions, encoding))
                    {
                        if (fileSearchResults != null && fileSearchResults.Count > 0)
                        {
                            AddSearchResults(fileSearchResults);
                        }
                        int hits = fileSearchResults.Where(r => r.IsSuccess).Count();
                        Interlocked.Add(ref foundfilesCount, hits);

                        ProcessedFile(this, new ProgressStatus(false, processedFilesCount, foundfilesCount, fileSearchResults, file));
                    }
                    archiveEngine.StartingFileSearch -= ArchiveEngine_StartingFileSearch;
                }
                else
                {
                    Interlocked.Increment(ref processedFilesCount);

                    var fileSearchResults = engine.Search(file, searchPattern, searchType, searchOptions, encoding).ToList();

                    if (fileSearchResults != null && fileSearchResults.Count > 0)
                    {
                        AddSearchResults(fileSearchResults);
                    }
                    int hits = fileSearchResults.Where(r => r.IsSuccess).Count();
                    Interlocked.Add(ref foundfilesCount, hits);

                    ProcessedFile(this, new ProgressStatus(false, processedFilesCount, foundfilesCount, fileSearchResults, file));
                }

                GrepEngineFactory.ReturnToPool(file, engine);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed in Search");
                AddSearchResult(new GrepSearchResult(file, searchPattern, ex.Message, false));
                if (ProcessedFile != null)
                {
                    List<GrepSearchResult> _results = new List<GrepSearchResult>
                    {
                        new GrepSearchResult(file, searchPattern, ex.Message, false)
                    };
                    ProcessedFile(this, new ProgressStatus(false, processedFilesCount, foundfilesCount, _results, file));
                }
            }
            finally
            {
                if (searchOptions.HasFlag(GrepSearchOption.StopAfterFirstMatch) && searchResults.Count > 0)
                {
                    if (cancellationTokenSource != null)
                        cancellationTokenSource.Cancel();
                }
            }
        }

        private void ArchiveEngine_StartingFileSearch(object sender, DataEventArgs<string> e)
        {
            Interlocked.Increment(ref processedFilesCount);
            ProcessedFile(this, new ProgressStatus(true, processedFilesCount, foundfilesCount, null, e.Data));
        }

        public int Replace(IEnumerable<ReplaceDef> files, SearchType searchType, string searchPattern, string replacePattern, GrepSearchOption searchOptions, int codePage)
        {
            string undoFolder = Utils.GetUndoFolder();

            if (files == null || !files.Any() || !Directory.Exists(undoFolder))
                return 0;

            GrepEngineBase.ResetGuidxCache();

            replacePattern = Utils.ReplaceSpecialCharacters(replacePattern);

            bool restoreLastModifiedDate = GrepSettings.Instance.Get<bool>(GrepSettings.Key.RestoreLastModifiedDate);

            int processedFiles = 0;
            Utils.CancelSearch = false;
            string undoFileName = null;

            try
            {
                foreach (var item in files)
                {
                    ProcessedFile(this, new ProgressStatus(true, processedFiles, processedFiles, null, item.OrginalFile));

                    // the value in the files dictionary is the temp file name assigned by
                    // the caller for any possible Undo operation
                    undoFileName = Path.Combine(undoFolder, item.BackupName);
                    IGrepEngine engine = GrepEngineFactory.GetReplaceEngine(item.OrginalFile, SearchParams, FileFilter);

                    try
                    {
                        processedFiles++;
                        // Copy file					
                        Utils.CopyFile(item.OrginalFile, undoFileName, true);
                        Utils.DeleteFile(item.OrginalFile);

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

                        if (Utils.CancelSearch)
                        {
                            break;
                        }

                        if (!engine.Replace(undoFileName, item.OrginalFile, searchPattern, replacePattern, searchType, searchOptions, encoding, item.ReplaceItems))
                        {
                            throw new ApplicationException("Replace failed for file: " + item.OrginalFile);
                        }
                        

                        if (!Utils.CancelSearch)
                            ProcessedFile(this, new ProgressStatus(false, processedFiles, processedFiles, null, item.OrginalFile));


                        File.SetAttributes(item.OrginalFile, File.GetAttributes(undoFileName));
                        
                        if (restoreLastModifiedDate)
                        {
                            try
                            {
                                FileInfo info = new FileInfo(item.OrginalFile);
                                info.LastWriteTime = item.LastWriteTime;
                            }
                            catch (System.IO.IOException ex)
                            {
                                throw new ApplicationException("Failed to reset last write time for file: " + item.OrginalFile, ex);
                            }
                        }

                        GrepEngineFactory.ReturnToPool(item.OrginalFile, engine);

                        if (Utils.CancelSearch)
                        {
                            // Replace the file
                            Utils.DeleteFile(item.OrginalFile);
                            Utils.CopyFile(undoFileName, item.OrginalFile, true);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failure in Replace");
                        try
                        {
                            // Replace the file
                            if (File.Exists(undoFileName) && File.Exists(item.OrginalFile))
                            {
                                Utils.DeleteFile(item.OrginalFile);
                                Utils.CopyFile(undoFileName, item.OrginalFile, true);
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

        public bool Undo(IEnumerable<ReplaceDef> undoMap)
        {
            string undoFolder = Utils.GetUndoFolder();
            if (!Directory.Exists(undoFolder) || Directory.IsEmpty(undoFolder))
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
                        Utils.CopyFile(sourceFile, item.OrginalFile, true);
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

    public class ProgressStatus
    {
        public ProgressStatus(bool beginSearch, int processed, int successful, List<GrepSearchResult> results, string fileName)
        {
            BeginSearch = beginSearch;
            ProcessedFiles = processed;
            SuccessfulFiles = successful;
            SearchResults = results;
            FileName = fileName;
        }
        public bool BeginSearch { get; private set; }
        public int ProcessedFiles { get; private set; }
        public int SuccessfulFiles { get; private set; }
        public List<GrepSearchResult> SearchResults { get; private set; }
        public string FileName { get; private set; }
    }
}
