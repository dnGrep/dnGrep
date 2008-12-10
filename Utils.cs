using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace nGREP
{
	public class Utils
	{
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
			string[] files = GetFileList(path, "*.*", true, true, 0, 0);
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
			FileStream file = new FileStream(srcFile, FileMode.Open);
			file.Read(buffer, 0, 5);
			file.Close();

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
		/// <param name="namePattern">File name pattern. (E.g. *.cs)</param>
		/// <param name="includeSubfolders"></param>
		/// <param name="includeHidden"></param>
		/// <param name="sizeFrom"></param>
		/// <param name="sizeTo"></param>
		/// <returns></returns>
		public static string[] GetFileList(string pathToFolder, string namePattern, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo)
		{
			if (!Directory.Exists(pathToFolder))
				return new string[0];

			DirectoryInfo di = new DirectoryInfo(pathToFolder);
			List<string> fileMatch = new List<string>();
			string[] namePatterns = namePattern.Split(';');
			foreach (string pattern in namePatterns)
			{
				recursiveFileSearch(pathToFolder, pattern.Trim(), includeSubfolders, includeHidden, sizeFrom, sizeTo, fileMatch);
			}
			
			return fileMatch.ToArray();
		}

		private static void recursiveFileSearch(string pathToFolder, string namePattern, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo, List<string> files)
		{
			DirectoryInfo di = new DirectoryInfo(pathToFolder);
			FileInfo[] fileMatch = di.GetFiles(namePattern, SearchOption.TopDirectoryOnly);
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
						recursiveFileSearch(subDir.FullName, namePattern, includeSubfolders, includeHidden, sizeFrom, sizeTo, files);
					}
				}
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
				ProcessStartInfo info = new ProcessStartInfo("cmd.exe");
				info.UseShellExecute = false;
				info.CreateNoWindow = true;
				info.Arguments = "/C " + Properties.Settings.Default.CustomEditor.Replace("%file", "\"" + fileName + "\"").Replace("%line", line.ToString());
				System.Diagnostics.Process.Start(info);
			}
		}
	}
}
