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
            if (di.Attributes.HasFlag(FileAttributes.Hidden))
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

        public static bool WildcardMatch(string str, string pattern, bool ignoreCase)
        {
            if (ignoreCase)
                return WildcardMatch(str.ToLower(), pattern.ToLower());
            else
                return WildcardMatch(str, pattern);
        }

        public static bool WildcardMatch(string str, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return str.Length == 0;

            if (pattern == "*" || pattern == "*.*")
                return !string.IsNullOrWhiteSpace(str);

            if (pattern == "*.")
                return string.IsNullOrWhiteSpace(Path.GetExtension(str));

            if (pattern == ".*")
                return str.StartsWith(".");

            if (pattern.StartsWith("*") && pattern.IndexOf('*', 1) == -1 && pattern.IndexOf('?') == -1)
                return str.EndsWith(pattern.Substring(1));

            int pS = 0;
            int pW = 0;
            int lS = str.Length;
            int lW = pattern.Length;

            while (pS < lS && pW < lW && pattern[pW] != '*')
            {
                char wild = pattern[pW];
                if (wild != '?' && wild != str[pS])
                    return false;
                pW++;
                pS++;
            }

            int pSm = 0;
            int pWm = 0;
            while (pS < lS && pW < lW)
            {
                char wild = pattern[pW];
                if (wild == '*')
                {
                    pW++;
                    if (pW == lW)
                        return true;
                    pWm = pW;
                    pSm = pS + 1;
                }
                else if (wild == '?' || wild == str[pS])
                {
                    pW++;
                    pS++;
                }
                else
                {
                    pW = pWm;
                    pS = pSm;
                    pSm++;
                }
            }
            while (pW < lW && pattern[pW] == '*')
                pW++;
            return pW == lW && pS == lS;
        }
    }
}
