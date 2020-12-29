using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using dnGREP.Common;
using NLog;
using SevenZip;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Engines
{
    public static class ArchiveEngine
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        internal static List<GrepSearchResult> Search(IGrepEngine engine, string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            if (string.IsNullOrEmpty(file))
                throw new ArgumentNullException(nameof(file));

            string[] parts = file.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (!file.Contains(ArchiveDirectory.ArchiveSeparator) || parts.Length < 2)
            {
                return new List<GrepSearchResult>();
            }
            else
            {
                string diskFile = parts.First();
                string innerFileName = parts.Last();
                string[] intermediateFiles = parts.Skip(1).Take(parts.Length - 2).ToArray();

                if (diskFile.Length > 260 && !diskFile.StartsWith(@"\\?\", StringComparison.InvariantCulture))
                {
                    diskFile = @"\\?\" + diskFile;
                }

                using (FileStream fileStream = File.Open(diskFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
                {
                    return Search(engine, fileStream, file, diskFile, intermediateFiles, innerFileName, searchPattern, searchType, searchOptions, encoding);
                }
            }
        }
        private static List<GrepSearchResult> Search(IGrepEngine engine, Stream input, string compositeFileName, string diskFile,
            string[] intermediateFiles, string innerFileName, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding)
        {
            List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

            try
            {
                using (SevenZipExtractor extractor = new SevenZipExtractor(input))
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
                            var fd = extractor.ArchiveFileData.FirstOrDefault(f => string.Equals(f.FileName, name));
                            if (fd != null)
                                index = fd.Index;
                        }
                        if (index > -1)
                        {
                            using (Stream stream = new MemoryStream())
                            {
                                extractor.ExtractFile(index, stream);
                                string[] newIntermediateFiles = intermediateFiles.Skip(1).ToArray();

                                searchResults.AddRange(
                                    Search(engine, stream, compositeFileName, diskFile, newIntermediateFiles, innerFileName,
                                        searchPattern, searchType, searchOptions, encoding));
                            }
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
                            using (Stream stream = new MemoryStream())
                            {
                                extractor.ExtractFile(index, stream);
                                stream.Seek(0, SeekOrigin.Begin);

                                // Need to check the encoding of each file in the archive. If the encoding parameter is not default
                                // then it is the user-specified code page.  If the encoding parameter *is* the default,
                                // then it most likely not been set, so get the encoding of the extracted text file:
                                if (encoding == Encoding.Default && !Utils.IsBinary(stream))
                                {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    encoding = Utils.GetFileEncoding(stream);
                                }

                                var innerFileResults = engine.Search(stream, innerFileName, searchPattern, searchType, searchOptions, encoding);

                                if (innerFileResults.Count > 0)
                                {
                                    using (Stream readStream = new MemoryStream())
                                    {
                                        extractor.ExtractFile(index, readStream);
                                        readStream.Seek(0, SeekOrigin.Begin);
                                        using (StreamReader streamReader = new StreamReader(readStream, encoding))
                                        {
                                            foreach (var result in innerFileResults)
                                            {
                                                if (Utils.CancelSearch)
                                                    break;

                                                if (!result.HasSearchResults)
                                                    result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, engine.LinesBefore, engine.LinesAfter);
                                            }
                                        }
                                        searchResults.AddRange(innerFileResults);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (GrepSearchResult result in searchResults)
                {
                    result.InnerFileName = result.FileNameDisplayed;
                    result.FileNameDisplayed = compositeFileName;
                    result.FileNameReal = diskFile;
                    result.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, string.Format("Failed to search inside archive '{0}'", diskFile));
            }
            return searchResults;
        }
    }
}
