using System;
using System.Collections.Generic;
using System.Text;
using dnGREP.Common;
using System.IO;
using NLog;

namespace dnGREP.Engines
{
	public class GrepEnginePlainText : GrepEngineBase, IGrepEngine
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public GrepEnginePlainText() : base() { }

		public bool IsSearchOnly
		{
			get { return false; }
		}

		public string Description
		{
			get { return "Basic engine for searching plain text file"; }
		}

		public List<string> SupportedFileExtensions
		{
			get { return new List<string>(new string[] { "*" }); }
		}

		public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, bool isCaseSensitive, bool isMultiline, Encoding encoding)
		{
			SearchDelegates.DoSearch searchMethod = doTextSearchCaseSensitive;
			SearchDelegates.DoSearchMultiline searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
			switch (searchType)
			{
				case SearchType.PlainText:
					if (isCaseSensitive)
					{
						if (isMultiline)
							searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
						else
							searchMethod = doTextSearchCaseSensitive;
					}
					else
					{
						if (isMultiline)
							searchMethodMultiline = doTextSearchCaseInsensitiveMultiline;
						else
							searchMethod = doTextSearchCaseInsensitive;
					}
					break;
				case SearchType.Regex:
					if (isCaseSensitive)
					{
						if (isMultiline)
							searchMethodMultiline = doRegexSearchCaseSensitiveMultiline;
						else
							searchMethod = doRegexSearchCaseSensitive;
					}
					else
					{
						if (isMultiline)
							searchMethodMultiline = doRegexSearchCaseInsensitiveMultiline;
						else
							searchMethod = doRegexSearchCaseInsensitive;
					}
					break;
				case SearchType.XPath:
					searchMethodMultiline = doXPathSearch;
					break;
			}

			if (isMultiline)
				return searchMultiline(file, searchPattern, searchMethodMultiline, encoding);
			else
				return search(file, searchPattern, searchMethod, encoding);
		}

		public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, bool isCaseSensitive, bool isMultiline, Encoding encoding)
		{
			SearchDelegates.DoSearch searchMethod = doTextSearchCaseSensitive;
			SearchDelegates.DoSearchMultiline searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
			SearchDelegates.DoReplace replaceMethod = doTextReplaceCaseSensitive;
			switch (searchType)
			{
				case SearchType.PlainText:
					if (isCaseSensitive)
					{
						if (isMultiline)
							searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
						else
							searchMethod = doTextSearchCaseSensitive;
						
						replaceMethod = doTextReplaceCaseSensitive;
					}
					else
					{
						if (isMultiline)
							searchMethodMultiline = doTextSearchCaseInsensitiveMultiline;
						else
							searchMethod = doTextSearchCaseInsensitive;

						replaceMethod = doTextReplaceCaseInsensitive;
					}
					break;
				case SearchType.Regex:
					if (isCaseSensitive)
					{
						if (isMultiline)
							searchMethodMultiline = doRegexSearchCaseSensitiveMultiline;
						else
							searchMethod = doRegexSearchCaseSensitive;

						replaceMethod = doRegexReplaceCaseSensitive;
					}
					else
					{
						if (isMultiline)
							searchMethodMultiline = doRegexSearchCaseInsensitiveMultiline;
						else
							searchMethod = doRegexSearchCaseInsensitive;

						replaceMethod = doRegexReplaceCaseInsensitive;
					}
					break;
				case SearchType.XPath:
					searchMethodMultiline = doXPathSearch;
					replaceMethod = doXPathReplace;
					break;
			}

			if (isMultiline)
				return replaceMultiline(sourceFile, destinationFile, searchPattern, replacePattern, searchMethodMultiline, replaceMethod, encoding);
			else
				return replace(sourceFile, destinationFile, searchPattern, replacePattern, searchMethod, replaceMethod, encoding);
		}

		#region Actual Implementation

		private List<GrepSearchResult> search(string file, string searchPattern, SearchDelegates.DoSearch searchMethod, Encoding encoding)
		{
			List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

			using (StreamReader readStream = new StreamReader(File.OpenRead(file), encoding))
			{
				string line = null;
				int counter = 1;
				List<GrepSearchResult.GrepLine> lines = new List<GrepSearchResult.GrepLine>();
				Queue<GrepSearchResult.GrepLine> preContextLines = new Queue<GrepSearchResult.GrepLine>();
				Queue<GrepSearchResult.GrepLine> postContextLines = new Queue<GrepSearchResult.GrepLine>();
				bool collectPostContextLines = false;
				while ((line = readStream.ReadLine()) != null)
				{
					// Collecting context lines
					if (showLinesInContext)
					{
						if (preContextLines.Count > linesBefore)
							preContextLines.Dequeue();

						preContextLines.Enqueue(new GrepSearchResult.GrepLine(counter, line, true));

						if (collectPostContextLines)
						{
							if (postContextLines.Count < linesAfter)
							{
								postContextLines.Enqueue(new GrepSearchResult.GrepLine(counter, line, true));
							}
							else
							{
								collectPostContextLines = false;
								lines.AddRange(postContextLines);
								postContextLines.Clear();
							}
						}
					}

					if (searchMethod(line, searchPattern))
					{
						lines.Add(new GrepSearchResult.GrepLine(counter, line, false));
						lines.AddRange(preContextLines);
						preContextLines.Clear();
						postContextLines.Clear();
						collectPostContextLines = true;
					}
					counter++;
				}
				lines.AddRange(postContextLines);
				Utils.CleanResults(ref lines);
				if (lines.Count > 0)
				{
					searchResults.Add(new GrepSearchResult(file, lines));
				}
			}
			return searchResults;
		}

		private List<GrepSearchResult> searchMultiline(string file, string searchPattern, SearchDelegates.DoSearchMultiline searchMethod, Encoding encoding)
		{
			List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

			using (StreamReader readStream = new StreamReader(File.OpenRead(file), encoding))
			{
				List<GrepSearchResult.GrepLine> lines = new List<GrepSearchResult.GrepLine>();
				string fileBody = readStream.ReadToEnd();
				lines = searchMethod(fileBody, searchPattern);
				Utils.CleanResults(ref lines);
				if (lines.Count > 0)
				{
					searchResults.Add(new GrepSearchResult(file, lines));
				}
			}
				
			return searchResults;
		}

		private bool replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchDelegates.DoSearch searchMethod, SearchDelegates.DoReplace replaceMethod, Encoding encoding)
		{
			using (StreamReader readStream = new StreamReader(File.OpenRead(sourceFile), encoding))
			using (StreamWriter writeStream = new StreamWriter(File.OpenWrite(destinationFile), encoding))
			{
				string line = null;
				int counter = 1;

				while ((line = readStream.ReadLine()) != null)
				{
					if (searchMethod(line, searchPattern))
					{
						line = replaceMethod(line, searchPattern, replacePattern);
					}
					writeStream.WriteLine(line);
					counter++;
				}
			}
				
			return true;
		}

		private bool replaceMultiline(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchDelegates.DoSearchMultiline searchMethod, SearchDelegates.DoReplace replaceMethod, Encoding encoding)
		{
			using (StreamReader readStream = new StreamReader(File.OpenRead(sourceFile), encoding))
			using (StreamWriter writeStream = new StreamWriter(File.OpenWrite(destinationFile), encoding))
			{
				string fileBody = readStream.ReadToEnd();

				fileBody = replaceMethod(fileBody, searchPattern, replacePattern);
				writeStream.Write(fileBody);
			}

			return true;
		}

		#endregion
	}
}
