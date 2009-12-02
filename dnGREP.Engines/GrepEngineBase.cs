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

		protected bool doTextSearchCaseInsensitive(string text, string searchText)
		{
			return text.ToLower().Contains(searchText.ToLower());
		}

		protected bool doTextSearchCaseSensitive(string text, string searchText)
		{
			return text.Contains(searchText);
		}

		protected bool doRegexSearchCaseInsensitive(string text, string searchPattern)
		{
			return Regex.IsMatch(text, searchPattern, RegexOptions.IgnoreCase);
		}

		protected bool doRegexSearchCaseSensitive(string text, string searchPattern)
		{
			return Regex.IsMatch(text, searchPattern);
		}

		protected List<GrepSearchResult.GrepLine> doXPathSearch(string text, string searchXPath)
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
					results.Add(new GrepSearchResult.GrepLine(-1, line, false));
				}
			}

			return results;
		}

		protected List<GrepSearchResult.GrepLine> doRegexSearchCaseSensitiveMultiline(string text, string searchPattern)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
			foreach (Match match in Regex.Matches(text, searchPattern, RegexOptions.Multiline))
			{
				List<int> lineNumbers = new List<int>();
				List<string> lines = Utils.GetLines(text, match.Index, match.Length, out lineNumbers);
				if (lineNumbers != null)
				{
					for (int i = 0; i < lineNumbers.Count; i++)
					{
						results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lines[i], false));
						if (showLinesInContext)
						{
							results.AddRange(Utils.GetContextLines(text, linesBefore,
								linesAfter, lineNumbers[i]));
						}
					}
				}
			}
			return results;
		}

		protected List<GrepSearchResult.GrepLine> doRegexSearchCaseInsensitiveMultiline(string text, string searchPattern)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
			foreach (Match match in Regex.Matches(text, searchPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
			{
				List<int> lineNumbers = new List<int>();
				List<string> lines = Utils.GetLines(text, match.Index, match.Length, out lineNumbers);
				if (lineNumbers != null)
				{
					for (int i = 0; i < lineNumbers.Count; i++)
					{
						results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lines[i], false));
						if (showLinesInContext)
						{
							results.AddRange(Utils.GetContextLines(text, linesBefore,
								linesAfter, lineNumbers[i]));
						}
					}
				}
			}
			return results;
		}

		protected List<GrepSearchResult.GrepLine> doTextSearchCaseInsensitiveMultiline(string text, string searchText)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
			int index = 0;
			while (index >= 0)
			{
				index = text.IndexOf(searchText, index, StringComparison.InvariantCultureIgnoreCase);
				if (index >= 0)
				{
					List<int> lineNumbers = new List<int>();
					List<string> lines = Utils.GetLines(text, index, searchText.Length, out lineNumbers);
					if (lineNumbers != null)
					{
						for (int i = 0; i < lineNumbers.Count; i++)
						{
							results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lines[i], false));
							if (showLinesInContext)
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

		protected List<GrepSearchResult.GrepLine> doTextSearchCaseSensitiveMultiline(string text, string searchText)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
			int index = 0;
			while (index >= 0)
			{
				index = text.IndexOf(searchText, index, StringComparison.InvariantCulture);
				if (index >= 0)
				{
					List<int> lineNumbers = new List<int>();
					List<string> lines = Utils.GetLines(text, index, searchText.Length, out lineNumbers);
					if (lineNumbers != null)
					{
						for (int i = 0; i < lineNumbers.Count; i++)
						{
							results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lines[i], false));
							if (showLinesInContext)
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

		protected string doTextReplaceCaseSensitive(string text, string searchText, string replaceText)
		{
			return text.Replace(searchText, replaceText);
		}

		protected string doTextReplaceCaseInsensitive(string text, string searchText, string replaceText)
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

		protected string doRegexReplaceCaseInsensitive(string text, string searchPattern, string replacePattern)
		{
			return Regex.Replace(text, searchPattern, replacePattern, RegexOptions.IgnoreCase);
		}

		public string doRegexReplaceCaseSensitive(string text, string searchPattern, string replacePattern)
		{
			return Regex.Replace(text, searchPattern, replacePattern);
		}

		protected string doXPathReplace(string text, string searchXPath, string replaceText)
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
