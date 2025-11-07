using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using dnGREP.Common;
using dnGREP.Localization;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Engines.OpenXml
{
    /// <summary>
    /// Plug-in for searching OpenXml Word and Excel documents
    /// </summary>
    public partial class GrepEngineOpenXml : GrepEngineBase, IGrepPluginEngine
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public List<string> DefaultFileExtensions => ["docx", "docm", "xls", "xlsx", "xlsm", "pptx", "pptm"];

        public override bool Initialize(GrepEngineInitParams param, FileFilter filter)
        {
            WordReader.Initialize();
            return base.Initialize(param, filter);
        }

        public bool IsSearchOnly => true;

        public bool PreviewPlainText { get; set; }

        public bool ApplyStringMap { get; set; }

        public List<GrepSearchResult> Search(string documentFilePath, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            SearchDelegates.DoSearch searchMethod = GetSearchDelegate(searchType);

            string cacheFilePath = string.Empty;
            if (CreatePlainTextFile)
            {
                cacheFilePath = GetCacheFilePath(new(documentFilePath), null);
            }

            return SearchMultiline(documentFilePath, cacheFilePath, null, searchPattern,
                searchOptions, searchMethod, encoding, pauseCancelToken);
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, FileData fileData, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding,
            PauseCancelToken pauseCancelToken)
        {
            SearchDelegates.DoSearch searchMethodMultiline = GetSearchDelegate(searchType);

            string cacheFilePath = string.Empty;
            if (CreatePlainTextFile)
            {
                cacheFilePath = GetCacheFilePath(fileData, input);
            }

            List<GrepSearchResult> result = SearchMultiline(fileData.FullName, cacheFilePath, input, searchPattern,
                searchOptions, searchMethodMultiline, encoding, pauseCancelToken);
            return result;
        }

        private SearchDelegates.DoSearch GetSearchDelegate(SearchType searchType)
        {
            return searchType switch
            {
                SearchType.Regex => DoRegexSearch,
                SearchType.Soundex => DoFuzzySearch,
                _ => DoTextSearch,
            };
        }

        private bool CreatePlainTextFile => PreviewPlainText ||
            GrepSettings.Instance.Get<bool>(GrepSettings.Key.CacheExtractedFiles);

        private static string GetCacheFilePath(FileData fileData, Stream? stream)
        {
            string cacheFolder = Path.Combine(Utils.GetCacheFolder(), @"dnGREP-OpenXML");
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);


            // get the unique filename for this file using SHA256
            // if the same file exists multiple places in the search tree, all will use the same temp file
            string cacheFileName;
            HashOption hashOption = GrepSettings.Instance.Get<HashOption>(GrepSettings.Key.CacheFileHashType);
            if (stream != null)
            {
                cacheFileName = hashOption == HashOption.SizeTimestamp ?
                    Utils.GetTempTextFileName(fileData) :
                    Utils.GetTempTextFileName(stream, fileData.FullName);
            }
            else
            {
                cacheFileName = hashOption == HashOption.SizeTimestamp ?
                    Utils.GetTempTextFileName(fileData) :
                    Utils.GetTempTextFileName(fileData.FullName);
            }
            string cacheFilePath = Path.Combine(cacheFolder, cacheFileName);
            return cacheFilePath;
        }

        private static string GetCacheSheetFilePath(string cacheFilePath, int number, string sheetName)
        {
            string? path = Path.GetDirectoryName(cacheFilePath);
            string fileName = Path.GetFileName(cacheFilePath);
            Match match = CacheFileNameRegex().Match(fileName);
            if (match.Success)
            {
                string documentName = match.Groups[1].Value;
                string sha256 = match.Groups[2].Value;

                // new name must match the SheetNameRegex below
                fileName = documentName + "_" + number + "_[" + sheetName + "]_" + sha256 + ".txt";
                return Path.Combine(path ?? string.Empty, fileName);
            }
            return cacheFilePath;
        }

        private static bool CacheFileExists(string cacheFilePath)
        {
            if (File.Exists(cacheFilePath))
                return true;

            return GetCacheFiles(cacheFilePath).Length > 0;
        }

        private static string[] GetCacheFiles(string cacheFilePath)
        {
            string? dir = Path.GetDirectoryName(cacheFilePath);
            if (dir != null && Directory.Exists(dir))
            {
                Match match = CacheFileNameRegex().Match(Path.GetFileName(cacheFilePath));
                if (match.Success)
                {
                    string sha256 = match.Groups[2].Value;
                    return Directory.GetFiles(dir, "*" + sha256 + ".txt");
                }
            }
            return [];
        }

        private static string ReadCacheFile(string cacheFilePath, Encoding encoding)
        {
            Encoding fileEncoding = encoding;
            string textOut;
            // GrepCore cannot check encoding of the original document file. If the encoding parameter is
            // not default then it is the user-specified code page.  If the encoding parameter *is*
            // the default, then it most likely not been set, so get the encoding of the extracted
            // text file:
            if (encoding == Encoding.Default)
                fileEncoding = Utils.GetFileEncoding(cacheFilePath);

            using (StreamReader streamReader = new(cacheFilePath, fileEncoding, detectEncodingFromByteOrderMarks: false))
                textOut = streamReader.ReadToEnd();

            return textOut;
        }

        private static List<Sheet> ReadCacheFilePages(string[] files, Encoding encoding)
        {
            List<Sheet> pages = [];

            if (files.Length == 0)
                return pages;

            string firstFile = files.First();

            Encoding fileEncoding = encoding;
            if (encoding == Encoding.Default)
                fileEncoding = Utils.GetFileEncoding(firstFile);

            foreach (string file in files)
            {
                string number = string.Empty;
                string name = string.Empty;
                var match = SheetNameRegex().Match(Path.GetFileNameWithoutExtension(file));
                if (match.Success)
                {
                    number = match.Groups[1].Value;
                    name = match.Groups[2].Value;

                    string textOut;
                    using (StreamReader streamReader = new(file, fileEncoding, detectEncodingFromByteOrderMarks: false))
                        textOut = streamReader.ReadToEnd();

                    pages.Add(new(name, textOut));
                }
            }

            return pages;
        }

        private static void WriteCacheText(string cacheFilePath, string content)
        {
            try
            {
                if (File.Exists(cacheFilePath))
                {
                    File.Delete(cacheFilePath);
                }
                File.WriteAllText(cacheFilePath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format("Failed to write cache text '{0}'", cacheFilePath));
            }
        }


        private List<GrepSearchResult> SearchMultiline(string documentFilePath, string cacheFilePath, Stream? stream,
            string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod, Encoding encoding,
            PauseCancelToken pauseCancelToken)
        {
            List<GrepSearchResult> searchResults = [];

            string ext = Path.GetExtension(documentFilePath);

            if (ext.StartsWith(".doc", StringComparison.OrdinalIgnoreCase))
            {
                SearchWord(documentFilePath, cacheFilePath, stream, searchPattern, searchOptions, searchMethod, searchResults, encoding, pauseCancelToken);
            }
            else if (ext.StartsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                SearchExcel(documentFilePath, cacheFilePath, stream, searchPattern, searchOptions, searchMethod, searchResults, encoding, pauseCancelToken);
            }
            else if (ext.StartsWith(".ppt", StringComparison.OrdinalIgnoreCase))
            {
                SearchPowerPoint(documentFilePath, cacheFilePath, stream, searchPattern, searchOptions, searchMethod, searchResults, encoding, pauseCancelToken);
            }

            return searchResults;
        }

        private void SearchExcel(string documentFilePath, string cacheFilePath, Stream? stream,
            string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod,
            List<GrepSearchResult> searchResults, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            Stream? fileStream = null;
            try
            {
                List<Sheet> sheets;
                if (CacheFileExists(cacheFilePath))
                {
                    sheets = ReadCacheFilePages(GetCacheFiles(cacheFilePath), encoding);
                }
                else
                {
                    if (stream == null)
                    {
                        fileStream = File.Open(documentFilePath, FileMode.Open, FileAccess.Read);
                        stream = fileStream;
                    }
                    sheets = ExcelReader.ExtractExcelText(stream, pauseCancelToken);

                    int idx = 1;
                    foreach (Sheet sheet in sheets)
                    {
                        string cacheSheetFilePath = GetCacheSheetFilePath(cacheFilePath, idx++, sheet.Name);
                        WriteCacheText(cacheSheetFilePath, sheet.Content);
                    }
                }

                int count = 0;

                foreach (var sheet in sheets)
                {
                    count++;
                    var lines = searchMethod(-1, 0, sheet.Content, searchPattern, searchOptions, true, pauseCancelToken);
                    if (lines.Count > 0)
                    {
                        GrepSearchResult result = new(documentFilePath, searchPattern, lines, Encoding.Default)
                        {
                            AdditionalInformation = " " + TranslationSource.Format(Resources.Main_ExcelSheetName, sheet.Name)
                        };
                        using (StringReader reader = new(sheet.Content))
                        {
                            result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                        }
                        result.FileInfo = new(documentFilePath);
                        result.IsReadOnlyFileType = true;

                        if (PreviewPlainText && !string.IsNullOrEmpty(cacheFilePath))
                        {
                            result.FileInfo.TempFile = GetCacheSheetFilePath(cacheFilePath, count, sheet.Name);
                        }
                        searchResults.Add(result);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected exception
                searchResults.Clear();
            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format("Failed to search inside Excel file '{0}'", documentFilePath));
                searchResults.Add(new GrepSearchResult(documentFilePath, searchPattern, ex.Message, false));
            }
            finally
            {
                fileStream?.Dispose();
            }
        }
        private void SearchWord(string documentFilePath, string cacheFilePath, Stream? stream,
            string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod,
            List<GrepSearchResult> searchResults, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            Stream? fileStream = null;
            try
            {
                string text;
                if (CacheFileExists(cacheFilePath))
                {
                    text = ReadCacheFile(cacheFilePath, encoding);
                }
                else
                {
                    if (stream == null)
                    {
                        fileStream = File.Open(documentFilePath, FileMode.Open, FileAccess.Read);
                        stream = fileStream;
                    }
                    text = WordReader.ExtractWordText(stream, ApplyStringMap, pauseCancelToken);

                    if (!string.IsNullOrEmpty(cacheFilePath) && !File.Exists(cacheFilePath))
                    {
                        WriteCacheText(cacheFilePath, text);
                    }
                }

                var lines = searchMethod(-1, 0, text, searchPattern,
                    searchOptions, true, pauseCancelToken);
                if (lines.Count > 0)
                {
                    GrepSearchResult result = new(documentFilePath, searchPattern, lines, Encoding.Default);
                    using (StringReader reader = new(text))
                    {
                        result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                    }
                    result.FileInfo = new(documentFilePath);
                    result.IsReadOnlyFileType = true;

                    if (PreviewPlainText && !string.IsNullOrEmpty(cacheFilePath))
                    {
                        result.FileInfo.TempFile = cacheFilePath;
                    }

                    searchResults.Add(result);
                }
            }
            catch (OperationCanceledException)
            {
                // expected exception
                searchResults.Clear();
            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format("Failed to search inside Word file '{0}'", documentFilePath));
                searchResults.Add(new GrepSearchResult(documentFilePath, searchPattern, ex.Message, false));
            }
            finally
            {
                fileStream?.Dispose();
            }
        }

        private void SearchPowerPoint(string documentFilePath, string cacheFilePath, Stream? stream,
            string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod,
            List<GrepSearchResult> searchResults, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            Stream? fileStream = null;
            try
            {
                List<Sheet> slides;
                if (CacheFileExists(cacheFilePath))
                {
                    slides = ReadCacheFilePages(GetCacheFiles(cacheFilePath), encoding);
                }
                else
                {
                    if (stream == null)
                    {
                        fileStream = File.Open(documentFilePath, FileMode.Open, FileAccess.Read);
                        stream = fileStream;
                    }
                    slides = PowerPointReader.ExtractPowerPointText(stream, ApplyStringMap, pauseCancelToken);

                    if (!string.IsNullOrEmpty(cacheFilePath))
                    {
                        int idx = 1;
                        foreach (var slide in slides)
                        {
                            string cacheSheetFilePath = GetCacheSheetFilePath(cacheFilePath, idx++, slide.Name);
                            WriteCacheText(cacheSheetFilePath, slide.Content);
                        }
                    }
                }

                int count = 0;

                foreach (var slide in slides)
                {
                    count++;
                    var lines = searchMethod(-1, 0, slide.Content, searchPattern,
                        searchOptions, true, pauseCancelToken);
                    if (lines.Count > 0)
                    {
                        GrepSearchResult result = new(documentFilePath, searchPattern, lines, Encoding.Default)
                        {
                            AdditionalInformation = " " + TranslationSource.Format(Resources.Main_PowerPointSlideNumber, slide.Name)
                        };

                        using (StringReader reader = new(slide.Content))
                        {
                            result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                        }
                        result.FileInfo = new(documentFilePath);
                        result.IsReadOnlyFileType = true;

                        if (PreviewPlainText && !string.IsNullOrEmpty(cacheFilePath))
                        {
                            result.FileInfo.TempFile = GetCacheSheetFilePath(cacheFilePath, count, slide.Name);
                        }
                        searchResults.Add(result);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected exception
                searchResults.Clear();
            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format("Failed to search inside PowerPoint file '{0}'", documentFilePath));
                searchResults.Add(new GrepSearchResult(documentFilePath, searchPattern, ex.Message, false));
            }
            finally
            {
                fileStream?.Dispose();
            }
        }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Version? FrameworkVersion => Assembly.GetAssembly(typeof(IGrepEngine))?.GetName()?.Version;

        public void Unload()
        {
            //Do nothing
        }

        public override void OpenFile(OpenFileArgs args)
        {
            args.UseCustomEditor = false;
            Utils.OpenFile(args);
        }

        [GeneratedRegex(@"(.+)_([0-9a-fA-F]{64})")]
        private static partial Regex CacheFileNameRegex();

        [GeneratedRegex(@"_(\d+)_\[(.+)\]_[0-9a-fA-F]{64}")]
        private static partial Regex SheetNameRegex();
    }
}
