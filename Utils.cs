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

namespace dnGREP
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

			if (destinationDirectory[destinationDirectory.Length - 1] != Path.DirectorySeparatorChar)
				destinationDirectory += Path.DirectorySeparatorChar;

			if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

			files = Directory.GetFileSystemEntries(sourceDirectory);

			foreach (string element in files)
			{
				if (!string.IsNullOrEmpty(includePattern) && !Regex.IsMatch(element, includePattern))
					continue;

				if (!string.IsNullOrEmpty(excludePattern) && Regex.IsMatch(element, excludePattern))
					continue;

				// Sub directories
				if (Directory.Exists(element))
					CopyFiles(element, destinationDirectory + Path.GetFileName(element), includePattern, excludePattern);
				// Files in directory
				else
					File.Copy(element, destinationDirectory + Path.GetFileName(element), true);
			}
		}

		/// <summary>
		/// Copies file based on search results. If folder does not exist, creates it.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destinationDirectory"></param>
		/// <param name="overWrite"></param>
		public static void CopyFiles(List<GrepSearchResult> source, string destinationDirectory, bool overWrite)
		{
			if (destinationDirectory[destinationDirectory.Length - 1] != Path.DirectorySeparatorChar)
				destinationDirectory += Path.DirectorySeparatorChar;

			if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

			List<string> files = new List<string>();

			foreach (GrepSearchResult result in source)
			{
				if (!files.Contains(result.FileName))
				{
					files.Add(result.FileName);
					FileInfo sourceFileInfo = new FileInfo(result.FileName);
					FileInfo destinationFileInfo = new FileInfo(destinationDirectory + Path.GetFileName(result.FileName));
					if (sourceFileInfo.FullName != destinationFileInfo.FullName)
						sourceFileInfo.CopyTo(destinationFileInfo.FullName, true);
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
			if (destinationDirectory[destinationDirectory.Length - 1] != Path.DirectorySeparatorChar)
				destinationDirectory += Path.DirectorySeparatorChar;

			if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

			List<string> files = new List<string>();

			foreach (GrepSearchResult result in source)
			{
				if (!files.Contains(result.FileName))
				{
					files.Add(result.FileName);
					FileInfo sourceFileInfo = new FileInfo(result.FileName);
					FileInfo destinationFileInfo = new FileInfo(destinationDirectory + Path.GetFileName(result.FileName));
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
					sb.AppendLine("\"" + result.FileName + "\"," + line.LineNumber + ",\"" + line.LineText + "\"");
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
				if (!files.Contains(result.FileName))
				{
					files.Add(result.FileName);
					File.Delete(result.FileName);
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
			File.SetAttributes(path, FileAttributes.Normal);
			File.Delete(path);
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


		public static string FixFolderName(string name)
		{
			if (name != null && name.Length > 1 && name[name.Length - 1] != Path.DirectorySeparatorChar)
				name += Path.DirectorySeparatorChar;
			return name;
		}

		/// <summary>
		/// Searches folder and it's subfolders for files that match pattern and
		/// returns array of strings that contain full paths to the files.
		/// If no files found returns 0 length array.
		/// </summary>
		/// <param name="pathToFolder"></param>
		/// <param name="namePattern">File name pattern. (E.g. *.cs) or regex</param>
		/// <param name="isRegex">Whether to use regex as search pattern. Otherwise use asterisks</param>
		/// <param name="includeSubfolders"></param>
		/// <param name="includeHidden"></param>
		/// <param name="sizeFrom"></param>
		/// <param name="sizeTo"></param>
		/// <returns></returns>
		public static string[] GetFileList(string pathToFolder, string namePattern, bool isRegex, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo)
		{
			if (!Directory.Exists(pathToFolder))
				return new string[0];

			DirectoryInfo di = new DirectoryInfo(pathToFolder);
			List<string> fileMatch = new List<string>();
			string[] namePatterns = namePattern.Split(';');
			foreach (string pattern in namePatterns)
			{
				recursiveFileSearch(pathToFolder, pattern.Trim(), isRegex, includeSubfolders, includeHidden, sizeFrom, sizeTo, fileMatch);
			}
			
			return fileMatch.ToArray();
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

		public static int ParseInt(string value)
		{
			return ParseInt(value, int.MinValue);
		}

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

		public static void OpenFile(string fileName, int line)
		{
			if (!Properties.Settings.Default.UseCustomEditor)
				System.Diagnostics.Process.Start(@"" + fileName + "");
			else
			{
				ProcessStartInfo info = new ProcessStartInfo(Properties.Settings.Default.CustomEditor);
				info.UseShellExecute = false;
				info.CreateNoWindow = true;
				info.Arguments = Properties.Settings.Default.CustomEditorArgs.Replace("%file", "\"" + fileName + "\"").Replace("%line", line.ToString());
				System.Diagnostics.Process.Start(info);
			}
		}

		public static void OpenContainingFolder(string fileName, int line)
		{
			System.Diagnostics.Process.Start(@"" + Path.GetDirectoryName(fileName) + "");
		}

		
		public static string GetCurrentPath()
		{
			Assembly thisAssembly = Assembly.GetAssembly(typeof(Utils));
			return Path.GetDirectoryName(thisAssembly.Location);
		}

		public static string[] GetReadOnlyFiles(List<GrepSearchResult> results)
		{
			List<string> files = new List<string>();
			foreach (GrepSearchResult result in results)
			{
				if (!files.Contains(result.FileName))
				{
					if ((File.GetAttributes(result.FileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					{
						files.Add(result.FileName);
					}
				}
			}
			return files.ToArray();
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
		/// Returns a list of GrepLines by line numbers provided in the input parameter
		/// </summary>
		/// <param name="body"></param>
		/// <param name="linesBefore"></param>
		/// <param name="linesAfter"></param>
		/// <param name="foundLine"></param>
		/// <returns></returns>
		public static List<GrepSearchResult.GrepLine> GetContextLines(string body, int linesBefore, int linesAfter, int foundLine)
		{
			List<GrepSearchResult.GrepLine> result = new List<GrepSearchResult.GrepLine>();
			List<int> lineNumbers = new List<int>();
			string[] lines = body.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			for (int i = foundLine - linesBefore - 1; i <= foundLine + linesAfter - 1; i++)
			{
				if (i >= 0 && i < lines.Length - 1 && (i + 1) != foundLine)
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
			foreach (GrepSearchResult.GrepLine line in result.SearchResults)
			{
				if (!line.IsContext)
					counter++;
			}
			return counter;
		}

		/// <summary>
		/// Sorts and removes dupes
		/// </summary>
		/// <param name="results"></param>
		public static void CleanResults(ref List<GrepSearchResult.GrepLine> results)
		{
			results.Sort();
			for (int i = results.Count - 1; i >= 0; i -- )
			{
				for (int j = 0; j < results.Count; j ++ )
				{
					if (i < results.Count && 
						results[i].LineNumber == results[j].LineNumber && i != j)
					{
						if (results[i].IsContext || 
							(results[i].IsContext == results[j].IsContext && results[i].IsContext == false))
							results.RemoveAt(i);
					}
				}
			}
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
