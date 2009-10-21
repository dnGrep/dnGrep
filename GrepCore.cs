using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NLog;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace dnGREP
{
	public class GrepCore
	{
		private bool showLinesInContext = false;

		public bool ShowLinesInContext
		{
			get { return showLinesInContext; }
			set { showLinesInContext = value; }
		}
		private int linesBefore = 0;

		public int LinesBefore
		{
			get { return linesBefore; }
			set { linesBefore = value; }
		}
		private int linesAfter = 0;

		public int LinesAfter
		{
			get { return linesAfter; }
			set { linesAfter = value; }
		}
		
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
		private delegate List<GrepSearchResult.GrepLine> doSearchMultiline(string text, string searchPattern);
		private delegate string doReplace(string text, string searchPattern, string replacePattern);
		
		/// <summary>
		/// Searches folder for files whose content matches regex
		/// </summary>
		/// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
		/// <param name="searchRegex">Regex pattern</param>
		/// <returns>List of results</returns>
		public GrepSearchResult[] SearchRegex(string[] files, string searchRegex, bool isCaseSensitive, bool isMultiline, int codePage)
		{
			if (files == null || files.Length == 0)
				return null;

			if (searchRegex == null || searchRegex.Trim() == "")
			{
				searchResults = new List<GrepSearchResult>();
				foreach (string file in files)
				{
					searchResults.Add(new GrepSearchResult(file, null));
				}
				return searchResults.ToArray();
			}

			if (isCaseSensitive)
			{
				if (isMultiline)
					return searchMultiline(files, searchRegex, new doSearchMultiline(doRegexSearchCaseSensitiveMultiline), codePage);
				else
					return search(files, searchRegex, new doSearch(doRegexSearchCaseSensitive), codePage);
			}
			else
			{
				if (isMultiline)
					return searchMultiline(files, searchRegex, new doSearchMultiline(doRegexSearchCaseInsensitiveMultiline), codePage);
				else
					return search(files, searchRegex, new doSearch(doRegexSearchCaseInsensitive), codePage);
			}
		}

		public GrepSearchResult[] SearchXPath(string[] files, string searchXPath, int codePage)
		{
			if (files == null || files.Length == 0)
				return null;

			if (searchXPath == null || searchXPath.Trim() == "")
			{
				searchResults = new List<GrepSearchResult>();
				foreach (string file in files)
				{
					searchResults.Add(new GrepSearchResult(file, null));
				}
				return searchResults.ToArray();
			}
			else
			{
				return searchMultiline(files, searchXPath, new doSearchMultiline(doXPathSearch), codePage);
			}
		}

		/// <summary>
		/// Searches folder for files whose content matches text
		/// </summary>
		/// <param name="files">Files to search in. If one of the files does not exist or is open, it is skipped.</param>
		/// <param name="searchText">Text</param>
		/// <returns></returns>
		public GrepSearchResult[] SearchText(string[] files, string searchText, bool isCaseSensitive, bool isMultiline, int codePage)
		{
			if (files == null || files.Length == 0)
				return null;

			if (searchText == null || searchText.Trim() == "")
			{
				searchResults = new List<GrepSearchResult>();
				foreach (string file in files)
				{
					searchResults.Add(new GrepSearchResult(file, null));
				}
				return searchResults.ToArray();
			}

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

		public int ReplaceRegex(string[] files, string baseFolder, string searchRegex, string replaceRegex, bool isCaseSensitive, bool isMultiline, int codePage)
		{
			string tempFolder = Utils.FixFolderName(Path.GetTempPath()) + "dnGREP\\";
			if (Directory.Exists(tempFolder))
				Utils.DeleteFolder(tempFolder);
			Directory.CreateDirectory(tempFolder);
			if (isCaseSensitive)
			{
				if (isMultiline)
					return replaceMultiline(files, baseFolder, tempFolder, searchRegex, replaceRegex, new doSearchMultiline(doRegexSearchCaseSensitiveMultiline), new doReplace(doRegexReplaceCaseSensitive), codePage);
				else
					return replace(files, baseFolder, tempFolder, searchRegex, replaceRegex, new doSearch(doRegexSearchCaseSensitive), new doReplace(doRegexReplaceCaseSensitive), codePage);
			}
			else
			{
				if (isMultiline)
					return replaceMultiline(files, baseFolder, tempFolder, searchRegex, replaceRegex, new doSearchMultiline(doRegexSearchCaseInsensitiveMultiline), new doReplace(doRegexReplaceCaseInsensitive), codePage);
				else
					return replace(files, baseFolder, tempFolder, searchRegex, replaceRegex, new doSearch(doRegexSearchCaseInsensitive), new doReplace(doRegexReplaceCaseInsensitive), codePage);
			}
		}

		public int ReplaceXPath(string[] files, string baseFolder, string searchXPath, string replaceText, int codePage)
		{
			string tempFolder = Utils.FixFolderName(Path.GetTempPath()) + "dnGREP\\";
			if (Directory.Exists(tempFolder))
				Utils.DeleteFolder(tempFolder);
			Directory.CreateDirectory(tempFolder);
			return replaceMultiline(files, baseFolder, tempFolder, searchXPath, replaceText, new doSearchMultiline(doXPathSearch), new doReplace(doXPathReplace), codePage);
		}

		public int ReplaceText(string[] files, string baseFolder, string searchText, string replaceText, bool isCaseSensitive, bool isMultiline, int codePage)
		{
			string tempFolder = Utils.FixFolderName(Path.GetTempPath()) + "dnGREP\\";
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
			string tempFolder = Utils.FixFolderName(Path.GetTempPath()) + "dnGREP\\";
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

		private bool doRegexSearchCaseInsensitive(string text, string searchPattern)
		{
			return Regex.IsMatch(text, searchPattern, RegexOptions.IgnoreCase);
		}

		private bool doRegexSearchCaseSensitive(string text, string searchPattern)
		{
			return Regex.IsMatch(text, searchPattern);
		}

		private List<GrepSearchResult.GrepLine> doXPathSearch(string text, string searchXPath)
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

		private List<GrepSearchResult.GrepLine> doRegexSearchCaseSensitiveMultiline(string text, string searchPattern)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
			foreach(Match match in Regex.Matches(text, searchPattern, RegexOptions.Multiline)) 
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

		private List<GrepSearchResult.GrepLine> doRegexSearchCaseInsensitiveMultiline(string text, string searchPattern)
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

		private List<GrepSearchResult.GrepLine> doTextSearchCaseInsensitiveMultiline(string text, string searchText)
		{
			List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
			int index = 0;
			while (index >= 0) {
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
					index ++;
				}
			}
			return results;
		}

		private List<GrepSearchResult.GrepLine> doTextSearchCaseSensitiveMultiline(string text, string searchText)
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

		private string doRegexReplaceCaseInsensitive(string text, string searchPattern, string replacePattern)
		{
			return Regex.Replace(text, searchPattern, replacePattern, RegexOptions.IgnoreCase);
		}

		private string doRegexReplaceCaseSensitive(string text, string searchPattern, string replacePattern)
		{
			return Regex.Replace(text, searchPattern, replacePattern);
		}

		private string doXPathReplace(string text, string searchXPath, string replaceText)
		{
			if (text.Length > 5 && text.Substring(0, 5).ToLower() == "<?xml")
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(text);
				XmlNodeList xmlNodes = xmlDoc.SelectNodes(searchXPath);
				string line = "";
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
						Queue<GrepSearchResult.GrepLine> preContextLines = new Queue<GrepSearchResult.GrepLine>();
						Queue<GrepSearchResult.GrepLine> postContextLines = new Queue<GrepSearchResult.GrepLine>();
						bool collectPostContextLines = false;
						while ((line = readStream.ReadLine()) != null)
						{
							if (GrepCore.CancelProcess)
							{
								return searchResults.ToArray();
							}

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
						lines = searchMethod(fileBody, searchPattern);
						Utils.CleanResults(ref lines);
						if (lines.Count > 0)
						{
							searchResults.Add(new GrepSearchResult(file, lines));
						}
						if (ProcessedFile != null)
							ProcessedFile(this, new ProgressStatus(totalFiles, processedFiles));
						if (GrepCore.CancelProcess)
						{
							return searchResults.ToArray();
						}
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
