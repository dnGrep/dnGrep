using System;
using System.Collections.Generic;
using System.Globalization;
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
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const DirectoryEnumerationOptions baseDirOptions =
            DirectoryEnumerationOptions.Folders |
            DirectoryEnumerationOptions.SkipReparsePoints |
            DirectoryEnumerationOptions.BasicSearch |
            DirectoryEnumerationOptions.LargeCache;

        private const DirectoryEnumerationOptions baseFileOptions =
            DirectoryEnumerationOptions.Files |
            DirectoryEnumerationOptions.SkipReparsePoints |
            DirectoryEnumerationOptions.BasicSearch |
            DirectoryEnumerationOptions.LargeCache;
        private const string dot = ".";
        private const string star = "*";

        public static IList<string> GetGitignoreDirectories(string path, bool recursive, bool followSymlinks)
        {
            if (File.Exists(Path.Combine(path, ".gitignore")))
                return new List<string> { path };

            var fileOptions = baseFileOptions;
            if (recursive)
                fileOptions |= DirectoryEnumerationOptions.Recursive;
            if (followSymlinks)
                fileOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

            List<string> dontRecurseBelow = new List<string>();

            DirectoryEnumerationFilters fileFilters = new DirectoryEnumerationFilters
            {
                ErrorFilter = (errorCode, errorMessage, pathProcessed) =>
                {
                    logger.Error($"Find file error {errorCode}: {errorMessage} on {pathProcessed}");
                    return true;
                },
                RecursionFilter = fsei =>
                {
                    if (fsei.IsDirectory && dontRecurseBelow.Any(p => fsei.FullPath.StartsWith(p, StringComparison.CurrentCulture)))
                        return false;
                    return true;
                },
                InclusionFilter = fsei =>
                {
                    if (fsei.FileName == ".gitignore")
                    {
                        dontRecurseBelow.Add(Path.GetDirectoryName(fsei.FullPath));
                        return true;
                    }
                    return false;
                }
            };

            var list = Directory.EnumerateFiles(path, fileOptions, fileFilters, PathFormat.FullPath)
                .Select(s => Path.GetDirectoryName(s)).ToList();

            if (list.Count == 0)
            {
                DirectoryInfo di = new DirectoryInfo(path);
                while (di.Parent != null)
                {
                    if (File.Exists(Path.Combine(di.Parent.FullName, ".gitignore")))
                    {
                        list.Add(path);
                        break;
                    }

                    di = di.Parent;
                }
            }

            return list;
        }

        public static IEnumerable<string> EnumerateFiles(string path, IList<string> patterns,
            Gitignore gitignore, FileFilter filter)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return Enumerable.Empty<string>();

            if (patterns == null)
            {
                throw new ArgumentNullException(nameof(patterns));
            }
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            bool simpleSearch = filter.IncludeHidden && filter.MaxSubfolderDepth == -1 &&
                (gitignore == null || gitignore.IsEmpty) &&
                string.IsNullOrWhiteSpace(filter.NamePatternToExclude);

            if (simpleSearch)
                return EnumerateAllFiles(path, patterns, filter.IncludeArchive, filter.IncludeSubfolders, filter.FollowSymlinks);
            else
                return EnumerateFilesWithFilters(path, patterns, gitignore, filter);
        }

        private static IEnumerable<string> EnumerateAllFiles(string path, IList<string> patterns, bool includeArchive, bool recursive, bool followSymlinks)
        {
            // without filters, just enumerate files, which is faster

            var fileOptions = baseFileOptions;
            if (recursive)
                fileOptions |= DirectoryEnumerationOptions.Recursive;
            if (followSymlinks)
                fileOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

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

                    if (includeArchive)
                    {
                        foreach (string pattern in ArchiveDirectory.Patterns)
                        {
                            if (WildcardMatch(fsei.FileName, pattern, true))
                                return true;
                        }
                    }

                    return false;
                };
            }

            return Directory.EnumerateFiles(path, fileOptions, fileFilters, PathFormat.FullPath);
        }

        private static IEnumerable<string> EnumerateFilesWithFilters(string path, IList<string> patterns,
            Gitignore gitignore, FileFilter filter)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            // the root of the drive has the hidden attribute set, so don't stop on this hidden directory
            if (di.Attributes.HasFlag(FileAttributes.Hidden) && (di.Root != di))
                yield break;

            int startDepth = 0;
            if (filter.MaxSubfolderDepth > 0)
                startDepth = GetDepth(di);

            IEnumerable<string> directories = new string[] { path };
            if (filter.IncludeSubfolders)
                directories = directories.Concat(EnumerateDirectoriesImpl(path, filter, startDepth, gitignore));

            foreach (var directory in directories)
            {
                IEnumerable<string> matches = EnumerateFilesImpl(directory, patterns, filter, gitignore);

                foreach (var file in matches)
                    yield return file;
            }
        }

        private static IEnumerable<string> EnumerateDirectoriesImpl(string path,
            FileFilter filter, int startDepth, Gitignore gitignore)
        {
            var dirOptions = baseDirOptions;
            if (filter.IncludeSubfolders)
                dirOptions |= DirectoryEnumerationOptions.Recursive;
            if (filter.FollowSymlinks)
                dirOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

            DirectoryEnumerationFilters dirFilters = new DirectoryEnumerationFilters
            {
                ErrorFilter = (errorCode, errorMessage, pathProcessed) =>
                {
                    logger.Error($"Find file error {errorCode}: {errorMessage} on {pathProcessed}");
                    return true;
                },

                RecursionFilter = fsei =>
                {
                    if (gitignore != null && gitignore.Directories.Contains(fsei.FullPath))
                    {
                        return false;
                    }

                    if (!filter.IncludeHidden && fsei.IsHidden)
                    {
                        return false;
                    }

                    if (filter.MaxSubfolderDepth >= 0)
                    {
                        int depth = GetDepth(new DirectoryInfo(fsei.FullPath));
                        if (depth - startDepth > filter.MaxSubfolderDepth)
                            return false;
                    }

                    if (filter.UseGitIgnore && fsei.FileName == ".git")
                    {
                        return false;
                    }

                    return true;
                },

                InclusionFilter = fsei =>
                {
                    if (gitignore != null && gitignore.Directories.Contains(fsei.FullPath))
                    {
                        return false;
                    }

                    if (!filter.IncludeHidden && fsei.IsHidden)
                    {
                        return false;
                    }

                    if (filter.MaxSubfolderDepth >= 0)
                    {
                        int depth = GetDepth(new DirectoryInfo(fsei.FullPath));
                        if (depth - startDepth > filter.MaxSubfolderDepth)
                            return false;
                    }

                    if (filter.UseGitIgnore && fsei.FileName == ".git")
                    {
                        return false;
                    }

                    return true;
                },
            };

            return Directory.EnumerateDirectories(path, dirOptions, dirFilters, PathFormat.FullPath);
        }

        private static IEnumerable<string> EnumerateFilesImpl(string path, IList<string> patterns,
            FileFilter filter, Gitignore gitignore)
        {
            DirectoryEnumerationFilters fileFilters = new DirectoryEnumerationFilters
            {
                ErrorFilter = (errorCode, errorMessage, pathProcessed) =>
                {
                    logger.Error($"Find file error {errorCode}: {errorMessage} on {pathProcessed}");
                    return true;
                }
            };


            bool includeAllFiles = (patterns.Count == 0 ||
                (patterns.Count == 1 && (patterns[0] == "*.*" || patterns[0] == "*"))) &&
                (gitignore == null || gitignore.Files.Count == 0);

            if (includeAllFiles)
            {
                fileFilters.InclusionFilter = fsei =>
                {
                    if (!filter.IncludeHidden && fsei.IsHidden)
                        return false;

                    return true;
                };
            }
            else
            {
                fileFilters.InclusionFilter = fsei =>
                {
                    if (!filter.IncludeHidden && fsei.IsHidden)
                        return false;

                    if (gitignore != null && gitignore.Files.Contains(fsei.FullPath))
                    {
                        return false;
                    }

                    foreach (string pattern in patterns)
                    {
                        if (WildcardMatch(fsei.FileName, pattern, true))
                            return true;
                    }

                    if (filter.IncludeArchive)
                    {
                        foreach (string pattern in ArchiveDirectory.Patterns)
                        {
                            if (WildcardMatch(fsei.FileName, pattern, true))
                                return true;
                        }
                    }
                    return false;
                };
            }

            var fileOptions = baseFileOptions;
            if (filter.FollowSymlinks)
                fileOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

            return Directory.EnumerateFiles(path, fileOptions, fileFilters, PathFormat.FullPath);
        }

        private static int GetDepth(DirectoryInfo di)
        {
            int depth = 0;
            var parent = di.Parent;
            while (parent != null)
            {
                depth++;
                parent = parent.Parent;
            }
            return depth;
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
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (ignoreCase)
                return WildcardMatch(fileName.ToLower(CultureInfo.CurrentCulture), pattern.ToLower(CultureInfo.CurrentCulture));
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
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (string.IsNullOrEmpty(pattern))
                return fileName.Length == 0;

            if (pattern == "*" || pattern == "*.*")
                return !string.IsNullOrWhiteSpace(fileName);

            if (pattern == "*.")
                return string.IsNullOrWhiteSpace(Path.GetExtension(fileName));

            if (pattern == ".*")
                return fileName.StartsWith(dot, StringComparison.OrdinalIgnoreCase);

            if (pattern.StartsWith(star, StringComparison.OrdinalIgnoreCase) && pattern.IndexOf('*', 1) == -1 && pattern.IndexOf('?') == -1)
                return fileName.EndsWith(pattern.Substring(1), StringComparison.CurrentCulture);

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
