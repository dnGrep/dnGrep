using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using dnGREP.Common;
using dnGREP.Engines;
using NLog;
using System.IO;
using System.Reflection;

namespace dnGREP.WPF
{
    /// <summary>
	/// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class MainForm : Window
    {
		private List<GrepSearchResult> searchResults = new List<GrepSearchResult>();
		private List<KeyValuePair<string, int>> encodings = new List<KeyValuePair<string, int>>();
		private const string SEARCH_KEY = "search";
		private const string REPLACE_KEY = "replace";
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private DateTime timer = DateTime.Now;
		private PublishedVersionExtractor ve = new PublishedVersionExtractor();
		private List<string> treeViewExtensionList = new List<string>();
		private FileFolderDialog fileFolderDialog = new FileFolderDialog();
		private BackgroundWorker workerSearchReplace = new BackgroundWorker();		

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
			set
			{
				searchPatternEntered = value;
				changeState();
			}
		}
		private bool replacePatternEntered = false;

		public bool ReplacePatternEntered
		{
			get { return replacePatternEntered; }
			set
			{
				replacePatternEntered = value;
				changeState();
			}
		}

		private bool filesFound = false;

		public bool FilesFound
		{
			get { return filesFound; }
			set
			{
				filesFound = value;
				changeState();
			}
		}

		private bool isAllSizes = true;

		public bool IsAllSizes
		{
			get { return isAllSizes; }
			set
			{
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
			set
			{
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
			set
			{
				isMultiline = value;
				changeState();
			}
		}

		private _screenParameters p = new _screenParameters();

		private void changeState()
		{
			p.codePage = CodePage;
			p.rbFileAsterisk = rbFileAsterisk.IsChecked == true;
			p.rbFileRegex = rbFileRegex.IsChecked == true;
			p.rbFilterAllSizes = rbFilterAllSizes.IsChecked == true;
			p.rbFilterSpecificSize = rbFilterSpecificSize.IsChecked == true;
			if (rbRegex.IsChecked == true)
				p.searchType = SearchType.Regex;
			else if (rbText.IsChecked == true)
				p.searchType = SearchType.PlainText;
			else if (rbXPath.IsChecked == true)
				p.searchType = SearchType.XPath;
			else if (rbSoundex.IsChecked == true)
				p.searchType = SearchType.Soundex;
			p.tbFilePattern = tbFilePattern.Text;
			p.tbFileSizeFrom = tbFileSizeFrom.Text;
			p.tbFileSizeTo = tbFileSizeTo.Text;
			p.tbFolderName = tbFolderName.Text;
			p.tbReplaceWith = tbReplaceWith.Text;
			p.tbSearchFor = tbSearchFor.Text;
			p.cbIncludeSubfolders = cbIncludeSubfolders.IsChecked == true;
			p.cbIncludeHiddenFolders = cbIncludeHiddenFolders.IsChecked == true;
			p.cbCaseSensitive = cbCaseSensitive.IsChecked == true;
			p.cbMultiline = cbMultiline.IsChecked == true;

			//tbSearchFor
			//tbReplaceWith
			//splitContainer
			if (IsMultiline)
			{
				//TODO
				//tbSearchFor.Multiline = true;
				//tbReplaceWith.Multiline = true;
				//splitContainer.SplitterDistance = 180;
				//splitContainer.IsSplitterFixed = false;
			}
			else
			{
				//TODO
				//tbSearchFor.Multiline = false;
				//tbReplaceWith.Multiline = false;
				//splitContainer.SplitterDistance = 134;
				//splitContainer.IsSplitterFixed = true;
			}

			// btnSearch
			// searchInResultsToolStripMenuItem
			if (FolderSelected && !IsSearching && !IsReplacing &&
				(SearchPatternEntered || Properties.Settings.Default.AllowSearchingForFileNamePattern))
			{
				btnSearch.IsEnabled = true;
				searchInResultsToolStripMenuItem.IsEnabled = true;
			}
			else
			{
				btnSearch.IsEnabled = false;
				searchInResultsToolStripMenuItem.IsEnabled = false;
			}

			//btnSearch.ShowAdvance
			if (searchResults.Count > 0)
			{
				//TODO
				//btnSearch.ShowSplit = true;
			}
			else
			{
				//TODO
				//btnSearch.ShowSplit = false;
			}

			// btnReplace
			if (FolderSelected && FilesFound && !IsSearching && !IsReplacing
				&& SearchPatternEntered)
			{
				btnReplace.IsEnabled = true;
			}
			else
			{
				btnReplace.IsEnabled = false;
			}

			//btnCancel
			if (IsSearching)
			{
				btnCancel.IsEnabled = true;
			}
			else if (IsReplacing)
			{
				btnCancel.IsEnabled = true;
			}
			else
			{
				btnCancel.IsEnabled = false;
			}

			//undoToolStripMenuItem
			if (CanUndo)
			{
				undoToolStripMenuItem.IsEnabled = true;
			}
			else
			{
				undoToolStripMenuItem.IsEnabled = false;
			}

			//cbCaseSensitive
			if (rbXPath.IsChecked == true)
			{
				cbCaseSensitive.IsEnabled = false;
			}
			else
			{
				cbCaseSensitive.IsEnabled = true;
			}

			//btnTest
			if (!IsPlainText &&
				!rbXPath.IsChecked == true)
			{
				btnTest.IsEnabled = true;
			}
			else
			{
				btnTest.IsEnabled = false;
			}

			//cbMultiline
			if (rbXPath.IsChecked == true)
			{
				cbMultiline.IsEnabled = false;
			}
			else
			{
				cbMultiline.IsEnabled = true;
			}

			//tbFileSizeFrom
			//tbFileSizeTo
			if (IsAllSizes)
			{
				tbFileSizeFrom.IsEnabled = false;
				tbFileSizeTo.IsEnabled = false;
			}
			else
			{
				tbFileSizeFrom.IsEnabled = true;
				tbFileSizeTo.IsEnabled = true;
			}

			//copyFilesToolStripMenuItem
			//moveFilesToolStripMenuItem
			//deleteFilesToolStripMenuItem
			//saveAsCSVToolStripMenuItem
			//btnOtherActions
			if (FilesFound)
			{
				copyFilesToolStripMenuItem.IsEnabled = true;
				moveFilesToolStripMenuItem.IsEnabled = true;
				deleteFilesToolStripMenuItem.IsEnabled = true;
				saveAsCSVToolStripMenuItem.IsEnabled = true;
				btnOtherActions.IsEnabled = true;
			}
			else
			{
				copyFilesToolStripMenuItem.IsEnabled = false;
				moveFilesToolStripMenuItem.IsEnabled = false;
				deleteFilesToolStripMenuItem.IsEnabled = false;
				saveAsCSVToolStripMenuItem.IsEnabled = false;
				btnOtherActions.IsEnabled = false;
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
						if (MessageBox.Show("New version of dnGREP (" + version.Version + ") is available for download.\nWould you like to download it now?", "New version", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
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

		#region Structure for passing paparemeters to another thread
		private struct _screenParameters
		{
			public string tbFolderName;
			public SearchType searchType;
			public string tbSearchFor;
			public string tbReplaceWith;
			public bool rbFilterAllSizes;
			public bool rbFilterSpecificSize;
			public string tbFileSizeFrom;
			public string tbFileSizeTo;
			public string tbFilePattern;
			public bool rbFileRegex;
			public bool rbFileAsterisk;
			public int codePage;
			public string key;
			public bool cbIncludeSubfolders;
			public bool cbIncludeHiddenFolders;
			public bool cbCaseSensitive;
			public bool cbMultiline;
		}
		#endregion

		private void winFormControlsInit()
		{
			this.workerSearchReplace.WorkerReportsProgress = true;
			this.workerSearchReplace.WorkerSupportsCancellation = true;
			this.workerSearchReplace.DoWork += new System.ComponentModel.DoWorkEventHandler(this.doSearchReplace);
			this.workerSearchReplace.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.searchComplete);
			this.workerSearchReplace.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.searchProgressChanged);
		}

		public MainForm()
		{
			InitializeComponent();
			winFormControlsInit();
			restoreSettings();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty(Properties.Settings.Default.SearchFolder))
			{
				tbFolderName.Text = Properties.Settings.Default.SearchFolder;
				FolderSelected = true;
			}
			SearchPatternEntered = !string.IsNullOrEmpty(tbSearchFor.Text);
			ReplacePatternEntered = !string.IsNullOrEmpty(tbReplaceWith.Text);
			IsPlainText = rbText.IsChecked == true;
			IsMultiline = cbMultiline.IsChecked == true;
			populateEncodings();
			ve.RetrievedVersion += new PublishedVersionExtractor.VersionExtractorHandler(ve_RetrievedVersion);
			checkVersion();

			changeState();
		}

		private void btnBrowse_Click(object sender, RoutedEventArgs e)
		{
			fileFolderDialog.Dialog.Multiselect = true;
			fileFolderDialog.SelectedPath = Utils.GetBaseFolder(tbFolderName.Text);
			if (tbFolderName.Text == "")
			{
				string clipboard = Clipboard.GetText();
				try
				{
					if (System.IO.Path.IsPathRooted(clipboard))
						fileFolderDialog.SelectedPath = clipboard;
				}
				catch (Exception ex)
				{
					// Ignore
				}
			}
			if (fileFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				if (fileFolderDialog.SelectedPaths != null)
					tbFolderName.Text = fileFolderDialog.SelectedPaths;
				else
					tbFolderName.Text = fileFolderDialog.SelectedPath;
			}
		}

		private void tbFolderName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (Utils.IsPathValid(tbFolderName.Text))
			{
				FolderSelected = true;
			}
			else
			{
				FolderSelected = false;
			}
		}

		private void btnSearch_Click(object sender, RoutedEventArgs e)
		{
			DoSearchInResults = false;
			
			if (!IsSearching && !workerSearchReplace.IsBusy)
			{
				lblStatus.Text = "Searching...";
				IsSearching = true;
				barProgressBar.Value = 0;
				//TODO
				//tvSearchResult.Nodes.Clear();
				p.key = SEARCH_KEY;
			
				workerSearchReplace.RunWorkerAsync(p);
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
				//TODO
				//tvSearchResult.Nodes.Clear();
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
					_screenParameters param = (_screenParameters)e.Argument;
					if (param.key == SEARCH_KEY)
					{
						int sizeFrom = 0;
						int sizeTo = 0;
						if (!IsAllSizes)
						{
							sizeFrom = Utils.ParseInt(param.tbFileSizeFrom, 0);
							sizeTo = Utils.ParseInt(param.tbFileSizeTo, 0);
						}

						string filePattern = "*.*";
						if (param.rbFileRegex)
							filePattern = ".*";

						if (!string.IsNullOrEmpty(param.tbFilePattern))
							filePattern = param.tbFilePattern;

						if (param.rbFileAsterisk)
							filePattern = filePattern.Replace("\\", "");

						string[] files;

						if (DoSearchInResults)
						{
							List<string> filesFromSearch = new List<string>();
							foreach (GrepSearchResult result in searchResults)
							{
								if (!filesFromSearch.Contains(result.FileNameReal))
								{
									filesFromSearch.Add(result.FileNameReal);
								}
							}
							files = filesFromSearch.ToArray();
						}
						else
						{
							files = Utils.GetFileList(param.tbFolderName, filePattern, param.rbFileRegex, param.cbIncludeSubfolders,
								param.cbIncludeHiddenFolders, sizeFrom, sizeTo);
						}
						GrepCore grep = new GrepCore();
						grep.ShowLinesInContext = Properties.Settings.Default.ShowLinesInContext;
						grep.LinesBefore = Properties.Settings.Default.ContextLinesBefore;
						grep.LinesAfter = Properties.Settings.Default.ContextLinesAfter;
						grep.PreviewFilesDuringSearch = Properties.Settings.Default.PreviewResults;

						grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
						List<GrepSearchResult> results = null;
						if (param.searchType == SearchType.Regex)
							results = grep.Search(files, SearchType.Regex, param.tbSearchFor, param.cbCaseSensitive, param.cbMultiline, param.codePage);
						else if (param.searchType == SearchType.XPath)
							results = grep.Search(files, SearchType.XPath, param.tbSearchFor, true, true, param.codePage);
						else
							results = grep.Search(files, SearchType.PlainText, param.tbSearchFor, param.cbCaseSensitive, param.cbMultiline, param.codePage);

						grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
						if (results != null)
						{
							searchResults = new List<GrepSearchResult>(results);
							e.Result = results.Count;
						}
						else
						{
							searchResults = new List<GrepSearchResult>();
							e.Result = 0;
						}
					}
					else
					{
						GrepCore grep = new GrepCore();
						grep.ShowLinesInContext = Properties.Settings.Default.ShowLinesInContext;
						grep.LinesBefore = Properties.Settings.Default.ContextLinesBefore;
						grep.LinesAfter = Properties.Settings.Default.ContextLinesAfter;
						grep.PreviewFilesDuringSearch = Properties.Settings.Default.PreviewResults;

						grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
						List<string> files = new List<string>();
						foreach (GrepSearchResult result in searchResults)
						{
							if (!result.ReadOnly)
								files.Add(result.FileNameReal);
						}

						if (param.searchType == SearchType.Regex)
							e.Result = grep.Replace(files.ToArray(), SearchType.Regex, Utils.GetBaseFolder(param.tbFolderName), param.tbSearchFor, param.tbReplaceWith, param.cbCaseSensitive, param.cbMultiline, param.codePage);
						else if (param.searchType == SearchType.XPath)
							e.Result = grep.Replace(files.ToArray(), SearchType.XPath, Utils.GetBaseFolder(param.tbFolderName), param.tbSearchFor, param.tbReplaceWith, true, true, param.codePage);
						else
							e.Result = grep.Replace(files.ToArray(), SearchType.PlainText, Utils.GetBaseFolder(param.tbFolderName), param.tbSearchFor, param.tbReplaceWith, param.cbCaseSensitive, param.cbMultiline, param.codePage);

						grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, ex.Message, ex);
				if (e.Argument == SEARCH_KEY)
					MessageBox.Show("Search failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				else
					MessageBox.Show("Replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
				if (progress.SearchResults != null)
				{
					searchResults.AddRange(progress.SearchResults);
					for (int i = 0; i < progress.SearchResults.Count; i++)
					{
						appendResults(progress.SearchResults[i]);
					}
				}
			}
		}

		private void searchComplete(object sender, RunWorkerCompletedEventArgs e)
		{
			if (IsSearching)
			{
				if (e.Result == null)
				{
					lblStatus.Text = "Search Failed";
				} 
				else if (!e.Cancelled)
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
						MessageBox.Show("Replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
			//TODO
			//if (!Properties.Settings.Default.PreviewResults || tvSearchResult.Nodes.Count != searchResults.Count)
			//    populateResults();

			string outdatedEngines = dnGREP.Engines.GrepEngineFactory.GetListOfFailedEngines();
			if (!string.IsNullOrEmpty(outdatedEngines))
			{
				MessageBox.Show("The following plugins failed to load:\n\n" + outdatedEngines + "\n\nDefault engine was used instead.", "Plugin Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void MainForm_Closing(object sender, CancelEventArgs e)
		{
			GrepCore.CancelProcess = true;
			if (workerSearchReplace.IsBusy)
				workerSearchReplace.CancelAsync();
			populateSettings();
			Properties.Settings.Default.Save();
		}

		private void populateSettings()
		{
			Properties.Settings.Default.SearchRegex = rbRegex.IsChecked == true;
			Properties.Settings.Default.SearchText = rbText.IsChecked == true;
			Properties.Settings.Default.SearchXPath = rbXPath.IsChecked == true;
			Properties.Settings.Default.FileSearchRegex = rbFileRegex.IsChecked == true;
			Properties.Settings.Default.FileSearchAsterisk = rbFileAsterisk.IsChecked == true;
			Properties.Settings.Default.FilterAllSizes = rbFilterAllSizes.IsChecked == true;
			Properties.Settings.Default.FilterSpecificSize = rbFilterSpecificSize.IsChecked == true;
		}

		private void restoreSettings()
		{

			rbRegex.IsChecked = Properties.Settings.Default.SearchRegex;
			rbText.IsChecked = Properties.Settings.Default.SearchText;
			rbXPath.IsChecked = Properties.Settings.Default.SearchXPath;
			rbFileRegex.IsChecked = Properties.Settings.Default.FileSearchRegex;
			rbFileAsterisk.IsChecked = Properties.Settings.Default.FileSearchAsterisk;
			rbFilterAllSizes.IsChecked = Properties.Settings.Default.FilterAllSizes;
			rbFilterSpecificSize.IsChecked = Properties.Settings.Default.FilterSpecificSize;
		}

		private void appendResults(GrepSearchResult result)
		{
			//if (result == null)
			//    return;

			//// Populate icon list
			//string ext = System.IO.Path.GetExtension(result.FileNameDisplayed);
			//if (!treeViewExtensionList.Contains(ext))
			//{
			//    treeViewExtensionList.Add(ext);
			//    FileIcons.LoadImageList(treeViewExtensionList.ToArray());
			//    tvSearchResult.ImageList = FileIcons.SmallIconList;
			//}

			//bool isFileReadOnly = Utils.IsReadOnly(result);
			//string displayedName = Path.GetFileName(result.FileNameDisplayed);
			//if (Properties.Settings.Default.ShowFilePathInResults &&
			//    result.FileNameDisplayed.Contains(Utils.GetBaseFolder(tbFolderName.Text) + "\\"))
			//{
			//    displayedName = result.FileNameDisplayed.Substring(Utils.GetBaseFolder(tbFolderName.Text).Length + 1);
			//}
			//int lineCount = Utils.MatchCount(result);
			//if (lineCount > 0)
			//    displayedName = string.Format("{0} ({1})", displayedName, lineCount);
			//if (isFileReadOnly)
			//    displayedName = displayedName + " [read-only]";

			//TreeNode node = new TreeNode(displayedName);
			//node.Tag = result;
			//tvSearchResult.Nodes.Add(node);

			//node.ImageKey = ext;
			//node.SelectedImageKey = node.ImageKey;
			//node.StateImageKey = node.ImageKey;
			//if (isFileReadOnly)
			//{
			//    node.ForeColor = Color.DarkGray;
			//}

			//if (result.SearchResults != null)
			//{
			//    for (int i = 0; i < result.SearchResults.Count; i++)
			//    {
			//        GrepSearchResult.GrepLine line = result.SearchResults[i];
			//        string lineSummary = line.LineText.Replace("\n", "").Replace("\t", "").Replace("\r", "").Trim();
			//        if (lineSummary.Length == 0)
			//            lineSummary = " ";
			//        else if (lineSummary.Length > 100)
			//            lineSummary = lineSummary.Substring(0, 100) + "...";
			//        string lineNumber = (line.LineNumber == -1 ? "" : line.LineNumber + ": ");
			//        TreeNode lineNode = new TreeNode(lineNumber + lineSummary);
			//        lineNode.ImageKey = "%line%";
			//        lineNode.SelectedImageKey = lineNode.ImageKey;
			//        lineNode.StateImageKey = lineNode.ImageKey;
			//        lineNode.Tag = line.LineNumber;
			//        if (!line.IsContext && Properties.Settings.Default.ShowLinesInContext)
			//        {
			//            lineNode.ForeColor = Color.Red;
			//        }
			//        node.Nodes.Add(lineNode);
			//    }
			//}
		}

		private void populateResults()
		{
			//tvSearchResult.Nodes.Clear();
			//if (searchResults == null)
			//    return;

			//for (int i = 0; i < searchResults.Count; i++)
			//{
			//    appendResults(searchResults[i]);
			//}
		}

		private void populateEncodings()
		{
			KeyValuePair<string, int> defaultValue = new KeyValuePair<string, int>("Auto detection (default)", -1);

			foreach (EncodingInfo ei in Encoding.GetEncodings())
			{
				Encoding e = ei.GetEncoding();
				encodings.Add(new KeyValuePair<string, int>(e.EncodingName, e.CodePage));
			}

			encodings.Sort(new KeyValueComparer());

			encodings.Insert(0, defaultValue);

			cbEncoding.DataContext = encodings;
			cbEncoding.DisplayMemberPath = "Value";
			cbEncoding.SelectedValuePath = "Key";
			cbEncoding.SelectedIndex = 0;
		}

		private void formKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Close();
		}

		private void rbFilterSpecificSize_Click(object sender, RoutedEventArgs e)
		{
			if (rbFilterAllSizes.IsChecked == true)
				IsAllSizes = true;
			else
				IsAllSizes = false;
		}

		//TODO
		//private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
		//{
		//    OptionsForm options = new OptionsForm();
		//    options.ShowDialog();
		//    changeState();
		//}

		private void tvSearchResult_MouseDown(object sender, MouseButtonEventArgs e)
		{
			//TODO
			//if (e.RightButton == MouseButtonState.Pressed)
			//    tvSearchResult.SelectedNode = e.Node;
		}

		private void tvSearchResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//TODO
			//TreeNode selectedNode = tvSearchResult.SelectedNode;
			//if (selectedNode != null && selectedNode.Nodes.Count == 0)
			//{
			//    openToolStripMenuItem_Click(tvContextMenu, null);
			//}
		}

		private void btnReplace_Click(object sender, RoutedEventArgs e)
		{
			if (!IsReplacing && !IsSearching && !workerSearchReplace.IsBusy)
			{
				if (!ReplacePatternEntered)
				{
					if (MessageBox.Show("Are you sure you want to replace search pattern with empty string?", "Replace", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
						return;
				}
				List<string> roFiles = Utils.GetReadOnlyFiles(searchResults);
				if (roFiles.Count > 0)
				{
					StringBuilder sb = new StringBuilder("Some of the files can not be modified. If you continue, these files will be skipped.\nWould you like to continue?\n\n");
					foreach (string fileName in roFiles)
					{
						sb.AppendLine(" - " + new FileInfo(fileName).Name);
					}
					if (MessageBox.Show(sb.ToString(), "Replace", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
						return;
				}
				lblStatus.Text = "Replacing...";
				IsReplacing = true;
				CanUndo = false;
				undoFolder = Utils.GetBaseFolder(tbFolderName.Text);
				barProgressBar.Value = 0;
				//TODO
				//tvSearchResult.Nodes.Clear();
				p.key = REPLACE_KEY;
				workerSearchReplace.RunWorkerAsync(p);
			}
		}

		private void textBoxTextChanged(object sender, TextChangedEventArgs e)
		{
			SearchPatternEntered = !string.IsNullOrEmpty(tbSearchFor.Text);
			ReplacePatternEntered = !string.IsNullOrEmpty(tbReplaceWith.Text);
			if (sender == tbSearchFor)
			{
				FilesFound = false;
			}
		}

		private void undoToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (CanUndo)
			{
				MessageBoxResult response = MessageBox.Show("Undo will revert modified file(s) back to their original state. Any changes made to the file(s) after the replace will be overwritten. Are you sure you want to procede?", "Undo", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
				if (response == MessageBoxResult.Yes)
				{
					GrepCore core = new GrepCore();
					core.ShowLinesInContext = Properties.Settings.Default.ShowLinesInContext;
					core.LinesBefore = Properties.Settings.Default.ContextLinesBefore;
					core.LinesAfter = Properties.Settings.Default.ContextLinesAfter;
					core.PreviewFilesDuringSearch = Properties.Settings.Default.PreviewResults;

					bool result = core.Undo(undoFolder);
					if (result)
					{
						MessageBox.Show("Files have been successfully reverted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
						Utils.DeleteTempFolder();
					}
					else
					{
						MessageBox.Show("There was an error reverting files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
					}
					CanUndo = false;
				}
			}
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//TODO
			//AboutForm about = new AboutForm();
			//about.ShowDialog();
		}

		private void helpToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//Help.ShowHelp(this, helpProvider.HelpNamespace);
		}

		private void rbTextSearch_CheckedChanged(object sender, RoutedEventArgs e)
		{
			if (sender == rbText)
				IsPlainText = rbText.IsChecked == true;

			FilesFound = false;
		}

		private void cbCaseSensitive_CheckedChanged(object sender, RoutedEventArgs e)
		{
			FilesFound = false;
		}

		private void cbMultiline_CheckedChanged(object sender, RoutedEventArgs e)
		{
			IsMultiline = cbMultiline.IsChecked == true;
		}

		private void btnTest_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//try
			//{
			//    RegexTest rTest = new RegexTest(tbSearchFor.Text, tbReplaceWith.Text);
			//    rTest.Show();
			//}
			//catch (Exception ex)
			//{
			//    MessageBox.Show("There was an error running regex test. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
			//}
		}

		private void cbEncoding_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cbEncoding.SelectedValue != null && cbEncoding.SelectedValue is int)
				CodePage = (int)cbEncoding.SelectedValue;
		}

		//TODO
		//private void openToolStripMenuItem1_Click(object sender, EventArgs e)
		//{
		//    BookmarksForm form = new BookmarksForm();
		//    form.Show();
		//}

		private void btnBookmark_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//BookmarkDetails bookmarkEditForm = new BookmarkDetails(CreateOrEdit.Create);
			//Bookmark newBookmark = new Bookmark(tbSearchFor.Text, tbReplaceWith.Text, tbFilePattern.Text, "");
			//bookmarkEditForm.Bookmark = newBookmark;
			//if (bookmarkEditForm.ShowDialog() == DialogResult.OK)
			//{
			//    if (!BookmarkLibrary.Instance.Bookmarks.Contains(newBookmark))
			//    {
			//        BookmarkLibrary.Instance.Bookmarks.Add(newBookmark);
			//        BookmarkLibrary.Save();
			//    }
			//}
		}

		private void addToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//btnBookmark_Click(this, null);
		}

		private void openToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//TreeNode selectedNode = tvSearchResult.SelectedNode;
			//if (selectedNode != null)
			//{
			//    // Line was selected
			//    int lineNumber = 0;
			//    if (selectedNode.Parent != null)
			//    {
			//        if (selectedNode.Tag != null && selectedNode.Tag is int)
			//        {
			//            lineNumber = (int)selectedNode.Tag;
			//        }
			//        selectedNode = selectedNode.Parent;
			//    }
			//    if (selectedNode != null && selectedNode.Tag != null)
			//    {
			//        GrepSearchResult result = (GrepSearchResult)selectedNode.Tag;
			//        OpenFileArgs fileArg = new OpenFileArgs(result, lineNumber, Properties.Settings.Default.UseCustomEditor, Properties.Settings.Default.CustomEditor, Properties.Settings.Default.CustomEditorArgs);
			//        dnGREP.Engines.GrepEngineFactory.GetSearchEngine(result.FileNameReal, false, 0, 0).OpenFile(fileArg);
			//        if (fileArg.UseBaseEngine)
			//            Utils.OpenFile(new OpenFileArgs(result, lineNumber, Properties.Settings.Default.UseCustomEditor, Properties.Settings.Default.CustomEditor, Properties.Settings.Default.CustomEditorArgs));
			//    }
			//}
		}


		private void expandAllToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//tvSearchResult.ExpandAll();
		}

		#region Advance actions
		private void copyFilesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//if (FilesFound)
			//{
			//    if (folderSelectDialog.ShowDialog() == DialogResult.OK)
			//    {
			//        try
			//        {
			//            if (!Utils.CanCopyFiles(searchResults, folderSelectDialog.SelectedPath))
			//            {
			//                MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			//                return;
			//            }

			//            Utils.CopyFiles(searchResults, Utils.GetBaseFolder(tbFolderName.Text), folderSelectDialog.SelectedPath, true);
			//            MessageBox.Show("Files have been successfully copied.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			//        }
			//        catch (Exception ex)
			//        {
			//            MessageBox.Show("There was an error copying files. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
			//        }
			//        CanUndo = false;
			//    }
			//}
		}

		private void moveFilesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//if (FilesFound)
			//{
			//    if (folderSelectDialog.ShowDialog() == DialogResult.OK)
			//    {
			//        try
			//        {
			//            if (!Utils.CanCopyFiles(searchResults, folderSelectDialog.SelectedPath))
			//            {
			//                MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			//                return;
			//            }

			//            Utils.CopyFiles(searchResults, Utils.GetBaseFolder(tbFolderName.Text), folderSelectDialog.SelectedPath, true);
			//            Utils.DeleteFiles(searchResults);
			//            MessageBox.Show("Files have been successfully moved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			//        }
			//        catch (Exception ex)
			//        {
			//            MessageBox.Show("There was an error moving files. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
			//        }
			//        CanUndo = false;
			//        searchResults = new List<GrepSearchResult>();
			//        populateResults();
			//        FilesFound = false;
			//    }
			//}
		}

		private void deleteFilesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//if (FilesFound)
			//{
			//    try
			//    {
			//        if (MessageBox.Show("Attention, you are about to delete files found during search.\nAre you sure you want to procede?", "Attention", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) != DialogResult.Yes)
			//        {
			//            return;
			//        }

			//        Utils.DeleteFiles(searchResults);
			//        MessageBox.Show("Files have been successfully deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			//    }
			//    catch (Exception ex)
			//    {
			//        MessageBox.Show("There was an error deleting files. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
			//    }
			//    CanUndo = false;
			//    searchResults = new List<GrepSearchResult>();
			//    populateResults();
			//    FilesFound = false;
			//}
		}

		private void saveAsCSVToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//if (FilesFound)
			//{
			//    saveFileDialog.InitialDirectory = folderSelectDialog.SelectedPath;
			//    if (saveFileDialog.ShowDialog() == DialogResult.OK)
			//    {
			//        try
			//        {
			//            Utils.SaveResultsAsCSV(searchResults, saveFileDialog.FileName);
			//            MessageBox.Show("CSV file has been successfully created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			//        }
			//        catch (Exception ex)
			//        {
			//            MessageBox.Show("There was an error creating a CSV file. Please examine the error log.", "Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
			//        }
			//    }
			//}
		}

		private void btnOtherActions_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//otherMenu.Show(btnOtherActions, new Point(0, btnOtherActions.Height));
		}
		#endregion

		private void openContainingFolderToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//TODO
			//TreeNode selectedNode = tvSearchResult.SelectedNode;
			//if (selectedNode != null)
			//{
			//    // Line was selected
			//    int lineNumber = 0;
			//    if (selectedNode.Parent != null)
			//    {
			//        if (selectedNode.Tag != null && selectedNode.Tag is int)
			//        {
			//            lineNumber = (int)selectedNode.Tag;
			//        }
			//        selectedNode = selectedNode.Parent;
			//    }
			//    if (selectedNode != null && selectedNode.Tag != null)
			//    {
			//        Utils.OpenContainingFolder(((GrepSearchResult)selectedNode.Tag).FileNameReal, lineNumber);
			//    }
			//}
		}
	}
}
