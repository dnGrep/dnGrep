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

        public static List<string> GetGitignoreDirectories(string path, bool recursive, bool followSymlinks,
            PauseCancelToken pauseCancelToken)
        {
            if (File.Exists(Path.Combine(path, ".gitignore")))
                return [path];

            var fileOptions = baseFileOptions;
            if (recursive)
                fileOptions |= DirectoryEnumerationOptions.Recursive;
            if (followSymlinks)
                fileOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

            List<string> dontRecurseBelow = [@"C:\$Recycle.Bin"];
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
                return [];
            }
        }

        public static IEnumerable<string> EnumerateFiles(string path, List<string> patterns,
            List<Regex>? excludePatterns, Gitignore? gitignore, FileFilter filter,
            PauseCancelToken pauseCancelToken)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return [];

            bool simpleSearch = filter.IncludeHidden && filter.MaxSubfolderDepth == -1 &&
                (excludePatterns == null || excludePatterns.Count == 0) &&
                (gitignore == null || gitignore.IsEmpty) &&
                string.IsNullOrWhiteSpace(filter.NamePatternToExclude);

            if (simpleSearch)
                return EnumerateAllFiles(path, patterns, filter.IncludeArchive, filter.IncludeSubfolders, filter.FollowSymlinks, pauseCancelToken);
            else
                return EnumerateFilesWithFilters(path, patterns, excludePatterns, gitignore, filter, pauseCancelToken);
        }

        private static IEnumerable<string> EnumerateAllFiles(string path, List<string> patterns,
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
                        if (ArchiveDirectory.ExtensionsSet.Contains(Path.GetExtension(fsei.FileName).ToLower(CultureInfo.CurrentCulture)))
                        {
                            return true;
                        }
                    }

                    return false;
                };
            }

            return DirectoryEx.EnumerateFiles(path, fileOptions, fileFilters);
        }

        private static IEnumerable<string> EnumerateFilesWithFilters(string path, List<string> patterns,
            List<Regex>? excludePatterns, Gitignore? gitignore, FileFilter filter,
            PauseCancelToken pauseCancelToken)
        {
            DirectoryInfo searchRoot = new(path);

            int startDepth = 0;
            if (filter.MaxSubfolderDepth > 0)
                startDepth = GetDepth(searchRoot);

            var fileOptions = baseFileOptions;
            if (filter.IncludeSubfolders)
                fileOptions |= DirectoryEnumerationOptions.Recursive;
            if (filter.FollowSymlinks)
                fileOptions &= ~DirectoryEnumerationOptions.SkipReparsePoints;

            bool includeAllFiles = patterns.Count == 0 ||
                (patterns.Count == 1 && (patterns[0] == "*.*" || patterns[0] == "*"));

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
                    if (gitignore != null && gitignore.Directories.Contains(fsei.FullPath))
                    {
                        return false;
                    }

                    if (!filter.IncludeHidden && fsei.IsHidden)
                    {
                        // the root of the drive has the hidden attribute set, so don't stop on this hidden directory
                        if (fsei.FullPath.Length < 4)
                        {
                            string? pathRoot = Path.GetPathRoot(fsei.FullPath);
                            if (pathRoot != null && !pathRoot.Equals(fsei.FullPath, StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
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
                            // do not apply the 'no extension' regex to directory paths
                            if (!regex.ToString().Equals(Utils.NoExtensionPattern, StringComparison.Ordinal) &&
                                !regex.ToString().Equals(Utils.DotFilesPattern, StringComparison.Ordinal))
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
                    }

                    return true;
                },

                InclusionFilter = fsei =>
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

                    if (includeAllFiles)
                    {
                        return true;
                    }
                    else
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
                    }

                    if (filter.IncludeArchive)
                    {
                        if (ArchiveDirectory.ExtensionsSet.Contains(Path.GetExtension(fsei.FileName).ToLower(CultureInfo.CurrentCulture)))
                        {
                            return true;
                        }
                    }

                    return false;
                },
            };

            // apply the directory filter to the initial start path
            if (!fileFilters.RecursionFilter(new FileSystemEntryInfo(path)))
            {
                return [];
            }

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
        /// https://devblogs.microsoft.com/oldnewthing/20071217-00/?p=24143
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
