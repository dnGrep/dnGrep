using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NLog;
using SevenZip;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

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

        public static List<string> Extensions { get; private set; } = new List<string>();

        public static List<string> Patterns { get; private set; } = new List<string>();

        private static void GetExtensionsFromSettings(string name)
        {
            Extensions.Clear();
            Extensions.AddRange(DefaultExtensions);

            if (!string.IsNullOrEmpty(name))
            {
                string addKey = "Add" + CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name) + "Extensions";
                string remKey = "Rem" + CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name) + "Extensions";

                if (GrepSettings.Instance.ContainsKey(addKey))
                {
                    string csv = GrepSettings.Instance.Get<string>(addKey).Trim();
                    if (!string.IsNullOrWhiteSpace(csv))
                    {
                        foreach (string extension in csv.Split(','))
                        {
                            var ext = extension.Trim().ToLower(CultureInfo.CurrentCulture);
                            Extensions.Add(ext);
                        }
                    }
                }

                if (GrepSettings.Instance.ContainsKey(remKey))
                {
                    string csv = GrepSettings.Instance.Get<string>(remKey).Trim();
                    if (!string.IsNullOrWhiteSpace(csv))
                    {
                        foreach (string extension in csv.Split(','))
                        {
                            var ext = extension.Trim().ToLower(CultureInfo.CurrentCulture);
                            if (Extensions.Contains(ext))
                                Extensions.Remove(ext);
                        }
                    }
                }
            }

            Patterns.Clear();
            Patterns.AddRange(Extensions.Select(s => "*." + s));
        }


        public static IList<FileData> EnumerateFiles(string file, IList<string> includeSearchPatterns, FileFilter filter)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (file.Length > 260 && !file.StartsWith(@"\\?\", StringComparison.InvariantCulture))
            {
                file = @"\\?\" + file;
            }

            using (FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                return EnumerateFiles(fileStream, file, includeSearchPatterns, filter);
            }
        }

        public static IList<FileData> EnumerateFiles(Stream input, string file, IList<string> includeSearchPatterns, FileFilter filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            List<FileData> results = new List<FileData>();

            HashSet<string> hiddenDirectories = new HashSet<string>();

            try
            {
                using (SevenZipExtractor extractor = new SevenZipExtractor(input))
                {
                    foreach (var fileInfo in extractor.ArchiveFileData)
                    {
                        FileData fileData = new FileData(file, fileInfo);

                        var attr = (FileAttributes)fileInfo.Attributes;
                        string innerFileName = fileInfo.FileName;
                        if (innerFileName == "[no name]" && extractor.ArchiveFileData.Count == 1)
                        {
                            innerFileName = Path.GetFileNameWithoutExtension(file);
                            ArchiveFileInfo temp = Copy(fileInfo);
                            temp.FileName = innerFileName;
                            fileData = new FileData(file, temp);
                        }

                        if (fileInfo.IsDirectory)
                        {
                            if (!filter.IncludeHidden && attr.HasFlag(FileAttributes.Hidden) && !hiddenDirectories.Contains(innerFileName))
                                hiddenDirectories.Add(innerFileName);

                            continue;
                        }

                        if (!filter.IncludeBinary)
                        {
                            using (Stream stream = new MemoryStream())
                            {
                                extractor.ExtractFile(innerFileName, stream);
                                stream.Seek(0, SeekOrigin.Begin);

                                fileData.IsBinary = Utils.IsBinary(stream);
                            }
                        }

                        if (Utils.IsArchive(innerFileName))
                        {
                            using (Stream stream = new MemoryStream())
                            {
                                extractor.ExtractFile(innerFileName, stream);

                                foreach (var item in EnumerateFiles(stream, file + ArchiveSeparator + innerFileName, includeSearchPatterns, filter))
                                {
                                    results.Add(item);
                                }
                            }
                        }
                        else if (includeSearchPatterns != null && includeSearchPatterns.Count > 0)
                        {
                            foreach (string pattern in includeSearchPatterns)
                            {
                                if (SafeDirectory.WildcardMatch(innerFileName, pattern, true))
                                    results.Add(fileData);
                            }
                        }
                        else
                        {
                            results.Add(fileData);
                        }

                        if (Utils.CancelSearch)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, string.Format(CultureInfo.CurrentCulture, "Failed to search inside archive '{0}'", file), ex);
            }
            return results;
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
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            string filePath = ExtractToTempFile(args.SearchResult);

            if (Utils.IsPdfFile(filePath) || Utils.IsWordFile(filePath) || Utils.IsExcelFile(filePath))
                args.UseCustomEditor = false;

            GrepSearchResult newResult = new GrepSearchResult
            {
                FileNameReal = args.SearchResult.FileNameReal,
                FileNameDisplayed = args.SearchResult.FileNameDisplayed
            };
            OpenFileArgs newArgs = new OpenFileArgs(newResult, args.Pattern, args.LineNumber, args.FirstMatch, args.ColumnNumber, args.UseCustomEditor, args.CustomEditor, args.CustomEditorArgs);
            newArgs.SearchResult.FileNameDisplayed = filePath;
            Utils.OpenFile(newArgs);
        }

        public static string ExtractToTempFile(GrepSearchResult searchResult)
        {
            if (searchResult == null)
                throw new ArgumentNullException(nameof(searchResult));

            string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-Archive", Utils.GetHash(searchResult.FileNameReal));

            string[] parts = searchResult.FileNameDisplayed.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (!searchResult.FileNameDisplayed.Contains(ArchiveSeparator) || parts.Length < 2)
            {
                return string.Empty;
            }
            string innerFileName = parts.Last();
            string[] intermediateFiles = parts.Skip(1).Take(parts.Length - 2).ToArray();
            string filePath = Path.Combine(tempFolder, innerFileName);

            if (!File.Exists(filePath))
            {
                // use the directory name to also include folders within the archive
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string zipFile = searchResult.FileNameReal;
                if (zipFile.Length > 260 && !zipFile.StartsWith(@"\\?\", StringComparison.InvariantCulture))
                {
                    zipFile = @"\\?\" + zipFile;
                }

                using (SevenZipExtractor extractor = new SevenZipExtractor(zipFile))
                {
                    if (intermediateFiles.Length > 0 && extractor.ArchiveFileNames.Contains(intermediateFiles.First()))
                    {
                        using (Stream stream = new MemoryStream())
                        {
                            extractor.ExtractFile(intermediateFiles.First(), stream);
                            string[] newIntermediateFiles = intermediateFiles.Skip(1).ToArray();

                            ExtractToTempFile(stream, filePath, searchResult.FileNameReal, innerFileName, newIntermediateFiles);
                        }
                    }
                    else
                    {
                        int index = -1;
                        var info = extractor.ArchiveFileData.FirstOrDefault(r => r.FileName == innerFileName);
                        if (info != null)
                        {
                            index = info.Index;
                        }
                        else if (extractor.ArchiveFileNames.Count == 1 && extractor.ArchiveFileNames[0] == "[no name]")
                        {
                            index = 0;
                        }

                        if (index > -1)
                        {
                            using (FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                try
                                {
                                    extractor.ExtractFile(index, stream);
                                }
                                catch (Exception ex)
                                {
                                    logger.Log<Exception>(LogLevel.Error, string.Format(CultureInfo.CurrentCulture, "Failed extract file {0} from archive '{1}'", innerFileName, searchResult.FileNameReal), ex);
                                }
                            }
                        }
                    }
                }
            }

            return filePath;
        }

        private static void ExtractToTempFile(Stream input, string filePath, string diskFile, string innerFileName, string[] intermediateFiles)
        {
            using (SevenZipExtractor extractor = new SevenZipExtractor(input))
            {
                if (intermediateFiles.Length > 0 && extractor.ArchiveFileNames.Contains(intermediateFiles.First()))
                {
                    using (Stream stream = new MemoryStream())
                    {
                        extractor.ExtractFile(intermediateFiles.First(), stream);
                        string[] newIntermediateFiles = intermediateFiles.Skip(1).ToArray();

                        ExtractToTempFile(stream, filePath, diskFile, innerFileName, newIntermediateFiles);
                    }
                }
                else
                {
                    int index = -1;
                    var info = extractor.ArchiveFileData.FirstOrDefault(r => r.FileName == innerFileName);
                    if (info != null)
                    {
                        index = info.Index;
                    }
                    else if (extractor.ArchiveFileNames.Count == 1 && extractor.ArchiveFileNames[0] == "[no name]")
                    {
                        index = 0;
                    }

                    if (index > -1)
                    {
                        using (FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            try
                            {
                                extractor.ExtractFile(0, stream);
                            }
                            catch (Exception ex)
                            {
                                logger.Log<Exception>(LogLevel.Error, string.Format(CultureInfo.CurrentCulture, "Failed extract file {0} from archive '{1}'", innerFileName, diskFile), ex);
                            }
                        }
                    }
                }
            }
        }
    }
}
