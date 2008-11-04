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

		private delegate bool doSearch(string text, string searchPattern);
		private delegate string doReplace(string text, string searchPattern, string replacePattern);
		
		/// <summary>
		/// Searches folder for files whose content matches regex
		/// </summary>
		/// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
		/// <param name="searchRegex">Regex pattern</param>
		/// <returns>List of results</returns>
		public GrepSearchResult[] SearchRegex(string[] files, string searchRegex)
		{
			return search(files, searchRegex, new doSearch(doRegexSearch));
		}

		/// <summary>
		/// Searches folder for files whose content matches text
		/// </summary>
		/// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
		/// <param name="searchText">Text</param>
		/// <returns></returns>
		public GrepSearchResult[] SearchText(string[] files, string searchText)
		{
			return search(files, searchText, new doSearch(doTextSearchCaseInsensitive));
		}

		public int ReplaceRegex(string[] files, string baseFolder, string searchRegex, string replaceRegex)
		{
			string tempFolder = FileUtils.FixFolderName(Path.GetTempPath()) + "nGREP\\";
			if (Directory.Exists(tempFolder))
				Directory.Delete(tempFolder, true);
			Directory.CreateDirectory(tempFolder);
			return replace(files, baseFolder, tempFolder, searchRegex, replaceRegex, new doSearch(doRegexSearch), new doReplace(doRegexReplace));
		}

		public int ReplaceText(string[] files, string baseFolder, string searchText, string replaceText)
		{
			string tempFolder = FileUtils.FixFolderName(Path.GetTempPath()) + "nGREP\\";
			if (Directory.Exists(tempFolder))
				Directory.Delete(tempFolder, true);
			Directory.CreateDirectory(tempFolder);
			return replace(files, baseFolder, tempFolder, searchText, replaceText, new doSearch(doTextSearchCaseInsensitive), new doReplace(doTextReplaceCaseInsensitive));
		}

		private bool doTextSearchCaseInsensitive(string text, string searchText)
		{
			return text.Contains(searchText);
		}

		private bool doRegexSearch(string text, string searchPattern)
		{
			return Regex.IsMatch(text, searchPattern);
		}

		private string doTextReplaceCaseInsensitive(string text, string searchText, string replaceText)
		{
			return text.Replace(searchText, replaceText);
		}

		private string doRegexReplace(string text, string searchPattern, string replacePattern)
		{
			return Regex.Replace(text, searchPattern, replacePattern);
		}

		private GrepSearchResult[] search(string[] files, string searchPattern, doSearch searchMethod)
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

							if (searchMethod(line, searchPattern))
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

		private int replace(string[] files, string baseFolder, string tempFolder, string searchPattern, string replacePattern, doSearch searchMethod, doReplace replaceMethod)
		{
			if (files == null || files.Length == 0 || !Directory.Exists(tempFolder) || !Directory.Exists(baseFolder))
				return 0;

			baseFolder = FileUtils.FixFolderName(baseFolder);
			tempFolder = FileUtils.FixFolderName(tempFolder);

			int totalFiles = files.Length;
			int processedFiles = 0;
			GrepCore.CancelProcess = false;

			foreach (string file in files)
			{
				string tempFileName = file.Replace(baseFolder, tempFolder);
				try
				{
					processedFiles++;
					// Copy file					
					FileUtils.CopyFile(file, tempFileName, true);
					File.Delete(file);

					using (StreamReader readStream = new StreamReader(File.OpenRead(tempFileName)))
					using (StreamWriter writeStream = new StreamWriter(File.OpenWrite(file)))
					{
						string line = null;
						int counter = 1;

						while ((line = readStream.ReadLine()) != null)
						{
							if (GrepCore.CancelProcess)
							{
								break;
							}

							if (searchMethod(line, searchPattern))
							{
								line = replaceMethod(line, searchPattern, replacePattern);
							}
							writeStream.WriteLine(line);
							counter++;
						}

						if (!GrepCore.CancelProcess && ProcessedFile != null)
							ProcessedFile(this, new ProgressStatus(totalFiles, processedFiles));
					}

					if (GrepCore.CancelProcess)
					{
						// Replace the file
						File.Delete(file);
						FileUtils.CopyFile(tempFileName, file, true);
						break;
					}
				}
				catch (Exception ex)
				{
					logger.LogException(LogLevel.Error, ex.Message, ex);
					try
					{
						// Replace the file
						if (File.Exists(tempFileName) && File.Exists(file))
						{
							File.Delete(file);
							FileUtils.CopyFile(tempFileName, file, true);
						}
					}
					catch (Exception ex2)
					{
						// DO NOTHING
					}
					return -1;
				}
			}
			return processedFiles;
		}
	}
}
