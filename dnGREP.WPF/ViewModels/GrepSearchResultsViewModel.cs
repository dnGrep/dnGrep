using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using dnGREP.WPF.MVHelpers;
using dnGREP.WPF.UserControls;
using Microsoft.VisualBasic.FileIO;
using NLog;
using Windows.Win32;

namespace dnGREP.WPF
{
    public partial class GrepSearchResultsViewModel : CultureAwareViewModel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static readonly Messenger SearchResultsMessenger = new();
        private static bool beenInitialized;

        internal ResultsTree? TreeControl { get; set; }

        static GrepSearchResultsViewModel()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (beenInitialized) return;

            beenInitialized = true;
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(OpenFilesCommand), "Main_Results_Open", string.Empty);

            if (GrepSettings.Instance.ContainsKey(GrepSettings.Key.CustomEditors))
            {
                List<CustomEditor> list = GrepSettings.Instance.Get<List<CustomEditor>>(GrepSettings.Key.CustomEditors);
                foreach (var editor in list)
                {
                    if (editor != null && !string.IsNullOrEmpty(editor.Label) && !string.IsNullOrEmpty(editor.Path))
                    {
                        KeyBindingManager.RegisterCustomEditor(KeyCategory.Main, editor.Label);
                    }
                }
            }

            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(OpenContainingFolderCommand), "Main_Results_OpenContainingFolder", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(RenameFileCommand), "Main_Results_RenameFile", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CopyFilesCommand), "Main_Results_CopyFiles", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(MoveFilesCommand), "Main_Results_MoveFiles", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(DeleteFilesCommand), "Main_Results_DeleteFiles", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(RecycleFilesCommand), "Main_Results_MoveFilesToRecycleBin", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(MakeWritableCommand), "Main_Results_MakeWritable", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(OpenExplorerMenuCommand), "Main_Results_ShowExplorerMenu", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ShowFilePropertiesCommand), "Main_Results_ShowFileProperties", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CompareFilesCommand), "Main_Results_CompareFiles", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CopyFileNamesCommand), "Main_Results_CopyFileNames", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CopyFullFilePathsCommand), "Main_Results_CopyFullFilePaths", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CopyGrepLinesCommand), "Main_Results_CopyLinesOfText", string.Empty);
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ExcludeFilesCommand), "Main_Results_ExcludeFromResults", "Delete");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(NextLineCommand), "Main_Results_NextMatch", "F3");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(NextFileCommand), "Main_Results_NextFile", "Shift+F3");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(PreviousLineCommand), "Main_Results_PreviousMatch", "F4");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(PreviousFileCommand), "Main_Results_PreviousFile", "Shift+F4");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ExpandAllCommand), "Main_Results_ExpandAll", "F6");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CollapseAllCommand), "Main_Results_CollapseAll", "Shift+F6");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(ResetZoomCommand), "Main_Results_ResetZoom", string.Empty);
            
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(CopyCommand), "", "Control+C");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SelectAllCommand), "", "Control+A");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SelectToStartCommand), "", "Control+Home");
            KeyBindingManager.RegisterCommand(KeyCategory.Main, nameof(SelectToEndCommand), "", "Control+End");
        }

        public GrepSearchResultsViewModel()
        {
            SelectedNodes = [];
            SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;
            SearchResults.CollectionChanged += ObservableGrepSearchResults_CollectionChanged;

            SearchResultsMessenger.Register<ITreeItem>("IsSelectedChanged", OnSelectionChanged);
            SearchResultsMessenger.Register("EditorsChanged", InitializeEditorMenuItems);

            InitializeEditorMenuItems();
            InitializeInputBindings();
            App.Messenger.Register<KeyCategory>("KeyGestureChanged", OnKeyGestureChanged);
        }

        private void InitializeInputBindings()
        {
            foreach (KeyBindingInfo kbi in KeyBindingManager.GetCommandGestures(KeyCategory.Main))
            {
                PropertyInfo? pi = GetType().GetProperty(kbi.CommandName, BindingFlags.Instance | BindingFlags.Public);
                if (pi != null && pi.GetValue(this) is RelayCommand cmd)
                {
                    InputBindings.Add(KeyBindingManager.CreateKeyBinding(cmd, kbi.KeyGesture));
                }
            }
        }

        private void OnKeyGestureChanged(KeyCategory category)
        {
            if (category == KeyCategory.Main)
            {
                InputBindings.Clear();
                InitializeEditorMenuItems();
                InitializeInputBindings();

                InputBindings.RaiseAfterCollectionChanged();
            }
        }

        public void Clear()
        {
            SearchResults.Clear();
            FailureCount = 0;
        }

        public void Clear(List<GrepSearchResult> list)
        {
            foreach (var item in list)
            {
                if (SearchResults.FirstOrDefault(r => r.GrepResult == item) is FormattedGrepResult result)
                {
                    SearchResults.Remove(result);
                }
            }
        }

        public PathSearchText PathSearchText { get; internal set; } = new();

        private void OnSelectionChanged(ITreeItem item)
        {
            if (item != null)
            {
                if (item.IsSelected && !SelectedNodes.Contains(item))
                {
                    SelectedNodes.Insert(0, item);
                }
                else
                {
                    SelectedNodes.Remove(item);
                }
            }
        }

        private readonly Dictionary<string, BitmapSource> icons = [];

        public ObservableCollection<MenuItemViewModel> EditorMenuItems { get; } = [];

        private readonly List<RelayCommand> customEditorCommands = [];
        private readonly List<InputBinding> customEditorInputBindings = [];

        public ObservableCollectionEx<InputBinding> InputBindings { get; } = [];

        public void InitializeEditorMenuItems()
        {
            EditorMenuItems.Clear();
            customEditorCommands.Clear();

            // remove custom editor key bindings from InputBinding collection
            foreach (var inputBinding in customEditorInputBindings)
            {
                InputBindings.Remove(inputBinding);
            }
            customEditorInputBindings.Clear();

            if (GrepSettings.Instance.ContainsKey(GrepSettings.Key.CustomEditors))
            {
                List<CustomEditor> list = GrepSettings.Instance.Get<List<CustomEditor>>(GrepSettings.Key.CustomEditors);
                foreach (var editor in list)
                {
                    if (editor != null && !string.IsNullOrEmpty(editor.Label) && !string.IsNullOrEmpty(editor.Path))
                    {
                        RelayCommand command = new(p => OpenFiles(true, editor.Label), q => HasSelection);
                        customEditorCommands.Add(command);
                        EditorMenuItems.Add(new MenuItemViewModel(editor.Label, command));

                        var kbi = KeyBindingManager.GetCustomEditorGesture(KeyCategory.Main, editor.Label);
                        if (kbi != null)
                        {
                            var kb = KeyBindingManager.CreateKeyBinding(command, kbi.KeyGesture);
                            InputBindings.Add(kb);
                            customEditorInputBindings.Add(kb);
                        }
                    }
                }
            }

            if (EditorMenuItems.Count == 0)
            {
                EditorMenuItems.Add(new MenuItemViewModel(Resources.Main_Results_Tooltip_NotConfigured, false));
            }

            InputBindings.RaiseAfterCollectionChanged();
        }

        public ObservableCollection<FormattedGrepResult> SearchResults { get; set; } = [];

        [ObservableProperty]
        private FormattedGrepResult? contextGrepResult;

        [ObservableProperty]
        private bool contextGrepResultVisible;

        /// <summary>
        /// Gets the collection of Selected tree nodes, in the order they were selected
        /// </summary>
        public ObservableCollection<ITreeItem> SelectedNodes { get; private set; }

        /// <summary>
        /// Gets a read-only collection of the selected items, in display order
        /// </summary>
        public ReadOnlyCollection<ITreeItem> SelectedItems
        {
            get
            {
                var list = SelectedNodes.Where(i => i != null).ToList();
                // sort the selected items in the order of the items appear in the tree!!!
                if (list.Count > 1)
                    list.Sort(new SelectionComparer(SearchResults));
                return list.AsReadOnly();
            }
        }

        public void DeselectAllItems()
        {
            foreach (var item in SelectedItems)
            {
                item.IsSelected = false;
            }
        }

        public void SelectItems(ICollection<ITreeItem> selections)
        {
            foreach (var item in SearchResults)
            {
                if (selections.Contains(item))
                {
                    item.IsSelected = true;
                }
            }
        }

        private class SelectionComparer(ObservableCollection<FormattedGrepResult> collection)
            : IComparer<ITreeItem>
        {
            public int Compare(ITreeItem? x, ITreeItem? y)
            {
                var fileX = x as FormattedGrepResult;
                var lineX = x as FormattedGrepLine;
                if (fileX == null && lineX != null)
                    fileX = lineX.Parent;

                var fileY = y as FormattedGrepResult;
                var lineY = y as FormattedGrepLine;
                if (fileY == null && lineY != null)
                    fileY = lineY.Parent;

                if (fileX != null && fileY != null)
                {
                    int posX;
                    int posY;
                    if (fileX == fileY && lineX != null && lineY != null)
                    {
                        posX = fileX.FormattedLines.IndexOf(lineX);
                        posY = fileX.FormattedLines.IndexOf(lineY);
                        return posX.CompareTo(posY);
                    }

                    posX = collection.IndexOf(fileX);
                    posY = collection.IndexOf(fileY);
                    return posX.CompareTo(posY);
                }
                return 0;
            }
        }

        void ObservableGrepSearchResults_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            List<ITreeItem> toRemove = [];
            foreach (var node in SelectedNodes)
            {
                if (node is FormattedGrepResult item && !SearchResults.Contains(item))
                    toRemove.Add(item);

                if (node is FormattedGrepLine line && !SearchResults.Contains(line.Parent))
                    toRemove.Add(line);
            }
            foreach (var item in toRemove)
                SelectedNodes.Remove(item);

            if (e.NewItems != null)
            {
                foreach (FormattedGrepResult newEntry in e.NewItems.Cast<FormattedGrepResult>())
                {
                    string extension = Path.GetExtension(newEntry.GrepResult.FileNameDisplayed);
                    if (extension.Length <= 1)
                        extension = ".na";
                    if (!icons.TryGetValue(extension, out BitmapSource? value))
                    {
                        var bitmapIcon = IconHandler.IconFromExtensionShell(extension, IconSize.Small) ??
                            Common.Properties.Resources.na_icon;
                        value = GetBitmapSource(bitmapIcon);
                        icons[extension] = value;
                    }
                    newEntry.Icon = value;
                }
            }
        }

        public List<GrepSearchResult> GetList()
        {
            List<GrepSearchResult> tempList = [];
            foreach (var l in SearchResults) tempList.Add(l.GrepResult);
            return tempList;
        }

        /// <summary>
        /// Gets the set of results that are writable and have matches
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GrepSearchResult> GetWritableFilesWithMatches()
        {
            foreach (var item in SearchResults)
            {
                if (item.GrepResult.Matches.Any() &&
                    !item.GrepResult.IsReadOnly)
                {
                    yield return item.GrepResult;
                }
            }
        }

        public void AddRange(List<GrepSearchResult> list)
        {
            foreach (var r in list)
            {
                if (!r.IsSuccess)
                {
                    FailureCount++;
                }

                bool showErrors = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFileErrorsInResults);
                if (r.IsSuccess || showErrors)
                {
                    var fmtResult = new FormattedGrepResult(r, FolderPath, ViewWhitespace)
                    {
                        WrapText = WrapText
                    };
                    SearchResults.Add(fmtResult);

                    // moved this check out of FormattedGrepResult constructor:
                    // does not work correctly in TestPatternView, which does not lazy load
                    if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ExpandResults))
                    {
                        fmtResult.IsExpanded = true;
                    }
                }
            }
        }

        public void AddRangeForTestView(List<GrepSearchResult> list)
        {
            foreach (var r in list)
            {
                SearchResults.Add(new FormattedGrepResult(r, FolderPath, false));
            }
        }

        public void AddRange(IEnumerable<FormattedGrepResult> items)
        {
            foreach (var item in items)
            {
                SearchResults.Add(item);
            }
        }

        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of search used for these results
        /// </summary>
        public SearchType TypeOfSearch { get; set; }

        public static BitmapSource GetBitmapSource(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            try
            {
                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());
                return bs;
            }
            finally
            {
                PInvoke.DeleteObject(new(ip));
            }
        }

        // some settings have changed, raise property changed events to update the UI
        public void RaiseSettingsPropertiesChanged()
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CustomEditorConfigured)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CompareApplicationConfigured)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(StickyScrollEnabled)));

            foreach (var item in SearchResults)
            {
                item.RaiseSettingsPropertiesChanged();
            }
        }

        public static bool CustomEditorConfigured
        {
            get { return GrepSettings.Instance.HasCustomEditor; }
        }

        public static bool CompareApplicationConfigured
        {
            get { return GrepSettings.Instance.IsSet(GrepSettings.Key.CompareApplication); }
        }

        public static bool StickyScrollEnabled
        {
            get { return GrepSettings.Instance.Get<bool>(GrepSettings.Key.StickyScroll); }
        }

        [ObservableProperty]
        private double resultsScale = 1.0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFailures))]
        [NotifyPropertyChangedFor(nameof(FileReadErrors))]
        public int failureCount;

        public bool HasFailures => FailureCount > 0;

        public string FileReadErrors => TranslationSource.Format(Resources.Error_FilesCouldNotBeSearched, FailureCount);

        public bool HasSelection
        {
            get { return SelectedNodes.Any(); }
        }

        public bool HasSingleSelection
        {
            get { return SelectedNodes.Count == 1; }
        }

        public bool HasMultipleSelection
        {
            get { return SelectedNodes.Count > 1; }
        }

        public bool HasReadOnlySelection
        {
            get
            {
                return SelectedNodes.Where(n => n is FormattedGrepResult)
                    .Select(n => n as FormattedGrepResult)
                    .Any(r => Utils.HasReadOnlyAttributeSet(r?.GrepResult));
            }
        }

        public bool HasGrepResultSelection
        {
            get { return SelectedNodes.Where(r => (r as FormattedGrepResult) != null).Any(); }
        }

        public bool HasGrepLineSelection
        {
            get { return SelectedNodes.Where(r => (r as FormattedGrepLine) != null).Any(); }
        }

        void SelectedNodes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasSelection)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasSingleSelection)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasMultipleSelection)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasReadOnlySelection)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasGrepResultSelection)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasGrepLineSelection)));
        }

        public List<GrepSearchResult> GetSelectedFiles()
        {
            List<GrepSearchResult> files = [];
            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepResult fileNode)
                {
                    if (!files.Contains(fileNode.GrepResult))
                        files.Add(fileNode.GrepResult);
                }
                if (item is FormattedGrepLine lineNode)
                {
                    if (!files.Contains(lineNode.Parent.GrepResult))
                        files.Add(lineNode.Parent.GrepResult);
                }
            }
            return files;
        }

        public List<GrepSearchResult> GetWritableSelectedFiles()
        {
            List<GrepSearchResult> files = [];
            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepResult fileNode)
                {
                    if (!files.Contains(fileNode.GrepResult))
                    {
                        if (!fileNode.GrepResult.IsReadOnly)
                            files.Add(fileNode.GrepResult);
                    }
                }
                if (item is FormattedGrepLine lineNode)
                {
                    if (!files.Contains(lineNode.Parent.GrepResult))
                    {
                        if (!lineNode.Parent.GrepResult.IsReadOnly)
                            files.Add(lineNode.Parent.GrepResult);
                    }
                }
            }
            return files;
        }


        [ObservableProperty]
        private bool wrapText;
        partial void OnWrapTextChanged(bool value)
        {
            foreach (var item in SearchResults)
            {
                item.WrapText = value;
            }
        }

        [ObservableProperty]
        private bool viewWhitespace;
        partial void OnViewWhitespaceChanged(bool value)
        {
            foreach (var item in SearchResults)
            {
                item.ViewWhitespace = value;
                item.SetLabel();
            }
        }

        [ObservableProperty]
        private bool isResultsTreeFocused;

        public event EventHandler<GrepLineEventArgs>? OpenFileLineRequest;
        public event EventHandler<GrepResultEventArgs>? OpenFileRequest;
        public event EventHandler<GrepLineEventArgs>? PreviewFileLineRequest;
        public event EventHandler<GrepResultEventArgs>? PreviewFileRequest;
        public event EventHandler<GrepLineSelectEventArgs>? GrepLineSelected;

        public void OpenFile(FormattedGrepLine line, bool useCustomEditor, string customEditorName)
        {
            OpenFileLineRequest?.Invoke(this, new GrepLineEventArgs { FormattedGrepLine = line, UseCustomEditor = useCustomEditor, CustomEditorName = customEditorName });
        }

        public void OpenFile(FormattedGrepResult line, bool useCustomEditor, string customEditorName)
        {
            OpenFileRequest?.Invoke(this, new GrepResultEventArgs { FormattedGrepResult = line, UseCustomEditor = useCustomEditor, CustomEditorName = customEditorName });
        }

        public void PreviewFile(FormattedGrepLine line, System.Drawing.RectangleF windowSize)
        {
            PreviewFileLineRequest?.Invoke(this, new GrepLineEventArgs { FormattedGrepLine = line, ParentWindowSize = windowSize });
        }

        public void PreviewFile(FormattedGrepResult line, System.Drawing.RectangleF windowSize)
        {
            PreviewFileRequest?.Invoke(this, new GrepResultEventArgs { FormattedGrepResult = line, ParentWindowSize = windowSize });
        }

        internal void OnGrepLineSelectionChanged(FormattedGrepLine? formattedGrepLine, int lineMatchCount, int matchOrdinal, int fileMatchCount)
        {
            GrepLineSelected?.Invoke(this, new GrepLineSelectEventArgs(formattedGrepLine, lineMatchCount, matchOrdinal, fileMatchCount));
        }

        #region Commands

        private RelayCommand? openContainingFolderCommand;
        public RelayCommand OpenContainingFolderCommand => openContainingFolderCommand ??= new RelayCommand(
            p => OpenFolders(),
            q => HasSelection);

        private RelayCommand? openExplorerMenuCommand;
        public RelayCommand OpenExplorerMenuCommand => openExplorerMenuCommand ??= new RelayCommand(
            p => OpenExplorerMenu(),
            q => HasSelection);

        private RelayCommand? openFilesCommand;
        public RelayCommand OpenFilesCommand => openFilesCommand ??= new RelayCommand(
            p => OpenFiles(false, string.Empty),
            q => HasSelection);

        private RelayCommand? renameFileCommand;
        public RelayCommand RenameFileCommand => renameFileCommand ??= new RelayCommand(
            p => RenameFile(),
            q => HasSingleSelection);

        private RelayCommand? copyFileNamesCommand;
        public RelayCommand CopyFileNamesCommand => copyFileNamesCommand ??= new RelayCommand(
            p => CopyFileNames(false),
            q => HasSelection);

        private RelayCommand? copyFullFilePathsCommand;
        public RelayCommand CopyFullFilePathsCommand => copyFullFilePathsCommand ??= new RelayCommand(
            p => CopyFileNames(true),
            q => HasSelection);

        private RelayCommand? copyGrepLinesCommand;
        public RelayCommand CopyGrepLinesCommand => copyGrepLinesCommand ??= new RelayCommand(
            p => CopyGrepLines(),
            q => HasGrepLineSelection);

        private RelayCommand? showFilePropertiesCommand;
        public RelayCommand ShowFilePropertiesCommand => showFilePropertiesCommand ??= new RelayCommand(
            p => ShowFileProperties(),
            q => HasSelection);

        private RelayCommand? makeWritableCommand;
        public RelayCommand MakeWritableCommand => makeWritableCommand ??= new RelayCommand(
            p => MakeFilesWritable(),
            q => HasReadOnlySelection);

        private RelayCommand? copyFilesCommand;
        public RelayCommand CopyFilesCommand => copyFilesCommand ??= new RelayCommand(
            p => CopyFiles(),
            q => HasSelection);

        private RelayCommand? moveFilesCommand;
        public RelayCommand MoveFilesCommand => moveFilesCommand ??= new RelayCommand(
            p => MoveFiles(),
            q => HasSelection);

        private RelayCommand? deleteFilesCommand;
        public RelayCommand DeleteFilesCommand => deleteFilesCommand ??= new RelayCommand(
            p => DeleteFiles(),
            q => HasSelection);

        private RelayCommand? recycleFilesCommand;
        public RelayCommand RecycleFilesCommand => recycleFilesCommand ??= new RelayCommand(
            p => RecycleFiles(),
            q => HasSelection);

        private RelayCommand? compareFilesCommand;
        public RelayCommand CompareFilesCommand => compareFilesCommand ??= new RelayCommand(
            p => CompareFiles(),
            q => CanCompareFiles);

        // Ctrl+C
        private RelayCommand? copyCommand;
        public RelayCommand CopyCommand => copyCommand ??= new RelayCommand(
            param => Copy(),
            q => HasSelection);

        // Ctrl+A
        private RelayCommand? selectAllCommand;
        public RelayCommand SelectAllCommand => selectAllCommand ??= new RelayCommand(
            p => SelectAll());

        // Ctrl+Home
        private RelayCommand? selectToStartCommand;
        public RelayCommand SelectToStartCommand => selectToStartCommand ??= new RelayCommand(
            p => SelectToStart(),
            q => HasSelection);

        // Ctrl+End
        private RelayCommand? selectToEndCommand;
        public RelayCommand SelectToEndCommand => selectToEndCommand ??= new RelayCommand(
            p => SelectToEnd(),
            q => HasSelection);

        // Delete
        private RelayCommand? excludeFilesCommand;
        public RelayCommand ExcludeFilesCommand => excludeFilesCommand ??= new RelayCommand(
            p => ExcludeFiles(),
            q => HasSelection);

        // F3
        private RelayCommand? nextLineCommand;
        public RelayCommand NextLineCommand => nextLineCommand ??= new RelayCommand(
            p => NextLine());

        // Shift+F3
        private RelayCommand? nextFileCommand;
        public RelayCommand NextFileCommand => nextFileCommand ??= new RelayCommand(
            p => NextFile());

        // F4
        private RelayCommand? previousLineCommand;
        public RelayCommand PreviousLineCommand => previousLineCommand ??= new RelayCommand(
            p => PreviousLine());

        // F4
        private RelayCommand? previousFileCommand;
        public RelayCommand PreviousFileCommand => previousFileCommand ??= new RelayCommand(
            p => PreviousFile());

        // F6
        private RelayCommand? expandAllCommand;
        public RelayCommand ExpandAllCommand => expandAllCommand ??= new RelayCommand(
            p => ExpandAll());

        // Shift+F6
        private RelayCommand? collapseAllCommand;
        public RelayCommand CollapseAllCommand => collapseAllCommand ??= new RelayCommand(
            p => CollapseAll());

        private RelayCommand? resetZoomCommand;
        public RelayCommand ResetZoomCommand => resetZoomCommand ??= new RelayCommand(
            p => ResultsScale = 1.0,
            q => true);


        #endregion

        #region Command Implementation

        private void OpenFiles(bool useCustomEditor, string customEditorName)
        {
            // get the unique set of file names to open from the selections
            // keep the first record from each file to use when opening the file
            // prefer to open by line, if any line is selected; otherwise by file

            List<string> fileNames = [];
            List<FormattedGrepLine> lines = [];
            List<FormattedGrepResult> files = [];
            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepLine lineNode)
                {
                    string name = lineNode.Parent.GrepResult.FileNameReal;
                    if (!fileNames.Contains(name))
                    {
                        fileNames.Add(name);
                        lines.Add(lineNode);
                    }
                }
            }

            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepResult fileNode)
                {
                    string name = fileNode.GrepResult.FileNameReal;
                    if (!fileNames.Contains(name))
                    {
                        fileNames.Add(name);
                        files.Add(fileNode);
                    }
                }
            }

            foreach (var item in lines)
                OpenFile(item, useCustomEditor, customEditorName);

            foreach (var item in files)
                OpenFile(item, useCustomEditor, customEditorName);
        }

        private void OpenFolders()
        {
            // get the unique set of folders from the selections
            // keep the first file from each folder to open the folder

            List<string> folders = [];
            List<string> files = [];
            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepResult fileNode)
                {
                    string name = fileNode.GrepResult.FileNameReal;
                    string? path = Path.GetDirectoryName(name);
                    if (!string.IsNullOrEmpty(path) && !folders.Contains(path))
                    {
                        folders.Add(path);
                        files.Add(name);
                    }
                }
                if (item is FormattedGrepLine lineNode)
                {
                    string name = lineNode.Parent.GrepResult.FileNameReal;
                    string? path = Path.GetDirectoryName(name);
                    if (!string.IsNullOrEmpty(path) && !folders.Contains(path))
                    {
                        folders.Add(path);
                        files.Add(name);
                    }
                }
            }

            foreach (var fileName in files)
                Utils.OpenContainingFolder(fileName);
        }

        private void OpenExplorerMenu()
        {
            // get the unique set of files from the selections
            List<string> files = [];
            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepResult fileNode)
                {
                    string name = fileNode.GrepResult.FileNameReal;
                    if (!files.Contains(name) && File.Exists(name))
                    {
                        files.Add(name);
                    }
                }
                if (item is FormattedGrepLine lineNode)
                {
                    string name = lineNode.Parent.GrepResult.FileNameReal;
                    if (!files.Contains(name) && File.Exists(name))
                    {
                        files.Add(name);
                    }
                }
            }

            if (files.Count > 0 && TreeControl != null)
            {
                ShellContextMenu menu = new();
                menu.ShowContextMenu(files.Select(f => new FileInfo(f)).ToArray(),
                    TreeControl.PointToScreen(Mouse.GetPosition(TreeControl)));
            }
        }

        private void RenameFile()
        {
            FormattedGrepResult? searchResult = null;
            var node = SelectedNodes.FirstOrDefault();

            if (node is FormattedGrepLine lineNode)
            {
                searchResult = lineNode.Parent;
            }
            else if (node is FormattedGrepResult fileNode)
            {
                searchResult = fileNode;
            }

            if (searchResult != null && searchResult.GrepResult != null &&
                !string.IsNullOrEmpty(searchResult.GrepResult.FileNameReal))
            {
                var grepResult = searchResult.GrepResult;
                var dlg = new RenameWindow
                {
                    Owner = Application.Current.MainWindow,
                    SourcePath = grepResult.FileNameReal
                };

                var result = dlg.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    string destPath = dlg.DestinationPath;
                    if (!string.IsNullOrEmpty(destPath) && !File.Exists(destPath))
                    {
                        try
                        {
                            string ext = string.Empty;
                            if (grepResult.FileNameReal != grepResult.FileNameDisplayed)
                            {
                                int index = grepResult.FileNameDisplayed.IndexOf(grepResult.FileNameReal, StringComparison.Ordinal);
                                if (index >= 0)
                                    ext = grepResult.FileNameDisplayed.Remove(index, grepResult.FileNameReal.Length);
                            }

                            File.Move(grepResult.FileNameReal, destPath);

                            grepResult.FileNameReal = destPath;
                            grepResult.FileNameDisplayed = destPath + ext;

                            // update label in the results tree
                            searchResult.SetLabel();
                            // update label on the preview window
                            TreeControl?.OnSelectedItemsChanged();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(Resources.MessageBox_RenameFailed + ex.Message,
                                Resources.MessageBox_DnGrep + " " + Resources.MessageBox_RenameFile,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                }
            }
        }

        private void CopyFileNames(bool showFullName)
        {
            var list = GetSelectedFileNames(showFullName);
            if (list.Count > 0)
                NativeMethods.SetClipboardText(string.Join(Environment.NewLine, (ReadOnlySpan<string?>)[.. list]));
        }

        private void CopyGrepLines()
        {
            var lines = GetSelectedGrepLineText();
            if (!string.IsNullOrWhiteSpace(lines))
                NativeMethods.SetClipboardText(lines);
        }

        private void CopyFiles()
        {
            var (files, indexOfFirst) = GetSelectedFilesInt();
            if (files.Count > 0)
            {
                var fileList = files.Select(f => f.GrepResult).ToList();
                var (success, message) = FileOperations.CopyFiles(
                    fileList, PathSearchText, null, false);
            }
        }

        private void MoveFiles()
        {
            if (TreeControl == null) return;

            var (files, indexOfFirst) = GetSelectedFilesInt();
            if (files.Count > 0)
            {
                var fileList = files.Select(f => f.GrepResult).ToList();
                var (success, filesMoved, message) = FileOperations.MoveFiles(
                    fileList, PathSearchText, null, false);

                if (success)
                {
                    DeselectAllItems();
                    foreach (var gr in files)
                    {
                        if (filesMoved.Contains(gr.GrepResult.FileNameReal, StringComparison.Ordinal))
                        {
                            SearchResults.Remove(gr);
                        }
                    }

                    if (indexOfFirst > -1 && SearchResults.Count > 0)
                    {
                        // the first item was removed, select the new item in that position
                        int idx = indexOfFirst;
                        if (idx >= SearchResults.Count) idx = SearchResults.Count - 1;

                        var nextResult = SearchResults[idx];
                        var tvi = ResultsTree.GetTreeViewItem(TreeControl.TreeView, nextResult, null, SearchDirection.Down, 1);
                        if (tvi != null)
                        {
                            tvi.IsSelected = false;
                            tvi.IsSelected = true;
                        }
                    }
                }
            }
        }

        private void DeleteFiles()
        {
            if (TreeControl == null) return;

            var (files, indexOfFirst) = GetSelectedFilesInt();
            if (files.Count > 0)
            {
                var fileList = files.Select(f => f.GrepResult).ToList();
                var (success, filesDeleted, message) = FileOperations.DeleteFiles(
                    fileList, false, false);

                if (success)
                {
                    DeselectAllItems();
                    foreach (var gr in files)
                    {
                        if (filesDeleted.Contains(gr.GrepResult.FileNameReal))
                        {
                            SearchResults.Remove(gr);
                        }
                    }

                    if (indexOfFirst > -1 && SearchResults.Count > 0)
                    {
                        // the first item was removed, select the new item in that position
                        int idx = indexOfFirst;
                        if (idx >= SearchResults.Count) idx = SearchResults.Count - 1;

                        var nextResult = SearchResults[idx];
                        var tvi = ResultsTree.GetTreeViewItem(TreeControl.TreeView, nextResult, null, SearchDirection.Down, 1);
                        if (tvi != null)
                        {
                            tvi.IsSelected = false;
                            tvi.IsSelected = true;
                        }
                    }
                }
            }
        }

        private void RecycleFiles()
        {
            if (TreeControl == null) return;

            var (files, indexOfFirst) = GetSelectedFilesInt();
            DeselectAllItems();
            foreach (var gr in files)
            {
                FileSystem.DeleteFile(gr.GrepResult.FileNameReal,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);


                SearchResults.Remove(gr);
            }

            if (indexOfFirst > -1 && SearchResults.Count > 0)
            {
                // the first item was removed, select the new item in that position
                int idx = indexOfFirst;
                if (idx >= SearchResults.Count) idx = SearchResults.Count - 1;

                var nextResult = SearchResults[idx];
                var tvi = ResultsTree.GetTreeViewItem(TreeControl.TreeView, nextResult, null, SearchDirection.Down, 1);
                if (tvi != null)
                {
                    tvi.IsSelected = false;
                    tvi.IsSelected = true;
                }
            }
        }


        public bool CanCompareFiles
        {
            get
            {
                if (CompareApplicationConfigured)
                {
                    int count = GetSelectedFiles().Count;
                    return count == 2 || count == 3;
                }
                return false;
            }
        }

        private void CompareFiles()
        {
            var files = GetSelectedFiles();
            if (files.Count == 2 || files.Count == 3)
                Utils.CompareFiles(files);
        }

        internal string GetSelectedGrepLineText()
        {
            if (HasGrepLineSelection)
            {
                StringBuilder sb = new();
                foreach (var item in SelectedItems)
                {
                    if (item is FormattedGrepLine node)
                    {
                        sb.AppendLine(node.GrepLine.LineText);
                    }
                }

                return sb.ToString().TrimEndOfLine();
            }
            return string.Empty;
        }

        internal List<string> GetSelectedFileNames(bool showFullName)
        {
            List<string> list = [];
            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepResult fileNode)
                {
                    string name = fileNode.GrepResult.FileNameDisplayed;
                    if (!showFullName)
                        name = Path.GetFileName(name);

                    if (!list.Contains(name))
                        list.Add(name);
                }
                if (item is FormattedGrepLine lineNode)
                {
                    string name = lineNode.Parent.GrepResult.FileNameDisplayed;
                    if (!showFullName)
                        name = Path.GetFileName(name);

                    if (!list.Contains(name))
                        list.Add(name);
                }
            }
            return list;
        }

        private (IList<FormattedGrepResult> files, int indexOfFirst) GetSelectedFilesInt()
        {
            // get the unique set of files from the selections
            List<FormattedGrepResult> files = [];
            int indexOfFirst = -1;

            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepResult fileNode)
                {
                    string name = fileNode.GrepResult.FileNameReal;
                    if (!files.Any(gr => gr.GrepResult.FileNameReal.Equals(name, StringComparison.Ordinal)) && File.Exists(name))
                    {
                        files.Add(fileNode);
                    }
                }
                if (item is FormattedGrepLine lineNode)
                {
                    string name = lineNode.Parent.GrepResult.FileNameReal;
                    if (!files.Any(gr => gr.GrepResult.FileNameReal.Equals(name, StringComparison.Ordinal)) && File.Exists(name))
                    {
                        files.Add(lineNode.Parent);
                    }
                }

                if (files.Count == 1)
                {
                    indexOfFirst = SearchResults.IndexOf(files.First());
                }
            }
            return (files, indexOfFirst);
        }

        private void Copy()
        {
            if (HasGrepLineSelection)
            {
                CopyGrepLines();
            }
            else if (HasGrepResultSelection)
            {
                CopyFileNames(true);
            }
        }

        private void SelectAll()
        {
            TreeControl?.TreeView.DeselectAllChildItems();

            foreach (var item in SearchResults)
            {
                item.IsSelected = true;

                if (item.IsExpanded)
                {
                    foreach (var child in item.Children)
                    {
                        child.IsSelected = true;
                    }
                }
            }
        }

        private void SelectToStart()
        {
            var startTreeViewItem = TreeControl?.TreeView.StartTreeViewItem;
            if (startTreeViewItem != null && startTreeViewItem.DataContext is ITreeItem startItem)
            {
                TreeControl?.TreeView.DeselectAllChildItems();

                if (startItem is FormattedGrepLine line)
                {
                    startItem = line.Parent;
                    if (!startItem.IsSelected)
                        startItem.IsSelected = true;
                }

                bool isSelecting = false;
                foreach (var item in SearchResults.Reverse())
                {
                    if (item == startItem)
                    {
                        isSelecting = true;
                    }
                    else if (isSelecting)
                    {
                        item.IsSelected = true;

                        if (item.IsExpanded)
                        {
                            foreach (var child in item.Children)
                            {
                                child.IsSelected = true;
                            }
                        }
                    }
                }
            }
        }

        private void SelectToEnd()
        {
            var startTreeViewItem = TreeControl?.TreeView.StartTreeViewItem;
            if (startTreeViewItem != null && startTreeViewItem.DataContext is ITreeItem startItem)
            {
                TreeControl?.TreeView.DeselectAllChildItems();

                if (startItem is FormattedGrepLine line)
                {
                    startItem = line.Parent;
                    if (!startItem.IsSelected)
                        startItem.IsSelected = true;
                }

                bool isSelecting = false;
                foreach (var item in SearchResults)
                {
                    if (item == startItem)
                    {
                        isSelecting = true;
                    }
                    else if (isSelecting)
                    {
                        item.IsSelected = true;

                        if (item.IsExpanded)
                        {
                            foreach (var child in item.Children)
                            {
                                child.IsSelected = true;
                            }
                        }
                    }
                }
            }
        }

        private void ExcludeFiles()
        {
            List<FormattedGrepResult> files = [];
            int indexOfFirst = -1;
            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepLine lineNode)
                {
                    var grepResult = lineNode.Parent;
                    if (!files.Contains(grepResult))
                    {
                        files.Add(grepResult);
                    }
                }
                if (item is FormattedGrepResult fileNode)
                {
                    if (!files.Contains(fileNode))
                    {
                        files.Add(fileNode);
                    }
                }

                if (files.Count == 1)
                {
                    indexOfFirst = SearchResults.IndexOf(files.First());
                }
            }

            DeselectAllItems();

            foreach (var item in files)
            {
                SearchResults.Remove(item);
            }

            if (indexOfFirst > -1 && SearchResults.Count > 0 &&
                TreeControl != null)
            {
                // the first item was removed, select the new item in that position
                int idx = indexOfFirst;
                if (idx >= SearchResults.Count) idx = SearchResults.Count - 1;

                var nextResult = SearchResults[idx];
                var tvi = ResultsTree.GetTreeViewItem(TreeControl.TreeView, nextResult, null, SearchDirection.Down, 1);
                if (tvi != null)
                {
                    tvi.IsSelected = false;
                    tvi.IsSelected = true;
                }
            }
        }

        private void ShowFileProperties()
        {
            // get the unique set of files from the selections
            List<string> files = [];
            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepResult fileNode)
                {
                    string name = fileNode.GrepResult.FileNameReal;
                    if (!files.Contains(name) && File.Exists(name))
                    {
                        files.Add(name);
                    }
                }
                if (item is FormattedGrepLine lineNode)
                {
                    string name = lineNode.Parent.GrepResult.FileNameReal;
                    if (!files.Contains(name) && File.Exists(name))
                    {
                        files.Add(name);
                    }
                }
            }

            foreach (var fileName in files)
                ShellIntegration.ShowFileProperties(fileName);
        }

        private void MakeFilesWritable()
        {
            List<FormattedGrepResult> files = [];
            foreach (var item in SelectedItems)
            {
                if (item is FormattedGrepLine lineNode)
                {
                    var grepResult = lineNode.Parent;
                    if (!files.Contains(grepResult))
                    {
                        files.Add(grepResult);
                    }
                }
                if (item is FormattedGrepResult fileNode)
                {
                    if (!files.Contains(fileNode))
                    {
                        files.Add(fileNode);
                    }
                }
            }

            foreach (var item in files)
            {
                if (File.Exists(item.GrepResult.FileNameReal))
                {
                    var info = new FileInfo(item.GrepResult.FileNameReal);
                    if (info.IsReadOnly)
                    {
                        info.IsReadOnly = false;
                        item.SetLabel();
                    }
                }
            }
        }

        private async void NextLine()
        {
            try
            {
                if (TreeControl != null)
                {
                    await TreeControl.Next();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Calling TreeControl.Next()");
            }
        }

        private async void NextFile()
        {
            try
            {
                if (TreeControl != null)
                {
                    await TreeControl.NextFile();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Calling TreeControl.NextFile()");
            }
        }

        private async void PreviousLine()
        {
            try
            {
                if (TreeControl != null)
                {
                    await TreeControl.Previous();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Calling TreeControl.Previous()");
            }
        }

        private async void PreviousFile()
        {
            try
            {
                if (TreeControl != null)
                {
                    await TreeControl.PreviousFile();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Calling TreeControl.PreviousFile()");
            }
        }

        private async void ExpandAll()
        {
            try
            {
                if (TreeControl != null)
                {
                    await TreeControl.ExpandAll();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Calling TreeControl.ExpandAll()");
            }
        }

        private void CollapseAll()
        {
            try
            {
                TreeControl?.CollapseAll();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Calling TreeControl.CollapseAll()");
            }
        }



        #endregion
    }
}
