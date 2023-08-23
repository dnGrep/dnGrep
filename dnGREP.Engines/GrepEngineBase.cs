using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using dnGREP.Common;

namespace dnGREP.Engines
{
    public class GrepEngineBase
    {
#pragma warning disable IDE0060
        private readonly string KEYWORD_GUID_LOWER = "$(guid)";
        private readonly string KEYWORD_GUID_UPPER = "$(GUID)";
        private readonly string KEYWORD_GUIDX = "$(guidx)";
        protected GrepEngineInitParams initParams = GrepEngineInitParams.Default;
        private GoogleMatch? fuzzyMatchEngine;

        private static readonly ConcurrentDictionary<string, string> guidxMatches = new();
        internal static void ResetGuidxCache() => guidxMatches.Clear();


        public GrepEngineBase()
        {
        }

        public virtual bool Initialize(GrepEngineInitParams param, FileFilter filter)
        {
            initParams = param;
            FileFilter = filter;
            return true;
        }

        public FileFilter FileFilter { get; protected set; } = FileFilter.Default;

        public int LinesAfter => initParams.LinesAfter;
        public int LinesBefore => initParams.LinesBefore;

        public virtual void OpenFile(OpenFileArgs args)
        {
            Utils.OpenFile(args);
        }

        private static List<int> GetLineEndIndexes(string text, PauseCancelToken pauseCancelToken)
        {
            List<int> list = new();

            if (!string.IsNullOrWhiteSpace(text))
            {
                int idx = 0;
                while (idx > -1 && idx < text.Length)
                {
                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                    idx = text.IndexOfAny(new char[] { '\r', '\n' }, idx);
                    if (idx == -1)
                    {
                        list.Add(text.Length);
                        break;
                    }
                    else
                    {
                        list.Add(idx);

                        idx++;
                        if (idx < text.Length && text[idx - 1] == '\r' && text[idx] == '\n')
                            idx++;
                    }
                }
            }
            return list;
        }

        private string DoPatternReplacement(string searchPattern, string replaceText)
        {
            if (replaceText.Contains(KEYWORD_GUID_LOWER, StringComparison.Ordinal))
                return replaceText.Replace(KEYWORD_GUID_LOWER, Guid.NewGuid().ToString(), StringComparison.Ordinal);
            if (replaceText.Contains(KEYWORD_GUID_UPPER, StringComparison.Ordinal))
                return replaceText.Replace(KEYWORD_GUID_UPPER, Guid.NewGuid().ToString().ToUpper(), StringComparison.Ordinal);
            if (replaceText.Contains(KEYWORD_GUIDX, StringComparison.Ordinal))
            {
                string guidx = guidxMatches.GetOrAdd(searchPattern, (key) => Guid.NewGuid().ToString());
                return replaceText.Replace(KEYWORD_GUIDX, guidx, StringComparison.Ordinal);
            }
            else
                return replaceText;
        }

        #region Regex Search and Replace

        private IEnumerable<GrepMatch> RegexSearchIterator(int lineNumber, int filePosition, string text,
            string searchPattern, GrepSearchOption searchOptions, PauseCancelToken pauseCancelToken)
        {
            RegexOptions regexOptions = RegexOptions.None;
            if (!searchOptions.HasFlag(GrepSearchOption.CaseSensitive))
                regexOptions |= RegexOptions.IgnoreCase;
            if (searchOptions.HasFlag(GrepSearchOption.Multiline))
                regexOptions |= RegexOptions.Multiline;
            if (searchOptions.HasFlag(GrepSearchOption.SingleLine))
                regexOptions |= RegexOptions.Singleline;

            bool isWholeWord = searchOptions.HasFlag(GrepSearchOption.WholeWord);

            if (searchOptions.HasFlag(GrepSearchOption.BooleanOperators))
            {
                BooleanExpression exp = new();
                if (exp.TryParse(searchPattern))
                {
                    return RegexSearchIteratorBoolean(lineNumber, filePosition,
                        text, exp, isWholeWord, regexOptions, pauseCancelToken);
                }
            }

            return RegexSearchIterator(lineNumber, filePosition, text, searchPattern, isWholeWord, regexOptions, pauseCancelToken);
        }

        private IEnumerable<GrepMatch> RegexSearchIterator(int lineNumber, int filePosition, string text,
            string searchPattern, bool isWholeWord, RegexOptions regexOptions, PauseCancelToken pauseCancelToken)
        {
            if (isWholeWord)
            {
                // Issue #813
                searchPattern = $@"(?<=\W|\b|^)({searchPattern})(?=\W|\b|$)";
            }

            // Issue #210 .net regex will only match the $ end of line token with a \n, not \r\n or \r
            // see https://msdn.microsoft.com/en-us/library/yd1hzczs.aspx#Multiline
            // http://stackoverflow.com/questions/8618557/why-doesnt-in-net-multiline-regular-expressions-match-crlf
            // https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference

            // Note: in Singleline mode, need to capture the new line chars

            bool searchPatternEndsWithDot =
                (searchPattern.EndsWith(".", StringComparison.Ordinal) && !searchPattern.EndsWith(@"\.", StringComparison.Ordinal)) ||
                (searchPattern.EndsWith(".*", StringComparison.Ordinal) && !searchPattern.EndsWith(@"\.*", StringComparison.Ordinal)) ||
                (searchPattern.EndsWith(".?", StringComparison.Ordinal) && !searchPattern.EndsWith(@"\.?", StringComparison.Ordinal)) ||
                (searchPattern.EndsWith(".+", StringComparison.Ordinal) && !searchPattern.EndsWith(@"\.+", StringComparison.Ordinal));

            string textToSearch = text;
            bool convertedFromWindowsNewline = false;
            List<int>? newlineIndexes = null;

            if (searchPattern.ConstainsNotEscaped("$") || searchPatternEndsWithDot)
            {
                if (text.Contains("\r\n", StringComparison.Ordinal))
                {
                    // the match index will be off by one for each line where the \r was dropped
                    searchPattern = searchPattern.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
                    textToSearch = ConvertNewLines(text, out newlineIndexes, pauseCancelToken);
                    convertedFromWindowsNewline = true;
                }
                else if (text.Contains('\r', StringComparison.Ordinal))
                {
                    // this will be the same length, just change the newline char while searching
                    searchPattern = searchPattern.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
                    textToSearch = text.Replace('\r', '\n');
                }
            }

            var lineEndIndexes = GetLineEndIndexes((initParams.VerboseMatchCount && lineNumber == -1) ? textToSearch : string.Empty, pauseCancelToken);

            List<GrepMatch> globalMatches = new();

            Regex regex = new(searchPattern, regexOptions, GrepCore.MatchTimeout);
            var matches = regex.Matches(textToSearch);
            foreach (Match match in matches.Cast<Match>())
            {
                if (match.Length < 1)
                {
                    continue;
                }

                if (initParams.VerboseMatchCount && lineEndIndexes.Count > 0)
                    lineNumber = lineEndIndexes.FindIndex(i => i > match.Index) + 1;

                int matchStart = match.Index;
                int length = match.Length;
                if (convertedFromWindowsNewline && newlineIndexes != null)
                {
                    // since the search text is shorter by one for each converted newline,
                    // move the match start by one for each converted Windows newline
                    matchStart += CountWindowsNewLines(0, match.Index, newlineIndexes);
                    length += CountWindowsNewLines(match.Index, match.Index + length, newlineIndexes);
                }

                var grepMatch = new GrepMatch(searchPattern, lineNumber, matchStart + filePosition, length);

                if (match.Groups.Count > 1)
                {
                    // Note that group 0 is always the whole match
                    for (int idx = 1; idx < match.Groups.Count; idx++)
                    {
                        var group = match.Groups[idx];
                        if (group.Success)
                        {
                            length = group.Length;
                            if (!regexOptions.HasFlag(RegexOptions.Singleline) && group.Value.EndsWith("\r", StringComparison.Ordinal))
                                length -= 1;

                            grepMatch.Groups.Add(
                                new GrepCaptureGroup(regex.GroupNameFromNumber(idx), group.Index, length, group.Value));
                        }
                    }
                }

                yield return grepMatch;
            }
        }

        /// <summary>
        /// Counts the number of Windows newlines that were converted to Unix newlines
        /// between the start and end indexes
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="newlineIndexes"></param>
        /// <returns></returns>
        private static int CountWindowsNewLines(int startIndex, int endIndex, List<int> newlineIndexes)
        {
            var count = newlineIndexes.Count(idx => startIndex < idx && endIndex >= idx);
            return count;
        }

        /// <summary>
        /// Converts \r\n to \n and stores the index of the output string where
        /// each conversion was done.
        /// </summary>
        /// <remarks>
        /// Need to know which line ends were converted to handle mixed Windows and Unix newlines.
        /// </remarks>
        /// <param name="text">input text with Windows newlines</param>
        /// <param name="newlineIndexes">the indexes where newlines were converted</param>
        /// <returns>output text with Unix newlines</returns>
        private static string ConvertNewLines(string text, out List<int> newlineIndexes, PauseCancelToken pauseCancelToken)
        {
            newlineIndexes = new List<int>();
            string output = string.Empty;
            int start = 0, pos;
            while (start < text.Length)
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                pos = text.IndexOf("\r\n", start, StringComparison.Ordinal);
                if (pos > -1)
                {
                    output += string.Concat(text.AsSpan(start, pos - start), "\n");
                    newlineIndexes.Add(output.Length);
                    start = pos + 2;
                }
                else
                {
                    output += text[start..];
                    break;
                }
            }
            return output.Replace('\r', '\n'); // for that rare case
        }

        private IEnumerable<GrepMatch> RegexSearchIteratorBoolean(int lineNumber, int filePosition, string text,
            BooleanExpression expression, bool isWholeWord, RegexOptions regexOptions, PauseCancelToken pauseCancelToken)
        {
            List<GrepMatch> results = new();
            foreach (var operand in expression.Operands)
            {
                var matches = RegexSearchIterator(lineNumber, filePosition, text,
                    operand.Value, isWholeWord, regexOptions, pauseCancelToken);

                operand.EvaluatedResult = matches.Any();
                operand.Matches = matches.ToList();

                if (!expression.IsComplete && expression.IsShortCircuitFalse())
                {
                    return results;
                }
            }

            if (expression.IsComplete)
            {
                var evalResult = expression.Evaluate();
                if (evalResult == EvaluationResult.True)
                {
                    foreach (var operand in expression.Operands)
                    {
                        if (operand.Matches != null)
                        {
                            results.AddRange(operand.Matches);
                        }
                    }

                    // if the expression evaluated to true, then the expression is searching
                    // for things that do not match the operands. Return the whole line
                    if (results.Count == 0)
                    {
                        string temp = text.TrimEndOfLine();
                        results.Add(new GrepMatch(string.Empty, lineNumber, filePosition, temp.Length));
                    }

                    GrepMatch.Normalize(results);
                }
            }
            return results;
        }

        protected List<GrepMatch> DoRegexSearch(int lineNumber, int filePosition, string text, string searchPattern,
            GrepSearchOption searchOptions, bool includeContext, PauseCancelToken pauseCancelToken)
        {
            List<GrepMatch> globalMatches = new();

            foreach (var match in RegexSearchIterator(lineNumber, filePosition, text, searchPattern, searchOptions, pauseCancelToken))
            {
                globalMatches.Add(match);

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
            }

            return globalMatches;
        }

        protected string DoRegexReplace(int lineNumber, int filePosition, string text, string searchPattern, string replacePattern,
            GrepSearchOption searchOptions, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken)
        {
            string result = text;

            RegexOptions regexOptions = RegexOptions.None;
            if (!searchOptions.HasFlag(GrepSearchOption.CaseSensitive))
                regexOptions |= RegexOptions.IgnoreCase;
            if (searchOptions.HasFlag(GrepSearchOption.Multiline))
                regexOptions |= RegexOptions.Multiline;
            if (searchOptions.HasFlag(GrepSearchOption.SingleLine))
                regexOptions |= RegexOptions.Singleline;

            bool isWholeWord = searchOptions.HasFlag(GrepSearchOption.WholeWord);

            if (isWholeWord)
            {
                if (!searchPattern.Trim().StartsWith("\\b", StringComparison.Ordinal))
                    searchPattern = "\\b" + searchPattern.Trim();
                if (!searchPattern.Trim().EndsWith("\\b", StringComparison.Ordinal))
                    searchPattern = searchPattern.Trim() + "\\b";
            }

            // check this block of text for any matches that are marked for replace
            // just return the original text if not
            var matches = RegexSearchIterator(lineNumber, filePosition, text, searchPattern, searchOptions, pauseCancelToken);
            var toDo = replaceItems.Intersect(matches);
            if (toDo.Any(r => r.ReplaceMatch))
            {
                // Issue #210 .net regex will only match the $ end of line token with a \n, not \r\n or \r
                bool convertToWindowsNewline = false;
                string searchPatternForReplace = searchPattern;
                if (searchPattern.ConstainsNotEscaped("$") && text.Contains("\r\n", StringComparison.Ordinal))
                {
                    convertToWindowsNewline = true;
                    searchPatternForReplace = searchPattern.Replace("\r\n", "\n", StringComparison.Ordinal);
                    replacePattern = replacePattern.Replace("\r\n", "\n", StringComparison.Ordinal);
                    text = text.Replace("\r\n", "\n", StringComparison.Ordinal);
                }

                // because we're possibly altering the new line chars, the match.Index in Regex.Replace
                // may not match the startPos in the GrepMatch, but the order of the matches should be 
                // the same.  So get the indexes of matches to change in this text block
                var indexes = toDo.Select((item, index) => new { item, index }).Where(t => t.item.ReplaceMatch).Select(t => t.index).ToList();

                int matchIndex = 0;
                string replaceText = Regex.Replace(text, searchPatternForReplace, (match) =>
                    {
                        if (indexes.Contains(matchIndex++))
                        {
                            var pattern = DoPatternReplacement(match.Value, replacePattern);
                            return match.Result(pattern);
                        }
                        else
                        {
                            return match.Value;
                        }
                    },
                    regexOptions, GrepCore.MatchTimeout);

                if (convertToWindowsNewline)
                    replaceText = replaceText.Replace("\n", "\r\n", StringComparison.Ordinal);

                result = replaceText;
            }

            return result;
        }

        #endregion

        #region Text Search and Replace

        protected List<GrepMatch> DoTextSearch(int lineNumber, int filePosition, string text,
            string searchText, GrepSearchOption searchOptions, bool includeContext,
            PauseCancelToken pauseCancelToken)
        {
            List<GrepMatch> globalMatches = new();

            foreach (var match in TextSearchIterator(lineNumber, filePosition, text, searchText, searchOptions, pauseCancelToken))
            {
                globalMatches.Add(match);

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
            }

            return globalMatches;
        }

        protected string DoTextReplace(int lineNumber, int filePosition, string text, string searchText, string replaceText,
            GrepSearchOption searchOptions, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken)
        {
            if (lineNumber > -1 && !replaceItems.Any(r => r.LineNumber == lineNumber && r.ReplaceMatch))
                return text;

            StringBuilder sb = new();
            int counter = 0;

            foreach (var match in TextSearchIterator(lineNumber, filePosition, text, searchText, searchOptions, pauseCancelToken))
            {
                if (replaceItems.Any(r => match.Equals(r) && r.ReplaceMatch))
                {
                    sb.Append(text[counter..(match.StartLocation - filePosition)]);
                    sb.Append(DoPatternReplacement(searchText, replaceText));

                    counter = match.StartLocation - filePosition + match.Length;
                }

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
            }

            sb.Append(text[counter..]);
            return sb.ToString();
        }

        private IEnumerable<GrepMatch> TextSearchIterator(int lineNumber, int filePosition, string text,
            string searchText, GrepSearchOption searchOptions, PauseCancelToken pauseCancelToken)
        {
            var lineEndIndexes = GetLineEndIndexes(initParams.VerboseMatchCount && lineNumber == -1 ? text : string.Empty, pauseCancelToken);

            bool isWholeWord = searchOptions.HasFlag(GrepSearchOption.WholeWord);
            StringComparison comparisonType = searchOptions.HasFlag(GrepSearchOption.CaseSensitive) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (searchOptions.HasFlag(GrepSearchOption.BooleanOperators))
            {
                BooleanExpression exp = new();
                if (exp.TryParse(searchText))
                {
                    return TextSearchIteratorBoolean(lineNumber, filePosition,
                        text, exp, lineEndIndexes, isWholeWord, comparisonType, pauseCancelToken);
                }
            }

            return TextSearchIterator(lineNumber, filePosition, text, searchText, lineEndIndexes, isWholeWord, comparisonType, pauseCancelToken);
        }

        private IEnumerable<GrepMatch> TextSearchIterator(int lineNumber, int filePosition, string text,
            string searchText, List<int> lineEndIndexes, bool isWholeWord, StringComparison comparisonType,
            PauseCancelToken pauseCancelToken)
        {
            int index = 0;
            while (index >= 0)
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                index = text.IndexOf(searchText, index, comparisonType);
                if (index >= 0)
                {
                    if (isWholeWord && (!Utils.IsValidBeginText(text[..index]) ||
                        !Utils.IsValidEndText(text[(index + searchText.Length)..])))
                    {
                        index++;
                        continue;
                    }

                    if (initParams.VerboseMatchCount && lineEndIndexes.Count > 0)
                    {
                        lineNumber = lineEndIndexes.FindIndex(i => i > index) + 1;
                    }

                    yield return new GrepMatch(searchText, lineNumber, index + filePosition, searchText.Length);
                    index += searchText.Length;
                }
            }
        }

        private IEnumerable<GrepMatch> TextSearchIteratorBoolean(int lineNumber, int filePosition, string text,
            BooleanExpression expression, List<int> lineEndIndexes, bool isWholeWord, StringComparison comparisonType,
            PauseCancelToken pauseCancelToken)
        {
            List<GrepMatch> results = new();
            foreach (var operand in expression.Operands)
            {
                var matches = TextSearchIterator(lineNumber, filePosition, text, operand.Value,
                    lineEndIndexes, isWholeWord, comparisonType, pauseCancelToken);

                operand.EvaluatedResult = matches.Any();
                operand.Matches = matches.ToList();

                if (!expression.IsComplete && expression.IsShortCircuitFalse())
                {
                    return results;
                }
            }

            if (expression.IsComplete)
            {
                var evalResult = expression.Evaluate();
                if (evalResult == EvaluationResult.True)
                {
                    foreach (var operand in expression.Operands)
                    {
                        if (operand.Matches != null)
                        {
                            results.AddRange(operand.Matches);
                        }
                    }

                    // if the expression evaluated to true, then the expression is searching
                    // for things that do not match the operands. Return the whole line
                    if (results.Count == 0)
                    {
                        string temp = text.TrimEndOfLine();
                        results.Add(new GrepMatch(string.Empty, lineNumber, filePosition, temp.Length));
                    }

                    GrepMatch.Normalize(results);
                }
            }

            return results;
        }

        #endregion

        #region XPath Search and Replace

        protected List<GrepMatch> DoXPathSearch(int lineNumber, int filePosition, string text,
            string searchXPath, GrepSearchOption searchOptions, bool includeContext,
            PauseCancelToken pauseCancelToken)
        {
            List<GrepMatch> results = new();

            // skip files that are obviously not xml files, but report errors on badly formed xml
            int firstElemIdx = text.IndexOf('<', StringComparison.Ordinal);
            int firstCharIdx = text.TakeWhile(char.IsWhiteSpace).Count();
            if (firstElemIdx > -1 && firstElemIdx <= firstCharIdx)
            {
                List<XPathPosition> positions = new();
                using (StringReader reader = new(text))
                {
                    // Code from http://stackoverflow.com/questions/10606534/how-to-search-xml-files-with-xpath-returning-line-and-column-numbers-of-found
                    XPathDocument xmlDoc = new(reader);
                    var xpn = xmlDoc.CreateNavigator();
                    xpn.MoveToFollowing(XPathNodeType.Element);
                    var xns = xpn.GetNamespacesInScope(XmlNamespaceScope.All);
                    var xmngr = new XmlNamespaceManager(xpn.NameTable);
                    foreach (var key in xns.Keys)
                        xmngr.AddNamespace(key, xns[key]);
                    var xpni = xpn.Select(searchXPath, xmngr);

                    int foundCounter = 0;
                    while (xpni.MoveNext())
                    {
                        pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                        foundCounter++;
                        var xn = xpni.Current;

                        var xpathPositions = new XPathPosition();
                        if (xn?.NodeType == XPathNodeType.Attribute)
                        {
                            xpathPositions.EndsOnAttribute = true;
                            xpathPositions.AttributeName = xn.Name;
                        }
                        List<int> currentPositions = new();
                        GetXpathPositions(xn, ref currentPositions, pauseCancelToken);
                        if (xpathPositions.EndsOnAttribute)
                            currentPositions.RemoveAt(currentPositions.Count - 1);
                        xpathPositions.Path = currentPositions;
                        positions.Add(xpathPositions);
                    }
                }

                results.AddRange(GetFilePositions(text, positions, pauseCancelToken));
            }
            return results;
        }

        protected string DoXPathReplace(int lineNumber, int filePosition, string text, string searchXPath, string replaceText, GrepSearchOption searchOptions,
            IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken)
        {
            // shouldn't get here, but skip non-xml files:
            if (text.StartsWith("<", StringComparison.Ordinal))
            {
                XmlDocument xmlDoc = new();
                xmlDoc.LoadXml(text);
                XmlNodeList? xmlNodes = xmlDoc.SelectNodes(searchXPath);

                if (xmlNodes == null)
                {
                    return text;
                }

                // assume the replace items and xml nodes are in the same order
                var replaceList = replaceItems.ToArray();
                bool checkReplace = replaceList.Length == xmlNodes.Count;

                int idx = 0;
                foreach (XmlNode xmlNode in xmlNodes)
                {
                    bool doReplace = !checkReplace || replaceList[idx].ReplaceMatch;

                    if (doReplace)
                        xmlNode.InnerXml = DoPatternReplacement(xmlNode.InnerXml, replaceText);

                    idx++;
                }
                StringBuilder sb = new();
                using (StringWriter stringWriter = new(sb))
                using (XmlTextWriter xmlWriter = new(stringWriter))
                {
                    xmlWriter.Formatting = Formatting.Indented;
                    xmlDoc.WriteContentTo(xmlWriter);
                    xmlWriter.Flush();
                }

                return sb.ToString();
            }
            return text;
        }

        #region XPath helper functions
        public class XPathPosition
        {
            public List<int> Path { get; set; } = new();
            public bool EndsOnAttribute { get; set; }
            public string AttributeName { get; set; } = string.Empty;
        }

        /// <summary>
        /// Evaluates the absolute position of the current node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="positions">Lists the number of node in the according level, including root, that is first element. Positions start at 1.</param>
        private void GetXpathPositions(XPathNavigator? node, ref List<int> positions,
            PauseCancelToken pauseCancelToken)
        {
            int pos = 1;

            while (node?.MoveToPrevious() ?? false)
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                pos++;
            }

            if (node?.MoveToParent() ?? false)
            {
                positions.Insert(0, pos);
                GetXpathPositions(node, ref positions, pauseCancelToken);
            }
        }

        private static int GetAbsoluteCharPosition(int line, int position, string text, List<int> lineLengths, bool leftTrimmed)
        {
            if (line > lineLengths.Count)
                throw new ArgumentException("Error getting absolute char position. Line number too high.");
            int counter = 0;
            for (int i = 0; i < line; i++)
                counter += lineLengths[i];
            if (!leftTrimmed)
                return counter + position;
            else
            {
                for (int i = counter + position; i >= 0; i--)
                {
                    if (text[i] != '\n' && text[i] != '\r' && text[i] != '\t' && text[i] != ' ' && text[i] != '<' && text[i] != '/')
                        return i;
                }
                return 0;
            }
        }


        public static GrepMatch[] GetFilePositions(string text, List<XPathPosition> positions,
            PauseCancelToken pauseCancelToken)
        {
            bool[] endFound = new bool[positions.Count];
            // Getting line lengths
            List<int> lineLengths = new();
            using (StringReader baseReader = new(text))
            {
                using EolReader reader = new(baseReader);
                while (!reader.EndOfStream)
                {
                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                    lineLengths.Add(reader.ReadLine()?.Length ?? 0);
                }
            }
            // These are absolute positions
            GrepMatch[] results = new GrepMatch[positions.Count];
            if (positions.Count == 0)
                return results;

            XmlReaderSettings settings = new()
            {
                DtdProcessing = DtdProcessing.Parse
            };
            using (StringReader textReader = new(text))
            using (XmlReader reader = XmlReader.Create(textReader, settings))
            {
                List<int> currPos = new();

                try
                {
                    IXmlLineInfo lineInfo = (IXmlLineInfo)reader;
                    if (lineInfo.HasLineInfo())
                    {
                        // Parse the XML and display each node.
                        while (reader.Read())
                        {
                            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                            switch (reader.NodeType)
                            {
                                case XmlNodeType.Element:
                                case XmlNodeType.Comment:
                                case XmlNodeType.Text:

                                    if (currPos.Count <= reader.Depth)
                                    {
                                        currPos.Add(1);
                                    }
                                    else
                                    {
                                        currPos[reader.Depth]++;
                                    }

                                    break;

                                case XmlNodeType.EndElement:
                                    while (reader.Depth < currPos.Count - 1)
                                    {
                                        currPos.RemoveAt(reader.Depth + 1); // currPos.Count - 1 would work too.
                                    }

                                    for (int i = 0; i < positions.Count; i++)
                                    {
                                        if (XPathPositionsMatch(currPos, positions[i].Path))
                                        {
                                            endFound[i] = true;
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }

                            if (reader.NodeType == XmlNodeType.EndElement ||
                                reader.NodeType == XmlNodeType.Comment)
                            {
                                int tokenOffset = reader.NodeType == XmlNodeType.Element ? 3 : 5;
                                for (int i = 0; i < positions.Count; i++)
                                {
                                    if (endFound[i] && !XPathPositionsMatch(currPos, positions[i].Path))
                                    {
                                        if (results[i] != null)
                                        {
                                            results[i].EndPosition = GetAbsoluteCharPosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - tokenOffset, text, lineLengths, true) + 1;
                                        }
                                        endFound[i] = false;
                                    }
                                }
                            }

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                for (int i = 0; i < positions.Count; i++)
                                {
                                    if (endFound[i])
                                    {
                                        if (results[i] != null)
                                        {
                                            results[i].EndPosition = GetAbsoluteCharPosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 3, text, lineLengths, true) + 1;
                                        }
                                        endFound[i] = false;
                                    }

                                    if (XPathPositionsMatch(currPos, positions[i].Path))
                                    {
                                        results[i] = new GrepMatch(string.Empty, lineInfo.LineNumber, GetAbsoluteCharPosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 2, text, lineLengths, false), 0);
                                    }

                                    // If empty element (e.g.<element/>)
                                    if (reader.IsEmptyElement)
                                    {
                                        if (XPathPositionsMatch(currPos, positions[i].Path))
                                        {
                                            endFound[i] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    for (int i = 0; i < positions.Count; i++)
                    {
                        if (results[i] != null && results[i].Length == 0)
                            results[i].EndPosition = text.Length - 1;
                    }

                    reader.Close();
                }
                // Close the reader.
            }
            return results;
        }

        private static bool XPathPositionsMatch(List<int> currPos, List<int> positions)
        {
            if (currPos.Count != positions.Count)
            {
                return false; // tree is not yet so deep traversed, like the target node
            }

            for (int i = 0; i < positions.Count; i++)
            {
                if (currPos[i] != positions[i])
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #endregion

        #region Fuzzy Search and Replace

        protected List<GrepMatch> DoFuzzySearch(int lineNumber, int filePosition, string text,
            string searchPattern, GrepSearchOption searchOptions, bool includeContext,
            PauseCancelToken pauseCancelToken)
        {
            List<GrepMatch> globalMatches = new();

            foreach (var match in FuzzySearchIterator(lineNumber, filePosition, text, searchPattern, searchOptions, pauseCancelToken))
            {
                globalMatches.Add(match);

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
            }

            return globalMatches;
        }

        public string DoFuzzyReplace(int lineNumber, int filePosition, string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions,
            IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken)
        {
            if (lineNumber > -1 && !replaceItems.Any(r => r.LineNumber == lineNumber && r.ReplaceMatch))
                return text;

            StringBuilder sb = new();
            int counter = 0;

            foreach (var match in FuzzySearchIterator(lineNumber, filePosition, text, searchPattern, searchOptions, pauseCancelToken))
            {
                if (replaceItems.Any(r => match.Equals(r) && r.ReplaceMatch))
                {
                    sb.Append(text[counter..(match.StartLocation - filePosition)]);
                    sb.Append(DoPatternReplacement(searchPattern, replacePattern));

                    counter = match.StartLocation - filePosition + match.Length;
                }

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
            }

            sb.Append(text[counter..]);
            return sb.ToString();
        }

        private IEnumerable<GrepMatch> FuzzySearchIterator(int lineNumber, int filePosition, string text,
            string searchPattern, GrepSearchOption searchOptions, PauseCancelToken pauseCancelToken)
        {
            var lineEndIndexes = GetLineEndIndexes(initParams.VerboseMatchCount && lineNumber == -1 ? text : string.Empty, pauseCancelToken);

            fuzzyMatchEngine ??= new GoogleMatch();
            fuzzyMatchEngine.Match_Threshold = initParams.FuzzyMatchThreshold;

            bool isWholeWord = searchOptions.HasFlag(GrepSearchOption.WholeWord);

            int counter = 0;
            while (counter < text.Length)
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                int matchLocation = fuzzyMatchEngine.MatchMain(text[counter..], searchPattern, counter);
                if (matchLocation == -1)
                    break;

                if (isWholeWord && !Utils.IsValidBeginText(text[counter..][..matchLocation]))
                {
                    counter = counter + matchLocation + searchPattern.Length;
                    continue;
                }

                int matchLength = GoogleMatch.MatchLength(text[counter..], searchPattern, matchLocation, isWholeWord, initParams.FuzzyMatchThreshold);

                if (matchLength == -1)
                {
                    counter = counter + matchLocation + searchPattern.Length;
                    continue;
                }

                if (initParams.VerboseMatchCount && lineEndIndexes.Count > 0)
                    lineNumber = lineEndIndexes.FindIndex(i => i > matchLocation + counter) + 1;

                yield return new GrepMatch(searchPattern, lineNumber, matchLocation + filePosition + counter, matchLength);

                counter = counter + matchLocation + matchLength;
            }
        }

        #endregion
#pragma warning restore IDE0060

    }

    public class GrepEngineInitParams
    {
        public static GrepEngineInitParams Default { get; } = new();

        public GrepEngineInitParams()
        {
            ShowLinesInContext = false;
            LinesBefore = 0;
            LinesAfter = 0;
            FuzzyMatchThreshold = 0.5f;
            VerboseMatchCount = false;
            // keep the default false for unit tests 
            SearchParallel = false;
        }

        public GrepEngineInitParams(bool showLinesInContext, int linesBefore, int linesAfter, double fuzzyMatchThreshold, bool verboseMatchCount, bool searchParallel)
        {
            ShowLinesInContext = showLinesInContext;
            if (!showLinesInContext)
            {
                LinesBefore = 0;
                LinesAfter = 0;
            }
            else
            {
                LinesBefore = linesBefore;
                LinesAfter = linesAfter;
            }
            FuzzyMatchThreshold = (float)fuzzyMatchThreshold;
            VerboseMatchCount = verboseMatchCount;
            SearchParallel = searchParallel;
        }

        public bool ShowLinesInContext { get; private set; }
        public int LinesBefore { get; private set; }
        public int LinesAfter { get; private set; }
        public float FuzzyMatchThreshold { get; private set; }
        public bool VerboseMatchCount { get; private set; }
        public bool SearchParallel { get; private set; }
    }
}
