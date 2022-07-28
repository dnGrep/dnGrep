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
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Engines
{
    public class GrepEngineBase
    {
        private readonly string KEYWORD_GUID_LOWER = "$(guid)";
        private readonly string KEYWORD_GUID_UPPER = "$(GUID)";
        private readonly string KEYWORD_GUIDX = "$(guidx)";
        protected GrepEngineInitParams initParams = GrepEngineInitParams.Default;
        private GoogleMatch fuzzyMatchEngine;

        private static ConcurrentDictionary<string, string> guidxMatches = new ConcurrentDictionary<string, string>();
        internal static void ResetGuidxCache() => guidxMatches.Clear();

        public static TimeSpan MatchTimeout = TimeSpan.FromSeconds(4.0);

        public GrepEngineBase()
        {
            FileFilter = new FileFilter();
        }

        public GrepEngineBase(GrepEngineInitParams param)
        {
            initParams = param;
        }

        public virtual bool Initialize(GrepEngineInitParams param, FileFilter filter)
        {
            initParams = param;
            FileFilter = filter;
            return true;
        }

        public FileFilter FileFilter { get; protected set; }

        public int LinesAfter => initParams.LinesAfter;
        public int LinesBefore => initParams.LinesBefore;

        public virtual void OpenFile(OpenFileArgs args)
        {
            Utils.OpenFile(args);
        }

        private List<int> GetLineEndIndexes(string text)
        {
            List<int> list = new List<int>();

            if (!string.IsNullOrWhiteSpace(text))
            {
                int idx = 0;
                while (idx > -1 && idx < text.Length)
                {
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
            if (replaceText.Contains(KEYWORD_GUID_LOWER))
                return replaceText.Replace(KEYWORD_GUID_LOWER, Guid.NewGuid().ToString());
            if (replaceText.Contains(KEYWORD_GUID_UPPER))
                return replaceText.Replace(KEYWORD_GUID_UPPER, Guid.NewGuid().ToString().ToUpper());
            if (replaceText.Contains(KEYWORD_GUIDX))
            {
                string guidx = guidxMatches.GetOrAdd(searchPattern, (key) => Guid.NewGuid().ToString());
                return replaceText.Replace(KEYWORD_GUIDX, guidx);
            }
            else
                return replaceText;
        }

        #region Regex Search and Replace

        private IEnumerable<GrepMatch> RegexSearchIterator(int lineNumber, int filePosition, string text,
            string searchPattern, GrepSearchOption searchOptions)
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
                BooleanExpression exp = new BooleanExpression();
                if (exp.TryParse(searchPattern))
                {
                    return RegexSearchIteratorBoolean(lineNumber, filePosition,
                        text, exp, isWholeWord, regexOptions);
                }
            }

            return RegexSearchIterator(lineNumber, filePosition, text, searchPattern, isWholeWord, regexOptions);
        }

        private IEnumerable<GrepMatch> RegexSearchIterator(int lineNumber, int filePosition, string text,
            string searchPattern, bool isWholeWord, RegexOptions regexOptions)
        {
            if (isWholeWord)
            {
                if (!searchPattern.Trim().StartsWith("\\b"))
                    searchPattern = "\\b" + searchPattern.Trim();
                if (!searchPattern.Trim().EndsWith("\\b"))
                    searchPattern = searchPattern.Trim() + "\\b";
            }

            // Issue #210 .net regex will only match the $ end of line token with a \n, not \r\n or \r
            // see https://msdn.microsoft.com/en-us/library/yd1hzczs.aspx#Multiline
            // http://stackoverflow.com/questions/8618557/why-doesnt-in-net-multiline-regular-expressions-match-crlf
            // https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference

            // Note: in Singleline mode, need to capture the new line chars

            bool searchPatternEndsWithDot =
                (searchPattern.EndsWith(".") && !searchPattern.EndsWith(@"\.")) ||
                (searchPattern.EndsWith(".*") && !searchPattern.EndsWith(@"\.*")) ||
                (searchPattern.EndsWith(".?") && !searchPattern.EndsWith(@"\.?")) ||
                (searchPattern.EndsWith(".+") && !searchPattern.EndsWith(@"\.+"));

            string textToSearch = text;
            bool convertedFromWindowsNewline = false;
            List<int> newlineIndexes = null;

            if (searchPattern.ConstainsNotEscaped("$") || searchPatternEndsWithDot)
            {
                if (text.Contains("\r\n"))
                {
                    // the match index will be off by one for each line where the \r was dropped
                    searchPattern = searchPattern.Replace("\r\n", "\n").Replace('\r', '\n');
                    textToSearch = ConvertNewLines(text, out newlineIndexes);
                    convertedFromWindowsNewline = true;
                }
                else if (text.Contains("\r"))
                {
                    // this will be the same length, just change the newline char while searching
                    searchPattern = searchPattern.Replace("\r\n", "\n").Replace('\r', '\n');
                    textToSearch = text.Replace('\r', '\n');
                }
            }

            var lineEndIndexes = GetLineEndIndexes((initParams.VerboseMatchCount && lineNumber == -1) ? textToSearch : null);

            List<GrepMatch> globalMatches = new List<GrepMatch>();

            var regex = new Regex(searchPattern, regexOptions, MatchTimeout);
            var matches = regex.Matches(textToSearch);
            foreach (Match match in matches)
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

                var grepMatch = new GrepMatch(searchPattern, lineNumber, matchStart + filePosition, length, regexOptions.HasFlag(RegexOptions.Multiline));

                if (match.Groups.Count > 1)
                {
                    // Note that group 0 is always the whole match
                    for (int idx = 1; idx < match.Groups.Count; idx++)
                    {
                        var group = match.Groups[idx];
                        if (group.Success)
                        {
                            length = group.Length;
                            if (!regexOptions.HasFlag(RegexOptions.Singleline) && group.Value.EndsWith("\r"))
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
        private int CountWindowsNewLines(int startIndex, int endIndex, List<int> newlineIndexes)
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
        private string ConvertNewLines(string text, out List<int> newlineIndexes)
        {
            newlineIndexes = new List<int>();
            string output = string.Empty;
            int start = 0, pos;
            while (start < text.Length)
            {
                pos = text.IndexOf("\r\n", start);
                if (pos > -1)
                {
                    output += text.Substring(start, pos - start) + "\n";
                    newlineIndexes.Add(output.Length);
                    start = pos + 2;
                }
                else
                {
                    output += text.Substring(start, text.Length - start);
                    break;
                }
            }
            return output.Replace('\r', '\n'); // for that rare case
        }

        private IEnumerable<GrepMatch> RegexSearchIteratorBoolean(int lineNumber, int filePosition, string text,
            BooleanExpression expression, bool isWholeWord, RegexOptions regexOptions)
        {
            List<GrepMatch> results = new List<GrepMatch>();
            foreach (var operand in expression.Operands)
            {
                var matches = RegexSearchIterator(lineNumber, filePosition, text,
                    operand.Value, isWholeWord, regexOptions);

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
                        results.Add(new GrepMatch(string.Empty, lineNumber, filePosition, temp.Length, regexOptions.HasFlag(RegexOptions.Multiline)));
                    }

                    GrepMatch.Normalize(results);
                }
            }
            return results;
        }

        protected List<GrepMatch> DoRegexSearch(int lineNumber, int filePosition, string text, string searchPattern,
            GrepSearchOption searchOptions, bool includeContext)
        {
            List<GrepMatch> globalMatches = new List<GrepMatch>();

            foreach (var match in RegexSearchIterator(lineNumber, filePosition, text, searchPattern, searchOptions))
            {
                globalMatches.Add(match);

                if (Utils.CancelSearch)
                    break;
            }

            return globalMatches;
        }

        protected string DoRegexReplace(int lineNumber, int filePosition, string text, string searchPattern, string replacePattern,
            GrepSearchOption searchOptions, IEnumerable<GrepMatch> replaceItems)
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
                if (!searchPattern.Trim().StartsWith("\\b"))
                    searchPattern = "\\b" + searchPattern.Trim();
                if (!searchPattern.Trim().EndsWith("\\b"))
                    searchPattern = searchPattern.Trim() + "\\b";
            }

            // check this block of text for any matches that are marked for replace
            // just return the original text if not
            var matches = RegexSearchIterator(lineNumber, filePosition, text, searchPattern, searchOptions);
            var toDo = replaceItems.Intersect(matches);
            if (toDo.Any(r => r.ReplaceMatch))
            {
                // Issue #210 .net regex will only match the $ end of line token with a \n, not \r\n or \r
                bool convertToWindowsNewline = false;
                string searchPatternForReplace = searchPattern;
                if (searchPattern.ConstainsNotEscaped("$") && text.Contains("\r\n"))
                {
                    convertToWindowsNewline = true;
                    searchPatternForReplace = searchPattern.Replace("\r\n", "\n");
                    replacePattern = replacePattern.Replace("\r\n", "\n");
                    text = text.Replace("\r\n", "\n");
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
                    regexOptions, MatchTimeout);

                if (convertToWindowsNewline)
                    replaceText = replaceText.Replace("\n", "\r\n");

                result = replaceText;
            }

            return result;
        }

        #endregion

        #region Text Search and Replace

        protected List<GrepMatch> DoTextSearch(int lineNumber, int filePosition, string text, string searchText, GrepSearchOption searchOptions, bool includeContext)
        {
            List<GrepMatch> globalMatches = new List<GrepMatch>();

            foreach (var match in TextSearchIterator(lineNumber, filePosition, text, searchText, searchOptions))
            {
                globalMatches.Add(match);

                if (Utils.CancelSearch)
                    break;
            }

            return globalMatches;
        }

        protected string DoTextReplace(int lineNumber, int filePosition, string text, string searchText, string replaceText,
            GrepSearchOption searchOptions, IEnumerable<GrepMatch> replaceItems)
        {
            if (lineNumber > -1 && !replaceItems.Any(r => r.LineNumber == lineNumber && r.ReplaceMatch))
                return text;

            StringBuilder sb = new StringBuilder();
            int counter = 0;

            foreach (var match in TextSearchIterator(lineNumber, filePosition, text, searchText, searchOptions))
            {
                if (replaceItems.Any(r => match.Equals(r) && r.ReplaceMatch))
                {
                    sb.Append(text.Substring(counter, match.StartLocation - filePosition - counter));
                    sb.Append(DoPatternReplacement(searchText, replaceText));

                    counter = match.StartLocation - filePosition + match.Length;
                }

                if (Utils.CancelSearch)
                    break;
            }

            sb.Append(text.Substring(counter));
            return sb.ToString();
        }

        private IEnumerable<GrepMatch> TextSearchIterator(int lineNumber, int filePosition, string text, string searchText, GrepSearchOption searchOptions)
        {
            var lineEndIndexes = GetLineEndIndexes(initParams.VerboseMatchCount && lineNumber == -1 ? text : null);

            bool isWholeWord = searchOptions.HasFlag(GrepSearchOption.WholeWord);
            StringComparison comparisonType = searchOptions.HasFlag(GrepSearchOption.CaseSensitive) ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

            if (searchOptions.HasFlag(GrepSearchOption.BooleanOperators))
            {
                BooleanExpression exp = new BooleanExpression();
                if (exp.TryParse(searchText))
                {
                    return TextSearchIteratorBoolean(lineNumber, filePosition,
                        text, exp, lineEndIndexes, isWholeWord, comparisonType);
                }
            }

            return TextSearchIterator(lineNumber, filePosition, text, searchText, lineEndIndexes, isWholeWord, comparisonType);
        }

        private IEnumerable<GrepMatch> TextSearchIterator(int lineNumber, int filePosition, string text,
            string searchText, List<int> lineEndIndexes, bool isWholeWord, StringComparison comparisonType)
        {
            bool isMultiline = lineNumber == -1;
            int index = 0;
            while (index >= 0)
            {
                index = text.IndexOf(searchText, index, comparisonType);
                if (index >= 0)
                {
                    if (isWholeWord && (!Utils.IsValidBeginText(text.Substring(0, index)) ||
                        !Utils.IsValidEndText(text.Substring(index + searchText.Length))))
                    {
                        index++;
                        continue;
                    }

                    if (initParams.VerboseMatchCount && lineEndIndexes.Count > 0)
                    {
                        lineNumber = lineEndIndexes.FindIndex(i => i > index) + 1;
                    }

                    yield return new GrepMatch(searchText, lineNumber, index + filePosition, searchText.Length, isMultiline);
                    index += searchText.Length;
                }
            }
        }

        private IEnumerable<GrepMatch> TextSearchIteratorBoolean(int lineNumber, int filePosition, string text,
            BooleanExpression expression, List<int> lineEndIndexes, bool isWholeWord, StringComparison comparisonType)
        {
            bool isMultiline = lineNumber == -1;
            List<GrepMatch> results = new List<GrepMatch>();
            foreach (var operand in expression.Operands)
            {
                var matches = TextSearchIterator(lineNumber, filePosition, text, operand.Value,
                    lineEndIndexes, isWholeWord, comparisonType);

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
                        results.Add(new GrepMatch(string.Empty, lineNumber, filePosition, temp.Length, isMultiline));
                    }

                    GrepMatch.Normalize(results);
                }
            }

            return results;
        }

        #endregion

        #region XPath Search and Replace

        protected List<GrepMatch> DoXPathSearch(int lineNumber, int filePosition, string text, string searchXPath, GrepSearchOption searchOptions, bool includeContext)
        {
            List<GrepMatch> results = new List<GrepMatch>();

            // skip files that are obviously not xml files, but report errors on badly formed xml
            int firstElemIdx = text.IndexOf('<');
            int firstCharIdx = text.TakeWhile(char.IsWhiteSpace).Count();
            if (firstElemIdx > -1 && firstElemIdx <= firstCharIdx)
            {
                List<XPathPosition> positions = new List<XPathPosition>();
                using (StringReader reader = new StringReader(text))
                {
                    // Code from http://stackoverflow.com/questions/10606534/how-to-search-xml-files-with-xpath-returning-line-and-column-numbers-of-found
                    XPathDocument xmlDoc = new XPathDocument(reader);
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
                        foundCounter++;
                        var xn = xpni.Current;

                        var xpathPositions = new XPathPosition();
                        if (xn.NodeType == System.Xml.XPath.XPathNodeType.Attribute)
                        {
                            xpathPositions.EndsOnAttribute = true;
                            xpathPositions.AttributeName = xn.Name;
                        }
                        List<int> currentPositions = new List<int>();
                        GetXpathPositions(xn, ref currentPositions);
                        if (xpathPositions.EndsOnAttribute)
                            currentPositions.RemoveAt(currentPositions.Count - 1);
                        xpathPositions.Path = currentPositions;
                        positions.Add(xpathPositions);
                    }
                }

                results.AddRange(GetFilePositions(text, positions));
            }
            return results;
        }

        protected string DoXPathReplace(int lineNumber, int filePosition, string text, string searchXPath, string replaceText, GrepSearchOption searchOptions,
            IEnumerable<GrepMatch> replaceItems)
        {
            // shouldn't get here, but skip non-xml files:
            if (text.StartsWith("<"))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(text);
                XmlNodeList xmlNodes = xmlDoc.SelectNodes(searchXPath);

                // assume the replace items and xml nodes are in the same order
                var replaceList = replaceItems.ToArray();
                bool checkReplace = replaceList.Length == xmlNodes.Count;

                int idx = 0;
                foreach (XmlNode xmlNode in xmlNodes)
                {
                    bool doReplace = checkReplace ? replaceList[idx].ReplaceMatch : true;

                    if (doReplace)
                        xmlNode.InnerXml = DoPatternReplacement(xmlNode.InnerXml, replaceText);

                    idx++;
                }
                StringBuilder sb = new StringBuilder();
                using (StringWriter stringWriter = new StringWriter(sb))
                using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
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
            public List<int> Path { get; set; }
            public bool EndsOnAttribute { get; set; }
            public string AttributeName { get; set; }
        }

        /// <summary>
        /// Evaluates the absolute position of the current node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="positions">Lists the number of node in the according level, including root, that is first element. Positions start at 1.</param>
        private void GetXpathPositions(XPathNavigator node, ref List<int> positions)
        {
            int pos = 1;

            while (node.MoveToPrevious())
            {
                pos++;
            }

            if (node.MoveToParent())
            {
                positions.Insert(0, pos);
                GetXpathPositions(node, ref positions);
            }
        }

        private int GetAbsoluteCharPosition(int line, int position, string text, List<int> lineLengths, bool leftTrimmed)
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


        public GrepMatch[] GetFilePositions(string text, List<XPathPosition> positions)
        {
            bool[] endFound = new bool[positions.Count];
            // Getting line lengths
            List<int> lineLengths = new List<int>();
            using (StringReader baseReader = new StringReader(text))
            {
                using (EolReader reader = new EolReader(baseReader))
                {
                    while (!reader.EndOfStream)
                    {
                        lineLengths.Add(reader.ReadLine().Length);
                    }
                }
            }
            // These are absolute positions
            GrepMatch[] results = new GrepMatch[positions.Count];
            if (positions.Count == 0)
                return results;

            using (StringReader textReader = new StringReader(text))
            using (XmlReader reader = XmlReader.Create(textReader))
            {
                List<int> currPos = new List<int>();

                try
                {
                    IXmlLineInfo lineInfo = ((IXmlLineInfo)reader);
                    if (lineInfo.HasLineInfo())
                    {
                        // Parse the XML and display each node.
                        while (reader.Read())
                        {
                            switch (reader.NodeType)
                            {
                                case XmlNodeType.Element:
                                case XmlNodeType.Comment:

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
                                        results[i] = new GrepMatch(string.Empty, lineInfo.LineNumber, GetAbsoluteCharPosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 2, text, lineLengths, false), 0, true);
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

        private bool XPathPositionsMatch(List<int> currPos, List<int> positions)
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

        protected List<GrepMatch> DoFuzzySearch(int lineNumber, int filePosition, string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext)
        {
            List<GrepMatch> globalMatches = new List<GrepMatch>();

            foreach (var match in FuzzySearchIterator(lineNumber, filePosition, text, searchPattern, searchOptions))
            {
                globalMatches.Add(match);

                if (Utils.CancelSearch)
                    break;
            }

            return globalMatches;
        }

        public string DoFuzzyReplace(int lineNumber, int filePosition, string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions,
            IEnumerable<GrepMatch> replaceItems)
        {
            if (lineNumber > -1 && !replaceItems.Any(r => r.LineNumber == lineNumber && r.ReplaceMatch))
                return text;

            StringBuilder sb = new StringBuilder();
            int counter = 0;

            foreach (var match in FuzzySearchIterator(lineNumber, filePosition, text, searchPattern, searchOptions))
            {
                if (replaceItems.Any(r => match.Equals(r) && r.ReplaceMatch))
                {
                    sb.Append(text.Substring(counter, match.StartLocation - filePosition - counter));
                    sb.Append(DoPatternReplacement(searchPattern, replacePattern));

                    counter = match.StartLocation - filePosition + match.Length;
                }

                if (Utils.CancelSearch)
                    break;
            }

            sb.Append(text.Substring(counter));
            return sb.ToString();
        }

        private IEnumerable<GrepMatch> FuzzySearchIterator(int lineNumber, int filePosition, string text, string searchPattern, GrepSearchOption searchOptions)
        {
            bool isMultiline = lineNumber == -1;
            var lineEndIndexes = GetLineEndIndexes(initParams.VerboseMatchCount && lineNumber == -1 ? text : null);

            if (fuzzyMatchEngine == null)
                fuzzyMatchEngine = new GoogleMatch();
            fuzzyMatchEngine.Match_Threshold = initParams.FuzzyMatchThreshold;

            bool isWholeWord = searchOptions.HasFlag(GrepSearchOption.WholeWord);

            int counter = 0;
            while (counter < text.Length)
            {
                int matchLocation = fuzzyMatchEngine.match_main(text.Substring(counter), searchPattern, counter);
                if (matchLocation == -1)
                    break;

                if (isWholeWord && !Utils.IsValidBeginText(text.Substring(counter).Substring(0, matchLocation)))
                {
                    counter = counter + matchLocation + searchPattern.Length;
                    continue;
                }

                int matchLength = fuzzyMatchEngine.match_length(text.Substring(counter), searchPattern, matchLocation, isWholeWord, initParams.FuzzyMatchThreshold);

                if (matchLength == -1)
                {
                    counter = counter + matchLocation + searchPattern.Length;
                    continue;
                }

                if (initParams.VerboseMatchCount && lineEndIndexes.Count > 0)
                    lineNumber = lineEndIndexes.FindIndex(i => i > matchLocation + counter) + 1;

                yield return new GrepMatch(searchPattern, lineNumber, matchLocation + filePosition + counter, matchLength, isMultiline);

                counter = counter + matchLocation + matchLength;
            }
        }

        #endregion
    }

    public class GrepEngineInitParams
    {
        public static GrepEngineInitParams Default = new GrepEngineInitParams();

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
