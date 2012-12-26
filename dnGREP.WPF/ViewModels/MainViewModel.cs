using Blue.Windows;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Engines;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace dnGREP.WPF
{
    public class MainViewModel : BaseMainViewModel
	{
		public MainViewModel() : base()
		{
            SearchResults.PreviewFileLineRequest += searchResults_PreviewFileLineRequest;
            SearchResults.PreviewFileRequest += searchResults_PreviewFileRequest;
            SearchResults.OpenFileLineRequest += searchResults_OpenFileLineRequest;
            SearchResults.OpenFileRequest += searchResults_OpenFileRequest;

            ve.RetrievedVersion += ve_RetrievedVersion;
            this.RequestClose += MainViewModel_RequestClose;
            checkVersion();
            winFormControlsInit();
            populateEncodings();            
		}

        void searchResults_OpenFileRequest(object sender, MVHelpers.GrepResultEventArgs e)
        {
            OpenFile(e.FormattedGrepResult, e.UseCustomEditor);
        }

        void searchResults_OpenFileLineRequest(object sender, MVHelpers.GrepLineEventArgs e)
        {
            OpenFile(e.FormattedGrepLine, e.UseCustomEditor);
        }

        void searchResults_PreviewFileRequest(object sender, MVHelpers.GrepResultEventArgs e)
        {
            PreviewFile(e.FormattedGrepResult, e.ParentWindowSize);
        }

        void searchResults_PreviewFileLineRequest(object sender, MVHelpers.GrepLineEventArgs e)
        {
            PreviewFile(e.FormattedGrepLine, e.ParentWindowSize);
        }

        #region Private Variables and Properties
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DateTime timer = DateTime.Now;
        private PublishedVersionExtractor ve = new PublishedVersionExtractor();
        private FileFolderDialogWin32 fileFolderDialog = new FileFolderDialogWin32();
        private BackgroundWorker workerSearchReplace = new BackgroundWorker();
        private BookmarksForm bookmarkForm;
        private System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
        private PreviewView preview;
        private PreviewViewModel previewModel;
        private StickyWindow stickyWindow;

        #endregion

        #region Properties
        //private ObservableCollection<CodeSnippet> codeSnippets = new ObservableCollection<CodeSnippet>();
        //public ObservableCollection<CodeSnippet> CodeSnippets
        //{
        //    get { return codeSnippets; }
        //}
                
        public StickyWindow StickyWindow
        {
            get 
            {
                return stickyWindow; 
            }
            set
            {
                stickyWindow = value;
            }
        }

        #endregion

        #region Presentation Properties
        RelayCommand _undoCommand;
        /// <summary>
        /// Returns an undo command
        /// </summary>
        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand == null)
                {
                    _undoCommand = new RelayCommand(
                        param => this.undo(),
                        param => CanUndo
                        );
                }
                return _undoCommand;
            }
        }

        RelayCommand _optionsCommand;
        /// <summary>
        /// Returns an options command
        /// </summary>
        public ICommand OptionsCommand
        {
            get
            {
                if (_optionsCommand == null)
                {
                    _optionsCommand = new RelayCommand(
                        param => this.showOptions()
                        );
                }
                return _optionsCommand;
            }
        }
        RelayCommand _helpCommand;
        /// <summary>
        /// Returns a help command
        /// </summary>
        public ICommand HelpCommand
        {
            get
            {
                if (_helpCommand == null)
                {
                    _helpCommand = new RelayCommand(
                        param => this.showHelp()
                        );
                }
                return _helpCommand;
            }
        }
        RelayCommand _aboutCommand;
        /// <summary>
        /// Returns an about command
        /// </summary>
        public ICommand AboutCommand
        {
            get
            {
                if (_aboutCommand == null)
                {
                    _aboutCommand = new RelayCommand(
                        param => this.showAbout()
                        );
                }
                return _aboutCommand;
            }
        }
        RelayCommand _browseCommand;
        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BrowseCommand
        {
            get
            {
                if (_browseCommand == null)
                {
                    _browseCommand = new RelayCommand(
                        param => this.browse()
                        );
                }
                return _browseCommand;
            }
        }
        RelayCommand _searchCommand;
        /// <summary>
        /// Returns a command that starts a search.
        /// </summary>
        public ICommand SearchCommand
        {
            get
            {
                if (_searchCommand == null)
                {
                    _searchCommand = new RelayCommand(
                        param => this.search(),
                        param => this.CanSearch
                        );
                }
                return _searchCommand;
            }
        }
        RelayCommand _replaceCommand;
        /// <summary>
        /// Returns a command that starts a search in results.
        /// </summary>
        public ICommand ReplaceCommand
        {
            get
            {
                if (_replaceCommand == null)
                {
                    _replaceCommand = new RelayCommand(
                        param => this.replace(),
                        param => this.CanReplace
                        );
                }
                return _replaceCommand;
            }
        }
        RelayCommand _copyFilesCommand;
        /// <summary>
        /// Returns a command that copies files
        /// </summary>
        public ICommand CopyFilesCommand
        {
            get
            {
                if (_copyFilesCommand == null)
                {
                    _copyFilesCommand = new RelayCommand(
                        param => this.copyFiles()
                        );
                }
                return _copyFilesCommand;
            }
        }
        RelayCommand _moveFilesCommand;
        /// <summary>
        /// Returns a command that moves files
        /// </summary>
        public ICommand MoveFilesCommand
        {
            get
            {
                if (_moveFilesCommand == null)
                {
                    _moveFilesCommand = new RelayCommand(
                        param => this.moveFiles()
                        );
                }
                return _moveFilesCommand;
            }
        }
        RelayCommand _deleteFilesCommand;
        /// <summary>
        /// Returns a command that deletes files
        /// </summary>
        public ICommand DeleteFilesCommand
        {
            get
            {
                if (_deleteFilesCommand == null)
                {
                    _deleteFilesCommand = new RelayCommand(
                        param => this.deleteFiles()
                        );
                }
                return _deleteFilesCommand;
            }
        }
        RelayCommand _copyToClipboardCommand;
        /// <summary>
        /// Returns a command that copies content to clipboard
        /// </summary>
        public ICommand CopyToClipboardCommand
        {
            get
            {
                if (_copyToClipboardCommand == null)
                {
                    _copyToClipboardCommand = new RelayCommand(
                        param => this.copyToClipboard()
                        );
                }
                return _copyToClipboardCommand;
            }
        }
        RelayCommand _saveAsCsvCommand;
        /// <summary>
        /// Returns a command that copies content to clipboard
        /// </summary>
        public ICommand SaveAsCsvCommand
        {
            get
            {
                if (_saveAsCsvCommand == null)
                {
                    _saveAsCsvCommand = new RelayCommand(
                        param => this.saveAsCsv()
                        );
                }
                return _saveAsCsvCommand;
            }
        }
        RelayCommand _copyAsCsvCommand;
        /// <summary>
        /// Returns a command that copies content to clipboard
        /// </summary>
        public ICommand CopyAsCsvCommand
        {
            get
            {
                if (_copyAsCsvCommand == null)
                {
                    _copyAsCsvCommand = new RelayCommand(
                        param => this.copyAsCsvToClipboard()
                        );
                }
                return _copyAsCsvCommand;
            }
        }
        RelayCommand _cancelCommand;
        /// <summary>
        /// Returns a command that cancels search
        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                {
                    _cancelCommand = new RelayCommand(
                        param => this.cancel(),
                        param => CanCancel
                        );
                }
                return _cancelCommand;
            }
        }
        RelayCommand _testCommand;
        /// <summary>
        /// Returns a command that opens test view
        /// </summary>
        public ICommand TestCommand
        {
            get
            {
                if (_testCommand == null)
                {
                    _testCommand = new RelayCommand(
                        param => this.test()
                        );
                }
                return _testCommand;
            }
        }
        RelayCommand _bookmarkAddCommand;
        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BookmarkAddCommand
        {
            get
            {
                if (_bookmarkAddCommand == null)
                {
                    _bookmarkAddCommand = new RelayCommand(
                        param => this.bookmarkAddRemove()
                        );
                }
                return _bookmarkAddCommand;
            }
        }
        RelayCommand _bookmarkOpenCommand;
        /// <summary>
        /// Returns a command that opens file browse dialog.
        /// </summary>
        public ICommand BookmarkOpenCommand
        {
            get
            {
                if (_bookmarkOpenCommand == null)
                {
                    _bookmarkOpenCommand = new RelayCommand(
                        param => this.bookmarkOpen()
                        );
                }
                return _bookmarkOpenCommand;
            }
        }
        #endregion

        #region Public Methods
        public override void UpdateState(string name)
        {
            base.UpdateState(name);
            
            if (IsProperty(() => PreviewFileContent, name))
            {
                if (preview != null)
                    preview.Hide();
            }
        }

        public override void LoadSettings()
        {
            base.LoadSettings();            
        }

        public override void SaveSettings()
        {
            base.SaveSettings();
            settings.Save();
            copyBookmarksToSettings();
            if (preview != null)
            {
                settings.Set<System.Drawing.Rectangle>(GrepSettings.Key.PreviewWindowSize, preview.StickyWindow.OriginalForm.Bounds);
                settings.Set<StickyWindow.StickDir>(GrepSettings.Key.PreviewWindowPosition, preview.StickyWindow.IsStuckTo(stickyWindow.OriginalForm, true));
                preview.ForceClose();
            }         
        }

        public void OpenFile(FormattedGrepLine selectedNode, bool useCustomEditor)
        {
            try
            {
                // Line was selected
                int lineNumber = selectedNode.GrepLine.LineNumber;

                FormattedGrepResult result = selectedNode.Parent;
                OpenFileArgs fileArg = new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                dnGREP.Engines.GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, new GrepEngineInitParams(false, 0, 0, 0.5)).OpenFile(fileArg);
                if (fileArg.UseBaseEngine)
                    Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, "Failed to open file.", ex);
                if (useCustomEditor)
                    MessageBox.Show("There was an error opening file by custom editor. \nCheck editor path via \"Options..\".", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("There was an error opening file. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenFile(FormattedGrepResult result, bool useCustomEditor)
        {
            try
            {
                // Line was selected
                int lineNumber = 0;
                OpenFileArgs fileArg = new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs));
                dnGREP.Engines.GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, new GrepEngineInitParams(false, 0, 0, 0.5)).OpenFile(fileArg);
                if (fileArg.UseBaseEngine)
                    Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, "Failed to open file.", ex);
                if (useCustomEditor)
                    MessageBox.Show("There was an error opening file by custom editor. \nCheck editor path via \"Options..\".", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("There was an error opening file. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PreviewFile(FormattedGrepLine formattedGrepLine, RectangleF parentWindow)
        {
            if (PreviewFileContent)
            {
                int lineNumber = formattedGrepLine.GrepLine.LineNumber;
                FormattedGrepResult result = formattedGrepLine.Parent;
                previewFile(result.GrepResult.FileNameReal, result.GrepResult, lineNumber, parentWindow);
            }
        }

        public void PreviewFile(FormattedGrepResult formattedGrepResult, RectangleF parentWindow)
        {
            if (PreviewFileContent)
            {                
                previewFile(formattedGrepResult.GrepResult.FileNameReal, formattedGrepResult.GrepResult, 0, parentWindow);
            }            
        }

        public void ActivatePreviewWindow()
        {
            if (preview != null && preview.IsVisible)
            {
                preview.Topmost = true;  // important
                preview.Topmost = false; // important
                preview.Focus();         // important
            }
        }

        public void ChangePreviewWindowState(System.Windows.WindowState state)
        {
            if (preview != null && preview.IsVisible)
            {
                if (state != System.Windows.WindowState.Maximized)
                    preview.WindowState = state;
            }
        }

        public void SetCodeSnippets(ICollection<FormattedGrepResult> results)
        {
            foreach (var result in results)
            {
                StringBuilder blocks = new StringBuilder();
                List<int> lines = new List<int>();

                foreach (var block in Utils.GetSnippets(result.GrepResult,
                        settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                        settings.Get<int>(GrepSettings.Key.ContextLinesAfter)))
                {
                    blocks.AppendLine(block.Text);
                    blocks.AppendLine("...");
                    lines.AddRange(Utils.GetIntArray(block.FirstLineNumber, block.LineCount));
                    lines.Add(-1);
                }
                var previewViewModel = new SyntaxHighlighterViewModel();
                previewViewModel.Text = blocks.ToString().TrimEndOfLine();
                previewViewModel.LineNumbers = lines.ToArray();
                previewViewModel.SearchResult = result.GrepResult;
                previewViewModel.FileName = result.GrepResult.FileNameDisplayed;
                ContentPreviewModel = previewViewModel;
            }           
        }

        #endregion

        #region Private Methods

        private void MainViewModel_RequestClose(object sender, EventArgs e)
        {
            if (workerSearchReplace.IsBusy)
                workerSearchReplace.CancelAsync();
        }

        private void doSearchReplace(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!workerSearchReplace.CancellationPending)
                {
                    timer = DateTime.Now;
                    Dictionary<string, object> workerParams = (Dictionary<string, object>)e.Argument;
                    //TODO: Check if this is needed
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
                            files = Utils.GetFileListEx(FileOrFolderPath, filePatternInclude, filePatternExclude, param.TypeOfFileSearch == FileSearchType.Regex, param.IncludeSubfolder,
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
                        grep.SearchParams.LinesBefore = settings.Get<int>(GrepSettings.Key.ContextLinesBefore);
                        grep.SearchParams.LinesAfter = settings.Get<int>(GrepSettings.Key.ContextLinesAfter);
                        grep.SearchParams.ShowLinesInContext = settings.Get<bool>(GrepSettings.Key.ShowLinesInContext);

                        GrepSearchOption searchOptions = GrepSearchOption.None;
                        if (Multiline)
                            searchOptions |= GrepSearchOption.Multiline;
                        if (CaseSensitive)
                            searchOptions |= GrepSearchOption.CaseSensitive;
                        if (Singleline)
                            searchOptions |= GrepSearchOption.SingleLine;
                        if (WholeWord)
                            searchOptions |= GrepSearchOption.WholeWord;
                        if (StopAfterFirstMatch)
                            searchOptions |= GrepSearchOption.StopAfterFirstMatch;

                        grep.ProcessedFile += new GrepCore.SearchProgressHandler(grep_ProcessedFile);
                        e.Result = grep.Search(files, param.TypeOfSearch, param.SearchFor, searchOptions, param.CodePage);
                        grep.ProcessedFile -= new GrepCore.SearchProgressHandler(grep_ProcessedFile);
                    }
                    else
                    {
                        GrepCore grep = new GrepCore();
                        grep.SearchParams.FuzzyMatchThreshold = settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold);
                        grep.SearchParams.LinesBefore = settings.Get<int>(GrepSettings.Key.ContextLinesBefore);
                        grep.SearchParams.LinesAfter = settings.Get<int>(GrepSettings.Key.ContextLinesAfter);
                        grep.SearchParams.ShowLinesInContext = settings.Get<bool>(GrepSettings.Key.ShowLinesInContext);

                        GrepSearchOption searchOptions = GrepSearchOption.None;
                        if (Multiline)
                            searchOptions |= GrepSearchOption.Multiline;
                        if (CaseSensitive)
                            searchOptions |= GrepSearchOption.CaseSensitive;
                        if (Singleline)
                            searchOptions |= GrepSearchOption.SingleLine;
                        if (WholeWord)
                            searchOptions |= GrepSearchOption.WholeWord;
                        if (StopAfterFirstMatch)
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

        private void searchProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (!Utils.CancelSearch)
                {
                    GrepCore.ProgressStatus progress = (GrepCore.ProgressStatus)e.UserState;
                    string result = string.Empty;
                    if (progress.SearchResults != null)
                    {
                        SearchResults.AddRange(progress.SearchResults);
                        result = string.Format("Searched {0} files. Found {1} matching files.", progress.ProcessedFiles, SearchResults.Count);
                    }
                    else
                    {
                        result = string.Format("Searched {0} files.", progress.ProcessedFiles);
                    }

                    StatusMessage = result;
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
                if (CurrentGrepOperation == GrepOperation.Search || CurrentGrepOperation == GrepOperation.SearchInResults)
                {
                    List<GrepSearchResult> results = new List<GrepSearchResult>();
                    if (e.Result == null)
                    {
                        StatusMessage = "Search Canceled or Failed";
                    }
                    else if (!e.Cancelled)
                    {
                        TimeSpan duration = DateTime.Now.Subtract(timer);
                        results = (List<GrepSearchResult>)e.Result;
                        StatusMessage = "Search Complete - " + results.Count + " files found in " + duration.TotalMilliseconds + "ms.";
                    }
                    else
                    {
                        StatusMessage = "Search Canceled";
                    }
                    if (SearchResults.Count > 0)
                        FilesFound = true;
                    CurrentGrepOperation = GrepOperation.None;
                    base.OnPropertyChanged(() => CurrentGrepOperation);
                    CanSearch = true;
                }
                else if (CurrentGrepOperation == GrepOperation.Replace)
                {
                    if (!e.Cancelled)
                    {
                        if (e.Result == null || ((int)e.Result) == -1)
                        {
                            StatusMessage = "Replace Failed.";
                            MessageBox.Show("Replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            StatusMessage = "Replace Complete - " + (int)e.Result + " files replaced.";
                            CanUndo = true;
                        }
                    }
                    else
                    {
                        StatusMessage = "Replace Canceled";
                    }
                    CurrentGrepOperation = GrepOperation.None;
                    base.OnPropertyChanged(() => CurrentGrepOperation);
                    CanSearch = true;
                    SearchResults.Clear();
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
            }
        }

        void grep_ProcessedFile(object sender, GrepCore.ProgressStatus progress)
        {
            workerSearchReplace.ReportProgress((int)progress.ProcessedFiles, progress);
        }

        private void browse()
        {
            fileFolderDialog.Dialog.Multiselect = true;
            fileFolderDialog.SelectedPath = Utils.GetBaseFolder(FileOrFolderPath);
            if (FileOrFolderPath == "")
            {
                string clipboard = Clipboard.GetText();
                try
                {
                    if (System.IO.Path.IsPathRooted(clipboard))
                        fileFolderDialog.SelectedPath = clipboard;
                }
                catch
                {
                    // Ignore
                }
            }
            if (fileFolderDialog.ShowDialog() == true)
            {
                if (fileFolderDialog.SelectedPaths != null)
                    FileOrFolderPath = fileFolderDialog.SelectedPaths;
                else
                    FileOrFolderPath = fileFolderDialog.SelectedPath;
            }
        }

        private void search()
        {
            if (CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
            {
                if (SearchInResultsContent && CanSearchInResults)
                    CurrentGrepOperation = GrepOperation.SearchInResults;
                else
                    CurrentGrepOperation = GrepOperation.Search;
                StatusMessage = "Searching...";
                if (preview != null)
                    preview.ResetTextEditor();
                Dictionary<string, object> workerParames = new Dictionary<string, object>();
                if (SearchInResultsContent && CanSearchInResults)
                {
                    List<string> foundFiles = new List<string>();
                    foreach (FormattedGrepResult n in SearchResults) foundFiles.Add(n.GrepResult.FileNameReal);
                    workerParames["Files"] = foundFiles;
                }
                SearchResults.Clear();
                workerParames["State"] = this;
                workerSearchReplace.RunWorkerAsync(workerParames);
                updateBookmarks();
            }
        }

        private void replace()
        {
            if (CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
            {
                if (string.IsNullOrEmpty(ReplaceWith))
                {
                    if (MessageBox.Show("Are you sure you want to replace search pattern with empty string?", "Replace", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;
                }
                List<string> roFiles = Utils.GetReadOnlyFiles(SearchResults.GetList());
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
                StatusMessage = "Replacing...";
                if (preview != null)
                    preview.ResetTextEditor();
                CurrentGrepOperation = GrepOperation.Replace;
                CanUndo = false;
                UndoFolder = Utils.GetBaseFolder(FileOrFolderPath);
                List<string> foundFiles = new List<string>();
                foreach (FormattedGrepResult n in SearchResults)
                {
                    if (!n.GrepResult.ReadOnly)
                        foundFiles.Add(n.GrepResult.FileNameReal);
                }
                Dictionary<string, object> workerParames = new Dictionary<string, object>();
                workerParames["State"] = this;
                workerParames["Files"] = foundFiles;
                SearchResults.Clear();
                workerSearchReplace.RunWorkerAsync(workerParames);
                updateBookmarks();
            }
        }

        private void updateBookmarks()
        {
            // Update bookmarks
            if (!FastSearchBookmarks.Contains(SearchFor))
            {
                FastSearchBookmarks.Insert(0, SearchFor);
            }
            if (!FastFileMatchBookmarks.Contains(FilePattern))
            {
                FastFileMatchBookmarks.Insert(0, FilePattern);
            }
            if (!FastFileNotMatchBookmarks.Contains(FilePatternIgnore))
            {
                FastFileNotMatchBookmarks.Insert(0, FilePatternIgnore);
            }
            if (!FastPathBookmarks.Contains(FileOrFolderPath))
            {
                FastPathBookmarks.Insert(0, FileOrFolderPath);
            }
        }

        private void cancel()
        {
            if (CurrentGrepOperation != GrepOperation.None)
            {
                Utils.CancelSearch = true;
            }
        }

        private void undo()
        {
            if (CanUndo)
            {
                MessageBoxResult response = MessageBox.Show("Undo will revert modified file(s) back to their original state. Any changes made to the file(s) after the replace will be overwritten. Are you sure you want to procede?",
                    "Undo", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                if (response == MessageBoxResult.Yes)
                {
                    GrepCore core = new GrepCore();
                    bool result = core.Undo(UndoFolder);
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

        private void showOptions()
        {
            string fileOrFolderPath = FileOrFolderPath;
            string searchFor = SearchFor;
            string replaceWith = ReplaceWith;
            string filePattern = FilePattern;
            string filePatternIgnore = FilePatternIgnore;

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
            LoadSettings();
            FileOrFolderPath = fileOrFolderPath;
            SearchFor = searchFor;
            ReplaceWith = replaceWith;
            FilePattern = filePattern;
            FilePatternIgnore = filePatternIgnore;
            SearchResults.CustomEditorConfigured = true;
        }

        private void showHelp()
        {
            //TODO: Fix
            //ApplicationCommands.Help.Execute(null, helpToolStripMenuItem);
        }

        private void showAbout()
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void bookmarkAddRemove()
        {
            Bookmark newBookmark = new Bookmark(SearchFor, ReplaceWith, FilePattern, "");
            if (IsBookmarked)
            {
                if (!BookmarkLibrary.Instance.Bookmarks.Contains(newBookmark))
                    BookmarkLibrary.Instance.Bookmarks.Add(newBookmark);
                IsBookmarked = true;
            }
            else
            {
                if (BookmarkLibrary.Instance.Bookmarks.Contains(newBookmark))
                    BookmarkLibrary.Instance.Bookmarks.Remove(newBookmark);
                IsBookmarked = false;
            }
            BookmarkLibrary.Save();
        }

        private void bookmarkOpen()
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

        private void copyFiles()
        {
            if (FilesFound)
            {
                if (fileFolderDialog.ShowDialog() == true)
                {
                    try
                    {
                        if (!Utils.CanCopyFiles(SearchResults.GetList(), Utils.GetBaseFolder(fileFolderDialog.SelectedPath)))
                        {
                            MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        Utils.CopyFiles(SearchResults.GetList(), Utils.GetBaseFolder(FileOrFolderPath), Utils.GetBaseFolder(fileFolderDialog.SelectedPath), true);
                        MessageBox.Show("Files have been successfully copied.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("There was an error copying files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.LogException(LogLevel.Error, "Error copying files", ex);
                    }
                    CanUndo = false;
                }
            }
        }

        private void moveFiles()
        {
            if (FilesFound)
            {
                if (fileFolderDialog.ShowDialog() == true)
                {
                    try
                    {
                        if (!Utils.CanCopyFiles(SearchResults.GetList(), Utils.GetBaseFolder(fileFolderDialog.SelectedPath)))
                        {
                            MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.",
                                "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        Utils.CopyFiles(SearchResults.GetList(), Utils.GetBaseFolder(FileOrFolderPath), Utils.GetBaseFolder(fileFolderDialog.SelectedPath), true);
                        Utils.DeleteFiles(SearchResults.GetList());
                        MessageBox.Show("Files have been successfully moved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(LogLevel.Error, "Error moving files", ex);
                        MessageBox.Show("There was an error moving files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CanUndo = false;
                    SearchResults.Clear();
                    FilesFound = false;
                }
            }
        }

        private void deleteFiles()
        {
            if (FilesFound)
            {
                try
                {
                    if (MessageBox.Show("Attention, you are about to delete files found during search.\nAre you sure you want to procede?", "Attention", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    Utils.DeleteFiles(SearchResults.GetList());
                    MessageBox.Show("Files have been successfully deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was an error deleting files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    logger.LogException(LogLevel.Error, "Error deleting files", ex);
                }
                CanUndo = false;
                SearchResults.Clear();
                FilesFound = false;
            }
        }

        private void copyToClipboard()
        {
            StringBuilder sb = new StringBuilder();
            foreach (GrepSearchResult result in SearchResults.GetList())
            {
                sb.AppendLine(result.FileNameReal);
            }
            Clipboard.SetText(sb.ToString());
        }

        private void copyAsCsvToClipboard()
        {
            Clipboard.SetText(Utils.GetResultsAsCSV(SearchResults.GetList()));
        }

        private void saveAsCsv()
        {
            if (FilesFound)
            {
                saveFileDialog.InitialDirectory = Utils.GetBaseFolder(FileOrFolderPath);
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        Utils.SaveResultsAsCSV(SearchResults.GetList(), saveFileDialog.FileName);
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

        private void test()
        {
            try
            {
                TestPattern testForm = new TestPattern();
                testForm.ShowDialog();
                LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error running regex test. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.LogException(LogLevel.Error, "Error running regex", ex);
            }
        }

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

            List<KeyValuePair<string, int>> tempEnc = new List<KeyValuePair<string, int>>();
            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                Encoding e = ei.GetEncoding();
                tempEnc.Add(new KeyValuePair<string, int>(e.EncodingName, e.CodePage));
            }

            tempEnc.Sort(new KeyValueComparer());
            tempEnc.Insert(0, defaultValue);
            Encodings.Clear();
            foreach (var enc in tempEnc)
                Encodings.Add(enc);
        }

        void bookmarkForm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FilePattern")
                FilePattern = bookmarkForm.FilePattern;
            else if (e.PropertyName == "SearchFor")
                SearchFor = bookmarkForm.SearchFor;
            else if (e.PropertyName == "ReplaceWith")
                ReplaceWith = bookmarkForm.ReplaceWith;
        }

        private void copyBookmarksToSettings()
        {
            //Saving bookmarks
            List<string> fsb = new List<string>();
            for (int i = 0; i < FastSearchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                fsb.Add(FastSearchBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastSearchBookmarks, fsb);
            List<string> frb = new List<string>();
            for (int i = 0; i < FastReplaceBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                frb.Add(FastReplaceBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastReplaceBookmarks, frb);
            List<string> ffmb = new List<string>();
            for (int i = 0; i < FastFileMatchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                ffmb.Add(FastFileMatchBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastFileMatchBookmarks, ffmb);
            List<string> ffnmb = new List<string>();
            for (int i = 0; i < FastFileNotMatchBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                ffnmb.Add(FastFileNotMatchBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastFileNotMatchBookmarks, ffnmb);
            List<string> fpb = new List<string>();
            for (int i = 0; i < FastPathBookmarks.Count && i < MainViewModel.FastBookmarkCapacity; i++)
            {
                fpb.Add(FastPathBookmarks[i]);
            }
            settings.Set<List<string>>(GrepSettings.Key.FastPathBookmarks, fpb);
        }

        private void previewFile(string filePath, GrepSearchResult result, int line, RectangleF parentWindow)
        {
            if (PreviewFileContent)
            {
                if (previewModel == null)
                {
                    previewModel = new PreviewViewModel();
                }
            
                if (preview == null)
                {
                    preview = new PreviewView();
                    preview.DataContext = previewModel;
                    System.Drawing.Rectangle bounds = settings.Get<System.Drawing.Rectangle>(GrepSettings.Key.PreviewWindowSize);
                    if (bounds.Left == 0 && bounds.Right == 0)
                    {
                        preview.Height = parentWindow.Height;
                        preview.Left = parentWindow.Left + parentWindow.Width;
                        preview.Width = parentWindow.Width;
                        preview.Top = parentWindow.Top;
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
                previewModel.GrepResult = result;
                previewModel.LineNumber = line;
                previewModel.FilePath = filePath;
            }  
        }
        #endregion
	}
}
