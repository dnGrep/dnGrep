using System;
using System.Collections.Generic;
using System.Text;
using dnGREP.Common;
using System.IO;
using NLog;
using System.Reflection;

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

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            using (FileStream fileStream = File.OpenRead(file))
            {
                return Search(fileStream, file, searchPattern, searchType, searchOptions, encoding);
            }
        }

        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
		{
			SearchDelegates.DoSearch searchMethod = doTextSearchCaseSensitive;
			SearchDelegates.DoSearchMultiline searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
			switch (searchType)
			{
				case SearchType.PlainText:
                    if ((searchOptions & GrepSearchOption.CaseSensitive) == GrepSearchOption.CaseSensitive)
					{
                        if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
							searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
						else
							searchMethod = doTextSearchCaseSensitive;
					}
					else
					{
                        if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
							searchMethodMultiline = doTextSearchCaseInsensitiveMultiline;
						else
							searchMethod = doTextSearchCaseInsensitive;
					}
					break;
				case SearchType.Regex:
                    if ((searchOptions & GrepSearchOption.CaseSensitive) == GrepSearchOption.CaseSensitive)
					{
                        if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
							searchMethodMultiline = doRegexSearchCaseSensitiveMultiline;
						else
							searchMethod = doRegexSearchCaseSensitive;
					}
					else
					{
                        if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
							searchMethodMultiline = doRegexSearchCaseInsensitiveMultiline;
						else
							searchMethod = doRegexSearchCaseInsensitive;
					}
					break;
				case SearchType.XPath:
					searchMethodMultiline = doXPathSearch;
					break;
                case SearchType.Soundex:
                    searchMethod = doFuzzySearch;
                    searchMethodMultiline = doFuzzySearchMultiline;
                    break;
			}

            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                return searchMultiline(input, fileName, searchPattern, searchOptions, searchMethodMultiline, encoding);
			else
                return search(input, fileName, searchPattern, searchOptions, searchMethod, encoding);
		}

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        { 
            using (FileStream readStream = File.OpenRead(sourceFile))
            using (FileStream writeStream = File.OpenWrite(destinationFile))
            {
                return Replace(readStream, writeStream, searchPattern, replacePattern, searchType, searchOptions, encoding);
            }
        }

		public bool Replace(Stream readStream, Stream writeStream, string searchPattern, string replacePattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
		{
			SearchDelegates.DoSearch searchMethod = doTextSearchCaseSensitive;
			SearchDelegates.DoSearchMultiline searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
			SearchDelegates.DoReplace replaceMethod = doTextReplaceCaseSensitive;
			switch (searchType)
			{
				case SearchType.PlainText:
                    if ((searchOptions & GrepSearchOption.CaseSensitive) == GrepSearchOption.CaseSensitive)
					{
                        if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
							searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
						else
							searchMethod = doTextSearchCaseSensitive;
						
						replaceMethod = doTextReplaceCaseSensitive;
					}
					else
					{
                        if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
							searchMethodMultiline = doTextSearchCaseInsensitiveMultiline;
						else
							searchMethod = doTextSearchCaseInsensitive;

						replaceMethod = doTextReplaceCaseInsensitive;
					}
					break;
				case SearchType.Regex:
                    if ((searchOptions & GrepSearchOption.CaseSensitive) == GrepSearchOption.CaseSensitive)
					{
                        if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
							searchMethodMultiline = doRegexSearchCaseSensitiveMultiline;
						else
							searchMethod = doRegexSearchCaseSensitive;

						replaceMethod = doRegexReplaceCaseSensitive;
					}
					else
					{
                        if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
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
                case SearchType.Soundex:
                    searchMethod = doFuzzySearch;
                    searchMethodMultiline = doFuzzySearchMultiline;
                    replaceMethod = doFuzzyReplace;
                    break;
			}

            if ((searchOptions & GrepSearchOption.Multiline) == GrepSearchOption.Multiline)
                return replaceMultiline(readStream, writeStream, searchPattern, replacePattern, searchOptions, searchMethodMultiline, replaceMethod, encoding);
			else
                return replace(readStream, writeStream, searchPattern, replacePattern, searchOptions, searchMethod, replaceMethod, encoding);
		}

		public void Unload()
		{
			// Do nothing
		}

		public Version FrameworkVersion
		{
			get
			{
				return Assembly.GetAssembly(typeof(IGrepEngine)).GetName().Version;
			}
		}

		#region Actual Implementation

        private List<GrepSearchResult> search(Stream input, string fileName, string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod, Encoding encoding)
		{
			List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

            using (StreamReader readStream = new StreamReader(input, encoding))
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

                        preContextLines.Enqueue(new GrepSearchResult.GrepLine(counter, line, true, null));

						if (collectPostContextLines)
						{
							if (postContextLines.Count < linesAfter)
							{
                                postContextLines.Enqueue(new GrepSearchResult.GrepLine(counter, line, true, null));
							}
							else
							{
								collectPostContextLines = false;
								lines.AddRange(postContextLines);
								postContextLines.Clear();
							}
						}
					}

                    if (searchMethod(line, searchPattern, searchOptions))
					{
                        lines.Add(new GrepSearchResult.GrepLine(counter, line, false, null));
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
                    searchResults.Add(new GrepSearchResult(fileName, lines));
				}
			}
			return searchResults;
		}

        private List<GrepSearchResult> searchMultiline(Stream input, string fileName, string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearchMultiline searchMethod, Encoding encoding)
		{
			List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

			using (StreamReader readStream = new StreamReader(input, encoding))
			{
				List<GrepSearchResult.GrepLine> lines = new List<GrepSearchResult.GrepLine>();
				string fileBody = readStream.ReadToEnd();
                lines = searchMethod(fileBody, searchPattern, searchOptions);
				Utils.CleanResults(ref lines);
				if (lines.Count > 0)
				{
                    searchResults.Add(new GrepSearchResult(fileName, lines));
				}
			}
				
			return searchResults;
		}

        private bool replace(Stream inputStream, Stream outputStream, string searchPattern, string replacePattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod, SearchDelegates.DoReplace replaceMethod, Encoding encoding)
		{
            using (StreamReader readStream = new StreamReader(inputStream, encoding))
			{
                StreamWriter writeStream = new StreamWriter(outputStream, encoding);

				string line = null;
				int counter = 1;

				while ((line = readStream.ReadLine()) != null)
				{
                    if (searchMethod(line, searchPattern, searchOptions))
					{
                        line = replaceMethod(line, searchPattern, replacePattern, searchOptions);
					}
					writeStream.WriteLine(line);
					counter++;
				}

                writeStream.Flush();
			}
				
			return true;
		}

        private bool replaceMultiline(Stream inputStream, Stream outputStream, string searchPattern, string replacePattern, GrepSearchOption searchOptions, SearchDelegates.DoSearchMultiline searchMethod, SearchDelegates.DoReplace replaceMethod, Encoding encoding)
		{
            using (StreamReader readStream = new StreamReader(inputStream, encoding))
			{
                StreamWriter writeStream = new StreamWriter(outputStream, encoding);

				string fileBody = readStream.ReadToEnd();

                fileBody = replaceMethod(fileBody, searchPattern, replacePattern, searchOptions);
				writeStream.Write(fileBody);

                writeStream.Flush();
			}

			return true;
		}

		#endregion
	}
}
