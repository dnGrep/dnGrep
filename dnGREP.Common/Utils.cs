using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using dnGREP.Common.UI;
using dnGREP.Everything;
using dnGREP.Localization;
using NLog;
using UtfUnknown;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Common
{
    public static class Utils
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly char[] chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

        private static readonly string tempFolderName;
        private static readonly string undoFolderName;

        private static readonly object regexLock = new object();
        private static readonly Dictionary<string, Regex> regexCache = new Dictionary<string, Regex>();

        static Utils()
        {
            tempFolderName = "dnGrep-temp-" + GetUniqueKey(12);
            undoFolderName = "dnGrep-undo-" + GetUniqueKey(12);
        }

        /// <summary>
        /// Copies the folder recursively. Uses includePattern to avoid unnecessary objects
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="includePattern">Regex pattern that matches file or folder to be included. If null or empty, the parameter is ignored</param>
        /// <param name="excludePattern">Regex pattern that matches file or folder to be included. If null or empty, the parameter is ignored</param>
        public static void CopyFiles(string sourceDirectory, string destinationDirectory, string includePattern, string excludePattern)
        {
            if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

            var files = Directory.GetFileSystemEntries(sourceDirectory);

            foreach (string element in files)
            {
                if (!string.IsNullOrEmpty(includePattern) && File.Exists(element) && !Regex.IsMatch(element, includePattern))
                    continue;

                if (!string.IsNullOrEmpty(excludePattern) && File.Exists(element) && Regex.IsMatch(element, excludePattern))
                    continue;

                // Sub directories
                if (Directory.Exists(element))
                    CopyFiles(element, Path.Combine(destinationDirectory, Path.GetFileName(element)), includePattern, excludePattern);
                // Files in directory
                else
                    CopyFile(element, Path.Combine(destinationDirectory, Path.GetFileName(element)), true);
            }
        }

        /// <summary>
        /// Copies files with directory structure based on search results. If destination folder does not exist, creates it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="action"></param>
        /// <returns>number of files copied</returns>
        public static int CopyFiles(List<GrepSearchResult> source, string sourceDirectory, string destinationDirectory, OverwriteFile action)
        {
            return CopyMoveFilesImpl(source, sourceDirectory, destinationDirectory, action, false);
        }

        /// <summary>
        /// Moves files with directory structure based on search results. If destination folder does not exist, creates it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="action"></param>
        /// <returns>number of files copied</returns>
        public static int MoveFiles(List<GrepSearchResult> source, string sourceDirectory, string destinationDirectory, OverwriteFile action)
        {
            return CopyMoveFilesImpl(source, sourceDirectory, destinationDirectory, action, true);
        }

        private static int CopyMoveFilesImpl(List<GrepSearchResult> source, string sourceDirectory, string destinationDirectory, OverwriteFile action, bool deleteAfterCopy)
        {
            sourceDirectory = FixFolderName(sourceDirectory);
            destinationDirectory = FixFolderName(destinationDirectory);

            if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

            int count = 0;
            HashSet<string> files = new HashSet<string>();

            foreach (GrepSearchResult result in source)
            {
                if (!files.Contains(result.FileNameReal) && result.FileNameReal.Contains(sourceDirectory))
                {
                    files.Add(result.FileNameReal);
                    FileInfo sourceFileInfo = new FileInfo(result.FileNameReal);
                    FileInfo destinationFileInfo = new FileInfo(destinationDirectory + result.FileNameReal.Substring(sourceDirectory.Length));
                    if (sourceFileInfo.FullName != destinationFileInfo.FullName)
                    {
                        bool overwrite = action == OverwriteFile.Yes;
                        if (destinationFileInfo.Exists && action == OverwriteFile.Prompt)
                        {
                            var answer = MessageBox.Show(
                                TranslationSource.Format(Resources.MessageBox_TheFile0AlreadyExistsIn1OverwriteExisting,
                                    destinationFileInfo.Name, destinationFileInfo.DirectoryName),
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.YesNoCancel, MessageBoxImage.Question,
                                MessageBoxResult.No, TranslationSource.Instance.FlowDirection);

                            if (answer == MessageBoxResult.Cancel)
                                return count;
                            if (answer == MessageBoxResult.No)
                                continue;

                            overwrite = true;
                        }

                        CopyFile(sourceFileInfo.FullName, destinationFileInfo.FullName, overwrite);
                        if (deleteAfterCopy)
                            DeleteFile(sourceFileInfo.FullName);
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Copies source files to destination folder without source directory structure.
        /// If destination folder does not exist, creates it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="action"></param>
        /// <returns>number of files copied</returns>
        public static int CopyFiles(List<GrepSearchResult> source, string destinationDirectory, OverwriteFile action)
        {
            return CopyMoveImpl(source, destinationDirectory, action, false);
        }

        /// <summary>
        /// Moves source files to destination folder without source directory structure.
        /// If destination folder does not exist, creates it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destinationDirectory"></param>
        /// <param name="action"></param>
        /// <returns>number of files moved</returns>
        public static int MoveFiles(List<GrepSearchResult> source, string destinationDirectory, OverwriteFile action)
        {
            return CopyMoveImpl(source, destinationDirectory, action, true);
        }

        private static int CopyMoveImpl(List<GrepSearchResult> source, string destinationDirectory, OverwriteFile action, bool deleteAfterCopy)
        {
            if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

            int count = 0;
            HashSet<string> files = new HashSet<string>();

            foreach (GrepSearchResult result in source)
            {
                if (!files.Contains(result.FileNameReal))
                {
                    files.Add(result.FileNameReal);
                    FileInfo sourceFileInfo = new FileInfo(result.FileNameReal);
                    FileInfo destinationFileInfo = new FileInfo(Path.Combine(destinationDirectory, Path.GetFileName(result.FileNameReal)));
                    if (sourceFileInfo.FullName != destinationFileInfo.FullName)
                    {
                        bool overwrite = action == OverwriteFile.Yes;
                        if (destinationFileInfo.Exists && action == OverwriteFile.Prompt)
                        {
                            var answer = MessageBox.Show(
                                TranslationSource.Format(Resources.MessageBox_TheFile0AlreadyExistsIn1OverwriteExisting,
                                    destinationFileInfo.Name, destinationFileInfo.DirectoryName),
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.YesNoCancel, MessageBoxImage.Question,
                                MessageBoxResult.No, TranslationSource.Instance.FlowDirection);

                            if (answer == MessageBoxResult.Cancel)
                                return count;
                            if (answer == MessageBoxResult.No)
                                continue;

                            overwrite = true;
                        }

                        CopyFile(sourceFileInfo.FullName, destinationFileInfo.FullName, overwrite);
                        if (deleteAfterCopy)
                            DeleteFile(sourceFileInfo.FullName);
                        count++;
                    }
                }
            }
            return count;
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

            HashSet<string> files = new HashSet<string>();

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
        /// Deletes file based on search results. 
        /// </summary>
        /// <param name="source"></param>
        public static int DeleteFiles(List<GrepSearchResult> source)
        {
            HashSet<string> files = new HashSet<string>();
            int count = 0;
            foreach (GrepSearchResult result in source)
            {
                if (!files.Contains(result.FileNameReal))
                {
                    files.Add(result.FileNameReal);
                    DeleteFile(result.FileNameReal);
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Deletes files to the recycle bin based on search results. 
        /// </summary>
        /// <param name="source"></param>
        public static int SendToRecycleBin(List<GrepSearchResult> source)
        {
            HashSet<string> files = new HashSet<string>();
            int count = 0;
            foreach (GrepSearchResult result in source)
            {
                if (!files.Contains(result.FileNameReal))
                {
                    files.Add(result.FileNameReal);

                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(result.FileNameReal,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    count++;
                }
            }
            return count;
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
                throw new IOException($"File: '{destinationPath}' exists.");

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
            if (File.Exists(path))
            {
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
            string[] files = GetFileList(path, "*.*", null, false, false, true, true, true, false, false, 0, 0, FileDateFilter.None, null, null, false, -1, true);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            Directory.Delete(path, true, true);
        }

        /// <summary>
        /// Detects the byte order mark of a file and returns
        /// an appropriate encoding for the file.
        /// </summary>
        /// <param name="srcFile"></param>
        /// <returns></returns>
        public static Encoding GetFileEncoding(string srcFile)
        {
            using (FileStream readStream = File.Open(srcFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var results = CharsetDetector.DetectFromStream(readStream);
                // Get the best Detection
                DetectionDetail resultDetected = results.Detected;
                // Get the System.Text.Encoding of the found encoding (can be null if not available)
                Encoding encoding = resultDetected?.Encoding ?? Encoding.Default;
                return encoding;
            }
        }

        /// <summary>
        /// Detects the byte order mark of a file and returns an appropriate encoding for the file.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Encoding GetFileEncoding(Stream stream)
        {
            var results = CharsetDetector.DetectFromStream(stream);
            // Get the best Detection
            DetectionDetail resultDetected = results.Detected;
            // Get the System.Text.Encoding of the found encoding (can be null if not available)
            Encoding encoding = resultDetected?.Encoding ?? Encoding.Default;

            // reset the stream back to the beginning
            stream.Seek(0, SeekOrigin.Begin);
            return encoding;
        }

        /// <summary>
        /// Returns true is file is binary.
        /// </summary>
        /// <param name="filePath">Path to a file</param>
        /// <returns>True is file is binary otherwise false</returns>
        public static bool IsBinary(string srcFile)
        {
            try
            {
                if (File.Exists(srcFile))
                {
                    using (FileStream readStream = File.Open(srcFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        return IsBinary(readStream);
                    }
                }
            }
            catch
            {
                // ignore - file cannot be opened
            }
            return false;
        }

        public static bool IsBinary(Stream stream)
        {
            bool result = false;
            try
            {
                byte[] buffer = new byte[1024];
                int count = stream.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < count - 3; i++)
                {
                    // check for 4 consecutive nulls - 2 will give false positive on UTF-32
                    if (buffer[i] == 0 && buffer[i + 1] == 0 && buffer[i + 2] == 0 && buffer[i + 3] == 0)
                    {
                        result = true;
                    }
                }
            }
            catch
            {
                result = false;
            }
            finally
            {
                // reset the stream back to the beginning
                stream.Seek(0, SeekOrigin.Begin);
            }
            return result;
        }

        public static bool IsRTL(string srcFile, Encoding encoding)
        {
            if (File.Exists(srcFile))
            {
                using (FileStream readStream = File.Open(srcFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return IsRTL(readStream, encoding);
                }
            }
            return false;
        }

        public static bool IsRTL(Stream stream, Encoding encoding)
        {
            using (StreamReader streamReader = new StreamReader(stream, encoding))
            {
                string line = streamReader.ReadLine();
                return IsRTL(line);
            }
        }

        public static bool IsRTL(string text)
        {
            bool isRtl = false;
            if (!string.IsNullOrWhiteSpace(text))
            {
                Regex regex = new Regex(@"\p{IsArabic}|\p{IsHebrew}");
                isRtl = regex.IsMatch(text);
            }
            return isRtl;
        }

        /// <summary>
        /// Returns true if the source file extension is ".pdf"
        /// </summary>
        /// <param name="srcFile"></param>
        /// <returns></returns>
        public static bool IsPdfFile(string srcFile)
        {
            string ext = Path.GetExtension(srcFile);
            if (!string.IsNullOrWhiteSpace(ext) && ext.Equals(".PDF", StringComparison.CurrentCultureIgnoreCase))
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if the source file extension is ".doc" or ".docx" or ".docm"
        /// </summary>
        /// <param name="srcFile"></param>
        /// <returns></returns>
        public static bool IsWordFile(string srcFile)
        {
            string ext = Path.GetExtension(srcFile);
            if (!string.IsNullOrWhiteSpace(ext) &&
                (ext.Equals(".DOC", StringComparison.CurrentCultureIgnoreCase) ||
                 ext.Equals(".DOCX", StringComparison.CurrentCultureIgnoreCase) ||
                 ext.Equals(".DOCM", StringComparison.CurrentCultureIgnoreCase)))
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if the source file extension is ".xls" or ".xlsx" or ".xlsm"
        /// At this time we can't parse ".xlsb" files, so do not include them here 
        /// </summary>
        /// <param name="srcFile"></param>
        /// <returns></returns>
        public static bool IsExcelFile(string srcFile)
        {
            string ext = Path.GetExtension(srcFile);
            if (!string.IsNullOrWhiteSpace(ext) &&
                (ext.Equals(".XLS", StringComparison.CurrentCultureIgnoreCase) ||
                 ext.Equals(".XLSX", StringComparison.CurrentCultureIgnoreCase) ||
                 ext.Equals(".XLSM", StringComparison.CurrentCultureIgnoreCase)))
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if the source file extension is ".pptx" or ".pptm"
        /// </summary>
        /// <param name="srcFile"></param>
        /// <returns></returns>
        public static bool IsPowerPointFile(string srcFile)
        {
            string ext = Path.GetExtension(srcFile);
            if (!string.IsNullOrWhiteSpace(ext) &&
                (ext.Equals(".PPTX", StringComparison.CurrentCultureIgnoreCase) ||
                 ext.Equals(".PPTM", StringComparison.CurrentCultureIgnoreCase)))
                return true;
            return false;
        }

        /// <summary>
        /// Returns true if the source file extension is a recognized archive file
        /// </summary>
        /// <param name="srcFile">a file name</param>
        /// <returns></returns>
        public static bool IsArchive(string srcFile)
        {
            if (!string.IsNullOrWhiteSpace(srcFile))
            {
                return IsArchiveExtension(Path.GetExtension(srcFile));
            }
            return false;
        }

        public static bool IsFileInArchive(string srcFile)
        {
            return srcFile.Contains(ArchiveDirectory.ArchiveSeparator);
        }

        /// <summary>
        /// Returns true if the parameter is a recognized archive file format file extension.
        /// </summary>
        /// <param name="ext">a file extension, with/without a leading '.'</param>
        /// <returns></returns>
        public static bool IsArchiveExtension(string ext)
        {
            if (!string.IsNullOrWhiteSpace(ext))
            {
                // regex extensions may have a 'match end of line' char: remove it
                ext = ext.TrimStart('.').TrimEnd('$').ToLower(CultureInfo.CurrentCulture);
                return ArchiveExtensions.Contains(ext);
            }
            return false;
        }

        /// <summary>
        /// Gets or set the list of archive extensions (lowercase, without leading '.')
        /// </summary>
        public static List<string> ArchiveExtensions => ArchiveDirectory.Extensions;

        /// <summary>
        /// returns a list of archiveExtensions used to search for files (with leading '*.')
        /// </summary>
        public static List<string> ArchivePatterns => ArchiveDirectory.Patterns;

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
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                string[] paths = UiUtils.SplitPath(path, false);
                foreach (string subPath in paths)
                {
                    if (!File.Exists(subPath) && !Directory.Exists(subPath))
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CancelSearch = false;

        public static bool PrepareSearchPatterns(FileFilter filter, List<string> includeSearchPatterns)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            if (includeSearchPatterns == null)
                throw new ArgumentNullException(nameof(includeSearchPatterns));

            bool handled = false;
            if (!filter.IsRegex && !filter.NamePatternToInclude.Contains("#!"))
            {
                var includePatterns = UiUtils.SplitPattern(filter.NamePatternToInclude);
                foreach (var pattern in includePatterns)
                {
                    if (pattern == "*.doc" || pattern == "*.xls" || pattern == "*.ppt")
                        includeSearchPatterns.Add(pattern + "*");
                    else
                        includeSearchPatterns.Add(pattern);
                }
                handled = true;
            }
            return handled;
        }

        public static void PrepareFilters(FileFilter filter,
            List<Regex> includeRegexPatterns, List<Regex> excludeRegexPatterns,
            List<Regex> includeShebangPatterns, bool includePatternHandled)
        {
            if (includeRegexPatterns == null || excludeRegexPatterns == null || includeShebangPatterns == null)
                return;

            var includePatterns = UiUtils.SplitPattern(filter.NamePatternToInclude);
            if (HasShebangPattern(includePatterns))
            {
                foreach (var pattern in includePatterns.Where(p => HasShebangPattern(p)))
                {
                    includeShebangPatterns.Add(GetRegex(pattern, filter.IsRegex));
                }
            }

            // non-regex include patterns are used as search patterns in the call to EnumerateFiles
            if (filter.IsRegex || !includePatternHandled)
            {
                foreach (var pattern in includePatterns.Where(p => !HasShebangPattern(p)))
                {
                    includeRegexPatterns.Add(GetRegex(pattern, filter.IsRegex));
                }
            }

            var excludePatterns = UiUtils.SplitPattern(filter.NamePatternToExclude);
            foreach (var pattern in excludePatterns)
            {
                excludeRegexPatterns.Add(GetRegex(pattern, filter.IsRegex));
            }
        }

        private static Regex GetRegex(string pattern, bool isRegex)
        {
            lock (regexLock)
            {
                Regex regex = null;
                try
                {
                    if (!isRegex)
                        pattern = WildcardToRegex(pattern);

                    if (!regexCache.TryGetValue(pattern, out regex))
                    {
                        regex = new Regex(pattern, RegexOptions.IgnoreCase);
                        regexCache.Add(pattern, regex);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed in Utils.GetRegex");
                    throw;
                }
                return regex;
            }
        }

        public static IEnumerable<FileData> GetFileListIncludingArchives(FileFilter filter)
        {
            foreach (var file in GetFileListEx(filter))
            {
                if (IsArchive(file))
                {
                    foreach (var innerFile in ArchiveDirectory.EnumerateFiles(file, filter))
                    {
                        yield return innerFile;
                    }
                }
                else
                {
                    yield return new FileData(file);
                }
            }
        }

        /// <summary>
        /// Iterator based file search
        /// Searches folder and it's subfolders for files that match pattern and
        /// returns array of strings that contain full paths to the files.
        /// If no files found returns 0 length array.
        /// </summary>
        /// <param name="filter">the file filter parameters</param>
        /// <returns></returns>
        public static IEnumerable<string> GetFileListEx(FileFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (string.IsNullOrWhiteSpace(filter.Path) || filter.NamePatternToInclude == null)
            {
                yield break;
            }


            // Hash set to ensure file name uniqueness
            HashSet<string> matches = new HashSet<string>();

            var includeSearchPatterns = new List<string>();
            bool hasSearchPattern = PrepareSearchPatterns(filter, includeSearchPatterns);

            var includeRegexPatterns = new List<Regex>();
            var excludeRegexPatterns = new List<Regex>();
            var includeShebangPatterns = new List<Regex>();
            PrepareFilters(filter, includeRegexPatterns, excludeRegexPatterns, includeShebangPatterns, hasSearchPattern);

            if (filter.UseEverything)
            {
                var files = GetFileListEverything(filter, includeRegexPatterns, excludeRegexPatterns, includeShebangPatterns);
                foreach (var file in files)
                {
                    if (!matches.Contains(file))
                    {
                        matches.Add(file);
                        yield return file;
                    }
                }

                yield break;
            }

            foreach (var subPath in UiUtils.SplitPath(filter.Path, false))
            {
                if (File.Exists(subPath))
                {
                    if (IsArchive(subPath) && filter.IncludeArchive)
                    {
                        matches.Add(subPath);
                        yield return subPath;
                    }
                    else if (IncludeFile(subPath, filter, null, hasSearchPattern, includeSearchPatterns,
                        includeRegexPatterns, excludeRegexPatterns, includeShebangPatterns) &&
                        !matches.Contains(subPath))
                    {
                        matches.Add(subPath);
                        yield return subPath;
                    }
                    continue;
                }
                else if (!Directory.Exists(subPath))
                {
                    continue;
                }

                Gitignore gitignore = null;
                if (filter.UseGitIgnore)
                {
                    var gitDirectories = SafeDirectory.GetGitignoreDirectories(subPath, filter.IncludeSubfolders, filter.FollowSymlinks);
                    if (gitDirectories != null)
                    {
                        gitignore = GitUtil.GetGitignore(gitDirectories);
                    }
                }

                foreach (var filePath in SafeDirectory.EnumerateFiles(subPath, includeSearchPatterns, gitignore, filter))
                {
                    if (IsArchive(filePath))
                    {
                        if (filter.IncludeArchive)
                        {
                            matches.Add(filePath);
                            yield return filePath;
                        }
                    }
                    else if (IncludeFile(filePath, filter, null, hasSearchPattern, includeSearchPatterns,
                        includeRegexPatterns, excludeRegexPatterns, includeShebangPatterns) &&
                        !matches.Contains(filePath))
                    {
                        matches.Add(filePath);
                        yield return filePath;
                    }
                }
            }
        }

        private static IEnumerable<string> GetFileListEverything(FileFilter filter, IList<Regex> includeRegexPatterns,
            IList<Regex> excludeRegexPatterns, IList<Regex> includeShebangPatterns)
        {
            string searchString = filter.Path.Trim();
            if (filter.IncludeArchive)
            {
                // to search in archives, ask Everything to return all archive files
                searchString += "|*." + string.Join("|*.", ArchiveExtensions);
            }

            if (filter.SizeFrom > 0 || filter.SizeTo > 0)
            {
                searchString = AddEverythingSizeFilters(filter, searchString);
            }

            if (filter.DateFilter != FileDateFilter.None)
            {
                searchString = AddEverythingDateFilters(filter, searchString);
            }

            foreach (var fileInfo in EverythingSearch.FindFiles(searchString, filter.IncludeHidden))
            {
                FileData fileData = new FileData(fileInfo);

                if (IsArchive(fileInfo.FullName))
                {
                    if (filter.IncludeArchive)
                    {
                        yield return fileInfo.FullName;
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (IncludeFile(fileInfo.FullName, filter, fileData, true, new List<string>(),
                    includeRegexPatterns, excludeRegexPatterns, includeShebangPatterns))
                {
                    yield return fileInfo.FullName;
                }
            }
        }

        private static string AddEverythingSizeFilters(FileFilter filter, string searchString)
        {
            if ((filter.SizeFrom > 0 || filter.SizeTo > 0) && !searchString.Contains("size:"))
            {
                if (filter.SizeFrom == 0)
                {
                    searchString += $" size:<={filter.SizeTo}kb";
                }
                else if (filter.SizeTo == 0)
                {
                    searchString += $" size:>={filter.SizeFrom}kb";
                }
                else
                {
                    searchString += $" size:{filter.SizeFrom}kb-{filter.SizeTo}kb";
                }
            }
            return searchString;
        }

        private static string AddEverythingDateFilters(FileFilter filter, string searchString)
        {
            if (!filter.StartTime.HasValue && !filter.EndTime.HasValue)
            {
                return searchString;
            }

            string function = string.Empty;
            if (filter.DateFilter == FileDateFilter.Modified)
            {
                if (!searchString.Contains("datemodified:") && !searchString.Contains("dm:"))
                {
                    function += " dm:";
                }
            }
            else if (filter.DateFilter == FileDateFilter.Created)
            {
                if (!searchString.Contains("datecreated:") && !searchString.Contains("dc:"))
                {
                    function += " dc:";
                }
            }

            if (!string.IsNullOrEmpty(function))
            {
                if (filter.StartTime.HasValue && filter.EndTime.HasValue)
                {
                    function += $"{filter.StartTime.Value.ToIso8601DateTime()}-{filter.EndTime.Value.ToIso8601DateTime()}";
                }
                else if (filter.StartTime.HasValue)
                {
                    function += $">={filter.StartTime.Value.ToIso8601DateTime()}";
                }
                else if (filter.EndTime.HasValue)
                {
                    function += $"<={filter.EndTime.Value.ToIso8601DateTime()}";
                }
            }

            return searchString + function;
        }

        //public static string Quote(string text)
        //{
        //    return "\"" + text + "\"";
        //}

        /// <summary>
        /// Evaluates if a file should be included in the search results
        /// </summary>
        public static bool IncludeFile(string filePath, FileFilter filter, FileData fileInfo,
            bool hasSearchPattern, IList<string> includeSearchPatterns,
            IList<Regex> includeRegexPatterns, IList<Regex> excludeRegexPatterns,
            IList<Regex> includeShebangPatterns)
        {
            try
            {
                // check filters that do not read the file first...

                // regex include
                if (includeRegexPatterns != null && includeRegexPatterns.Count > 0)
                {
                    bool include = false;
                    foreach (var pattern in includeRegexPatterns)
                    {
                        if (pattern.IsMatch(filePath))
                        {
                            include = true;
                            break;
                        }
                    }
                    if (!include)
                    {
                        return false;
                    }
                }

                // exclude this file?
                // wildcard exclude files are converted to regex
                foreach (var pattern in excludeRegexPatterns)
                {
                    if (pattern.IsMatch(filePath))
                    {
                        return false;
                    }
                }

                if (filter.SkipRemoteCloudStorageFiles)
                {
                    var attr = (uint)File.GetAttributes(filePath);
                    bool FILE_ATTRIBUTE_RECALL_ON_OPEN = (attr & 0x40000) == 0x40000;
                    bool FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = (attr & 0x400000) == 0x400000;

                    if (FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS || FILE_ATTRIBUTE_RECALL_ON_OPEN)
                    {
                        return false;
                    }
                }

                if ((filter.SizeFrom > 0 || filter.SizeTo > 0) && !filter.UseEverything) // Everything search has size filter in query
                {
                    if (fileInfo == null)
                        fileInfo = new FileData(filePath);

                    long sizeKB = fileInfo.Length / 1000;
                    if (filter.SizeFrom > 0 && sizeKB < filter.SizeFrom)
                    {
                        return false;
                    }
                    if (filter.SizeTo > 0 && sizeKB > filter.SizeTo)
                    {
                        return false;
                    }
                }

                if (filter.DateFilter != FileDateFilter.None && !filter.UseEverything) // Everything search has date filter in query
                {
                    if (fileInfo == null)
                        fileInfo = new FileData(filePath);

                    DateTime fileDate = filter.DateFilter == FileDateFilter.Created ? fileInfo.CreationTime : fileInfo.LastWriteTime;
                    if (filter.StartTime.HasValue && fileDate < filter.StartTime.Value)
                    {
                        return false;
                    }
                    if (filter.EndTime.HasValue && fileDate >= filter.EndTime.Value)
                    {
                        return false;
                    }
                }

                if (!filter.IncludeBinary && !IsArchive(filePath) && !IsFileInArchive(filePath))
                {
                    bool isExcelMatch = IsExcelFile(filePath) && includeSearchPatterns.Contains(".xls", StringComparison.OrdinalIgnoreCase);
                    bool isWordMatch = IsWordFile(filePath) && includeSearchPatterns.Contains(".doc", StringComparison.OrdinalIgnoreCase);
                    bool isPowerPointMatch = IsPowerPointFile(filePath) && includeSearchPatterns.Contains(".ppt", StringComparison.OrdinalIgnoreCase);
                    bool isPdfMatch = IsPdfFile(filePath) && includeSearchPatterns.Contains(".pdf", StringComparison.OrdinalIgnoreCase);

                    // When searching for Excel, Word, PowerPoint, or PDF files, skip the binary file check:
                    // If someone is searching for one of these types, don't make them include binary to 
                    // find their files.
                    if (!(isExcelMatch || isWordMatch || isPowerPointMatch || isPdfMatch) && IsBinary(filePath))
                    {
                        return false;
                    }
                }

                if (includeShebangPatterns != null && includeShebangPatterns.Any())
                {
                    bool include = false;
                    foreach (var pattern in includeShebangPatterns)
                    {
                        if (CheckShebang(filePath, pattern.ToString()))
                        {
                            include = true;
                            break;
                        }
                    }
                    if (!include)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failure in applying file filters");
                // returning true shows an error in the results tree
                return true;
            }
        }

        public static bool HasShebangPattern(IList<string> patterns)
        {
            if (patterns != null)
            {
                foreach (var pattern in patterns)
                {
                    if (HasShebangPattern(pattern))
                        return true;
                }
            }
            return false;
        }

        public static bool HasShebangPattern(string pattern)
        {
            return pattern != null && pattern.Length > 2 && pattern[0] == '#' && pattern[1] == '!';
        }

        public static bool CheckShebang(string file, string pattern)
        {
            if (HasShebangPattern(pattern))
            {
                using (FileStream readStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return CheckShebang(readStream, pattern);
                }
            }
            return false;
        }

        public static bool CheckShebang(Stream stream, string pattern)
        {
            bool result = false;
            if (HasShebangPattern(pattern))
            {
                using (StreamReader streamReader = new StreamReader(stream, GetFileEncoding(stream), false, 4096, true))
                {
                    string firstLine = streamReader.ReadLine();
                    // Check if first 2 bytes are '#!'
                    if (firstLine[0] == '#' && firstLine[1] == '!')
                    {
                        // Do more reading (start from 3rd character in case there is a space after #!)
                        for (int i = 3; i < firstLine.Length; i++)
                        {
                            if (firstLine[i] == ' ' || firstLine[i] == '\r' || firstLine[i] == '\n' || firstLine[i] == '\t')
                            {
                                firstLine = firstLine.Substring(0, i);
                                break;
                            }
                        }
                        result = Regex.IsMatch(firstLine.Substring(2).Trim(), pattern.Substring(2), RegexOptions.IgnoreCase);
                    }
                }
                stream.Seek(0, SeekOrigin.Begin);
            }
            return result;
        }

        /// <summary>
        /// Searches folder and it's subfolders for files that match pattern and
        /// returns array of strings that contain full paths to the files.
        /// If no files found returns 0 length array.
        /// </summary>
        /// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
        /// <param name="namePatternToInclude">File name pattern. (E.g. *.cs) or regex to include. If null returns empty array. If empty string returns all files.</param>
        /// <param name="namePatternToExclude">File name pattern. (E.g. *.cs) or regex to exclude. If null or empty is ignored.</param>
        /// <param name="isRegex">Whether to use regex as search pattern. Otherwise use asterisks</param>
        /// <param name="useEverything">use Everything for file search</param>
        /// <param name="includeSubfolders">Include sub-folders</param>
        /// <param name="includeHidden">Include hidden folders</param>
        /// <param name="includeBinary">Include binary files</param>
        /// <param name="includeArchive">Include search in archives</param>
        /// <param name="followSymlinks">Include search in symbolic links</param>
        /// <param name="sizeFrom">Size in KB</param>
        /// <param name="sizeTo">Size in KB</param>
        /// <param name="dateFilter">Filter by file modified or created date time range</param>
        /// <param name="startTime">start of time range</param>
        /// <param name="endTime">end of time range</param>
        /// <param name="useGitignore">use .gitignore as an exclusion filter</param>
        /// <param name="maxSubfolderDepth">Max depth of sub-folders where 0 is root only and -1 is all</param>
        /// <returns>List of file or empty list if nothing is found</returns>
        public static string[] GetFileList(string path, string namePatternToInclude, string namePatternToExclude, bool isRegex,
            bool useEverything, bool includeSubfolders, bool includeHidden, bool includeBinary, bool includeArchive,
            bool followSymlinks, int sizeFrom, int sizeTo, FileDateFilter dateFilter,
            DateTime? startTime, DateTime? endTime, bool useGitignore, int maxSubfolderDepth,
            bool skipRemoteCloudStorageFiles = true)
        {
            var filter = new FileFilter(path, namePatternToInclude, namePatternToExclude, isRegex, useGitignore, useEverything,
                includeSubfolders, maxSubfolderDepth, includeHidden, includeBinary, includeArchive, followSymlinks, sizeFrom, sizeTo,
                dateFilter, startTime, endTime, skipRemoteCloudStorageFiles);
            return GetFileListEx(filter).ToArray();
        }

        /// <summary>
        /// Converts unix asterisk based file pattern to regex
        /// </summary>
        /// <param name="wildcard">Asterisk based pattern</param>
        /// <returns>Regular expression of null is empty</returns>
        public static string WildcardToRegex(string wildcard)
        {
            if (string.IsNullOrWhiteSpace(wildcard)) return wildcard;

            StringBuilder sb = new StringBuilder();

            char[] chars = wildcard.ToCharArray();
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
        /// <param name="value">String. May include null, empty string or text with spaces before or after.</param>
        /// <returns>Attempts to parse string. Otherwise returns int.MinValue</returns>
        public static int ParseInt(string value)
        {
            return ParseInt(value, int.MinValue);
        }

        /// <summary>
        /// Parses text into int
        /// </summary>
        /// <param name="value">String. May include null, empty string or text with spaces before or after.</param>
        /// <param name="defaultValue">Default value if fails to parse.</param>
        /// <returns>Attempts to parse string. Otherwise returns defaultValue</returns>
        public static int ParseInt(string value, int defaultValue)
        {
            if (value != null && value.Length != 0)
            {
                value = value.Trim();
                if (int.TryParse(value, out int output))
                {
                    return output;
                }
            }
            return defaultValue;
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
            string filePath = args.SearchResult.FileNameDisplayed;
            if (filePath != null && filePath.Length > 260)
                filePath = Path.GetShort83Path(filePath);

            if (!args.UseCustomEditor || string.IsNullOrWhiteSpace(args.CustomEditor))
            {
                try
                {
                    using (var proc = Process.Start(UiUtils.Quote(filePath)))
                    {
                    }
                }
                catch
                {
                    using (var proc = new Process())
                    {
                        proc.StartInfo = new ProcessStartInfo("notepad.exe")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            Arguments = filePath
                        };
                        proc.Start();
                    }
                }
            }
            else
            {
                if (args.CustomEditorArgs == null)
                    args.CustomEditorArgs = "";

                using (var proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo(args.CustomEditor)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Arguments = args.CustomEditorArgs.Replace("%file", UiUtils.Quote(filePath))
                            .Replace("%line", args.LineNumber.ToString())
                            .Replace("%pattern", args.Pattern)
                            .Replace("%match", args.FirstMatch)
                            .Replace("%column", args.ColumnNumber.ToString()),
                    };
                    proc.Start();
                }
            }
        }

        /// <summary>
        /// Returns path to a temp folder used by dnGREP (including trailing slash). If folder does not exist
        /// it gets created.
        /// </summary>
        /// <returns></returns>
        public static string GetTempFolder()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), tempFolderName);
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
            return tempPath + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Deletes temp folder
        /// </summary>
        public static void DeleteTempFolder()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), tempFolderName);
            try
            {
                if (Directory.Exists(tempPath))
                    DeleteFolder(tempPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to delete temp folder");
            }
        }

        /// <summary>
        /// Returns path to a folder used by dnGREP for undo files (including trailing slash). If folder does not exist
        /// it gets created.
        /// </summary>
        /// <returns></returns>
        public static string GetUndoFolder()
        {
            string undoPath = Path.Combine(Path.GetTempPath(), undoFolderName);
            if (!Directory.Exists(undoPath))
            {
                Directory.CreateDirectory(undoPath);
            }
            return undoPath + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Deletes undo folder
        /// </summary>
        public static void DeleteUndoFolder()
        {
            string undoPath = Path.Combine(Path.GetTempPath(), undoFolderName);
            try
            {
                if (Directory.Exists(undoPath))
                    DeleteFolder(undoPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to delete undo folder");
            }
        }

        public static string GetUniqueKey(int size)
        {
            byte[] data = new byte[4 * size];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Open folder in explorer
        /// </summary>
        /// <param name="fileName"></param>
        public static void OpenContainingFolder(string fileName)
        {
            if (fileName.Length > 260)
                fileName = Path.GetShort83Path(fileName);

            using (Process.Start("explorer.exe", "/select,\"" + fileName + "\""))
            {
            }
        }

        public static void CompareFiles(IList<GrepSearchResult> files)
        {
            var settings = GrepSettings.Instance;
            string application = settings.Get<string>(GrepSettings.Key.CompareApplication);
            string args = settings.Get<string>(GrepSettings.Key.CompareApplicationArgs);

            if (!string.IsNullOrWhiteSpace(application))
            {
                List<string> paths = new List<string>();
                foreach (var item in files)
                {
                    string filePath = item.FileNameReal;
                    if (Utils.IsArchive(filePath))
                        filePath = ArchiveDirectory.ExtractToTempFile(item);

                    if (!paths.Contains(filePath))
                        paths.Add(filePath);

                    if (paths.Count == 3)
                        break;
                }

                string appArgs = string.IsNullOrWhiteSpace(args) ? string.Empty : args + " ";
                string fileArgs = string.Join(" ", paths.Select(p => UiUtils.Quote(p)));

                using (var proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo(application)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Arguments = appArgs + fileArgs
                    };
                    proc.Start();
                }
            }
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
            if (!canUseCurrentFolder.HasValue)
            {
                // if started in Admin mode, the user can write to these directories
                // so filter them out first...
                if (currentFolder.IsSubDirectoryOf(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)) ||
                    currentFolder.IsSubDirectoryOf(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) ||
                    currentFolder.IsSubDirectoryOf(Environment.GetFolderPath(Environment.SpecialFolder.Windows)))
                {
                    canUseCurrentFolder = false;
                }
                else
                {
                    canUseCurrentFolder = HasWriteAccessToFolder(currentFolder);
                }
            }

            if (canUseCurrentFolder == true)
            {
                return currentFolder;
            }
            else
            {
                string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnGREP");
                if (!Directory.Exists(dataFolder))
                    Directory.CreateDirectory(dataFolder);
                return dataFolder;
            }
        }

        public static bool IsPortableMode
        {
            get
            {
                if (!canUseCurrentFolder.HasValue)
                {
                    GetDataFolderPath();
                }
                return canUseCurrentFolder.Value;
            }
        }

        public static bool IsSubDirectoryOf(this string candidate, string other)
        {
            var isChild = false;
            try
            {
                var candidateInfo = new DirectoryInfo(candidate);
                var otherInfo = new DirectoryInfo(other);

                while (candidateInfo.Parent != null)
                {
                    if (candidateInfo.Parent.FullName == otherInfo.FullName)
                    {
                        isChild = true;
                        break;
                    }
                    else candidateInfo = candidateInfo.Parent;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Unable to check directories {candidate} and {other}");
            }

            return isChild;
        }

        private static bool HasWriteAccessToFolder(string folderPath)
        {
            string filename = Path.Combine(folderPath, "~temp.dat");
            bool canAccess = true;
            //1. Provide early notification that the user does not have permission to write.
            FileIOPermission writePermission = new FileIOPermission(FileIOPermissionAccess.Write, filename);
            var permissionSet = new PermissionSet(PermissionState.None);
            permissionSet.AddPermission(writePermission);
            bool isGranted = permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
            if (!isGranted)
            {
                //No permission. 
                canAccess = false;
            }


            //2. Attempt the action but handle permission changes.
            if (canAccess)
            {
                try
                {
                    using (FileStream fstream = File.Open(filename, FileMode.Create))
                    using (TextWriter writer = new StreamWriter(fstream))
                    {
                        writer.WriteLine("sometext");
                    }
                }
                catch
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
            catch
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
        /// Returns read-only from the results
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

        /// <summary>
        /// Checks if another process has a lock on the file
        /// </summary>
        /// <remarks>
        /// This test can be very slow, do not use for UI updates or on a long list of files
        /// </remarks>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool IsFileLocked(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    var list = File.GetProcessForFileLock(filePath);

                    return list != null && list.Count > 0;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failure in file lock check");
                }
            }

            return false;
        }

        public static bool IsReadOnly(GrepSearchResult result)
        {
            if (result.IsHexFile)
            {
                return true;
            }

            if (result.IsReadOnlyFileType)
            {
                return true;
            }

            if (File.Exists(result.FileNameReal))
            {
                return File.GetAttributes(result.FileNameReal).HasFlag(FileAttributes.ReadOnly);
            }

            return false;
        }

        public static bool HasReadOnlyAttributeSet(GrepSearchResult result)
        {
            if (File.Exists(result.FileNameReal))
            {
                return File.GetAttributes(result.FileNameReal).HasFlag(FileAttributes.ReadOnly);
            }

            return false;
        }

        public static string[] GetLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new string[0];
            }
            else
            {
                return text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            }
        }

        public static string GetEOL(string path, Encoding encoding)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                using (FileStream reader = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader streamReader = new StreamReader(reader, encoding))
                using (EolReader eolReader = new EolReader(streamReader))
                {
                    string line = eolReader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        if (line.EndsWith("\r\n"))
                            return "\r\n";
                        else if (line.EndsWith("\n"))
                            return "\n";
                        else if (line.EndsWith("\r"))
                            return "\r";
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Retrieves lines with context based on matches
        /// </summary>
        /// <param name="body">Text</param>
        /// <param name="bodyMatches">List of matches with positions relative to entire text body</param>
        /// <param name="beforeLines">Context line (before)</param>
        /// <param name="afterLines">Context line (after</param>
        /// <param name="isPdfText">True if file is PDF text and to count a page for each \f character</param>
        /// <returns></returns>
        public static List<GrepLine> GetLinesEx(TextReader body, List<GrepMatch> bodyMatches, int beforeLines, int afterLines, bool isPdfText = false)
        {
            if (body == null || bodyMatches == null)
                return new List<GrepLine>();

            List<GrepMatch> bodyMatchesClone = new List<GrepMatch>(bodyMatches);
            Dictionary<int, GrepLine> results = new Dictionary<int, GrepLine>();
            Dictionary<int, int> lineToPageMap = new Dictionary<int, int>();
            List<GrepLine> contextLines = new List<GrepLine>();
            Dictionary<int, string> lineStrings = new Dictionary<int, string>();
            List<int> lineNumbers = new List<int>();
            List<GrepMatch> matches = new List<GrepMatch>();

            string ZWSP = char.ConvertFromUtf32(0x200B); //zero width space 

            // Context line (before)
            Queue<string> beforeQueue = new Queue<string>();
            // Context line (after)
            int currentAfterLine = 0;
            bool startRecordingAfterLines = false;
            // Current page (using \f)
            int pageNumber = isPdfText ? 1 : -1;
            // Current line
            int lineNumber = 0;
            // Current index of character
            int currentIndex = 0;
            int startIndex = 0;
            int tempLinesTotalLength = 0;
            int startLine = 0;
            bool startMatched = false;
            Queue<string> lineQueue = new Queue<string>();

            using (EolReader reader = new EolReader(body))
            {
                while (!reader.EndOfStream && (bodyMatchesClone.Count > 0 || startRecordingAfterLines))
                {
                    lineNumber++;
                    string line = reader.ReadLine();
                    if (isPdfText)
                    {
                        if (reader.EndOfStream && line.Equals("\f"))
                        {
                            break;
                        }

                        pageNumber += line.Count(c => c.Equals('\f'));
                        // replace the form feed character with a zero width space; keeps the same character count
                        line = line.Replace("\f", ZWSP);
                        lineToPageMap.Add(lineNumber, pageNumber);
                    }

                    bool moreMatches = true;
                    // Building context queue
                    if (beforeLines > 0)
                    {
                        if (beforeQueue.Count >= beforeLines + 1)
                            beforeQueue.Dequeue();

                        beforeQueue.Enqueue(line.TrimEndOfLine());
                    }
                    if (startRecordingAfterLines && currentAfterLine < afterLines)
                    {
                        currentAfterLine++;
                        contextLines.Add(new GrepLine(lineNumber, line.TrimEndOfLine(), true, null) { PageNumber = pageNumber });
                    }
                    else if (currentAfterLine == afterLines)
                    {
                        currentAfterLine = 0;
                        startRecordingAfterLines = false;
                    }

                    while (moreMatches && bodyMatchesClone.Count > 0)
                    {
                        // Head of match found
                        if (bodyMatchesClone[0].StartLocation >= currentIndex && bodyMatchesClone[0].StartLocation < currentIndex + line.Length && !startMatched)
                        {
                            startMatched = true;
                            moreMatches = true;
                            lineQueue = new Queue<string>();
                            startLine = lineNumber;
                            startIndex = bodyMatchesClone[0].StartLocation - currentIndex;
                            tempLinesTotalLength = 0;

                            // Recording the before match context lines
                            while (beforeQueue.Count > 0)
                            {
                                // If only 1 line - it is the same as matched line
                                if (beforeQueue.Count == 1)
                                    beforeQueue.Dequeue();
                                else
                                    contextLines.Add(new GrepLine(startLine - beforeQueue.Count + 1 + (lineNumber - startLine),
                                        beforeQueue.Dequeue(), true, null)
                                    { PageNumber = pageNumber });
                            }
                        }

                        // Add line to queue
                        if (startMatched)
                        {
                            lineQueue.Enqueue(line);
                            tempLinesTotalLength += line.Length;
                        }

                        // Tail of match found
                        if (bodyMatchesClone[0].StartLocation + bodyMatchesClone[0].Length <= currentIndex + line.Length && startMatched)
                        {
                            startMatched = false;
                            moreMatches = false;
                            int firstLineLength = lineQueue.Peek().Length;
                            bool multilineMatch = startLine != lineNumber;
                            bool multilineGroups = bodyMatchesClone[0].Groups.Any(g => g.StartLocation > firstLineLength);
                            int startOfLineIndex = 0;
                            // Start creating matches
                            for (int i = startLine; i <= lineNumber; i++)
                            {
                                lineNumbers.Add(i);
                                string tempLine = lineQueue.Dequeue();
                                lineStrings[i] = tempLine;

                                string fileMatchId = bodyMatchesClone[0].FileMatchId;

                                List<GrepCaptureGroup> lineGroups;
                                // for multiline regex, get just the groups on the current line
                                if (multilineMatch)
                                {
                                    lineGroups = bodyMatchesClone[0].Groups.Where(g => g.StartLocation >= startOfLineIndex &&
                                            g.StartLocation < startOfLineIndex + tempLine.Length)
                                        .Select(g => new GrepCaptureGroup(g.Name, g.StartLocation - startOfLineIndex, g.Length, g.Value))
                                        .ToList();
                                }
                                else if (multilineGroups)
                                {
                                    lineGroups = bodyMatchesClone[0].Groups.Where(g => g.StartLocation >= currentIndex &&
                                            g.StartLocation < currentIndex + tempLine.Length)
                                        .Select(g => new GrepCaptureGroup(g.Name, g.StartLocation - currentIndex, g.Length, g.Value))
                                        .ToList();
                                }
                                else
                                {
                                    lineGroups = bodyMatchesClone[0].Groups;
                                }

                                // First and only line
                                if (i == startLine && i == lineNumber)
                                    matches.Add(new GrepMatch(fileMatchId, bodyMatchesClone[0].SearchPattern, i, startIndex, bodyMatchesClone[0].Length, lineGroups));
                                // First but not last line
                                else if (i == startLine)
                                    matches.Add(new GrepMatch(fileMatchId, bodyMatchesClone[0].SearchPattern, i, startIndex, tempLine.TrimEndOfLine().Length - startIndex, lineGroups));
                                // Middle line
                                else if (i > startLine && i < lineNumber)
                                    matches.Add(new GrepMatch(fileMatchId, bodyMatchesClone[0].SearchPattern, i, 0, tempLine.TrimEndOfLine().Length, lineGroups));
                                // Last line
                                else
                                    matches.Add(new GrepMatch(fileMatchId, bodyMatchesClone[0].SearchPattern, i, 0, bodyMatchesClone[0].Length - tempLinesTotalLength + line.Length + startIndex, lineGroups));

                                startOfLineIndex += tempLine.TrimEndOfLine().Length + 1; //add 1 for the \n character that was used when the regex was run
                                startRecordingAfterLines = true;
                            }
                            bodyMatchesClone.RemoveAt(0);
                        }

                        // Another match on this line
                        if (bodyMatchesClone.Count > 0 && bodyMatchesClone[0].StartLocation >= currentIndex && bodyMatchesClone[0].StartLocation < currentIndex + line.Length && !startMatched)
                            moreMatches = true;
                        else
                            moreMatches = false;
                    }

                    currentIndex += line.Length;
                }
            }

            if (lineStrings.Count == 0)
            {
                return new List<GrepLine>();
            }

            // Removing duplicate lines (when more than 1 match is on the same line) and grouping all matches belonging to the same line
            for (int i = 0; i < matches.Count; i++)
            {
                if (isPdfText)
                {
                    if (lineToPageMap.ContainsKey(matches[i].LineNumber))
                    {
                        pageNumber = lineToPageMap[matches[i].LineNumber];
                    }
                    else
                    {
                        pageNumber = 0;
                    }
                }
                AddGrepMatch(results, matches[i], lineStrings[matches[i].LineNumber], pageNumber, false);
            }
            for (int i = 0; i < contextLines.Count; i++)
            {
                if (!results.ContainsKey(contextLines[i].LineNumber))
                    results[contextLines[i].LineNumber] = contextLines[i];
            }

            return results.Values.OrderBy(l => l.LineNumber).ToList();
        }

        public static List<GrepLine> GetLinesHexFormat(BinaryReader body, List<GrepMatch> bodyMatches, int beforeLines, int afterLines)
        {
            if (body == null || bodyMatches == null)
                return new List<GrepLine>();

            //List<GrepMatch> bodyMatchesClone = new List<GrepMatch>(bodyMatches);
            Dictionary<int, GrepLine> results = new Dictionary<int, GrepLine>();
            List<GrepLine> contextLines = new List<GrepLine>();
            Dictionary<int, string> lineStrings = new Dictionary<int, string>();
            List<int> lineNumbers = new List<int>();
            List<GrepMatch> matches = new List<GrepMatch>();

            // Context line (before)
            Queue<string> beforeQueue = new Queue<string>();
            // Context line (after)
            int currentAfterLine = 0;
            bool startRecordingAfterLines = false;
            // Current line
            int lineNumber = 0;
            // Current index of character
            int currentIndex = 0;
            int startIndex = 0;
            int tempLinesTotalLength = 0;
            int startLine = 0;
            bool startMatched = false;
            Queue<string> lineQueue = new Queue<string>();

            int bufferSize = GrepSettings.Instance.Get<int>(GrepSettings.Key.HexResultByteLength);
            byte[] buffer = new byte[bufferSize];
            long length = body.BaseStream.Length;

            List<GrepMatch> bodyMatchesClone = ConvertGrepMatchesToHexLines(bodyMatches, bufferSize);

            while (body.BaseStream.Position < length && (bodyMatchesClone.Count > 0 || startRecordingAfterLines))
            {
                buffer = body.ReadBytes(bufferSize);
                string line = GetHexText(buffer);
                lineNumber++;
                bool moreMatches = true;
                // Building context queue
                if (beforeLines > 0)
                {
                    if (beforeQueue.Count >= beforeLines + 1)
                        beforeQueue.Dequeue();

                    beforeQueue.Enqueue(line.TrimEndOfLine());
                }
                if (startRecordingAfterLines && currentAfterLine < afterLines)
                {
                    currentAfterLine++;
                    contextLines.Add(new GrepLine(lineNumber, line.TrimEndOfLine(), true, null) { IsHexFile = true });
                }
                else if (currentAfterLine == afterLines)
                {
                    currentAfterLine = 0;
                    startRecordingAfterLines = false;
                }

                while (moreMatches && bodyMatchesClone.Count > 0)
                {
                    // Head of match found
                    if (bodyMatchesClone[0].StartLocation >= currentIndex && bodyMatchesClone[0].StartLocation < currentIndex + line.Length && !startMatched)
                    {
                        startMatched = true;
                        moreMatches = true;
                        lineQueue = new Queue<string>();
                        startLine = lineNumber;
                        startIndex = bodyMatchesClone[0].StartLocation - currentIndex;
                        tempLinesTotalLength = 0;

                        // Recording the before match context lines
                        while (beforeQueue.Count > 0)
                        {
                            // If only 1 line - it is the same as matched line
                            if (beforeQueue.Count == 1)
                            {
                                beforeQueue.Dequeue();
                            }
                            else
                            {
                                contextLines.Add(new GrepLine(startLine - beforeQueue.Count + 1 + (lineNumber - startLine),
                                    beforeQueue.Dequeue(), true, null)
                                {
                                    IsHexFile = true
                                });
                            }
                        }
                    }

                    // Add line to queue
                    if (startMatched)
                    {
                        lineQueue.Enqueue(line);
                        tempLinesTotalLength += line.Length;
                    }

                    // Tail of match found
                    if (bodyMatchesClone[0].StartLocation + bodyMatchesClone[0].Length <= currentIndex + line.Length && startMatched)
                    {
                        startMatched = false;
                        moreMatches = false;
                        // Start creating matches
                        for (int i = startLine; i <= lineNumber; i++)
                        {
                            lineNumbers.Add(i);
                            string tempLine = lineQueue.Dequeue();
                            lineStrings[i] = tempLine;

                            string fileMatchId = bodyMatchesClone[0].FileMatchId;
                            // First and only line
                            if (i == startLine && i == lineNumber)
                                matches.Add(new GrepMatch(fileMatchId, bodyMatchesClone[0].SearchPattern, i, startIndex, bodyMatchesClone[0].Length, bodyMatchesClone[0].Groups));
                            // First but not last line
                            else if (i == startLine)
                                matches.Add(new GrepMatch(fileMatchId, bodyMatchesClone[0].SearchPattern, i, startIndex, tempLine.TrimEndOfLine().Length - startIndex, bodyMatchesClone[0].Groups));
                            // Middle line
                            else if (i > startLine && i < lineNumber)
                                matches.Add(new GrepMatch(fileMatchId, bodyMatchesClone[0].SearchPattern, i, 0, tempLine.TrimEndOfLine().Length, bodyMatchesClone[0].Groups));
                            // Last line
                            else
                                matches.Add(new GrepMatch(fileMatchId, bodyMatchesClone[0].SearchPattern, i, 0, bodyMatchesClone[0].Length - tempLinesTotalLength + line.Length + startIndex, bodyMatchesClone[0].Groups));

                            startRecordingAfterLines = true;
                        }
                        bodyMatchesClone.RemoveAt(0);
                    }

                    // Another match on this line
                    if (bodyMatchesClone.Count > 0 && bodyMatchesClone[0].StartLocation >= currentIndex && bodyMatchesClone[0].StartLocation < currentIndex + line.Length && !startMatched)
                        moreMatches = true;
                    else
                        moreMatches = false;
                }

                currentIndex += line.Length;
            }

            if (lineStrings.Count == 0)
            {
                return new List<GrepLine>();
            }

            // Removing duplicate lines (when more than 1 match is on the same line) and grouping all matches belonging to the same line
            for (int i = 0; i < matches.Count; i++)
            {
                AddGrepMatch(results, matches[i], lineStrings[matches[i].LineNumber], -1, true);
            }
            for (int i = 0; i < contextLines.Count; i++)
            {
                if (!results.ContainsKey(contextLines[i].LineNumber))
                    results[contextLines[i].LineNumber] = contextLines[i];
            }

            return results.Values.OrderBy(l => l.LineNumber).ToList();
        }

        private static List<GrepMatch> ConvertGrepMatchesToHexLines(List<GrepMatch> bodyMatches, int bufferSize)
        {
            // 2 digit hex number plus space for each byte
            // and trailing space is removed
            int lineLength = bufferSize * 3 - 1;

            List<GrepMatch> list = new List<GrepMatch>();
            foreach (GrepMatch match in bodyMatches)
            {
                int lineNum = match.StartLocation / bufferSize;
                int lineStart = match.StartLocation % bufferSize;
                int startLocation = lineNum * lineLength + lineStart * 3;
                int matchLength = match.Length * 3 - 1;

                int newLines = (match.StartLocation % bufferSize + match.Length - 1) / bufferSize;
                matchLength -= newLines;

                list.Add(new GrepMatch(match.SearchPattern, lineNum, startLocation, matchLength));
            }
            return list;
        }

        private static string GetHexText(byte[] buffer)
        {
            StringBuilder sb = new StringBuilder();

            for (int idx = 0; idx < buffer.Length; idx++)
            {
                sb.AppendFormat("{0:x2}", buffer[idx]).Append(" ");
            }

            return sb.ToString().TrimEnd();
        }

        private static void AddGrepMatch(Dictionary<int, GrepLine> lines, GrepMatch match, string lineText, int pageNumber, bool isHexFile)
        {
            if (!lines.ContainsKey(match.LineNumber))
                lines[match.LineNumber] = new GrepLine(match.LineNumber, lineText.TrimEndOfLine(), false, null) { PageNumber = pageNumber, IsHexFile = isHexFile };
            lines[match.LineNumber].Matches.Add(match);
        }

        /// <summary>
        /// Converts result lines into blocks of text
        /// </summary>
        /// <param name="result"></param>
        /// <param name="linesBefore"></param>
        /// <param name="linesAfter"></param>
        /// <returns></returns>
        public static IEnumerable<NumberedString> GetSnippets(GrepSearchResult result, int linesBefore, int linesAfter)
        {
            if (result.Matches.Count > 0)
            {
                int lastLine = 0;
                int firstLine = 0;
                StringBuilder snippetText = new StringBuilder();
                var lines = result.GetLinesWithContext(linesBefore, linesAfter);
                foreach (var line in lines)
                {
                    // First line of a block
                    if (firstLine == 0)
                    {
                        firstLine = line.LineNumber;
                        lastLine = line.LineNumber - 1;
                    }
                    // Sequence
                    if (line.LineNumber == lastLine + 1)
                    {
                        snippetText.AppendLine(line.LineText);
                    }
                    else
                    {
                        yield return new NumberedString { Text = snippetText.ToString().TrimEndOfLine(), FirstLineNumber = firstLine, LineCount = lines.Count };
                        lastLine = 0;
                        firstLine = 0;
                        snippetText.Clear();
                    }
                    lastLine = line.LineNumber;
                }
                if (snippetText.Length > 0)
                    yield return new NumberedString { Text = snippetText.ToString().TrimEndOfLine(), FirstLineNumber = firstLine, LineCount = lines.Count };
            }
            else
            {
                yield return new NumberedString() { LineCount = 0, FirstLineNumber = 0, Text = "" };
            }
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
        public static void CleanResults(ref List<GrepLine> results)
        {
            if (results == null || results.Count == 0)
                return;

            results.Sort();
            for (int i = results.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < results.Count; j++)
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
        /// Merges sorted context lines into sorted result lines
        /// </summary>
        /// <param name="results"></param>
        public static void MergeResults(ref List<GrepLine> results, List<GrepLine> contextLines)
        {
            if (contextLines == null || contextLines.Count == 0)
                return;

            if (results == null || results.Count == 0)
            {
                results = new List<GrepLine>();
                foreach (var line in contextLines)
                    results.Add(line);
                return;
            }

            // Current list location
            int rIndex = 0;
            int cIndex = 0;

            while (rIndex < results.Count && cIndex < contextLines.Count)
            {
                if (contextLines[cIndex].LineNumber < results[rIndex].LineNumber)
                {
                    results.Insert(rIndex, contextLines[cIndex]);
                    cIndex++;
                    rIndex++;
                }
                else if (results[rIndex].LineNumber < contextLines[cIndex].LineNumber)
                {
                    rIndex++;
                }
                else if (results[rIndex].LineNumber == contextLines[cIndex].LineNumber)
                {
                    rIndex++;
                    cIndex++;
                }
            }

            while (cIndex < contextLines.Count)
            {
                results.Add(contextLines[cIndex]);
                cIndex++;
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
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);

                // step 2, convert byte array to hex string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns true if beginText end with a non-alphanumeric character. Copied from AtroGrep.
        /// </summary>
        /// <param name="beginText">Text to test</param>
        /// <returns></returns>
        public static bool IsValidBeginText(string beginText)
        {
            if (beginText == null) 
                return false;

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
               beginText.EndsWith("=") ||
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
            if (string.IsNullOrEmpty(input)) 
                return string.Empty;

            string result = input.Replace(@"\\a", "\a")
                                 .Replace(@"\\b", "\b")
                                 .Replace(@"\\f", "\f")
                                 .Replace(@"\\n", "\n")
                                 .Replace(@"\\r", "\r")
                                 .Replace(@"\\t", "\t")
                                 .Replace(@"\\v", "\v")
                                 .Replace(@"\\0", "\0");
            return result;
        }

        /// <summary>
        /// Returns true if endText starts with a non-alphanumeric character. Copied from AtroGrep.
        /// </summary>
        /// <param name="endText"></param>
        /// <returns></returns>
        public static bool IsValidEndText(string endText)
        {
            if (endText == null)
                return false;

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
               endText.StartsWith("=") ||
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


        /// <summary>
        /// Extension method on TimeSpan that gets a "pretty", human readable string of a TimeSpan, e.g. "1h 23m 45.678s".
        /// Hours and minutes are left off as not needed. Hours are the largest unit of time shown (e.g. not days, weeks).
        /// </summary>
        /// <param name="duration">The time span in question.</param>
        /// <returns>"Pretty", human readable string of the time span.</returns>
        public static string GetPrettyString(this TimeSpan duration)
        {
            var durationStringBuilder = new System.Text.StringBuilder();
            var totalHoursTruncated = (int)duration.TotalHours;

            if (totalHoursTruncated > 0)
                durationStringBuilder.Append(totalHoursTruncated + "h ");

            if (duration.Minutes > 0 || totalHoursTruncated > 0)
                durationStringBuilder.Append(duration.Minutes + "m ");

            durationStringBuilder.Append(duration.Seconds + "." + duration.Milliseconds + "s");

            return durationStringBuilder.ToString();
        }

        public static bool HasUtf8ByteOrderMark(string srcFile)
        {
            using (FileStream readStream = File.Open(srcFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return HasUtf8ByteOrderMark(readStream);
            }
        }

        public static bool HasUtf8ByteOrderMark(Stream inputStream)
        {
            int b1 = inputStream.ReadByte();
            int b2 = inputStream.ReadByte();
            int b3 = inputStream.ReadByte();
            inputStream.Seek(0, SeekOrigin.Begin);

            return (0xEF == b1 && 0xBB == b2 && 0xBF == b3);
        }

        public static bool IsGitInstalled => GitUtil.IsGitInstalled;

        public static bool ValidateRegex(string pattern)
        {
            try
            {
                Regex regex = new Regex(pattern);
                return true;
            }
            catch
            {
                return false;
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

    public class NumberedString
    {
        public int FirstLineNumber;
        public int LineCount;
        public string Text;
    }

    public static class TextReaderEx
    {
        public static string TrimEndOfLine(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (text.EndsWith("\r\n"))
                return text.Substring(0, text.Length - 2);
            else if (text.EndsWith("\r"))
                return text.Substring(0, text.Length - 1);
            else if (text.EndsWith("\n"))
                return text.Substring(0, text.Length - 1);
            else
                return text;
        }
    }
}
