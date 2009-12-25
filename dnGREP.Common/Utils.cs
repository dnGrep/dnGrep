using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.Net;
using System.Xml;
using NLog;

namespace dnGREP.Common
{
	public class Utils
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Copies the folder recursively. Uses includePattern to avoid unnecessary objects
		/// </summary>
		/// <param name="sourceDirectory"></param>
		/// <param name="destinationDirectory"></param>
		/// <param name="includePattern">Regex pattern that matches file or folder to be included. If null or empty, the parameter is ignored</param>
		/// <param name="excludePattern">Regex pattern that matches file or folder to be included. If null or empty, the parameter is ignored</param>
		public static void CopyFiles(string sourceDirectory, string destinationDirectory, string includePattern, string excludePattern)
		{
			String[] files;

			destinationDirectory = FixFolderName(destinationDirectory);

			if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

			files = Directory.GetFileSystemEntries(sourceDirectory);

			foreach (string element in files)
			{
				if (!string.IsNullOrEmpty(includePattern) && File.Exists(element) && !Regex.IsMatch(element, includePattern))
					continue;

				if (!string.IsNullOrEmpty(excludePattern) && File.Exists(element) && Regex.IsMatch(element, excludePattern))
					continue;

				// Sub directories
				if (Directory.Exists(element))
					CopyFiles(element, destinationDirectory + Path.GetFileName(element), includePattern, excludePattern);
				// Files in directory
				else
					CopyFile(element, destinationDirectory + Path.GetFileName(element), true);
			}
		}

		/// <summary>
		/// Copies file based on search results. If folder does not exist, creates it.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="sourceDirectory"></param>
		/// <param name="destinationDirectory"></param>
		/// <param name="overWrite"></param>
		public static void CopyFiles(List<GrepSearchResult> source, string sourceDirectory, string destinationDirectory, bool overWrite)
		{
			sourceDirectory = FixFolderName(sourceDirectory);
			destinationDirectory = FixFolderName(destinationDirectory);

			if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

			List<string> files = new List<string>();

			foreach (GrepSearchResult result in source)
			{
				if (!files.Contains(result.FileNameReal) && result.FileNameDisplayed.Contains(sourceDirectory))
				{
					files.Add(result.FileNameReal);
					FileInfo sourceFileInfo = new FileInfo(result.FileNameReal);
					FileInfo destinationFileInfo = new FileInfo(destinationDirectory + result.FileNameReal.Substring(sourceDirectory.Length));
					if (sourceFileInfo.FullName != destinationFileInfo.FullName)
						CopyFile(sourceFileInfo.FullName, destinationFileInfo.FullName, overWrite);
				}
			}
		}

		/// <summary>
		/// Returns true if destinationDirectory is not included in source files
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destinationDirectory"></param>
		/// <returns></returns>
		public static bool CanCopyFiles(List<GrepSearchResult> source, string destinationDirectory)
		{
			if (destinationDirectory == null || source == null || source.Count == 0)
				return false;

			destinationDirectory = FixFolderName(destinationDirectory);

			List<string> files = new List<string>();

			foreach (GrepSearchResult result in source)
			{
				if (!files.Contains(result.FileNameReal))
				{
					files.Add(result.FileNameReal);
					FileInfo sourceFileInfo = new FileInfo(result.FileNameReal);
					FileInfo destinationFileInfo = new FileInfo(destinationDirectory + Path.GetFileName(result.FileNameReal));
					if (sourceFileInfo.FullName == destinationFileInfo.FullName)
						return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Creates a CSV file from search results
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destinationPath"></param>
		public static void SaveResultsAsCSV(List<GrepSearchResult> source, string destinationPath)
		{
			if (File.Exists(destinationPath))
				File.Delete(destinationPath);

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("File Name,Line Number,String");
			foreach (GrepSearchResult result in source)
			{
				foreach (GrepSearchResult.GrepLine line in result.SearchResults)
				{
					if (!line.IsContext)
						sb.AppendLine("\"" + result.FileNameDisplayed + "\"," + line.LineNumber + ",\"" + line.LineText + "\"");
				}
			}
			File.WriteAllText(destinationPath, sb.ToString());
		}

		/// <summary>
		/// Deletes file based on search results. 
		/// </summary>
		/// <param name="source"></param>
		public static void DeleteFiles(List<GrepSearchResult> source)
		{
			List<string> files = new List<string>();

			foreach (GrepSearchResult result in source)
			{
				if (!files.Contains(result.FileNameReal))
				{
					files.Add(result.FileNameReal);
					DeleteFile(result.FileNameReal);
				}
			}
		}

		/// <summary>
		/// Copies file. If folder does not exist, creates it.
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <param name="destinationPath"></param>
		/// <param name="overWrite"></param>
		public static void CopyFile(string sourcePath, string destinationPath, bool overWrite)
		{
			if (File.Exists(destinationPath) && !overWrite)
				throw new IOException("File: '" + destinationPath + "' exists.");

			if (!new FileInfo(destinationPath).Directory.Exists)
				new FileInfo(destinationPath).Directory.Create();

			File.Copy(sourcePath, destinationPath, overWrite);
		}

		/// <summary>
		/// Deletes files even if they are read only
		/// </summary>
		/// <param name="path"></param>
		public static void DeleteFile(string path)
		{
			if (File.Exists(path)) {
				File.SetAttributes(path, FileAttributes.Normal);
				File.Delete(path);
			}
		}

		/// <summary>
		/// Deletes folder even if it contains read only files
		/// </summary>
		/// <param name="path"></param>
		public static void DeleteFolder(string path)
		{
			string[] files = GetFileList(path, "*.*", false, true, true, 0, 0);
			foreach (string file in files)
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}
			Directory.Delete(path, true);
		}

		/// <summary>
		/// Detects the byte order mark of a file and returns
		/// an appropriate encoding for the file.
		/// </summary>
		/// <param name="srcFile"></param>
		/// <returns></returns>
		public static Encoding GetFileEncoding(string srcFile)
		{
			// *** Use Default of Encoding.Default (Ansi CodePage)
			Encoding enc = Encoding.Default;

			// *** Detect byte order mark if any - otherwise assume default
			byte[] buffer = new byte[5];
			using (FileStream readStream = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				readStream.Read(buffer, 0, 5);
			}
			if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
				enc = Encoding.UTF8;
			else if (buffer[0] == 0xfe && buffer[1] == 0xff)
				enc = Encoding.Unicode;
			else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
				enc = Encoding.UTF32;
			else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
				enc = Encoding.UTF7;
			return enc;
		}

		/// <summary>
		/// Add DirectorySeparatorChar to the end of the folder path if does not exist
		/// </summary>
		/// <param name="name">Folder path</param>
		/// <returns></returns>
		public static string FixFolderName(string name)
		{
			if (name != null && name.Length > 1 && name[name.Length - 1] != Path.DirectorySeparatorChar)
				name += Path.DirectorySeparatorChar;
			return name;
		}

		/// <summary>
		/// Validates whether the path is a valid directory, file, or list of files
		/// </summary>
		/// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
		/// <returns>True is all paths are valid, otherwise false</returns>
		public static bool IsPathValid(string path)
		{
			try
			{
				if (string.IsNullOrEmpty(path))
					return false;

				string[] paths = path.Split(';');
				foreach (string subPath in paths)
				{
					if (subPath.Trim() != "" && !File.Exists(subPath) && !Directory.Exists(subPath))
						return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		/// <summary>
		/// Returns base folder of one or many files or folders. 
		/// If multiple files are passed in, takes the first one.
		/// </summary>
		/// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
		/// <returns>Base folder path or null if none exists</returns>
		public static string GetBaseFolder(string path)
		{
			try
			{
				string[] paths = path.Split(';');
				if (paths[0].Trim() != "" && File.Exists(paths[0]))
					return Path.GetDirectoryName(paths[0]);
				else if (paths[0].Trim() != "" && Directory.Exists(paths[0]))
					return paths[0];
				else
					return null;
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		/// <summary>
		/// Searches folder and it's subfolders for files that match pattern and
		/// returns array of strings that contain full paths to the files.
		/// If no files found returns 0 length array.
		/// </summary>
		/// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
		/// <param name="namePattern">File name pattern. (E.g. *.cs) or regex. If null returns empty array. If empty string returns all files.</param>
		/// <param name="isRegex">Whether to use regex as search pattern. Otherwise use asterisks</param>
		/// <param name="includeSubfolders">Include sub folders</param>
		/// <param name="includeHidden">Include hidden folders</param>
		/// <param name="sizeFrom">Size in KB</param>
		/// <param name="sizeTo">Size in KB</param>
		/// <returns></returns>
		public static string[] GetFileList(string path, string namePattern, bool isRegex, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo)
		{
			if (string.IsNullOrEmpty(path) || namePattern == null)
			{
				return new string[0];
			}
			else
			{
				List<string> fileMatch = new List<string>();

				string[] paths = path.Split(';');
				foreach (string subPath in paths)
				{
					if (subPath.Trim() == "")
						continue;

					try
					{
						DirectoryInfo di = new DirectoryInfo(subPath);

						if (di.Exists)
						{
							string[] namePatterns = namePattern.Split(';');
							foreach (string pattern in namePatterns)
							{
								recursiveFileSearch(di.FullName, pattern.Trim(), isRegex, includeSubfolders, includeHidden, sizeFrom, sizeTo, fileMatch);
							}
						}
						else if (File.Exists(subPath))
						{
							if (!fileMatch.Contains(subPath))
								fileMatch.Add(subPath);
						}
					}
					catch (Exception ex)
					{
						continue;
					}
				}
				return fileMatch.ToArray();
			}
		}

		private static void recursiveFileSearch(string pathToFolder, string namePattern, bool isRegex, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo, List<string> files)
		{
			DirectoryInfo di = new DirectoryInfo(pathToFolder);
			FileInfo[] fileMatch;
			try
			{
				if (isRegex)
				{
					List<FileInfo> tempFileList = new List<FileInfo>();
					foreach (FileInfo fileInDirectory in di.GetFiles())
					{
						if (Regex.IsMatch(fileInDirectory.Name, namePattern))
							tempFileList.Add(fileInDirectory);
					}

					fileMatch = tempFileList.ToArray();
				}
				else
				{
					fileMatch = di.GetFiles(namePattern, SearchOption.TopDirectoryOnly);
				}
				for (int i = 0; i < fileMatch.Length; i++)
				{
					if (sizeFrom > 0 || sizeTo > 0)
					{
						long sizeKB = fileMatch[i].Length / 1000;
						if (sizeFrom > 0 && sizeKB < sizeFrom)
						{
							continue;
						}
						if (sizeTo > 0 && sizeKB > sizeTo)
						{
							continue;
						}
					}
					if (!files.Contains(fileMatch[i].FullName))
						files.Add(fileMatch[i].FullName);
				}
				if (includeSubfolders)
				{
					foreach (DirectoryInfo subDir in di.GetDirectories())
					{
						if (((subDir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) && !includeHidden)
						{
							continue;
						}
						else
						{
							recursiveFileSearch(subDir.FullName, namePattern, isRegex, includeSubfolders, includeHidden, sizeFrom, sizeTo, files);
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, ex.Message, ex);
			}
		}

		/// <summary>
		/// Parses text into int
		/// </summary>
		/// <param name="value">String. May include null, empty srting or text with spaces before or after.</param>
		/// <returns>Attempts to parse string. Otherwise returns int.MinValue</returns>
		public static int ParseInt(string value)
		{
			return ParseInt(value, int.MinValue);
		}

		/// <summary>
		/// Parses text into int
		/// </summary>
		/// <param name="value">String. May include null, empty srting or text with spaces before or after.</param>
		/// <param name="defaultValue">Default value if fails to parse.</param>
		/// <returns>Attempts to parse string. Otherwise returns defaultValue</returns>
		public static int ParseInt(string value, int defaultValue)
		{
			if (value != null && value.Length != 0)
			{
				int output;
				value = value.Trim();
				if (int.TryParse(value, out output))
				{
					return output;
				}
			}
			return defaultValue;
		}

		/// <summary>
		/// Parses text into bool
		/// </summary>
		/// <param name="value">String. May include null, empty srting or text with spaces before or after.
		/// Text may be in the format of True/False, Yes/No, Y/N, On/Off, 1/0</param>
		/// <returns></returns>
		public static bool ParseBoolean(string value)
		{
			return ParseBoolean(value, false);
		}

		/// <summary>
		/// Parses text into bool
		/// </summary>
		/// <param name="value">String. May include null, empty srting or text with spaces before or after.
		/// Text may be in the format of True/False, Yes/No, Y/N, On/Off, 1/0</param>
		/// <param name="defaultValue">Default value</param>
		/// <returns></returns>
		public static bool ParseBoolean(string value, bool defaultValue)
		{
			if (value != null && value.Length != 0)
			{
				switch (value.Trim().ToLower())
				{
					case "true":
					case "yes":
					case "y":
					case "on":
					case "1":
						return true;
					case "false":
					case "no":
					case "n":
					case "off":
					case "0":
						return false;
				}
			}
			return defaultValue;
		}

		/// <summary>
		/// Parses text into enum
		/// </summary>
		/// <typeparam name="T">Type of enum</typeparam>
		/// <param name="value">Value to parse</param>
		/// <param name="defaultValue">Default value</param>
		/// <returns></returns>
		public static T ParseEnum<T>(string value, T defaultValue)
		{
			if (string.IsNullOrEmpty(value))
				return defaultValue;

			T result = defaultValue;
			try
			{
				result = (T)Enum.Parse(defaultValue.GetType(), value);
			}
			catch { }

			return result;
		}

		/// <summary>
		/// Open file using either default editor or the one provided via customEditor parameter
		/// </summary>
		/// <param name="fileName">File to open</param>
		/// <param name="line">Line number</param>
		/// <param name="useCustomEditor">True if customEditor parameter is provided</param>
		/// <param name="customEditor">Custom editor path</param>
		/// <param name="customEditorArgs">Arguments for custom editor</param>
		public static void OpenFile(OpenFileArgs args)
		{
			if (!args.UseCustomEditor || args.CustomEditor == null || args.CustomEditor.Trim() == "")
				System.Diagnostics.Process.Start(@"" + args.SearchResult.FileNameDisplayed + "");
			else
			{
				ProcessStartInfo info = new ProcessStartInfo(args.CustomEditor);
				info.UseShellExecute = false;
				info.CreateNoWindow = true;
				if (args.CustomEditorArgs == null)
					args.CustomEditorArgs = "";
				info.Arguments = args.CustomEditorArgs.Replace("%file", "\"" + args.SearchResult.FileNameDisplayed + "\"").Replace("%line", args.LineNumber.ToString());
				System.Diagnostics.Process.Start(info);
			}
		}

		/// <summary>
		/// Returns path to a temp folder used by dnGREP (including trailing slash). If folder does not exist
		/// it gets created.
		/// </summary>
		/// <returns></returns>
		public static string GetTempFolder()
		{
			string tempPath = FixFolderName(GetCurrentPath()) + "~dnGREP-Temp\\";
			if (!Directory.Exists(tempPath))
			{
				DirectoryInfo di = Directory.CreateDirectory(tempPath);
				di.Attributes = FileAttributes.Directory | FileAttributes.Hidden; 
			}
			return tempPath;
		}

		/// <summary>
		/// Deletes temp folder
		/// </summary>
		public static void DeleteTempFolder()
		{
			string tempPath = FixFolderName(GetCurrentPath()) + "~dnGREP-Temp\\";
			try
			{
				if (Directory.Exists(tempPath))
					DeleteFolder(tempPath);
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, "Failed to delete temp folder", ex);
			}
		}

		/// <summary>
		/// Open folder in explorer
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="line"></param>
		public static void OpenContainingFolder(string fileName, int line)
		{
			System.Diagnostics.Process.Start(@"" + Path.GetDirectoryName(fileName) + "");
		}

		
		/// <summary>
		/// Returns current path of DLL without trailing slash
		/// </summary>
		/// <returns></returns>
		public static string GetCurrentPath()
		{
			return GetCurrentPath(typeof(Utils));
		}

		/// <summary>
		/// Returns current path of DLL without trailing slash
		/// </summary>
		/// <param name="type">Type to check</param>
		/// <returns></returns>
		public static string GetCurrentPath(Type type)
		{
			Assembly thisAssembly = Assembly.GetAssembly(type);
			return Path.GetDirectoryName(thisAssembly.Location);
		}

		/// <summary>
		/// Returns read only files
		/// </summary>
		/// <param name="results"></param>
		/// <returns></returns>
		public static List<string> GetReadOnlyFiles(List<GrepSearchResult> results)
		{
			List<string> files = new List<string>();
			if (results == null || results.Count == 0)
				return files;

			foreach (GrepSearchResult result in results)
			{
				if (!files.Contains(result.FileNameReal))
				{
					if (IsReadOnly(result))
					{
						files.Add(result.FileNameReal);
					}
				}
			}
			return files;
		}

		public static bool IsReadOnly(GrepSearchResult result)
		{
			if ((File.GetAttributes(result.FileNameReal) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || result.ReadOnly)
				return true;
			else
				return false;			
		}

		/// <summary>
		/// Returns line and line number from a multiline string based on character index
		/// </summary>
		/// <param name="body">Multiline string</param>
		/// <param name="index">Index of any character in the line</param>
		/// <param name="lineNumber">Return parameter - 1-based line number or -1 if index is outside text length</param>
		/// <returns>Line of text or null if index is outside text length</returns>
		public static string GetLine(string body, int index, out int lineNumber)
		{
			if (body == null || index < 0 || index > body.Length)
			{
				lineNumber = -1;
				return null;
			}

			string subBody1 = body.Substring(0, index);
			string[] lines1 = subBody1.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			string subBody2 = body.Substring(index);
			string[] lines2 = subBody2.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			lineNumber = lines1.Length;
			return lines1[lines1.Length - 1] + lines2[0];
		}

		/// <summary>
		/// Returns lines and line numbers from a multiline string based on character index and length
		/// </summary>
		/// <param name="body">Multiline string</param>
		/// <param name="index">Index of any character in the line</param>
		/// <param name="length">Length of a line</param>
		/// <param name="lineNumbers">Return parameter - 1-based line numbers or null if index is outside text length</param>
		/// <returns>Line of text or null if index is outside text length</returns>
		public static List<string> GetLines(string body, int index, int length, out List<int> lineNumbers)
		{
			List<string> result = new List<string>();
			lineNumbers = new List<int>();
			if (body == null || index < 0 || index > body.Length || index + length > body.Length)
			{
				lineNumbers = null;
				return null;
			}

			string subBody1 = body.Substring(0, index);
			string[] lines1 = subBody1.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			string subBody2 = body.Substring(index, length);
			string[] lines2 = subBody2.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			string subBody3 = body.Substring(index + length);
			string[] lines3 = subBody3.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			for (int i = 0; i < lines2.Length; i++)
			{
				string line = "";
				lineNumbers.Add(lines1.Length + i);
				if (i == 0)
				{
					if (lines2.Length == 1 && lines3.Length > 0)
						line = lines1[lines1.Length - 1] + lines2[0] + lines3[0];
					else
						line = lines1[lines1.Length - 1] + lines2[0];
				}
				else if (i == lines2.Length - 1)
				{
					if (lines3.Length > 0)
						line = lines2[lines2.Length - 1] + lines3[0];
					else
						line = lines2[lines2.Length - 1];					
				}
				else
				{
					line = lines2[i];
				}
				result.Add(line);
			}

			return result;
		}

		/// <summary>
		/// Returns a list of context GrepLines by line numbers provided in the input parameter. Matched line is not returned.
		/// </summary>
		/// <param name="body"></param>
		/// <param name="linesBefore"></param>
		/// <param name="linesAfter"></param>
		/// <param name="foundLine">1 based line number</param>
		/// <returns></returns>
		public static List<GrepSearchResult.GrepLine> GetContextLines(string body, int linesBefore, int linesAfter, int foundLine)
		{
			List<GrepSearchResult.GrepLine> result = new List<GrepSearchResult.GrepLine>();
			if (body == null || body.Trim() == "")
				return result;

			List<int> lineNumbers = new List<int>();
			string[] lines = body.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			for (int i = foundLine - linesBefore - 1; i <= foundLine + linesAfter - 1; i++)
			{
				if (i >= 0 && i < lines.Length && (i + 1) != foundLine)
					result.Add(new GrepSearchResult.GrepLine(i + 1, lines[i], true));
			}
			return result;
		}

		/// <summary>
		/// Returns number of matches
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		public static int MatchCount(GrepSearchResult result)
		{
			int counter = 0;
			if (result != null && result.SearchResults != null)
			{
				foreach (GrepSearchResult.GrepLine line in result.SearchResults)
				{
					if (!line.IsContext)
						counter++;
				}
			}
			return counter;
		}

		/// <summary>
		/// Replaces unix-style linebreaks with \r\n
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string CleanLineBreaks(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;
			string textTemp = Regex.Replace(text, "(\r)([^\n])", "\r\n$2");
			textTemp = Regex.Replace(textTemp, "([^\r])(\n)", "$1\r\n");
			textTemp = Regex.Replace(textTemp, "(\v)", "\r\n");
			return textTemp;
		}

		/// <summary>
		/// Sorts and removes dupes
		/// </summary>
		/// <param name="results"></param>
		public static void CleanResults(ref List<GrepSearchResult.GrepLine> results)
		{
			if (results == null || results.Count == 0)
				return;

			results.Sort();
			for (int i = results.Count - 1; i >= 0; i -- )
			{
				for (int j = 0; j < results.Count; j ++ )
				{
					if (i < results.Count && 
						results[i].LineNumber == results[j].LineNumber && i != j)
					{
						if (results[i].IsContext || 
							(results[i].IsContext == results[j].IsContext &&
							results[i].IsContext == false && results[i].LineNumber != -1))
							results.RemoveAt(i);
					}
				}
			}
		}

		/// <summary>
		/// Returns MD5 hash for string
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string GetHash(string input)
		{
			// step 1, calculate MD5 hash from input
			System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);

			// step 2, convert byte array to hex string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("X2"));
			}
			return sb.ToString();
		}
	}

	public class KeyValueComparer : IComparer<KeyValuePair<string, int>>
	{
		public int Compare(KeyValuePair<string, int> x, KeyValuePair<string, int> y)
		{
			return x.Key.CompareTo(y.Key);
		}
	}
}
