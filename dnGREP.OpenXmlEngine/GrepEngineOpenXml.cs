using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnGREP.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using ExcelNumberFormat;
using NLog;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Engines.OpenXml
{
    /// <summary>
    /// Plug-in for searching OpenXml Word and Excel documents
    /// </summary>
    public class GrepEngineOpenXml : GrepEngineBase, IGrepEngine
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, Dictionary<string, Level>> numberFormats = new Dictionary<string, Dictionary<string, Level>>();


        public List<GrepSearchResult> Search(string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            using (var input = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                return Search(input, fileName, searchPattern, searchType, searchOptions, encoding);
            }
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
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

            List<GrepSearchResult> result = SearchMultiline(input, fileName, searchPattern, searchOptions, searchMethodMultiline);
            return result;
        }

        private List<GrepSearchResult> SearchMultiline(Stream input, string file, string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod)
        {
            List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

            string ext = Path.GetExtension(file);

            if (ext.StartsWith(".doc", StringComparison.OrdinalIgnoreCase))
            {
                SearchWord(input, file, searchPattern, searchOptions, searchMethod, searchResults);
            }
            else if (ext.StartsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                SearchExcel(input, file, searchPattern, searchOptions, searchMethod, searchResults);
            }

            return searchResults;
        }

        private void SearchExcel(Stream stream, string file, string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod, List<GrepSearchResult> searchResults)
        {
            try
            {
                var sheets = ExtractExcelText(stream);
                foreach (var kvPair in sheets)
                {
                    var lines = searchMethod(-1, 0, kvPair.Value, searchPattern, searchOptions, true);
                    if (lines.Count > 0)
                    {
                        GrepSearchResult result = new GrepSearchResult(file, searchPattern, lines, Encoding.Default)
                        {
                            AdditionalInformation = string.Format(" Sheet [{0}]", kvPair.Key)
                        };
                        using (StringReader reader = new StringReader(kvPair.Value))
                        {
                            result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                        }
                        result.ReadOnly = true;
                        searchResults.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, string.Format("Failed to search inside Excel file '{0}'", file), ex);
            }
        }

        private List<KeyValuePair<string, string>> ExtractExcelText(Stream stream)
        {
            List<KeyValuePair<string, string>> results = new List<KeyValuePair<string, string>>();

            // Auto-detect format, supports:
            //  - Binary Excel files (2.0-2003 format; *.xls)
            //  - OpenXml Excel files (2007 format; *.xlsx)
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    StringBuilder sb = new StringBuilder();
                    while (reader.Read())
                    {
                        for (int col = 0; col < reader.FieldCount; col++)
                        {
                            sb.Append(GetFormattedValue(reader, col, CultureInfo.CurrentCulture)).Append('\t');
                        }

                        sb.Append(Environment.NewLine);
                    }

                    results.Add(new KeyValuePair<string, string>(reader.Name, sb.ToString()));

                } while (reader.NextResult());

            }

            return results;
        }

        private string GetFormattedValue(IExcelDataReader reader, int columnIndex, CultureInfo culture)
        {
            var value = reader.GetValue(columnIndex);
            var formatString = reader.GetNumberFormatString(columnIndex);
            if (formatString != null)
            {
                var format = new NumberFormat(formatString);
                return format.Format(value, culture);
            }
            return Convert.ToString(value, culture);
        }

        private void SearchWord(Stream stream, string file, string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod, List<GrepSearchResult> searchResults)
        {
            try
            {
                var text = ExtractWordText(stream);

                var lines = searchMethod(-1, 0, text, searchPattern, searchOptions, true);
                if (lines.Count > 0)
                {
                    GrepSearchResult result = new GrepSearchResult(file, searchPattern, lines, Encoding.Default);
                    using (StringReader reader = new StringReader(text))
                    {
                        result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                    }
                    result.ReadOnly = true;
                    searchResults.Add(result);
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, string.Format("Failed to search inside Word file '{0}'", file), ex);
            }
        }

        private string ExtractWordText(Stream stream)
        {
            StringBuilder sb = new StringBuilder();

            // Open a given Word document as readonly
            using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                var docStyles = doc.MainDocumentPart.StyleDefinitionsPart.Styles
                    .Where(r => r is Style).Select(r => r as Style);

                WordListManager wlm = WordListManager.Empty;
                if (doc.MainDocumentPart.NumberingDefinitionsPart != null && doc.MainDocumentPart.NumberingDefinitionsPart.Numbering != null)
                {
                    wlm = new WordListManager(doc.MainDocumentPart.NumberingDefinitionsPart.Numbering);
                }

                ExtractText(body, docStyles, wlm, sb);
            }

            return sb.ToString();
        }

        private bool isInTableRow;
        private void ExtractText(OpenXmlElement elem, IEnumerable<Style> docStyles, WordListManager wlm, StringBuilder sb)
        {
            if (elem is Paragraph)
            {
                var para = elem as Paragraph;

                string indent = GetIndent(para, docStyles);
                string fmtNum = wlm.GetFormattedNumber(para);

                if (isInTableRow)
                    sb.Append(indent).Append(fmtNum).Append(elem.InnerText).Append('\t');
                else
                    sb.Append(indent).Append(fmtNum).AppendLine(elem.InnerText);
            }
            else if (elem is TableRow)
            {
                isInTableRow = true;

                sb.Append('\t');

                foreach (var child in elem)
                {
                    ExtractText(child, docStyles, wlm, sb);
                }

                sb.AppendLine();

                isInTableRow = false;
            }
            else
            {
                foreach (var child in elem)
                {
                    ExtractText(child, docStyles, wlm, sb);
                }
            }
        }

        private string GetIndent(Paragraph para, IEnumerable<Style> docStyles)
        {
            string indent = string.Empty;
            if (para != null && para.ParagraphProperties != null && para.ParagraphProperties.Indentation != null)
            {
                var indentation = para.ParagraphProperties.Indentation;
                if (indentation.Left != null && indentation.Left.HasValue)
                {
                    indent = WordListManager.TwipsToSpaces(indentation.Left);
                }
                else if (indentation.Start != null && indentation.Start.HasValue)
                {
                    indent = WordListManager.TwipsToSpaces(indentation.Start);
                }
            }
            if (para != null && para.ParagraphProperties != null && para.ParagraphProperties.ParagraphStyleId != null &&
                para.ParagraphProperties.ParagraphStyleId.Val != null &&
                para.ParagraphProperties.ParagraphStyleId.Val.HasValue)
            {
                var style = docStyles.Where(r => r.StyleId == para.ParagraphProperties.ParagraphStyleId.Val.Value)
                    .Select(r => r).FirstOrDefault();

                if (style != null)
                {
                    var pp = style.Where(r => r is StyleParagraphProperties)
                        .Select(r => r as StyleParagraphProperties).FirstOrDefault();

                    if (pp != null && pp.Indentation != null)
                    {
                        if (pp.Indentation.Left != null && pp.Indentation.Left.HasValue)
                        {
                            indent = WordListManager.TwipsToSpaces(pp.Indentation.Left);
                        }
                        else if (pp.Indentation.Start != null && pp.Indentation.Start.HasValue)
                        {
                            indent = WordListManager.TwipsToSpaces(pp.Indentation.Start);
                        }
                    }
                }
            }

            return indent;
        }


        public bool IsSearchOnly { get { return true; } }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Version FrameworkVersion
        {
            get { return Assembly.GetAssembly(typeof(IGrepEngine)).GetName().Version; }
        }

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
