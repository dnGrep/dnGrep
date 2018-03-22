using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;

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

            IEnumerable<DirectoryInfo> matches = Enumerable.Empty<DirectoryInfo>();
            try
            {
                matches = matches.Concat(root.EnumerateDirectories(pattern, SearchOption.TopDirectoryOnly));
            }
            catch (UnauthorizedAccessException)
            {
                logger.Warn(string.Format(@"Unable to access '{0}'. Skipping...", root.FullName));
                yield break;
            }
            catch (PathTooLongException ptle)
            {
                logger.Warn(ptle, string.Format(@"Could not process path '{0}\{1}'.", root.Parent.FullName, root.Name));
                yield break;
            }
            catch (IOException ioe)
            {
                // "The symbolic link cannot be followed because its type is disabled."
                // "The specified network name is no longer available."
                logger.Warn(ioe, string.Format(@"Could not process path (check SymlinkEvaluation rules) '{0}\{1}'.", root.Parent.FullName, root.Name));
                yield break;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, string.Format(@"Could not process path '{0}\{1}'.", root.Parent.FullName, root.Name));
                yield break;
            }


            foreach (var dir in matches)
            {
                yield return dir.FullName;
            }

            if (option == SearchOption.AllDirectories)
            {
                foreach (var subdir in root.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    foreach (var match in EnumerateDirectories(subdir, pattern, option))
                    {
                        yield return match;
                    }
                }
            }
        }

        public static IEnumerable<string> EnumerateFiles(string path, IEnumerable<string> patterns)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) yield break;

            IEnumerable<string> matches = Enumerable.Empty<string>();
            try
            {
                if (patterns.Any())
                {
                    foreach (var pattern in patterns)
                        matches = matches.Concat(Directory.EnumerateFiles(path, pattern));
                }
                else
                {
                    matches = matches.Concat(Directory.EnumerateFiles(path));
                }
            }
            catch (UnauthorizedAccessException)
            {
                logger.Warn(string.Format(@"Unable to access '{0}'. Skipping...", path));
                yield break;
            }
            catch (PathTooLongException ptle)
            {
                logger.Warn(ptle, string.Format(@"Could not process file in path '{0}'.", path));
                yield break;
            }
            catch (IOException ioe)
            {
                // "The symbolic link cannot be followed because its type is disabled."
                // "The specified network name is no longer available."
                logger.Warn(ioe, string.Format(@"Could not process path (check SymlinkEvaluation rules)'{0} '.", path));
                yield break;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, string.Format(@"Could not process path '{0}'.", path));
                yield break;
            }


            foreach (var file in matches)
            {
                yield return file;
            }

        }
    }
}
