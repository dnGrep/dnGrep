using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Engines;
using dnGREP.WPF.MVHelpers;
using Microsoft.Win32;
using NLog;

namespace dnGREP.WPF
{
    public class MainViewModel : BaseMainViewModel
    {
        public MainViewModel()
            : base()
        {
            SearchResults.PreviewFileLineRequest += SearchResults_PreviewFileLineRequest;
            SearchResults.PreviewFileRequest += SearchResults_PreviewFileRequest;
            SearchResults.OpenFileLineRequest += SearchResults_OpenFileLineRequest;
            SearchResults.OpenFileRequest += SearchResults_OpenFileRequest;

            RequestClose += MainViewModel_RequestClose;
            CheckVersion();
            ControlsInit();
            PopulateEncodings();

            idleTimer.Interval = TimeSpan.FromMilliseconds(250);
            idleTimer.Tick += IdleTimer_Tick;
        }

        void SearchResults_OpenFileRequest(object sender, MVHelpers.GrepResultEventArgs e)
        {
            OpenFile(e.FormattedGrepResult, e.UseCustomEditor);
        }

        void SearchResults_OpenFileLineRequest(object sender, MVHelpers.GrepLineEventArgs e)
        {
            OpenFile(e.FormattedGrepLine, e.UseCustomEditor);
        }

        void SearchResults_PreviewFileRequest(object sender, MVHelpers.GrepResultEventArgs e)
        {
            PreviewFile(e.FormattedGrepResult, e.ParentWindowSize);
        }

        void SearchResults_PreviewFileLineRequest(object sender, MVHelpers.GrepLineEventArgs e)
        {
            PreviewFile(e.FormattedGrepLine, e.ParentWindowSize);
        }

        #region Private Variables and Properties
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DateTime timer = DateTime.Now;
        private FileFolderDialogWin32 fileFolderDialog = new FileFolderDialogWin32();
        private BackgroundWorker workerSearchReplace = new BackgroundWorker();
        private BookmarksForm bookmarkForm;
        private PreviewView preview;
        private PreviewViewModel previewModel;
        private HashSet<string> currentSearchFiles = new HashSet<string>();
        private int processedFiles;
        private bool isSorted;
        private Dictionary<string, string> undoMap = new Dictionary<string, string>();
        private DispatcherTimer idleTimer = new DispatcherTimer(DispatcherPriority.ContextIdle);
        private string latestStatusMessage;

        #endregion

        #region Properties

        public Window ParentWindow { get; set; }

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
                        param => this.Undo(),
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
                        param => this.ShowOptions()
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
                        param => this.ShowHelp()
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
                        param => this.ShowAbout()
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
                        param => this.Browse()
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
                        param => this.Search(),
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
                        param => this.Replace(),
                        param => this.CanReplace
                        );
                }
                return _replaceCommand;
            }
        }
        RelayCommand _sortCommand;
        /// <summary>
        /// Returns a command that sorts the results.
        /// </summary>
        public ICommand SortCommand
        {
            get
            {
                if (_sortCommand == null)
                {
                    _sortCommand = new RelayCommand(
                        param => this.SortResults(),
                        param => this.CanSortResults
                        );
                }
                return _sortCommand;
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
                        param => this.CopyFiles()
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
                        param => this.MoveFiles()
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
                        param => this.DeleteFiles()
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
                        param => this.CopyToClipboard()
                        );
                }
                return _copyToClipboardCommand;
            }
        }
        RelayCommand _saveResultsCommand;
        /// <summary>
        /// Returns a command that copies content to clipboard
        /// </summary>
        public ICommand SaveResultsCommand
        {
            get
            {
                if (_saveResultsCommand == null)
                {
                    _saveResultsCommand = new RelayCommand(
                        param => SaveResultsToFile()
                        );
                }
                return _saveResultsCommand;
            }
        }
        RelayCommand _copyMatchingLinesCommand;
        /// <summary>
        /// Returns a command that copies matching lines to clipboard
        /// </summary>
        public ICommand CopyMatchingLinesCommand
        {
            get
            {
                if (_copyMatchingLinesCommand == null)
                {
                    _copyMatchingLinesCommand = new RelayCommand(
                        param => CopyResults()
                        );
                }
                return _copyMatchingLinesCommand;
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
                        param => this.Cancel(),
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
                        param => this.Test()
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
                        param => this.BookmarkAddRemove()
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
                        param => this.BookmarkOpen()
                        );
                }
                return _bookmarkOpenCommand;
            }
        }

        RelayCommand _resetOptionsCommand;
        /// <summary>
        /// Returns a command that resets the search options.
        /// </summary>
        public ICommand ResetOptionsCommand
        {
            get
            {
                if (_resetOptionsCommand == null)
                {
                    _resetOptionsCommand = new RelayCommand(
                        param => this.ResetOptions()
                        );
                }
                return _resetOptionsCommand;
            }
        }

        RelayCommand _toggleFileOptionsCommand;
        /// <summary>
        /// Returns a command that resets the search options.
        /// </summary>
        public ICommand ToggleFileOptionsCommand
        {
            get
            {
                if (_toggleFileOptionsCommand == null)
                {
                    _toggleFileOptionsCommand = new RelayCommand(
                        param => IsFiltersExpanded = !IsFiltersExpanded
                        );
                }
                return _toggleFileOptionsCommand;
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
                {
                    if (PreviewFileContent)
                    {
                        if (SearchResults.SelectedNodes.Count > 0)
                        {
                            var item = SearchResults.SelectedNodes[0];

                            if (item is FormattedGrepLine)
                            {
                                PreviewFile(item as FormattedGrepLine, this.ParentWindow.GetBoundsF());
                            }
                            else if (item is FormattedGrepResult)
                            {
                                PreviewFile(item as FormattedGrepResult, this.ParentWindow.GetBoundsF());
                            }
                        }
                    }
                    else
                        preview.Hide();
                }
            }
        }

        public override void SaveSettings()
        {
            CopyBookmarksToSettings();
            if (preview != null)
            {
                settings.Set<Rectangle>(GrepSettings.Key.PreviewWindowSize, preview.GetBounds());
                preview.SaveSettings();
            }

            base.SaveSettings();
        }

        protected override void CloseChildWindows()
        {
            if (preview != null)
            {
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
                IGrepEngine engine = GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, GrepEngineInitParams.Default, new FileFilter());
                if (engine != null)
                {
                    engine.OpenFile(fileArg);
                    GrepEngineFactory.ReturnToPool(result.GrepResult.FileNameReal, engine);
                }
                if (fileArg.UseBaseEngine)
                    Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, "Failed to open file.", ex);
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
                IGrepEngine engine = GrepEngineFactory.GetSearchEngine(result.GrepResult.FileNameReal, GrepEngineInitParams.Default, new FileFilter());
                if (engine != null)
                {
                    engine.OpenFile(fileArg);
                    GrepEngineFactory.ReturnToPool(result.GrepResult.FileNameReal, engine);
                }
                if (fileArg.UseBaseEngine)
                    Utils.OpenFile(new OpenFileArgs(result.GrepResult, result.GrepResult.Pattern, lineNumber, useCustomEditor, settings.Get<string>(GrepSettings.Key.CustomEditor), settings.Get<string>(GrepSettings.Key.CustomEditorArgs)));
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, "Failed to open file.", ex);
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
                PreviewFile(result.GrepResult.FileNameReal, result.GrepResult, lineNumber, parentWindow);
            }
        }

        public void PreviewFile(FormattedGrepResult formattedGrepResult, RectangleF parentWindow)
        {
            if (PreviewFileContent)
            {
                int lineNumber = 0;
                if (formattedGrepResult.GrepResult != null && formattedGrepResult.GrepResult.Matches.Count > 0)
                    lineNumber = formattedGrepResult.GrepResult.Matches[0].LineNumber;

                PreviewFile(formattedGrepResult.GrepResult.FileNameReal, formattedGrepResult.GrepResult, lineNumber, parentWindow);
            }
        }

        public void ChangePreviewWindowState(WindowState state)
        {
            if (preview != null && preview.IsVisible)
            {
                if (state != WindowState.Maximized)
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

        private void DoSearchReplace(object sender, DoWorkEventArgs e)
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

                        DateTime? startTime = null, endTime = null;
                        if (param.UseFileDateFilter != FileDateFilter.None)
                        {
                            if (param.TypeOfTimeRangeFilter == FileTimeRange.Dates)
                            {
                                startTime = param.StartDate;
                                endTime = param.EndDate;
                                // the end date should go through the end of the day
                                if (endTime.HasValue)
                                    endTime = endTime.Value.AddDays(1.0);
                            }
                            else if (param.TypeOfTimeRangeFilter == FileTimeRange.Hours)
                            {
                                int low = Math.Min(param.HoursFrom, param.HoursTo);
                                int high = Math.Max(param.HoursFrom, param.HoursTo);
                                startTime = DateTime.Now.AddHours(-1 * high);
                                endTime = DateTime.Now.AddHours(-1 * low);
                            }
                        }

                        string filePatternInclude = "*.*";
                        if (param.TypeOfFileSearch == FileSearchType.Regex)
                            filePatternInclude = ".*";
                        else if (param.TypeOfFileSearch == FileSearchType.Everything)
                            filePatternInclude = string.Empty;

                        if (!string.IsNullOrEmpty(param.FilePattern))
                            filePatternInclude = param.FilePattern;

                        string filePatternExclude = "";
                        if (!string.IsNullOrEmpty(param.FilePatternIgnore))
                            filePatternExclude = param.FilePatternIgnore;

                        IEnumerable<string> files;

                        Utils.CancelSearch = false;

                        FileFilter fileParams = new FileFilter(PathSearchText.CleanPath, filePatternInclude, filePatternExclude,
                            param.TypeOfFileSearch == FileSearchType.Regex, param.TypeOfFileSearch == FileSearchType.Everything,
                            param.IncludeSubfolder, param.IncludeHidden, param.IncludeBinary, param.IncludeArchive, sizeFrom,
                            sizeTo, param.UseFileDateFilter, startTime, endTime);

                        if (param.CurrentGrepOperation == GrepOperation.SearchInResults)
                        {
                            files = (List<string>)workerParams["Files"];
                        }
                        else
                        {
                            files = Utils.GetFileListEx(fileParams);
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
                        grep.SearchParams = new GrepEngineInitParams(
                            settings.Get<bool>(GrepSettings.Key.ShowLinesInContext),
                            settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                            settings.Get<int>(GrepSettings.Key.ContextLinesAfter),
                            settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                            settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
                            SearchParallel);

                        grep.FileFilter = new FileFilter(PathSearchText.CleanPath, filePatternInclude, filePatternExclude,
                            param.TypeOfFileSearch == FileSearchType.Regex, param.TypeOfFileSearch == FileSearchType.Everything,
                            param.IncludeSubfolder, param.IncludeHidden, param.IncludeBinary, param.IncludeArchive,
                            sizeFrom, sizeTo, param.UseFileDateFilter, startTime, endTime);

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

                        grep.ProcessedFile += GrepCore_ProcessedFile;
                        e.Result = grep.Search(files, param.TypeOfSearch, param.SearchFor, searchOptions, param.CodePage);
                        grep.ProcessedFile -= GrepCore_ProcessedFile;
                    }
                    else
                    {
                        GrepCore grep = new GrepCore();
                        grep.SearchParams = new GrepEngineInitParams(
                            settings.Get<bool>(GrepSettings.Key.ShowLinesInContext),
                            settings.Get<int>(GrepSettings.Key.ContextLinesBefore),
                            settings.Get<int>(GrepSettings.Key.ContextLinesAfter),
                            settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                            settings.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
                            SearchParallel);

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

                        grep.ProcessedFile += GrepCore_ProcessedFile;
                        var files = workerParams["Files"] as Dictionary<string, string>;
                        e.Result = grep.Replace(files, param.TypeOfSearch, param.SearchFor, param.ReplaceWith, searchOptions, param.CodePage);
                        grep.ProcessedFile -= GrepCore_ProcessedFile;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
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

        private object lockObjOne = new object();
        private void SearchProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (!Utils.CancelSearch)
                {
                    ProgressStatus progress = (ProgressStatus)e.UserState;

                    if (progress != null && !progress.BeginSearch && progress.SearchResults != null && progress.SearchResults.Count > 0)
                    {
                        lock (lockObjOne)
                        {
                            SearchResults.AddRange(progress.SearchResults);
                        }
                    }

                    UpdateStatus(progress);
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
                MessageBox.Show("Search or replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private object lockObjTwo = new object();
        private void UpdateStatus(ProgressStatus progress)
        {
            // When running in parallel, multiple files will be in progress at the same time.
            // This keeps track of the files that are running and the long running file names
            // are shown again as the short runs finish.
            lock (lockObjTwo)
            {
                string fileName = progress.FileName;
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    if (progress.BeginSearch)
                    {
                        if (!currentSearchFiles.Contains(fileName))
                            currentSearchFiles.Add(fileName);
                    }
                    else
                    {
                        if (currentSearchFiles.Contains(fileName))
                            currentSearchFiles.Remove(fileName);

                        if (currentSearchFiles.Count > 0)
                            fileName = currentSearchFiles.FirstOrDefault();
                    }
                }

                // keep the total count of processed files to report at completion
                processedFiles = Math.Max(processedFiles, progress.ProcessedFiles);

                // the search has gotten fast enough that setting the StatusMessage is slowing 
                // down the main thread.  Update the latest status message, but only update the 
                // UI periodically when the dispatcher is idle.
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    latestStatusMessage = $"Searched {progress.ProcessedFiles} files. Found {progress.SuccessfulFiles} matching files - processing {fileName}";
                }
                else
                {
                    latestStatusMessage = $"Searched {progress.ProcessedFiles} files. Found {progress.SuccessfulFiles} matching files.";
                }
            }
        }

        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(latestStatusMessage))
                StatusMessage = latestStatusMessage;
        }

        private void SearchComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            latestStatusMessage = string.Empty;
            idleTimer.Stop();
            try
            {
                if (CurrentGrepOperation == GrepOperation.Search || CurrentGrepOperation == GrepOperation.SearchInResults)
                {
                    if (e.Result == null)
                    {
                        StatusMessage = "Search Canceled or Failed";
                    }
                    else if (!e.Cancelled)
                    {
                        TimeSpan duration = DateTime.Now.Subtract(timer);
                        int successCount = 0;
                        var results = e.Result as List<GrepSearchResult>;
                        if (results != null)
                            successCount = results.Where(r => r.IsSuccess).Count();

                        StatusMessage = $"Search Complete - Searched {processedFiles} files. Found {successCount} files in {duration.GetPrettyString()}.";
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
                            StatusMessage = $"Replace Complete - {(int)e.Result} files replaced.";
                            CanUndo = undoMap.Count > 0;
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
                    isSorted = false;
                }

                string outdatedEngines = dnGREP.Engines.GrepEngineFactory.GetListOfFailedEngines();
                if (!string.IsNullOrEmpty(outdatedEngines))
                {
                    MessageBox.Show("The following plugins failed to load:\n\n" + outdatedEngines + "\n\nDefault engine was used instead.", "Plugin Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
                MessageBox.Show("Search or replace failed! See error log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Utils.CancelSearch = false;
                currentSearchFiles.Clear();
            }
        }

        void GrepCore_ProcessedFile(object sender, ProgressStatus progress)
        {
            workerSearchReplace.ReportProgress((int)progress.ProcessedFiles, progress);
        }

        private void Browse()
        {
            string filePattern = PathSearchText.FilePattern;

            fileFolderDialog.Dialog.Multiselect = true;
            fileFolderDialog.SelectedPath = PathSearchText.BaseFolder;
            if (string.IsNullOrWhiteSpace(PathSearchText.BaseFolder))
            {
                string clipboard = Clipboard.GetText();
                try
                {
                    if (Path.IsPathRooted(clipboard))
                        fileFolderDialog.SelectedPath = clipboard;
                }
                catch
                {
                    // Ignore
                }
            }
            if (fileFolderDialog.ShowDialog() == true)
            {
                string newPath = string.Empty;
                if (fileFolderDialog.SelectedPaths != null)
                {
                    if (TypeOfFileSearch == FileSearchType.Everything)
                    {
                        string[] paths = fileFolderDialog.SelectedPaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        newPath = string.Join("|", paths.Select(p => Utils.Quote(p)).ToArray());
                    }
                    else
                    {
                        newPath = fileFolderDialog.SelectedPaths;
                    }
                }
                else
                {
                    newPath = fileFolderDialog.SelectedPath;
                }

                if (!string.IsNullOrWhiteSpace(filePattern))
                {
                    if (newPath.Contains(" ") && !newPath.StartsWith("\""))
                        newPath = Utils.Quote(newPath);

                    newPath += " " + filePattern;
                }

                FileOrFolderPath = newPath;
            }
        }

        private void Search()
        {
            if (CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy)
            {
                if (TypeOfFileSearch == FileSearchType.Regex)
                {
                    if (!ValidateFilePatterns())
                        return;
                }

                if (SearchInResultsContent && CanSearchInResults)
                    CurrentGrepOperation = GrepOperation.SearchInResults;
                else
                    CurrentGrepOperation = GrepOperation.Search;
                StatusMessage = "Searching...";
                if (preview != null && preview.IsVisible)
                    preview.ResetTextEditor();
                Dictionary<string, object> workerParams = new Dictionary<string, object>();
                if (SearchInResultsContent && CanSearchInResults)
                {
                    List<string> foundFiles = new List<string>();
                    foreach (FormattedGrepResult n in SearchResults) foundFiles.Add(n.GrepResult.FileNameReal);
                    workerParams["Files"] = foundFiles;
                }
                SearchResults.Clear();
                isSorted = false;
                workerParams["State"] = this;
                processedFiles = 0;
                idleTimer.Start();
                workerSearchReplace.RunWorkerAsync(workerParams);
                UpdateBookmarks();
                // toggle value to move focus to the results tree, and enable keyboard actions on the tree
                SearchResults.IsResultsTreeFocused = false;
                SearchResults.IsResultsTreeFocused = true;
            }
        }

        private bool ValidateFilePatterns()
        {
            if (!string.IsNullOrWhiteSpace(FilePattern))
            {
                foreach (string pattern in Utils.SplitPath(FilePattern))
                {
                    string msg = ValidateRegex(pattern);
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        MessageBox.Show(string.Format("The file pattern '{0}' is not a valid regular expression:{1}{2}", pattern, Environment.NewLine, msg),
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(FilePatternIgnore))
            {
                foreach (string pattern in Utils.SplitPath(FilePatternIgnore))
                {
                    string msg = ValidateRegex(pattern);
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        MessageBox.Show(string.Format("The file pattern '{0}' is not a valid regular expression:{1}{2}", pattern, Environment.NewLine, msg),
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            return true;
        }

        private string ValidateRegex(string FilePattern)
        {
            try
            {
                Regex regex = new Regex(FilePattern);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void Replace()
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
                if (preview != null && preview.IsVisible)
                    preview.ResetTextEditor();
                CurrentGrepOperation = GrepOperation.Replace;

                CanUndo = false;
                undoMap.Clear();
                foreach (FormattedGrepResult n in SearchResults)
                {
                    string filePath = n.GrepResult.FileNameReal;
                    if (!n.GrepResult.ReadOnly && !undoMap.ContainsKey(filePath))
                    {
                        undoMap.Add(filePath, Guid.NewGuid().ToString() + Path.GetExtension(filePath));
                    }
                }

                Dictionary<string, object> workerParams = new Dictionary<string, object>
                {
                    ["State"] = this,
                    ["Files"] = undoMap
                };
                SearchResults.Clear();
                isSorted = false;
                idleTimer.Start();
                workerSearchReplace.RunWorkerAsync(workerParams);
                UpdateBookmarks();
            }
        }

        private void Undo()
        {
            if (CanUndo)
            {
                MessageBoxResult response = MessageBox.Show(
                    "Undo will revert modified file(s) back to their original state. Any changes made to the file(s) after the replace will be overwritten. Are you sure you want to proceed?",
                    "Undo", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (response == MessageBoxResult.Yes)
                {
                    GrepCore core = new GrepCore();
                    bool result = core.Undo(undoMap);
                    if (result)
                    {
                        MessageBox.Show("Files have been successfully reverted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        Utils.DeleteTempFolder();
                        undoMap.Clear();
                    }
                    else
                    {
                        MessageBox.Show("There was an error reverting files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CanUndo = false;
                }
            }
        }

        private void UpdateBookmarks()
        {
            int maxSearchReplaceCount = settings.Get<int>(GrepSettings.Key.MaxSearchBookmarks);
            int maxPathCount = settings.Get<int>(GrepSettings.Key.MaxPathBookmarks);
            int maxExtCount = settings.Get<int>(GrepSettings.Key.MaxExtensionBookmarks);

            // Update bookmarks, moving current to the top of the list
            if (FastSearchBookmarks.IndexOf(SearchFor) != 0)
            {
                FastSearchBookmarks.Insert(0, SearchFor);
                int idx = FastSearchBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == SearchFor).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = SearchFor;
                    FastSearchBookmarks.RemoveAt(idx);
                    SearchFor = s;
                }
            }
            while (FastSearchBookmarks.Count > maxSearchReplaceCount)
                FastSearchBookmarks.RemoveAt(FastSearchBookmarks.Count - 1);

            if (FastReplaceBookmarks.IndexOf(ReplaceWith) != 0)
            {
                FastReplaceBookmarks.Insert(0, ReplaceWith);
                int idx = FastReplaceBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == ReplaceWith).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = ReplaceWith;
                    FastReplaceBookmarks.RemoveAt(idx);
                    ReplaceWith = s;
                }
            }
            while (FastReplaceBookmarks.Count > maxSearchReplaceCount)
                FastReplaceBookmarks.RemoveAt(FastReplaceBookmarks.Count - 1);

            if (FastFileMatchBookmarks.IndexOf(FilePattern) != 0)
            {
                FastFileMatchBookmarks.Insert(0, FilePattern);
                int idx = FastFileMatchBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == FilePattern).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = FilePattern;
                    FastFileMatchBookmarks.RemoveAt(idx);
                    FilePattern = s;
                }
            }
            while (FastFileMatchBookmarks.Count > maxExtCount)
                FastFileMatchBookmarks.RemoveAt(FastFileMatchBookmarks.Count - 1);

            if (FastFileNotMatchBookmarks.IndexOf(FilePatternIgnore) != 0)
            {
                FastFileNotMatchBookmarks.Insert(0, FilePatternIgnore);
                int idx = FastFileNotMatchBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == FilePatternIgnore).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = FilePatternIgnore;
                    FastFileNotMatchBookmarks.RemoveAt(idx);
                    FilePatternIgnore = s;
                }
            }
            while (FastFileNotMatchBookmarks.Count > maxExtCount)
                FastFileNotMatchBookmarks.RemoveAt(FastFileNotMatchBookmarks.Count - 1);

            string searchPath = PathSearchText.CleanPath;
            if (FastPathBookmarks.IndexOf(searchPath) != 0)
            {
                FastPathBookmarks.Insert(0, searchPath);
                int idx = FastPathBookmarks.Select((x, n) => new { x, n }).Where(xn => xn.x == searchPath).Select(xn => xn.n).Skip(1).FirstOrDefault();
                if (idx > 0)
                {
                    string s = searchPath;
                    FastPathBookmarks.RemoveAt(idx);
                    FileOrFolderPath = s;
                }
            }
            while (FastPathBookmarks.Count > maxPathCount)
                FastPathBookmarks.RemoveAt(FastPathBookmarks.Count - 1);
        }

        private void Cancel()
        {
            if (CurrentGrepOperation != GrepOperation.None)
            {
                Utils.CancelSearch = true;
            }
        }

        private void ShowOptions()
        {
            SaveSettings();
            var optionsForm = new OptionsView();
            optionsForm.Owner = ParentWindow;
            var optionsViewModel = new OptionsViewModel();
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
                logger.Log<Exception>(LogLevel.Error, "Error saving options", ex);
            }
            LoadSettings();
            SearchResults.CustomEditorConfigured = true;
        }

        private void ShowHelp()
        {
            System.Diagnostics.Process.Start(@"https://github.com/dnGrep/dnGrep/wiki");
        }

        private void ShowAbout()
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void BookmarkAddRemove()
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

        private void BookmarkOpen()
        {
            try
            {
                Action<string, string, string> clearTheStar = (searchFor, replaceWith, filePattern) =>
                {
                    if (searchFor == SearchFor && replaceWith == ReplaceWith && filePattern == FilePattern)
                        IsBookmarked = false;
                };
                bookmarkForm = new BookmarksForm(clearTheStar);
                bookmarkForm.PropertyChanged += new PropertyChangedEventHandler(BookmarkForm_PropertyChanged);
                bookmarkForm.ShowDialog();
            }
            finally
            {
                bookmarkForm.PropertyChanged -= new PropertyChangedEventHandler(BookmarkForm_PropertyChanged);
            }
        }

        private void CopyFiles()
        {
            if (FilesFound)
            {
                if (fileFolderDialog.ShowDialog() == true)
                {
                    try
                    {
                        var fileList = SearchResults.GetList();
                        string destinationFolder = Utils.GetBaseFolder(fileFolderDialog.SelectedPath);
                        string baseFolder = PathSearchText.BaseFolder;

                        if (!Utils.CanCopyFiles(fileList, destinationFolder))
                        {
                            MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        int count = 0;
                        if (!string.IsNullOrWhiteSpace(baseFolder))
                        {
                            count = Utils.CopyFiles(fileList, baseFolder, destinationFolder, OverwriteFile.Prompt);
                        }
                        else
                        {
                            // without a common base path, copy all files to a single directory 
                            count = Utils.CopyFiles(fileList, destinationFolder, OverwriteFile.Prompt);
                        }
                        MessageBox.Show($"{count} files have been successfully copied.", "Copy Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("There was an error copying files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.Log<Exception>(LogLevel.Error, "Error copying files", ex);
                    }
                    CanUndo = false;
                }
            }
        }

        private void MoveFiles()
        {
            if (FilesFound)
            {
                if (fileFolderDialog.ShowDialog() == true)
                {
                    try
                    {
                        var fileList = SearchResults.GetList();
                        string destinationFolder = Utils.GetBaseFolder(fileFolderDialog.SelectedPath);
                        string baseFolder = PathSearchText.BaseFolder;

                        if (!Utils.CanCopyFiles(fileList, destinationFolder))
                        {
                            MessageBox.Show("Attention, some of the files are located in the selected directory.\nPlease select another directory and try again.",
                                "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        int count = 0;
                        if (!string.IsNullOrWhiteSpace(baseFolder))
                        {
                            count = Utils.MoveFiles(fileList, baseFolder, destinationFolder, OverwriteFile.Prompt);
                        }
                        else
                        {
                            // without a common base path, move all files to a single directory 
                            count = Utils.MoveFiles(fileList, destinationFolder, OverwriteFile.Prompt);
                        }
                        MessageBox.Show($"{count} files have been successfully moved.", "Move Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        logger.Log<Exception>(LogLevel.Error, "Error moving files", ex);
                        MessageBox.Show("There was an error moving files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CanUndo = false;
                    SearchResults.Clear();
                    isSorted = false;
                    FilesFound = false;
                }
            }
        }

        private void DeleteFiles()
        {
            if (FilesFound)
            {
                try
                {
                    if (MessageBox.Show("Attention, you are about to delete files found during search.\nAre you sure you want to procede?", "Attention", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    int count = Utils.DeleteFiles(SearchResults.GetList());
                    MessageBox.Show($"{count} files have been successfully deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was an error deleting files. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    logger.Log<Exception>(LogLevel.Error, "Error deleting files", ex);
                }
                CanUndo = false;
                SearchResults.Clear();
                isSorted = false;
                FilesFound = false;
            }
        }

        private void CopyToClipboard()
        {
            StringBuilder sb = new StringBuilder();
            foreach (GrepSearchResult result in SearchResults.GetList())
            {
                sb.AppendLine(result.FileNameReal);
            }
            Clipboard.SetText(sb.ToString());
        }

        private void CopyResults()
        {
            // can be a long [process if the results are not yet cached
            UIServices.SetBusyState();
            Clipboard.SetText(Utils.GetResultLines(SearchResults.GetList()));
        }

        private async void SaveResultsToFile()
        {
            if (FilesFound)
            {
                SaveFileDialog dlg = new SaveFileDialog();

                dlg.Filter = "Report file format|*.txt|Results file format|*.txt|CSV file format|*.csv";
                dlg.DefaultExt = "*.txt";
                dlg.InitialDirectory = PathSearchText.BaseFolder;

                var result = dlg.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    try
                    {
                        IsSaveInProgress = true;
                        await Task.Run(() =>
                        {
                            switch (dlg.FilterIndex)
                            {
                                case 1:
                                    Utils.SaveResultsReport(SearchResults.GetList(), GetSearchOptions(), dlg.FileName);
                                    break;
                                case 2:
                                    Utils.SaveResultsAsText(SearchResults.GetList(), dlg.FileName);
                                    break;
                                case 3:
                                    Utils.SaveResultsAsCSV(SearchResults.GetList(), dlg.FileName);
                                    break;
                            }
                        });

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("There was an error creating the file. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.Log<Exception>(LogLevel.Error, "Error creating results file", ex);
                    }
                    finally
                    {
                        IsSaveInProgress = false;
                    }
                }
            }
        }

        public bool CanSortResults
        {
            get
            {
                return SearchParallel && !isSorted && SearchResults.Count > 0 &&
                    CurrentGrepOperation == GrepOperation.None && !workerSearchReplace.IsBusy;
            }
        }

        private void SortResults()
        {
            using (var d = Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                var list = SearchResults.ToList();
                SearchResults.Clear();
                SearchResults.AddRange(list.OrderBy(r => r.Label));
                isSorted = true;
            }
        }

        private string GetSearchOptions()
        {
            StringBuilder sb = new StringBuilder();

            List<string> options = new List<string>();

            sb.Append("Search for: '").Append(SearchFor).AppendLine("'")
              .Append("Using ").Append(TypeOfSearch.ToString().ToLower()).AppendLine(" search");

            if (CaseSensitive) options.Add("Case sensitive");
            if (WholeWord) options.Add("Whole word");
            if (Multiline) options.Add("Multiline");
            if (Singleline) options.Add("Dot as newline");
            if (SearchInResultsContent) options.Add("Search in results");
            if (StopAfterFirstMatch) options.Add("Stop after first match");
            if (options.Count > 0)
                sb.AppendLine(string.Join(", ", options.ToArray()));
            sb.AppendLine();

            sb.Append("Search in: ").AppendLine(PathSearchText.CleanPath)
              .Append("Paths that match: ").AppendLine(FilePattern);
            if (!string.IsNullOrWhiteSpace(FilePatternIgnore))
                sb.Append("Paths to ignore: ").AppendLine(FilePatternIgnore);
            if (TypeOfFileSearch == FileSearchType.Regex)
                sb.AppendLine("Using regex file pattern");
            else if (TypeOfFileSearch == FileSearchType.Everything)
                sb.AppendLine("Using Everything index search");

            options.Clear();
            if (!IncludeSubfolder) options.Add("No subfolders");
            if (!IncludeHidden) options.Add("No hidden files");
            if (!IncludeBinary) options.Add("No binary files");
            if (!IncludeArchive) options.Add("No archives");
            if (options.Count > 0)
                sb.AppendLine(string.Join(", ", options.ToArray()));

            if (UseFileSizeFilter == FileSizeFilter.Yes)
                sb.AppendFormat("Size from {0} to {1} KB", SizeFrom, SizeTo).AppendLine();

            if (UseFileDateFilter != FileDateFilter.None && TypeOfTimeRangeFilter == FileTimeRange.Dates)
                sb.AppendFormat("{0} date from {1} to {2}", UseFileDateFilter.ToString(),
                    StartDate.HasValue ? StartDate.Value.ToShortDateString() : "*",
                    EndDate.HasValue ? EndDate.Value.ToShortDateString() : "*").AppendLine();

            if (UseFileDateFilter != FileDateFilter.None && TypeOfTimeRangeFilter == FileTimeRange.Hours)
                sb.AppendFormat("{0} date in past {1} to {2} hours", UseFileDateFilter.ToString(), HoursFrom, HoursTo)
                  .AppendLine();

            if (CodePage != -1)
            {
                string encoding = Encodings.Where(r => r.Value == CodePage).Select(r => r.Key).FirstOrDefault();
                sb.Append("Encoding: ").AppendLine(encoding);
            }

            return sb.ToString();
        }

        private void Test()
        {
            try
            {
                SaveSettings();
                TestPattern testForm = new TestPattern();
                testForm.ShowDialog();
                LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error running regex test. Please examine the error log.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Log<Exception>(LogLevel.Error, "Error running regex", ex);
            }
        }

        private async void CheckVersion()
        {
            try
            {
                if (settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking))
                {
                    DateTime lastCheck = settings.Get<DateTime>(GrepSettings.Key.LastCheckedVersion);
                    TimeSpan duration = DateTime.Now.Subtract(lastCheck);
                    if (duration.TotalDays >= settings.Get<int>(GrepSettings.Key.UpdateCheckInterval))
                    {
                        var versionChk = new PublishedVersionExtractor();
                        string version = await versionChk.QueryLatestVersion();

                        if (!string.IsNullOrEmpty(version))
                        {
                            string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                            if (PublishedVersionExtractor.IsUpdateNeeded(currentVersion, version))
                            {
                                if (MessageBox.Show("New version of dnGREP (" + version + ") is available for download.\nWould you like to download it now?",
                                    "New version", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                {
                                    System.Diagnostics.Process.Start("http://dngrep.github.io/");
                                }
                            }
                        }

                        settings.Set<DateTime>(GrepSettings.Key.LastCheckedVersion, DateTime.Now);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, ex.Message, ex);
            }
        }

        private void ControlsInit()
        {
            this.workerSearchReplace.WorkerReportsProgress = true;
            this.workerSearchReplace.WorkerSupportsCancellation = true;
            this.workerSearchReplace.DoWork += DoSearchReplace;
            this.workerSearchReplace.RunWorkerCompleted += SearchComplete;
            this.workerSearchReplace.ProgressChanged += SearchProgressChanged;

            DiginesisHelpProvider.HelpNamespace = @"https://github.com/dnGrep/dnGrep/wiki/";
            DiginesisHelpProvider.ShowHelp = true;
        }

        private void PopulateEncodings()
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

        void BookmarkForm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FilePattern")
                FilePattern = bookmarkForm.FilePattern;
            else if (e.PropertyName == "SearchFor")
                SearchFor = bookmarkForm.SearchFor;
            else if (e.PropertyName == "ReplaceWith")
                ReplaceWith = bookmarkForm.ReplaceWith;
        }

        private void CopyBookmarksToSettings()
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

        private void PreviewFile(string filePath, GrepSearchResult result, int line, RectangleF parentWindow)
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
                }
                previewModel.GrepResult = result;
                previewModel.LineNumber = line;
                previewModel.Encoding = result.Encoding;
                previewModel.FilePath = filePath;

                if (preview.WindowState == WindowState.Minimized)
                    preview.WindowState = WindowState.Normal;
                preview.Show();
                preview.BringToFront();
            }
        }
        #endregion
    }
}
