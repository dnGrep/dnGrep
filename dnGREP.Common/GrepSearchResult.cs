using System;
using System.Collections.Generic;
using System.Text;

namespace dnGREP.Common
{
	public class GrepSearchResult
	{
		public GrepSearchResult()
		{}

		public GrepSearchResult(string file, List<GrepLine> results)
		{
			fileName = file;
			searchResults = results;
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

		private List<GrepLine> searchResults = new List<GrepLine>();

		public List<GrepLine> SearchResults
		{
			get { return searchResults; }
		}

		public class GrepLine : IComparable<GrepLine>, IComparable
		{
			public GrepLine(int number, string text, bool context)
			{
				lineNumber = number;
				lineText = text;
				isContext = context;
			}

			private int lineNumber;

			public int LineNumber
			{
				get { return lineNumber; }
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
	}
}
