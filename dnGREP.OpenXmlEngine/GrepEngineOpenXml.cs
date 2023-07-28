using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using dnGREP.Common;
using dnGREP.Localization;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Engines.OpenXml
{
    /// <summary>
    /// Plug-in for searching OpenXml Word and Excel documents
    /// </summary>
    public class GrepEngineOpenXml : GrepEngineBase, IGrepPluginEngine
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public IList<string> DefaultFileExtensions
        {
            get { return new string[] { "docx", "docm", "xls", "xlsx", "xlsm", "pptx", "pptm" }; }
        }

        public bool IsSearchOnly => true;

        public bool PreviewPlainText { get; set; }

        public List<GrepSearchResult> Search(string fileName, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, CancellationToken cancellationToken)
        {
            using var input = File.Open(fileName, FileMode.Open, FileAccess.Read);
            return Search(input, fileName, searchPattern, searchType, searchOptions, encoding, cancellationToken);
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding,
            CancellationToken cancellationToken)
        {
            SearchDelegates.DoSearch searchMethodMultiline = DoTextSearch;
            switch (searchType)
            {
                case SearchType.PlainText:
                case SearchType.XPath:
                    searchMethodMultiline = DoTextSearch;
                    break;
                case SearchType.Regex:
                    searchMethodMultiline = DoRegexSearch;
                    break;
                case SearchType.Soundex:
                    searchMethodMultiline = DoFuzzySearch;
                    break;
            }

            List<GrepSearchResult> result = SearchMultiline(input, fileName, searchPattern, searchOptions,
                searchMethodMultiline, cancellationToken);
            return result;
        }

        private List<GrepSearchResult> SearchMultiline(Stream input, string file, string searchPattern,
            GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod,
            CancellationToken cancellationToken)
        {
            List<GrepSearchResult> searchResults = new();

            string ext = Path.GetExtension(file);

            if (ext.StartsWith(".doc", StringComparison.OrdinalIgnoreCase))
            {
                SearchWord(input, file, searchPattern, searchOptions, searchMethod, searchResults, cancellationToken);
            }
            else if (ext.StartsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                SearchExcel(input, file, searchPattern, searchOptions, searchMethod, searchResults, cancellationToken);
            }
            else if (ext.StartsWith(".ppt", StringComparison.OrdinalIgnoreCase))
            {
                SearchPowerPoint(input, file, searchPattern, searchOptions, searchMethod, searchResults, cancellationToken);
            }

            return searchResults;
        }

        private void SearchExcel(Stream stream, string file, string searchPattern,
            GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod,
            List<GrepSearchResult> searchResults, CancellationToken cancellationToken)
        {
            try
            {
                var sheets = ExcelReader.ExtractExcelText(stream, cancellationToken);
                foreach (var kvPair in sheets)
                {
                    var lines = searchMethod(-1, 0, kvPair.Value, searchPattern, searchOptions, true, cancellationToken);
                    if (lines.Count > 0)
                    {
                        GrepSearchResult result = new(file, searchPattern, lines, Encoding.Default)
                        {
                            AdditionalInformation = " " + TranslationSource.Format(Resources.Main_ExcelSheetName, kvPair.Key)
                        };
                        using (StringReader reader = new(kvPair.Value))
                        {
                            result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                        }
                        result.IsReadOnlyFileType = true;
                        if (PreviewPlainText)
                        {
                            result.FileInfo.TempFile = WriteTempFile(kvPair.Value, file, "XLS");
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
                logger.Error(ex, string.Format("Failed to search inside Excel file '{0}'", file));
            }
        }

        private void SearchWord(Stream stream, string file, string searchPattern,
            GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod,
            List<GrepSearchResult> searchResults, CancellationToken cancellationToken)
        {
            try
            {
                var text = WordReader.ExtractWordText(stream, cancellationToken);

                var lines = searchMethod(-1, 0, text, searchPattern,
                    searchOptions, true, cancellationToken);
                if (lines.Count > 0)
                {
                    GrepSearchResult result = new(file, searchPattern, lines, Encoding.Default);
                    using (StringReader reader = new(text))
                    {
                        result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                    }
                    result.IsReadOnlyFileType = true;
                    if (PreviewPlainText)
                    {
                        result.FileInfo.TempFile = WriteTempFile(text, file, "DOC");
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
                logger.Error(ex, string.Format("Failed to search inside Word file '{0}'", file));
            }
        }

        private void SearchPowerPoint(Stream stream, string file, string searchPattern,
            GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod,
            List<GrepSearchResult> searchResults, CancellationToken cancellationToken)
        {
            try
            {
                var slides = PowerPointReader.ExtractPowerPointText(stream, cancellationToken);

                foreach (var slide in slides)
                {
                    var lines = searchMethod(-1, 0, slide.Item2, searchPattern,
                        searchOptions, true, cancellationToken);
                    if (lines.Count > 0)
                    {
                        GrepSearchResult result = new(file, searchPattern, lines, Encoding.Default)
                        {
                            AdditionalInformation = " " + TranslationSource.Format(Resources.Main_PowerPointSlideNumber, slide.Item1)
                        };

                        using (StringReader reader = new(slide.Item2))
                        {
                            result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                        }
                        result.IsReadOnlyFileType = true;
                        if (PreviewPlainText)
                        {
                            result.FileInfo.TempFile = WriteTempFile(slide.Item2, file, "PPT");
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
                logger.Error(ex, string.Format("Failed to search inside PowerPoint file '{0}'", file));
            }
        }

        private static string WriteTempFile(string text, string filePath, string app)
        {
            string tempFolder = Path.Combine(Utils.GetTempFolder(), $"dnGREP-{app}");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            // ensure each temp file is unique, even if the file name exists elsewhere in the search tree
            string fileName = Path.GetFileNameWithoutExtension(filePath) + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".txt";
            string tempFileName = Path.Combine(tempFolder, fileName);

            File.WriteAllText(tempFileName, text);
            return tempFileName;
        }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems, CancellationToken cancellationToken)
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
    }
}
