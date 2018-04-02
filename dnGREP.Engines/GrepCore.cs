using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dnGREP.Engines;
using NLog;

namespace dnGREP.Common
{
    public class GrepCore
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public delegate void SearchProgressHandler(object sender, ProgressStatus files);
        public event SearchProgressHandler ProcessedFile = delegate { };

        public GrepCore()
        {
            SearchParams = GrepEngineInitParams.Default;
            FileFilter = new FileFilter();
        }

        public GrepEngineInitParams SearchParams { get; set; }
        public FileFilter FileFilter { get; set; }


        private List<GrepSearchResult> searchResults = new List<GrepSearchResult>();
        private object lockObj = new object();
        private CancellationTokenSource cancellationTokenSource;
        private int processedFilesCount;
        private int foundfilesCount;

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

            Utils.CancelSearch = false;

            if (searchPattern == null || searchPattern.Trim() == "")
            {
                int count = 0;
                foreach (string file in files)
                {
                    count++;
                    ProcessedFile(this, new ProgressStatus(true, searchResults.Count, count, null, file));

                    searchResults.Add(new GrepSearchResult(file, searchPattern, null, Encoding.Default));

                    if ((searchOptions & GrepSearchOption.StopAfterFirstMatch) == GrepSearchOption.StopAfterFirstMatch)
                        break;
                    if (Utils.CancelSearch)
                        break;
                }

                ProcessedFile(this, new ProgressStatus(false, searchResults.Count, count, searchResults, null));

                return new List<GrepSearchResult>(searchResults);
            }
            else
            {
                processedFilesCount = 0;
                foundfilesCount = 0;

                try
                {
                    if (SearchParams.SearchParallel)
                    {
                        cancellationTokenSource = new CancellationTokenSource();

                        ParallelOptions po = new ParallelOptions();
                        po.MaxDegreeOfParallelism = Environment.ProcessorCount * 4 / 5;
                        po.CancellationToken = cancellationTokenSource.Token;
                        Parallel.ForEach(files, po, f => Search(f, searchType, searchPattern, searchOptions, codePage));
                    }
                    else
                    {
                        foreach (var file in files)
                        {
                            Search(file, searchType, searchPattern, searchOptions, codePage);

                            if (searchOptions.HasFlag(GrepSearchOption.StopAfterFirstMatch) && searchResults.Count > 0)
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

                IGrepEngine engine = GrepEngineFactory.GetSearchEngine(file, SearchParams, FileFilter);

                Interlocked.Increment(ref processedFilesCount);

                Encoding encoding = Encoding.Default;
                if (codePage > -1)
                    encoding = Encoding.GetEncoding(codePage);
                else if (!Utils.IsBinary(file) && !Utils.IsPdfFile(file))
                    encoding = Utils.GetFileEncoding(file);

                if (Utils.CancelSearch)
                {
                    if (cancellationTokenSource != null)
                        cancellationTokenSource.Cancel();
                    return;
                }

                List<GrepSearchResult> fileSearchResults = engine.Search(file, searchPattern, searchType, searchOptions, encoding);

                if (fileSearchResults != null && fileSearchResults.Count > 0)
                {
                    AddSearchResults(fileSearchResults);
                }
                int hits = fileSearchResults.Where(r => r.IsSuccess).Count();
                Interlocked.Add(ref foundfilesCount, hits);

                ProcessedFile(this, new ProgressStatus(false, processedFilesCount, foundfilesCount, fileSearchResults, file));

                GrepEngineFactory.ReturnToPool(file, engine);
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
                AddSearchResult(new GrepSearchResult(file, searchPattern, ex.Message, false));
                if (ProcessedFile != null)
                {
                    List<GrepSearchResult> _results = new List<GrepSearchResult>();
                    _results.Add(new GrepSearchResult(file, searchPattern, ex.Message, false));
                    ProcessedFile(this, new ProgressStatus(false, processedFilesCount, foundfilesCount, _results, file));
                }
            }
            finally
            {
                if ((searchOptions & GrepSearchOption.StopAfterFirstMatch) == GrepSearchOption.StopAfterFirstMatch && searchResults.Count > 0)
                {
                    if (cancellationTokenSource != null)
                        cancellationTokenSource.Cancel();
                }
            }
        }

        public int Replace(Dictionary<string, string> files, SearchType searchType, string searchPattern, string replacePattern, GrepSearchOption searchOptions, int codePage)
        {
            string tempFolder = Utils.GetTempFolder();

            if (files == null || files.Count == 0 || !Directory.Exists(tempFolder))
                return 0;

            replacePattern = Utils.ReplaceSpecialCharacters(replacePattern);

            int processedFiles = 0;
            Utils.CancelSearch = false;
            string tempFileName = null;

            try
            {
                foreach (string file in files.Keys)
                {
                    ProcessedFile(this, new ProgressStatus(true, processedFiles, processedFiles, null, file));

                    // the value in the files dictionary is the temp file name assigned by
                    // the caller for any possible Undo operation
                    tempFileName = Path.Combine(tempFolder, files[file]);
                    IGrepEngine engine = GrepEngineFactory.GetReplaceEngine(file, SearchParams, FileFilter);

                    try
                    {
                        processedFiles++;
                        // Copy file					
                        Utils.CopyFile(file, tempFileName, true);
                        Utils.DeleteFile(file);

                        Encoding encoding = Encoding.Default;
                        if (codePage > -1)
                            encoding = Encoding.GetEncoding(codePage);
                        else if (!Utils.IsBinary(tempFileName))
                            encoding = Utils.GetFileEncoding(tempFileName);

                        if (Utils.CancelSearch)
                        {
                            break;
                        }

                        if (!engine.Replace(tempFileName, file, searchPattern, replacePattern, searchType, searchOptions, encoding))
                        {
                            throw new ApplicationException("Replace failed for file: " + file);
                        }

                        if (!Utils.CancelSearch)
                            ProcessedFile(this, new ProgressStatus(false, processedFiles, processedFiles, null, file));


                        File.SetAttributes(file, File.GetAttributes(tempFileName));

                        GrepEngineFactory.ReturnToPool(file, engine);

                        if (Utils.CancelSearch)
                        {
                            // Replace the file
                            Utils.DeleteFile(file);
                            Utils.CopyFile(tempFileName, file, true);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
                        try
                        {
                            // Replace the file
                            if (File.Exists(tempFileName) && File.Exists(file))
                            {
                                Utils.DeleteFile(file);
                                Utils.CopyFile(tempFileName, file, true);
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

        public bool Undo(Dictionary<string, string> undoMap)
        {
            string tempFolder = Utils.GetTempFolder();
            if (!Directory.Exists(tempFolder))
            {
                logger.Error("Failed to undo replacement as temporary directory was removed.");
                return false;
            }
            try
            {
                foreach(string filePath in undoMap.Keys)
                {
                    string sourceFile = Path.Combine(tempFolder, undoMap[filePath]);
                    if (File.Exists(sourceFile))
                        Utils.CopyFile(sourceFile, filePath, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, "Failed to undo replacement", ex);
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
