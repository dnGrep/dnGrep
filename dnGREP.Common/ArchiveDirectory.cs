using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using SevenZip;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Common
{
    public static class ArchiveDirectory
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public const string ArchiveSeparator = "///";

        static ArchiveDirectory()
        {
            GetExtensionsFromSettings("Archive");
        }

        public static IList<string> DefaultExtensions
        {
            get { return new string[] { "zip", "7z", "jar", "war", "ear", "rar", "cab", "gz", "gzip", "tar", "rpm", "iso", "isx", "bz2", "bzip2", "tbz2", "tbz", "tgz", "arj", "cpio", "deb", "dmg", "hfs", "hfsx", "lzh", "lha", "lzma", "z", "taz", "xar", "pkg", "xz", "txz", "zipx", "epub", "wim", "chm" }; }
        }

        public static void Reinitialize()
        {
            GetExtensionsFromSettings("Archive");
        }

        public static List<string> Extensions { get; private set; } = new();

        public static List<string> Patterns { get; private set; } = new();

        private static void GetExtensionsFromSettings(string name)
        {
            var list = GrepSettings.Instance.GetExtensionList(name, DefaultExtensions);

            Extensions.Clear();
            Extensions.AddRange(list);

            Patterns.Clear();
            Patterns.AddRange(Extensions.Select(s => "*." + s));
        }

        public static IEnumerable<FileData> EnumerateFiles(string file, FileFilter filter,
            PauseCancelToken pauseCancelToken)
        {
            if (file.Length > 260 && !file.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                file = @"\\?\" + file;
            }

            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            foreach (var item in EnumerateFiles(fileStream, file, filter, pauseCancelToken))
            {
                yield return item;
            }
        }

        private static IEnumerable<FileData> EnumerateFiles(Stream input, string fileName, FileFilter filter,
            PauseCancelToken pauseCancelToken)
        {
            List<string> includeSearchPatterns = new();
            bool hasSearchPattern = Utils.PrepareSearchPatterns(filter, includeSearchPatterns);

            List<Regex> includeRegexPatterns = new();
            List<Regex> excludeRegexPatterns = new();
            List<Regex> includeShebangPatterns = new();
            Utils.PrepareFilters(filter, includeRegexPatterns, excludeRegexPatterns, includeShebangPatterns, hasSearchPattern);

            HashSet<string> hiddenDirectories = new();

            bool checkEncoding = GrepSettings.Instance.Get<bool>(GrepSettings.Key.DetectEncodingForFileNamePattern);

            var enumerator = EnumerateFiles(input, fileName, filter, checkEncoding, includeSearchPatterns,
                    includeRegexPatterns, excludeRegexPatterns, includeShebangPatterns, hiddenDirectories,
                    pauseCancelToken).GetEnumerator();
            while (true)
            {
                FileData ret;
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                    ret = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    // expected for stop after first match or user cancel
                    yield break;
                }
                catch (Exception ex)
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedToSearchInsideArchive0, fileName);
                    logger.Error(ex, msg);

                    ret = new FileData(fileName)
                    {
                        ErrorMsg = msg + ": " + ex.Message
                    };
                }
                if (ret != null)
                {
                    yield return ret;
                }
            }
        }

        private static IEnumerable<FileData> EnumerateFiles(Stream input, string fileName,
            FileFilter fileFilter, bool checkEncoding, List<string> includeSearchPatterns,
            List<Regex> includeRegexPatterns, List<Regex> excludeRegexPatterns,
            List<Regex> includeShebangPatterns, HashSet<string> hiddenDirectories,
            PauseCancelToken pauseCancelToken)
        {
            using SevenZipExtractor extractor = new(input, true);
            foreach (var fileInfo in extractor.ArchiveFileData)
            {
                FileData fileData = new(fileName, fileInfo);

                var attr = (FileAttributes)fileInfo.Attributes;
                string innerFileName = fileInfo.FileName;

                int index = fileInfo.Index;
                if (innerFileName == "[no name]" && extractor.ArchiveFileData.Count == 1)
                {
                    index = 0;
                    innerFileName = Path.GetFileNameWithoutExtension(fileName);
                    ArchiveFileInfo temp = Copy(fileInfo);
                    temp.FileName = innerFileName;
                    fileData = new(fileName, temp);
                }

                if (fileInfo.IsDirectory)
                {
                    if (!fileFilter.IncludeHidden && attr.HasFlag(FileAttributes.Hidden) &&
                        !hiddenDirectories.Contains(innerFileName))
                    {
                        hiddenDirectories.Add(innerFileName + Path.DirectorySeparatorChar);
                    }

                    continue;
                }

                if (!fileFilter.IncludeHidden)
                {
                    if (attr.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }

                    bool excludeFile = false;
                    foreach (string dir in hiddenDirectories)
                    {
                        if (innerFileName.StartsWith(dir, StringComparison.Ordinal))
                        {
                            excludeFile = true;
                            break;
                        }
                    }

                    if (excludeFile)
                    {
                        continue;
                    }
                }

                if (Utils.IsArchive(innerFileName))
                {
                    using Stream stream = new MemoryStream();
                    extractor.ExtractFile(index, stream);

                    var enumerator = EnumerateFiles(stream, fileName + ArchiveSeparator + innerFileName,
                        fileFilter, checkEncoding, includeSearchPatterns, includeRegexPatterns,
                        excludeRegexPatterns, includeShebangPatterns, hiddenDirectories,
                        pauseCancelToken).GetEnumerator();

                    while (true)
                    {
                        FileData? ret = null;
                        try
                        {
                            if (!enumerator.MoveNext())
                            {
                                break;
                            }
                            ret = enumerator.Current;
                        }
                        catch (OperationCanceledException)
                        {
                            // expected for stop after first match or user cancel
                            yield break;
                        }
                        catch (Exception ex)
                        {
                            string msg = string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedToSearchInsideArchive0, fileName + ArchiveSeparator + innerFileName);
                            logger.Error(ex, msg);

                            fileData.ErrorMsg = msg + ": " + ex.Message;
                            ret = fileData;
                        }
                        if (ret != null)
                        {
                            yield return ret;
                        }
                    }
                }
                else
                {
                    if (IncludeFile(innerFileName,
                        fileName + ArchiveSeparator + innerFileName,
                        fileFilter, fileData, includeSearchPatterns,
                        includeRegexPatterns, excludeRegexPatterns))
                    {
                        if (NeedsIncludeFileStream(fileName, fileFilter, checkEncoding,
                            includeSearchPatterns, includeShebangPatterns))
                        {
                            using Stream stream = new MemoryStream(4096);
                            extractor.ExtractFile(index, stream);

                            if (IncludeFileStream(stream, fileFilter, fileData,
                                checkEncoding, includeShebangPatterns))
                            {
                                yield return fileData;
                            }
                        }
                        else
                        {
                            yield return fileData;
                        }
                    }
                }

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Evaluates if a file should be included in the search results
        /// only checks filters that do not extract/read the file...
        /// call NeedsIncludeFileStream and IncludeFileStream if file needs
        /// to be extracted and read to evaluate
        /// </summary>
        public static bool IncludeFile(string innerFileName, string compositeFileName,
            FileFilter filter, FileData fileData, IList<string> includeSearchPatterns,
            IList<Regex> includeRegexPatterns, IList<Regex> excludeRegexPatterns)
        {
            try
            {
                if (includeSearchPatterns != null && includeSearchPatterns.Count > 0)
                {
                    bool include = false;
                    string fileName = Path.GetFileName(innerFileName); // strip inner directory names
                    foreach (string pattern in includeSearchPatterns)
                    {
                        if (pattern.Contains('*', StringComparison.Ordinal) || pattern.Contains('?', StringComparison.Ordinal))
                        {
                            if (SafeDirectory.WildcardMatch(fileName, pattern, true))
                            {
                                include = true;
                                break;
                            }
                        }
                        else
                        {
                            if (fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                            {
                                include = true;
                                break;
                            }
                        }
                    }
                    if (!include)
                    {
                        return false;
                    }
                }

                if (includeRegexPatterns != null && includeRegexPatterns.Count > 0)
                {
                    bool include = false;
                    foreach (var pattern in includeRegexPatterns)
                    {
                        if (pattern.IsMatch(innerFileName))
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
                    if (pattern.IsMatch(innerFileName))
                    {
                        return false;
                    }
                }

                if (filter.SizeFrom > 0 || filter.SizeTo > 0)
                {
                    long sizeKB = fileData.Length / 1000;
                    if (filter.SizeFrom > 0 && sizeKB < filter.SizeFrom)
                    {
                        return false;
                    }
                    if (filter.SizeTo > 0 && sizeKB > filter.SizeTo)
                    {
                        return false;
                    }
                }

                if (filter.DateFilter != FileDateFilter.None)
                {
                    DateTime fileDate = filter.DateFilter == FileDateFilter.Created ? fileData.CreationTime : fileData.LastWriteTime;
                    if (filter.StartTime.HasValue && fileDate < filter.StartTime.Value)
                    {
                        return false;
                    }
                    if (filter.EndTime.HasValue && fileDate >= filter.EndTime.Value)
                    {
                        return false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected for stop after first match or user cancel
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedToSearchInsideArchive0, compositeFileName));
            }
            return true;
        }

        public static bool NeedsIncludeFileStream(
            string fileName, FileFilter filter, bool checkEncoding,
            IList<string> includeSearchPatterns, IList<Regex> includeShebangPatterns)
        {
            if (includeShebangPatterns.Any())
            {
                return true;
            }

            if (!filter.IncludeBinary || checkEncoding)
            {
                bool isExcelMatch = Utils.IsExcelFile(fileName) && includeSearchPatterns.Contains(".xls", StringComparison.OrdinalIgnoreCase);
                bool isWordMatch = Utils.IsWordFile(fileName) && includeSearchPatterns.Contains(".doc", StringComparison.OrdinalIgnoreCase);
                bool isPowerPointMatch = Utils.IsPowerPointFile(fileName) && includeSearchPatterns.Contains(".ppt", StringComparison.OrdinalIgnoreCase);
                bool isPdfMatch = Utils.IsPdfFile(fileName) && includeSearchPatterns.Contains(".pdf", StringComparison.OrdinalIgnoreCase);

                // When searching for Excel, Word, PowerPoint, or PDF files, skip the binary file check
                // and the encoding check.
                // If someone is searching for one of these types, don't make them include binary to 
                // find their files.
                if (!(isExcelMatch || isWordMatch || isPowerPointMatch || isPdfMatch))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Evaluates if a file should be included in the search results
        /// </summary>
        public static bool IncludeFileStream(Stream stream, FileFilter filter,
            FileData fileData, bool checkEncoding, IList<Regex> includeShebangPatterns)
        {
            if (stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);

                bool isExcelMatch = Utils.IsExcelFile(fileData.Name) && filter.NamePatternToInclude.Contains(".xls", StringComparison.OrdinalIgnoreCase);
                bool isWordMatch = Utils.IsWordFile(fileData.Name) && filter.NamePatternToInclude.Contains(".doc", StringComparison.OrdinalIgnoreCase);
                bool isPowerPointMatch = Utils.IsPowerPointFile(fileData.Name) && filter.NamePatternToInclude.Contains(".ppt", StringComparison.OrdinalIgnoreCase);
                bool isPdfMatch = Utils.IsPdfFile(fileData.Name) && filter.NamePatternToInclude.Contains(".pdf", StringComparison.OrdinalIgnoreCase);

                // When searching for Excel, Word, PowerPoint, or PDF files, skip the binary file check:
                // If someone is searching for one of these types, don't make them include binary to 
                // find their files.
                // the isBinary flag is needed for the Encoding check below
                fileData.IsBinary = Utils.IsBinary(stream);
                if (!(isExcelMatch || isWordMatch || isPowerPointMatch || isPdfMatch) && !filter.IncludeBinary && fileData.IsBinary)
                {
                    return false;
                }

                if (checkEncoding && !fileData.IsBinary)
                {
                    fileData.Encoding = Utils.GetFileEncoding(stream);
                }

                bool hasSheBangPattern = includeShebangPatterns.Any();
                if (hasSheBangPattern)
                {
                    bool include = false;
                    foreach (var pattern in includeShebangPatterns)
                    {
                        if (Utils.CheckShebang(stream, pattern.ToString()))
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
            }
            return true;
        }

        public static ArchiveFileInfo Copy(ArchiveFileInfo fileInfo)
        {
            return new ArchiveFileInfo
            {
                Attributes = fileInfo.Attributes,
                Comment = fileInfo.Comment,
                Crc = fileInfo.Crc,
                CreationTime = fileInfo.CreationTime,
                Encrypted = fileInfo.Encrypted,
                FileName = fileInfo.FileName,
                Index = fileInfo.Index,
                IsDirectory = fileInfo.IsDirectory,
                LastAccessTime = fileInfo.LastAccessTime,
                LastWriteTime = fileInfo.LastWriteTime,
                Size = fileInfo.Size,
            };
        }

        public static void OpenFile(OpenFileArgs args)
        {
            string filePath = ExtractToTempFile(args.SearchResult);

            if (Utils.IsWordFile(filePath) || Utils.IsExcelFile(filePath) || Utils.IsPowerPointFile(filePath))
                args.UseCustomEditor = false;

            GrepSearchResult newResult = new()
            {
                FileNameReal = args.SearchResult.FileNameReal,
                FileNameDisplayed = args.SearchResult.FileNameDisplayed
            };
            OpenFileArgs newArgs = new(newResult, args.Pattern, args.PageNumber, args.LineNumber, args.FirstMatch, args.ColumnNumber, args.UseCustomEditor, args.CustomEditor, args.CustomEditorArgs);
            newArgs.SearchResult.FileNameDisplayed = filePath;
            Utils.OpenFile(newArgs);
        }

        public static List<GrepLine> GetLinesWithContext(GrepSearchResult searchResult, int linesBefore, int linesAfter)
        {
            string[] parts = searchResult.FileNameDisplayed.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (!searchResult.FileNameDisplayed.Contains(ArchiveSeparator, StringComparison.Ordinal) || parts.Length < 2)
            {
                return new List<GrepLine>();
            }

            string innerFileName = parts.Last();
            string[] intermediateFiles = parts.Skip(1).Take(parts.Length - 2).ToArray();

            string zipFile = searchResult.FileNameReal;
            if (zipFile.Length > 260 && !zipFile.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                zipFile = @"\\?\" + zipFile;
            }

            using FileStream input = File.Open(zipFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return GetLinesWithContext(input, searchResult, linesBefore, linesAfter, innerFileName, intermediateFiles);
        }

        private static List<GrepLine> GetLinesWithContext(Stream input, GrepSearchResult searchResult, int linesBefore, int linesAfter, string innerFileName, string[] intermediateFiles)
        {
            List<GrepLine> results = new();

            using (SevenZipExtractor extractor = new(input, true))
            {
                if (intermediateFiles.Length > 0)
                {
                    int index = -1;
                    if (extractor.ArchiveFileData.Count == 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        string name = intermediateFiles.First();
                        var fd = extractor.ArchiveFileData.FirstOrDefault(f => string.Equals(f.FileName, name, StringComparison.Ordinal));
                        if (fd != default)
                        {
                            index = fd.Index;
                        }
                    }

                    if (index > -1)
                    {
                        using Stream stream = new MemoryStream(4096);
                        extractor.ExtractFile(index, stream);
                        string[] newIntermediateFiles = intermediateFiles.Skip(1).ToArray();

                        results = GetLinesWithContext(stream, searchResult, linesBefore, linesAfter, innerFileName, newIntermediateFiles);
                    }
                }
                else
                {
                    int index = -1;
                    if (extractor.ArchiveFileData.Count == 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        var fd = extractor.ArchiveFileData.FirstOrDefault(f => string.Equals(f.FileName, innerFileName, StringComparison.Ordinal));
                        if (fd != default)
                        {
                            index = fd.Index;
                        }
                    }

                    if (index > -1)
                    {
                        using Stream stream = new MemoryStream(4096);
                        try
                        {
                            extractor.ExtractFile(index, stream);
                            stream.Seek(0, SeekOrigin.Begin);
                            using StreamReader reader = new(stream);
                            results = Utils.GetLinesEx(reader, searchResult.Matches, linesBefore, linesAfter);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedExtractFile0FromArchive1, innerFileName, searchResult.FileNameReal));
                        }
                    }
                }
            }
            return results;
        }

        public static string ExtractToTempFile(GrepSearchResult searchResult)
        {
            string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-Archive", Utils.GetHash(searchResult.FileNameReal));

            string[] parts = searchResult.FileNameDisplayed.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (!searchResult.FileNameDisplayed.Contains(ArchiveSeparator, StringComparison.Ordinal) || parts.Length < 2)
            {
                return string.Empty;
            }
            string innerFileName = parts.Last();
            string[] intermediateFiles = parts.Skip(1).Take(parts.Length - 2).ToArray();
            string filePath = Path.Combine(tempFolder, innerFileName);

            if (!File.Exists(filePath))
            {
                // use the directory name to also include folders within the archive
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string zipFile = searchResult.FileNameReal;
                if (zipFile.Length > 260 && !zipFile.StartsWith(@"\\?\", StringComparison.Ordinal))
                {
                    zipFile = @"\\?\" + zipFile;
                }

                using FileStream input = File.Open(zipFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                ExtractToTempFile(input, filePath, searchResult.FileNameReal, innerFileName, intermediateFiles);
            }

            return filePath;
        }

        private static void ExtractToTempFile(Stream input, string filePath, string diskFile, string innerFileName, string[] intermediateFiles)
        {
            using SevenZipExtractor extractor = new(input, true);
            if (intermediateFiles.Length > 0)
            {
                int index = -1;
                if (extractor.ArchiveFileData.Count == 1)
                {
                    index = 0;
                }
                else
                {
                    string name = intermediateFiles.First();
                    var fd = extractor.ArchiveFileData.FirstOrDefault(f => string.Equals(f.FileName, name, StringComparison.Ordinal));
                    if (fd != default)
                    {
                        index = fd.Index;
                    }
                }

                if (index > -1)
                {
                    using Stream stream = new MemoryStream(4096);
                    extractor.ExtractFile(index, stream);
                    string[] newIntermediateFiles = intermediateFiles.Skip(1).ToArray();

                    ExtractToTempFile(stream, filePath, diskFile, innerFileName, newIntermediateFiles);
                }
            }
            else
            {
                int index = -1;
                if (extractor.ArchiveFileData.Count == 1)
                {
                    index = 0;
                }
                else
                {
                    var fd = extractor.ArchiveFileData.FirstOrDefault(f => string.Equals(f.FileName, innerFileName, StringComparison.Ordinal));
                    if (fd != default)
                    {
                        index = fd.Index;
                    }
                }

                if (index > -1)
                {
                    using FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    try
                    {
                        extractor.ExtractFile(index, stream);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, string.Format(CultureInfo.CurrentCulture, Resources.Error_FailedExtractFile0FromArchive1, innerFileName, diskFile));
                    }
                }
            }
        }
    }
}
