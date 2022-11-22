using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;
using dnGREP.Localization.Properties;
using Microsoft.Win32;

namespace dnGREP.WPF
{
    public partial class MainViewModel
    {
        private static readonly IDictionary<string, IScriptCommand> SetCommandMap = new Dictionary<string, IScriptCommand>();
        private static readonly IDictionary<string, IScriptCommand> UseCommandMap = new Dictionary<string, IScriptCommand>();
        private static readonly IDictionary<string, IScriptCommand> AddCommandMap = new Dictionary<string, IScriptCommand>();
        private static readonly IDictionary<string, IScriptCommand> RemoveCommandMap = new Dictionary<string, IScriptCommand>();
        private static readonly IDictionary<string, IScriptCommand> ReportCommandMap = new Dictionary<string, IScriptCommand>();

        private bool cancelingScript = false;
        private bool showEmptyMessageWindow = false;
        private string currentScriptFile;
        private string currentScriptLine;

        private void AddScriptMessage(string message)
        {
            if (ScriptMessages.Count == 0 && !string.IsNullOrEmpty(currentScriptFile))
            {
                ScriptMessages.Add(currentScriptFile);
            }
            if (!string.IsNullOrEmpty(currentScriptLine) && !ScriptMessages.Contains(currentScriptLine))
            {
                ScriptMessages.Add(string.Empty);
                ScriptMessages.Add(currentScriptLine);
            }
            ScriptMessages.Add(message);
        }
        public ObservableCollection<string> ScriptMessages { get; } = new ObservableCollection<string>();

        public void InitializeScriptTargets()
        {
            if (SetCommandMap.Count == 0)
            {
                SetCommandMap.Add("folder", new ScriptCommand<string>(p => FileOrFolderPath = Utils.QuoteIfNeeded(p)));
                SetCommandMap.Add("pathtomatch", new ScriptCommand<string>(p => FilePattern = p) { AllowNullValue = true });
                SetCommandMap.Add("pathtoignore", new ScriptCommand<string>(p => FilePatternIgnore = p) { AllowNullValue = true });
                SetCommandMap.Add("searchinarchives", new ScriptCommand<bool>(p => IncludeArchive = p));

                SetCommandMap.Add("patterntype", new ScriptCommand<FileSearchType>(p => TypeOfFileSearch = p));
                SetCommandMap.Add("searchparallel", new ScriptCommand<bool>(p => SearchParallel = p));
                SetCommandMap.Add("usegitignore", new ScriptCommand<bool>(p => UseGitignore = p));
                SetCommandMap.Add("skipremotecloudstoragefiles", new ScriptCommand<bool>(p => SkipRemoteCloudStorageFiles = p));
                SetCommandMap.Add("encoding", new ScriptCommand<int>(p => CodePage = p));

                SetCommandMap.Add("filterbyfilesize", new ScriptCommand<FileSizeFilter>(p => UseFileSizeFilter = p));
                SetCommandMap.Add("sizefrom", new ScriptCommand<int>(p => SizeFrom = p));
                SetCommandMap.Add("sizeto", new ScriptCommand<int>(p => SizeTo = p));
                SetCommandMap.Add("includesubfolder", new ScriptCommand<bool>(p => IncludeSubfolder = p));
                SetCommandMap.Add("maxsubfolderdepth", new ScriptCommand<int>(p => MaxSubfolderDepth = p));
                SetCommandMap.Add("includehidden", new ScriptCommand<bool>(p => IncludeHidden = p));
                SetCommandMap.Add("includebinary", new ScriptCommand<bool>(p => IncludeBinary = p));
                SetCommandMap.Add("followsymlinks", new ScriptCommand<bool>(p => FollowSymlinks = p));

                SetCommandMap.Add("filedatefilter", new ScriptCommand<FileDateFilter>(p => UseFileDateFilter = p));
                SetCommandMap.Add("filetimerange", new ScriptCommand<FileTimeRange>(p => TypeOfTimeRangeFilter = p));
                SetCommandMap.Add("startdate", new ScriptCommand<DateTime>(p => StartDate = p) { AllowNullValue = true });
                SetCommandMap.Add("enddate", new ScriptCommand<DateTime>(p => EndDate = p) { AllowNullValue = true });
                SetCommandMap.Add("hoursfrom", new ScriptCommand<int>(p => HoursFrom = p));
                SetCommandMap.Add("hoursto", new ScriptCommand<int>(p => HoursTo = p));

                SetCommandMap.Add("searchtype", new ScriptCommand<SearchType>(p => TypeOfSearch = p));
                SetCommandMap.Add("searchfor", new ScriptCommand<string>(p => SearchFor = p) { AllowNullValue = true });
                SetCommandMap.Add("replacewith", new ScriptCommand<string>(p => ReplaceWith = p) { AllowNullValue = true });

                SetCommandMap.Add("casesensitive", new ScriptCommand<bool>(p => CaseSensitive = p));
                SetCommandMap.Add("wholeword", new ScriptCommand<bool>(p => WholeWord = p));
                SetCommandMap.Add("multiline", new ScriptCommand<bool>(p => Multiline = p));
                SetCommandMap.Add("dotasnewline", new ScriptCommand<bool>(p => Singleline = p));
                SetCommandMap.Add("booleanoperators", new ScriptCommand<bool>(p => BooleanOperators = p));
                SetCommandMap.Add("capturegroupsearch", new ScriptCommand<bool>(p => CaptureGroupSearch = p));

                SetCommandMap.Add("searchinresults", new ScriptCommand<bool>(p => SearchInResultsContent = p));
                SetCommandMap.Add("previewfile", new ScriptCommand<bool>(p => PreviewFileContent = p));
                SetCommandMap.Add("stopafterfirstmatch", new ScriptCommand<bool>(p => StopAfterFirstMatch = p));

                SetCommandMap.Add("highlightmatches", new ScriptCommand<bool>(p => HighlightsOn = p));
                SetCommandMap.Add("highlightgroups", new ScriptCommand<bool>(p => HighlightCaptureGroups = p));
                SetCommandMap.Add("showcontextlines", new ScriptCommand<bool>(p => ShowLinesInContext = p));
                SetCommandMap.Add("contextlinesbefore", new ScriptCommand<int>(p => ContextLinesBefore = p));
                SetCommandMap.Add("contextlinesafter", new ScriptCommand<int>(p => ContextLinesAfter = p));

                SetCommandMap.Add("wraptext", new ScriptCommand<bool>(p => SearchResults.WrapText = p));
                SetCommandMap.Add("resultszoom", new ScriptCommand<double>(p => SearchResults.ResultsScale = p));
                SetCommandMap.Add("sorttype", new ScriptCommand<SortType>(p => SortType = p));
                SetCommandMap.Add("sortdirection", new ScriptCommand<ListSortDirection>(p => SortDirection = p));

                SetCommandMap.Add("reportmode", new ScriptCommand<ReportMode>(p => GrepSettings.Instance.Set(GrepSettings.Key.ReportMode, p)));
                SetCommandMap.Add("fileinformation", new ScriptCommand<bool>(p => GrepSettings.Instance.Set(GrepSettings.Key.IncludeFileInformation, p)));
                SetCommandMap.Add("trimwhitespace", new ScriptCommand<bool>(p => GrepSettings.Instance.Set(GrepSettings.Key.TrimWhitespace, p)));
                SetCommandMap.Add("uniquevalues", new ScriptCommand<bool>(p => GrepSettings.Instance.Set(GrepSettings.Key.FilterUniqueValues, p)));
                SetCommandMap.Add("uniquescope", new ScriptCommand<UniqueScope>(p => GrepSettings.Instance.Set(GrepSettings.Key.UniqueScope, p)));
                SetCommandMap.Add("separatelines", new ScriptCommand<bool>(p => GrepSettings.Instance.Set(GrepSettings.Key.OutputOnSeparateLines, p)));
                SetCommandMap.Add("listitemseparator", new ScriptCommand<string>(p => GrepSettings.Instance.Set(GrepSettings.Key.ListItemSeparator, p)) { AllowNullValue = true });

                UseCommandMap.Add("bookmark", new ScriptCommand<string>(p => UseBookmark(p)));

                AddCommandMap.Add("bookmark", new ScriptCommand<string>(p => AddBookmark(p, false)) { AllowNullValue = true });
                AddCommandMap.Add("folderbookmark", new ScriptCommand<string>(p => AddBookmark(p, true)) { AllowNullValue = true });

                RemoveCommandMap.Add("bookmark", new ScriptCommand<string>(p => RemoveBookmark(p, false)) { AllowNullValue = true });
                RemoveCommandMap.Add("folderbookmark", new ScriptCommand<string>(p => RemoveBookmark(p, true)) { AllowNullValue = true });

                ReportCommandMap.Add("full", new ScriptCommand<string>(p =>
                    ReportWriter.SaveResultsReport(SearchResults.GetList(), BooleanOperators, SearchFor, GetSearchOptions(), p)));
                ReportCommandMap.Add("text", new ScriptCommand<string>(p =>
                    ReportWriter.SaveResultsAsText(SearchResults.GetList(), SearchResults.TypeOfSearch, p)));
                ReportCommandMap.Add("csv", new ScriptCommand<string>(p =>
                    ReportWriter.SaveResultsAsCSV(SearchResults.GetList(), SearchResults.TypeOfSearch, p)));
            }
        }

        public IScriptCommand ExpandFileFiltersCommand => new ScriptCommand<bool>(
            param => IsFiltersExpanded = param);

        public IScriptCommand MaximizeResultsCommand => new ScriptCommand<bool>(
            param => IsResultTreeMaximized = param);

        public IScriptCommand ExpandResultOptionsCommand => new ScriptCommand<bool>(
            param => IsResultOptionsExpanded = param);

        private void PopulateScripts()
        {
            InitializeScriptTargets();

            ScriptMenuItems.Add(new MenuItemViewModel(Resources.Main_Menu_NewScript, new RelayCommand(p => NewScript(), q => !IsScriptRunning)));
            ScriptMenuItems.Add(new MenuItemViewModel(Resources.Main_Menu_EditScript, new RelayCommand(p => EditScript(), q => !IsScriptRunning)));
            ScriptMenuItems.Add(new MenuItemViewModel(Resources.Main_Menu_CancelScript, new RelayCommand(p => CancelScript(), q => IsScriptRunning)));
            ScriptMenuItems.Add(new MenuItemViewModel(null, null));
            AddScriptFilesToMenu();
        }

        private void AddScriptFilesToMenu()
        {
            while (ScriptMenuItems.Count > 4)
            {
                ScriptMenuItems.RemoveAt(ScriptMenuItems.Count - 1);
            }

            foreach (var key in ScriptManager.Instance.Scripts)
            {
                string[] parts = key.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    string header = parts[parts.Length - 1];

                    ObservableCollection<MenuItemViewModel> menuItems = ScriptMenuItems;

                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        string dir = parts[i];
                        var parent = menuItems.Where(m => m.Header == dir).FirstOrDefault();
                        if (parent == null)
                        {
                            parent = new MenuItemViewModel(dir, null);
                            menuItems.Add(parent);
                        }

                        menuItems = parent.Children;
                    }

                    menuItems.Add(new MenuItemViewModel(header,
                        new RelayCommand(p => RunScript(key), q => !IsScriptRunning)));
                }
            }

            SortMenuRecursive(ScriptMenuItems, 4);
        }

        void SortMenuRecursive(Collection<MenuItemViewModel> coll, int start)
        {
            // sorts directories on top, files on bottom, both in sort order
            for (int i = start; i < coll.Count - 1; i++)
            {
                for (int j = i + 1; j < coll.Count; j++)
                {
                    if (coll[i].Command != null && coll[j].Command == null)
                    {
                        var temp = coll[j];
                        coll[j] = coll[i];
                        coll[i] = temp;
                    }

                    if ((coll[i].Command == null && coll[j].Command == null) ||
                        (coll[i].Command != null && coll[j].Command != null))
                    {
                        if (string.Compare(coll[i].Header, coll[j].Header) > 0)
                        {
                            var temp = coll[j];
                            coll[j] = coll[i];
                            coll[i] = temp;
                        }
                    }
                }
            }

            foreach (MenuItemViewModel item in coll)
            {
                SortMenuRecursive(item.Children, 0);
            }
        }

        private bool isScriptRunning = false;
        public bool IsScriptRunning
        {
            get { return isScriptRunning; }
            set
            {
                if (isScriptRunning == value)
                {
                    return;
                }

                isScriptRunning = value;
                OnPropertyChanged(nameof(IsScriptRunning));
            }
        }

        private void NewScript()
        {
            ScriptEditorWindow wnd = new ScriptEditorWindow();
            wnd.NewScriptFileSaved += ScriptEditor_NewScriptFileSaved;
            wnd.Show();
        }

        private void EditScript()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Script files|*.script",
                DefaultExt = "script",
                CheckFileExists = true,
            };

            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                ScriptEditorWindow wnd = new ScriptEditorWindow();
                wnd.NewScriptFileSaved += ScriptEditor_NewScriptFileSaved;
                wnd.OpenScriptFile(dlg.FileName);
                wnd.Show();
            }
        }

        private void ScriptEditor_NewScriptFileSaved(object sender, EventArgs e)
        {
            ScriptManager.Instance.LoadScripts();
            AddScriptFilesToMenu();
        }

        private void CancelScript()
        {
            cancelingScript = true;
            Cancel();

            if (!ScriptMessages.Contains(Resources.Scripts_ScriptCanceled))
            {
                AddScriptMessage(Resources.Scripts_ScriptCanceled);
            }
        }

        private Queue<ScriptStatement> currentScript = null;

        private void RunScript(string name)
        {
            if (currentScript == null)
            {
                ScriptMessages.Clear();
                currentScript = ScriptManager.Instance.ParseScript(name);
                currentScriptFile = name;
                cancelingScript = false;
                showEmptyMessageWindow = false;
                IsScriptRunning = true;
                CommandManager.InvalidateRequerySuggested();
                ContinueScript();
            }
        }

        private void ContinueScript()
        {
            while (currentScript != null && currentScript.Count > 0)
            {
                if (cancelingScript || Utils.CancelSearch)
                {
                    currentScript.Clear();
                    break;
                }

                var stmt = currentScript.Dequeue();
                currentScriptLine = $"{Resources.Scripts_AtLine} {stmt.LineNumber}: {stmt.Command} {stmt.Target} {stmt.Value}";
                switch (stmt.Command)
                {
                    case "set":
                        if (SetCommandMap.TryGetValue(stmt.Target, out IScriptCommand set))
                        {
                            set.Execute(stmt.Value);
                        }
                        break;

                    case "use":
                        if (UseCommandMap.TryGetValue(stmt.Target, out IScriptCommand use))
                        {
                            use.Execute(stmt.Value);
                        }
                        break;

                    case "add":
                        if (AddCommandMap.TryGetValue(stmt.Target, out IScriptCommand add))
                        {
                            add.Execute(stmt.Value);
                        }
                        break;

                    case "remove":
                        if (RemoveCommandMap.TryGetValue(stmt.Target, out IScriptCommand remove))
                        {
                            remove.Execute(stmt.Value);
                        }
                        break;

                    case "report":
                        if (ReportCommandMap.TryGetValue(stmt.Target, out IScriptCommand rpt))
                        {
                            rpt.Execute(stmt.Value);
                        }
                        break;

                    case "resetfilters":
                        ResetOptionsCommand.Execute(null);
                        break;

                    case "sort":
                        SortCommand.Execute(null);
                        break;

                    case "undo":
                        UndoCommand.Execute(false);
                        break;

                    case "copyfiles":
                        CopyFilesCommand.Execute(stmt.Value);
                        break;

                    case "movefiles":
                        MoveFilesCommand.Execute(stmt.Value);
                        break;

                    case "deletefiles":
                        DeleteFilesCommand.Execute(null);
                        break;

                    case "copyfilenames":
                        CopyToClipboardCommand.Execute(null);
                        break;

                    case "copyresults":
                        CopyMatchingLinesCommand.Execute(null);
                        break;

                    case "expandfilefilters":
                        ExpandFileFiltersCommand.Execute(stmt.Value);
                        break;

                    case "maximizeresults":
                        MaximizeResultsCommand.Execute(stmt.Value);
                        break;

                    case "expandresultoptions":
                        ExpandResultOptionsCommand.Execute(stmt.Value);
                        break;

                    case "search":
                        SearchCommand.Execute(null);
                        if (cancelingScript || Utils.CancelSearch)
                        {
                            break;
                        }
                        else
                        {
                            return;
                        }

                    case "replace":
                        ReplaceCommand.Execute(null);
                        if (cancelingScript || Utils.CancelSearch)
                        {
                            break;
                        }
                        else
                        {
                            return;
                        }

                    case "messages":
                        showEmptyMessageWindow = true;
                        break;

                    case "exit":
                        Application.Current.MainWindow.Close();
                        return;
                }
            }

            if (currentScript != null && currentScript.Count == 0)
            {
                currentScript = null;
                IsScriptRunning = false;

                if (ScriptMessages.Count > 0 || showEmptyMessageWindow)
                {
                    if (!cancelingScript)
                    {
                        if (ScriptMessages.Count == 0 && !string.IsNullOrEmpty(currentScriptFile))
                        {
                            ScriptMessages.Add(currentScriptFile);
                        }
                        ScriptMessages.Add(string.Empty);
                        ScriptMessages.Add(Resources.Scripts_ScriptComplete);
                    }

                    MessagesWindow dlg = new MessagesWindow(this);
                    dlg.ShowDialog();
                }
            }

            CommandManager.InvalidateRequerySuggested();
        }
    }
}
