using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
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

        public static IEnumerable<string> EnumerateDirectories(string path, string pattern, SearchOption option)
        {
            return EnumerateDirectories(new DirectoryInfo(path), pattern, option);
        }

        public static IEnumerable<string> EnumerateDirectories(DirectoryInfo root, string pattern, SearchOption option)
        {
            if (root == null || !root.Exists) yield break;

            IEnumerable<DirectoryInfo> matches = root.EnumerateDirectories(pattern, SearchOption.TopDirectoryOnly);

            using (var enumerator = matches.GetEnumerator())
            {
                bool next = true;

                while (next)
                {
                    try
                    {
                        next = enumerator.MoveNext();
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        logger.Warn(ex, $@"Unable to access '{root.FullName}'. Skipping...");
                        continue;
                    }
                    catch (IOException ex)
                    {
                        // "The symbolic link cannot be followed because its type is disabled."
                        // "The specified network name is no longer available."
                        logger.Warn(ex, $@"Could not process path (check SymlinkEvaluation rules) '{root.Parent.FullName}\{root.Name}'.");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $@"Could not process path '{root.Parent.FullName}\{root.Name}'.");
                        continue;
                    }

                    if (next)
                    {
                        yield return enumerator.Current.FullName;
                    }
                }
            }

            if (option == SearchOption.AllDirectories)
            {
                var rootMatches = root.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
                using (var enumerator = rootMatches.GetEnumerator())
                {
                    bool next = true;

                    while (next)
                    {
                        try
                        {
                            next = enumerator.MoveNext();
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            logger.Warn(ex, $@"Unable to access '{root.FullName}'. Skipping...");
                        }
                        catch (IOException ex)
                        {
                            // "The symbolic link cannot be followed because its type is disabled."
                            // "The specified network name is no longer available."
                            logger.Warn(ex, $@"Could not process path (check SymlinkEvaluation rules) '{root.Parent.FullName}\{root.Name}'.");
                            continue;
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $@"Could not process path '{root.Parent.FullName}\{root.Name}'.");
                            continue;
                        }

                        if (next)
                        {
                            foreach (var match in EnumerateDirectories(enumerator.Current, pattern, option))
                            {
                                yield return match;
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<string> EnumerateFiles(string path, IEnumerable<string> patterns)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) yield break;

            IEnumerable<string> matches = Enumerable.Empty<string>();
            if (patterns.Any())
            {
                foreach (var pattern in patterns)
                    matches = matches.Concat(Directory.EnumerateFiles(path, pattern));
            }
            else
            {
                matches = matches.Concat(Directory.EnumerateFiles(path));
            }

            using (var enumerator = matches.GetEnumerator())
            {
                bool next = true;

                while (next)
                {
                    try
                    {
                        next = enumerator.MoveNext();
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        logger.Warn(ex, $@"Unable to access '{path}'. Skipping...");
                        yield break;
                    }
                    catch (IOException ex)
                    {
                        // "The symbolic link cannot be followed because its type is disabled."
                        // "The specified network name is no longer available."
                        logger.Warn(ex, $@"Could not process path (check SymlinkEvaluation rules)'{path} '.");
                        yield break;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $@"Could not process path '{path}'.");
                        yield break;
                    }

                    if (next)
                    {
                        yield return enumerator.Current;
                    }
                }
            }
        }
    }
}
