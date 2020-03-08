using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.WPF.MVHelpers;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.WPF
{
    public interface IGrepResult
    {
    }

    public class ObservableGrepSearchResults : ObservableCollection<FormattedGrepResult>, INotifyPropertyChanged
    {
        public ObservableGrepSearchResults()
        {
            SelectedNodes = new ObservableCollection<IGrepResult>();
            SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;
            this.CollectionChanged += ObservableGrepSearchResults_CollectionChanged;
        }

        private readonly Dictionary<string, BitmapSource> icons = new Dictionary<string, BitmapSource>();

        /// <summary>
        /// Gets the collection of Selected tree nodes, in the order they were selected
        /// </summary>
        public ObservableCollection<IGrepResult> SelectedNodes { get; private set; }

        /// <summary>
        /// Gets a read-only collection of the selected items, in display order
        /// </summary>
        public ReadOnlyCollection<IGrepResult> SelectedItems
        {
            get
            {
                var list = SelectedNodes.ToList();
                // sort the selected items in the order of the items appear in the tree!!!
                if (list.Count > 1)
                    list.Sort(new SelectionComparer(this));
                return list.AsReadOnly();
            }
        }

        private class SelectionComparer : IComparer<IGrepResult>
        {
            private ObservableGrepSearchResults orderBy;
            public SelectionComparer(ObservableGrepSearchResults orderBy)
            {
                this.orderBy = orderBy;
            }

            public int Compare(IGrepResult x, IGrepResult y)
            {
                var fileX = x as FormattedGrepResult;
                var lineX = x as FormattedGrepLine;
                if (fileX == null && lineX != null)
                    fileX = lineX.Parent;

                var fileY = y as FormattedGrepResult;
                var lineY = y as FormattedGrepLine;
                if (fileY == null && lineY != null)
                    fileY = lineY.Parent;


                int posX = 0, posY = 0;
                if (fileX != null && fileY != null)
                {
                    if (fileX == fileY && lineX != null && lineY != null)
                    {
                        posX = fileX.FormattedLines.IndexOf(lineX);
                        posY = fileX.FormattedLines.IndexOf(lineY);
                        return posX.CompareTo(posY);
                    }

                    posX = orderBy.IndexOf(fileX);
                    posY = orderBy.IndexOf(fileY);
                    return posX.CompareTo(posY);
                }
                return 0;
            }
        }

        void ObservableGrepSearchResults_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<IGrepResult> toRemove = new List<IGrepResult>();
            foreach (var node in SelectedNodes)
            {
                if (node is FormattedGrepResult item && !this.Contains(item))
                    toRemove.Add(item);

                if (node is FormattedGrepLine line && !this.Contains(line.Parent))
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
                        System.Drawing.Bitmap bitmapIcon = IconHandler.IconFromExtensionShell(extension, IconSize.Small);
                        if (bitmapIcon == null)
                            bitmapIcon = dnGREP.Common.Properties.Resources.na_icon;
                        icons[extension] = GetBitmapSource(bitmapIcon);
                    }
                    newEntry.Icon = icons[extension];
                }
            }
        }

        public ObservableGrepSearchResults(List<GrepSearchResult> list)
            : this()
        {
            AddRange(list);
        }

        public List<GrepSearchResult> GetList()
        {
            List<GrepSearchResult> tempList = new List<GrepSearchResult>();
            foreach (var l in this) tempList.Add(l.GrepResult);
            return tempList;
        }

        public List<GrepSearchResult> GetWritableList()
        {
            List<GrepSearchResult> tempList = new List<GrepSearchResult>();
            foreach (var item in this)
            {
                if (!Utils.IsReadOnly(item.GrepResult))
                    tempList.Add(item.GrepResult);
            }
            return tempList;
        }

        public void AddRange(List<GrepSearchResult> list)
        {
            foreach (var l in list)
            {
                var fmtResult = new FormattedGrepResult(l, folderPath);
                Add(fmtResult);

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
                Add(new FormattedGrepResult(l, folderPath));
            }
        }

        public void AddRange(IEnumerable<FormattedGrepResult> items)
        {
            foreach (var item in items)
                Add(item);
        }

        private string folderPath = "";
        public string FolderPath
        {
            get { return folderPath; }
            set { folderPath = value; }
        }

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);
        public static BitmapSource GetBitmapSource(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            try
            {
                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                return bs;
            }
            finally
            {
                DeleteObject(ip);
            }
        }

        // some settings have changed, raise property changed events to update the UI
        public void RaiseSettingsPropertiesChanged()
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs("CustomEditorConfigured"));
            base.OnPropertyChanged(new PropertyChangedEventArgs("CompareApplicationConfigured"));

            foreach (var item in this)
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

        private double resultsScale = 1.0;
        public double ResultsScale
        {
            get { return resultsScale; }
            set
            {
                if (value == resultsScale)
                    return;

                resultsScale = value;

                base.OnPropertyChanged(new PropertyChangedEventArgs("ResultsScale"));
            }
        }

        private double resultsMenuScale = 1.0;
        public double ResultsMenuScale
        {
            get { return resultsMenuScale; }
            set
            {
                if (value == resultsMenuScale)
                    return;

                resultsMenuScale = value;

                base.OnPropertyChanged(new PropertyChangedEventArgs("ResultsMenuScale"));
            }
        }

        public bool HasSelection
        {
            get { return SelectedNodes.Any(); }
        }

        public bool HasMultipleSelection
        {
            get { return SelectedNodes.Count() > 1; }
        }

        public bool HasGrepResultSelection
        {
            get { return SelectedNodes.Where(r => (r as FormattedGrepResult) != null).Any(); }
        }

        public bool HasGrepLineSelection
        {
            get { return SelectedNodes.Where(r => (r as FormattedGrepLine) != null).Any(); }
        }

        void SelectedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs("HasSelection"));
            base.OnPropertyChanged(new PropertyChangedEventArgs("HasMultipleSelection"));
            base.OnPropertyChanged(new PropertyChangedEventArgs("HasGrepResultSelection"));
            base.OnPropertyChanged(new PropertyChangedEventArgs("HasGrepLineSelection"));
        }

        RelayCommand _compareFilesCommand;
        public ICommand CompareFilesCommand
        {
            get
            {
                if (_compareFilesCommand == null)
                {
                    _compareFilesCommand = new RelayCommand(
                        param => CompareFiles(),
                        param => CanCompareFiles
                        );
                }
                return _compareFilesCommand;
            }
        }

        private IList<GrepSearchResult> GetSelectedFiles()
        {
            List<GrepSearchResult> files = new List<GrepSearchResult>();
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
                int count = GetSelectedFiles().Count;
                return count == 2 || count == 3;
            }
        }

        private void CompareFiles()
        {
            var files = GetSelectedFiles();
            if (files.Count == 2 || files.Count == 3)
                Utils.CompareFiles(files);
        }

        private bool isResultsTreeFocused;
        public bool IsResultsTreeFocused
        {
            get { return isResultsTreeFocused; }
            set
            {
                if (value == isResultsTreeFocused)
                    return;

                isResultsTreeFocused = value;

                base.OnPropertyChanged(new PropertyChangedEventArgs("IsResultsTreeFocused"));
            }
        }

        public event EventHandler<GrepLineEventArgs> OpenFileLineRequest;
        public event EventHandler<GrepResultEventArgs> OpenFileRequest;
        public event EventHandler<GrepLineEventArgs> PreviewFileLineRequest;
        public event EventHandler<GrepResultEventArgs> PreviewFileRequest;

        public void OpenFile(FormattedGrepLine line, bool useCustomEditor)
        {
            OpenFileLineRequest(this, new GrepLineEventArgs { FormattedGrepLine = line, UseCustomEditor = useCustomEditor });
        }

        public void OpenFile(FormattedGrepResult line, bool useCustomEditor)
        {
            OpenFileRequest(this, new GrepResultEventArgs { FormattedGrepResult = line, UseCustomEditor = useCustomEditor });
        }

        public void PreviewFile(FormattedGrepLine line, System.Drawing.RectangleF windowSize)
        {
            PreviewFileLineRequest(this, new GrepLineEventArgs { FormattedGrepLine = line, ParentWindowSize = windowSize });
        }

        public void PreviewFile(FormattedGrepResult line, System.Drawing.RectangleF windowSize)
        {
            PreviewFileRequest(this, new GrepResultEventArgs { FormattedGrepResult = line, ParentWindowSize = windowSize });
        }
    }

    public class FormattedGrepResult : ViewModelBase, IGrepResult
    {
        public GrepSearchResult GrepResult { get; private set; } = new GrepSearchResult();

        public int Matches
        {
            get { return GrepResult.Matches.Count; }
        }

        public string Style { get; private set; } = "";

        public string Label { get; private set; } = "";

        internal int MatchIdx { get; set; }
        internal Dictionary<string, string> GroupColors { get; } = new Dictionary<string, string>();

        public bool ShowFileInfoTooltips
        {
            get { return GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFileInfoTooltips); }
        }

        // some settings have changed, raise property changed events to update the UI
        public void RaiseSettingsPropertiesChanged()
        {
            base.OnPropertyChanged(() => ShowFileInfoTooltips);
        }

        private bool isExpanded = false;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                if (value == true && !FormattedLines.IsLoaded && !FormattedLines.IsLoading)
                {
                    IsLoading = true;
                    FormattedLines.Load(true);
                }
                OnPropertyChanged("IsExpanded");
            }
        }

        private bool isLoading;
        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                isLoading = value;
                OnPropertyChanged("IsLoading");
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (value == isSelected)
                    return;

                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        private int lineNumberColumnWidth = 30;
        public int LineNumberColumnWidth
        {
            get { return lineNumberColumnWidth; }
            set { lineNumberColumnWidth = value; OnPropertyChanged("LineNumberColumnWidth"); }
        }

        public BitmapSource Icon { get; set; }

        public LazyResultsList FormattedLines { get; private set; }

        public FormattedGrepResult(GrepSearchResult result, string folderPath)
        {
            GrepResult = result;

            bool isFileReadOnly = Utils.IsReadOnly(GrepResult);
            bool isSuccess = GrepResult.IsSuccess;

            string basePath = string.IsNullOrWhiteSpace(folderPath) ? string.Empty : folderPath.TrimEnd('\\');
            string displayedName = Path.GetFileName(GrepResult.FileNameDisplayed);

            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFilePathInResults) &&
                GrepResult.FileNameDisplayed.Contains(basePath, StringComparison.CurrentCultureIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(basePath))
                    displayedName = GrepResult.FileNameDisplayed.Substring(basePath.Length + 1).TrimStart('\\');
                else
                    displayedName = GrepResult.FileNameDisplayed;
            }
            if (!string.IsNullOrWhiteSpace(GrepResult.AdditionalInformation))
            {
                displayedName += " " + GrepResult.AdditionalInformation + " ";
            }
            int matchCount = (GrepResult.Matches == null ? 0 : GrepResult.Matches.Count);
            if (matchCount > 0)
            {
                if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount))
                {
                    var lineCount = GrepResult.Matches.Where(r => r.LineNumber > 0)
                       .Select(r => r.LineNumber).Distinct().Count();
                    displayedName = string.Format("{0} ({1} matches on {2} lines)", displayedName, matchCount, lineCount);
                }
                else
                {
                    displayedName = string.Format("{0} ({1})", displayedName, matchCount);
                }
            }
            if (isFileReadOnly)
            {
                result.ReadOnly = true;
                displayedName = displayedName + " [read-only]";
            }

            Label = displayedName;

            if (isFileReadOnly)
            {
                Style = "ReadOnly";
            }
            if (!isSuccess)
            {
                Style = "Error";
            }

            FormattedLines = new LazyResultsList(result, this);
            FormattedLines.LineNumberColumnWidthChanged += FormattedLines_PropertyChanged;
            FormattedLines.LoadFinished += FormattedLines_LoadFinished;
        }

        void FormattedLines_LoadFinished(object sender, EventArgs e)
        {
            IsLoading = false;
        }

        void FormattedLines_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LineNumberColumnWidth")
                LineNumberColumnWidth = FormattedLines.LineNumberColumnWidth;
        }

        public ObservableCollection<FormattedGrepMatch> FormattedMatches { get; } = new ObservableCollection<FormattedGrepMatch>();
    }

    public class FormattedGrepLine : ViewModelBase, IGrepResult
    {
        public FormattedGrepLine(GrepLine line, FormattedGrepResult parent, int initialColumnWidth, bool breakSection)
        {
            Parent = parent;
            GrepLine = line;
            Parent.PropertyChanged += new PropertyChangedEventHandler(Parent_PropertyChanged);
            LineNumberColumnWidth = initialColumnWidth;
            IsSectionBreak = breakSection;

            FormattedLineNumber = (line.LineNumber == -1 ? "" : line.LineNumber.ToString());

            //string fullText = lineSummary;
            if (line.IsContext)
            {
                Style = "Context";
            }
            if (line.LineNumber == -1 && line.LineText == "")
            {
                Style = "Empty";
            }
        }

        public GrepLine GrepLine { get; private set; }
        public string FormattedLineNumber { get; private set; }

        private InlineCollection formattedText;
        public InlineCollection FormattedText
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
            }
        }

        // FormattedGrepLines don't expand, but the XAML code expects this property on TreeViewItems
        public bool IsExpanded { get; set; }

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (value == isSelected)
                    return;

                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        private bool isSectionBreak = false;
        public bool IsSectionBreak
        {
            get { return isSectionBreak; }
            set
            {
                if (isSectionBreak == value)
                {
                    return;
                }

                isSectionBreak = value;
                OnPropertyChanged("IsSectionBreak");
            }
        }

        public string Style { get; private set; } = "";

        private int lineNumberColumnWidth = 30;
        public int LineNumberColumnWidth
        {
            get { return lineNumberColumnWidth; }
            set { lineNumberColumnWidth = value; OnPropertyChanged("LineNumberColumnWidth"); }
        }

        void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LineNumberColumnWidth))
                LineNumberColumnWidth = Parent.LineNumberColumnWidth;
        }

        public bool HighlightCaptureGroups
        {
            get { return GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightCaptureGroups); }
        }

        public FormattedGrepResult Parent { get; private set; }

        private InlineCollection FormatLine(GrepLine line)
        {
            Paragraph paragraph = new Paragraph();

            const int MAX_LINE_LENGTH = 500;

            string fullLine = line.LineText;
            if (line.LineText.Length > MAX_LINE_LENGTH)
                fullLine = line.LineText.Substring(0, MAX_LINE_LENGTH);

            if (line.Matches.Count == 0)
            {
                Run mainRun = new Run(fullLine);
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
                    try
                    {
                        string regLine = null;
                        string fmtLine = null;
                        if (m.StartLocation < fullLine.Length)
                        {
                            regLine = fullLine.Substring(counter, m.StartLocation - counter);
                        }

                        if (m.StartLocation + m.Length <= fullLine.Length)
                        {
                            fmtLine = fullLine.Substring(m.StartLocation, m.Length);
                        }
                        else
                        {
                            // match may include the non-printing newline chars at the end of the line: don't overflow the length
                            fmtLine = fullLine.Substring(m.StartLocation, fullLine.Length - m.StartLocation);
                        }

                        if (regLine != null)
                        {
                            Run regularRun = new Run(regLine);
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
                                var run = new Run(fmtLine);
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
                        Run regularRun = new Run(fullLine);
                        paragraph.Inlines.Add(regularRun);
                    }
                    finally
                    {
                        counter = m.StartLocation + m.Length;
                    }
                }
                if (counter < fullLine.Length)
                {
                    try
                    {
                        string regLine = fullLine.Substring(counter);
                        Run regularRun = new Run(regLine);
                        paragraph.Inlines.Add(regularRun);
                    }
                    catch
                    {
                        Run regularRun = new Run(fullLine);
                        paragraph.Inlines.Add(regularRun);
                    }
                }
                if (line.LineText.Length > MAX_LINE_LENGTH)
                {
                    string msg = string.Format("...(+{0:n0} characters)", line.LineText.Length - MAX_LINE_LENGTH);

                    var msgRun = new Run(msg);
                    msgRun.SetResourceReference(Run.ForegroundProperty, "TreeView.Message.Highlight.Foreground");
                    msgRun.SetResourceReference(Run.BackgroundProperty, "TreeView.Message.Highlight.Background");
                    paragraph.Inlines.Add(msgRun);

                    var hiddenMatches = line.Matches.Where(m => m.StartLocation > MAX_LINE_LENGTH).Select(m => m);
                    int count = hiddenMatches.Count();
                    if (count > 0)
                        paragraph.Inlines.Add(new Run(" additional matches:"));

                    // if close to getting them all, then take them all,
                    // otherwise, stop at 20 and just show the remaining count
                    int takeCount = count > 25 ? 20 : count;

                    foreach (GrepMatch m in hiddenMatches.Take(takeCount))
                    {
                        if (m.StartLocation + m.Length <= line.LineText.Length)
                        {
                            paragraph.Inlines.Add(new Run("  "));
                            string fmtLine = line.LineText.Substring(m.StartLocation, m.Length);
                            var run = new Run(fmtLine);
                            run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                            run.SetResourceReference(Run.BackgroundProperty, "Match.Highlight.Background");
                            paragraph.Inlines.Add(run);

                            if (m.StartLocation + m.Length == line.LineText.Length)
                                paragraph.Inlines.Add(new Run(" (at end of line)"));
                            else
                                paragraph.Inlines.Add(new Run($" at position {m.StartLocation}"));
                        }
                    }

                    if (count > takeCount)
                    {
                        paragraph.Inlines.Add(new Run($", +{count - takeCount} more matches"));
                    }
                }
            }
            return paragraph.Inlines;
        }

        private void FormatCaptureGroups(Paragraph paragraph, GrepMatch match, string fmtLine)
        {
            if (paragraph == null || match == null || string.IsNullOrEmpty(fmtLine))
                return;

            GroupMap map = new GroupMap(match, fmtLine);
            foreach (var range in map.Ranges.Where(r => r.Length > 0))
            {
                var run = new Run(range.RangeText);
                if (range.Group == null)
                {
                    run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                    run.SetResourceReference(Run.BackgroundProperty, "Match.Highlight.Background");
                    run.ToolTip = $"Match {Parent.MatchIdx}{Environment.NewLine}{fmtLine}";
                    paragraph.Inlines.Add(run);
                }
                else
                {
                    if (!Parent.GroupColors.TryGetValue(range.Group.Name, out string bgColor))
                    {
                        int groupIdx = Parent.GroupColors.Count % 10;
                        bgColor = $"Match.Group.{groupIdx}.Highlight.Background";
                        Parent.GroupColors.Add(range.Group.Name, bgColor);
                    }
                    run.SetResourceReference(Run.ForegroundProperty, "Match.Highlight.Foreground");
                    run.SetResourceReference(Run.BackgroundProperty, bgColor);
                    run.ToolTip = $"Match {Parent.MatchIdx}{Environment.NewLine}Group {range.Group.Name}:   {range.Group.Value}";
                    paragraph.Inlines.Add(run);
                }
            }
        }

        private class GroupMap
        {
            private readonly int start;
            private readonly List<Range> ranges = new List<Range>();
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
            public Range(int start, int end, GroupMap parent, GrepCaptureGroup group)
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

            public GrepCaptureGroup Group { get; }

            public int CompareTo(object obj)
            {
                return CompareTo(obj as Range);
            }

            public int CompareTo(Range other)
            {
                if (other == null)
                    return 1;
                else
                    return Start.CompareTo(other.Start); // should never be equal
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Range);
            }

            public bool Equals(Range other)
            {
                if (other == null) return false;

                return Start == other.Start &&
                    End == other.End;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 13;
                    hashCode = (hashCode * 397) ^ Start;
                    hashCode = (hashCode * 397) ^ End;
                    return hashCode;
                }
            }

            public override string ToString()
            {
                return $"{Start} - {End}:  {RangeText}";
            }
        }
    }

    public class FormattedGrepMatch : ViewModelBase
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
            return Match.ToString() + $" selected={isSelected}";
        }


        private bool replaceMatch = false;
        public bool ReplaceMatch
        {
            get { return replaceMatch; }
            set
            {
                if (replaceMatch == value)
                    return;

                replaceMatch = value;
                OnPropertyChanged(() => ReplaceMatch);

                Match.ReplaceMatch = value;

                Background = Match.ReplaceMatch ? Brushes.PaleGreen : Brushes.Bisque;
            }
        }

        private bool isSelected = false;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected == value)
                    return;

                isSelected = value;
                OnPropertyChanged(() => IsSelected);

                FontWeight = isSelected ? FontWeights.Black : FontWeights.Normal;
                FontSize = isSelected ? 18 : 12;
            }
        }

        private Brush background = Brushes.Bisque;
        public Brush Background
        {
            get { return background; }
            set
            {
                if (background == value)
                    return;

                background = value;
                OnPropertyChanged(() => Background);
            }
        }

        private double fontSize = 12;
        public double FontSize
        {
            get { return fontSize; }
            set
            {
                if (fontSize == value)
                    return;

                fontSize = value;
                OnPropertyChanged(() => FontSize);
            }
        }

        private FontWeight fontWeight = FontWeights.Normal;
        public FontWeight FontWeight
        {
            get { return fontWeight; }
            set
            {
                if (fontWeight == value)
                    return;

                fontWeight = value;
                OnPropertyChanged(() => FontWeight);
            }
        }
    }
}
