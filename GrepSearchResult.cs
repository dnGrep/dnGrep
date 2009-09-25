using System;
using System.Collections.Generic;
using System.Text;

namespace dnGREP
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

		public string FileName
		{
			get { return fileName; }
			set { fileName = value; }
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
