using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NLog;
using System.Text.RegularExpressions;

namespace nGREP
{
	internal class GrepCore
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private List<GrepSearchResult> searchResults = new List<GrepSearchResult>();
		public delegate void SearchProgressHandler(object sender, ProgressStatus files);
		public event SearchProgressHandler ProcessedFile;
		public class ProgressStatus
		{
			public ProgressStatus(int total, int processed)
			{
				TotalFiles = total;
				ProcessedFiles = processed;
			}
			public int TotalFiles;
			public int ProcessedFiles;
		}

		private static bool cancelProcess = false;

		public static bool CancelProcess
		{
			get { return GrepCore.cancelProcess; }
			set { GrepCore.cancelProcess = value; }
		}

		private delegate bool doSearch(string text, string searchText, Regex searchPattern);
		
		/// <summary>
		/// Searches folder for files whose content matches regex
		/// </summary>
		/// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
		/// <param name="searchRegex">Regex pattern</param>
		/// <returns>List of results</returns>
		public GrepSearchResult[] Search(string[] files, Regex searchRegex)
		{
			return search(files, null, searchRegex, new doSearch(doRegexSearch));
		}

		/// <summary>
		/// Searches folder for files whose content matches text
		/// </summary>
		/// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
		/// <param name="searchText">Text</param>
		/// <returns></returns>
		public GrepSearchResult[] Search(string[] files, string searchText)
		{
			return search(files, searchText, null, new doSearch(doTextSearchCaseInsensitive));
		}

		private bool doTextSearchCaseInsensitive(string text, string searchText, Regex searchPattern)
		{
			return text.Contains(searchText);
		}

		private bool doRegexSearch(string text, string searchText, Regex searchPattern)
		{
			return searchPattern.IsMatch(text);
		}

		private GrepSearchResult[] search(string[] files, string searchText, Regex searchRegex, doSearch searchMethod)
		{
			if (files == null || files.Length == 0)
				return new GrepSearchResult[0];

			searchResults = new List<GrepSearchResult>();

			int totalFiles = files.Length;
			int processedFiles = 0;
			GrepCore.CancelProcess = false;

			foreach (string file in files)
			{
				try
				{
					processedFiles++;
					using (StreamReader readStream = new StreamReader(File.OpenRead(file)))
					{
						string line = null;
						int counter = 1;
						List<GrepSearchResult.GrepLine> lines = new List<GrepSearchResult.GrepLine>();
						while ((line = readStream.ReadLine()) != null)
						{
							if (GrepCore.CancelProcess)
							{
								return searchResults.ToArray();
							}

							if (searchMethod(line, searchText, searchRegex))
							{
								lines.Add(new GrepSearchResult.GrepLine(counter, line));
							}
							counter++;
						}
						if (lines.Count > 0)
						{
							searchResults.Add(new GrepSearchResult(file, lines));
						}
						if (ProcessedFile != null)
							ProcessedFile(this, new ProgressStatus(totalFiles, processedFiles));
					}
				}
				catch (Exception ex)
				{
					logger.LogException(LogLevel.Error, ex.Message, ex);
				}
			}
			return searchResults.ToArray();
		}
	}
}
