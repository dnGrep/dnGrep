using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using dnGREP.Common;
using System.IO;

namespace dnGREP.Engines
{
	public class GrepEngineBase
	{
        private string KEYWORD_GUID_LOWER = "$(guid)";
        private string KEYWORD_GUID_UPPER = "$(GUID)";
        private string KEYWORD_GUIDX = "$(guidx)";
		protected double fuzzyMatchThreshold = 0.5;
        private GoogleMatch fuzzyMatchEngine = new GoogleMatch();

		public GrepEngineBase() { }

		public GrepEngineBase(GrepEngineInitParams param)
		{
			Initialize(param);
		}

		public virtual bool Initialize(GrepEngineInitParams param)
		{
			this.fuzzyMatchThreshold = param.FuzzyMatchThreshold;
			return true;
		}

		public virtual void OpenFile(OpenFileArgs args)
		{
			Utils.OpenFile(args);
		}

        protected List<GrepSearchResult.GrepLine> doFuzzySearchMultiline(string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext)
        {
            List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
            int counter = 0;
			fuzzyMatchEngine.Match_Threshold = (float)fuzzyMatchThreshold;
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

				int matchLength = fuzzyMatchEngine.match_length(text.Substring(counter), searchPattern, matchLocation, isWholeWord, fuzzyMatchThreshold);

				if (matchLength == -1)
				{
					counter = counter + matchLocation + searchPattern.Length;
					continue;
				}

                globalMatches.Add(new GrepSearchResult.GrepMatch(0, matchLocation + counter, matchLength));
                
                counter = counter + matchLocation + matchLength;
            }
            if (globalMatches.Count > 0)
            {
                if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                {
                    using (StringReader reader = new StringReader(text))
                    {
                        results = Utils.GetLinesEx(reader, globalMatches);
                    }
                }
                else
                {
                    results.Add(new GrepSearchResult.GrepLine(0, text, false, globalMatches));
                }
            }
            return results;
        }

        protected List<GrepSearchResult.GrepLine> doXPathSearch(string text, string searchXPath, GrepSearchOption searchOptions, bool includeContext)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
			// Check if file is an XML file
			if (text.Length > 5 && text.Substring(0, 5).ToLower() == "<?xml")
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(text);
				XmlNodeList xmlNodes = xmlDoc.SelectNodes(searchXPath);
				string line = "";
				foreach (XmlNode xmlNode in xmlNodes)
				{
					line = xmlNode.OuterXml;
                    results.Add(new GrepSearchResult.GrepLine(-1, line, false, null));
				}
			}

			return results;
		}

		protected List<GrepSearchResult.GrepLine> doRegexSearch(string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext)
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

			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
            List<GrepSearchResult.GrepMatch> globalMatches = new List<GrepSearchResult.GrepMatch>();
            foreach (Match match in Regex.Matches(text, searchPattern, regexOptions))
			{
                globalMatches.Add(new GrepSearchResult.GrepMatch(0, match.Index, match.Length));
			}

            if (globalMatches.Count > 0)
            {
                if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                {
                    using (StringReader reader = new StringReader(text))
                    {
                        results = Utils.GetLinesEx(reader, globalMatches);
                    }
                }
                else
                {
                    results.Add(new GrepSearchResult.GrepLine(0, text, false, globalMatches));
                }
            }         
			return results;
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

        protected List<GrepSearchResult.GrepLine> doTextSearchCaseInsensitive(string text, string searchText, GrepSearchOption searchOptions, bool includeContext)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
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

                    globalMatches.Add(new GrepSearchResult.GrepMatch(0, index, searchText.Length));
					index++;
				}
			}

            if (globalMatches.Count > 0)
            {
                if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                {
                    using (StringReader reader = new StringReader(text))
                    {
                        results = Utils.GetLinesEx(reader, globalMatches);
                    }
                }
                else
                {
                    results.Add(new GrepSearchResult.GrepLine(0, text, false, globalMatches));
                }
            }

			return results;
		}

        protected List<GrepSearchResult.GrepLine> doTextSearchCaseSensitive(string text, string searchText, GrepSearchOption searchOptions, bool includeContext)
		{
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

                    globalMatches.Add(new GrepSearchResult.GrepMatch(0, index, searchText.Length));
					index++;
				}
			}

            if (globalMatches.Count > 0)
            {
                if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                {
                    using (StringReader reader = new StringReader(text))
                    {
                        results = Utils.GetLinesEx(reader, globalMatches);
                    }
                }
                else
                {
                    results.Add(new GrepSearchResult.GrepLine(0, text, false, globalMatches));
                }
            }
			return results;
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

            return Regex.Replace(text, searchPattern, doPatternReplacement(replacePattern), regexOptions);
		}

        public string doFuzzyReplace(string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions)
        {
            int counter = 0;
            StringBuilder result = new StringBuilder();
			fuzzyMatchEngine.Match_Threshold = (float)fuzzyMatchThreshold;
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

				int matchLength = fuzzyMatchEngine.match_length(text.Substring(counter), searchPattern, matchLocation, isWholeWord, fuzzyMatchThreshold);

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
		public GrepEngineInitParams() { }

		public GrepEngineInitParams(double fuzzyMatchThreshold)
		{
			this.fuzzyMatchThreshold = fuzzyMatchThreshold;
		}

		private double fuzzyMatchThreshold = 0.5;

		public double FuzzyMatchThreshold
		{
			get { return fuzzyMatchThreshold; }
			set { fuzzyMatchThreshold = value; }
		}
	}
}
