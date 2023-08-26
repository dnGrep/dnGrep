using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using dnGREP.Common.IO;
using NLog;

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

        public static IList<string> GetGitignoreDirectories(string path, bool recursive, bool followSymlinks,
            PauseCancelToken pauseCancelToken)
        {
            if (File.Exists(Path.Combine(path, ".gitignore")))
                return new List<string> { path };

            var fileOptions = baseFileOptions;
            if (recursive)
                fileOptions |= DirectoryEnumerationOptions.Recursive;
            if (followSymlinks)
                fileOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

            List<string> dontRecurseBelow = new()
            {
                @"C:\$Recycle.Bin"
            };
            foreach (var sf in new[]
                {
                    Environment.SpecialFolder.Windows,
                    Environment.SpecialFolder.ProgramFiles,
                    Environment.SpecialFolder.ProgramFilesX86,
                })
            {
                string p = Environment.GetFolderPath(sf);
                if (!string.IsNullOrEmpty(p))
                    dontRecurseBelow.Add(p);
            }

            DirectoryEnumerationFilters fileFilters = new()
            {
                PauseCancelToken = pauseCancelToken,
                ErrorFilter = (errorCode, errorMessage, pathProcessed) =>
                {
                    logger.Error($"Find file error {errorCode}: {errorMessage} on {pathProcessed}");
                    return true;
                },
                RecursionFilter = fsei =>
                {
                    if (fsei.IsDirectory && dontRecurseBelow.Any(p => fsei.FullPath.StartsWith(p, true, CultureInfo.CurrentCulture)))
                        return false;
                    return true;
                },
                InclusionFilter = fsei =>
                {
                    if (fsei.FileName == ".gitignore")
                    {
                        var dir = Path.GetDirectoryName(fsei.FullPath);
                        if (!string.IsNullOrEmpty(dir))
                        {
                            dontRecurseBelow.Add(dir);
                        }

                        return true;
                    }
                    return false;
                }
            };

            try
            {
                // search down subdirectories
                var list = DirectoryEx.EnumerateFiles(path, fileOptions, fileFilters)
                    .Select(s => Path.GetDirectoryName(s) ?? string.Empty).ToList();

                if (list.Count == 0)
                {
                    // not found, search up the tree
                    DirectoryInfo di = new(path);
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
            catch (OperationCanceledException)
            {
                return new List<string>();
            }
        }

        public static IEnumerable<string> EnumerateFiles(string path, IList<string> patterns,
            IList<Regex>? excludePatterns, Gitignore? gitignore, FileFilter filter,
            PauseCancelToken pauseCancelToken)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return Enumerable.Empty<string>();

            bool simpleSearch = filter.IncludeHidden && filter.MaxSubfolderDepth == -1 &&
                (excludePatterns == null || excludePatterns.Count == 0) &&
                (gitignore == null || gitignore.IsEmpty) &&
                string.IsNullOrWhiteSpace(filter.NamePatternToExclude);

            if (simpleSearch)
                return EnumerateAllFiles(path, patterns, filter.IncludeArchive, filter.IncludeSubfolders, filter.FollowSymlinks, pauseCancelToken);
            else
                return EnumerateFilesWithFilters(path, patterns, excludePatterns, gitignore, filter, pauseCancelToken);
        }

        private static IEnumerable<string> EnumerateAllFiles(string path, IList<string> patterns,
            bool includeArchive, bool recursive, bool followSymlinks,
            PauseCancelToken pauseCancelToken)
        {
            // without filters, just enumerate files, which is faster

            var fileOptions = baseFileOptions;
            if (recursive)
                fileOptions |= DirectoryEnumerationOptions.Recursive;
            if (followSymlinks)
                fileOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

            DirectoryEnumerationFilters fileFilters = new()
            {
                PauseCancelToken = pauseCancelToken,
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
                        if (pattern.Contains('*', StringComparison.Ordinal) || pattern.Contains('?', StringComparison.Ordinal))
                        {
                            if (WildcardMatch(fsei.FileName, pattern, true))
                                return true;
                        }
                        else
                        {
                            if (fsei.FileName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
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

            return DirectoryEx.EnumerateFiles(path, fileOptions, fileFilters);
        }

        private static IEnumerable<string> EnumerateFilesWithFilters(string path, IList<string> patterns,
            IList<Regex>? excludePatterns, Gitignore? gitignore, FileFilter filter,
            PauseCancelToken pauseCancelToken)
        {
            DirectoryInfo di = new(path);
            // the root of the drive has the hidden attribute set, so don't stop on this hidden directory
            if (di.Attributes.HasFlag(FileAttributes.Hidden) && (di.Root != di))
                yield break;

            int startDepth = 0;
            if (filter.MaxSubfolderDepth > 0)
                startDepth = GetDepth(di);

            IEnumerable<string> directories = new string[] { path };
            if (filter.IncludeSubfolders)
                directories = directories.Concat(EnumerateDirectoriesImpl(path, filter, startDepth, excludePatterns, gitignore, pauseCancelToken));

            foreach (var directory in directories)
            {
                IEnumerable<string> matches = EnumerateFilesImpl(directory, patterns, filter, excludePatterns, gitignore, pauseCancelToken);

                foreach (var file in matches)
                    yield return file;
            }
        }

        private static IEnumerable<string> EnumerateDirectoriesImpl(string path,
            FileFilter filter, int startDepth, IList<Regex>? excludePatterns, Gitignore? gitignore,
            PauseCancelToken pauseCancelToken)
        {
            var dirOptions = baseDirOptions;
            if (filter.IncludeSubfolders)
                dirOptions |= DirectoryEnumerationOptions.Recursive;
            if (filter.FollowSymlinks)
                dirOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

            DirectoryEnumerationFilters dirFilters = new()
            {
                PauseCancelToken = pauseCancelToken,
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

                    if (excludePatterns?.Count > 0)
                    {
                        // name\* (in wildcard) and name\\.*$ (in regex) is the canonical pattern
                        // for excluding a directory. Unfortunately, it doesn't work with directory
                        // paths with the trailing backslash

                        foreach (Regex regex in excludePatterns)
                        {
                            if (regex.IsMatch(fsei.FullPath + Path.DirectorySeparatorChar))
                            {
                                return false;
                            }
                            if (regex.IsMatch(fsei.FullPath))
                            {
                                return false;
                            }
                        }
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

                    if (excludePatterns?.Count > 0)
                    {
                        foreach (Regex regex in excludePatterns)
                        {
                            if (regex.IsMatch(fsei.FullPath + Path.DirectorySeparatorChar))
                            {
                                return false;
                            }
                            if (regex.IsMatch(fsei.FullPath))
                            {
                                return false;
                            }
                        }
                    }

                    if (filter.UseGitIgnore && fsei.FileName == ".git")
                    {
                        return false;
                    }

                    return true;
                },
            };

            return DirectoryEx.EnumerateDirectories(path, dirOptions, dirFilters);
        }

        private static IEnumerable<string> EnumerateFilesImpl(string path, IList<string> patterns,
            FileFilter filter, IList<Regex>? excludePatterns, Gitignore? gitignore,
            PauseCancelToken pauseCancelToken)
        {
            DirectoryEnumerationFilters fileFilters = new()
            {
                PauseCancelToken = pauseCancelToken,
                ErrorFilter = (errorCode, errorMessage, pathProcessed) =>
                {
                    logger.Error($"Find file error {errorCode}: {errorMessage} on {pathProcessed}");
                    return true;
                }
            };


            bool includeAllFiles = (patterns.Count == 0 ||
                (patterns.Count == 1 && (patterns[0] == "*.*" || patterns[0] == "*"))) &&
                (excludePatterns == null || excludePatterns.Count == 0) &&
                (gitignore == null || gitignore.Files.Count == 0);

            if (includeAllFiles)
            {
                fileFilters.InclusionFilter = fsei =>
                {
                    if (!filter.IncludeHidden && fsei.IsHidden)
                    {
                        return false;
                    }

                    return true;
                };
            }
            else
            {
                fileFilters.InclusionFilter = fsei =>
                {
                    if (!filter.IncludeHidden && fsei.IsHidden)
                    {
                        return false;
                    }

                    if (gitignore != null && gitignore.Files.Contains(fsei.FullPath))
                    {
                        return false;
                    }

                    if (excludePatterns?.Count > 0)
                    {
                        foreach (Regex regex in excludePatterns)
                        {
                            if (regex.IsMatch(fsei.FullPath))
                            {
                                return false;
                            }
                        }
                    }

                    if (patterns.Count > 0)
                    {
                        foreach (string pattern in patterns)
                        {
                            if (WildcardMatch(fsei.FileName, pattern, true))
                                return true;
                        }
                    }
                    else
                    {
                        return true;
                    }

                    if (filter.IncludeArchive)
                    {
                        foreach (string pattern in ArchiveDirectory.Patterns)
                        {
                            if (WildcardMatch(fsei.FileName, pattern, true))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                };
            }

            var fileOptions = baseFileOptions;
            if (filter.FollowSymlinks)
                fileOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

            return DirectoryEx.EnumerateFiles(path, fileOptions, fileFilters);
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
            if (string.IsNullOrEmpty(pattern))
                return fileName.Length == 0;

            if (pattern == "*" || pattern == "*.*")
                return !string.IsNullOrWhiteSpace(fileName);

            if (pattern == "*.")
                return string.IsNullOrWhiteSpace(Path.GetExtension(fileName));

            if (pattern == ".*")
                return fileName.StartsWith(dot, StringComparison.OrdinalIgnoreCase);

            if (pattern.StartsWith(star, StringComparison.OrdinalIgnoreCase) && pattern.IndexOf('*', 1) == -1 && !pattern.Contains('?', StringComparison.Ordinal))
                return fileName.EndsWith(pattern[1..], StringComparison.CurrentCulture);

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
