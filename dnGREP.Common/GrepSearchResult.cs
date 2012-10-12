using System;
using System.Collections.Generic;
using System.IO;

namespace dnGREP.Common
{
	public class GrepSearchResult
	{
		public GrepSearchResult()
		{
            isSuccess = true;
        }

        public GrepSearchResult(string file, List<GrepMatch> matches)
            : this(file, matches, true)
		{			
		}

        public GrepSearchResult(string file, List<GrepMatch> matches, bool success)
        {
            fileName = file;
            bodyMatches = matches;
            isSuccess = success;
        }

        public GrepSearchResult(string file, string errorMessage, bool success)
        {
            fileName = file;
            bodyMatches = new List<GrepMatch>();
            searchResults = new List<GrepLine>();
            searchResults.Add(new GrepSearchResult.GrepLine(-1, errorMessage, false, null));

            isSuccess = success;
        }

		private string fileName;

		public string FileNameDisplayed
		{
			get { return fileName; }
			set { fileName = value; }
		}

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
			get {
				if (fileNameToOpen == null)
					return fileName;
				else
					return fileNameToOpen;
			}
			set { fileNameToOpen = value; }
		}

		private bool readOnly = false;

		public bool ReadOnly
		{
			get { return readOnly; }
			set { readOnly = value; }
		}

        private List<GrepLine> searchResults;

		public List<GrepLine> SearchResults
		{
			get 
            {
                if (searchResults == null)
                {
                    using (FileStream reader = File.Open(FileNameReal, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader streamReader = new StreamReader(reader))
                    {
                        searchResults = Utils.GetLinesEx(streamReader, bodyMatches, 0, 0);
                    }
                }
                return searchResults; 
            }
            set
            {
                searchResults = value;
            }
		}


        public List<GrepLine> GetLinesWithContext(int linesBefore, int linesAfter)
        {
            using (FileStream reader = File.Open(FileNameReal, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader streamReader = new StreamReader(reader))
            {
                return Utils.GetLinesEx(streamReader, bodyMatches, linesBefore, linesAfter);
            }
        }

        private List<GrepSearchResult.GrepMatch> bodyMatches = new List<GrepMatch>();

        public List<GrepSearchResult.GrepMatch> Matches
        {
            get { return bodyMatches; }
        }

        private bool isSuccess;

        public bool IsSuccess
        {
            get { return isSuccess; }
        }

		public class GrepLine : IComparable<GrepLine>, IComparable
		{
            const int MAX_LINE_LENGTH = 500;

			public GrepLine(int number, string text, bool context, List<GrepMatch> matches)
			{
				lineNumber = number;
                if (text.Length > MAX_LINE_LENGTH)
                    lineText = string.Format("{0}...", text.Substring(0, MAX_LINE_LENGTH));
                else
				    lineText = text;
				isContext = context;
                if (matches == null)
                    this.matches = new List<GrepMatch>();
                else
                    this.matches = matches;
			}

			private int lineNumber;

			public int LineNumber
			{
				get { return lineNumber; }
				set { lineNumber = value; }
			}
			private string lineText;

			public string LineText
			{
				get { return lineText; }
			}
			private bool isContext = false;

			public bool IsContext
			{
				get { return isContext; }
			}

            private List<GrepMatch> matches;

            public List<GrepMatch> Matches
            {
                get { return matches; }
            }

			public override string ToString()
			{
				return string.Format("{0}. {1} ({2})", lineNumber, lineText, isContext);
			}

			#region IComparable<GrepLine> Members

			public int CompareTo(GrepLine other)
			{
				if (other == null)
					return 1;
				else
					return lineNumber.CompareTo(other.LineNumber);
			}

			#endregion

			#region IComparable Members

			public int CompareTo(object obj)
			{
				if (obj == null)
					return 1;
				if (obj is GrepLine)
					return lineNumber.CompareTo(((GrepLine)obj).LineNumber);
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

		public class GrepMatch : IComparable<GrepMatch>, IComparable
        {
            public GrepMatch(int line, int start, int length)
            {
                lineNumber = line;
                startLocation = start;
                this.length = length;
            }

            private int lineNumber = 0;
            public int LineNumber
            {
                get { return lineNumber; }
                set { lineNumber = value; }
            }

            private int startLocation = 0;
            public int StartLocation
            {
                get { return startLocation; }
                set { startLocation = value; }
            }

            private int length = 0;
            public int Length
            {
                get { return length; }
                set { length = value; }
            }

            public int EndPosition
            {
                get
                {
                    return startLocation + length;
                }
                set {
                    length = value - startLocation;                    
                }
            }

			#region IComparable<GrepMatch> Members

			public int CompareTo(GrepMatch other)
			{
				if (other == null)
					return 1;
				else
					return startLocation.CompareTo(other.StartLocation);
			}

			#endregion

			#region IComparable Members

			public int CompareTo(object obj)
			{
				if (obj == null)
					return 1;
				if (obj is GrepMatch)
					return startLocation.CompareTo(((GrepMatch)obj).StartLocation);
				else
					return 1;
			}

			#endregion
        }
	}
}
