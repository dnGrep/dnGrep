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
		protected bool showLinesInContext = false;
		protected int linesBefore = 0;
		protected int linesAfter = 0;
        private GoogleMatch fuzzyMatchEngine = new GoogleMatch();

		public GrepEngineBase() { }

		public GrepEngineBase(bool showLinesInContext, int linesBefore, int linesAfter)
		{
			Initialize(showLinesInContext, linesBefore, linesAfter);
		}

		public virtual bool Initialize(bool showLinesInContext, int linesBefore, int linesAfter)
		{
			this.showLinesInContext = showLinesInContext;
			this.linesBefore = linesBefore;
			this.linesAfter = linesAfter;
			return true;
		}

		public virtual void OpenFile(OpenFileArgs args)
		{
			Utils.OpenFile(args);
		}

        protected bool doTextSearchCaseInsensitive(string text, string searchText, GrepSearchOption searchOptions)
		{
			return text.ToLower().Contains(searchText.ToLower());
		}

        protected bool doTextSearchCaseSensitive(string text, string searchText, GrepSearchOption searchOptions)
		{
			return text.Contains(searchText);
		}

        protected bool doRegexSearchCaseInsensitive(string text, string searchPattern, GrepSearchOption searchOptions)
		{
            RegexOptions regexOptions = RegexOptions.None;
            if ((searchOptions & GrepSearchOption.CaseSensitive) != GrepSearchOption.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;
            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                regexOptions |= RegexOptions.Multiline;
            if ((searchOptions & GrepSearchOption.SingleLine) == GrepSearchOption.SingleLine)
                regexOptions |= RegexOptions.Singleline;

            return Regex.IsMatch(text, searchPattern, regexOptions);
		}

        protected bool doRegexSearchCaseSensitive(string text, string searchPattern, GrepSearchOption searchOptions)
		{
            RegexOptions regexOptions = RegexOptions.None;
            if ((searchOptions & GrepSearchOption.CaseSensitive) != GrepSearchOption.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;
            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                regexOptions |= RegexOptions.Multiline;
            if ((searchOptions & GrepSearchOption.SingleLine) == GrepSearchOption.SingleLine)
                regexOptions |= RegexOptions.Singleline;

            return Regex.IsMatch(text, searchPattern, regexOptions);
		}

        protected bool doFuzzySearch(string text, string searchPattern, GrepSearchOption searchOptions)
        {
            return fuzzyMatchEngine.match_main(text, searchPattern, 0) != -1; 
        }

		protected List<GrepSearchResult.GrepLine> doFuzzySearchMultiline(string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext)
        {
            List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
            int counter = 0;
            while (counter < text.Length)
            {
                int matchLocation = fuzzyMatchEngine.match_main(text.Substring(counter), searchPattern, counter);
                if (matchLocation == -1)
                    break;

				int matchLength = fuzzyMatchEngine.match_length(text.Substring(counter), searchPattern, matchLocation);

                List<int> lineNumbers = new List<int>();
                List<GrepSearchResult.GrepMatch> matches = new List<GrepSearchResult.GrepMatch>();
                List<string> lines = Utils.GetLines(text, matchLocation + counter, matchLength, out matches, out lineNumbers);
                if (lineNumbers != null)
                {
                    for (int i = 0; i < lineNumbers.Count; i++)
                    {
                        List<GrepSearchResult.GrepMatch> lineMatches = new List<GrepSearchResult.GrepMatch>();
                        foreach (GrepSearchResult.GrepMatch m in matches) if (m.LineNumber == lineNumbers[i]) lineMatches.Add(m);

                        results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lines[i], false, lineMatches));
						if (showLinesInContext && includeContext)
                        {
                            results.AddRange(Utils.GetContextLines(text, linesBefore,
                                linesAfter, lineNumbers[i]));
                        }
                    }
                }

                counter = counter + matchLocation + matchLength;
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

		protected List<GrepSearchResult.GrepLine> doRegexSearchCaseSensitiveMultiline(string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext)
		{
            RegexOptions regexOptions = RegexOptions.None;
            if ((searchOptions & GrepSearchOption.CaseSensitive) != GrepSearchOption.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;
            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                regexOptions |= RegexOptions.Multiline;
            if ((searchOptions & GrepSearchOption.SingleLine) == GrepSearchOption.SingleLine)
                regexOptions |= RegexOptions.Singleline;

			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
            foreach (Match match in Regex.Matches(text, searchPattern, regexOptions))
			{
				List<int> lineNumbers = new List<int>();
                List<GrepSearchResult.GrepMatch> matches = new List<GrepSearchResult.GrepMatch>();
                List<string> lines = Utils.GetLines(text, match.Index, match.Length, out matches, out lineNumbers);
				if (lineNumbers != null)
				{
					for (int i = 0; i < lineNumbers.Count; i++)
					{
                        List<GrepSearchResult.GrepMatch> lineMatches = new List<GrepSearchResult.GrepMatch>();
                        foreach (GrepSearchResult.GrepMatch m in matches) if (m.LineNumber == lineNumbers[i]) lineMatches.Add(m);

                        results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lines[i], false, lineMatches));
						if (showLinesInContext && includeContext)
						{
							results.AddRange(Utils.GetContextLines(text, linesBefore,
								linesAfter, lineNumbers[i]));
						}
					}
				}
			}
			return results;
		}

		protected List<GrepSearchResult.GrepLine> doRegexSearchCaseInsensitiveMultiline(string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext)
		{
            RegexOptions regexOptions = RegexOptions.None;
            if ((searchOptions & GrepSearchOption.CaseSensitive) != GrepSearchOption.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;
            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                regexOptions |= RegexOptions.Multiline;
            if ((searchOptions & GrepSearchOption.SingleLine) == GrepSearchOption.SingleLine)
                regexOptions |= RegexOptions.Singleline;

			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
            foreach (Match match in Regex.Matches(text, searchPattern, regexOptions))
			{
				List<int> lineNumbers = new List<int>();
                List<GrepSearchResult.GrepMatch> matches = new List<GrepSearchResult.GrepMatch>();
                List<string> lines = Utils.GetLines(text, match.Index, match.Length, out matches, out lineNumbers);
				if (lineNumbers != null)
				{
					for (int i = 0; i < lineNumbers.Count; i++)
					{
                        List<GrepSearchResult.GrepMatch> lineMatches = new List<GrepSearchResult.GrepMatch>();
                        foreach (GrepSearchResult.GrepMatch m in matches) if (m.LineNumber == lineNumbers[i]) lineMatches.Add(m);

                        results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lines[i], false, lineMatches));
						if (showLinesInContext && includeContext)
						{
							results.AddRange(Utils.GetContextLines(text, linesBefore,
								linesAfter, lineNumbers[i]));
						}
					}
				}
			}
			return results;
		}

        protected List<GrepSearchResult.GrepLine> doTextSearchCaseInsensitiveMultiline(string text, string searchText, GrepSearchOption searchOptions, bool includeContext)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
			int index = 0;
			while (index >= 0)
			{
				index = text.IndexOf(searchText, index, StringComparison.InvariantCultureIgnoreCase);
				if (index >= 0)
				{
					List<int> lineNumbers = new List<int>();
                    List<GrepSearchResult.GrepMatch> matches = new List<GrepSearchResult.GrepMatch>();
                    List<string> lines = Utils.GetLines(text, index, searchText.Length, out matches, out lineNumbers);
					if (lineNumbers != null)
					{
						for (int i = 0; i < lineNumbers.Count; i++)
						{
                            List<GrepSearchResult.GrepMatch> lineMatches = new List<GrepSearchResult.GrepMatch>();
                            foreach (GrepSearchResult.GrepMatch m in matches) if (m.LineNumber == lineNumbers[i]) lineMatches.Add(m);

                            results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lines[i], false, lineMatches));
							if (showLinesInContext && includeContext)
							{
								results.AddRange(Utils.GetContextLines(text, linesBefore,
									linesAfter, lineNumbers[i]));
							}
						}
					}
					index++;
				}
			}
			return results;
		}

        protected List<GrepSearchResult.GrepLine> doTextSearchCaseSensitiveMultiline(string text, string searchText, GrepSearchOption searchOptions, bool includeContext)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
			int index = 0;
			while (index >= 0)
			{
				index = text.IndexOf(searchText, index, StringComparison.InvariantCulture);
				if (index >= 0)
				{
					List<int> lineNumbers = new List<int>();
                    List<GrepSearchResult.GrepMatch> matches = new List<GrepSearchResult.GrepMatch>();
					List<string> lines = Utils.GetLines(text, index, searchText.Length, out matches, out lineNumbers);
					if (lineNumbers != null)
					{
						for (int i = 0; i < lineNumbers.Count; i++)
						{
                            List<GrepSearchResult.GrepMatch> lineMatches = new List<GrepSearchResult.GrepMatch>();
                            foreach (GrepSearchResult.GrepMatch m in matches) if (m.LineNumber == lineNumbers[i]) lineMatches.Add(m);

                            results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lines[i], false, lineMatches));
							if (showLinesInContext && includeContext)
							{
								results.AddRange(Utils.GetContextLines(text, linesBefore,
									linesAfter, lineNumbers[i]));
							}
						}
					}
					index++;
				}
			}
			return results;
		}

        protected string doTextReplaceCaseSensitive(string text, string searchText, string replaceText, GrepSearchOption searchOptions)
		{
			return text.Replace(searchText, replaceText);
		}

        protected string doTextReplaceCaseInsensitive(string text, string searchText, string replaceText, GrepSearchOption searchOptions)
		{
			int count, position0, position1;
			count = position0 = position1 = 0;
			string upperString = text.ToUpper();
			string upperPattern = searchText.ToUpper();
			int inc = (text.Length / searchText.Length) *
					  (replaceText.Length - searchText.Length);
			char[] chars = new char[text.Length + Math.Max(0, inc)];
			while ((position1 = upperString.IndexOf(upperPattern,
											  position0)) != -1)
			{
				for (int i = position0; i < position1; ++i)
					chars[count++] = text[i];
				for (int i = 0; i < replaceText.Length; ++i)
					chars[count++] = replaceText[i];
				position0 = position1 + searchText.Length;
			}
			if (position0 == 0) return text;
			for (int i = position0; i < text.Length; ++i)
				chars[count++] = text[i];
			return new string(chars, 0, count);
		}

        protected string doRegexReplaceCaseInsensitive(string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions)
		{
            RegexOptions regexOptions = RegexOptions.None;
            if ((searchOptions & GrepSearchOption.CaseSensitive) != GrepSearchOption.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;
            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                regexOptions |= RegexOptions.Multiline;
            if ((searchOptions & GrepSearchOption.SingleLine) == GrepSearchOption.SingleLine)
                regexOptions |= RegexOptions.Singleline;

            return Regex.Replace(text, searchPattern, replacePattern, regexOptions);
		}

        public string doRegexReplaceCaseSensitive(string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions)
		{
            RegexOptions regexOptions = RegexOptions.None;
            if ((searchOptions & GrepSearchOption.CaseSensitive) != GrepSearchOption.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;
            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                regexOptions |= RegexOptions.Multiline;
            if ((searchOptions & GrepSearchOption.SingleLine) == GrepSearchOption.SingleLine)
                regexOptions |= RegexOptions.Singleline;

            return Regex.Replace(text, searchPattern, replacePattern, regexOptions);
		}

        public string doFuzzyReplace(string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions)
        {
            int counter = 0;
            StringBuilder result = new StringBuilder();
            while (counter < text.Length)
            {
                int matchLocation = fuzzyMatchEngine.match_main(text.Substring(counter), searchPattern, counter);
                if (matchLocation == -1)
                {
                    result.Append(text.Substring(counter));
                    break;
                }

				int matchLength = fuzzyMatchEngine.match_length(text.Substring(counter), searchPattern, matchLocation);

                // Text before match
                result.Append(text.Substring(counter, matchLocation));
                // New text
                result.Append(replacePattern);

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
					xmlNode.InnerXml = replaceText;
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
}
