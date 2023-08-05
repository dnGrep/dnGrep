using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using dnGREP.WPF.MVHelpers;
using dnGREP.WPF.UserControls;
using Windows.Win32;

namespace dnGREP.WPF
{
    public partial class GrepSearchResultsViewModel : CultureAwareViewModel
    {
        public static readonly Messenger SearchResultsMessenger = new();

        public GrepSearchResultsViewModel()
        {
            SelectedNodes = new ObservableCollection<ITreeItem>();
            SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;
            SearchResults.CollectionChanged += ObservableGrepSearchResults_CollectionChanged;

            SearchResultsMessenger.Register<ITreeItem>("IsSelectedChanged", OnSelectionChanged);
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

        private readonly Dictionary<string, BitmapSource> icons = new();

        public ObservableCollection<FormattedGrepResult> SearchResults { get; set; } = new();

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

        private class SelectionComparer : IComparer<ITreeItem>
        {
            private readonly ObservableCollection<FormattedGrepResult> collection;
            public SelectionComparer(ObservableCollection<FormattedGrepResult> collection)
            {
                this.collection = collection;
            }

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
            List<ITreeItem> toRemove = new();
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
                    if (!icons.ContainsKey(extension))
                    {
                        var bitmapIcon = IconHandler.IconFromExtensionShell(extension, IconSize.Small) ??
                            Common.Properties.Resources.na_icon;
                        icons[extension] = GetBitmapSource(bitmapIcon);
                    }
                    newEntry.Icon = icons[extension];
                }
            }
        }

        public GrepSearchResultsViewModel(List<GrepSearchResult> list)
            : this()
        {
            AddRange(list);
        }

        public List<GrepSearchResult> GetList()
        {
            List<GrepSearchResult> tempList = new();
            foreach (var l in SearchResults) tempList.Add(l.GrepResult);
            return tempList;
        }

        /// <summary>
        /// Gets the list of results that are writable
        /// </summary>
        /// <returns></returns>
        public List<GrepSearchResult> GetWritableList()
        {
            List<GrepSearchResult> writableFiles = new();
            foreach (var item in SearchResults)
            {
                if (!Utils.IsReadOnly(item.GrepResult))
                {
                    writableFiles.Add(item.GrepResult);
                }
            }
            return writableFiles;
        }

        public void AddRange(List<GrepSearchResult> list)
        {
            foreach (var l in list)
            {
                var fmtResult = new FormattedGrepResult(l, FolderPath)
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

        public void AddRangeForTestView(List<GrepSearchResult> list)
        {
            foreach (var l in list)
            {
                SearchResults.Add(new FormattedGrepResult(l, FolderPath));
            }
        }

        public void AddRange(IEnumerable<FormattedGrepResult> items)
        {
            foreach (var item in items)
                SearchResults.Add(item);
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

            foreach (var item in SearchResults)
            {
                item.RaiseSettingsPropertiesChanged();
            }
        }
        public bool CustomEditorConfigured
        {
            get { return GrepSettings.Instance.IsSet(GrepSettings.Key.CustomEditor); }
        }

        public bool CompareApplicationConfigured
        {
            get { return GrepSettings.Instance.IsSet(GrepSettings.Key.CompareApplication); }
        }

        [ObservableProperty]
        private double resultsScale = 1.0;

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

        public ICommand CompareFilesCommand => new RelayCommand(
            p => CompareFiles(),
            q => CanCompareFiles);

        private IList<GrepSearchResult> GetSelectedFiles()
        {
            List<GrepSearchResult> files = new();
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
        private bool isResultsTreeFocused;

        public event EventHandler<GrepLineEventArgs>? OpenFileLineRequest;
        public event EventHandler<GrepResultEventArgs>? OpenFileRequest;
        public event EventHandler<GrepLineEventArgs>? PreviewFileLineRequest;
        public event EventHandler<GrepResultEventArgs>? PreviewFileRequest;
        public event EventHandler<GrepLineSelectEventArgs>? GrepLineSelected;

        public void OpenFile(FormattedGrepLine line, bool useCustomEditor)
        {
            OpenFileLineRequest?.Invoke(this, new GrepLineEventArgs { FormattedGrepLine = line, UseCustomEditor = useCustomEditor });
        }

        public void OpenFile(FormattedGrepResult line, bool useCustomEditor)
        {
            OpenFileRequest?.Invoke(this, new GrepResultEventArgs { FormattedGrepResult = line, UseCustomEditor = useCustomEditor });
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
    }

    public partial class FormattedGrepResult : CultureAwareViewModel, ITreeItem
    {
        public GrepSearchResult GrepResult { get; private set; } = new();

        public int Matches
        {
            get { return GrepResult.Matches.Count; }
        }

        public string Style { get; private set; } = string.Empty;

        [ObservableProperty]
        private string label = string.Empty;

        internal int MatchIdx { get; set; }

        internal Dictionary<string, string> GroupColors { get; } = new();

        public static bool ShowFileInfoTooltips
        {
            get { return GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFileInfoTooltips); }
        }

        // some settings have changed, raise property changed events to update the UI
        public void RaiseSettingsPropertiesChanged()
        {
            OnPropertyChanged(nameof(ShowFileInfoTooltips));
        }

        public async Task ExpandTreeNode()
        {
            if (!FormattedLines.IsLoaded)
            {
                IsLoading = true;
                await FormattedLines.LoadAsync();
                IsLoading = false;
            }
            IsExpanded = true;
        }

        internal void CollapseTreeNode()
        {
            IsExpanded = false;
        }

        [ObservableProperty]
        private bool isExpanded = false;
        partial void OnIsExpandedChanged(bool value)
        {
            if (value == true && !FormattedLines.IsLoaded && !FormattedLines.IsLoading)
            {
                IsLoading = true;
                Task.Run(() => FormattedLines.LoadAsync());
            }
        }

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isSelected;
        partial void OnIsSelectedChanged(bool value)
        {
            GrepSearchResultsViewModel.SearchResultsMessenger.NotifyColleagues("IsSelectedChanged", this);
        }

        [ObservableProperty]
        private int lineNumberColumnWidth = 30;

        public BitmapSource? Icon { get; set; }

        public LazyResultsList FormattedLines { get; private set; }

        private readonly string searchFolderPath;

        public FormattedGrepResult(GrepSearchResult result, string folderPath)
        {
            GrepResult = result;

            searchFolderPath = folderPath;
            SetLabel();

            FormattedLines = new LazyResultsList(result, this);
            FormattedLines.LineNumberColumnWidthChanged += FormattedLines_PropertyChanged;
            FormattedLines.LoadFinished += FormattedLines_LoadFinished;
        }

        internal void SetLabel()
        {
            bool isFileReadOnly = Utils.IsReadOnly(GrepResult);

            string basePath = string.IsNullOrWhiteSpace(searchFolderPath) ? string.Empty : searchFolderPath;
            string displayedName = Path.GetFileName(GrepResult.FileNameDisplayed);

            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFilePathInResults) &&
                GrepResult.FileNameDisplayed.Contains(basePath, StringComparison.CurrentCultureIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(basePath))
                    displayedName = GrepResult.FileNameDisplayed[basePath.Length..].TrimStart('\\');
                else
                    displayedName = GrepResult.FileNameDisplayed;
            }
            if (!string.IsNullOrWhiteSpace(GrepResult.AdditionalInformation))
            {
                displayedName += " " + GrepResult.AdditionalInformation + " ";
            }
            int matchCount = GrepResult.Matches == null ? 0 : GrepResult.Matches.Count;
            if (matchCount > 0)
            {
                if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount) && !GrepResult.IsHexFile)
                {
                    var lineCount = GrepResult.Matches?.Where(r => r.LineNumber > 0)
                       .Select(r => r.LineNumber).Distinct().Count() ?? 0;
                    displayedName = TranslationSource.Format(Resources.Main_ResultList_CountMatchesOnLines, displayedName, matchCount, lineCount);
                }
                else
                {
                    displayedName = string.Format(Resources.Main_ResultList_CountMatches, displayedName, matchCount);
                }
            }
            if (isFileReadOnly)
            {
                displayedName = displayedName + " " + Resources.Main_ResultList_ReadOnly;
            }

            Label = displayedName;

            Style = string.Empty;
            if (isFileReadOnly)
            {
                Style = "ReadOnly";
            }

            if (!GrepResult.IsSuccess)
            {
                Style = "Error";
            }

            if (!string.IsNullOrEmpty(GrepResult.FileInfo.ErrorMsg))
            {
                Style = "Error";
            }
            OnPropertyChanged(nameof(Style));
        }

        void FormattedLines_LoadFinished(object? sender, EventArgs e)
        {
            IsLoading = false;
        }

        void FormattedLines_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LineNumberColumnWidth")
                LineNumberColumnWidth = FormattedLines.LineNumberColumnWidth;
        }

        public ObservableCollection<FormattedGrepMatch> FormattedMatches { get; } = new();

        [ObservableProperty]
        private bool wrapText;
        partial void OnWrapTextChanged(bool value)
        {
            foreach (var item in FormattedLines)
            {
                item.WrapText = value;
            }
        }

        public int Level => 0;

        public IEnumerable<ITreeItem> Children => FormattedLines;
    }

    public partial class FormattedGrepLine : CultureAwareViewModel, ITreeItem
    {
        private readonly string enQuad = char.ConvertFromUtf32(0x2000);

        public FormattedGrepLine(GrepLine line, FormattedGrepResult parent, int initialColumnWidth, bool breakSection)
        {
            Parent = parent;
            GrepLine = line;
            Parent.PropertyChanged += Parent_PropertyChanged;
            LineNumberColumnWidth = initialColumnWidth;
            IsSectionBreak = breakSection;
            WrapText = Parent.WrapText;
            int lineSize = GrepSettings.Instance.Get<int>(GrepSettings.Key.HexResultByteLength);
            var pdfNumberStyle = GrepSettings.Instance.Get<PdfNumberType>(GrepSettings.Key.PdfNumberStyle);

            LineNumberAlignment = TranslationSource.Instance.CurrentCulture.TextInfo.IsRightToLeft ? TextAlignment.Left : TextAlignment.Right;

            if (pdfNumberStyle == PdfNumberType.PageNumber && line.PageNumber > -1)
            {
                FormattedLineNumber = line.PageNumber.ToString();
            }
            else
            {
                FormattedLineNumber = line.LineNumber == -1 ? string.Empty :
                    line.IsHexFile ? string.Format("{0:X8}", (line.LineNumber - 1) * lineSize) :
                    line.LineNumber.ToString();
            }

            //string fullText = lineSummary;
            if (line.IsContext)
            {
                Style = "Context";
            }
            if (line.LineNumber == -1 && string.IsNullOrEmpty(line.LineText))
            {
                Style = "Empty";
            }
        }

        public GrepLine GrepLine { get; private set; }
        public string FormattedLineNumber { get; private set; }

        public TextAlignment LineNumberAlignment { get; private set; } = TextAlignment.Right;

        private InlineCollection? formattedText;
        public InlineCollection? FormattedText
        {
            get
            {
                LoadFormattedText();
                return formattedText;
            }
        }

        public void LoadFormattedText()
        {
            if (formattedText == null || formattedText.Count == 0)
            {
                formattedText = FormatLine(GrepLine);

                if (GrepLine.IsHexFile)
                {
                    IsHexData = true;
                    ResultColumn1Width = "Auto";
                    ResultColumn2Width = "Auto";
                    ResultColumn1SharedSizeGroupName = "COL1";
                    FormattedHexValues = FormatHexValues(GrepLine);
                }
                else
                {
                    IsHexData = false;
                    ResultColumn1Width = "*";
                    ResultColumn2Width = "0";
                    ResultColumn1SharedSizeGroupName = null;
                }
            }
        }

        [ObservableProperty]
        private string? formattedHexValues;

        [ObservableProperty]
        private bool isHexData;

        [ObservableProperty]
        private string? resultColumn1SharedSizeGroupName = null; // cannot be empty string, but looks like null works

        [ObservableProperty]
        private string resultColumn1Width = "*";

        [ObservableProperty]
        private string resultColumn2Width = "0";

        // FormattedGrepLines don't expand, but the XAML code expects this property on TreeViewItems
        public bool IsExpanded { get; set; }

        [ObservableProperty]
        private bool isSelected;
        partial void OnIsSelectedChanged(bool value)
        {
            GrepSearchResultsViewModel.SearchResultsMessenger.NotifyColleagues("IsSelectedChanged", this);
        }

        [ObservableProperty]
        private bool isSectionBreak = false;

        public string Style { get; private set; } = "";

        [ObservableProperty]
        private int lineNumberColumnWidth = 30;

        void Parent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LineNumberColumnWidth))
                LineNumberColumnWidth = Parent.LineNumberColumnWidth;
        }

        public static bool HighlightCaptureGroups
        {
            get { return GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightCaptureGroups); }
        }

        public FormattedGrepResult Parent { get; private set; }

        [ObservableProperty]
        private bool wrapText;
        partial void OnWrapTextChanged(bool value)
        {
            MaxLineLength = value ? 10000 : 500;
        }

        public int MaxLineLength { get; private set; } = 500;

        public int Level => 1;

        public IEnumerable<ITreeItem> Children => Enumerable.Empty<ITreeItem>();

        private InlineCollection FormatLine(GrepLine line)
        {
            Paragraph paragraph = new();

            string fullLine = line.LineText;
            if (line.LineText.Length > MaxLineLength)
            {
                fullLine = line.LineText[..MaxLineLength];
            }

            if (line.Matches.Count == 0)
            {
                Run mainRun = new(fullLine);
                paragraph.Inlines.Add(mainRun);
            }
            else
            {
                int counter = 0;
                GrepMatch[] lineMatches = new GrepMatch[line.Matches.Count];
                line.Matches.CopyTo(lineMatches);
                foreach (GrepMatch m in lineMatches)
                {
                    Parent.MatchIdx++;
                    int matchStartLocation = m.StartLocation;
                    int matchLength = m.Length;
                    if (matchStartLocation < counter)
                    {
                        // overlapping match: continue highlight from previous end
                        int overlap = counter - matchStartLocation;
                        matchStartLocation = counter;
                        matchLength -= overlap;
                    }

                    try
                    {
                        string? regLine = null;
                        string? fmtLine = null;
                        if (matchStartLocation < fullLine.Length)
                        {
                            regLine = fullLine[counter..matchStartLocation];
                        }

                        if (matchStartLocation + matchLength <= fullLine.Length)
                        {
                            fmtLine = fullLine.Substring(matchStartLocation, matchLength);
                        }
                        else if (fullLine.Length > matchStartLocation)
                        {
                            // match may include the non-printing newline chars at the end of the line: don't overflow the length
                            fmtLine = fullLine[matchStartLocation..];
                        }
                        else
                        {
                            // past the end of the line: line may be truncated, or it may be the newline chars
                        }

                        if (regLine != null)
                        {
                            Run regularRun = new(regLine);
                            paragraph.Inlines.Add(regularRun);
                        }
                        if (fmtLine != null)
                        {
                            if (HighlightCaptureGroups && m.Groups.Count > 0)
                            {
                                FormatCaptureGroups(paragraph, m, fmtLine);
                            }
                            else
                            {
                                Run run = new(fmtLine);
                                run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                                run.SetResourceReference(Run.BackgroundProperty, "Match.Highlight.Background");
                                paragraph.Inlines.Add(run);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // on error show the whole line with no highlights
                        paragraph.Inlines.Clear();
                        Run regularRun = new(fullLine);
                        paragraph.Inlines.Add(regularRun);
                        // set position to end of line
                        matchStartLocation = fullLine.Length;
                        matchLength = 0;
                    }
                    finally
                    {
                        counter = matchStartLocation + matchLength;
                    }
                }

                if (counter < fullLine.Length)
                {
                    try
                    {
                        string regLine = fullLine[counter..];
                        Run regularRun = new(regLine);
                        paragraph.Inlines.Add(regularRun);
                    }
                    catch
                    {
                        Run regularRun = new(fullLine);
                        paragraph.Inlines.Add(regularRun);
                    }
                }

                if (line.LineText.Length > MaxLineLength)
                {
                    string msg = TranslationSource.Format(Resources.Main_ResultList_CountAdditionalCharacters, line.LineText.Length - MaxLineLength);

                    var msgRun = new Run(msg);
                    msgRun.SetResourceReference(Run.ForegroundProperty, "TreeView.Message.Highlight.Foreground");
                    msgRun.SetResourceReference(Run.BackgroundProperty, "TreeView.Message.Highlight.Background");
                    paragraph.Inlines.Add(msgRun);

                    var hiddenMatches = line.Matches.Where(m => m.StartLocation > MaxLineLength).Select(m => m);
                    int count = hiddenMatches.Count();
                    if (count > 0)
                    {
                        paragraph.Inlines.Add(new Run(" " + Resources.Main_ResultList_AdditionalMatches));
                    }

                    // if close to getting them all, then take them all,
                    // otherwise, stop at 20 and just show the remaining count
                    int takeCount = count > 25 ? 20 : count;

                    foreach (GrepMatch m in hiddenMatches.Take(takeCount))
                    {
                        if (m.StartLocation + m.Length <= line.LineText.Length)
                        {
                            paragraph.Inlines.Add(new Run(enQuad));
                            string fmtLine = line.LineText.Substring(m.StartLocation, m.Length);
                            var run = new Run(fmtLine);
                            run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                            run.SetResourceReference(Run.BackgroundProperty, "Match.Highlight.Background");
                            paragraph.Inlines.Add(run);

                            if (m.StartLocation + m.Length == line.LineText.Length)
                                paragraph.Inlines.Add(new Run(" " + Resources.Main_ResultList_AtEndOfLine));
                            else
                                paragraph.Inlines.Add(new Run(" " + TranslationSource.Format(Resources.Main_ResultList_AtPosition, m.StartLocation)));
                        }
                    }

                    if (count > takeCount)
                    {
                        paragraph.Inlines.Add(new Run(TranslationSource.Format(Resources.Main_ResultList_PlusCountMoreMatches, count - takeCount)));
                    }
                }
            }
            return paragraph.Inlines;
        }

        private void FormatCaptureGroups(Paragraph paragraph, GrepMatch match, string fmtLine)
        {
            if (paragraph == null || match == null || string.IsNullOrEmpty(fmtLine))
                return;

            GroupMap map = new(match, fmtLine);
            foreach (var range in map.Ranges.Where(r => r.Length > 0))
            {
                var run = new Run(range.RangeText);
                if (range.Group == null)
                {
                    run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                    run.SetResourceReference(Run.BackgroundProperty, "Match.Highlight.Background");
                    run.ToolTip = TranslationSource.Format(Resources.Main_ResultList_MatchToolTip1, Parent.MatchIdx, Environment.NewLine, fmtLine);
                    paragraph.Inlines.Add(run);
                }
                else
                {
                    if (!Parent.GroupColors.TryGetValue(range.Group.Name, out string? bgColor))
                    {
                        int groupIdx = Parent.GroupColors.Count % 10;
                        bgColor = $"Match.Group.{groupIdx}.Highlight.Background";
                        Parent.GroupColors.Add(range.Group.Name, bgColor);
                    }
                    run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                    run.SetResourceReference(Run.BackgroundProperty, bgColor);
                    run.ToolTip = TranslationSource.Format(Resources.Main_ResultList_MatchToolTip2,
                        Parent.MatchIdx, Environment.NewLine, range.Group.Name, range.Group.Value);
                    paragraph.Inlines.Add(run);
                }
            }
        }

        private string FormatHexValues(GrepLine grepLine)
        {
            string[] parts = grepLine.LineText.TrimEnd().Split(' ');
            List<byte> list = new();
            foreach (string num in parts)
            {
                if (byte.TryParse(num, System.Globalization.NumberStyles.HexNumber, null, out byte result))
                {
                    list.Add(result);
                }
            }
            string text = Parent.GrepResult.Encoding.GetString(list.ToArray());
            List<char> nonPrintableChars = new();
            for (int idx = 0; idx < text.Length; idx++)
            {
                if (!char.IsLetterOrDigit(text[idx]) && !char.IsPunctuation(text[idx]) && text[idx] != ' ')
                {
                    nonPrintableChars.Add(text[idx]);
                }
            }
            foreach (char c in nonPrintableChars)
            {
                text = text.Replace(c, '.');
            }
            return text;
        }

        private class GroupMap
        {
            private readonly int start;
            private readonly List<Range> ranges = new();
            public GroupMap(GrepMatch match, string text)
            {
                start = match.StartLocation;
                MatchText = text;
                ranges.Add(new Range(0, MatchText.Length, this, null));

                foreach (var group in match.Groups.OrderByDescending(g => g.Length))
                {
                    Insert(group);
                }
                ranges.Sort();
            }

            public IEnumerable<Range> Ranges => ranges;

            public string MatchText { get; }

            private void Insert(GrepCaptureGroup group)
            {
                int startIndex = group.StartLocation - start;
                int endIndex = startIndex + group.Length;

                //gggggg
                //xxxxxx
                var replace = ranges.FirstOrDefault(r => r.Start == startIndex && r.End == endIndex);
                if (replace != null)
                {
                    ranges.Remove(replace);
                    ranges.Add(new Range(startIndex, endIndex, this, group));
                }
                else
                {
                    //gg
                    //xxxxxx
                    var head = ranges.FirstOrDefault(r => r.Start == startIndex && r.End > endIndex);
                    if (head != null)
                    {
                        ranges.Remove(head);
                        ranges.Add(new Range(startIndex, endIndex, this, group));
                        ranges.Add(new Range(endIndex, head.End, this, head.Group));
                    }
                    else
                    {
                        //    gg
                        //xxxxxx
                        var tail = ranges.FirstOrDefault(r => r.Start < startIndex && r.End == endIndex);
                        if (tail != null)
                        {
                            ranges.Remove(tail);
                            ranges.Add(new Range(tail.Start, startIndex, this, tail.Group));
                            ranges.Add(new Range(startIndex, endIndex, this, group));
                        }
                        else
                        {
                            //  gg
                            //xxxxxx
                            var split = ranges.FirstOrDefault(r => r.Start < startIndex && r.End > endIndex);
                            if (split != null)
                            {
                                ranges.Remove(split);
                                ranges.Add(new Range(split.Start, startIndex, this, split.Group));
                                ranges.Add(new Range(startIndex, endIndex, this, group));
                                ranges.Add(new Range(endIndex, split.End, this, split.Group));
                            }
                            else
                            {
                                //   gggg  
                                //xxxxxyyyyy
                                var spans = ranges.Where(r => (r.Start < startIndex && r.End < endIndex) ||
                                    (r.Start > startIndex && r.End > endIndex)).OrderBy(r => r.Start).ToList();

                                if (spans.Count == 2)
                                {
                                    ranges.Remove(spans[0]);
                                    ranges.Remove(spans[1]);
                                    ranges.Add(new Range(spans[0].Start, startIndex, this, spans[0].Group));
                                    ranges.Add(new Range(startIndex, endIndex, this, group));
                                    ranges.Add(new Range(endIndex, spans[1].End, this, spans[1].Group));
                                }
                            }
                        }
                    }
                }
            }
        }

        private class Range : IComparable<Range>, IComparable, IEquatable<Range>
        {
            private readonly GroupMap parentMap;
            public Range(int start, int end, GroupMap parent, GrepCaptureGroup? group)
            {
                Start = Math.Min(start, end);
                End = Math.Max(start, end);
                parentMap = parent;
                Group = group;
            }

            public int Start { get; }
            public int End { get; }

            public int Length { get { return End - Start; } }

            public string RangeText { get { return parentMap.MatchText.Substring(Start, Length); } }

            public GrepCaptureGroup? Group { get; }

            public int CompareTo(object? obj)
            {
                return CompareTo(obj as Range);
            }

            public int CompareTo(Range? other)
            {
                if (other == null)
                    return 1;
                else
                    return Start.CompareTo(other.Start); // should never be equal
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as Range);
            }

            public bool Equals(Range? other)
            {
                if (other == null) return false;

                return Start == other.Start &&
                    End == other.End;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Start, End);
            }

            public override string ToString()
            {
                return $"{Start} - {End}:  {RangeText}";
            }
        }
    }

    public partial class FormattedGrepMatch : CultureAwareViewModel
    {
        public FormattedGrepMatch(GrepMatch match)
        {
            Match = match;
            ReplaceMatch = Match.ReplaceMatch;

            Background = Match.ReplaceMatch ? Brushes.PaleGreen : Brushes.Bisque;
        }

        public GrepMatch Match { get; }

        public override string ToString()
        {
            return Match.ToString() + $" replace={ReplaceMatch}";
        }

        [ObservableProperty]
        private bool replaceMatch = false;

        partial void OnReplaceMatchChanging(bool value)
        {
            Match.ReplaceMatch = value;
            Background = Match.ReplaceMatch ? Brushes.PaleGreen : Brushes.Bisque;
        }

        [ObservableProperty]
        private Brush background = Brushes.Bisque;

        [ObservableProperty]
        private double fontSize = 12;

        [ObservableProperty]
        private FontWeight fontWeight = FontWeights.Normal;
    }
}
