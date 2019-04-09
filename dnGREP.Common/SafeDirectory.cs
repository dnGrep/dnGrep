using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Alphaleonis.Win32.Filesystem;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Common
{
    public static class SafeDirectory
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly DirectoryEnumerationOptions baseDirOptions =
            DirectoryEnumerationOptions.Folders |
            DirectoryEnumerationOptions.SkipReparsePoints |
            DirectoryEnumerationOptions.BasicSearch |
            DirectoryEnumerationOptions.LargeCache;

        private static readonly DirectoryEnumerationOptions baseFileOptions =
            DirectoryEnumerationOptions.Files |
            DirectoryEnumerationOptions.SkipReparsePoints |
            DirectoryEnumerationOptions.BasicSearch |
            DirectoryEnumerationOptions.LargeCache;


        public static IEnumerable<string> EnumerateFiles(string path, IList<string> patterns, bool includeHidden, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return Enumerable.Empty<string>();

            if (includeHidden)
                return EnumerateFilesIncludeHidden(path, patterns, recursive);
            else
                return EnumerateFilesExcludeHidden(path, patterns, recursive);
        }

        private static IEnumerable<string> EnumerateFilesIncludeHidden(string path, IList<string> patterns, bool recursive)
        {
            // when not checking for hidden directories or files, just enumerate files, which is faster

            var fileOptions = baseFileOptions;
            if (recursive)
                fileOptions |= DirectoryEnumerationOptions.Recursive;

            DirectoryEnumerationFilters fileFilters = new DirectoryEnumerationFilters
            {
                ErrorFilter = (errorCode, errorMessage, pathProcessed) =>
                {
                    logger.Error($"Find file error {errorCode}: {errorMessage} on {pathProcessed}");
                    return true;
                }
            };


            bool includeAllFiles = patterns.Count == 0 ||
                (patterns.Count == 1 && (patterns[0] == "*.*" || patterns[0] == "*"));

            if (!includeAllFiles)
            {
                fileFilters.InclusionFilter = fsei =>
                {
                    foreach (string pattern in patterns)
                    {
                        if (WildcardMatch(fsei.FileName, pattern, true))
                            return true;
                        else if (pattern == "*.doc" && WildcardMatch(fsei.FileName, "*.doc*", true))
                            return true;
                        else if (pattern == "*.xls" && WildcardMatch(fsei.FileName, "*.xls*", true))
                            return true;
                    }
                    return false;
                };
            }

            return Directory.EnumerateFiles(path, fileOptions, fileFilters, PathFormat.FullPath);
        }

        private static IEnumerable<string> EnumerateFilesExcludeHidden(string path, IList<string> patterns, bool recursive)
        {
            // when checking for hidden directories, enumerate the directories separately from files to check for hidden flag on directories

            DirectoryInfo di = new DirectoryInfo(path);
            // the root of the drive has the hidden attribute set, so don't stop on this hidden directory
            if (di.Attributes.HasFlag(FileAttributes.Hidden) && (di.Root != di))
                yield break;

            var dirOptions = baseDirOptions;
            if (recursive)
                dirOptions |= DirectoryEnumerationOptions.Recursive;


            DirectoryEnumerationFilters dirFilters = new DirectoryEnumerationFilters
            {
                ErrorFilter = (errorCode, errorMessage, pathProcessed) =>
                {
                    logger.Error($"Find file error {errorCode}: {errorMessage} on {pathProcessed}");
                    return true;
                }
            };

            dirFilters.InclusionFilter = fsei =>
            {
                return !fsei.IsHidden;
            };


            DirectoryEnumerationFilters fileFilters = new DirectoryEnumerationFilters
            {
                ErrorFilter = (errorCode, errorMessage, pathProcessed) =>
                {
                    logger.Error($"Find file error {errorCode}: {errorMessage} on {pathProcessed}");
                    return true;
                }
            };


            bool includeAllFiles = patterns.Count == 0 ||
                (patterns.Count == 1 && (patterns[0] == "*.*" || patterns[0] == "*"));

            if (includeAllFiles)
            {
                fileFilters.InclusionFilter = fsei =>
                {
                    return !fsei.IsHidden;
                };
            }
            else
            {
                fileFilters.InclusionFilter = fsei =>
                {
                    if (fsei.IsHidden)
                        return false;

                    foreach (string pattern in patterns)
                    {
                        if (WildcardMatch(fsei.FileName, pattern, true))
                            return true;
                        else if (pattern == "*.doc" && WildcardMatch(fsei.FileName, "*.doc*", true))
                            return true;
                        else if (pattern == "*.xls" && WildcardMatch(fsei.FileName, "*.xls*", true))
                            return true;
                    }
                    return false;
                };
            }

            IEnumerable<string> directories = new string[] { path };
            if (recursive)
                directories = directories.Concat(Directory.EnumerateDirectories(path, dirOptions, dirFilters, PathFormat.FullPath));

            foreach (var directory in directories)
            {
                IEnumerable<string> matches = Directory.EnumerateFiles(directory, baseFileOptions, fileFilters, PathFormat.FullPath);

                foreach (var file in matches)
                    yield return file;
            }
        }

        /// <summary>
        /// Tests if a file name matches a Windows file pattern
        /// </summary>
        /// <param name="fileName">the file name to test</param>
        /// <param name="pattern">the Windows file patten</param>
        /// <param name="ignoreCase">true for case-insensitive</param>
        /// <returns>True if match, otherwise false</returns>
        public static bool WildcardMatch(string fileName, string pattern, bool ignoreCase)
        {
            if (ignoreCase)
                return WildcardMatch(fileName.ToLower(), pattern.ToLower());
            else
                return WildcardMatch(fileName, pattern);
        }

        /// <summary>
        /// Tests if a file name matches a Windows file pattern
        /// </summary>
        /// <remarks>
        /// Not an exact duplicate of the internal Windows implementation, but most common patterns are supported
        /// https://blogs.msdn.microsoft.com/jeremykuhne/2017/06/04/wildcards-in-windows/
        /// https://blogs.msdn.microsoft.com/oldnewthing/20071217-00/?p=24143/
        /// </remarks>
        /// <param name="fileName">the file name to test</param>
        /// <param name="pattern">the Windows file patten</param>
        /// <returns>True if match, otherwise false</returns>
        public static bool WildcardMatch(string fileName, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return fileName.Length == 0;

            if (pattern == "*" || pattern == "*.*")
                return !string.IsNullOrWhiteSpace(fileName);

            if (pattern == "*.")
                return string.IsNullOrWhiteSpace(Path.GetExtension(fileName));

            if (pattern == ".*")
                return fileName.StartsWith(".");

            if (pattern.StartsWith("*") && pattern.IndexOf('*', 1) == -1 && pattern.IndexOf('?') == -1)
                return fileName.EndsWith(pattern.Substring(1));

            int fileNameIndex = 0;
            int patternIndex = 0;
            int fileNameLength = fileName.Length;
            int patternLength = pattern.Length;

            while (fileNameIndex < fileNameLength && patternIndex < patternLength && pattern[patternIndex] != '*')
            {
                char wild = pattern[patternIndex];
                if (wild != '?' && wild != fileName[fileNameIndex])
                    return false;
                patternIndex++;
                fileNameIndex++;
            }

            int fileNameIndex2 = 0;
            int patternIndex2 = 0;
            while (fileNameIndex < fileNameLength && patternIndex < patternLength)
            {
                char wild = pattern[patternIndex];
                if (wild == '*')
                {
                    patternIndex++;
                    if (patternIndex == patternLength)
                        return true;
                    patternIndex2 = patternIndex;
                    fileNameIndex2 = fileNameIndex + 1;
                }
                else if (wild == '?' || wild == fileName[fileNameIndex])
                {
                    patternIndex++;
                    fileNameIndex++;
                }
                else
                {
                    patternIndex = patternIndex2;
                    fileNameIndex = fileNameIndex2;
                    fileNameIndex2++;
                }
            }
            while (patternIndex < patternLength && pattern[patternIndex] == '*')
                patternIndex++;
            return patternIndex == patternLength && fileNameIndex == fileNameLength;
        }
    }
}
