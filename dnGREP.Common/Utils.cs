using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.Net;
using System.Xml;
using System.Security.Permissions;
using System.Security;
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
                if (result.SearchResults == null)
                {
                    sb.AppendLine("\"" + result.FileNameDisplayed + "\"");
                }
                else
                {
                    foreach (GrepSearchResult.GrepLine line in result.SearchResults)
                    {
                        if (!line.IsContext)
                            sb.AppendLine("\"" + result.FileNameDisplayed + "\"," + line.LineNumber + ",\"" + line.LineText.Replace("\"", "\"\"") + "\"");
                    }
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
			string[] files = GetFileList(path, "*.*", null, false, true, true, true, 0, 0);
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
			using (FileStream readStream = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
		/// Returns true is file is binary. Algorithm taken from winGrep.
		/// The function scans first 10KB for 0x0000 sequence
		/// and if found, assumes the file to be binary
		/// </summary>
		/// <param name="filePath">Path to a file</param>
		/// <returns>True is file is binary otherwise false</returns>
		public static bool IsBinary(string srcFile)
		{
			byte[] buffer = new byte[1024];
			int count = 0;
			using (FileStream readStream = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				count = readStream.Read(buffer, 0, buffer.Length);
			}
			for (int i = 0; i < count - 1; i = i + 2)
			{
				if (buffer[i] == 0 && buffer[i + 1] == 0)
				{
					return true;
				}
			}
			return false;
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

                string[] paths = SplitPath(path);
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
                string[] paths = SplitPath(path);
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
        /// Splits path into subpaths if ; or , are found in path.
        /// If folder name contains ; or , returns as one path
        /// </summary>
        /// <param name="path">Path to split</param>
        /// <returns>Array of strings. If path is null, returns null. If path is empty, returns empty array.</returns>
        public static string[] SplitPath(string path)
        {
            if (path == null)
                return null;
            else if (path.Trim() == "")
                return new string[0];
            
            List<string> output = new List<string>();
            string[] paths = path.Split(';',',');
            int splitterIndex = -1;
            for (int i = 0; i < paths.Length; i++)
            {
                splitterIndex += paths[i].Length + 1;
                string splitter = (splitterIndex < path.Length ? path[splitterIndex].ToString() : "");
                StringBuilder sb = new StringBuilder();
                if (File.Exists(paths[i]) || Directory.Exists(paths[i]))
                    output.Add(paths[i]);
                else
                {
                    int subSplitterIndex = 0;
                    bool found = false;
                    sb.Append(paths[i] + splitter);
                    for (int j = i + 1; j < paths.Length; j++)
                    {
                        subSplitterIndex += paths[j].Length + 1;
                        sb.Append(paths[j]);
                        if (File.Exists(sb.ToString()) || Directory.Exists(sb.ToString()))
                        {
                            output.Add(sb.ToString());
                            splitterIndex += subSplitterIndex;
                            i = j;
                            found = true;
                            break;
                        }
                        sb.Append(splitterIndex + subSplitterIndex < path.Length ? path[splitterIndex + subSplitterIndex].ToString() : "");
                    }
                    if (!found)
                        output.Add(paths[i]);
                }
            }
            return output.ToArray();
        }

		public static bool CancelSearch = false;

		/// <summary>
		/// Searches folder and it's subfolders for files that match pattern and
		/// returns array of strings that contain full paths to the files.
		/// If no files found returns 0 length array.
		/// </summary>
		/// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
		/// <param name="namePatternToInclude">File name pattern. (E.g. *.cs) or regex to include. If null returns empty array. If empty string returns all files.</param>
		/// <param name="namePatternToExclude">File name pattern. (E.g. *.cs) or regex to exclude. If null or empty is ignored.</param>
		/// <param name="isRegex">Whether to use regex as search pattern. Otherwise use asterisks</param>
		/// <param name="includeSubfolders">Include sub folders</param>
		/// <param name="includeHidden">Include hidden folders</param>
		/// <param name="includeBinary">Include binary files</param>
		/// <param name="sizeFrom">Size in KB</param>
		/// <param name="sizeTo">Size in KB</param>
		/// <returns></returns>
		public static string[] GetFileList(string path, string namePatternToInclude, string namePatternToExclude, bool isRegex, bool includeSubfolders, bool includeHidden, bool includeBinary, int sizeFrom, int sizeTo)
		{
			if (string.IsNullOrEmpty(path) || namePatternToInclude == null)
			{
				return new string[0];
			}
			else
			{
				List<string> fileMatch = new List<string>();
				if (namePatternToExclude == null)
					namePatternToExclude = "";

				if (!isRegex)
				{
                    string[] excludePaths = SplitPath(namePatternToExclude);
					StringBuilder sb = new StringBuilder();
					foreach (string exPath in excludePaths)
					{
						if (exPath.Trim() == "")
							continue;
						sb.Append(wildcardToRegex(exPath) + ";");
					}
					namePatternToExclude = sb.ToString();
				}

                string[] paths = SplitPath(path);
				foreach (string subPath in paths)
				{
					if (subPath.Trim() == "")
						continue;

					try
					{
						DirectoryInfo di = new DirectoryInfo(subPath);

						if (di.Exists)
						{
                            string[] namePatterns = SplitPath(namePatternToInclude);
							foreach (string pattern in namePatterns)
							{
								string rxPattern = pattern.Trim();
								if (!isRegex)
									rxPattern = wildcardToRegex(rxPattern);
								recursiveFileSearch(di.FullName, rxPattern, namePatternToExclude.Trim(), includeSubfolders, includeHidden, includeBinary, sizeFrom, sizeTo, fileMatch);
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

		private static void recursiveFileSearch(string pathToFolder, string namePatternToInclude, string namePatternToExclude, bool includeSubfolders, bool includeHidden, bool includeBinary, int sizeFrom, int sizeTo, List<string> files)
		{
			string[] fileMatch;
            string[] excludePattern = SplitPath(namePatternToExclude);

			if (CancelSearch)
				return;
			
			try
			{
				List<string> tempFileList = new List<string>();
				foreach (string fileInDirectory in Directory.GetFiles(pathToFolder))
				{
					if (Regex.IsMatch(Path.GetFileName(fileInDirectory), namePatternToInclude, RegexOptions.IgnoreCase))
					{
						bool isExcluded = false;
						foreach (string subPath in excludePattern)
						{
							if (subPath.Trim() == "")
								continue;

							if (Regex.IsMatch(Path.GetFileName(fileInDirectory), subPath, RegexOptions.IgnoreCase))
								isExcluded = true;
						}
						if (!isExcluded)
							tempFileList.Add(fileInDirectory);
					}
				}

				fileMatch = tempFileList.ToArray();
				
				for (int i = 0; i < fileMatch.Length; i++)
				{
					if (sizeFrom > 0 || sizeTo > 0)
					{
						long sizeKB = new FileInfo(fileMatch[i]).Length / 1000;
						if (sizeFrom > 0 && sizeKB < sizeFrom)
						{
							continue;
						}
						if (sizeTo > 0 && sizeKB > sizeTo)
						{
							continue;
						}
					}

					if (!includeBinary && IsBinary(fileMatch[i]))
						continue;

					if (!files.Contains(fileMatch[i]))
						files.Add(fileMatch[i]);
				}
				if (includeSubfolders)
				{
					foreach (string subDir in Directory.GetDirectories(pathToFolder))
					{
						DirectoryInfo dirInfo = new DirectoryInfo(subDir);
						if (((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) && !includeHidden)
						{
							continue;
						}
						else
						{
							recursiveFileSearch(subDir, namePatternToInclude, namePatternToExclude, includeSubfolders, includeHidden, includeBinary, sizeFrom, sizeTo, files);
							
							if (CancelSearch)
								return;
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
		/// Converts unix asterisk based file pattern to regex
		/// </summary>
		/// <param name="wildcard">Asterisk based pattern</param>
		/// <returns>Regeular expression of null is empty</returns>
		private static string wildcardToRegex(string wildcard)
		{
			if (wildcard == null || wildcard == "") return wildcard;

			StringBuilder sb = new StringBuilder();

			char[] chars = wildcard.ToCharArray();
			sb.Append("^");
			for (int i = 0; i < chars.Length; ++i)
			{
				if (chars[i] == '*')
					sb.Append(".*");
				else if (chars[i] == '?')
					sb.Append(".");
				else if ("+()^$.{}|\\".IndexOf(chars[i]) != -1)
					sb.Append('\\').Append(chars[i]); // prefix all metacharacters with backslash
				else
					sb.Append(chars[i]);
			}
			sb.Append("$");
			return sb.ToString().ToLowerInvariant();
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
		/// Parses text into double
		/// </summary>
		/// <param name="value">String. May include null, empty srting or text with spaces before or after.</param>
		/// <returns>Attempts to parse string. Otherwise returns double.MinValue</returns>
		public static double ParseDouble(string value)
		{
			return ParseDouble(value, double.MinValue);
		}

		/// <summary>
		/// Parses text into double
		/// </summary>
		/// <param name="value">String. May include null, empty srting or text with spaces before or after.</param>
		/// <param name="defaultValue">Default value if fails to parse.</param>
		/// <returns>Attempts to parse string. Otherwise returns defaultValue</returns>
		public static double ParseDouble(string value, double defaultValue)
		{
			if (value != null && value.Length != 0)
			{
				double output;
				value = value.Trim();
				if (double.TryParse(value, out output))
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
            {
                try
                {
                    System.Diagnostics.Process.Start(@"" + args.SearchResult.FileNameDisplayed + "");
                }
                catch (Exception ex)
                {
                    ProcessStartInfo info = new ProcessStartInfo("notepad.exe");
                    info.UseShellExecute = false;
                    info.CreateNoWindow = true;
                    info.Arguments = args.SearchResult.FileNameDisplayed;
                    System.Diagnostics.Process.Start(info);
                }
            }
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
			string tempPath = FixFolderName(GetDataFolderPath()) + "~dnGREP-Temp\\";
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
			string tempPath = FixFolderName(GetDataFolderPath()) + "~dnGREP-Temp\\";
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

		private static bool? canUseCurrentFolder = null;
		/// <summary>
		/// Returns path to folder where user has write access to. Either current folder or user APP_DATA.
		/// </summary>
		/// <returns></returns>
		public static string GetDataFolderPath()
		{
			string currentFolder = GetCurrentPath(typeof(Utils));
			if (canUseCurrentFolder == null)
			{
				canUseCurrentFolder = hasWriteAccessToFolder(currentFolder);
			}
			
			if (canUseCurrentFolder == true)
			{
				return currentFolder;
			}
			else
			{
				string dataFolder = Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "\\dnGREP";
				if (!Directory.Exists(dataFolder))
					Directory.CreateDirectory(dataFolder);
				return dataFolder;
			}
		}

		private static bool hasWriteAccessToFolder(string folderPath)
		{
			string filename = FixFolderName(folderPath) + "~temp.dat";
			bool canAccess = true;
			//1. Provide early notification that the user does not have permission to write.
			FileIOPermission writePermission = new FileIOPermission(FileIOPermissionAccess.Write, filename);
			if (!SecurityManager.IsGranted(writePermission))
			{
				//No permission. 
				canAccess = false;
			}

			
			//2. Attempt the action but handle permission changes.
			if (canAccess)
			{
				try
				{
					using (FileStream fstream = new FileStream(filename, FileMode.Create))
					using (TextWriter writer = new StreamWriter(fstream))
					{
						writer.WriteLine("sometext");
					}
				}
				catch (UnauthorizedAccessException ex)
				{
					//No permission. 
					canAccess = false;
				}
			}

			// Cleanup
			try
			{
				DeleteFile(filename);
			}
			catch (Exception ex)
			{
				// Ignore
			}

			return canAccess;
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
			if (File.Exists(result.FileNameReal) && (File.GetAttributes(result.FileNameReal) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly || result.ReadOnly)
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
			string[] lines1 = subBody1.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
			string subBody2 = body.Substring(index);
			string[] lines2 = subBody2.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
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
		public static List<string> GetLines(string body, int index, int length, out List<GrepSearchResult.GrepMatch> matches, out List<int> lineNumbers)
		{
			List<string> result = new List<string>();
			lineNumbers = new List<int>();
            matches = new List<GrepSearchResult.GrepMatch>();
			if (body == null || index < 0 || index > body.Length || index + length > body.Length)
			{
				lineNumbers = null;
                matches = null;
				return null;
			}

			string subBody1 = body.Substring(0, index);
			string[] lines1 = subBody1.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
			string subBody2 = body.Substring(index, length);
			string[] lines2 = subBody2.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
			string subBody3 = body.Substring(index + length);
			string[] lines3 = subBody3.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
			for (int i = 0; i < lines2.Length; i++)
			{
				string line = "";
				lineNumbers.Add(lines1.Length + i);
				if (i == 0)
				{
                    if (lines2.Length == 1 && lines3.Length > 0)
                    {
                        line = lines1[lines1.Length - 1] + lines2[0] + lines3[0];
                    }
                    else
                    {
                        line = lines1[lines1.Length - 1] + lines2[0];
                    }

                    matches.Add(new GrepSearchResult.GrepMatch(lines1.Length + i, index - subBody1.Length + lines1[lines1.Length - 1].Length, lines2[0].Length));
				}
				else if (i == lines2.Length - 1)
				{
                    if (lines3.Length > 0)
                    {
                        line = lines2[lines2.Length - 1] + lines3[0];
                    }
                    else
                    {
                        line = lines2[lines2.Length - 1];
                    }

                    matches.Add(new GrepSearchResult.GrepMatch(lines1.Length + i, 0, lines2[lines2.Length - 1].Length));
				}
				else
				{
					line = lines2[i];
                    matches.Add(new GrepSearchResult.GrepMatch(lines1.Length + i, 0, lines2[i].Length));
				}
				result.Add(line);
			}

			return result;
		}

        public static List<GrepSearchResult.GrepLine> GetLinesEx(TextReader body, List<GrepSearchResult.GrepMatch> bodyMatches)
        {
            List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
            List<string> lineStrings = new List<string>();
            List<int> lineNumbers = new List<int>();
            List<GrepSearchResult.GrepMatch> matches = new List<GrepSearchResult.GrepMatch>();
            if (body == null || bodyMatches == null)
            {
                return new List<GrepSearchResult.GrepLine>();
            }

            // Current line
            int lineNumber = 0;
            // Current index of character
            int currentIndex = 0;
            int startIndex = 0;
            int tempLinesTotalLength = 0;
            int startLine = 0;
            bool startMatched = false;
            Queue<string> lineQueue = new Queue<string>();
            
            while (body.Peek() >= 0 && bodyMatches.Count > 0)
            {
                lineNumber++;
                string line = body.ReadLine();
                bool moreMatches = true;

                while (moreMatches)
                {
                    // Head of match found
                    if (bodyMatches[0].StartLocation >= currentIndex && bodyMatches[0].StartLocation <= currentIndex + line.Length && !startMatched)
                    {
                        startMatched = true;
                        moreMatches = true;
                        lineQueue = new Queue<string>();
                        startLine = lineNumber;
                        startIndex = bodyMatches[0].StartLocation - currentIndex;
                        tempLinesTotalLength = 0;
                    }

                    // Add line to queue
                    if (startMatched)
                    {
                        lineQueue.Enqueue(line);
                        tempLinesTotalLength += line.Length;
                    }

                    // Tail of match found
                    if (bodyMatches[0].StartLocation + bodyMatches[0].Length <= currentIndex + line.Length && startMatched)
                    {
                        startMatched = false;
                        moreMatches = false;
                        // Start creating matches
                        for (int i = startLine; i <= lineNumber; i++)
                        {
                            lineNumbers.Add(i);
                            string tempLine = lineQueue.Dequeue();
                            lineStrings.Add(tempLine);
                            // First and only line
                            if (i == startLine && i == lineNumber)
                                matches.Add(new GrepSearchResult.GrepMatch(i, startIndex, bodyMatches[0].Length));
                            // First but not last line
                            else if (i == startLine)
                                matches.Add(new GrepSearchResult.GrepMatch(i, startIndex, tempLine.Length - startIndex));
                            // Middle line
                            else if (i > startLine && i < lineNumber)
                                matches.Add(new GrepSearchResult.GrepMatch(i, 0, tempLine.Length));
                            // Last line
                            else
                                matches.Add(new GrepSearchResult.GrepMatch(i, 0, bodyMatches[0].Length - tempLinesTotalLength + line.Length));
                        }
                        bodyMatches.RemoveAt(0);
                    }

                    // Another match on this line
                    if (bodyMatches.Count > 0 && bodyMatches[0].StartLocation >= currentIndex && bodyMatches[0].StartLocation <= currentIndex + line.Length && !startMatched)
                        moreMatches = true;
                    else
                        moreMatches = false;
                }

                currentIndex += line.Length;                
            }

            if (lineStrings.Count == 0)
            {
                return new List<GrepSearchResult.GrepLine>();
            }

            int lastLineNumber = -1;
            for (int i = 0; i < lineNumbers.Count; i++)
            {
                List<GrepSearchResult.GrepMatch> lineMatches = new List<GrepSearchResult.GrepMatch>();
                foreach (GrepSearchResult.GrepMatch m in matches) if (m.LineNumber == lineNumbers[i]) lineMatches.Add(m);

                if (lastLineNumber != lineNumbers[i])
                {
                    results.Add(new GrepSearchResult.GrepLine(lineNumbers[i], lineStrings[i], false, lineMatches));
                    lastLineNumber = lineNumbers[i];
                }
                //if (showLinesInContext && includeContext)
                //{
                //    // Fix this one
                //    results.AddRange(Utils.GetContextLines(text, linesBefore,
                //        linesAfter, lineNumbers[i]));
                //}
            }
            return results;
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
			string[] lines = body.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
			for (int i = foundLine - linesBefore - 1; i <= foundLine + linesAfter - 1; i++)
			{
				if (i >= 0 && i < lines.Length && (i + 1) != foundLine)
					result.Add(new GrepSearchResult.GrepLine(i + 1, lines[i], true, null));
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
				for (int i = 0; i < result.SearchResults.Count; i++)
				{
					GrepSearchResult.GrepLine line = null;
					if (result.SearchResults.Count >= i)
						line = result.SearchResults[i];

					if (!line.IsContext)
					{
						if (line.Matches == null || line.Matches.Count == 0)
						{
							counter++;
						}
						else
						{
							counter += line.Matches.Count;
						}
					}
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
						if (results[i].IsContext)
							results.RemoveAt(i);
						else if (results[i].IsContext == results[j].IsContext && results[i].IsContext == false && results[i].LineNumber != -1)
						{
							results[j].Matches.AddRange(results[i].Matches);
							results.RemoveAt(i);
						}
					}
				}
			}

			for (int j = 0; j < results.Count; j++)
			{
				results[j].Matches.Sort();
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

		/// <summary>
		/// Returns true if beginText end with a non-alphanumeric character. Copied from AtroGrep.
		/// </summary>
		/// <param name="beginText">Text to test</param>
		/// <returns></returns>
		public static bool IsValidBeginText(string beginText)
		{
			if (beginText.Equals(string.Empty) ||
			   beginText.EndsWith(" ") ||
			   beginText.EndsWith("<") ||
               beginText.EndsWith(">") ||
			   beginText.EndsWith("$") ||
			   beginText.EndsWith("+") ||
			   beginText.EndsWith("*") ||
			   beginText.EndsWith("[") ||
			   beginText.EndsWith("{") ||
			   beginText.EndsWith("(") ||
			   beginText.EndsWith(".") ||
			   beginText.EndsWith("?") ||
			   beginText.EndsWith("!") ||
			   beginText.EndsWith(",") ||
			   beginText.EndsWith(":") ||
			   beginText.EndsWith(";") ||
			   beginText.EndsWith("-") ||
			   beginText.EndsWith("\\") ||
			   beginText.EndsWith("/") ||
			   beginText.EndsWith("'") ||
			   beginText.EndsWith("\"") ||
			   beginText.EndsWith(Environment.NewLine) ||
			   beginText.EndsWith("\r\n") ||
			   beginText.EndsWith("\r") ||
			   beginText.EndsWith("\n") ||
			   beginText.EndsWith("\t")
			   )
			{
				return true;
			}

			return false;
		}

        public static string ReplaceSpecialCharacters(string input)
        {
            string result = input.Replace("\\t", "\t")
                                 .Replace("\\n", "\n")
                                 .Replace("\\0", "\0")
                                 .Replace("\\b", "\b")
                                 .Replace("\\r", "\r");
            return result;
        }

		/// <summary>
		/// Returns true if endText starts with a non-alphanumeric character. Copied from AtroGrep.
		/// </summary>
		/// <param name="endText"></param>
		/// <returns></returns>
		public static bool IsValidEndText(string endText)
		{
			if (endText.Equals(string.Empty) ||
			   endText.StartsWith(" ") ||
			   endText.StartsWith("<") ||
			   endText.StartsWith("$") ||
			   endText.StartsWith("+") ||
			   endText.StartsWith("*") ||
			   endText.StartsWith("[") ||
			   endText.StartsWith("{") ||
			   endText.StartsWith("(") ||
			   endText.StartsWith(".") ||
			   endText.StartsWith("?") ||
			   endText.StartsWith("!") ||
			   endText.StartsWith(",") ||
			   endText.StartsWith(":") ||
			   endText.StartsWith(";") ||
			   endText.StartsWith("-") ||
			   endText.StartsWith(">") ||
			   endText.StartsWith("]") ||
			   endText.StartsWith("}") ||
			   endText.StartsWith(")") ||
			   endText.StartsWith("\\") ||
			   endText.StartsWith("/") ||
			   endText.StartsWith("'") ||
			   endText.StartsWith("\"") ||
			   endText.StartsWith(Environment.NewLine) ||
			   endText.StartsWith("\r\n") ||
			   endText.StartsWith("\r") ||
			   endText.StartsWith("\n") ||
               endText.StartsWith("\t")
			   )
			{
				return true;
			}

			return false;
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
