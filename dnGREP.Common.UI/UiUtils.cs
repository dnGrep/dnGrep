using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Everything;
using TextFieldParser = Microsoft.VisualBasic.FileIO.TextFieldParser;

namespace dnGREP.Common.UI
{
    public static class UiUtils
    {
        /// <summary>
        /// Encloses the text in quotes
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Quote(string text)
        {
            return "\"" + text + "\"";
        }

        /// <summary>
        /// Assumes the path argument should be a valid path and adds leading/tailing quotes
        /// if needed so SplitPath splits it correctly
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string QuoteIfNeeded(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (path.StartsWith("\"", StringComparison.Ordinal) && path.EndsWith("\"", StringComparison.Ordinal))
            {
                return path;
            }

            var parts = SplitPath(path, true);
            if (parts.Length > 1 || parts[0] != path)
                return "\"" + path + "\"";

            return path;
        }

        /// <summary>
        /// Assumes the path argument is a valid single path 
        /// and adds leading/tailing quotes if the path contains a space
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string QuoteIfIncludesSpaces(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            // check if it is already quoted
            if (path.StartsWith("\"", StringComparison.Ordinal) || path.EndsWith("\"", StringComparison.Ordinal))
            {
                return path;
            }

            if (path.Contains(' ', StringComparison.Ordinal))
            {
                return Quote(path);
            }

            return path;
        }


        /// <summary>
        /// Attempts to remove Everything query parameters from a path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string CleanPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string trimmedPath = path.Trim('\"').Trim();
            if (Directory.Exists(trimmedPath) || File.Exists(trimmedPath))
            {
                return path;
            }

            var parts = path.Split('|');
            string newPath = string.Empty;

            foreach (string part in parts)
            {
                try
                {
                    string cleaned = part.Trim();
                    while (cleaned.Length > 2)
                    {
                        cleaned = EverythingSearch.RemovePrefixes(cleaned);

                        if (cleaned.StartsWith("\"", StringComparison.Ordinal) || cleaned.EndsWith("\"", StringComparison.Ordinal) ||
                            cleaned.StartsWith("(", StringComparison.Ordinal) || cleaned.EndsWith(")", StringComparison.Ordinal))
                        {
                            cleaned = cleaned.Trim('\"', '(', ')', ' ').Trim();
                        }

                        if (Directory.Exists(cleaned) || File.Exists(cleaned))
                        {
                            if (newPath.Length > 0)
                            {
                                newPath += ";";
                            }
                            newPath += QuoteIfNeeded(cleaned);
                            break;
                        }

                        cleaned = cleaned.Remove(cleaned.Length - 1).Trim();
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(newPath))
            {
                return newPath;
            }

            return path;
        }

        /// <summary>
        /// Test if a string (single or multi path delimited string) has a valid, common base folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool HasSingleBaseFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string[] paths = SplitPath(path, false);
            if (paths.Length == 0)
                return false;

            if (paths.Length == 1 && !string.IsNullOrWhiteSpace(GetBaseFolder(path)))
                return true;

            string commonPath = FindCommonPath(paths);
            if (!string.IsNullOrWhiteSpace(commonPath) && Directory.Exists(commonPath))
                return true;

            return false;
        }

        /// <summary>
        /// Returns base folder of one or many files or folders. 
        /// If multiple files are passed in, takes the first one.
        /// </summary>
        /// <param name="path">Path to one or many files separated by semi-colon or path to a folder</param>
        /// <returns>Base folder path or empty string if none exists</returns>
        public static string GetBaseFolder(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return string.Empty;

                string[] paths = SplitPath(path, false);
                if (paths.Length > 0)
                {
                    if (paths.Length > 1)
                    {
                        string commonPath = FindCommonPath(paths);
                        if (!string.IsNullOrWhiteSpace(commonPath) && Directory.Exists(commonPath))
                            return commonPath;
                    }

                    if (paths[0].Trim() != "" && File.Exists(paths[0]))
                        return Path.GetDirectoryName(paths[0]) ?? string.Empty;
                    else if (paths[0].Trim() != "" && Directory.Exists(paths[0]))
                        return paths[0];
                    else
                        return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Finds the common path shared by all paths in the list
        /// </summary>
        /// <param name="paths">the paths to compare</param>
        /// <returns>the common path or empty string if not found</returns>
        public static string FindCommonPath(IList<string> paths)
        {
            if (paths == null || paths.Count == 0)
                return string.Empty;

            string commonPath = string.Empty;
            List<string> separatedPath = paths
                .First(str => str.Length == paths.Max(st2 => st2.Length))
                .Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            foreach (string pathSegment in separatedPath)
            {
                if (commonPath.Length == 0 && paths.All(str => str.StartsWith(pathSegment, StringComparison.CurrentCultureIgnoreCase)))
                {
                    commonPath = pathSegment;
                }
                else if (paths.All(str => str.StartsWith(commonPath + Path.DirectorySeparatorChar + pathSegment, StringComparison.CurrentCultureIgnoreCase)))
                {
                    commonPath += Path.DirectorySeparatorChar + pathSegment;
                }
                else
                {
                    break;
                }
            }

            return commonPath;
        }

        /// <summary>
        /// Splits a list of patterns separated by ; or ,
        /// </summary>
        /// <param name="pattern">Pattern to split</param>
        /// <returns>Array of strings. If path is null, returns null. If path is empty, returns empty array.</returns>
        public static string[] SplitPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return Array.Empty<string>();

            // remove quotes
            pattern = pattern.Replace("\"", string.Empty, StringComparison.Ordinal);

            string[] parts = pattern.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Select(p => p.Trim()).ToArray();
        }

        /// <summary>
        /// Splits path into subpaths if [,;|] are found in path.
        /// If folder name contains ; or , returns as one path
        /// </summary>
        /// <param name="path">Path to split</param>
        /// <returns>Array of strings. If path is null, returns null. If path is empty, returns empty array.</returns>
        public static string[] SplitPath(string? path, bool preserveWildcards)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Array.Empty<string>();

            List<string> output = new();

            string[]? paths = new string[] { path };

            // if path contains separators, parse it
            if (path.Contains(';', StringComparison.Ordinal) || path.Contains(',', StringComparison.Ordinal) || path.Contains('|', StringComparison.Ordinal) || path.Contains('\\', StringComparison.Ordinal))
            {
                using TextReader reader = new StringReader(path);
                // using TextFieldParser take quoted strings as-is
                using TextFieldParser parser = new(reader);
                parser.HasFieldsEnclosedInQuotes = path.Contains('"', StringComparison.Ordinal);
                parser.TrimWhiteSpace = false;
                parser.SetDelimiters(",", ";", "|");
                paths = parser.ReadFields();
            }

            path = path.Replace("\"", string.Empty, StringComparison.Ordinal);

            int splitterIndex = -1;
            for (int i = 0; i < paths?.Length; i++)
            {
                string testPath = paths[i];
                splitterIndex += testPath.Length + 1;
                string splitter = splitterIndex < path.Length ? path[splitterIndex].ToString() : string.Empty;
                string testPathTrimmed = testPath.Trim();
                if (File.Exists(testPathTrimmed) || Directory.Exists(testPathTrimmed))
                {
                    output.Add(testPathTrimmed);
                }
                else
                {
                    bool found = false;
                    IList<string> subPaths = GetPathsByWildcard(testPathTrimmed);
                    if (subPaths.Count > 0)
                    {
                        if (preserveWildcards)
                            output.Add(testPathTrimmed);
                        else
                            output.AddRange(subPaths);
                        found = true;
                    }

                    if (!found)
                    {
                        // this handles folder names containing a comma or semicolon
                        StringBuilder sb = new();
                        int subSplitterIndex = 0;
                        sb.Append(testPath + splitter);
                        for (int j = i + 1; j < paths.Length; j++)
                        {
                            subSplitterIndex += paths[j].Length + 1;
                            sb.Append(paths[j]);
                            testPathTrimmed = sb.ToString().Trim();
                            if (File.Exists(testPathTrimmed) || Directory.Exists(testPathTrimmed))
                            {
                                output.Add(testPathTrimmed);
                                splitterIndex += subSplitterIndex;
                                i = j;
                                found = true;
                                break;
                            }
                            else
                            {
                                subPaths = GetPathsByWildcard(testPathTrimmed);
                                if (subPaths.Count > 0)
                                {
                                    if (preserveWildcards)
                                        output.Add(testPathTrimmed);
                                    else
                                        output.AddRange(subPaths);

                                    splitterIndex += subSplitterIndex;
                                    i = j;
                                    found = true;
                                    break;
                                }
                            }
                            sb.Append(splitterIndex + subSplitterIndex < path.Length ? path[splitterIndex + subSplitterIndex].ToString() : "");
                        }
                        if (!found && !string.IsNullOrWhiteSpace(testPath))
                        {
                            output.Add(testPath.Trim());
                        }
                    }
                }
            }
            return output.ToArray();
        }


        /// <summary>
        /// If the last path segment contains wild card chars, return the set of matching paths or files.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static IList<string> GetPathsByWildcard(string path)
        {
            List<string> output = new();
            if (!string.IsNullOrWhiteSpace(path))
            {
                string? parent = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
                {
                    string pattern = Path.GetFileName(path);
                    if (pattern.Contains('?', StringComparison.Ordinal) || pattern.Contains('*', StringComparison.Ordinal))
                    {
                        string[] subDirs = Directory.GetDirectories(parent, pattern, SearchOption.TopDirectoryOnly);
                        output.AddRange(subDirs);

                        string[] files = Directory.GetFiles(parent, pattern, SearchOption.TopDirectoryOnly);
                        output.AddRange(files);
                    }
                }
            }
            return output;
        }
    }
}
