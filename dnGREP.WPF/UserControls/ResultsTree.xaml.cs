using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using dnGREP.Common;
using Res = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for ResultsTree.xaml
    /// </summary>
    public partial class ResultsTree : UserControl, INameScope
    {
        private GrepSearchResultsViewModel? viewModel;
        private bool skipScrollOnExpand;
        private bool inNextPrevious;
        private bool stickyScrollEnabled;

        public GridViewColumnCollection TreeListViewColumns { get; }

        // Logical column identifiers
        internal const int ColIcon = 0;
        internal const int ColPath = 1;
        internal const int ColName = 2;
        internal const int ColMatches = 3;
        internal const int ColSize = 4;
        internal const int ColType = 5;
        internal const int ColDate = 6;
        internal const int ColReadOnly = 7;
        internal const int ColInfo = 8;

        // Default column order and widths
        private static readonly double[] DefaultColumnWidths = [22, 150, 200, 200, 70, 70, 150, 80, 120];

        // Map from GridViewColumn to its logical column id
        private readonly Dictionary<GridViewColumn, int> columnIds = [];

        private int GetColumnId(GridViewColumn col) => columnIds.TryGetValue(col, out int id) ? id : -1;

        public ResultsTree()
        {
            var columns = new (string header, double width, int id)[]
            {
                ("", 22, ColIcon),
                (Res.Main_ResultsHeader_Path, 150, ColPath),
                (Res.Main_ResultsHeader_Name, 200, ColName),
                (Res.Main_ResultsHeader_Matches, 200, ColMatches),
                (Res.Main_ResultsHeader_Size, 70, ColSize),
                (Res.Main_ResultsHeader_Type, 70, ColType),
                (Res.Main_ResultsHeader_DateModified, 150, ColDate),
                (Res.Main_ResultsHeader_ReadOnly, 80, ColReadOnly),
                (Res.Main_ResultsHeader_Info, 120, ColInfo),
            };

            TreeListViewColumns = [];
            foreach (var (header, width, id) in columns)
            {
                var col = new GridViewColumn { Header = header, Width = width };
                TreeListViewColumns.Add(col);
                columnIds[col] = id;
            }

            // Restore saved column order and widths
            RestoreColumnLayout();

            InitializeComponent();
            DataContextChanged += ResultsTree_DataContextChanged;

            // used to map the editor menu items on the TextBlock context menu
            NameScope.SetNameScope(contextMenu, this);
            NameScope.SetNameScope(contextMenuClassic, this);

            stickyScrollEnabled = GrepSearchResultsViewModel.StickyScrollEnabled;

            // Listen for column width changes from header resize and sync to view model
            var widthDescriptor = DependencyPropertyDescriptor.FromProperty(
                GridViewColumn.WidthProperty, typeof(GridViewColumn));

            foreach (var column in TreeListViewColumns)
            {
                widthDescriptor?.AddValueChanged(column, (s, e) => OnColumnLayoutChanged());
            }

            // Listen for column reorder (Move action in the collection)
            ((System.Collections.Specialized.INotifyCollectionChanged)TreeListViewColumns)
                .CollectionChanged += TreeListViewColumns_CollectionChanged;

            // Listen for column header clicks for sorting
            headerRowPresenter.PreviewMouseLeftButtonDown += HeaderRowPresenter_PreviewMouseLeftButtonDown;
            headerRowPresenter.PreviewMouseLeftButtonUp += HeaderRowPresenter_PreviewMouseLeftButtonUp;

            treeView.PreviewMouseWheel += TreeView_PreviewMouseWheel;
            treeView.PreviewTouchDown += TreeView_PreviewTouchDown;
            treeView.PreviewTouchMove += TreeView_PreviewTouchMove;
            treeView.PreviewTouchUp += TreeView_PreviewTouchUp;

            GrepSearchResultsViewModel.SearchResultsMessenger.Register("FormattedLinesLoaded", ScrollExpandedItemToTop);

            Loaded += (s, e) =>
            {
                OnColumnLayoutChanged();
                contextRoot.Margin = new Thickness(0, 0, SystemParameters.VerticalScrollBarWidth, 0);

                if (treeView.Template.FindName("_tv_scrollviewer_", treeView) is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
                }

                if (DataContext is GrepSearchResultsViewModel vm)
                {
                    vm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(GrepSearchResultsViewModel.StickyScrollEnabled))
                        {
                            stickyScrollEnabled = GrepSearchResultsViewModel.StickyScrollEnabled;
                            if (stickyScrollEnabled)
                            {
                                ResetContextItemVisible();
                            }
                            else
                            {
                                vm.ContextGrepResult = null;
                                vm.ContextGrepResultVisible = false;
                            }
                        }
                    };
                }
            };
        }

        private void TreeListViewColumns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Move ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                OnColumnLayoutChanged();
            }
        }

        private void RestoreColumnLayout()
        {
            try
            {
                var orderStr = GrepSettings.Instance.Get<string>(GrepSettings.Key.TreeListViewColumnOrder);
                var widthsStr = GrepSettings.Instance.Get<string>(GrepSettings.Key.TreeListViewColumnWidths);

                int[]? order = null;
                double[]? widths = null;

                if (!string.IsNullOrEmpty(orderStr))
                {
                    order = orderStr.Split(',').Select(s => int.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                }
                if (!string.IsNullOrEmpty(widthsStr))
                {
                    widths = widthsStr.Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                }

                if (order != null && order.Length == TreeListViewColumns.Count &&
                    widths != null && widths.Length == TreeListViewColumns.Count)
                {
                    // Build a lookup from logical column id to GridViewColumn
                    var columnById = new Dictionary<int, GridViewColumn>();
                    foreach (var col in TreeListViewColumns)
                    {
                        int id = GetColumnId(col);
                        if (id >= 0)
                            columnById[id] = col;
                    }

                    // Set widths by logical column tag (order[i] is the logical column at position i)
                    for (int i = 0; i < order.Length; i++)
                    {
                        if (columnById.TryGetValue(order[i], out var col))
                        {
                            col.Width = widths[i];
                        }
                    }

                    // Reorder the collection to match saved order
                    // Build desired sequence
                    var desired = new GridViewColumn[order.Length];
                    for (int i = 0; i < order.Length; i++)
                    {
                        if (columnById.TryGetValue(order[i], out var col))
                        {
                            desired[i] = col;
                        }
                    }

                    // Move columns into position
                    for (int i = 0; i < desired.Length; i++)
                    {
                        int currentIndex = TreeListViewColumns.IndexOf(desired[i]);
                        if (currentIndex != i)
                        {
                            TreeListViewColumns.Move(currentIndex, i);
                        }
                    }
                }
            }
            catch
            {
                // If anything goes wrong, keep defaults
            }
        }

        private void OnColumnLayoutChanged()
        {
            SyncColumnWidthsToViewModel();
            SaveColumnLayout();
        }

        private void SyncColumnWidthsToViewModel()
        {
            if (DataContext is GrepSearchResultsViewModel vm)
            {
                // Update positional widths (Column0Width..Column8Width)
                for (int i = 0; i < TreeListViewColumns.Count; i++)
                {
                    double w = TreeListViewColumns[i].ActualWidth;
                    switch (i)
                    {
                        case 0: vm.Column0Width = w; break;
                        case 1: vm.Column1Width = w; break;
                        case 2: vm.Column2Width = w; break;
                        case 3: vm.Column3Width = w; break;
                        case 4: vm.Column4Width = w; break;
                        case 5: vm.Column5Width = w; break;
                        case 6: vm.Column6Width = w; break;
                        case 7: vm.Column7Width = w; break;
                        case 8: vm.Column8Width = w; break;
                    }
                }

                // Update column indices (which display position each logical column is at)
                for (int i = 0; i < TreeListViewColumns.Count; i++)
                {
                    int colId = GetColumnId(TreeListViewColumns[i]);
                    if (colId >= 0)
                    {
                        switch (colId)
                        {
                            case ColIcon: vm.IconColumnIndex = i; break;
                            case ColPath: vm.PathColumnIndex = i; break;
                            case ColName: vm.NameColumnIndex = i; break;
                            case ColMatches: vm.MatchesColumnIndex = i; break;
                            case ColSize: vm.SizeColumnIndex = i; break;
                            case ColType: vm.TypeColumnIndex = i; break;
                            case ColDate: vm.DateColumnIndex = i; break;
                            case ColReadOnly: vm.ReadOnlyColumnIndex = i; break;
                            case ColInfo: vm.InfoColumnIndex = i; break;
                        }
                    }
                }
            }
        }

        private void SaveColumnLayout()
        {
            var order = new int[TreeListViewColumns.Count];
            var widths = new double[TreeListViewColumns.Count];

            for (int i = 0; i < TreeListViewColumns.Count; i++)
            {
                order[i] = GetColumnId(TreeListViewColumns[i]);
                widths[i] = TreeListViewColumns[i].ActualWidth;
            }

            GrepSettings.Instance.Set(GrepSettings.Key.TreeListViewColumnOrder,
                string.Join(",", order.Select(o => o.ToString(System.Globalization.CultureInfo.InvariantCulture))));
            GrepSettings.Instance.Set(GrepSettings.Key.TreeListViewColumnWidths,
                string.Join(",", widths.Select(w => w.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        }

        private static readonly Dictionary<int, SortType> columnSortTypeMap = new()
        {
            { ColPath, SortType.FileNameDepthFirst },
            { ColName, SortType.FileNameOnly },
            { ColMatches, SortType.MatchCount },
            { ColSize, SortType.Size },
            { ColType, SortType.FileTypeAndName },
            { ColDate, SortType.Date },
            { ColReadOnly, SortType.ReadOnly },
        };

        private Point headerMouseDownPos;
        private bool headerMouseDownOnHeader;

        private void HeaderRowPresenter_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            headerMouseDownPos = e.GetPosition(headerRowPresenter);

            // Check if the mouse is over a column header (not a gripper/thumb)
            DependencyObject? current = e.OriginalSource as DependencyObject;
            headerMouseDownOnHeader = false;
            while (current != null && current != headerRowPresenter)
            {
                if (current is System.Windows.Controls.Primitives.Thumb)
                {
                    // Clicking on the resize gripper, not a header click
                    return;
                }
                if (current is GridViewColumnHeader)
                {
                    headerMouseDownOnHeader = true;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }
        }

        private void HeaderRowPresenter_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!headerMouseDownOnHeader)
                return;

            // Check for drag (resize or reorder) — if the mouse moved significantly, ignore
            Point upPos = e.GetPosition(headerRowPresenter);
            if (Math.Abs(upPos.X - headerMouseDownPos.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(upPos.Y - headerMouseDownPos.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            // Walk up the visual tree from the clicked element to find the GridViewColumnHeader
            DependencyObject? current = e.OriginalSource as DependencyObject;
            GridViewColumnHeader? header = null;
            while (current != null && current != headerRowPresenter)
            {
                if (current is GridViewColumnHeader h)
                {
                    header = h;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            if (header == null || header.Role == GridViewColumnHeaderRole.Padding)
                return;

            // Find which column this header belongs to by matching header content
            GridViewColumn? column = null;
            foreach (var col in TreeListViewColumns)
            {
                if (Equals(col.Header, header.Content))
                {
                    column = col;
                    break;
                }
            }

            if (column == null)
                return;

            int colId = GetColumnId(column);
            if (!columnSortTypeMap.TryGetValue(colId, out SortType sortType))
                return;

            // Toggle direction if clicking the same column
            var currentSortType = GrepSettings.Instance.Get<SortType>(GrepSettings.Key.TypeOfSort);
            var currentDirection = GrepSettings.Instance.Get<ListSortDirection>(GrepSettings.Key.SortDirection);

            ListSortDirection newDirection;
            if (currentSortType == sortType)
            {
                newDirection = currentDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                newDirection = ListSortDirection.Ascending;
            }

            GrepSearchResultsViewModel.SearchResultsMessenger.NotifyColleagues(
                "SortColumn", new SortColumnRequest(sortType, newDirection));
        }

        internal void UpdateSortIndicator(int sortColumnId, ListSortDirection direction)
        {
            if (DataContext is GrepSearchResultsViewModel vm)
            {
                vm.SortColumnId = sortColumnId;
                vm.SortColumnDirection = direction;
            }

            // Update the sort arrow on each column header
            foreach (var column in TreeListViewColumns)
            {
                int colId = GetColumnId(column);
                var header = FindColumnHeader(column);
                if (header != null)
                {
                    var sortArrow = FindSortArrow(header);
                    if (sortArrow != null)
                    {
                        if (colId == sortColumnId)
                        {
                            sortArrow.Visibility = Visibility.Visible;
                            sortArrow.Data = direction == ListSortDirection.Ascending
                                ? System.Windows.Media.Geometry.Parse("M 0,6 L 4,0 L 8,6 Z")
                                : System.Windows.Media.Geometry.Parse("M 0,0 L 4,6 L 8,0 Z");
                        }
                        else
                        {
                            sortArrow.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        private GridViewColumnHeader? FindColumnHeader(GridViewColumn column)
        {
            // Walk the visual tree of the headerRowPresenter to find the header for this column
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(headerRowPresenter); i++)
            {
                if (VisualTreeHelper.GetChild(headerRowPresenter, i) is GridViewColumnHeader header &&
                    Equals(header.Content, column.Header))
                {
                    return header;
                }
            }
            return null;
        }

        private static System.Windows.Shapes.Path? FindSortArrow(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Shapes.Path path && path.Name == "sortArrow")
                {
                    return path;
                }
                var result = FindSortArrow(child);
                if (result != null) return result;
            }
            return null;
        }

        #region INameScope Members

        private readonly Dictionary<string, object> items = [];

        object INameScope.FindName(string name)
        {
            return items[name];
        }

        void INameScope.RegisterName(string name, object scopedElement)
        {
            items.Add(name, scopedElement);
        }

        void INameScope.UnregisterName(string name)
        {
            items.Remove(name);
        }

        #endregion

        void ResultsTree_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            viewModel = (GrepSearchResultsViewModel)DataContext;
            viewModel.TreeControl = this;
            viewModel.SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;
        }

        internal MultiSelectTreeView TreeView => treeView;

        internal async Task Next()
        {
            inNextPrevious = true;
            try
            {
                Cursor = Cursors.Wait;
                await NextLineMatch();
            }
            finally
            {
                Cursor = Cursors.Arrow;
                inNextPrevious = false;
            }
        }

        internal async Task NextFile()
        {
            inNextPrevious = true;
            try
            {
                Cursor = Cursors.Wait;
                await NextFileMatch();
            }
            finally
            {
                Cursor = Cursors.Arrow;
                inNextPrevious = false;
            }
        }

        internal async Task Previous()
        {
            inNextPrevious = true;
            try
            {
                // when moving backward, do not scroll to the top of the expanded item
                skipScrollOnExpand = true;
                Cursor = Cursors.Wait;
                await PreviousLineMatch();
            }
            finally
            {
                Cursor = Cursors.Arrow;
                skipScrollOnExpand = false;
                inNextPrevious = false;
            }
        }

        internal async Task PreviousFile()
        {
            inNextPrevious = true;
            try
            {
                // when moving backward, do not scroll to the top of the expanded item
                skipScrollOnExpand = true;
                Cursor = Cursors.Wait;
                await PreviousFileMatch();
            }
            finally
            {
                Cursor = Cursors.Arrow;
                skipScrollOnExpand = false;
                inNextPrevious = false;
            }
        }

        private void SelectedNodes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                OnSelectedItemsChanged();
            }
        }

        private bool isInRequestBringIntoView;
        private void TreeView_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Ignore re-entrant calls
            if (isInRequestBringIntoView)
                return;

            isInRequestBringIntoView = true;

            // Cancel the current scroll attempt
            e.Handled = true;

            // Call BringIntoView using a rectangle that extends into "negative space" to the left of our
            // actual control. This allows the vertical scrolling behavior to operate without adversely
            // affecting the current horizontal scroll position.
            if (sender is TreeViewItem tvi && e.OriginalSource is FrameworkElement element)
            {
                // +3 is a margin between the selected tvi and the bottom of control
                // otherwise it will be clipped slightly
                double height = element.ActualHeight + 3;
                double width = element.ActualWidth;
                Rect newTargetRect = new(double.NegativeInfinity, 0, width, height);
                tvi.BringIntoView(newTargetRect);
            }

            isInRequestBringIntoView = false;
        }

        private void TreeView_OnSelected(object sender, RoutedEventArgs e)
        {
            // Correctly handle programmatically selected items
            if (sender is TreeViewItem tvi)
            {
                tvi.BringIntoView();
                e.Handled = true;
            }
        }

        private TreeViewItem? expandedTreeViewItem;
        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            // if going backward, expandedTreeViewItem will be set to null
            // so it does not get scrolled to the top: we want the bottom
            // of the expanded item to be visible
            if (skipScrollOnExpand)
            {
                expandedTreeViewItem = null; // clear if already set
                return;
            }

            if (sender is TreeViewItem tvi && tvi.Header is FormattedGrepResult)
            {
                expandedTreeViewItem = tvi;
            }
        }

        private void ScrollExpandedItemToTop()
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (expandedTreeViewItem != null)
                {
                    // This is used for navigating forward in the tree view
                    // scroll down so the expanded item is above top of the view
                    treeView.ScrollViewer?.ScrollToVerticalOffset(treeView.ScrollViewer.VerticalOffset + treeView.ActualHeight);
                    // BringIntoView will scroll the expanded item to the top of the view
                    expandedTreeViewItem.BringIntoView();
                    expandedTreeViewItem = null;
                }
            });
        }

        #region Tree Tasks

        internal void SetFocus()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                treeView.Focus();
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }

        private async Task NextLineMatch()
        {
            if (viewModel == null) return;

            FormattedGrepResult? selectedResult = viewModel.SelectedNodes.OfType<FormattedGrepResult>()
                .Where(n => n != null)
                .LastOrDefault();
            FormattedGrepLine? selectedLine = viewModel.SelectedNodes.OfType<FormattedGrepLine>()
                .Where(n => n != null)
                .OrderBy(n => n.GrepLine.LineNumber)
                .LastOrDefault();

            if (selectedResult == null && selectedLine == null)
            {
                var firstResult = viewModel.SearchResults.FirstOrDefault();
                if (firstResult != null && firstResult.FormattedLines.Count > 0)
                {
                    await SelectFirstChild(firstResult);
                }
                else if (firstResult != null)
                {
                    var tvi = GetContainerForItem(treeView, firstResult);
                    if (tvi != null)
                    {
                        tvi.IsSelected = true;
                    }
                }
            }
            else if (selectedLine != null)
            {
                if (!SelectNextLine(selectedLine))
                {
                    selectedLine.Parent.IsExpanded = false;
                    await SelectNextResult(selectedLine);
                }
            }
            else if (selectedResult != null && selectedResult.FormattedLines.Count > 0)
            {
                await SelectFirstChild(selectedResult);
            }
            else if (selectedResult != null)
            {
                SelectNextFile(selectedResult);
            }
        }

        private async Task NextFileMatch()
        {
            if (viewModel == null) return;

            FormattedGrepResult? selectedResult = viewModel.SelectedNodes.OfType<FormattedGrepResult>()
                .Where(n => n != null)
                .LastOrDefault();
            FormattedGrepLine? selectedLine = viewModel.SelectedNodes.OfType<FormattedGrepLine>()
                .Where(n => n != null)
                .OrderBy(n => n.GrepLine.LineNumber)
                .LastOrDefault();

            if (selectedResult == null && selectedLine == null)
            {
                var firstResult = viewModel.SearchResults.FirstOrDefault();
                if (firstResult != null && firstResult.FormattedLines.Count > 0)
                {
                    await SelectFirstChild(firstResult);
                }
                else if (firstResult != null)
                {
                    var tvi = GetContainerForItem(treeView, firstResult);
                    if (tvi != null)
                    {
                        tvi.IsSelected = true;
                    }
                }
            }
            else if (selectedLine != null)
            {
                await SelectNextResult(selectedLine);
            }
            else if (selectedResult != null && selectedResult.FormattedLines.Count > 0)
            {
                await SelectNextResult(selectedResult.FormattedLines.First());
            }
            else if (selectedResult != null)
            {
                SelectNextFile(selectedResult);
            }
        }

        private async Task PreviousLineMatch()
        {
            if (viewModel == null) return;

            FormattedGrepResult? selectedResult = viewModel.SelectedNodes.OfType<FormattedGrepResult>()
                .Where(n => n != null)
                .FirstOrDefault();
            FormattedGrepLine? selectedLine = viewModel.SelectedNodes.OfType<FormattedGrepLine>()
                .Where(n => n != null)
                .OrderBy(n => n.GrepLine.LineNumber)
                .FirstOrDefault();

            if (selectedResult == null && selectedLine == null)
            {
                var lastResult = viewModel.SearchResults.LastOrDefault();
                if (lastResult != null && lastResult.FormattedLines.Count > 0)
                {
                    await SelectLastChild(lastResult);
                }
                else if (lastResult != null)
                {
                    var tvi = GetContainerForItem(treeView, lastResult);
                    if (tvi != null)
                    {
                        tvi.IsSelected = true;
                    }
                }
            }
            else if (selectedLine != null)
            {
                if (!SelectPreviousLine(selectedLine))
                {
                    selectedLine.Parent.IsExpanded = false;
                    await SelectPreviousResult(selectedLine);
                }
            }
            else if (selectedResult != null && selectedResult.FormattedLines.Count > 0)
            {
                await SelectPreviousResult(selectedResult);
            }
            else if (selectedResult != null)
            {
                SelectPreviousFile(selectedResult);
            }
        }

        private async Task PreviousFileMatch()
        {
            if (viewModel == null) return;

            FormattedGrepResult? selectedResult = viewModel.SelectedNodes.OfType<FormattedGrepResult>()
                .Where(n => n != null)
                .FirstOrDefault();
            FormattedGrepLine? selectedLine = viewModel.SelectedNodes.OfType<FormattedGrepLine>()
                .Where(n => n != null)
                .OrderBy(n => n.GrepLine.LineNumber)
                .FirstOrDefault();

            if (selectedResult == null && selectedLine == null)
            {
                var lastResult = viewModel.SearchResults.LastOrDefault();
                if (lastResult != null && lastResult.FormattedLines.Count > 0)
                {
                    await SelectLastChild(lastResult);
                }
                else if (lastResult != null)
                {
                    var tvi = GetContainerForItem(treeView, lastResult);
                    if (tvi != null)
                    {
                        tvi.IsSelected = true;
                    }
                }
            }
            else if (selectedLine != null)
            {
                await SelectPreviousResult(selectedLine);
            }
            else if (selectedResult != null && selectedResult.FormattedLines.Count > 0)
            {
                await SelectPreviousResult(selectedResult);
            }
            else if (selectedResult != null)
            {
                SelectPreviousFile(selectedResult);
            }
        }

        internal async Task ExpandAll()
        {
            skipScrollOnExpand = true;
            foreach (FormattedGrepResult result in treeView.Items)
            {
                await result.ExpandTreeNode();
            }
            skipScrollOnExpand = false;
        }

        internal void CollapseAll()
        {
            foreach (FormattedGrepResult result in treeView.Items)
            {
                result.CollapseTreeNode();
            }
        }

        internal bool IsAnyExpanded()
        {
            foreach (FormattedGrepResult result in treeView.Items)
            {
                if (result.IsExpanded)
                    return true;
            }
            return false;
        }

        private void TreeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item)
            {
                item.Focus();
                e.Handled = true;
            }
        }

        private void TreeView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (viewModel == null) return;

            // middle button click on a file node or line node opens file with custom editor
            if (e.ChangedButton == MouseButton.Middle)
            {
                if (treeView.SelectedItem is FormattedGrepResult file)
                {
                    viewModel.OpenFile(file, GrepSettings.Instance.HasCustomEditor,
                        OpenFileArgs.DefaultEditor);
                }
                else if (treeView.SelectedItem is FormattedGrepLine line)
                {
                    viewModel.OpenFile(line, GrepSettings.Instance.HasCustomEditor,
                        OpenFileArgs.DefaultEditor);
                }
                e.Handled = true;
            }
        }

        private void TreeView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (viewModel == null) return;

            // alt+double click on a file node or line node opens file with associated application
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (treeView.SelectedItem is FormattedGrepResult fileNode)
                {
                    viewModel.OpenFile(fileNode, false, string.Empty);
                }
                else if (treeView.SelectedItem is FormattedGrepLine line)
                {
                    viewModel.OpenFile(line, false, string.Empty);
                }
                e.Handled = true;
            }
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (viewModel == null) return;

            // double click on a line node opens file
            if (treeView.SelectedItem is FormattedGrepLine line &&
                (e.OriginalSource is TextBlock || e.OriginalSource is Run))
            {
                bool useCustomEditor = !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) &&
                    GrepSettings.Instance.HasCustomEditor;
                viewModel.OpenFile(line, useCustomEditor,
                        OpenFileArgs.DefaultEditor);
                e.Handled = true;
            }
        }

        internal void OnSelectedItemsChanged()
        {
            if (viewModel == null) return;

            Window parentWindow = Window.GetWindow(this);

            var rect = new System.Drawing.RectangleF { Height = (float)parentWindow.ActualHeight, Width = (float)parentWindow.ActualWidth, X = (float)parentWindow.Left, Y = (float)parentWindow.Top };

            bool lineSelectionRaised = false;
            if (viewModel.SelectedNodes is IList items && items.Count > 0)
            {
                if (items[0] is FormattedGrepLine formattedGrepLine)
                {
                    if (items.Count == 1)
                    {
                        var lineNumber = formattedGrepLine.GrepLine.LineNumber;
                        var lineMatches = formattedGrepLine.GrepLine.Matches;
                        var fileMatches = formattedGrepLine.Parent.GrepResult.Matches;
                        if (lineMatches.Count > 0)
                        {
                            var id = lineMatches[0].FileMatchId;
                            var matchOrdinal = 1 + fileMatches.IndexOf(m => m.FileMatchId == id);
                            viewModel.OnGrepLineSelectionChanged(formattedGrepLine, lineMatches.Count, matchOrdinal, fileMatches.Count);
                            lineSelectionRaised = true;
                        }
                    }

                    viewModel.PreviewFile(formattedGrepLine, rect);
                }
                else if (items[0] is FormattedGrepResult formattedGrepResult)
                {
                    viewModel.PreviewFile(formattedGrepResult, rect);
                }
            }

            if (!lineSelectionRaised)
            {
                viewModel.OnGrepLineSelectionChanged(null, 0, -1, 0);
            }
        }

        #endregion

        #region Zoom

        private readonly Dictionary<int, Point> touchIds = [];

        private void TreeView_PreviewTouchDown(object? sender, TouchEventArgs e)
        {
            if (sender is IInputElement ctrl && !touchIds.ContainsKey(e.TouchDevice.Id))
            {
                var pt = e.GetTouchPoint(ctrl).Position;
                touchIds.Add(e.TouchDevice.Id, pt);
            }
        }

        private void TreeView_PreviewTouchUp(object? sender, TouchEventArgs e)
        {
            touchIds.Remove(e.TouchDevice.Id);
        }

        private void TreeView_PreviewTouchMove(object? sender, TouchEventArgs e)
        {
            if (sender is IInputElement ctrl)
            {
                // sometimes a PreviewTouchUp event is lost when the user is on the scrollbar or edge of the window
                // if our captured touches do not match the scrollviewer, resynch to the scrollviewer
                if (e.OriginalSource is ScrollViewer scrollViewer)
                {
                    var svTouches = scrollViewer.TouchesCaptured.Select(t => t.Id);
                    var myTouches = touchIds.Keys.Select(k => k);
                    bool equal = svTouches.OrderBy(i => i).SequenceEqual(myTouches.OrderBy(i => i));

                    if (!equal)
                    {
                        touchIds.Clear();
                        foreach (var t in scrollViewer.TouchesCaptured)
                        {
                            var pt = t.GetTouchPoint(ctrl).Position;
                            touchIds.Add(t.Id, pt);
                        }
                    }
                }

                if (viewModel != null && viewModel.SearchResults.Count > 0 &&
                    touchIds.TryGetValue(e.TouchDevice.Id, out Point value) && touchIds.Count == 2)
                {
                    var pNew = e.GetTouchPoint(ctrl).Position;

                    var otherTouchId = touchIds.Keys.Where(k => k != e.TouchDevice.Id).FirstOrDefault();
                    var p0 = touchIds[otherTouchId];
                    var p1 = value;

                    var dx = p1.X - p0.X;
                    var dy = p1.Y - p0.Y;
                    var dist1 = dx * dx + dy * dy;

                    dx = pNew.X - p0.X;
                    dy = pNew.Y - p0.Y;
                    var dist2 = dx * dx + dy * dy;

                    if (Math.Abs(dist2 - dist1) > 200)
                    {
                        if (dist1 < dist2 && viewModel.ResultsScale < 2.0)
                        {
                            viewModel.ResultsScale *= 1.005;
                        }

                        if (dist1 > dist2 && viewModel.ResultsScale > 0.8)
                        {
                            viewModel.ResultsScale /= 1.005;
                        }

                        e.Handled = true;
                    }

                    // and update position for this touch
                    touchIds[e.TouchDevice.Id] = pNew;
                }
            }
        }

        private void TreeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (viewModel == null) return;

            bool handle = (Keyboard.Modifiers & ModifierKeys.Control) > 0 && viewModel.SearchResults.Count > 0;
            if (!handle)
                return;

            if (e.Delta > 0 && viewModel.ResultsScale < 2.0)
            {
                viewModel.ResultsScale *= 1.05;
            }

            if (e.Delta < 0 && viewModel.ResultsScale > 0.8)
            {
                viewModel.ResultsScale /= 1.05;
            }

            e.Handled = true;
        }

        #endregion

        #region DragDropEvents
        private static UIElement? _draggedElt;
        private static bool _isMouseDown = false;
        private static System.Windows.Point _dragStartPoint;

        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Make this the new drag source
            _draggedElt = e.Source as UIElement;
            _dragStartPoint = e.GetPosition(GetTopContainer());
            _isMouseDown = true;
        }

        private void TreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is UIElement uiElt && _isMouseDown && IsDragGesture(e.GetPosition(GetTopContainer())))
            {
                TreeDragStarted(uiElt);
            }
        }

        private void TreeDragStarted(UIElement uiElt)
        {
            _isMouseDown = false;
            Mouse.Capture(uiElt);

            DataObject data = new();
            // set this data format to prevent dropping onto our own main window
            data.SetData(App.InstanceId, string.Empty);

            // if there are lines selected, choose text drag and drop operation
            var lines = viewModel?.GetSelectedGrepLineText();
            if (!string.IsNullOrWhiteSpace(lines))
            {
                data.SetData(DataFormats.Text, lines);
            }
            else if (viewModel != null && viewModel.HasGrepResultSelection)
            {
                var list = viewModel.GetSelectedFileNames(true);
                StringCollection files = [.. list.ToArray()];
                data.SetFileDropList(files);
            }

            DragDropEffects supportedEffects = DragDropEffects.Move | DragDropEffects.Copy;
            // Perform DragDrop
            _ = DragDrop.DoDragDrop(_draggedElt, data, supportedEffects);

            // Clean up
            Mouse.Capture(null);
            _draggedElt = null;
        }

        private static bool IsDragGesture(Point point)
        {
            bool hGesture = Math.Abs(point.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance;
            bool vGesture = Math.Abs(point.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;

            return hGesture | vGesture;
        }

        private static UIElement? GetTopContainer()
        {
            return Application.Current.MainWindow.Content as UIElement;
        }
        #endregion

        #region Find & Select Tree View Item

        private async Task SelectFirstChild(FormattedGrepResult grepResult)
        {
            if (!grepResult.IsExpanded)
            {
                await grepResult.ExpandTreeNode();
            }
            var firstLine = grepResult.FormattedLines.Where(l => !l.GrepLine.IsContext).FirstOrDefault();
            if (firstLine != null)
            {
                var parent = GetContainerForItem(treeView, grepResult);
                if (parent != null)
                {
                    var tvi = GetContainerForItem(parent, firstLine);
                    if (tvi != null)
                    {
                        tvi.IsSelected = true;
                    }
                }
            }
        }

        private void SelectNextFile(FormattedGrepResult selectedResult)
        {
            if (viewModel == null) return;

            int idx = viewModel.SearchResults.IndexOf(selectedResult) + 1;
            if (idx >= viewModel.SearchResults.Count) idx = 0;

            var nextResult = viewModel.SearchResults[idx];
            var tvi = GetContainerForItem(treeView, nextResult);
            if (tvi != null)
            {
                tvi.IsSelected = true;
            }
        }

        private async Task SelectNextResult(FormattedGrepLine currentLine)
        {
            if (viewModel == null) return;

            var grepResult = currentLine.Parent;
            int idx = viewModel.SearchResults.IndexOf(grepResult) + 1;
            if (idx >= viewModel.SearchResults.Count) idx = 0;

            var nextResult = viewModel.SearchResults[idx];
            await SelectFirstChild(nextResult);
        }

        private bool SelectNextLine(FormattedGrepLine currentLine)
        {
            var grepResult = currentLine.Parent;
            var nextLine = grepResult.FormattedLines.Where(l => !l.GrepLine.IsContext &&
                    l.GrepLine.LineNumber > currentLine.GrepLine.LineNumber)
                .FirstOrDefault();

            if (nextLine != null)
            {
                var parent = GetTreeViewItemParent(treeView, currentLine);
                if (parent != null)
                {
                    var tvi = GetContainerForItem(parent, nextLine);
                    if (tvi != null)
                    {
                        tvi.IsSelected = true;
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task SelectLastChild(FormattedGrepResult grepResult)
        {
            if (!grepResult.IsExpanded)
            {
                await grepResult.ExpandTreeNode();
            }
            var lastLine = grepResult.FormattedLines.Where(l => !l.GrepLine.IsContext).LastOrDefault();
            if (lastLine != null)
            {
                var parent = GetContainerForItem(treeView, grepResult);
                if (parent != null)
                {
                    var tvi = GetContainerForItem(parent, lastLine);
                    if (tvi != null)
                    {
                        tvi.IsSelected = true;
                    }
                }
            }
        }

        private void SelectPreviousFile(FormattedGrepResult result)
        {
            if (viewModel == null) return;

            int idx = viewModel.SearchResults.IndexOf(result) - 1;
            if (idx < 0) idx = viewModel.SearchResults.Count - 1;

            var previousResult = viewModel.SearchResults[idx];
            var tvi = GetContainerForItem(treeView, previousResult);
            if (tvi != null)
            {
                tvi.IsSelected = true;
            }
        }

        private async Task SelectPreviousResult(FormattedGrepResult result)
        {
            if (viewModel == null) return;

            int idx = viewModel.SearchResults.IndexOf(result) - 1;
            if (idx < 0) idx = viewModel.SearchResults.Count - 1;

            var previousResult = viewModel.SearchResults[idx];
            await SelectLastChild(previousResult);
        }

        private async Task SelectPreviousResult(FormattedGrepLine currentLine)
        {
            if (viewModel == null) return;

            var grepResult = currentLine.Parent;
            int idx = viewModel.SearchResults.IndexOf(grepResult) - 1;
            if (idx < 0) idx = viewModel.SearchResults.Count - 1;

            var previousResult = viewModel.SearchResults[idx];
            await SelectLastChild(previousResult);
        }

        private bool SelectPreviousLine(FormattedGrepLine currentLine)
        {
            var grepResult = currentLine.Parent;
            var previousLine = grepResult.FormattedLines.Where(l => !l.GrepLine.IsContext &&
                    l.GrepLine.LineNumber < currentLine.GrepLine.LineNumber)
                .LastOrDefault();

            if (previousLine != null)
            {
                var parent = GetTreeViewItemParent(treeView, currentLine);
                if (parent != null)
                {
                    var tvi = GetContainerForItem(parent, previousLine);
                    if (tvi != null)
                    {
                        tvi.IsSelected = true;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the TreeViewItem container for a known data item by looking up its index in the data model and
        /// materializing only that single item via BringIntoView on the virtualizing panel. This avoids iterating
        /// through all items in the container.
        /// </summary>
        internal static TreeViewItem? GetContainerForItem(ItemsControl container, object item)
        {
            if (container == null)
                return null;

            // Look up the index from the data model - this works even for virtualized items
            int index = container.Items.IndexOf(item);
            if (index < 0)
                return null;

            // Expand the container if needed
            if (container is TreeViewItem tvi && !tvi.IsExpanded)
            {
                container.SetValue(TreeViewItem.IsExpandedProperty, true);
            }

            container.ApplyTemplate();
            ItemsPresenter? itemsPresenter =
                (ItemsPresenter)container.Template.FindName("ItemsHost", container);
            if (itemsPresenter != null)
            {
                itemsPresenter.ApplyTemplate();
            }
            else
            {
                itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                if (itemsPresenter == null)
                {
                    container.UpdateLayout();
                    itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                }
            }

            if (itemsPresenter == null)
                return null;

            Panel itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);
            _ = itemsHostPanel.Children;

            // Always ask the virtualizing panel to bring the index into view.
            // This ensures the item is scrolled into the visible area, even if
            // the container was already realized but just outside the viewport.
            if (itemsHostPanel is MyVirtualizingStackPanel virtualizingPanel)
            {
                virtualizingPanel.BringIntoView(index);
            }

            var result = container.ItemContainerGenerator.ContainerFromIndex(index) as TreeViewItem;
            result?.BringIntoView();
            return result;
        }

        /// <summary>
        /// Search for an element of a certain type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of element to find.</typeparam>
        /// <param name="visual">The parent element.</param>
        /// <returns></returns>
        private static T? FindVisualChild<T>(Visual visual) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual child = (Visual)VisualTreeHelper.GetChild(visual, i);
                if (child != null)
                {
                    if (child is T correctlyTyped)
                    {
                        return correctlyTyped;
                    }

                    T? descendent = FindVisualChild<T>(child);
                    if (descendent != null)
                    {
                        return descendent;
                    }
                }
            }

            return null;
        }

        private static TreeViewItem? GetTreeViewItemParent(TreeView treeView, object item)
        {
            TreeViewItem? treeViewItem = ContainerFromItemRecursive(treeView.ItemContainerGenerator, item);
            if (treeViewItem != null)
            {
                DependencyObject parent = VisualTreeHelper.GetParent(treeViewItem);
                while (!(parent is TreeViewItem || parent is TreeView))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                return parent as TreeViewItem;
            }
            return null;
        }

        private static TreeViewItem? ContainerFromItemRecursive(ItemContainerGenerator root, object item)
        {
            if (root.ContainerFromItem(item) is TreeViewItem tvItem)
            {
                return tvItem;
            }

            TreeViewItem? treeViewItem;
            foreach (var subItem in root.Items)
            {
                treeViewItem = root.ContainerFromItem(subItem) as TreeViewItem;
                if (treeViewItem != null)
                {
                    var search = ContainerFromItemRecursive(treeViewItem.ItemContainerGenerator, item);
                    if (search != null)
                    {
                        return search;
                    }
                }
            }
            return null;
        }
        #endregion

        #region Sticky Scroll

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                if (e.HorizontalChange != 0 || e.ExtentWidthChange != 0)
                {
                    // Ensure header and context have enough width to scroll the full extent
                    headerRowPresenter.MinWidth = sv.ExtentWidth;
                    contextControl.MinWidth = sv.ExtentWidth - 22; // account for the 22px left margin

                    // Keep the header row and context overlay aligned with the horizontal scroll
                    headerScrollViewer.ScrollToHorizontalOffset(sv.HorizontalOffset);
                    contextScrollViewer.ScrollToHorizontalOffset(sv.HorizontalOffset);
                }
            }

            if (stickyScrollEnabled && !inNextPrevious && e.VerticalChange != 0)
            {
                ResetContextItemVisible();
            }
        }

        private void ResetContextItemVisible()
        {
            if (DataContext is GrepSearchResultsViewModel vm)
            {
                FormattedGrepResult? item = GetTopVisibleLine(treeView);
                if (!ReferenceEquals(item, vm.ContextGrepResult))
                {
                    vm.ContextGrepResult = item;
                    vm.ContextGrepResultVisible = item != null;
                    return;
                }

                bool newValue = false, currentValue = vm.ContextGrepResultVisible;

                if (contextControl.DataContext is FormattedGrepResult result)
                {
                    if (treeView.ItemContainerGenerator.ContainerFromItem(result) is
                            TreeViewItem treeViewItem)
                    {
                        newValue = !IsUserVisible(treeView, treeViewItem);
                    }
                }
                else
                {
                    newValue = false;
                }

                if (newValue != currentValue)
                {
                    vm.ContextGrepResultVisible = newValue;
                }
            }
        }

        private static bool IsUserVisible(TreeView treeView, TreeViewItem treeViewItem)
        {
            if (!treeViewItem.IsVisible)
            {
                return false;
            }

            // a TreeViewItem Actual Height includes all of its children
            Rect tviRect = new(0.0, 0.0, treeViewItem.ActualWidth, treeViewItem.ActualHeight);
            var header = GetHeaderControl(treeViewItem);
            if (header != null)
            {
                tviRect = new(0.0, 0.0, header.ActualWidth, header.ActualHeight);
            }

            Rect ItemBounds = treeViewItem.TransformToAncestor(treeView).TransformBounds(tviRect);
            Rect containerRect = new(0.0, 0.0, treeView.ActualWidth, treeView.ActualHeight);
            bool visible = ItemBounds.Top + 5 >= containerRect.Top && ItemBounds.Bottom <= containerRect.Bottom;
            return visible;
        }

        private static FrameworkElement GetHeaderControl(TreeViewItem item)
        {
            return (FrameworkElement)item.Template.FindName("PART_Header", item);
        }

        private static FormattedGrepResult? GetTopVisibleLine(TreeView treeView)
        {
            if (treeView.Items.Count > 0)
            {
                foreach (FormattedGrepResult node in treeView.Items.Cast<FormattedGrepResult>())
                {
                    if (node.IsExpanded)
                    {
                        if (treeView.ItemContainerGenerator.ContainerFromItem(node) is TreeViewItem container)
                        {
                            if (IsUserVisible(treeView, container))
                            {
                                return null;
                            }
                            else
                            {
                                foreach (FormattedGrepLine childNode in node.Children.Cast<FormattedGrepLine>())
                                {
                                    if (container.ItemContainerGenerator.ContainerFromItem(childNode) is TreeViewItem treeViewItem &&
                                        IsUserVisible(treeView, treeViewItem))
                                    {
                                        return node;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
