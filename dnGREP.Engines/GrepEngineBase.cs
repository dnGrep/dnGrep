using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using dnGREP.Common;

namespace dnGREP.Engines
{
    public class GrepEngineBase
    {
        private string KEYWORD_GUID_LOWER = "$(guid)";
        private string KEYWORD_GUID_UPPER = "$(GUID)";
        private string KEYWORD_GUIDX = "$(guidx)";
        protected GrepEngineInitParams initParams = GrepEngineInitParams.Default;
        private GoogleMatch fuzzyMatchEngine = new GoogleMatch();

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

        public virtual void OpenFile(OpenFileArgs args)
        {
            Utils.OpenFile(args);
        }

        protected List<GrepSearchResult.GrepMatch> doFuzzySearchMultiline(int lineNumber, string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext)
        {
            var lineEndIndexes = GetLineEndIndexes(initParams.VerboseMatchCount && lineNumber == -1 ? text : null);

            int counter = 0;
            fuzzyMatchEngine.Match_Threshold = initParams.FuzzyMatchThreshold;
            bool isWholeWord = (searchOptions & GrepSearchOption.WholeWord) == GrepSearchOption.WholeWord;
            List<GrepSearchResult.GrepMatch> globalMatches = new List<GrepSearchResult.GrepMatch>();
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

                globalMatches.Add(new GrepSearchResult.GrepMatch(lineNumber, matchLocation + counter, matchLength));

                counter = counter + matchLocation + matchLength;
            }
            return globalMatches;
        }

        protected List<GrepSearchResult.GrepMatch> doXPathSearch(int lineNumber, string text, string searchXPath, GrepSearchOption searchOptions, bool includeContext)
        {
            List<GrepSearchResult.GrepMatch> results = new List<GrepSearchResult.GrepMatch>();
            // Check if file is an XML file
            if (text.Length > 5 && text.Substring(0, 5).ToLower() == "<?xml")
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
                        getXpathPositions(xn, ref currentPositions);
                        if (xpathPositions.EndsOnAttribute)
                            currentPositions.RemoveAt(currentPositions.Count - 1);
                        xpathPositions.Path = currentPositions;
                        positions.Add(xpathPositions);
                    }
                }

                results.AddRange(getFilePositions(text, positions));
            }

            return results;
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
        private void getXpathPositions(XPathNavigator node, ref List<int> positions)
        {
            int pos = 1;

            while (node.MoveToPrevious())
            {
                pos++;
            }

            if (node.MoveToParent())
            {
                positions.Insert(0, pos);
                getXpathPositions(node, ref positions);
            }
        }

        private int getAbsoluteCharPosition(int line, int position, string text, List<int> lineLengths, bool leftTrimmed)
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


        public GrepSearchResult.GrepMatch[] getFilePositions(string text, List<XPathPosition> positions)
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
            GrepSearchResult.GrepMatch[] results = new GrepSearchResult.GrepMatch[positions.Count];
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
                        bool readyToBreak = false;
                        // Parse the XML and display each node.
                        while (reader.Read() && !readyToBreak)
                        {
                            switch (reader.NodeType)
                            {
                                case XmlNodeType.Element:

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
                                        if (xPathPositionsMatch(currPos, positions[i].Path))
                                        {
                                            endFound[i] = true;
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }

                            if (reader.NodeType == XmlNodeType.EndElement)
                            {
                                for (int i = 0; i < positions.Count; i++)
                                {
                                    if (endFound[i] && !xPathPositionsMatch(currPos, positions[i].Path))
                                    {
                                        if (results[i] != null)
                                        {
                                            results[i].EndPosition = getAbsoluteCharPosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 3, text, lineLengths, true) + 1;
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
                                            results[i].EndPosition = getAbsoluteCharPosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 3, text, lineLengths, true) + 1;
                                        }
                                        endFound[i] = false;
                                    }

                                    if (xPathPositionsMatch(currPos, positions[i].Path))
                                    {
                                        results[i] = new GrepSearchResult.GrepMatch(lineInfo.LineNumber - 1, getAbsoluteCharPosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 2, text, lineLengths, false), 0);
                                    }

                                    // If empty element (e.g.<element/>)
                                    if (reader.IsEmptyElement)
                                    {
                                        if (xPathPositionsMatch(currPos, positions[i].Path))
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

        private bool xPathPositionsMatch(List<int> currPos, List<int> positions)
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

        protected List<GrepSearchResult.GrepMatch> doRegexSearch(int lineNumber, string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext)
        {
            RegexOptions regexOptions = RegexOptions.None;
            if ((searchOptions & GrepSearchOption.CaseSensitive) != GrepSearchOption.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;
            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                regexOptions |= RegexOptions.Multiline;
            if ((searchOptions & GrepSearchOption.SingleLine) == GrepSearchOption.SingleLine)
                regexOptions |= RegexOptions.Singleline;

            bool isWholeWord = (searchOptions & GrepSearchOption.WholeWord) == GrepSearchOption.WholeWord;

            if (isWholeWord)
            {
                if (!searchPattern.Trim().StartsWith("\\b"))
                    searchPattern = "\\b" + searchPattern.Trim();
                if (!searchPattern.Trim().EndsWith("\\b"))
                    searchPattern = searchPattern.Trim() + "\\b";
            }

            // Issue #210 .net regex will only match the $ end of line token with a \n, not \r\n or \r
            // see https://msdn.microsoft.com/en-us/library/yd1hzczs.aspx#Multiline
            // and http://stackoverflow.com/questions/8618557/why-doesnt-in-net-multiline-regular-expressions-match-crlf
            // must change the Windows and Mac line ends to just the Unix \n char before calling Regex
            if (searchPattern.Contains("$"))
            {
                // if the search pattern has Windows or Mac newlines, they must be converted, too
                searchPattern = searchPattern.Replace("\r\n", "\n");
                searchPattern = searchPattern.Replace('\r', '\n');

                if (lineNumber == -1 && text.Contains("\r\n"))
                {
                    return doRegexSearchSpecial(text, searchPattern, regexOptions);
                }

                if (text.Contains("\r\n"))
                {
                    text = text.Replace("\r\n", "\n");
                }
                else if (text.Contains("\r"))
                {
                    text = text.Replace('\r', '\n');
                }
            }

            var lineEndIndexes = GetLineEndIndexes((initParams.VerboseMatchCount && lineNumber == -1) ? text : null);

            List<GrepSearchResult.GrepMatch> globalMatches = new List<GrepSearchResult.GrepMatch>();
            var matches = Regex.Matches(text, searchPattern, regexOptions, MatchTimeout);
            foreach (Match match in matches)
            {
                if (initParams.VerboseMatchCount && lineEndIndexes.Count > 0)
                    lineNumber = lineEndIndexes.FindIndex(i => i > match.Index) + 1;

                globalMatches.Add(new GrepSearchResult.GrepMatch(lineNumber, match.Index, match.Length));

                if (Utils.CancelSearch)
                    break;
            }

            return globalMatches;
        }

        private List<GrepSearchResult.GrepMatch> doRegexSearchSpecial(string text, string searchPattern, RegexOptions regexOptions)
        {
            // this is a special case for multiline searches with Windows EOL characters and a '$' in the search pattern
            // must remove the \r from the EOL (see above), and then account for the missing character in the start index
            // of each match

            text = text.Replace("\r\n", "\n");

            var lineEndIndexes = GetLineEndIndexes(text);
            int lineNumber = 1;

            List<GrepSearchResult.GrepMatch> globalMatches = new List<GrepSearchResult.GrepMatch>();
            var matches = Regex.Matches(text, searchPattern, regexOptions, MatchTimeout);
            foreach (Match match in matches)
            {
                if (lineEndIndexes.Count > 0)
                    lineNumber = lineEndIndexes.FindIndex(i => i > match.Index) + 1;

                globalMatches.Add(new GrepSearchResult.GrepMatch(lineNumber, match.Index + (lineNumber - 1), match.Length));

                if (Utils.CancelSearch)
                    break;
            }

            return globalMatches;
        }

        protected string doPatternReplacement(string replaceText)
        {
            if (replaceText.Contains(KEYWORD_GUID_LOWER))
                return replaceText.Replace(KEYWORD_GUID_LOWER, Guid.NewGuid().ToString());
            if (replaceText.Contains(KEYWORD_GUID_UPPER))
                return replaceText.Replace(KEYWORD_GUID_UPPER, Guid.NewGuid().ToString().ToUpper());
            else if (replaceText.Contains(KEYWORD_GUIDX))
                return replaceText;
            else
                return replaceText;
        }

        protected List<GrepSearchResult.GrepMatch> doTextSearchCaseInsensitive(int lineNumber, string text, string searchText, GrepSearchOption searchOptions, bool includeContext)
        {
            var lineEndIndexes = GetLineEndIndexes(initParams.VerboseMatchCount && lineNumber == -1 ? text : null);

            int index = 0;
            bool isWholeWord = (searchOptions & GrepSearchOption.WholeWord) == GrepSearchOption.WholeWord;
            List<GrepSearchResult.GrepMatch> globalMatches = new List<GrepSearchResult.GrepMatch>();
            while (index >= 0)
            {
                index = text.IndexOf(searchText, index, StringComparison.InvariantCultureIgnoreCase);
                if (index >= 0)
                {
                    if (isWholeWord && (!Utils.IsValidBeginText(text.Substring(0, index)) ||
                    !Utils.IsValidEndText(text.Substring(index + searchText.Length))))
                    {
                        index++;
                        continue;
                    }

                    if (initParams.VerboseMatchCount && lineEndIndexes.Count > 0)
                        lineNumber = lineEndIndexes.FindIndex(i => i > index) + 1;

                    globalMatches.Add(new GrepSearchResult.GrepMatch(lineNumber, index, searchText.Length));
                    index++;
                }

                if (Utils.CancelSearch)
                    break;
            }

            return globalMatches;
        }

        protected List<GrepSearchResult.GrepMatch> doTextSearchCaseSensitive(int lineNumber, string text, string searchText, GrepSearchOption searchOptions, bool includeContext)
        {
            var lineEndIndexes = GetLineEndIndexes(initParams.VerboseMatchCount && lineNumber == -1 ? text : null);

            List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
            int index = 0;
            bool isWholeWord = (searchOptions & GrepSearchOption.WholeWord) == GrepSearchOption.WholeWord;
            List<GrepSearchResult.GrepMatch> globalMatches = new List<GrepSearchResult.GrepMatch>();
            while (index >= 0)
            {
                index = text.IndexOf(searchText, index, StringComparison.InvariantCulture);
                if (index >= 0)
                {
                    if (isWholeWord && (!Utils.IsValidBeginText(text.Substring(0, index)) ||
                    !Utils.IsValidEndText(text.Substring(index + searchText.Length))))
                    {
                        index++;
                        continue;
                    }

                    if (initParams.VerboseMatchCount && lineEndIndexes.Count > 0)
                        lineNumber = lineEndIndexes.FindIndex(i => i > index) + 1;

                    globalMatches.Add(new GrepSearchResult.GrepMatch(lineNumber, index, searchText.Length));
                    index++;
                }

                if (Utils.CancelSearch)
                    break;
            }

            return globalMatches;
        }

        protected string doTextReplaceCaseSensitive(string text, string searchText, string replaceText, GrepSearchOption searchOptions)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            int counter = 0;
            bool isWholeWord = (searchOptions & GrepSearchOption.WholeWord) == GrepSearchOption.WholeWord;
            while (index >= 0)
            {
                index = text.IndexOf(searchText, index, StringComparison.InvariantCulture);
                if (index >= 0)
                {
                    if (isWholeWord && (!Utils.IsValidBeginText(text.Substring(0, index)) ||
                    !Utils.IsValidEndText(text.Substring(index + searchText.Length))))
                    {
                        index++;
                        continue;
                    }

                    sb.Append(text.Substring(counter, index - counter));
                    sb.Append(doPatternReplacement(replaceText));

                    counter = index + searchText.Length;

                    index++;
                }

                if (Utils.CancelSearch)
                    break;
            }
            sb.Append(text.Substring(counter));
            return sb.ToString();
        }

        protected string doTextReplaceCaseInsensitive(string text, string searchText, string replaceText, GrepSearchOption searchOptions)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            int counter = 0;
            bool isWholeWord = (searchOptions & GrepSearchOption.WholeWord) == GrepSearchOption.WholeWord;
            while (index >= 0)
            {
                index = text.ToLowerInvariant().IndexOf(searchText.ToLowerInvariant(), index, StringComparison.InvariantCulture);
                if (index >= 0)
                {
                    if (isWholeWord && (!Utils.IsValidBeginText(text.Substring(0, index)) ||
                    !Utils.IsValidEndText(text.Substring(index + searchText.Length))))
                    {
                        index++;
                        continue;
                    }

                    sb.Append(text.Substring(counter, index - counter));
                    sb.Append(doPatternReplacement(replaceText));

                    counter = index + searchText.Length;

                    index++;
                }

                if (Utils.CancelSearch)
                    break;
            }
            sb.Append(text.Substring(counter));
            return sb.ToString();
        }

        protected string doRegexReplace(string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions)
        {
            RegexOptions regexOptions = RegexOptions.None;
            if ((searchOptions & GrepSearchOption.CaseSensitive) != GrepSearchOption.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;
            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                regexOptions |= RegexOptions.Multiline;
            if ((searchOptions & GrepSearchOption.SingleLine) == GrepSearchOption.SingleLine)
                regexOptions |= RegexOptions.Singleline;

            bool isWholeWord = (searchOptions & GrepSearchOption.WholeWord) == GrepSearchOption.WholeWord;

            if (isWholeWord)
            {
                if (!searchPattern.Trim().StartsWith("\\b"))
                    searchPattern = "\\b" + searchPattern.Trim();
                if (!searchPattern.Trim().EndsWith("\\b"))
                    searchPattern = searchPattern.Trim() + "\\b";
            }

            // Another issue #210 change (see above)
            // regex only matches $ end of line on a \n newline
            // change Windows and Mac newlines to UNIX
            string prevNewLine = null;
            if (searchPattern.Contains("$"))
            {
                // if the search pattern has Windows or Mac newlines, they must be converted, too
                searchPattern = searchPattern.Replace("\r\n", "\n");
                searchPattern = searchPattern.Replace('\r', '\n');

                if (text.Contains("\r\n"))
                {
                    text = text.Replace("\r\n", "\n");
                    prevNewLine = "\r\n";
                }
                else if (text.Contains("\r"))
                {
                    text = text.Replace("\r", "\n");
                    prevNewLine = "\r";
                }
            }

            string result = Regex.Replace(text, searchPattern, doPatternReplacement(replacePattern), regexOptions);
            if (!string.IsNullOrEmpty(prevNewLine))
                result = result.Replace("\n", prevNewLine);
            return result;
        }

        public string doFuzzyReplace(string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions)
        {
            int counter = 0;
            StringBuilder result = new StringBuilder();
            fuzzyMatchEngine.Match_Threshold = initParams.FuzzyMatchThreshold;
            bool isWholeWord = (searchOptions & GrepSearchOption.WholeWord) == GrepSearchOption.WholeWord;
            while (counter < text.Length)
            {
                int matchLocation = fuzzyMatchEngine.match_main(text.Substring(counter), searchPattern, counter);
                if (matchLocation == -1)
                {
                    result.Append(text.Substring(counter));
                    break;
                }

                if (isWholeWord && !Utils.IsValidBeginText(text.Substring(counter).Substring(0, matchLocation + counter)))
                {
                    result.Append(text.Substring(counter));
                    counter = counter + matchLocation + searchPattern.Length;
                    continue;
                }

                int matchLength = fuzzyMatchEngine.match_length(text.Substring(counter), searchPattern, matchLocation, isWholeWord, initParams.FuzzyMatchThreshold);

                if (matchLength == -1)
                {
                    result.Append(text.Substring(counter));
                    counter = counter + matchLocation + searchPattern.Length;
                    continue;
                }

                // Text before match
                result.Append(text.Substring(counter, matchLocation));
                // New text
                result.Append(doPatternReplacement(replacePattern));

                counter = counter + matchLocation + matchLength;
            }
            return result.ToString();
        }

        protected string doXPathReplace(string text, string searchXPath, string replaceText, GrepSearchOption searchOptions)
        {
            if (text.Length > 5 && text.Substring(0, 5).ToLower() == "<?xml")
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(text);
                XmlNodeList xmlNodes = xmlDoc.SelectNodes(searchXPath);

                foreach (XmlNode xmlNode in xmlNodes)
                {
                    xmlNode.InnerXml = doPatternReplacement(replaceText);
                }
                StringBuilder sb = new StringBuilder();
                StringWriter stringWriter = new StringWriter(sb);
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
