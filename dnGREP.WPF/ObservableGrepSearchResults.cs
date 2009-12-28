using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using dnGREP.Common;
using System.Windows.Documents;
using System.Windows.Media;
using System.IO;

namespace dnGREP.WPF
{
	public class ObservableGrepSearchResults : ObservableCollection<FormattedGrepResult>
	{
		private string folderPath = "";

		public string FolderPath
		{
			get { return folderPath; }
			set { folderPath = value; }
		}

		public ObservableGrepSearchResults()
		{ }

		public ObservableGrepSearchResults(List<GrepSearchResult> list)
		{
			AddRange(list);
		}

		public List<GrepSearchResult> GetList()
		{
			List<GrepSearchResult> tempList = new List<GrepSearchResult>();
			foreach (var l in this) tempList.Add(l.GrepResult);
			return tempList;
		}

		public void AddRange(List<GrepSearchResult> list)
		{
			foreach (var l in list) this.Add(new FormattedGrepResult(l, folderPath));
		}
	}

	public class FormattedGrepResult
	{
		private GrepSearchResult grepResult = new GrepSearchResult();
		public GrepSearchResult GrepResult
		{
			get { return grepResult; }
		}

		private string style = "";
		public string Style
		{
			get { return style; }
			set { style = value; }
		}

		private string label = "";
		public string Label
		{
			get
			{
				return label;
			}
		}

		private List<FormattedGrepLine> formattedLines = new List<FormattedGrepLine>();
		public List<FormattedGrepLine> FormattedLines
		{
			get { return formattedLines; }
		}

		public FormattedGrepResult(GrepSearchResult result, string folderPath)
		{
			grepResult = result;

			// Populate icon list
			// TODO

			bool isFileReadOnly = Utils.IsReadOnly(grepResult);
			string displayedName = Path.GetFileName(grepResult.FileNameDisplayed);
			if (Properties.Settings.Default.ShowFilePathInResults &&
				grepResult.FileNameDisplayed.Contains(Utils.GetBaseFolder(folderPath) + "\\"))
			{
				displayedName = grepResult.FileNameDisplayed.Substring(Utils.GetBaseFolder(folderPath).Length + 1);
			}
			int lineCount = Utils.MatchCount(grepResult);
			if (lineCount > 0)
				displayedName = string.Format("{0} ({1})", displayedName, lineCount);
			if (isFileReadOnly)
				displayedName = displayedName + " [read-only]";

			label = displayedName;

			if (isFileReadOnly)
			{
				style = "ReadOnly";
			}

			if (result.SearchResults != null)
			{
				for (int i = 0; i < result.SearchResults.Count; i++)
				{
					GrepSearchResult.GrepLine line = result.SearchResults[i];
					formattedLines.Add(new FormattedGrepLine(line));
				}
			}
		}		
	}

	public class FormattedGrepLine
	{
		private GrepSearchResult.GrepLine grepLine;
		public GrepSearchResult.GrepLine GrepLine
		{
			get { return grepLine; }
		}

		private InlineCollection formattedText;
		public InlineCollection FormattedText
		{
			get { return formattedText; }
		}

		private string style = "";
		public string Style
		{
			get { return style; }
			set { style = value; }
		}

		public FormattedGrepLine(GrepSearchResult.GrepLine line)
		{
			grepLine = line;

			string lineSummary = line.LineText.Replace("\n", "").Replace("\t", "").Replace("\r", "").Trim();
			if (lineSummary.Length == 0)
				lineSummary = " ";
			else if (lineSummary.Length > 100)
				lineSummary = lineSummary.Substring(0, 100) + "...";
			string lineNumber = (line.LineNumber == -1 ? "" : line.LineNumber + ": ");

			string fullText = lineNumber + lineSummary;
			if (!line.IsContext && Properties.Settings.Default.ShowLinesInContext)
			{
				style = "Context";
			}
			
			Paragraph paragraph = new Paragraph();
			//Run highlightedRun = new Run("hello ");
			//highlightedRun.Background = Brushes.Yellow;
			//paragraph.Inlines.Add(highlightedRun);
			Run mainRun = new Run(fullText);
			paragraph.Inlines.Add(mainRun);
			formattedText = paragraph.Inlines;
		}
	}
}
