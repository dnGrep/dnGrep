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
		private delegate int[] doSearchMultiline(string text, string searchPattern);
		private delegate string doReplace(string text, string searchPattern, string replacePattern);
		
		/// <summary>
		/// Searches folder for files whose content matches regex
		/// </summary>
		/// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
		/// <param name="searchRegex">Regex pattern</param>
		/// <returns>List of results</returns>
		public GrepSearchResult[] SearchRegex(string[] files, string searchRegex, bool isMultiline, int codePage)
		{
			if (isMultiline)
				return searchMultiline(files, searchRegex, new doSearchMultiline(doRegexSearchMultiline), codePage);
			else
				return search(files, searchRegex, new doSearch(doRegexSearch), codePage);
		}

		/// <summary>
		/// Searches folder for files whose content matches text
		/// </summary>
		/// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
		/// <param name="searchText">Text</param>
		/// <returns></returns>
		public GrepSearchResult[] SearchText(string[] files, string searchText, bool isCaseSensitive, bool isMultiline, int codePage)
		{
			if (isCaseSensitive)
			{
				if (isMultiline)
					return searchMultiline(files, searchText, new doSearchMultiline(doTextSearchCaseSensitiveMultiline), codePage);
				else
					return search(files, searchText, new doSearch(doTextSearchCaseSensitive), codePage);
			}
			else
			{
				if (isMultiline)
					return searchMultiline(files, searchText, new doSearchMultiline(doTextSearchCaseInsensitiveMultiline), codePage);
				else
					return search(files, searchText, new doSearch(doTextSearchCaseInsensitive), codePage);
			}
		}

		public int ReplaceRegex(string[] files, string baseFolder, string searchRegex, string replaceRegex, bool isMultiline, int codePage)
		{
			string tempFolder = Utils.FixFolderName(Path.GetTempPath()) + "nGREP\\";
			if (Directory.Exists(tempFolder))
				Utils.DeleteFolder(tempFolder);
			Directory.CreateDirectory(tempFolder);
			if (isMultiline)
				return replaceMultiline(files, baseFolder, tempFolder, searchRegex, replaceRegex, new doSearchMultiline(doRegexSearchMultiline), new doReplace(doRegexReplace), codePage);
			else
				return replace(files, baseFolder, tempFolder, searchRegex, replaceRegex, new doSearch(doRegexSearch), new doReplace(doRegexReplace), codePage);
		}

		public int ReplaceText(string[] files, string baseFolder, string searchText, string replaceText, bool isCaseSensitive, bool isMultiline, int codePage)
		{
			string tempFolder = Utils.FixFolderName(Path.GetTempPath()) + "nGREP\\";
			if (Directory.Exists(tempFolder))
				Utils.DeleteFolder(tempFolder);
			Directory.CreateDirectory(tempFolder);
			if (isCaseSensitive)
			{
				if (isMultiline)
					return replaceMultiline(files, baseFolder, tempFolder, searchText, replaceText, new doSearchMultiline(doTextSearchCaseSensitiveMultiline), new doReplace(doTextReplaceCaseSensitive), codePage);
				else
					return replace(files, baseFolder, tempFolder, searchText, replaceText, new doSearch(doTextSearchCaseSensitive), new doReplace(doTextReplaceCaseSensitive), codePage);
			}
			else
			{
				if (isMultiline)
					return replaceMultiline(files, baseFolder, tempFolder, searchText, replaceText, new doSearchMultiline(doTextSearchCaseInsensitiveMultiline), new doReplace(doTextReplaceCaseInsensitive), codePage);
				else
					return replace(files, baseFolder, tempFolder, searchText, replaceText, new doSearch(doTextSearchCaseInsensitive), new doReplace(doTextReplaceCaseInsensitive), codePage);
			}
		}

		public bool Undo(string folderPath)
		{
			string tempFolder = Utils.FixFolderName(Path.GetTempPath()) + "nGREP\\";
			if (!Directory.Exists(tempFolder))
			{
				logger.Error("Failed to undo replacement as temporary directory was removed.");
				return false;
			}
			try
			{
				Utils.CopyFiles(tempFolder, folderPath, null, null);
				return true;
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, "Failed to undo replacement", ex);
				return false;
			}
		}

		private bool doTextSearchCaseInsensitive(string text, string searchText)
		{
			return text.ToLower().Contains(searchText.ToLower());
		}

		private bool doTextSearchCaseSensitive(string text, string searchText)
		{
			return text.Contains(searchText);
		}

		private bool doRegexSearch(string text, string searchPattern)
		{
			return Regex.IsMatch(text, searchPattern);
		}

		private int[] doRegexSearchMultiline(string text, string searchPattern)
		{
			List<int> results = new List<int>();
			foreach(Match match in Regex.Matches(text, searchPattern)) 
			{
				results.Add(match.Index);
			}
			return results.ToArray();
		}

		private int[] doTextSearchCaseInsensitiveMultiline(string text, string searchText)
		{
			List<int> results = new List<int>();
			int index = 0;
			while (index >= 0) {
				index = text.IndexOf(searchText, index, StringComparison.InvariantCultureIgnoreCase);
				if (index >= 0)
				{
					results.Add(index);
					index ++;
				}
			}
			return results.ToArray();
		}

		private int[] doTextSearchCaseSensitiveMultiline(string text, string searchText)
		{
			List<int> results = new List<int>();
			int index = 0;
			while (index >= 0)
			{
				index = text.IndexOf(searchText, index, StringComparison.InvariantCulture);
				if (index >= 0)
				{
					results.Add(index);
					index++;
				}
			}
			return results.ToArray();
		}

		private string doTextReplaceCaseSensitive(string text, string searchText, string replaceText)
		{
			return text.Replace(searchText, replaceText);
		}
		
		private string doTextReplaceCaseInsensitive(string text, string searchText, string replaceText)
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

		private string doRegexReplace(string text, string searchPattern, string replacePattern)
		{
			return Regex.Replace(text, searchPattern, replacePattern);
		}

		private GrepSearchResult[] search(string[] files, string searchPattern, doSearch searchMethod, int codePage)
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

					Encoding encoding = null;
					if (codePage == -1)
						encoding = Utils.GetFileEncoding(file);
					else
						encoding = Encoding.GetEncoding(codePage);
					
					using (StreamReader readStream = new StreamReader(File.OpenRead(file), encoding))
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

		private GrepSearchResult[] searchMultiline(string[] files, string searchPattern, doSearchMultiline searchMethod, int codePage)
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

					Encoding encoding = null;
					if (codePage == -1)
						encoding = Utils.GetFileEncoding(file);
					else
						encoding = Encoding.GetEncoding(codePage);

					using (StreamReader readStream = new StreamReader(File.OpenRead(file), encoding))
					{
						List<GrepSearchResult.GrepLine> lines = new List<GrepSearchResult.GrepLine>();
						string fileBody = readStream.ReadToEnd();
						foreach (int index in searchMethod(fileBody, searchPattern))
						{
							if (GrepCore.CancelProcess)
							{
								return searchResults.ToArray();
							}

							int lineNumber = -1;
							string line = Utils.GetLine(fileBody, index, out lineNumber);
							if (lineNumber != -1)
								lines.Add(new GrepSearchResult.GrepLine(lineNumber, line));
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

		private int replace(string[] files, string baseFolder, string tempFolder, string searchPattern, string replacePattern, doSearch searchMethod, doReplace replaceMethod, int codePage)
		{
			if (files == null || files.Length == 0 || !Directory.Exists(tempFolder) || !Directory.Exists(baseFolder))
				return 0;

			baseFolder = Utils.FixFolderName(baseFolder);
			tempFolder = Utils.FixFolderName(tempFolder);

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
					Utils.CopyFile(file, tempFileName, true);
					Utils.DeleteFile(file);

					Encoding encoding = null;
					if (codePage == -1)
						encoding = Utils.GetFileEncoding(tempFileName);
					else
						encoding = Encoding.GetEncoding(codePage);

					using (StreamReader readStream = new StreamReader(File.OpenRead(tempFileName), encoding))
					using (StreamWriter writeStream = new StreamWriter(File.OpenWrite(file), encoding))
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

					File.SetAttributes(file, File.GetAttributes(tempFileName));

					if (GrepCore.CancelProcess)
					{
						// Replace the file
						Utils.DeleteFile(file);
						Utils.CopyFile(tempFileName, file, true);
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
							Utils.DeleteFile(file);
							Utils.CopyFile(tempFileName, file, true);
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

		private int replaceMultiline(string[] files, string baseFolder, string tempFolder, string searchPattern, string replacePattern, doSearchMultiline searchMethod, doReplace replaceMethod, int codePage)
		{
			if (files == null || files.Length == 0 || !Directory.Exists(tempFolder) || !Directory.Exists(baseFolder))
				return 0;

			baseFolder = Utils.FixFolderName(baseFolder);
			tempFolder = Utils.FixFolderName(tempFolder);

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
					Utils.CopyFile(file, tempFileName, true);
					Utils.DeleteFile(file);

					Encoding encoding = null;
					if (codePage == -1)
						encoding = Utils.GetFileEncoding(tempFileName);
					else
						encoding = Encoding.GetEncoding(codePage);

					using (StreamReader readStream = new StreamReader(File.OpenRead(tempFileName), encoding))
					using (StreamWriter writeStream = new StreamWriter(File.OpenWrite(file), encoding))
					{
						string fileBody = readStream.ReadToEnd();

						if (GrepCore.CancelProcess)
						{
							break;
						}

						fileBody = replaceMethod(fileBody, searchPattern, replacePattern);
						writeStream.Write(fileBody);

						if (!GrepCore.CancelProcess && ProcessedFile != null)
							ProcessedFile(this, new ProgressStatus(totalFiles, processedFiles));
					}

					File.SetAttributes(file, File.GetAttributes(tempFileName));

					if (GrepCore.CancelProcess)
					{
						// Replace the file
						Utils.DeleteFile(file);
						Utils.CopyFile(tempFileName, file, true);
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
							Utils.DeleteFile(file);
							Utils.CopyFile(tempFileName, file, true);
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
