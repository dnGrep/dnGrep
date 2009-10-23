using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using NLog;
using System.Reflection;

namespace dnGREP
{
	public partial class MainForm : Form
	{
		List<GrepSearchResult> searchResults = new List<GrepSearchResult>();
		List<KeyValuePair<string, int>> encodings = new List<KeyValuePair<string, int>>();
		private const string SEARCH_KEY = "search";
		private const string REPLACE_KEY = "replace";
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private DateTime timer = DateTime.Now;
		PublishedVersionExtractor ve = new PublishedVersionExtractor();
		
		#region States

		private int codePage = -1;

		public int CodePage
		{
			get { return codePage; }
			set { codePage = value; }
		}

		private bool doSearchInResults = false;

		public bool DoSearchInResults
		{
			get { return doSearchInResults; }
			set { doSearchInResults = value; }
		}

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

		private bool searchPatternEntered = false;

		public bool SearchPatternEntered
		{
			get { return searchPatternEntered; }
			set { searchPatternEntered = value;
				changeState();
			}
		}
		private bool replacePatternEntered = false;

		public bool ReplacePatternEntered
		{
			get { return replacePatternEntered; }
			set { replacePatternEntered = value;
				changeState();
			}
		}

		private bool filesFound = false;

		public bool FilesFound
		{
			get { return filesFound; }
			set { 
				filesFound = value;
				changeState();
			}
		}

		private bool isAllSizes = true;

		public bool IsAllSizes
		{
			get { return isAllSizes; }
			set { 
				isAllSizes = value;
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

		private bool isReplacing = false;

		public bool IsReplacing
		{
			get { return isReplacing; }
			set
			{
				isReplacing = value;
				changeState();
			}
		}

		private bool isPlainText = false;

		public bool IsPlainText
		{
			get { return isPlainText; }
			set
			{
				isPlainText = value;
				changeState();
			}
		}

		private bool canUndo = false;
		private string undoFolder = "";

		public bool CanUndo
		{
			get { return canUndo; }
			set {
				if (value && Directory.Exists(undoFolder))
					canUndo = value;
				else
					canUndo = false;
				changeState();
			}
		}

		private bool isMultiline = false;

		public bool IsMultiline
		{
			get { return isMultiline; }
			set { 
				isMultiline = value;
				changeState();
			}
		}

		private void changeState()
		{
			//tbSearchFor
			//tbReplaceWith
			//splitContainer
			if (IsMultiline)
			{
				tbSearchFor.Multiline = true;
				tbReplaceWith.Multiline = true;
				splitContainer.SplitterDistance = 180;
				splitContainer.IsSplitterFixed = false;
			}
			else
			{
				tbSearchFor.Multiline = false;
				tbReplaceWith.Multiline = false;
				splitContainer.SplitterDistance = 134;
				splitContainer.IsSplitterFixed = true;
			}			

			// btnSearch
			// searchInResultsToolStripMenuItem
			if (FolderSelected && !IsSearching && !IsReplacing && 
				(SearchPatternEntered || Properties.Settings.Default.AllowSearchingForFileNamePattern))
			{
				btnSearch.Enabled = true;
				searchInResultsToolStripMenuItem.Enabled = true;
			} else {
				btnSearch.Enabled = false;
				searchInResultsToolStripMenuItem.Enabled = false;
			}

			//btnSearch.ShowAdvance
			if (searchResults.Count > 0)
			{
				btnSearch.ShowSplit = true;
			}
			else 
			{
				btnSearch.ShowSplit = false;
			}

			// btnReplace
			if (FolderSelected && FilesFound && !IsSearching && !IsReplacing 
				&& SearchPatternEntered)
			{
				btnReplace.Enabled = true;
			} else {
				btnReplace.Enabled = false;
			}

			//btnCancel
			if (IsSearching)
			{
				btnCancel.Enabled = true;
			}
			else if (IsReplacing)
			{
				btnCancel.Enabled = true;
			}
			else
			{
				btnCancel.Enabled = false;
			}

			//undoToolStripMenuItem
			if (CanUndo)
			{
				undoToolStripMenuItem.Enabled = true;
			}
			else
			{
				undoToolStripMenuItem.Enabled = false;
			}

			//cbCaseSensitive
			if (rbXPathSearch.Checked)
			{
				cbCaseSensitive.Enabled = false;
			}
			else
			{
				cbCaseSensitive.Enabled = true;
			}

			//btnTest
			if (!IsPlainText &&
				!rbXPathSearch.Checked)
			{
				btnTest.Enabled = true;
			}
			else
			{
				btnTest.Enabled = false;
			}

			//cbMultiline
			if (rbXPathSearch.Checked)
			{
				cbMultiline.Enabled = false;
			} 
			else 
			{
				cbMultiline.Enabled = true;
			}

			//tbFileSizeFrom
			//tbFileSizeTo
			if (IsAllSizes)
			{
				tbFileSizeFrom.Enabled = false;
				tbFileSizeTo.Enabled = false;
			}
			else
			{
				tbFileSizeFrom.Enabled = true;
				tbFileSizeTo.Enabled = true;
			}

			//copyFilesToolStripMenuItem
			//moveFilesToolStripMenuItem
			//deleteFilesToolStripMenuItem
			//saveAsCSVToolStripMenuItem
			//btnOtherActions
			if (FilesFound)
			{
				copyFilesToolStripMenuItem.Enabled = true;
				moveFilesToolStripMenuItem.Enabled = true;
				deleteFilesToolStripMenuItem.Enabled = true;
				saveAsCSVToolStripMenuItem.Enabled = true;
				btnOtherActions.Enabled = true;
			}
			else
			{
				copyFilesToolStripMenuItem.Enabled = false;
				moveFilesToolStripMenuItem.Enabled = false;
				deleteFilesToolStripMenuItem.Enabled = false;
				saveAsCSVToolStripMenuItem.Enabled = false;
				btnOtherActions.Enabled = false;
			}
		}

		#endregion

		#region Check version
		private void checkVersion()
		{
			try
			{
				if (Properties.Settings.Default.EnableUpdateChecking)
				{
					DateTime lastCheck = Properties.Settings.Default.LastCheckedVersion;
					TimeSpan duration = DateTime.Now.Subtract(lastCheck);
					if (duration.TotalDays >= Utils.ParseInt(Properties.Settings.Default.UpdateCheckInterval, 99))
					{
						ve.StartWebRequest();
						Properties.Settings.Default.LastCheckedVersion = DateTime.Now;
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, ex.Message, ex);
			}
		}

		void ve_RetrievedVersion(object sender, PublishedVersionExtractor.PackageVersion version)
		{
			try
			{
				if (version.Version != null)
				{
					string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
					if (PublishedVersionExtractor.IsUpdateNeeded(currentVersion, version.Version))
					{
						if (MessageBox.Show("New version of dnGREP (" + version.Version + ") is available for download.\nWould you like to download it now?", "New version", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information) == DialogResult.Yes)
						{
							System.Diagnostics.Process.Start("http://code.google.com/p/dngrep/");
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, ex.Message, ex);
			}
		}
		#endregion

		public MainForm()
		{
			InitializeComponent();
			restoreSettings();
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(Properties.Settings.Default.SearchFolder))
			{
				tbFolderName.Text = Properties.Settings.Default.SearchFolder;
				FolderSelected = true;
			}
			SearchPatternEntered = !string.IsNullOrEmpty(tbSearchFor.Text);
			ReplacePatternEntered = !string.IsNullOrEmpty(tbReplaceWith.Text);
			IsPlainText = rbTextSearch.Checked;
			IsMultiline = cbMultiline.Checked;
			populateEncodings();
			ve.RetrievedVersion += new PublishedVersionExtractor.VersionExtractorHandler(ve_RetrievedVersion);
			checkVersion();

			changeState();
		}

		private void btnSelectFolder_Click(object sender, EventArgs e)
		{
			folderSelectDialog.SelectedPath = tbFolderName.Text;
			if (tbFolderName.Text == "")
			{
				string clipboard = Clipboard.GetText();
				try
				{
					if (Path.IsPathRooted(clipboard))
						folderSelectDialog.SelectedPath = clipboard;
				}
				catch (Exception ex)
				{
					// Ignore
				}
			}
			if (folderSelectDialog.ShowDialog() == DialogResult.OK &&
				Directory.Exists(folderSelectDialog.SelectedPath))
			{
				FolderSelected = true;
				tbFolderName.Text = folderSelectDialog.SelectedPath;
			}
		}

		private void btnSearch_Click(object sender, EventArgs e)
		{
			DoSearchInResults = false;
			if (!IsSearching && !workerSearchReplace.IsBusy)
			{
				lblStatus.Text = "Searching...";
				IsSearching = true;
				barProgressBar.Value = 0;
				tvSearchResult.Nodes.Clear();
				workerSearchReplace.RunWorkerAsync(SEARCH_KEY);
			}
		}

		private void searchInResultsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DoSearchInResults = true;
			if (!IsSearching && !workerSearchReplace.IsBusy)
			{
				lblStatus.Text = "Searching...";
				IsSearching = true;
				barProgressBar.Value = 0;
				tvSearchResult.Nodes.Clear();
				workerSearchReplace.RunWorkerAsync(SEARCH_KEY);
			}
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			if (IsSearching || IsReplacing)
			{
				GrepCore.CancelProcess = true;
			}
		}

		private void doSearchReplace(object sender, DoWorkEventArgs e)
		{
			try
			{
				if (!workerSearchReplace.CancellationPending)
				{
					timer = DateTime.Now;
					if (e.Argument == SEARCH_KEY)
					{
						int sizeFrom = 0;
						int sizeTo = 0;
						if (!IsAllSizes)
						{
							sizeFrom = Utils.ParseInt(tbFileSizeFrom.Text, 0);
							sizeTo = Utils.ParseInt(tbFileSizeTo.Text, 0);
						}
						
						string filePattern = "*.*";
						if (rbFileRegex.Checked)
							filePattern = ".*";

						if (!string.IsNullOrEmpty(tbFilePattern.Text))
							filePattern = tbFilePattern.Text;

						if (rbFileAsterisk.Checked)
							filePattern = filePattern.Replace("\\", "");													

						string[] files;

						if (DoSearchInResults)
						{
							List<string> filesFromSearch = new List<string>();
							foreach (GrepSearchResult result in searchResults)
							{
								if (!filesFromSearch.Contains(result.FileName))
								{
									filesFromSearch.Add(result.FileName);
								}
							}
							files = filesFromSearch.ToArray();
						}
						else
						{
							files = Utils.GetFileList(tbFolderName.Text, filePattern, rbFileRegex.Checked, cbIncludeSubfolders.Checked,
								cbIncludeHiddenFolders.Checked, sizeFrom, sizeTo);
						}
						GrepCore grep = new GrepCore();
						grep.ShowLinesInContext = Properties.Settings.Default.ShowLinesInContext;
						grep.LinesBefore = Properties.Settings.Default.ContextLinesBefore;
						grep.LinesAfter = Properties.Settings.Default.ContextLinesAfter;

						grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
						GrepSearchResult[] results = null;
						if (rbRegexSearch.Checked)
							results = grep.SearchRegex(files, tbSearchFor.Text, cbCaseSensitive.Checked, cbMultiline.Checked, CodePage);
						else if (rbXPathSearch.Checked)
							results = grep.SearchXPath(files, tbSearchFor.Text, CodePage);
						else 
							results = grep.SearchText(files, tbSearchFor.Text, cbCaseSensitive.Checked, cbMultiline.Checked, CodePage);

						grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
						searchResults = new List<GrepSearchResult>(results);
						e.Result = results.Length;
					}
					else
					{
						GrepCore grep = new GrepCore();
						grep.ShowLinesInContext = Properties.Settings.Default.ShowLinesInContext;
						grep.LinesBefore = Properties.Settings.Default.ContextLinesBefore;
						grep.LinesAfter = Properties.Settings.Default.ContextLinesAfter;

						grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
						List<string> files = new List<string>();
						foreach (GrepSearchResult result in searchResults)
						{
							files.Add(result.FileName);
						}

						if (rbRegexSearch.Checked)
							e.Result = grep.ReplaceRegex(files.ToArray(), tbFolderName.Text, tbSearchFor.Text, tbReplaceWith.Text, cbCaseSensitive.Checked, cbMultiline.Checked, CodePage);
						else if (rbXPathSearch.Checked)
							e.Result = grep.ReplaceXPath(files.ToArray(), tbFolderName.Text, tbSearchFor.Text, tbReplaceWith.Text, CodePage);
						else
							e.Result = grep.ReplaceText(files.ToArray(), tbFolderName.Text, tbSearchFor.Text, tbReplaceWith.Text, cbCaseSensitive.Checked, cbMultiline.Checked, CodePage);

						grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, ex.Message, ex);
				if (e.Argument == SEARCH_KEY)
					MessageBox.Show("Search failed! See error log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				else
					MessageBox.Show("Replace failed! See error log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		void grep_ProcessedFile(object sender, GrepCore.ProgressStatus progress)
		{
			workerSearchReplace.ReportProgress((int)(progress.ProcessedFiles * 100 / progress.TotalFiles), progress);
		}

		private void searchProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (!GrepCore.CancelProcess)
			{
				GrepCore.ProgressStatus progress = (GrepCore.ProgressStatus)e.UserState;
				barProgressBar.Value = e.ProgressPercentage;
				lblStatus.Text = "(" + progress.ProcessedFiles + " of " + progress.TotalFiles + ")";
			}
		}

		private void searchComplete(object sender, RunWorkerCompletedEventArgs e)
		{
			if (IsSearching)
			{
				if (!e.Cancelled)
				{
					TimeSpan duration = DateTime.Now.Subtract(timer);
					lblStatus.Text = "Search Complete - " + (int)e.Result + " files found in " + duration.TotalMilliseconds + "ms.";
				}
				else
				{
					lblStatus.Text = "Search Canceled";
				}
				barProgressBar.Value = 0;
				IsSearching = false;
				if (searchResults.Count > 0)
					FilesFound = true;
				else
					FilesFound = false;
			}
			else if (IsReplacing)
			{
				if (!e.Cancelled)
				{
					if (e.Result == null || ((int)e.Result) == -1)
					{
						lblStatus.Text = "Replace Failed.";
						MessageBox.Show("Replace failed! See error log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					else
					{
						lblStatus.Text = "Replace Complete - " + (int)e.Result + " files replaced.";
						CanUndo = true;
					}
				}
				else
				{
					lblStatus.Text = "Replace Canceled";
				}
				barProgressBar.Value = 0;
				IsReplacing = false;
				searchResults.Clear();
				FilesFound = false;
			}
			populateResults();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			GrepCore.CancelProcess = true;
			if (workerSearchReplace.IsBusy)
				workerSearchReplace.CancelAsync();
			populateSettings();
			Properties.Settings.Default.Save();
		}

		private void populateSettings()
		{
			Properties.Settings.Default.SearchRegex = rbRegexSearch.Checked;
			Properties.Settings.Default.SearchText = rbTextSearch.Checked;
			Properties.Settings.Default.SearchXPath = rbXPathSearch.Checked;
			Properties.Settings.Default.FileSearchRegex = rbFileRegex.Checked;
			Properties.Settings.Default.FileSearchAsterisk = rbFileAsterisk.Checked;
			Properties.Settings.Default.FilterAllSizes = rbFilterAllSizes.Checked;
			Properties.Settings.Default.FilterSpecificSize = rbFilterSpecificSize.Checked;
		}

		private void restoreSettings()
		{

			rbRegexSearch.Checked =Properties.Settings.Default.SearchRegex;
			rbTextSearch.Checked = Properties.Settings.Default.SearchText;
			rbXPathSearch.Checked = Properties.Settings.Default.SearchXPath;
			rbFileRegex.Checked = Properties.Settings.Default.FileSearchRegex;
			rbFileAsterisk.Checked = Properties.Settings.Default.FileSearchAsterisk;
			rbFilterAllSizes.Checked = Properties.Settings.Default.FilterAllSizes;
			rbFilterSpecificSize.Checked = Properties.Settings.Default.FilterSpecificSize;
		}

		private void populateResults()
		{
			tvSearchResult.Nodes.Clear();
			List<string> tempExtensionList = new List<string>();
			if (searchResults == null)
				return;

			// Populate icon list
			foreach (GrepSearchResult result in searchResults)
			{
				string ext = Path.GetExtension(result.FileName);
				if (!tempExtensionList.Contains(ext))
					tempExtensionList.Add(ext);
			}
			FileIcons.LoadImageList(tempExtensionList.ToArray());
			tvSearchResult.ImageList = FileIcons.SmallIconList;

			foreach (GrepSearchResult result in searchResults)
			{
				string displayedName = Path.GetFileName(result.FileName);
				if (Properties.Settings.Default.ShowFilePathInResults &&
					result.FileName.Contains(tbFolderName.Text + "\\"))
				{
					displayedName = result.FileName.Substring(tbFolderName.Text.Length + 1);
				}
				int lineCount = Utils.MatchCount(result);
				if (lineCount > 0)
					displayedName = string.Format("{0} ({1})", displayedName, lineCount);
				
				TreeNode node = new TreeNode(displayedName);
				node.Tag = result.FileName;
				tvSearchResult.Nodes.Add(node);				
				string ext = Path.GetExtension(result.FileName);

				node.ImageKey = ext;
				node.SelectedImageKey = node.ImageKey;
				node.StateImageKey = node.ImageKey;
				if (result.SearchResults != null)
				{
					foreach (GrepSearchResult.GrepLine line in result.SearchResults)
					{
						string lineSummary = line.LineText.Replace("\n", "").Replace("\t", "").Replace("\r", "").Trim();
						if (lineSummary.Length == 0)
							lineSummary = " ";
						else if (lineSummary.Length > 100)
							lineSummary = lineSummary.Substring(0, 100) + "...";
						string lineNumber = (line.LineNumber == -1 ? "" : line.LineNumber + ": ");
						TreeNode lineNode = new TreeNode(lineNumber + lineSummary);
						lineNode.ImageKey = "%line%";
						lineNode.SelectedImageKey = lineNode.ImageKey;
						lineNode.StateImageKey = lineNode.ImageKey;
						lineNode.Tag = line.LineNumber;
						if (!line.IsContext && Properties.Settings.Default.ShowLinesInContext)
						{
							lineNode.ForeColor = Color.Red;
						}
						node.Nodes.Add(lineNode);
					}
				}
			}			
		}

		private void populateEncodings()
		{
			KeyValuePair<string, int> defaultValue = new KeyValuePair<string,int>("Auto detection (default)",-1);
			
			foreach (EncodingInfo ei in Encoding.GetEncodings())
			{
				Encoding e = ei.GetEncoding();
				encodings.Add(new KeyValuePair<string, int>(e.EncodingName, e.CodePage));
			}

			encodings.Sort(new KeyValueComparer());

			encodings.Insert(0,defaultValue);

			cbEncoding.DataSource = encodings;
			cbEncoding.ValueMember = "Value";
			cbEncoding.DisplayMember = "Key";

			cbEncoding.SelectedIndex = 0;
		}

		private void formKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				Close();
		}

		private void rbFilterSizes_CheckedChanged(object sender, EventArgs e)
		{
			if (rbFilterAllSizes.Checked)
				IsAllSizes = true;
			else
				IsAllSizes = false;
		}

		private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OptionsForm options = new OptionsForm();
			options.ShowDialog();
			changeState();
		}

		private void tvSearchResult_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
				tvSearchResult.SelectedNode = e.Node;
		}

		private void tvSearchResult_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			TreeNode selectedNode = tvSearchResult.SelectedNode;
			if (selectedNode != null && selectedNode.Nodes.Count == 0)
			{
				openToolStripMenuItem_Click(tvContextMenu, null);
			}
		}

		private void btnReplace_Click(object sender, EventArgs e)
		{
			if (!IsReplacing && !IsSearching && !workerSearchReplace.IsBusy)
			{
				if (!ReplacePatternEntered)
				{
					if (MessageBox.Show("Are you sure you want to replace search pattern with empty string?", "Replace", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) != DialogResult.Yes)
						return;
				}
				string[] roFiles = Utils.GetReadOnlyFiles(searchResults);
				if (roFiles.Length > 0)
				{
					StringBuilder sb = new StringBuilder("Some of the files are read only. If you continue, the application will 'force-replace' values in these files.\nWould you like to continue?\n\n");
					foreach (string fileName in roFiles)
					{
						sb.AppendLine(" - " + new FileInfo(fileName).Name);
					}
					if (MessageBox.Show(sb.ToString(), "Replace", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) != DialogResult.Yes)
						return;
				}
				lblStatus.Text = "Replacing...";
				IsReplacing = true;
				CanUndo = false;
				undoFolder = tbFolderName.Text;
				barProgressBar.Value = 0;
				tvSearchResult.Nodes.Clear();
				workerSearchReplace.RunWorkerAsync(REPLACE_KEY);
			}
		}

		private void textBoxTextChanged(object sender, EventArgs e)
		{
			SearchPatternEntered = !string.IsNullOrEmpty(tbSearchFor.Text);
			ReplacePatternEntered = !string.IsNullOrEmpty(tbReplaceWith.Text);
			if (sender == tbSearchFor)
			{
				FilesFound = false;
			}
		}

		private void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (CanUndo)
			{
				DialogResult response = MessageBox.Show("Undo will revert modified file(s) back to their original state. Any changes made to the file(s) after the replace will be overwritten. Are you sure you want to procede?", "Undo", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button3);
				if (response == DialogResult.Yes)
				{
					GrepCore core = new GrepCore();
					core.ShowLinesInContext = Properties.Settings.Default.ShowLinesInContext;
					core.LinesBefore = Properties.Settings.Default.ContextLinesBefore;
					core.LinesAfter = Properties.Settings.Default.ContextLinesAfter;

					bool result = core.Undo(undoFolder);
					if (result)
						MessageBox.Show("Files have been successfully reverted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
					else
						MessageBox.Show("There was an error reverting files. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
					CanUndo = false;
				}
			}
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AboutForm about = new AboutForm();
			about.ShowDialog();
		}

		private void onHelpRequested(object sender, HelpEventArgs hlpevent)
		{
			hlpevent.Handled = false;
		}

		private void helpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Help.ShowHelp(this, helpProvider.HelpNamespace);
		}

		private void regexText_CheckedChanged(object sender, EventArgs e)
		{
			if (sender == rbTextSearch)
				IsPlainText = rbTextSearch.Checked;

			FilesFound = false;
		}

		private void cbCaseSensitive_CheckedChanged(object sender, EventArgs e)
		{
			FilesFound = false;
		}

		private void btnTest_Click(object sender, EventArgs e)
		{
			try
			{
				RegexTest rTest = new RegexTest(tbSearchFor.Text, tbReplaceWith.Text);
				rTest.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show("There was an error running regex test. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void cbEncoding_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cbEncoding.SelectedValue != null && cbEncoding.SelectedValue is int)
				CodePage = (int)cbEncoding.SelectedValue;
		}

		private void cbMultiline_CheckedChanged(object sender, EventArgs e)
		{
			IsMultiline = cbMultiline.Checked;
		}

		private void openToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			BookmarksForm form = new BookmarksForm();
			form.Show();
		}

		private void btnBookmark_Click(object sender, EventArgs e)
		{
			BookmarkDetails bookmarkEditForm = new BookmarkDetails(CreateOrEdit.Create);
			Bookmark newBookmark = new Bookmark(tbSearchFor.Text, tbReplaceWith.Text, tbFilePattern.Text, "");
			bookmarkEditForm.Bookmark = newBookmark;
			if (bookmarkEditForm.ShowDialog() == DialogResult.OK)
			{
				if (!BookmarkLibrary.Instance.Bookmarks.Contains(newBookmark))
				{
					BookmarkLibrary.Instance.Bookmarks.Add(newBookmark);
					BookmarkLibrary.Save();
				}
			}			
		}

		private void addToolStripMenuItem_Click(object sender, EventArgs e)
		{
			btnBookmark_Click(this, null);
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TreeNode selectedNode = tvSearchResult.SelectedNode;
			if (selectedNode != null)
			{
				// Line was selected
				int lineNumber = 0;
				if (selectedNode.Parent != null)
				{
					if (selectedNode.Tag != null && selectedNode.Tag is int)
					{
						lineNumber = (int)selectedNode.Tag;
					}
					selectedNode = selectedNode.Parent;
				}
				if (selectedNode != null && selectedNode.Tag != null)
				{
					Utils.OpenFile((string)selectedNode.Tag, lineNumber, Properties.Settings.Default.UseCustomEditor, Properties.Settings.Default.CustomEditor, Properties.Settings.Default.CustomEditorArgs);
				}
			}
		}


		private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			tvSearchResult.ExpandAll();
		}

		#region Advance actions
		private void copyFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (FilesFound) {
				if (folderSelectDialog.ShowDialog() == DialogResult.OK)
				{
					try
					{
						if (!Utils.CanCopyFiles(searchResults, folderSelectDialog.SelectedPath))
						{
							MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							return;
						}

						Utils.CopyFiles(searchResults, tbFolderName.Text, folderSelectDialog.SelectedPath, true);
						MessageBox.Show("Files have been successfully copied.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show("There was an error copying files. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					CanUndo = false;
				}
			}
		}

		private void moveFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (FilesFound)
			{
				if (folderSelectDialog.ShowDialog() == DialogResult.OK)
				{
					try
					{
						if (!Utils.CanCopyFiles(searchResults, folderSelectDialog.SelectedPath))
						{
							MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							return;
						}

						Utils.CopyFiles(searchResults, tbFolderName.Text, folderSelectDialog.SelectedPath, true);
						Utils.DeleteFiles(searchResults);
						MessageBox.Show("Files have been successfully moved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show("There was an error moving files. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					CanUndo = false;
					searchResults = new List<GrepSearchResult>();
					populateResults();
					FilesFound = false;
				}
			}
		}

		private void deleteFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (FilesFound)
			{
				try
				{
					if (MessageBox.Show("Attention, you are about to delete files found during search.\nAre you sure you want to procede?", "Attention", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) != DialogResult.Yes)
					{
						return;
					}

					Utils.DeleteFiles(searchResults);
					MessageBox.Show("Files have been successfully deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show("There was an error deleting files. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				CanUndo = false;
				searchResults = new List<GrepSearchResult>();
				populateResults();
				FilesFound = false;
			}
		}

		private void saveAsCSVToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (FilesFound)
			{
				saveFileDialog.InitialDirectory = folderSelectDialog.SelectedPath;
				if (saveFileDialog.ShowDialog() == DialogResult.OK)
				{
					try
					{
						Utils.SaveResultsAsCSV(searchResults, saveFileDialog.FileName);
						MessageBox.Show("CSV file has been successfully created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show("There was an error creating a CSV file. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void btnOtherActions_Click(object sender, EventArgs e)
		{
			otherMenu.Show(btnOtherActions, new Point(0, btnOtherActions.Height));
		}
		#endregion

		private void openContainingFolderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TreeNode selectedNode = tvSearchResult.SelectedNode;
			if (selectedNode != null)
			{
				// Line was selected
				int lineNumber = 0;
				if (selectedNode.Parent != null)
				{
					if (selectedNode.Tag != null && selectedNode.Tag is int)
					{
						lineNumber = (int)selectedNode.Tag;
					}
					selectedNode = selectedNode.Parent;
				}
				if (selectedNode != null && selectedNode.Tag != null)
				{
					Utils.OpenContainingFolder((string)selectedNode.Tag, lineNumber);
				}
			}
		}
	}
}