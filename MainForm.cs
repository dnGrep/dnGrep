using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace nGREP
{
	public partial class MainForm : Form
	{
		List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

		private bool folderSelected = false;

		public bool FolderSelected
		{
			get { return folderSelected; }
			set
			{
				folderSelected = value;
				changeState();
			}
		}

		private bool isSearching = false;

		public bool IsSearching
		{
			get { return isSearching; }
			set
			{
				isSearching = value;
				changeState();
			}
		}

		private void changeState()
		{
			if (FolderSelected)
			{
				btnSearch.Enabled = true;
				btnReplace.Enabled = true;
			}
			else
			{
				btnSearch.Enabled = false;
				btnReplace.Enabled = false;
			}

			if (IsSearching)
			{
				btnSearch.Text = "Cancel";
			}
			else
			{
				btnSearch.Text = "Search";
			}
		}

		public MainForm()
		{
			InitializeComponent();

		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(Properties.Settings.Default.SearchFolder))
			{
				tbFolderName.Text = Properties.Settings.Default.SearchFolder;
				FolderSelected = true;
			}
			changeState();
		}

		private void btnSelectFolder_Click(object sender, EventArgs e)
		{
			folderSelectDialog.SelectedPath = tbFolderName.Text;
			if (folderSelectDialog.ShowDialog() == DialogResult.OK &&
				Directory.Exists(folderSelectDialog.SelectedPath))
			{
				FolderSelected = true;
				tbFolderName.Text = folderSelectDialog.SelectedPath;
			}
		}

		private void btnSearch_Click(object sender, EventArgs e)
		{
			if (!IsSearching)
			{
				Regex searchPattern = new Regex(tbSearchFor.Text);
				lblStatus.Text = "Searching...";
				IsSearching = true;
				barProgressBar.Value = 0;
				tvSearchResult.Nodes.Clear();
				workerSearcher.RunWorkerAsync(searchPattern);
			}
			else
			{
				GrepCore.CancelProcess = true;
			}
		}

		private void doSearch(object sender, DoWorkEventArgs e)
		{
			if (!workerSearcher.CancellationPending)
			{
				string[] files = FileUtils.GetFileList(tbFolderName.Text, tbFilePattern.Text, cbIncludeSubfolders.Checked, cbIncludeHiddenFolders.Checked);
				GrepCore grep = new GrepCore();
				grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
				GrepSearchResult[] results = grep.Search(files, (Regex)e.Argument);
				grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
				searchResults = new List<GrepSearchResult>(results);
			}
		}

		void grep_ProcessedFile(object sender, GrepCore.ProgressStatus progress)
		{
			workerSearcher.ReportProgress((int)(progress.ProcessedFiles * 100 / progress.TotalFiles), progress);
		}

		private void searchProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			GrepCore.ProgressStatus progress = (GrepCore.ProgressStatus)e.UserState;
			barProgressBar.Value = e.ProgressPercentage;
			lblStatus.Text = "(" + progress.ProcessedFiles + " of " + progress.TotalFiles + ")";
		}

		private void searchComplete(object sender, RunWorkerCompletedEventArgs e)
		{
			if (!e.Cancelled)
			{
				lblStatus.Text = "Search Complete - " + searchResults.Count + " files found.";				
			}
			else
			{
				lblStatus.Text = "Search Canceled";
			}
			IsSearching = false;
			populateResults();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Properties.Settings.Default.Save();
		}

		private void populateResults()
		{
			tvSearchResult.Nodes.Clear();
			if (searchResults == null)
				return;
			foreach (GrepSearchResult result in searchResults)
			{
				TreeNode node = new TreeNode(Path.GetFileName(result.FileName));
				tvSearchResult.Nodes.Add(node);
				foreach (GrepSearchResult.GrepLine line in result.SearchResults)
				{
					string lineSummary = line.LineText.Replace("\n", "").Replace("\t", "").Replace("\r", "").Trim();
					if (lineSummary.Length == 0)
						lineSummary = "<none>";
					else if (lineSummary.Length > 100)
						lineSummary = lineSummary.Substring(0, 100) + "...";
					node.Nodes.Add(line.LineNumber + ":" + lineSummary);
				}
			}
		}
	}
}