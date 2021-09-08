using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Common
{
    public class GrepSearchResult
    {
        public GrepSearchResult()
        {
            IsSuccess = true;
        }

        public GrepSearchResult(string file, string pattern, List<GrepMatch> matches, Encoding encoding)
            : this(file, pattern, matches, encoding, true)
        {
        }

        public GrepSearchResult(string file, string pattern, List<GrepMatch> matches, Encoding encoding, bool success)
        {
            FileNameDisplayed = file;
            if (matches != null)
                Matches = matches;
            Pattern = pattern;
            Encoding = encoding;
            IsSuccess = success;

            if (file.Contains(ArchiveDirectory.ArchiveSeparator))
            {
                ReadOnly = true;
                string[] parts = file.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                    FileNameReal = parts[0];
                if (parts.Length > 1)
                    InnerFileName = parts[1];
            }
        }

        public GrepSearchResult(string file, string pattern, string errorMessage, bool success)
        {
            FileNameDisplayed = file;
            Matches = new List<GrepMatch>();
            searchResults = new List<GrepLine> { new GrepLine(-1, errorMessage, false, null) };
            Pattern = pattern;
            IsSuccess = success;
        }

        public Encoding Encoding { get; }

        public string EOL { get; set; }

        public bool IsHexFile { get; set; }

        public string FileNameDisplayed { get; set; }

        public string InnerFileName { get; set; }

        public string Pattern { get; }

        private string fileNameToOpen = null;

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

        private FileData fileInfo;
        public FileData FileInfo
        {
            get
            {
                if (fileInfo == null)
                {
                    if (!string.IsNullOrEmpty(InnerFileName))
                    {
                        fileInfo = ArchiveDirectory.GetFileData(this);
                    }
                    else
                    {
                        fileInfo = new FileData(FileNameReal);
                    }
                }
                return fileInfo;
            }
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
        public string AdditionalInformation { get; set; }

        public bool ReadOnly { get; set; } = false;

        private List<GrepLine> searchResults;

        public bool HasSearchResults
        {
            get { return searchResults != null; }
        }

        public List<GrepLine> SearchResults
        {
            get
            {
                return GetLinesWithContext(GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore),
                    GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter));
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
                    EOL = Utils.GetEOL(FileNameReal, Encoding);

                    using (FileStream reader = File.Open(FileNameReal, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (IsHexFile)
                        {
                            using (BinaryReader readStream = new BinaryReader(reader))
                            { 
                                searchResults = Utils.GetLinesHexFormat(readStream, Matches, linesBefore, linesAfter);
                            }
                        }
                        else
                        {
                            using (StreamReader streamReader = new StreamReader(reader, Encoding))
                            {
                                searchResults = Utils.GetLinesEx(streamReader, Matches, linesBefore, linesAfter);
                            }
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
    }
}
