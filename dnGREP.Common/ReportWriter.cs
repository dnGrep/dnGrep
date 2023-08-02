using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Common
{
    public class ReportWriter
    {
        private static readonly string GLOBAL = "_global_";

        private static string Quote(string text)
        {
            if (text.Contains(',', StringComparison.Ordinal))
            {
                if (text.Contains('"', StringComparison.Ordinal))
                {
                    text = text.Replace("\"", "\"\"", StringComparison.Ordinal);
                }
                return "\"" + text + "\"";
            }
            return text;
        }

        public static string GetResultsAsText(List<GrepSearchResult> source, SearchType typeOfSearch)
        {
            return GetResultsAsText(source, new ReportOptions(typeOfSearch), -1);
        }

        public static string GetResultsAsText(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            if (source != null && source.Any())
            {
                if (options.FilterUniqueValues)
                {
                    return GetUniqueValuesAsText(source, options, limit);
                }
                else if (options.ReportMode == ReportMode.FullLine)
                {
                    return GetFullLinesAsText(source, options, limit);
                }
                else if (options.ReportMode == ReportMode.Matches)
                {
                    return GetMatchesAsText(source, options, limit);
                }
                else if (options.ReportMode == ReportMode.Groups)
                {
                    return GetGroupsAsText(source, options, limit);
                }
            }
            return string.Empty;
        }

        public static string GetResultsAsCSV(List<GrepSearchResult> source, SearchType typeOfSearch)
        {
            return GetResultsAsCSV(source, new ReportOptions(typeOfSearch), -1);
        }

        public static string GetResultsAsCSV(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            if (source != null && source.Any())
            {
                if (options.FilterUniqueValues)
                {
                    return GetUniqueValuesAsCSV(source, options, limit);
                }
                else if (options.ReportMode == ReportMode.FullLine)
                {
                    return GetFullLinesAsCSV(source, options, limit);
                }
                else if (options.ReportMode == ReportMode.Matches)
                {
                    return GetMatchesAsCSV(source, options, limit);
                }
                else if (options.ReportMode == ReportMode.Groups)
                {
                    return GetGroupsAsCSV(source, options, limit);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Creates a CSV file from search results
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destinationPath"></param>
        public static void SaveResultsAsCSV(List<GrepSearchResult> source, SearchType typeOfSearch, string destinationPath)
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.WriteAllText(destinationPath, GetResultsAsCSV(source, typeOfSearch), Encoding.UTF8);
        }

        /// <summary>
        /// Creates a text file from search results
        /// </summary>
        /// <param name="source">the search results</param>
        /// <param name="destinationPath">the file name to save</param>
        public static void SaveResultsAsText(List<GrepSearchResult> source, SearchType typeOfSearch, string destinationPath)
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.WriteAllText(destinationPath, GetResultsAsText(source, typeOfSearch), Encoding.UTF8);
        }

        public static void SaveResultsReport(List<GrepSearchResult> source, bool booleanOperators, string searchText,
            string options, string destinationPath)
        {
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            List<string>? orClauses = null;
            if (booleanOperators)
            {
                BooleanExpression exp = new();
                if (exp.TryParse(searchText) && exp.HasOrExpression)
                {
                    orClauses = exp.Operands.Select(o => o.Value).ToList();
                }
            }

            int fileCount = source.Where(r => !string.IsNullOrWhiteSpace(r.FileNameReal)).Select(r => r.FileNameReal).Distinct().Count();
            int lineCount = source.Sum(s => s.Matches.Where(r => r.LineNumber > 0).Select(r => r.LineNumber).Distinct().Count());
            int matchCount = source.Sum(s => s.Matches == null ? 0 : s.Matches.Count);

            StringBuilder sb = new();
            sb.AppendLine(Resources.Report_DnGrepSearchResults).AppendLine();
            sb.Append(options).AppendLine();
            sb.AppendFormat(Resources.Report_Found0MatchesOn1LinesIn2Files,
                matchCount.ToString("#,##0"), lineCount.ToString("#,##0"), fileCount.ToString("#,##0"))
                .AppendLine();
            sb.Append(GetResultLinesWithContext(source, orClauses ?? new()));

            File.WriteAllText(destinationPath, sb.ToString(), Encoding.UTF8);
        }

        public static string GetResultLinesWithContext(List<GrepSearchResult> source, List<string> orClauses)
        {
            int hexLineSize = GrepSettings.Instance.Get<int>(GrepSettings.Key.HexResultByteLength);

            StringBuilder sb = new();
            string previousFileName = string.Empty;
            foreach (var result in source)
            {
                string orResults = string.Empty;
                if (orClauses.Any())
                {
                    var set = result.Matches.Select(m => m.SearchPattern);
                    var hits = orClauses.Intersect(set);
                    var misses = orClauses.Except(set);
                    if (hits.Any())
                    {
                        orResults += Resources.Report_FoundMatchesFor + string.Join(", ", hits.Select(s => "\'" + s + "\'"));
                    }
                    if (misses.Any())
                    {
                        if (!string.IsNullOrEmpty(orResults))
                            orResults += Environment.NewLine;
                        orResults += Resources.Report_FoundNoMatchesFor + string.Join(", ", misses.Select(s => "\'" + s + "\'"));
                    }
                }

                // this call to SearchResults can be expensive if the results are not yet cached
                var searchResults = result.SearchResults;
                if (searchResults != null)
                {
                    int matchCount = result.Matches.Count;
                    var lineCount = result.Matches.Where(r => r.LineNumber > 0)
                        .Select(r => r.LineNumber).Distinct().Count();

                    if (previousFileName != result.FileNameDisplayed)
                    {
                        sb.AppendLine("--------------------------------------------------------------------------------")
                          .AppendLine()
                          .AppendLine(result.FileNameDisplayed);
                        previousFileName = result.FileNameDisplayed;
                    }

                    if (!string.IsNullOrEmpty(result.AdditionalInformation))
                    {
                        sb.Append(result.AdditionalInformation.Trim()).Append(' ');
                    }
                    sb.AppendFormat(Resources.Report_Has0MatchesOn1Lines, matchCount, lineCount);

                    if (!string.IsNullOrEmpty(orResults))
                        sb.AppendLine().Append(orResults);

                    if (searchResults.Any())
                    {
                        int prevPageNum = -1;
                        int prevLineNum = -1;
                        foreach (var line in searchResults)
                        {
                            if (line.PageNumber != prevPageNum)
                            {
                                sb.AppendLine().Append(Resources.Report_Page).Append(' ').AppendLine(line.PageNumber.ToString());
                                prevPageNum = line.PageNumber;
                            }
                            else if (line.LineNumber != prevLineNum + 1)
                            {
                                // Adding separator
                                sb.AppendLine();
                            }

                            var formattedLineNumber = line.LineNumber == -1 ? string.Empty :
                                line.IsHexFile ? string.Format("{0:X8}", (line.LineNumber - 1) * hexLineSize) :
                                line.LineNumber.ToString().PadLeft(6, ' ');

                            sb.Append(formattedLineNumber).Append(": ").AppendLine(line.LineText);
                            prevLineNum = line.LineNumber;
                        }
                    }
                    else
                    {
                        sb.AppendLine(Resources.Report_FileNotFoundHasItBeenDeletedOrMoved);
                    }
                }
            }
            return sb.ToString();
        }

        private static string GetFullLinesAsText(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            int lineCount = 0;
            StringBuilder sb = new();

            foreach (GrepSearchResult result in source)
            {
                if (options.IncludeFileInformation)
                {
                    sb.AppendLine(result.FileNameDisplayed);
                    lineCount++;
                }

                if (result.SearchResults != null)
                {
                    foreach (GrepLine line in result.SearchResults.Where(l => !l.IsContext))
                    {
                        if (options.IncludeFileInformation)
                        {
                            var formattedLineNumber = line.LineNumber == -1 ? string.Empty :
                                line.IsHexFile ? string.Format("{0:X8}", (line.LineNumber - 1) * options.HexLineSize) :
                                line.LineNumber.ToString().PadLeft(6, ' ');

                            sb.Append(formattedLineNumber).Append(": ");
                        }

                        sb.AppendLine(options.TrimWhitespace ? line.LineText.Trim() : line.LineText);
                        lineCount++;

                        if (limit > -1 && lineCount > limit)
                        {
                            break;
                        }
                    }
                }

                if (limit > -1 && lineCount > limit)
                {
                    sb.AppendLine("…");
                    break;
                }
            }
            return sb.ToString();
        }

        private static string GetMatchesAsText(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            int lineCount = 0;
            var separator = options.ListItemSeparator ?? string.Empty;

            StringBuilder sb = new();

            foreach (GrepSearchResult result in source)
            {
                bool firstOne = true;

                if (options.IncludeFileInformation)
                {
                    sb.AppendLine(result.FileNameDisplayed);
                    lineCount++;
                }

                if (result.SearchResults != null)
                {
                    bool mergeLines = !options.IncludeFileInformation && !options.OutputOnSeparateLines;

                    foreach (GrepLine line in result.SearchResults.Where(l => !l.IsContext))
                    {
                        int length = line.Matches.Count;
                        int count = 0;

                        foreach (GrepMatch match in line.Matches)
                        {
                            if (options.IncludeFileInformation && firstOne)
                            {
                                var formattedLineNumber = line.LineNumber == -1 ? string.Empty :
                                    line.IsHexFile ? string.Format("{0:X8}", (line.LineNumber - 1) * options.HexLineSize) :
                                    line.LineNumber.ToString().PadLeft(6, ' ');

                                sb.Append(formattedLineNumber).Append(": ");
                            }

                            string text = line.LineText.Substring(match.StartLocation, match.Length);
                            sb.Append(options.TrimWhitespace ? text.Trim() : text);
                            count++;

                            if (options.OutputOnSeparateLines)
                            {
                                sb.Append(Environment.NewLine);
                                lineCount++;
                            }
                            else if (mergeLines || count < length)
                            {
                                sb.Append(separator);
                                firstOne = false;
                            }

                            if (limit > -1 && lineCount > limit)
                            {
                                break;
                            }
                        }

                        // end of line
                        if (!mergeLines && !options.OutputOnSeparateLines)
                        {
                            sb.Append(Environment.NewLine);
                            firstOne = true;
                            lineCount++;
                        }

                        if (limit > -1 && lineCount > limit)
                        {
                            break;
                        }
                    }

                    // end of file
                    if (mergeLines)
                    {
                        sb.Remove(sb.Length - separator.Length, separator.Length);
                        sb.Append(Environment.NewLine);
                        firstOne = true;
                        lineCount++;
                    }
                }

                if (limit > -1 && lineCount > limit)
                {
                    sb.AppendLine("…");
                    break;
                }
            }
            return sb.ToString();
        }

        private static string GetGroupsAsText(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            int lineCount = 0;
            var separator = options.ListItemSeparator ?? string.Empty;

            StringBuilder sb = new();

            foreach (GrepSearchResult result in source)
            {
                bool firstOne = true;

                if (options.IncludeFileInformation)
                {
                    bool fileHasGroups = result.SearchResults
                        .SelectMany(s => s.Matches.Where(m => m.Groups.Any())).Any();

                    if (fileHasGroups)
                    {
                        sb.AppendLine(result.FileNameDisplayed);
                        lineCount++;
                    }
                }

                if (result.SearchResults.Any())
                {
                    bool mergeLines = !options.IncludeFileInformation && !options.OutputOnSeparateLines;

                    foreach (GrepLine line in result.SearchResults.Where(l => !l.IsContext))
                    {
                        int length = line.Matches.SelectMany(m => m.Groups).Count();
                        int count = 0;
                        bool lineHasGroups = false;

                        foreach (GrepMatch match in line.Matches.Where(m => m.Groups.Any()))
                        {
                            lineHasGroups = true;
                            foreach (GrepCaptureGroup group in match.Groups)
                            {
                                if (options.IncludeFileInformation && firstOne)
                                {
                                    var formattedLineNumber = line.LineNumber == -1 ? string.Empty :
                                        line.IsHexFile ? string.Format("{0:X8}", (line.LineNumber - 1) * options.HexLineSize) :
                                        line.LineNumber.ToString().PadLeft(6, ' ');

                                    sb.Append(formattedLineNumber).Append(": ");
                                }

                                sb.Append(options.TrimWhitespace ? group.Value.Trim() : group.Value);
                                count++;

                                if (options.OutputOnSeparateLines)
                                {
                                    sb.Append(Environment.NewLine);
                                    lineCount++;
                                }
                                else if (mergeLines || count < length)
                                {
                                    sb.Append(separator);
                                    firstOne = false;
                                }

                                if (limit > -1 && lineCount > limit)
                                {
                                    break;
                                }
                            }

                            if (limit > -1 && lineCount > limit)
                            {
                                break;
                            }
                        }

                        // end of line
                        if (lineHasGroups && !mergeLines && !options.OutputOnSeparateLines)
                        {
                            sb.Append(Environment.NewLine);
                            firstOne = true;
                            lineCount++;
                        }

                        if (limit > -1 && lineCount > limit)
                        {
                            break;
                        }
                    }

                    // end of file
                    if (mergeLines)
                    {
                        sb.Remove(sb.Length - separator.Length, separator.Length);
                        sb.Append(Environment.NewLine);
                        firstOne = true;
                        lineCount++;
                    }
                }

                if (limit > -1 && lineCount > limit)
                {
                    sb.AppendLine("…");
                    break;
                }
            }
            return sb.ToString();
        }

        private static string GetUniqueValuesAsText(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            Dictionary<string, List<string>> values =
                options.ReportMode == ReportMode.Matches ?
                GetUniqueMatches(source, options, limit) :
                GetUniqueGroups(source, options, limit);

            var separator = options.ListItemSeparator ?? string.Empty;

            StringBuilder sb = new();
            foreach (var file in values.Keys)
            {
                bool firstOne = true;

                int length = values[file].Count;
                int count = 0;

                foreach (var value in values[file])
                {
                    if (file != GLOBAL && firstOne && options.IncludeFileInformation)
                    {
                        sb.AppendLine(file);
                        firstOne = false;
                    }

                    sb.Append(value);
                    count++;

                    if (options.OutputOnSeparateLines)
                    {
                        sb.Append(Environment.NewLine);
                    }
                    else if (count < length)
                    {
                        sb.Append(separator);
                    }
                }

                // end of file
                if (!options.OutputOnSeparateLines)
                {
                    sb.Append(Environment.NewLine);
                }
            }
            if (limit > 0 && source.Count > 2)
            {
                sb.Append('…');
            }
            return sb.ToString();
        }

        private static Dictionary<string, List<string>> GetUniqueMatches(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            int fileCount = 0;
            HashSet<string> unique = new();
            Dictionary<string, List<string>> resultSet = new();
            List<string> values = new();

            if (options.UniqueScope == UniqueScope.Global)
            {
                resultSet.Add(GLOBAL, values);
            }

            foreach (GrepSearchResult result in source)
            {
                if (options.UniqueScope == UniqueScope.PerFile)
                {
                    unique.Clear();
                    values = new List<string>();
                    resultSet.Add(result.FileNameDisplayed, values);
                }

                if (result.SearchResults != null)
                {
                    foreach (GrepLine line in result.SearchResults.Where(l => !l.IsContext))
                    {
                        foreach (GrepMatch match in line.Matches)
                        {
                            string text = line.LineText.Substring(match.StartLocation, match.Length);
                            if (options.TrimWhitespace)
                            {
                                text = text.Trim();
                            }
                            if (!unique.Contains(text))
                            {
                                unique.Add(text);
                                values.Add(text);

                                if (limit > -1 && values.Count > limit)
                                {
                                    break;
                                }
                            }

                            if (limit > -1 && values.Count > limit)
                            {
                                break;
                            }
                        }

                        if (limit > -1 && values.Count > limit)
                        {
                            break;
                        }
                    }
                }

                if (limit > -1 && ++fileCount == 2)
                {
                    break;
                }
            }

            return resultSet;
        }

        private static Dictionary<string, List<string>> GetUniqueGroups(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            int fileCount = 0;
            HashSet<string> unique = new();
            Dictionary<string, List<string>> resultSet = new();
            List<string> values = new();

            if (options.UniqueScope == UniqueScope.Global)
            {
                resultSet.Add(GLOBAL, values);
            }

            foreach (GrepSearchResult result in source)
            {
                if (options.UniqueScope == UniqueScope.PerFile)
                {
                    unique.Clear();
                    values = new List<string>();
                    resultSet.Add(result.FileNameDisplayed, values);
                }

                if (result.SearchResults != null)
                {
                    foreach (GrepLine line in result.SearchResults.Where(l => !l.IsContext))
                    {
                        foreach (var match in result.Matches)
                        {
                            foreach (var group in match.Groups)
                            {
                                string text = options.TrimWhitespace ? group.Value.Trim() : group.Value;
                                if (!unique.Contains(text))
                                {
                                    unique.Add(text);
                                    values.Add(text);

                                    if (limit > -1 && values.Count > limit)
                                    {
                                        break;
                                    }
                                }
                            }

                            if (limit > -1 && values.Count > limit)
                            {
                                break;
                            }
                        }

                        if (limit > -1 && values.Count > limit)
                        {
                            break;
                        }
                    }
                }

                if (limit > -1 && ++fileCount == 2)
                {
                    break;
                }
            }

            return resultSet;
        }

        private static string GetFullLinesAsCSV(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            int lineCount = 0;
            StringBuilder sb = new();

            if (options.IncludeFileInformation)
            {
                sb.AppendLine(Resources.Report_CSVRecordHeaderText);
            }

            foreach (GrepSearchResult result in source)
            {
                if (result.SearchResults == null)
                {
                    sb.AppendLine(Quote(result.FileNameDisplayed));
                    lineCount++;
                }
                else
                {
                    foreach (GrepLine line in result.SearchResults.Where(l => !l.IsContext))
                    {
                        // column 1: file name
                        if (options.IncludeFileInformation)
                        {
                            sb.Append(Quote(result.FileNameDisplayed)).Append(',');

                            // column 2: line number
                            sb.Append(line.LineNumber).Append(',');
                        }

                        sb.AppendLine(Quote(options.TrimWhitespace ? line.LineText.Trim() : line.LineText));
                        lineCount++;

                        if (limit > -1 && lineCount > limit)
                        {
                            break;
                        }
                    }
                }

                if (limit > -1 && lineCount > limit)
                {
                    sb.AppendLine("…");
                    break;
                }
            }
            return sb.ToString();
        }

        private static string GetMatchesAsCSV(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            var separator = options.ListItemSeparator ?? string.Empty;

            int lineCount = 0;
            StringBuilder sb = new();

            if (options.IncludeFileInformation)
            {
                sb.AppendLine(Resources.Report_CSVRecordHeaderText);
                lineCount++;
            }

            foreach (GrepSearchResult result in source)
            {
                if (!result.SearchResults.Any() && options.IncludeFileInformation)
                {
                    sb.AppendLine(Quote(result.FileNameDisplayed));
                    lineCount++;
                }
                else
                {
                    bool mergeLines = !options.IncludeFileInformation && !options.OutputOnSeparateLines;

                    foreach (GrepLine line in result.SearchResults.Where(l => !l.IsContext))
                    {
                        bool firstOne = true;

                        int length = line.Matches.Count;
                        int count = 0;

                        foreach (GrepMatch match in line.Matches)
                        {
                            if (firstOne)
                            {
                                // column 1: file name
                                if (options.IncludeFileInformation)
                                {
                                    sb.Append(Quote(result.FileNameDisplayed)).Append(',');

                                    // column 2: line number
                                    sb.Append(line.LineNumber).Append(',');
                                }
                            }

                            // column 3: data
                            string matchText = line.LineText.Substring(match.StartLocation, match.Length);
                            sb.Append(Quote(options.TrimWhitespace ? matchText.Trim() : matchText));
                            count++;

                            if (options.OutputOnSeparateLines)
                            {
                                sb.Append(Environment.NewLine);
                                lineCount++;
                                firstOne = true;
                            }
                            else if (mergeLines || count < length)
                            {
                                sb.Append(separator);
                                firstOne = false;
                            }

                            if (limit > -1 && lineCount > limit)
                            {
                                break;
                            }
                        }

                        // end of line
                        if (!mergeLines && !options.OutputOnSeparateLines)
                        {
                            sb.Append(Environment.NewLine);
                            lineCount++;
                        }

                        if (limit > -1 && lineCount > limit)
                        {
                            break;
                        }
                    }

                    // end of file
                    if (mergeLines)
                    {
                        sb.Remove(sb.Length - separator.Length, separator.Length);
                        sb.Append(Environment.NewLine);
                        lineCount++;
                    }

                    if (limit > -1 && lineCount > limit)
                    {
                        break;
                    }
                }

                if (limit > -1 && lineCount > limit)
                {
                    sb.AppendLine("…");
                    break;
                }
            }
            return sb.ToString();
        }

        private static string GetGroupsAsCSV(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            var separator = options.ListItemSeparator ?? string.Empty;

            int lineCount = 0;
            StringBuilder sb = new();

            if (options.IncludeFileInformation)
            {
                sb.AppendLine(Resources.Report_CSVRecordHeaderText);
                lineCount++;
            }

            foreach (GrepSearchResult result in source)
            {
                if (!result.SearchResults.Any() && options.IncludeFileInformation)
                {
                    sb.AppendLine(Quote(result.FileNameDisplayed));
                    lineCount++;
                }
                else
                {
                    bool mergeLines = !options.IncludeFileInformation && !options.OutputOnSeparateLines;

                    foreach (GrepLine line in result.SearchResults.Where(l => !l.IsContext))
                    {
                        bool firstOne = true;

                        int length = line.Matches.SelectMany(m => m.Groups).Count();
                        int count = 0;
                        bool lineHasGroups = false;

                        foreach (GrepMatch match in line.Matches.Where(m => m.Groups.Any()))
                        {
                            lineHasGroups = true;
                            foreach (GrepCaptureGroup group in match.Groups)
                            {
                                if (firstOne)
                                {
                                    // column 1: file name
                                    if (options.IncludeFileInformation)
                                    {
                                        sb.Append(Quote(result.FileNameDisplayed)).Append(',');

                                        // column 2: line number
                                        sb.Append(line.LineNumber).Append(',');
                                    }
                                }

                                // column 3: data
                                sb.Append(Quote(options.TrimWhitespace ? group.Value.Trim() : group.Value));
                                count++;

                                if (options.OutputOnSeparateLines)
                                {
                                    sb.Append(Environment.NewLine);
                                    lineCount++;
                                    firstOne = true;
                                }
                                else if (mergeLines || count < length)
                                {
                                    sb.Append(separator);
                                    firstOne = false;
                                }

                                if (limit > -1 && lineCount > limit)
                                {
                                    break;
                                }
                            }

                            if (limit > -1 && lineCount > limit)
                            {
                                break;
                            }
                        }

                        // end of line
                        if (lineHasGroups && !mergeLines && !options.OutputOnSeparateLines)
                        {
                            sb.Append(Environment.NewLine);
                            lineCount++;
                        }

                        if (limit > -1 && lineCount > limit)
                        {
                            break;
                        }
                    }

                    // end of file
                    if (mergeLines)
                    {
                        sb.Remove(sb.Length - separator.Length, separator.Length);
                        sb.Append(Environment.NewLine);
                        lineCount++;
                    }
                }

                if (limit > -1 && lineCount > limit)
                {
                    sb.AppendLine("…");
                    break;
                }
            }
            return sb.ToString();
        }

        private static string GetUniqueValuesAsCSV(List<GrepSearchResult> source, ReportOptions options, int limit)
        {
            Dictionary<string, List<string>> values =
                options.ReportMode == ReportMode.Matches ?
                GetUniqueMatches(source, options, limit) :
                GetUniqueGroups(source, options, limit);

            var separator = options.ListItemSeparator ?? string.Empty;

            StringBuilder sb = new();
            sb.AppendLine(Resources.Report_CSVRecordHeaderText);

            foreach (var file in values.Keys)
            {
                bool firstOne = true;

                int length = values[file].Count;
                int count = 0;

                foreach (var value in values[file])
                {
                    if (firstOne)
                    {
                        // column 1: file name
                        if (file != GLOBAL && options.IncludeFileInformation)
                        {
                            sb.Append(Quote(file));
                        }
                        sb.Append(',');

                        // column 2: no line number
                        sb.Append(',');
                    }

                    // column 3: data
                    sb.Append(Quote(value));
                    count++;

                    if (options.OutputOnSeparateLines)
                    {
                        sb.Append(Environment.NewLine);
                        firstOne = true;
                    }
                    else if (count < length)
                    {
                        sb.Append(separator);
                        firstOne = false;
                    }
                }

                // end of file
                if (!options.OutputOnSeparateLines)
                {
                    sb.Append(Environment.NewLine);
                }

                if (limit > 0 && source.Count > 2)
                {
                    sb.Append('…');
                    break;
                }
            }
            return sb.ToString();
        }
    }
}
