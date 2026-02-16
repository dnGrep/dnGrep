using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using dnGREP.Common;

namespace dnGREP.WPF.UserControls
{
    public enum SearchDirection { Down = 0, Up };

    /// <summary>
    /// Interaction logic for ResultsTree.xaml
    /// </summary>
    public partial class ResultsTree : UserControl
    {
        private GrepSearchResultsViewModel? viewModel;
        private bool skipScrollOnExpand;

        public ResultsTree()
        {
            InitializeComponent();
            DataContextChanged += ResultsTree_DataContextChanged;

            treeView.PreviewMouseWheel += TreeView_PreviewMouseWheel;
            treeView.PreviewTouchDown += TreeView_PreviewTouchDown;
            treeView.PreviewTouchMove += TreeView_PreviewTouchMove;
            treeView.PreviewTouchUp += TreeView_PreviewTouchUp;

            GrepSearchResultsViewModel.SearchResultsMessenger.Register("FormattedLinesLoaded", ScrollExpandedItemToTop);
        }

        void ResultsTree_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            viewModel = (GrepSearchResultsViewModel)DataContext;
            viewModel.TreeControl = this;
            viewModel.SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;
        }

        internal MultiSelectTreeView TreeView => treeView;

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

        internal async Task Next()
        {
            try
            {
                Cursor = Cursors.Wait;
                await NextLineMatch();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        internal async Task NextFile()
        {
            try
            {
                Cursor = Cursors.Wait;
                await NextFileMatch();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        internal async Task Previous()
        {
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
            }
        }

        internal async Task PreviousFile()
        {
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
            }
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
                    var tvi = GetTreeViewItem(treeView, firstResult, null, SearchDirection.Down, 1);
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
                    var tvi = GetTreeViewItem(treeView, firstResult, null, SearchDirection.Down, 1);
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
                    var tvi = GetTreeViewItem(treeView, lastResult, null, SearchDirection.Down, 1);
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
                    var tvi = GetTreeViewItem(treeView, lastResult, null, SearchDirection.Down, 1);
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
                var parent = GetTreeViewItem(treeView, grepResult, null, SearchDirection.Down, 1);
                ItemsControl container = parent as ItemsControl ?? treeView;
                int depth = parent != null ? 1 : 2;
                var tvi = GetTreeViewItem(container, firstLine, null, SearchDirection.Down, depth);
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                }
            }
        }

        private void SelectNextFile(FormattedGrepResult selectedResult)
        {
            if (viewModel == null) return;

            int idx = viewModel.SearchResults.IndexOf(selectedResult) + 1;
            if (idx >= viewModel.SearchResults.Count) idx = 0;

            var nextResult = viewModel.SearchResults[idx];
            var tvi = GetTreeViewItem(treeView, nextResult, null, SearchDirection.Down, 1);
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
                ItemsControl container = parent as ItemsControl ?? treeView;
                int depth = parent != null ? 1 : 2;
                var tvi = GetTreeViewItem(container, nextLine, currentLine, SearchDirection.Down, depth);
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                    return true;
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
                var parent = GetTreeViewItem(treeView, grepResult, null, SearchDirection.Up, 1);
                ItemsControl container = parent as ItemsControl ?? treeView;
                int depth = parent != null ? 1 : 2;
                var tvi = GetTreeViewItem(container, lastLine, null, SearchDirection.Up, depth);
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                }
            }
        }

        private void SelectPreviousFile(FormattedGrepResult result)
        {
            if (viewModel == null) return;

            int idx = viewModel.SearchResults.IndexOf(result) - 1;
            if (idx < 0) idx = viewModel.SearchResults.Count - 1;

            var previousResult = viewModel.SearchResults[idx];
            var tvi = GetTreeViewItem(treeView, previousResult, null, SearchDirection.Down, 1);
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
                ItemsControl container = parent as ItemsControl ?? treeView;
                int depth = parent != null ? 1 : 2;
                var tvi = GetTreeViewItem(container, previousLine, currentLine, SearchDirection.Up, depth);
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                    return true;
                }
            }
            return false;
        }

        // this method is based on and modified from: 
        // https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-find-a-treeviewitem-in-a-treeview

        /// <summary>
        /// Recursively search for an item in this subtree.
        /// </summary>
        /// <param name="container">
        /// The parent ItemsControl. This can be a TreeView or a TreeViewItem.
        /// </param>
        /// <param name="item">
        /// The item to search for.
        /// </param>
        /// <returns>
        /// The TreeViewItem that contains the specified item.
        /// </returns>
        internal static TreeViewItem? GetTreeViewItem(ItemsControl container, object item, object? selectedItem, SearchDirection dir, int depth)
        {
            if (container != null)
            {
                if (container.DataContext == item)
                {
                    return container as TreeViewItem;
                }
                else if (depth <= 0)
                {
                    return null;
                }

                // Expand the current container
                if (container is TreeViewItem item1 && !item1.IsExpanded)
                {
                    container.SetValue(TreeViewItem.IsExpandedProperty, true);
                }

                // Try to generate the ItemsPresenter and the ItemsPanel.
                // by calling ApplyTemplate.  Note that in the
                // virtualizing case even if the item is marked
                // expanded we still need to do this step in order to
                // regenerate the visuals because they may have been virtualized away.

                container.ApplyTemplate();
                ItemsPresenter? itemsPresenter =
                    (ItemsPresenter)container.Template.FindName("ItemsHost", container);
                if (itemsPresenter != null)
                {
                    itemsPresenter.ApplyTemplate();
                }
                else
                {
                    // The Tree template has not named the ItemsPresenter,
                    // so walk the descendants and find the child.
                    itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                    if (itemsPresenter == null)
                    {
                        container.UpdateLayout();

                        itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                    }
                }

                Panel itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);

                // Ensure that the generator for this panel has been created.
                _ = itemsHostPanel.Children;

                int startIndex = -1;
                if (selectedItem != null)
                {
                    startIndex = IndexForItem(container.ItemContainerGenerator, selectedItem);
                }

                int count = container.Items.Count;
                int inc = dir == SearchDirection.Down ? 1 : -1;
                int idx = startIndex > -1 ? startIndex : dir == SearchDirection.Down ? 0 : count - 1;
                for (; idx < count && idx >= 0; idx += inc)
                {
                    TreeViewItem subContainer;
                    if (itemsHostPanel is MyVirtualizingStackPanel virtualizingPanel)
                    {
                        // Bring the item into view so
                        // that the container will be generated.
                        virtualizingPanel.BringIntoView(idx);

                        subContainer =
                            (TreeViewItem)container.ItemContainerGenerator.
                            ContainerFromIndex(idx);
                    }
                    else
                    {
                        subContainer =
                            (TreeViewItem)container.ItemContainerGenerator.
                            ContainerFromIndex(idx);

                        // Bring the item into view to maintain the
                        // same behavior as with a virtualizing panel.
                        subContainer?.BringIntoView();
                    }

                    if (subContainer != null)
                    {
                        // Search the next level for the object.
                        TreeViewItem? resultContainer = GetTreeViewItem(subContainer, item, selectedItem, dir, depth - 1);
                        if (resultContainer != null)
                        {
                            return resultContainer;
                        }
                        else
                        {
                            // The object is not under this TreeViewItem
                            // so collapse it.
                            subContainer.IsExpanded = false;
                        }
                    }
                }
            }

            return null;
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

        private static int IndexForItem(ItemContainerGenerator root, object item)
        {
            if (root.ContainerFromItem(item) is TreeViewItem treeViewItem)
            {
                int index = root.IndexFromContainer(treeViewItem);
                return index;
            }

            return -1;
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
    }
}
