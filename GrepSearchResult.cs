using System;
using System.Collections.Generic;
using System.Text;

namespace nGREP
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

		public class GrepLine
		{
			public GrepLine(int number, string text)
			{
				lineNumber = number;
				lineText = text;
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
		}
	}
}
