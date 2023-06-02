using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dnGREP.Common
{
    public class GrepSearchResult
    {
        public GrepSearchResult()
        {
            Id = Guid.NewGuid().ToString();

            IsSuccess = true;
        }

        public GrepSearchResult(FileData fileInfo, Encoding encoding)
        {
            Id = Guid.NewGuid().ToString();
            FileInfo = fileInfo;

            FileNameDisplayed = fileInfo.FullName;
            Pattern = string.Empty;
            Encoding = encoding;
            IsSuccess = string.IsNullOrEmpty(fileInfo.ErrorMsg);

            if (!string.IsNullOrEmpty(fileInfo.ErrorMsg))
            {
                searchResults = new List<GrepLine> { new GrepLine(-1, fileInfo.ErrorMsg, false, null) };
            }

            int pos = fileInfo.FullName.IndexOf(ArchiveDirectory.ArchiveSeparator, StringComparison.Ordinal);
            if (pos > -1)
            {
                IsReadOnlyFileType = true;

                FileNameReal = fileInfo.FullName[..pos];
                InnerFileName = fileInfo.FullName[(pos + ArchiveDirectory.ArchiveSeparator.Length)..];
            }
        }

        public GrepSearchResult(string file, string pattern, List<GrepMatch> matches, Encoding encoding)
            : this(file, pattern, matches, encoding, true)
        {
        }

        public GrepSearchResult(string file, string pattern, List<GrepMatch> matches, Encoding encoding, bool success)
        {
            Id = Guid.NewGuid().ToString();

            FileNameDisplayed = file;
            Matches = matches;
            Pattern = pattern;
            Encoding = encoding;
            IsSuccess = success;

            int pos = file.IndexOf(ArchiveDirectory.ArchiveSeparator, StringComparison.Ordinal);
            if (pos > -1)
            {
                IsReadOnlyFileType = true;

                FileNameReal = file[..pos];
                InnerFileName = file[(pos + ArchiveDirectory.ArchiveSeparator.Length)..];
            }
            else
            {
                FileInfo = new(file);
                if (Utils.IsBinary(file))
                {
                    IsReadOnlyFileType = true;
                }
            }
        }

        public GrepSearchResult(string file, string pattern, string errorMessage, bool success)
        {
            Id = Guid.NewGuid().ToString();

            FileNameDisplayed = file;
            searchResults = new List<GrepLine> { new GrepLine(-1, errorMessage, false, null) };
            Pattern = pattern;
            IsSuccess = success;
            FileInfo = new(file);
        }

        public string Id { get; }

        public Encoding Encoding { get; } = Encoding.UTF8;

        public string EOL { get; set; } = Environment.NewLine;

        public bool IsHexFile { get; set; }

        public string FileNameDisplayed { get; set; } = string.Empty;

        public string InnerFileName { get; set; } = string.Empty;

        public string Pattern { get; } = string.Empty;

        private string? fileNameToOpen = null;

        /// <summary>
        /// Use this property if FileNameDisplayed is not the same as FileNameReal.
        /// If null, FileNameDisplayed is used.
        /// </summary>
        /// <example>
        /// Files in archive have the following FileNameDisplayed "c:\path-to-archive\archive.zip\file1.txt" while
        /// FileNameReal is ""c:\path-to-archive\archive.zip". 
        /// </example>
        public string FileNameReal
        {
            get
            {
                if (fileNameToOpen == null)
                    return FileNameDisplayed;
                else
                    return fileNameToOpen;
            }
            set { fileNameToOpen = value; }
        }

        private FileData? fileInfo = null;
        public FileData FileInfo
        {
            get
            {
                fileInfo ??= new(FileNameReal);
                return fileInfo;

            }
            set { fileInfo = value; }
        }

        public string FileSize
        {
            get { return NativeMethods.StrFormatByteSize(FileInfo.Length); }
        }

        public string FileType
        {
            get { return NativeMethods.GetFileTypeDescription(Path.GetExtension(FileNameDisplayed)); }
        }

        /// <summary>
        /// Gets or sets additional information about the file to show in the results header
        /// </summary>
        public string AdditionalInformation { get; set; } = string.Empty;

        public bool IsReadOnlyFileType { get; set; } = false;

        private List<GrepLine>? searchResults;

        public bool HasSearchResults
        {
            get { return searchResults != null; }
        }

        public List<GrepLine> SearchResults
        {
            get
            {
                if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext))
                {
                    return GetLinesWithContext(
                        GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore),
                        GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter));
                }
                else
                {
                    return GetLinesWithContext(0, 0);
                }
            }
            set
            {
                searchResults = value;
            }
        }


        public List<GrepLine> GetLinesWithContext(int linesBefore, int linesAfter)
        {
            if (searchResults == null)
            {
                if (File.Exists(FileNameReal))
                {
                    if (Utils.IsArchive(FileNameReal))
                    {
                        searchResults = ArchiveDirectory.GetLinesWithContext(this, linesBefore, linesAfter);
                    }
                    else
                    {
                        EOL = Utils.GetEOL(FileNameReal, Encoding);

                        using FileStream reader = File.Open(FileNameReal, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        if (IsHexFile)
                        {
                            using BinaryReader readStream = new(reader);
                            searchResults = Utils.GetLinesHexFormat(readStream, Matches, linesBefore, linesAfter);
                        }
                        else
                        {
                            using StreamReader streamReader = new(reader, Encoding);
                            searchResults = Utils.GetLinesEx(streamReader, Matches, linesBefore, linesAfter);
                        }
                    }
                }
                else
                {
                    searchResults = new List<GrepLine>();
                }
            }
            return searchResults;
        }

        public List<GrepMatch> Matches { get; } = new List<GrepMatch>();

        public bool IsSuccess { get; }

        public override bool Equals(object? obj)
        {
            return obj is GrepSearchResult gsr && gsr.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode(StringComparison.Ordinal);
        }
    }
}
