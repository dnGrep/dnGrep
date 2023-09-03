using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Localization.Properties;
using Microsoft.Win32;

namespace dnGREP.WPF
{
    public partial class MainViewModel
    {
        private static readonly IDictionary<string, IScriptCommand> SetCommandMap = new Dictionary<string, IScriptCommand>();
        private static readonly IDictionary<string, IScriptCommand> BookmarkCommandMap = new Dictionary<string, IScriptCommand>();
        private static readonly IDictionary<string, IScriptCommand> ReportCommandMap = new Dictionary<string, IScriptCommand>();

        private readonly List<ScriptEditorWindow> scriptEditorWindows = new();
        private bool showEmptyMessageWindow = false;
        private string currentScriptFile = string.Empty;
        private string currentScriptLine = string.Empty;
        private DateTime scriptStartTime = DateTime.MinValue;

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

        public ObservableCollection<string> ScriptMessages { get; } = new();

        public void InitializeScriptTargets()
        {
            if (SetCommandMap.Count == 0)
            {
                SetCommandMap.Add("folder", new ScriptCommand<string>(p => FileOrFolderPath = UiUtils.QuoteIfNeeded(p)));
                SetCommandMap.Add("pathtomatch", new ScriptCommand<string>(p => FilePattern = p));
                SetCommandMap.Add("pathtoignore", new ScriptCommand<string>(p => FilePatternIgnore = p));
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
                SetCommandMap.Add("startdate", new ScriptCommand<DateTime>(p => StartDate = p));
                SetCommandMap.Add("enddate", new ScriptCommand<DateTime>(p => EndDate = p));
                SetCommandMap.Add("hoursfrom", new ScriptCommand<int>(p => HoursFrom = p));
                SetCommandMap.Add("hoursto", new ScriptCommand<int>(p => HoursTo = p));

                SetCommandMap.Add("searchtype", new ScriptCommand<SearchType>(p => TypeOfSearch = p));
                SetCommandMap.Add("searchfor", new ScriptCommand<string>(p => SearchFor = p));
                SetCommandMap.Add("replacewith", new ScriptCommand<string>(p => ReplaceWith = p));

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

                SetCommandMap.Add("wraptext", new ScriptCommand<bool>(p => ResultsViewModel.WrapText = p));
                SetCommandMap.Add("resultszoom", new ScriptCommand<double>(p => ResultsViewModel.ResultsScale = p));
                SetCommandMap.Add("sorttype", new ScriptCommand<SortType>(p => SortType = p));
                SetCommandMap.Add("sortdirection", new ScriptCommand<ListSortDirection>(p => SortDirection = p));

                SetCommandMap.Add("reportmode", new ScriptCommand<ReportMode>(p => GrepSettings.Instance.Set(GrepSettings.Key.ReportMode, p)));
                SetCommandMap.Add("fileinformation", new ScriptCommand<bool>(p => GrepSettings.Instance.Set(GrepSettings.Key.IncludeFileInformation, p)));
                SetCommandMap.Add("trimwhitespace", new ScriptCommand<bool>(p => GrepSettings.Instance.Set(GrepSettings.Key.TrimWhitespace, p)));
                SetCommandMap.Add("uniquevalues", new ScriptCommand<bool>(p => GrepSettings.Instance.Set(GrepSettings.Key.FilterUniqueValues, p)));
                SetCommandMap.Add("uniquescope", new ScriptCommand<UniqueScope>(p => GrepSettings.Instance.Set(GrepSettings.Key.UniqueScope, p)));
                SetCommandMap.Add("separatelines", new ScriptCommand<bool>(p => GrepSettings.Instance.Set(GrepSettings.Key.OutputOnSeparateLines, p)));
                SetCommandMap.Add("listitemseparator", new ScriptCommand<string>(p => GrepSettings.Instance.Set(GrepSettings.Key.ListItemSeparator, p)));

                BookmarkCommandMap.Add("use", new ScriptCommand<string>(p => UseBookmark(p)));
                BookmarkCommandMap.Add("add", new ScriptCommand<string>(p => AddBookmark(p, false)));
                BookmarkCommandMap.Add("addfolder", new ScriptCommand<string>(p => AddBookmark(p, true)));
                BookmarkCommandMap.Add("remove", new ScriptCommand<string>(p => RemoveBookmark(p, false)));
                BookmarkCommandMap.Add("removefolder", new ScriptCommand<string>(p => RemoveBookmark(p, true)));

                ReportCommandMap.Add("full", new ScriptCommand<string>(p =>
                    ReportWriter.SaveResultsReport(ResultsViewModel.GetList(), BooleanOperators, SearchFor, GetSearchOptions(), p)));
                ReportCommandMap.Add("text", new ScriptCommand<string>(p =>
                    ReportWriter.SaveResultsAsText(ResultsViewModel.GetList(), ResultsViewModel.TypeOfSearch, p)));
                ReportCommandMap.Add("csv", new ScriptCommand<string>(p =>
                    ReportWriter.SaveResultsAsCSV(ResultsViewModel.GetList(), ResultsViewModel.TypeOfSearch, p)));
            }
        }

        public IScriptCommand ExpandFileFiltersCommand => new ScriptCommand<bool>(
            param => IsFiltersExpanded = param);

        public IScriptCommand MaximizeResultsCommand => new ScriptCommand<bool>(
            param => IsResultTreeMaximized = param);

        public IScriptCommand ExpandResultOptionsCommand => new ScriptCommand<bool>(
            param => IsResultOptionsExpanded = param);

        public IScriptCommand RunScriptCommand => new ScriptCommand<string>(
            param => RunScript(param));

        private void PopulateScripts()
        {
            ScriptManager.Instance.LoadScripts();
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

            foreach (var key in ScriptManager.Instance.ScriptKeys)
            {
                string[] parts = key.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    string header = parts[^1];

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
                        (coll[i], coll[j]) = (coll[j], coll[i]);
                    }

                    if ((coll[i].Command == null && coll[j].Command == null) ||
                        (coll[i].Command != null && coll[j].Command != null))
                    {
                        if (string.Compare(coll[i].Header, coll[j].Header, StringComparison.Ordinal) > 0)
                        {
                            (coll[i], coll[j]) = (coll[j], coll[i]);
                        }
                    }
                }
            }

            foreach (MenuItemViewModel item in coll)
            {
                SortMenuRecursive(item.Children, 0);
            }
        }

        private void NewScript()
        {
            ScriptEditorWindow wnd = new();
            wnd.NewScriptFileSaved += ScriptEditor_NewScriptFileSaved;
            wnd.RequestRun += ScriptEditor_RequestRun;
            scriptEditorWindows.Add(wnd);
            wnd.Closed += (s, e) => { scriptEditorWindows.Remove(wnd); };
            wnd.Show();
        }

        private bool firstFileOpen = true;

        private void EditScript()
        {
            OpenFileDialog dlg = new()
            {
                Filter = Resources.Scripts_ScriptFiles + "|*" + ScriptManager.ScriptExt,
                DefaultExt = ScriptManager.ScriptExt.TrimStart('.'),
                CheckFileExists = true,
            };

            if (firstFileOpen)
            {
                firstFileOpen = false;
                string dataFolder = Path.Combine(Utils.GetDataFolderPath(), ScriptManager.ScriptFolder);
                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                }
                dlg.InitialDirectory = dataFolder;
            }

            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                ScriptEditorWindow wnd = new();
                wnd.NewScriptFileSaved += ScriptEditor_NewScriptFileSaved;
                wnd.RequestRun += ScriptEditor_RequestRun;
                scriptEditorWindows.Add(wnd);
                wnd.Closed += (s, e) => { scriptEditorWindows.Remove(wnd); };
                wnd.OpenScriptFile(dlg.FileName);
                wnd.Show();
            }
        }

        private void ScriptEditor_NewScriptFileSaved(object? sender, EventArgs e)
        {
            ScriptManager.Instance.LoadScripts();
            AddScriptFilesToMenu();
        }

        private void CancelScript()
        {
            Cancel();

            if (!ScriptMessages.Contains(Resources.Scripts_ScriptCanceled))
            {
                AddScriptMessage(Resources.Scripts_ScriptCanceled);
            }
        }

        private Queue<ScriptStatement>? currentScript = null;

        private void ScriptEditor_RequestRun(object? sender, EventArgs e)
        {
            if (sender is ScriptEditorWindow wnd && currentScript == null)
            {
                var script = wnd.ScriptText;
                if (script.Any())
                {
                    var sc = ScriptManager.Instance.ParseScript(script, true);
                    BeginScript(sc, wnd.ScriptFile);
                }
            }
        }

        private void RunScript(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            string key = name;
            if (!ScriptManager.Instance.ScriptKeys.Contains(key) &&
                key.EndsWith(ScriptManager.ScriptExt, StringComparison.OrdinalIgnoreCase))
            {
                key = key.Remove(key.Length - ScriptManager.ScriptExt.Length);
            }

            if (currentScript == null)
            {
                var sc = ScriptManager.Instance.ParseScript(key);
                BeginScript(sc, key);
            }
        }

        private void BeginScript(Queue<ScriptStatement> newScript, string name)
        {
            if (currentScript == null && pauseCancelTokenSource == null)
            {
                ScriptManager.Instance.ResetVariables();
                ScriptMessages.Clear();
                currentScript = newScript;
                currentScriptFile = name;
                showEmptyMessageWindow = false;
                IsScriptRunning = true;
                CommandManager.InvalidateRequerySuggested();
                scriptStartTime = DateTime.Now;
                pauseCancelTokenSource = new();
                ContinueScript(pauseCancelTokenSource.Token);
            }
        }

        private void ContinueScript(PauseCancelToken pauseCancelToken)
        {
            while (currentScript != null && currentScript.Count > 0)
            {
                pauseCancelToken.WaitWhilePaused();

                if (pauseCancelToken.IsCancellationRequested)
                {
                    currentScript.Clear();
                    break;
                }

                var stmt = currentScript.Dequeue();
                currentScriptLine = $"{Resources.Scripts_AtLine} {stmt.LineNumber}: {stmt.Command} {stmt.Target} {stmt.Value}";
                switch (stmt.Command)
                {
                    case "set":
                        if (SetCommandMap.TryGetValue(stmt.Target, out IScriptCommand? set) && !string.IsNullOrEmpty(stmt.Value))
                        {
                            set.Execute(stmt.Value);
                        }
                        break;

                    case "env":
                        if (!string.IsNullOrEmpty(stmt.Value) && stmt.Value.Contains('=', StringComparison.Ordinal))
                        {
                            int pos = stmt.Value.IndexOf('=', StringComparison.Ordinal);
                            if (pos > 1)
                            {
                                string variable = stmt.Value[..pos];
                                string value = stmt.Value[(pos + 1)..];
                                ScriptManager.Instance.SetScriptEnvironmentVariable(variable, value);
                            }
                        }
                        break;

                    case "bookmark":
                        if (BookmarkCommandMap.TryGetValue(stmt.Target, out IScriptCommand? use) && !string.IsNullOrEmpty(stmt.Value))
                        {
                            use.Execute(stmt.Value);
                        }
                        break;

                    case "report":
                        if (ReportCommandMap.TryGetValue(stmt.Target, out IScriptCommand? rpt) && !string.IsNullOrEmpty(stmt.Value))
                        {
                            rpt.Execute(ScriptManager.Instance.ExpandEnvironmentVariables(stmt.Value));
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
                        CopyFilesCommand.Execute(ScriptManager.Instance.ExpandEnvironmentVariables(stmt.Value));
                        break;

                    case "movefiles":
                        MoveFilesCommand.Execute(ScriptManager.Instance.ExpandEnvironmentVariables(stmt.Value));
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
                        if (!string.IsNullOrEmpty(stmt.Value))
                            ExpandFileFiltersCommand.Execute(stmt.Value);
                        break;

                    case "maximizeresults":
                        if (!string.IsNullOrEmpty(stmt.Value))
                            MaximizeResultsCommand.Execute(stmt.Value);
                        break;

                    case "expandresultoptions":
                        if (!string.IsNullOrEmpty(stmt.Value))
                            ExpandResultOptionsCommand.Execute(stmt.Value);
                        break;

                    case "search":
                        SearchCommand.Execute(null);
                        if (pauseCancelToken.IsCancellationRequested)
                        {
                            break;
                        }
                        else
                        {
                            return;
                        }

                    case "replace":
                        ReplaceCommand.Execute(null);
                        if (pauseCancelToken.IsCancellationRequested)
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

                    case "log":
                        if (string.Equals(stmt.Value, "time", StringComparison.OrdinalIgnoreCase))
                        {
                            TimeSpan elapsedTime = DateTime.Now.Subtract(scriptStartTime);
                            logger.Info($"Script [{currentScriptFile}] elapsed time: {elapsedTime.GetPrettyString()}");
                        }
                        else if (!string.IsNullOrWhiteSpace(stmt.Value))
                        {
                            logger.Info(stmt.Value);
                        }
                        break;

                    case "run":
                        if (!string.IsNullOrEmpty(stmt.Value))
                            RunCommand(stmt.Target, ScriptManager.Instance.ExpandEnvironmentVariables(stmt.Value));
                        break;

                    case "exit":
                        System.Windows.Application.Current.MainWindow.Close();
                        return;
                }
            }

            if (currentScript != null && currentScript.Count == 0)
            {
                currentScript = null;
                IsScriptRunning = false;
                pauseCancelTokenSource?.Dispose();
                pauseCancelTokenSource = null;

                if (ScriptMessages.Count > 0 || showEmptyMessageWindow)
                {
                    if (!pauseCancelTokenSource?.IsCancellationRequested ?? true)
                    {
                        if (ScriptMessages.Count == 0 && !string.IsNullOrEmpty(currentScriptFile))
                        {
                            ScriptMessages.Add(currentScriptFile);
                        }
                        ScriptMessages.Add(string.Empty);
                        ScriptMessages.Add(Resources.Scripts_ScriptComplete);
                    }

                    MessagesWindow dlg = new(this);
                    dlg.ShowDialog();
                }

                if (Application.Current is App app && app.AppArgs != null)
                {
                    ProcessCommands(app.AppArgs);
                }
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void RunCommand(string target, string value)
        {
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(value))
            {
                ScriptMessages.Add(Resources.Scripts_RunCommandInvalid);
                CancelScript();
                return;
            }

            var filePath = value;
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(ScriptManager.Instance.GetScriptPath(currentScriptFile), value);
            }
            if (!File.Exists(filePath))
            {
                ScriptMessages.Add(string.Format(Resources.Scripts_FileNotFound, target, value));
                CancelScript();
                return;
            }

            if (target == "powershell")
            {
                string ext = ".ps1";
                if (Path.GetExtension(filePath).Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var startInfo = new ProcessStartInfo()
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -ExecutionPolicy unrestricted -file \"{filePath}\"",
                            UseShellExecute = false,
                        };
                        foreach (var item in ScriptManager.Instance.ScriptEnvironmentVariables)
                        {
                            startInfo.Environment[item.Key] = item.Value;
                        }
                        if (Process.Start(startInfo) is Process proc)
                        {
                            proc.WaitForExit(60 * 1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        ScriptMessages.Add(Resources.Scripts_RunCommandError + ex.Message);
                        CancelScript();
                    }
                }
                else
                {
                    ScriptMessages.Add(string.Format(Resources.Scripts_InvalidFileExtension, target, ext));
                    CancelScript();
                }
            }
            else if (target == "cmd")
            {
                string ext1 = ".cmd";
                string ext2 = ".bat";
                if (Path.GetExtension(filePath).Equals(ext1, StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(filePath).Equals(ext2, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var startInfo = new ProcessStartInfo()
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/C \"{filePath}\"",
                            UseShellExecute = false,
                        };
                        foreach (var item in ScriptManager.Instance.ScriptEnvironmentVariables)
                        {
                            startInfo.Environment[item.Key] = item.Value;
                        }
                        if (Process.Start(startInfo) is Process proc)
                        {
                            proc.WaitForExit(60 * 1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        ScriptMessages.Add(Resources.Scripts_RunCommandError + ex.Message);
                        CancelScript();
                    }
                }
                else
                {
                    ScriptMessages.Add(string.Format(Resources.Scripts_InvalidFileExtension, target,
                        string.Join(CultureInfo.CurrentCulture.TextInfo.ListSeparator, ext1, ext2)));
                    CancelScript();
                }
            }
            else
            {
                ScriptMessages.Add(Resources.Scripts_RunCommandInvalid);
                CancelScript();
            }
        }
    }
}
