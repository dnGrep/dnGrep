using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.WPF.MVHelpers;

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

        private Dictionary<string, BitmapSource> icons = new Dictionary<string, BitmapSource>();

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
                FormattedGrepResult item = node as FormattedGrepResult;
                FormattedGrepLine line = node as FormattedGrepLine;

                if (item != null && !this.Contains(item))
                    toRemove.Add(item);

                if (line != null && !this.Contains(line.Parent))
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

        public void AddRange(List<GrepSearchResult> list)
        {
            foreach (var l in list)
                Add(new FormattedGrepResult(l, folderPath));
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

        public bool CustomEditorConfigured
        {
            get { return GrepSettings.Instance.IsSet(GrepSettings.Key.CustomEditor); }
            set
            {
                base.OnPropertyChanged(new PropertyChangedEventArgs("CustomEditorConfigured"));
            }
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
        private GrepSearchResult grepResult = new GrepSearchResult();
        public GrepSearchResult GrepResult
        {
            get { return grepResult; }
        }

        public int Matches
        {
            get { return GrepResult.Matches.Count; }
        }

        private FileInfo fileInfo;
        public string Size
        {
            get
            {
                return string.Format("{0}", fileInfo.Length);
            }
        }

        public string FileName
        {
            get { return fileInfo.Name; }
        }

        public string FilePath
        {
            get { return fileInfo.FullName; }
        }

        private string style = "";
        public string Style
        {
            get { return style; }
            set { style = value; }
        }

        private string label = "";
        public string Label
        {
            get
            {
                return label;
            }
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

        private BitmapSource icon;

        public BitmapSource Icon
        {
            get { return icon; }
            set { icon = value; }
        }

        private LazyResultsList formattedLines;
        public LazyResultsList FormattedLines
        {
            get { return formattedLines; }
        }

        public FormattedGrepResult(GrepSearchResult result, string folderPath)
        {
            grepResult = result;
            fileInfo = new FileInfo(grepResult.FileNameReal);

            bool isFileReadOnly = Utils.IsReadOnly(grepResult);
            bool isSuccess = grepResult.IsSuccess;

            string basePath = string.IsNullOrWhiteSpace(folderPath) ? string.Empty :
                Utils.GetBaseFolder(folderPath).TrimEnd('\\');
            string displayedName = Path.GetFileName(grepResult.FileNameDisplayed);

            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFilePathInResults) &&
                grepResult.FileNameDisplayed.Contains(basePath))
            {
                if (!string.IsNullOrWhiteSpace(basePath))
                    displayedName = grepResult.FileNameDisplayed.Substring(basePath.Length + 1).TrimStart('\\');
                else
                    displayedName = grepResult.FileNameDisplayed;
            }
            if (!string.IsNullOrWhiteSpace(grepResult.AdditionalInformation))
            {
                displayedName += " " + grepResult.AdditionalInformation + " ";
            }
            int matchCount = (grepResult.Matches == null ? 0 : grepResult.Matches.Count);
            if (matchCount > 0)
            {
                if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount))
                {
                    var lineCount = grepResult.Matches.Where(r => r.LineNumber > 0)
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

            label = displayedName;

            if (isFileReadOnly)
            {
                style = "ReadOnly";
            }
            if (!isSuccess)
            {
                style = "Error";
            }

            formattedLines = new LazyResultsList(result, this);
            formattedLines.LineNumberColumnWidthChanged += formattedLines_PropertyChanged;
            formattedLines.LoadFinished += formattedLines_LoadFinished;

            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ExpandResults))
            {
                IsExpanded = true;
            }
        }

        void formattedLines_LoadFinished(object sender, EventArgs e)
        {
            IsLoading = false;
        }

        void formattedLines_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LineNumberColumnWidth")
                LineNumberColumnWidth = formattedLines.LineNumberColumnWidth;
        }
    }

    public class FormattedGrepLine : ViewModelBase, IGrepResult
    {
        private GrepSearchResult.GrepLine grepLine;
        public GrepSearchResult.GrepLine GrepLine
        {
            get { return grepLine; }
        }

        private string formattedLineNumber;
        public string FormattedLineNumber
        {
            get { return formattedLineNumber; }
        }

        private InlineCollection formattedText;
        public InlineCollection FormattedText
        {
            get
            {
                if (formattedText == null || formattedText.Count == 0)
                    formattedText = formatLine(GrepLine);
                return formattedText;
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

        private string style = "";
        public string Style
        {
            get { return style; }
            set { style = value; }
        }

        private int lineNumberColumnWidth = 30;
        public int LineNumberColumnWidth
        {
            get { return lineNumberColumnWidth; }
            set { lineNumberColumnWidth = value; OnPropertyChanged("LineNumberColumnWidth"); }
        }

        void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LineNumberColumnWidth")
                LineNumberColumnWidth = Parent.LineNumberColumnWidth;
        }

        private FormattedGrepResult parent;
        public FormattedGrepResult Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public FormattedGrepLine(GrepSearchResult.GrepLine line, FormattedGrepResult parent, int initialColumnWidth, bool breakSection)
        {
            Parent = parent;
            grepLine = line;
            Parent.PropertyChanged += new PropertyChangedEventHandler(Parent_PropertyChanged);
            LineNumberColumnWidth = initialColumnWidth;
            IsSectionBreak = breakSection;

            formattedLineNumber = (line.LineNumber == -1 ? "" : line.LineNumber.ToString());

            //string fullText = lineSummary;
            if (line.IsContext)
            {
                style = "Context";
            }
            if (line.LineNumber == -1 && line.LineText == "")
            {
                style = "Empty";
            }
        }

        private InlineCollection formatLine(GrepSearchResult.GrepLine line)
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
                GrepSearchResult.GrepMatch[] lineMatches = new GrepSearchResult.GrepMatch[line.Matches.Count];
                line.Matches.CopyTo(lineMatches);
                foreach (GrepSearchResult.GrepMatch m in lineMatches)
                {
                    try
                    {
                        string regLine = null;
                        string fmtLine = null;
                        if (fullLine.Length < m.StartLocation + m.Length)
                        {
                            regLine = fullLine.Substring(counter, fullLine.Length - counter);
                        }
                        else
                        {
                            regLine = fullLine.Substring(counter, m.StartLocation - counter);
                            fmtLine = fullLine.Substring(m.StartLocation, m.Length);
                        }

                        Run regularRun = new Run(regLine);
                        paragraph.Inlines.Add(regularRun);

                        if (fmtLine != null)
                        {
                            Run highlightedRun = new Run(fmtLine);
                            highlightedRun.Background = Brushes.Yellow;
                            paragraph.Inlines.Add(highlightedRun);
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
                    Run run = new Run(msg);
                    run.Background = Brushes.AliceBlue;
                    paragraph.Inlines.Add(run);

                    var hiddenMatches = line.Matches.Where(m => m.StartLocation > MAX_LINE_LENGTH).Select(m => m);
                    int count = hiddenMatches.Count();
                    if (count > 0)
                        paragraph.Inlines.Add(new Run(" additional matches:"));

                    // if close to getting them all, then take them all,
                    // otherwise, stop at 20 and just show the remaining count
                    int takeCount = count > 25 ? 20 : count;

                    foreach (GrepSearchResult.GrepMatch m in hiddenMatches.Take(takeCount))
                    {
                        paragraph.Inlines.Add(new Run("  "));
                        string fmtLine = line.LineText.Substring(m.StartLocation, m.Length);
                        run = new Run(fmtLine);
                        run.Background = Brushes.Yellow;
                        paragraph.Inlines.Add(run);

                        paragraph.Inlines.Add(new Run(string.Format(" at position {0}", m.StartLocation)));
                    }

                    if (count > takeCount)
                    {
                        paragraph.Inlines.Add(new Run(string.Format(", +{0} more matches", count - takeCount)));
                    }
                }
            }
            return paragraph.Inlines;
        }
    }
}
