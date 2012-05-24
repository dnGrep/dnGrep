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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using dnGREP.Common.UI;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Blue.Windows;

namespace dnGREP.WPF
{
    /// <summary>
	/// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class MainForm : Window
    {
		private List<KeyValuePair<string, int>> encodings = new List<KeyValuePair<string, int>>();
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private DateTime timer = DateTime.Now;
		private PublishedVersionExtractor ve = new PublishedVersionExtractor();
		private FileFolderDialogWin32 fileFolderDialog = new FileFolderDialogWin32();
		private BackgroundWorker workerSearchReplace = new BackgroundWorker();
        private MainViewModel inputData;
		private BookmarksForm bookmarkForm;
        private System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
        private System.Windows.Forms.HelpProvider helpProvider = new System.Windows.Forms.HelpProvider();
        private bool isVisible;
        private Preview preview;
        private StickyWindow stickyWindow;
		public GrepSettings settings
		{
			get { return GrepSettings.Instance; }
		}
        
		#region Check version
		private void checkVersion()
		{
			try
			{
				if (settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking))
				{
					DateTime lastCheck = settings.Get<DateTime>(GrepSettings.Key.LastCheckedVersion);
					TimeSpan duration = DateTime.Now.Subtract(lastCheck);
					if (duration.TotalDays >= settings.Get<int>(GrepSettings.Key.UpdateCheckInterval))
					{
						ve.StartWebRequest();
						settings.Set<DateTime>(GrepSettings.Key.LastCheckedVersion, DateTime.Now);
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

        public MainForm()
            : this (true)
        {            
        }

        public MainForm(bool isVisible)
        {
            InitializeComponent();
            this.Width = Properties.Settings.Default.Width;
            this.Height = Properties.Settings.Default.Height;
            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            if (!UiUtils.IsOnScreen(this))
                UiUtils.CenterWindow(this);
            this.isVisible = isVisible;
        }

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwndNewParent);

        private const int HWND_MESSAGE = -3;

        private IntPtr hwnd;
        private IntPtr oldParent;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (!isVisible)
            {
                HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;

                if (hwndSource != null)
                {
                    hwnd = hwndSource.Handle;
                    oldParent = SetParent(hwnd, (IntPtr)HWND_MESSAGE);
                    Visibility = Visibility.Hidden;
                }
            }
        }

        public void UpdateState()
        {
            inputData.LoadAppSettings();
            inputData.UpdateState("");
        }

		private void winFormControlsInit()
		{
			this.workerSearchReplace.WorkerReportsProgress = true;
			this.workerSearchReplace.WorkerSupportsCancellation = true;
			this.workerSearchReplace.DoWork += new System.ComponentModel.DoWorkEventHandler(this.doSearchReplace);
			this.workerSearchReplace.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.searchComplete);
			this.workerSearchReplace.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.searchProgressChanged);
            this.saveFileDialog.Filter = "CSV file|*.csv";
            DiginesisHelpProvider.HelpNamespace = "Doc\\dnGREP.chm";
            DiginesisHelpProvider.ShowHelp = true;
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

			cbEncoding.ItemsSource = encodings;
			cbEncoding.DisplayMemberPath = "Key";
			cbEncoding.SelectedValuePath = "Value";
			cbEncoding.SelectedIndex = 0;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            stickyWindow = new StickyWindow(this);
            stickyWindow.StickToScreen = true;
            stickyWindow.StickToOther = true;
            stickyWindow.StickOnResize = true;
            stickyWindow.StickOnMove = true;

            inputData = new MainViewModel();
            this.DataContext = inputData;
            tvSearchResult.ItemsSource = inputData.SearchResults;
            
            winFormControlsInit();
			populateEncodings();
			ve.RetrievedVersion += new PublishedVersionExtractor.VersionExtractorHandler(ve_RetrievedVersion);
			checkVersion();
			inputData.UpdateState("");
            gridMain.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Auto);
            gridMain.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
		}

		void bookmarkForm_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "FilePattern")
				inputData.FilePattern = bookmarkForm.FilePattern;
			else if (e.PropertyName == "SearchFor")
				inputData.SearchFor = bookmarkForm.SearchFor;
			else if (e.PropertyName == "ReplaceWith")
				inputData.ReplaceWith = bookmarkForm.ReplaceWith;
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
			if (fileFolderDialog.ShowDialog() == true)
			{
				if (fileFolderDialog.SelectedPaths != null)
					inputData.FileOrFolderPath = fileFolderDialog.SelectedPaths;
				else
					inputData.FileOrFolderPath = fileFolderDialog.SelectedPath;
			}
		}

		private void btnSearch_Click(object sender, RoutedEventArgs e)
		{
			if (inputData.CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
			{
				inputData.CurrentGrepOperation = GrepOperation.Search;
				lblStatus.Text = "Searching...";
                if (preview != null)
                    preview.ResetTextEditor();
                barProgressBar.IsIndeterminate = true;
				inputData.SearchResults.Clear();
                Dictionary<string, object> workerParames = new Dictionary<string, object>();
                workerParames["State"] = inputData;
				workerSearchReplace.RunWorkerAsync(workerParames);
				// Update bookmarks
				if (!inputData.FastSearchBookmarks.Contains(tbSearchFor.Text))
				{
					inputData.FastSearchBookmarks.Insert(0, tbSearchFor.Text);
				}
				if (!inputData.FastFileMatchBookmarks.Contains(tbFilePattern.Text))
				{
					inputData.FastFileMatchBookmarks.Insert(0, tbFilePattern.Text);
				}
				if (!inputData.FastFileNotMatchBookmarks.Contains(tbFilePatternIgnore.Text))
				{
					inputData.FastFileNotMatchBookmarks.Insert(0, tbFilePatternIgnore.Text);
				}
				if (!inputData.FastPathBookmarks.Contains(tbFolderName.Text))
				{
					inputData.FastPathBookmarks.Insert(0, tbFolderName.Text);
				}
			}
		}

		private void btnSearchInResults_Click(object sender, RoutedEventArgs e)
		{
			if (inputData.CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
			{
				inputData.CurrentGrepOperation = GrepOperation.SearchInResults;
				lblStatus.Text = "Searching...";
                if (preview != null)
                    preview.ResetTextEditor();
                barProgressBar.IsIndeterminate = true;
                List<string> foundFiles = new List<string>();
                foreach (FormattedGrepResult n in inputData.SearchResults) foundFiles.Add(n.GrepResult.FileNameReal);
                Dictionary<string, object> workerParames = new Dictionary<string, object>();
                workerParames["State"] = inputData;
                workerParames["Files"] = foundFiles;
				inputData.SearchResults.Clear();
				workerSearchReplace.RunWorkerAsync(workerParames);
				// Update bookmarks
				if (!inputData.FastSearchBookmarks.Contains(tbSearchFor.Text))
				{
					inputData.FastSearchBookmarks.Insert(0, tbSearchFor.Text);
				}
				if (!inputData.FastFileMatchBookmarks.Contains(tbFilePattern.Text))
				{
					inputData.FastFileMatchBookmarks.Insert(0, tbFilePattern.Text);
				}
				if (!inputData.FastFileNotMatchBookmarks.Contains(tbFilePatternIgnore.Text))
				{
					inputData.FastFileNotMatchBookmarks.Insert(0, tbFilePatternIgnore.Text);
				}
				if (!inputData.FastPathBookmarks.Contains(tbFolderName.Text))
				{
					inputData.FastPathBookmarks.Insert(0, tbFolderName.Text);
				}
			}
		}

		private void btnReplace_Click(object sender, RoutedEventArgs e)
		{
			if (inputData.CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
			{
				if (string.IsNullOrEmpty(inputData.ReplaceWith))
				{
					if (MessageBox.Show("Are you sure you want to replace search pattern with empty string?", "Replace", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
						return;
				}
				List<string> roFiles = Utils.GetReadOnlyFiles(inputData.SearchResults.GetList());
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
                if (preview != null)
                    preview.ResetTextEditor();
				inputData.CurrentGrepOperation = GrepOperation.Replace;
				inputData.CanUndo = false;
				inputData.UndoFolder = Utils.GetBaseFolder(tbFolderName.Text);
                barProgressBar.IsIndeterminate = true;
                List<string> foundFiles = new List<string>();
                foreach (FormattedGrepResult n in inputData.SearchResults)
                {
                    if (!n.GrepResult.ReadOnly)
                        foundFiles.Add(n.GrepResult.FileNameReal);
                }
                Dictionary<string, object> workerParames = new Dictionary<string, object>();
                workerParames["State"] = inputData;
                workerParames["Files"] = foundFiles;
                inputData.SearchResults.Clear();
                workerSearchReplace.RunWorkerAsync(workerParames);
				// Update bookmarks
				if (!inputData.FastReplaceBookmarks.Contains(tbReplaceWith.Text))
				{
					inputData.FastReplaceBookmarks.Insert(0, tbReplaceWith.Text);
				}
				if (!inputData.FastFileMatchBookmarks.Contains(tbFilePattern.Text))
				{
					inputData.FastFileMatchBookmarks.Insert(0, tbFilePattern.Text);
				}
				if (!inputData.FastFileNotMatchBookmarks.Contains(tbFilePatternIgnore.Text))
				{
					inputData.FastFileNotMatchBookmarks.Insert(0, tbFilePatternIgnore.Text);
				}
				if (!inputData.FastPathBookmarks.Contains(tbFolderName.Text))
				{
					inputData.FastPathBookmarks.Insert(0, tbFolderName.Text);
				}
			}
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			if (inputData.CurrentGrepOperation != GrepOperation.None)
			{
				GrepCore.CancelProcess = true;
				Utils.CancelSearch = true;
			}
		}

		private void doSearchReplace(object sender, DoWorkEventArgs e)
		{
			try
			{
				if (!workerSearchReplace.CancellationPending)
				{
					timer = DateTime.Now;
                    Dictionary<string, object> workerParams = (Dictionary<string, object>)e.Argument;
                    MainViewModel param = (MainViewModel)workerParams["State"];
					if (param.CurrentGrepOperation == GrepOperation.Search || param.CurrentGrepOperation == GrepOperation.SearchInResults)
					{
						int sizeFrom = 0;
						int sizeTo = 0;
						if (param.UseFileSizeFilter == FileSizeFilter.Yes)
						{
							sizeFrom = param.SizeFrom;
							sizeTo = param.SizeTo;
						}

						string filePatternInclude = "*.*";
						if (param.TypeOfFileSearch == FileSearchType.Regex)
							filePatternInclude = ".*";

						if (!string.IsNullOrEmpty(param.FilePattern))
							filePatternInclude = param.FilePattern;

						if (param.TypeOfFileSearch == FileSearchType.Asterisk)
							filePatternInclude = filePatternInclude.Replace("\\", "");

						string filePatternExclude = "";
						if (!string.IsNullOrEmpty(param.FilePatternIgnore))
							filePatternExclude = param.FilePatternIgnore;

						if (param.TypeOfFileSearch == FileSearchType.Asterisk)
							filePatternExclude = filePatternExclude.Replace("\\", "");

						IEnumerable<string> files;

						Utils.CancelSearch = false;

						if (param.CurrentGrepOperation == GrepOperation.SearchInResults)
						{
                            files = (List<string>)workerParams["Files"];
						}
						else
						{
							files = Utils.GetFileListEx(inputData.FileOrFolderPath, filePatternInclude, filePatternExclude, param.TypeOfFileSearch == FileSearchType.Regex, param.IncludeSubfolder,
								param.IncludeHidden, param.IncludeBinary, sizeFrom, sizeTo);
						}

						if (Utils.CancelSearch)
						{
							e.Result = null;
							return;
						}

						if (param.TypeOfSearch == SearchType.Regex)
						{
							try
							{
								Regex pattern = new Regex(param.SearchFor);
							}
							catch (ArgumentException regException)
							{
								MessageBox.Show("Incorrect pattern: " + regException.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
								e.Result = null;
								return;
							}
						}

						GrepCore grep = new GrepCore();
						grep.SearchParams.FuzzyMatchThreshold = settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold);

                        GrepSearchOption searchOptions = GrepSearchOption.None;
                        if (inputData.Multiline)
                            searchOptions |= GrepSearchOption.Multiline;
                        if (inputData.CaseSensitive)
                            searchOptions |= GrepSearchOption.CaseSensitive;
                        if (inputData.Singleline)
                            searchOptions |= GrepSearchOption.SingleLine;
						if (inputData.WholeWord)
							searchOptions |= GrepSearchOption.WholeWord;

						grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
						List<GrepSearchResult> results = null;
						e.Result = grep.Search(files, param.TypeOfSearch, param.SearchFor, searchOptions, param.CodePage);

						grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
					}
					else
					{
						GrepCore grep = new GrepCore();
						grep.SearchParams.FuzzyMatchThreshold = settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold);

                        GrepSearchOption searchOptions = GrepSearchOption.None;
                        if (inputData.Multiline)
                            searchOptions |= GrepSearchOption.Multiline;
                        if (inputData.CaseSensitive)
                            searchOptions |= GrepSearchOption.CaseSensitive;
                        if (inputData.Singleline)
                            searchOptions |= GrepSearchOption.SingleLine;
						if (inputData.WholeWord)
							searchOptions |= GrepSearchOption.WholeWord;

						grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
                        string[] files = ((List<string>)workerParams["Files"]).ToArray();
                        e.Result = grep.Replace(files, param.TypeOfSearch, Utils.GetBaseFolder(param.FileOrFolderPath), param.SearchFor, param.ReplaceWith, searchOptions, param.CodePage);

						grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, ex.Message, ex);
				bool isSearch = true;
				if (e.Argument is MainViewModel)
				{
					MainViewModel param = (MainViewModel)e.Argument;
					if (param.CurrentGrepOperation == GrepOperation.Search || param.CurrentGrepOperation == GrepOperation.SearchInResults)
						isSearch = true;
					else
						isSearch = false;
				}
				if (isSearch)
					MessageBox.Show("Search failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				else
					MessageBox.Show("Replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		void grep_ProcessedFile(object sender, GrepCore.ProgressStatus progress)
		{
			workerSearchReplace.ReportProgress((int)progress.ProcessedFiles, progress);
		}

		private void searchProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			try
			{
				if (!GrepCore.CancelProcess)
				{
					GrepCore.ProgressStatus progress = (GrepCore.ProgressStatus)e.UserState;
                    string result = string.Empty;
                    if (progress.SearchResults != null)
                    {
                        inputData.SearchResults.AddRange(progress.SearchResults);
                        result = string.Format("Searched {0} files. Found {1} matching files.", progress.ProcessedFiles, inputData.SearchResults.Count);
                    }
                    else
                    {
                        result = string.Format("Searched {0} files.", progress.ProcessedFiles);
                    }

                    lblStatus.Text = result;
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, ex.Message, ex);
				MessageBox.Show("Search or replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void searchComplete(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				if (inputData.CurrentGrepOperation == GrepOperation.Search || inputData.CurrentGrepOperation == GrepOperation.SearchInResults)
				{
					List<GrepSearchResult> results = new List<GrepSearchResult>();
					if (e.Result == null)
					{
						lblStatus.Text = "Search Canceled or Failed";
					}
					else if (!e.Cancelled)
					{
						TimeSpan duration = DateTime.Now.Subtract(timer);
						results = (List<GrepSearchResult>)e.Result;
						lblStatus.Text = "Search Complete - " + results.Count + " files found in " + duration.TotalMilliseconds + "ms.";
					}
					else
					{
						lblStatus.Text = "Search Canceled";
					}
                    barProgressBar.IsIndeterminate = false;
					if (inputData.SearchResults.Count > 0)
						inputData.FilesFound = true;
					inputData.CurrentGrepOperation = GrepOperation.None;
				}
				else if (inputData.CurrentGrepOperation == GrepOperation.Replace)
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
							inputData.CanUndo = true;
						}
					}
					else
					{
						lblStatus.Text = "Replace Canceled";
					}
                    barProgressBar.IsIndeterminate = false;
					inputData.CurrentGrepOperation = GrepOperation.None;
					inputData.SearchResults.Clear();
				}

				string outdatedEngines = dnGREP.Engines.GrepEngineFactory.GetListOfFailedEngines();
				if (!string.IsNullOrEmpty(outdatedEngines))
				{
					MessageBox.Show("The following plugins failed to load:\n\n" + outdatedEngines + "\n\nDefault engine was used instead.", "Plugin Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, ex.Message, ex);
				MessageBox.Show("Search or replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				Utils.CancelSearch = false;
				inputData.UpdateState("");
			}			
		}

		private void MainForm_Closing(object sender, CancelEventArgs e)
		{
			GrepCore.CancelProcess = true;
			if (workerSearchReplace.IsBusy)
				workerSearchReplace.CancelAsync();
			copyBookmarksToSettings();
            Properties.Settings.Default.Width = (int)this.ActualWidth;
            Properties.Settings.Default.Height = (int)this.ActualHeight;
            Properties.Settings.Default.Top = (int)this.Top;
            Properties.Settings.Default.Left = (int)this.Left;
            if (preview != null)
            {
                GrepSettings.Instance.Set<System.Drawing.Rectangle>(GrepSettings.Key.PreviewWindowSize, preview.StickyWindow.OriginalForm.Bounds);
                GrepSettings.Instance.Set<StickyWindow.StickDir>(GrepSettings.Key.PreviewWindowPosition, preview.StickyWindow.IsStuckTo(stickyWindow.OriginalForm, true));
                preview.ForceClose();
            }
            Properties.Settings.Default.Save();
            settings.Save();
		}

		private void copyBookmarksToSettings()
		{
			//Saving bookmarks
			List<string> fsb = new List<string>();
			for (int i = 0; i < inputData.FastSearchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
			{
				fsb.Add(inputData.FastSearchBookmarks[i]);
			}
			settings.Set<List<string>>(GrepSettings.Key.FastSearchBookmarks, fsb);
			List<string> frb = new List<string>();
			for (int i = 0; i < inputData.FastReplaceBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
			{
				frb.Add(inputData.FastReplaceBookmarks[i]);
			}
			settings.Set<List<string>>(GrepSettings.Key.FastReplaceBookmarks, frb);
			List<string> ffmb = new List<string>();
			for (int i = 0; i < inputData.FastFileMatchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
			{
				ffmb.Add(inputData.FastFileMatchBookmarks[i]);
			}
			settings.Set<List<string>>(GrepSettings.Key.FastFileMatchBookmarks, ffmb);
			List<string> ffnmb = new List<string>();
			for (int i = 0; i < inputData.FastFileNotMatchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
			{
				ffnmb.Add(inputData.FastFileNotMatchBookmarks[i]);
			}
			settings.Set<List<string>>(GrepSettings.Key.FastFileNotMatchBookmarks, ffnmb);
			List<string> fpb = new List<string>();
			for (int i = 0; i < inputData.FastPathBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
			{
				fpb.Add(inputData.FastPathBookmarks[i]);
			}
			settings.Set<List<string>>(GrepSettings.Key.FastPathBookmarks, fpb);
		}

		private void formKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Close();
		}


		private void undoToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (inputData.CanUndo)
			{
				MessageBoxResult response = MessageBox.Show("Undo will revert modified file(s) back to their original state. Any changes made to the file(s) after the replace will be overwritten. Are you sure you want to procede?", 
					"Undo", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
				if (response == MessageBoxResult.Yes)
				{
					GrepCore core = new GrepCore();
					bool result = core.Undo(inputData.UndoFolder);
					if (result)
					{
						MessageBox.Show("Files have been successfully reverted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
						Utils.DeleteTempFolder();
					}
					else
					{
						MessageBox.Show("There was an error reverting files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
					}
					inputData.CanUndo = false;
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
            ApplicationCommands.Help.Execute(null, helpToolStripMenuItem);
		}

        private void aboutToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

		private void cbCaseSensitive_CheckedChanged(object sender, RoutedEventArgs e)
		{
			inputData.FilesFound = false;
		}

        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
			string fileOrFolderPath = inputData.FileOrFolderPath;
			string searchFor = inputData.SearchFor;
			string replaceWith = inputData.ReplaceWith;
			string filePattern = inputData.FilePattern;
			string filePatternIgnore = inputData.FilePatternIgnore;

			copyBookmarksToSettings();
			OptionsView optionsForm = new OptionsView();
            OptionsViewModel optionsViewModel = new OptionsViewModel();
            // When the ViewModel asks to be closed, 
            // close the window.
            EventHandler handler = null;
            handler = delegate
            {
                optionsViewModel.RequestClose -= handler;
                optionsForm.Close();
            };
            optionsViewModel.RequestClose += handler;
            optionsForm.DataContext = optionsViewModel;
			try
			{
				optionsForm.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show("There was an error saving options.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
				logger.LogException(LogLevel.Error, "Error saving options", ex);
			}
            inputData.LoadAppSettings();
			inputData.FileOrFolderPath = fileOrFolderPath;
			inputData.SearchFor = searchFor;
			inputData.ReplaceWith = replaceWith;
			inputData.FilePattern = filePattern;
			inputData.FilePatternIgnore = filePatternIgnore;
        }

		private void btnTest_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                TestPattern testForm = new TestPattern();
                testForm.ShowDialog();
                inputData.LoadAppSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error running regex test. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
				logger.LogException(LogLevel.Error, "Error running regex", ex);
            }
		}

		private void btnBookmarkOpen_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				bookmarkForm = new BookmarksForm();
				bookmarkForm.PropertyChanged += new PropertyChangedEventHandler(bookmarkForm_PropertyChanged);
				bookmarkForm.ShowDialog();
			}
			finally
			{
				bookmarkForm.PropertyChanged -= new PropertyChangedEventHandler(bookmarkForm_PropertyChanged);
			}
		}

		private void btnBookmark_Click(object sender, RoutedEventArgs e)
		{
			BookmarkDetails bookmarkEditForm = new BookmarkDetails(CreateOrEdit.Create);
			Bookmark newBookmark = new Bookmark(tbSearchFor.Text, tbReplaceWith.Text, tbFilePattern.Text, "");
			bookmarkEditForm.Bookmark = newBookmark;
			if (bookmarkEditForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
			    if (!BookmarkLibrary.Instance.Bookmarks.Contains(newBookmark))
			    {
			        BookmarkLibrary.Instance.Bookmarks.Add(newBookmark);
			        BookmarkLibrary.Save();
			    }
			}
		}

		#region Advance actions
		private void copyFilesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (inputData.FilesFound)
			{
				if (fileFolderDialog.ShowDialog() == true)
			    {
			        try
			        {
                        if (!Utils.CanCopyFiles(inputData.SearchResults.GetList(), Utils.GetBaseFolder(fileFolderDialog.SelectedPath)))
			            {
			                MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
			                return;
			            }

                        Utils.CopyFiles(inputData.SearchResults.GetList(), Utils.GetBaseFolder(tbFolderName.Text), Utils.GetBaseFolder(fileFolderDialog.SelectedPath), true);
                        MessageBox.Show("Files have been successfully copied.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
			        }
			        catch (Exception ex)
			        {
                        MessageBox.Show("There was an error copying files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
						logger.LogException(LogLevel.Error, "Error copying files", ex);
			        }
			        inputData.CanUndo = false;
			    }
			}
		}

		private void copyToClipboardToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			foreach (GrepSearchResult result in inputData.SearchResults.GetList())
			{
				sb.AppendLine(result.FileNameReal);
			}
			Clipboard.SetText(sb.ToString());
		}

		private void moveFilesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
            if (inputData.FilesFound)
            {
				if (fileFolderDialog.ShowDialog() == true)
                {
                    try
                    {
                        if (!Utils.CanCopyFiles(inputData.SearchResults.GetList(), Utils.GetBaseFolder(fileFolderDialog.SelectedPath)))
                        {
                            MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.", 
                                "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        Utils.CopyFiles(inputData.SearchResults.GetList(), Utils.GetBaseFolder(tbFolderName.Text), Utils.GetBaseFolder(fileFolderDialog.SelectedPath), true);
                        Utils.DeleteFiles(inputData.SearchResults.GetList());
                        MessageBox.Show("Files have been successfully moved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
						logger.LogException(LogLevel.Error, "Error moving files", ex);
                        MessageBox.Show("There was an error moving files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    inputData.CanUndo = false;
                    inputData.SearchResults.Clear();
                    inputData.FilesFound = false;
                }
            }
		}

		private void deleteFilesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
            if (inputData.FilesFound)
            {
                try
                {
                    if (MessageBox.Show("Attention, you are about to delete files found during search.\nAre you sure you want to procede?", "Attention", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) !=  MessageBoxResult.Yes)
                    {
                        return;
                    }

                    Utils.DeleteFiles(inputData.SearchResults.GetList());
                    MessageBox.Show("Files have been successfully deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was an error deleting files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
					logger.LogException(LogLevel.Error, "Error deleting files", ex);
                }
                inputData.CanUndo = false;
                inputData.SearchResults.Clear();
                inputData.FilesFound = false;
            }
		}

		private void saveAsCSVToolStripMenuItem_Click(object sender, RoutedEventArgs e)
		{
            if (inputData.FilesFound)
            {
                saveFileDialog.InitialDirectory = Utils.GetBaseFolder(inputData.FileOrFolderPath);
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        Utils.SaveResultsAsCSV(inputData.SearchResults.GetList(), saveFileDialog.FileName);
                        MessageBox.Show("CSV file has been successfully created.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("There was an error creating a CSV file. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
						logger.LogException(LogLevel.Error, "Error creating CSV file", ex);
                    }
                }
            }
		}

		private void btnOtherActions_Click(object sender, RoutedEventArgs e)
		{
            btnOtherActions.ContextMenu.IsOpen = true;
		}
		#endregion

        private void tvSearchResult_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null)
            {
                item.Focus();
                e.Handled = true;
            }
        }

        private void tvSearchResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine && 
				e.OriginalSource is TextBlock || e.OriginalSource is Run)
            {
                btnOpenFile_Click(sender, new RoutedEventArgs(e.RoutedEvent));
            }
		}

        private void tvSearchResults_SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (inputData.PreviewFileConent)
            {
                if (tvSearchResult.SelectedItem is FormattedGrepLine || tvSearchResult.SelectedItem is FormattedGrepResult)
                {
                    int lineNumber = 0;
                    FormattedGrepResult result = null;
                    if (tvSearchResult.SelectedItem is FormattedGrepLine)
                    {
                        FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                        // Line was selected
                        lineNumber = selectedNode.GrepLine.LineNumber;
                        result = selectedNode.Parent;
                    }
                    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
                    {
                        // File was selected
                        result = (FormattedGrepResult)tvSearchResult.SelectedItem;
                        lineNumber = 0;
                    }

                    if (preview == null)
                    {
                        preview = new Preview();
                        System.Drawing.Rectangle bounds = settings.Get<System.Drawing.Rectangle>(GrepSettings.Key.PreviewWindowSize);
                        if (bounds.Left == 0 && bounds.Right == 0)
                        {
                            preview.Height = this.ActualHeight;
                            preview.Left = this.Left + this.ActualWidth;
                            preview.Width = this.ActualWidth;
                            preview.Top = this.Top;
                        }
                        else
                        {
                            var stickyDir = GrepSettings.Instance.Get<StickyWindow.StickDir>(GrepSettings.Key.PreviewWindowPosition);
                            bounds = StickyWindow.PositionRelativeTo(stickyWindow.OriginalForm, stickyDir, bounds);
                            preview.Height = bounds.Height;
                            preview.Left = bounds.Left;
                            preview.Width = bounds.Width;
                            preview.Top = bounds.Top;
                        }
                    }
                    preview.Show(result.GrepResult.FileNameReal, result.GrepResult, lineNumber);
                }
            }
        }

		#region Tree right click events

		private void tvContexMenuOpening(object sender, RoutedEventArgs e)
		{
			if (tvSearchResult.SelectedItem is FormattedGrepLine)
			{
				btnCopyTreeItemClipboard.Header = "Line of text to clipboard";
				btnCopyFileNameClipboard.Visibility = Visibility.Collapsed;
			}
			else if (tvSearchResult.SelectedItem is FormattedGrepResult)
			{
				btnCopyTreeItemClipboard.Header = "Full file path to clipboard";
				btnCopyFileNameClipboard.Visibility = Visibility.Visible;
			}
		}

		private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tvSearchResult.SelectedItem is FormattedGrepLine)
                {
                    FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                    // Line was selected
                    int lineNumber = selectedNode.GrepLine.LineNumber;

                    FormattedGrepResult result = selectedNode.Parent;
                    OpenFileArgs fileArg = new OpenFileArgs(result.GrepResult, lineNumber, settings.Get<bool>(GrepSettings.Key.UseCustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                    dnGREP.Engines.GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, new GrepEngineInitParams(0.5)).OpenFile(fileArg);
                    if (fileArg.UseBaseEngine)
                        Utils.OpenFile(new OpenFileArgs(result.GrepResult, lineNumber, settings.Get<bool>(GrepSettings.Key.UseCustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
                }
                else if (tvSearchResult.SelectedItem is FormattedGrepResult)
                {
                    FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
                    // Line was selected
                    int lineNumber = 0;
                    OpenFileArgs fileArg = new OpenFileArgs(result.GrepResult, lineNumber, settings.Get<bool>(GrepSettings.Key.UseCustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                    dnGREP.Engines.GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, new GrepEngineInitParams(0.5)).OpenFile(fileArg);
                    if (fileArg.UseBaseEngine)
                        Utils.OpenFile(new OpenFileArgs(result.GrepResult, lineNumber, settings.Get<bool>(GrepSettings.Key.UseCustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
                }
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, "Failed to open file.", ex);
                if (settings.Get<bool>(GrepSettings.Key.UseCustomEditor))
                    MessageBox.Show("There was an error opening file by custom editor. \nCheck editor path via \"Options..\".", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("There was an error opening file. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnOpenContainingFolder_Click(object sender, RoutedEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
				//ShellIntegration.OpenFolder(selectedNode.Parent.GrepResult.FileNameReal);
                Utils.OpenContainingFolder(selectedNode.Parent.GrepResult.FileNameReal, selectedNode.GrepLine.LineNumber);				
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                FormattedGrepResult selectedNode = (FormattedGrepResult)tvSearchResult.SelectedItem;
				//ShellIntegration.OpenFolder(selectedNode.GrepResult.FileNameReal);
                Utils.OpenContainingFolder(selectedNode.GrepResult.FileNameReal, -1);
            }
        }

		private void btnShowFileProperties_Click(object sender, RoutedEventArgs e)
		{
			string fileName = "";
			if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                fileName = selectedNode.Parent.GrepResult.FileNameReal;
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                FormattedGrepResult selectedNode = (FormattedGrepResult)tvSearchResult.SelectedItem;
				fileName = selectedNode.GrepResult.FileNameReal;
            }

			if (fileName != "" && File.Exists(fileName))
				ShellIntegration.ShowFileProperties(fileName);
		}

        private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FormattedGrepResult result in tvSearchResult.Items)
            {
                result.IsExpanded = true;
            }
        }

        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FormattedGrepResult result in tvSearchResult.Items)
            {
                result.IsExpanded = false;
            }
        }

        private void btnExclude_Click(object sender, RoutedEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                inputData.SearchResults.Remove(selectedNode.Parent);
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                FormattedGrepResult selectedNode = (FormattedGrepResult)tvSearchResult.SelectedItem;
                inputData.SearchResults.Remove(selectedNode);
            }
        }

		private void copyToClipboard()
		{
			if (tvSearchResult.SelectedItem is FormattedGrepLine)
			{
				FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
				Clipboard.SetText(selectedNode.GrepLine.LineText);
			}
			else if (tvSearchResult.SelectedItem is FormattedGrepResult)
			{
				FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
				Clipboard.SetText(result.GrepResult.FileNameDisplayed);
			}
		}

		private void treeKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			{
				copyToClipboard();
			}
		}

		private void btnCopyTreeItemToClipboard_Click(object sender, RoutedEventArgs e)
		{
			copyToClipboard();
		}

		private void btnCopyNameToClipboard_Click(object sender, RoutedEventArgs e)
		{			
			if (tvSearchResult.SelectedItem is FormattedGrepLine)
			{
				FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
				Clipboard.SetText(System.IO.Path.GetFileName(selectedNode.Parent.GrepResult.FileNameDisplayed));
			}
			else if (tvSearchResult.SelectedItem is FormattedGrepResult)
			{
				FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
				Clipboard.SetText(System.IO.Path.GetFileName(result.GrepResult.FileNameDisplayed));
			}
		}

		#endregion

		private void TextBoxFocus(object sender, RoutedEventArgs e)
		{
			if (e.Source is TextBox)
			{
				((TextBox)e.Source).SelectAll();
			}
		}

		private void btnSearchFastBookmarks_Click(object sender, RoutedEventArgs e)
		{
			cbSearchFastBookmark.IsDropDownOpen = true;
            cbSearchFastBookmark.Focus();
		}

		private void btnReplaceFastBookmarks_Click(object sender, RoutedEventArgs e)
		{
			cbReplaceFastBookmark.IsDropDownOpen = true;
            cbReplaceFastBookmark.Focus();
			tbReplaceWith.SelectAll();            
		}

		#region DragDropEvents 
		private static UIElement _draggedElt;
		private static bool _isMouseDown = false;
		private static Point _dragStartPoint;

		private void tvSearchResult_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// Make this the new drag source
			_draggedElt = e.Source as UIElement;
			_dragStartPoint = e.GetPosition(getTopContainer());
			_isMouseDown = true;
		}

		private void tvSearchResult_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (_isMouseDown && isDragGesture(e.GetPosition(getTopContainer())))
			{
				treeDragStarted(sender as UIElement);
			}
		}

		private void treeDragStarted(UIElement uiElt)
		{
			_isMouseDown = false;
			Mouse.Capture(uiElt);

			DataObject data = new DataObject();
			
			if (tvSearchResult.SelectedItem is FormattedGrepLine)
			{
				FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
				data.SetData(DataFormats.Text, selectedNode.GrepLine.LineText);
			}
			else if (tvSearchResult.SelectedItem is FormattedGrepResult)
			{
				FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
				StringCollection files = new StringCollection();
				files.Add(result.GrepResult.FileNameReal);
				data.SetFileDropList(files);
			}


			DragDropEffects supportedEffects = DragDropEffects.Move | DragDropEffects.Copy;
			// Perform DragDrop
			DragDropEffects effects = System.Windows.DragDrop.DoDragDrop(_draggedElt, data, supportedEffects);

			// Clean up
			Mouse.Capture(null);
			_draggedElt = null;
		}

		private bool isDragGesture(Point point)
		{
			bool hGesture = Math.Abs(point.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance;
			bool vGesture = Math.Abs(point.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;

			return (hGesture | vGesture);
		}

		private UIElement getTopContainer()
		{
			return Application.Current.MainWindow.Content as UIElement;
		}

		private void tbFolderName_DragOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.All;
			e.Handled = true;
		}

		private void tbFolderName_Drop(object sender, DragEventArgs e)
		{
			if (e.Data is System.Windows.DataObject &&
			((System.Windows.DataObject)e.Data).ContainsFileDropList())
			{
				inputData.FileOrFolderPath = "";
				StringCollection fileNames = ((System.Windows.DataObject)e.Data).GetFileDropList();
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < fileNames.Count; i++)
				{
					sb.Append(fileNames[i]);
					if (i < (fileNames.Count - 1))
						sb.Append(";");
				}
				inputData.FileOrFolderPath = sb.ToString();
			}
		}
		#endregion

        private void cbMultiline_Unchecked(object sender, RoutedEventArgs e)
        {
            gridMain.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Auto);
            gridMain.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (preview != null && preview.IsVisible)
            {
                preview.Topmost = true;  // important
                preview.Topmost = false; // important
                preview.Focus();         // important
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (preview != null && preview.IsVisible)
            {
                if (this.WindowState != System.Windows.WindowState.Maximized)
                    preview.WindowState = this.WindowState;
            }
        }

        private void cbPreviewFile_Unchecked(object sender, RoutedEventArgs e)
        {
            if (preview != null)
                preview.Hide();
        }

        private void tbPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (!inputData.Multiline)
                {
                    if (sender != null && sender == tbSearchFor)
                    {
                        if (e.Key == Key.Down)
                            cbSearchFastBookmark.SelectedIndex++;
                        else
                        {
                            if (cbSearchFastBookmark.SelectedIndex > 0)
                                cbSearchFastBookmark.SelectedIndex--;
                        }
                    }
                    else if (sender != null && sender == tbReplaceWith)
                    {
                        if (e.Key == Key.Down)
                            cbReplaceFastBookmark.SelectedIndex++;
                        else
                        {
                            if (cbReplaceFastBookmark.SelectedIndex > 0)
                                cbReplaceFastBookmark.SelectedIndex--;
                        }
                    }
                }
            }
        }
	}
}
