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
            Matches = matches;
            Pattern = pattern;
            Encoding = encoding;
            IsSuccess = success;
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

        public string FileNameDisplayed { get; set; }

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
                    using (StreamReader streamReader = new StreamReader(reader, Encoding))
                    {
                        searchResults = Utils.GetLinesEx(streamReader, Matches, linesBefore, linesAfter);
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

    public class GrepLine : IComparable<GrepLine>, IComparable
    {
        public GrepLine(int number, string text, bool context, List<GrepMatch> matches)
        {
            LineNumber = number;
            LineText = text;
            IsContext = context;
            if (matches == null)
                Matches = new List<GrepMatch>();
            else
                Matches = matches;
        }

        public int LineNumber { get; set; }

        public string LineText { get; }

        public bool IsContext { get; } = false;

        public List<GrepMatch> Matches { get; }


        /// <summary>
        /// Gets or sets the line number from a clipped view of the file
        /// that is showing only matched lines and context lines.
        /// Returns the normal line number if not set
        /// </summary>
        public int ClippedFileLineNumber
        {
            get { return _clippedFileLineNumber > -1 ? _clippedFileLineNumber : LineNumber; }
            set { _clippedFileLineNumber = value; }
        }
        private int _clippedFileLineNumber = -1;

        public override string ToString()
        {
            return string.Format("{0}. {1} ({2})", LineNumber, LineText, IsContext);
        }

        #region IComparable<GrepLine> Members

        public int CompareTo(GrepLine other)
        {
            if (other == null)
                return 1;
            else
                return LineNumber.CompareTo(other.LineNumber);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;
            if (obj is GrepLine)
                return LineNumber.CompareTo(((GrepLine)obj).LineNumber);
            else
                return 1;
        }

        #endregion
    }

    public enum GrepMatchTails
    {
        Length,
        EndPosition,
        EndOfLineOrFile
    }

    public class GrepMatch : IComparable<GrepMatch>, IComparable, IEquatable<GrepMatch>
    {
        public GrepMatch(int line, int start, int length)
        {
            LineNumber = line;
            StartLocation = start;
            Length = length;

            FileMatchId = Guid.NewGuid().ToString();
        }

        public GrepMatch(string fileMatchId, int line, int start, int length)
        {
            LineNumber = line;
            StartLocation = start;
            Length = length;

            FileMatchId = fileMatchId;
        }

        public string FileMatchId { get; }

        public int LineNumber { get; } = 0;

        /// <summary>
        /// The start location: could be within the whole file if created for a GrepSearchResult,
        /// or just the within line if created for a GrepLine
        /// </summary>
        public int StartLocation { get; } = 0;

        public int Length { get; private set; } = 0;

        public int EndPosition
        {
            get
            {
                return StartLocation + Length;
            }
            set
            {
                Length = value - StartLocation;
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating if this match should be replaced
        /// </summary>
        public bool ReplaceMatch { get; set; } = false;

        public override string ToString()
        {
            return $"{LineNumber}: {StartLocation} + {Length}";
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 397) ^ LineNumber;
                hashCode = (hashCode * 397) ^ StartLocation;
                hashCode = (hashCode * 397) ^ Length;
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GrepMatch);
        }

        public bool Equals(GrepMatch other)
        {
            if (other == null) return false;

            return LineNumber == other.LineNumber &&
                StartLocation == other.StartLocation &&
                Length == other.Length;
        }

        #region IComparable<GrepMatch> Members

        public int CompareTo(GrepMatch other)
        {
            if (other == null)
                return 1;
            else
                return StartLocation.CompareTo(other.StartLocation);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;
            if (obj is GrepMatch)
                return StartLocation.CompareTo(((GrepMatch)obj).StartLocation);
            else
                return 1;
        }

        #endregion
    }
}
