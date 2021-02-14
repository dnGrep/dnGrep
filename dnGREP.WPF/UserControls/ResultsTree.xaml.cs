using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;

namespace dnGREP.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for ResultsTree.xaml
    /// </summary>
    public partial class ResultsTree : UserControl
    {
        private enum SearchDirection { Down = 0, Up };
        private ObservableGrepSearchResults inputData = new ObservableGrepSearchResults();

        public ResultsTree()
        {
            InitializeComponent();
            this.DataContextChanged += ResultsTree_DataContextChanged;

            treeView.PreviewMouseWheel += treeView_PreviewMouseWheel;
            treeView.PreviewTouchDown += treeView_PreviewTouchDown;
            treeView.PreviewTouchMove += treeView_PreviewTouchMove;
            treeView.PreviewTouchUp += treeView_PreviewTouchUp;
        }

        void ResultsTree_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            inputData = ((ObservableGrepSearchResults)(this.DataContext));
        }

        private void treeView_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // keep tree view from scrolling horizontally when an item is (mouse) selected
            var treeViewItem = (TreeViewItem)sender;
            var scrollViewer = treeView.Template.FindName("_tv_scrollviewer_", treeView) as ScrollViewer;

            Point topLeftInTreeViewCoordinates = treeViewItem.TransformToAncestor(treeView).Transform(new Point(0, 0));
            var treeViewItemTop = topLeftInTreeViewCoordinates.Y;
            if (treeViewItemTop < 0 ||
                treeViewItemTop + treeViewItem.ActualHeight > scrollViewer.ViewportHeight ||
                treeViewItem.ActualHeight > scrollViewer.ViewportHeight)
            {
                // if the item is not visible or too "tall", don't do anything; let them scroll it into view
                return;
            }

            // if the item is already fully within the viewport vertically, disallow horizontal scrolling
            e.Handled = true;
        }

        #region Tree right click events

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFiles(false);
        }

        private void btnOpenFileCustomEditor_Click(object sender, RoutedEventArgs e)
        {
            OpenFiles(true);
        }

        private void btnOpenContainingFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolders();
        }

        private void treeKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (inputData.HasGrepLineSelection)
                {
                    CopyGrepLines();
                    e.Handled = true;
                }
                else if (inputData.HasGrepResultSelection)
                {
                    CopyFileNames(true);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.A && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                MultiSelectTreeView.SelectAll(treeView);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                ExcludeLines();
                e.Handled = true;
            }
            else if (e.Key == Key.F3 && Keyboard.Modifiers == ModifierKeys.None)
            {
                Next();
                e.Handled = true;
            }
            else if (e.Key == Key.F4 && Keyboard.Modifiers == ModifierKeys.None)
            {
                Previous();
                e.Handled = true;
            }
        }

        internal void SetFocus()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                treeView.Focus();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        internal async void Next()
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

        internal async void Previous()
        {
            try
            {
                Cursor = Cursors.Wait;
                await PreviousLineMatch();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private async Task NextLineMatch()
        {
            FormattedGrepResult selectedResult = inputData.SelectedNodes.OfType<FormattedGrepResult>()
                .Where(n => n != null)
                .LastOrDefault();
            FormattedGrepLine selectedLine = inputData.SelectedNodes.OfType<FormattedGrepLine>()
                .Where(n => n != null)
                .OrderBy(n => n.GrepLine.LineNumber)
                .LastOrDefault();

            if (selectedResult == null && selectedLine == null)
            {
                var firstResult = inputData.FirstOrDefault();
                if (firstResult != null)
                {
                    await SelectFirstChild(firstResult);
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
            else
            {
                await SelectFirstChild(selectedResult);
            }
        }

        private async Task PreviousLineMatch()
        {
            FormattedGrepResult selectedResult = inputData.SelectedNodes.OfType<FormattedGrepResult>()
                .Where(n => n != null)
                .FirstOrDefault();
            FormattedGrepLine selectedLine = inputData.SelectedNodes.OfType<FormattedGrepLine>()
                .Where(n => n != null)
                .OrderBy(n => n.GrepLine.LineNumber)
                .FirstOrDefault();

            if (selectedResult == null && selectedLine == null)
            {
                var lastResult = inputData.LastOrDefault();
                if (lastResult != null)
                {
                    await SelectLastChild(lastResult);
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
            else
            {
                await SelectPreviousResult(selectedResult);
            }
        }

        private void btnRenameFile_Click(object sender, RoutedEventArgs e)
        {
            FormattedGrepResult searchResult = null;
            var node = inputData.SelectedNodes.FirstOrDefault();

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
                            OnSelectedItemsChanged(this, e);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Rename failed: " + ex.Message, "Rename File",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void btnCopyFileNames_Click(object sender, RoutedEventArgs e)
        {
            CopyFileNames(false);
        }

        private void btnCopyFullFilePath_Click(object sender, RoutedEventArgs e)
        {
            CopyFileNames(true);
        }

        private void btnCopyGrepLine_Click(object sender, RoutedEventArgs e)
        {
            CopyGrepLines();
        }

        private void btnShowFileProperties_Click(object sender, RoutedEventArgs e)
        {
            ShowFileProperties();
        }

        private void btnExclude_Click(object sender, RoutedEventArgs e)
        {
            ExcludeLines();
        }

        private async void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FormattedGrepResult result in treeView.Items)
            {
                await result.ExpandTreeNode();
            }
        }

        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FormattedGrepResult result in treeView.Items)
            {
                result.CollapseTreeNode();
            }
        }

        private void OpenFiles(bool useCustomEditor)
        {
            // get the unique set of file names to open from the selections
            // keep the first record from each file to use when opening the file
            // prefer to open by line, if any line is selected; otherwise by file

            List<string> fileNames = new List<string>();
            List<FormattedGrepLine> lines = new List<FormattedGrepLine>();
            List<FormattedGrepResult> files = new List<FormattedGrepResult>();
            foreach (var item in inputData.SelectedItems)
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

            foreach (var item in inputData.SelectedItems)
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
                inputData.OpenFile(item, useCustomEditor);

            foreach (var item in files)
                inputData.OpenFile(item, useCustomEditor);
        }

        private void OpenFolders()
        {
            // get the unique set of folders from the selections
            // keep the first file from each folder to open the folder

            List<string> folders = new List<string>();
            List<string> files = new List<string>();
            foreach (var item in inputData.SelectedItems)
            {
                if (item is FormattedGrepResult fileNode)
                {
                    string name = fileNode.GrepResult.FileNameReal;
                    string path = Path.GetDirectoryName(name);
                    if (!folders.Contains(path))
                    {
                        folders.Add(path);
                        files.Add(name);
                    }
                }
                if (item is FormattedGrepLine lineNode)
                {
                    string name = lineNode.Parent.GrepResult.FileNameReal;
                    string path = Path.GetDirectoryName(name);
                    if (!folders.Contains(path))
                    {
                        folders.Add(path);
                        files.Add(name);
                    }
                }
            }

            foreach (var fileName in files)
                Utils.OpenContainingFolder(fileName);
        }

        private void ShowFileProperties()
        {
            // get the unique set of files from the selections
            List<string> files = new List<string>();
            foreach (var item in inputData.SelectedItems)
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

        private IList<string> GetSelectedFileNames(bool showFullName)
        {
            List<string> list = new List<string>();
            foreach (var item in inputData.SelectedItems)
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

        private void CopyFileNames(bool showFullName)
        {
            var list = GetSelectedFileNames(showFullName);
            if (list.Count > 0)
                Clipboard.SetText(string.Join(Environment.NewLine, list.ToArray()));
        }

        private string GetSelectedGrepLineText()
        {
            if (inputData.HasGrepLineSelection)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in inputData.SelectedItems)
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

        private void CopyGrepLines()
        {
            var lines = GetSelectedGrepLineText();
            if (!string.IsNullOrWhiteSpace(lines))
                Clipboard.SetText(lines);
        }

        private void ExcludeLines()
        {
            List<FormattedGrepResult> files = new List<FormattedGrepResult>();
            foreach (var item in inputData.SelectedItems)
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
                inputData.Remove(item);
        }

        private void treeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item)
            {
                item.Focus();
                e.Handled = true;
            }
        }

        private void treeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem is FormattedGrepLine && (e.OriginalSource is TextBlock || e.OriginalSource is Run))
            {
                inputData.OpenFile(treeView.SelectedItem as FormattedGrepLine, GrepSettings.Instance.IsSet(GrepSettings.Key.CustomEditor));
            }
        }

        private void OnSelectedItemsChanged(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);

            var rect = new System.Drawing.RectangleF { Height = (float)parentWindow.ActualHeight, Width = (float)parentWindow.ActualWidth, X = (float)parentWindow.Left, Y = (float)parentWindow.Top };

            if (treeView.GetValue(MultiSelectTreeView.SelectedItemsProperty) is IList items && items.Count > 0)
            {
                if (items[0] is FormattedGrepLine)
                    inputData.PreviewFile(items[0] as FormattedGrepLine, rect);
                else if (items[0] is FormattedGrepResult)
                    inputData.PreviewFile(items[0] as FormattedGrepResult, rect);
            }
        }

        #endregion

        #region Zoom

        private Dictionary<int, Point> touchIds = new Dictionary<int, Point>();

        private void treeView_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (sender is IInputElement ctrl && !touchIds.ContainsKey(e.TouchDevice.Id))
            {
                var pt = e.GetTouchPoint(ctrl).Position;
                touchIds.Add(e.TouchDevice.Id, pt);
            }
        }

        private void treeView_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (touchIds.ContainsKey(e.TouchDevice.Id))
                touchIds.Remove(e.TouchDevice.Id);
        }

        private void treeView_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            IInputElement ctrl = sender as IInputElement;

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

            if (inputData != null && inputData.Count > 0 &&
                ctrl != null && touchIds.ContainsKey(e.TouchDevice.Id) && touchIds.Count == 2)
            {
                var pNew = e.GetTouchPoint(ctrl).Position;

                var otherTouchId = touchIds.Keys.Where(k => k != e.TouchDevice.Id).FirstOrDefault();
                var p0 = touchIds[otherTouchId];
                var p1 = touchIds[e.TouchDevice.Id];

                var dx = p1.X - p0.X;
                var dy = p1.Y - p0.Y;
                var dist1 = dx * dx + dy * dy;

                dx = pNew.X - p0.X;
                dy = pNew.Y - p0.Y;
                var dist2 = dx * dx + dy * dy;

                if (Math.Abs(dist2 - dist1) > 200)
                {
                    if (dist1 < dist2 && inputData.ResultsScale < 2.0)
                    {
                        inputData.ResultsScale *= 1.005;
                    }

                    if (dist1 > dist2 && inputData.ResultsScale > 0.8)
                    {
                        inputData.ResultsScale /= 1.005;
                    }

                    e.Handled = true;
                }

                // and update position for this touch
                touchIds[e.TouchDevice.Id] = pNew;
            }
        }

        private void treeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            bool handle = (Keyboard.Modifiers & ModifierKeys.Control) > 0 &&
                inputData != null && inputData.Count > 0;

            if (!handle)
                return;

            if (e.Delta > 0 && inputData.ResultsScale < 2.0)
            {
                inputData.ResultsScale *= 1.05;
            }

            if (e.Delta < 0 && inputData.ResultsScale > 0.8)
            {
                inputData.ResultsScale /= 1.05;
            }

            e.Handled = true;
        }

        private void btnResetZoom_Click(object sender, RoutedEventArgs e)
        {
            if (inputData != null)
            {
                inputData.ResultsScale = 1.0;
            }
        }

        #endregion

        #region DragDropEvents
        private static UIElement _draggedElt;
        private static bool _isMouseDown = false;
        private static System.Windows.Point _dragStartPoint;

        private void treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Make this the new drag source
            _draggedElt = e.Source as UIElement;
            _dragStartPoint = e.GetPosition(getTopContainer());
            _isMouseDown = true;
        }

        private void treeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown && isDragGesture(e.GetPosition(getTopContainer())))
            {
                treeDragStarted(sender as UIElement);
            }
        }

        private void treeDragStarted(UIElement uiElt)
        {
            _isMouseDown = false;
            Mouse.Capture(uiElt);

            DataObject data = new DataObject();
            // set this data format to prevent dropping onto our own main window
            data.SetData(App.InstanceId, string.Empty);

            // if there are lines selected, choose text drag and drop operation
            var lines = GetSelectedGrepLineText();
            if (!string.IsNullOrWhiteSpace(lines))
            {
                data.SetData(DataFormats.Text, lines);
            }
            else if (inputData.HasGrepResultSelection)
            {
                var list = GetSelectedFileNames(true);
                StringCollection files = new StringCollection();
                files.AddRange(list.ToArray());
                data.SetFileDropList(files);
            }

            DragDropEffects supportedEffects = DragDropEffects.Move | DragDropEffects.Copy;
            // Perform DragDrop
            DragDropEffects effects = System.Windows.DragDrop.DoDragDrop(_draggedElt, data, supportedEffects);

            // Clean up
            Mouse.Capture(null);
            _draggedElt = null;
        }

        private bool isDragGesture(Point point)
        {
            bool hGesture = Math.Abs(point.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance;
            bool vGesture = Math.Abs(point.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;

            return (hGesture | vGesture);
        }

        private UIElement getTopContainer()
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
                var tvi = GetTreeViewItem(treeView, firstLine, null, SearchDirection.Down);
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                }
            }
        }

        private async Task SelectNextResult(FormattedGrepLine currentLine)
        {
            var grepResult = currentLine.Parent;
            int idx = inputData.IndexOf(grepResult) + 1;
            if (idx >= inputData.Count) idx = 0;

            var nextResult = inputData[idx];
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
                var tvi = GetTreeViewItem(container, nextLine, currentLine, SearchDirection.Down);
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
                var tvi = GetTreeViewItem(treeView, lastLine, null, SearchDirection.Up);
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                }
            }
        }

        private async Task SelectPreviousResult(FormattedGrepResult result)
        {
            int idx = inputData.IndexOf(result) - 1;
            if (idx < 0) idx = inputData.Count - 1;

            var previousResult = inputData[idx];
            await SelectLastChild(previousResult);
        }

        private async Task SelectPreviousResult(FormattedGrepLine currentLine)
        {
            var grepResult = currentLine.Parent;
            int idx = inputData.IndexOf(grepResult) - 1;
            if (idx < 0) idx = inputData.Count - 1;

            var previousResult = inputData[idx];
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
                var tvi = GetTreeViewItem(container, previousLine, currentLine, SearchDirection.Up);
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
        private static TreeViewItem GetTreeViewItem(ItemsControl container, object item, object selectedItem, SearchDirection dir)
        {
            if (container != null)
            {
                if (container.DataContext == item)
                {
                    return container as TreeViewItem;
                }

                // Expand the current container
                if (container is TreeViewItem && !((TreeViewItem)container).IsExpanded)
                {
                    container.SetValue(TreeViewItem.IsExpandedProperty, true);
                }

                // Try to generate the ItemsPresenter and the ItemsPanel.
                // by calling ApplyTemplate.  Note that in the
                // virtualizing case even if the item is marked
                // expanded we still need to do this step in order to
                // regenerate the visuals because they may have been virtualized away.

                container.ApplyTemplate();
                ItemsPresenter itemsPresenter =
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
                        if (subContainer != null)
                        {
                            // Bring the item into view to maintain the
                            // same behavior as with a virtualizing panel.
                            subContainer.BringIntoView();
                        }
                    }

                    if (subContainer != null)
                    {
                        // Search the next level for the object.
                        TreeViewItem resultContainer = GetTreeViewItem(subContainer, item, selectedItem, dir);
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
        private static T FindVisualChild<T>(Visual visual) where T : Visual
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

                    T descendent = FindVisualChild<T>(child);
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

        private static TreeViewItem GetTreeViewItemParent(TreeView treeView, object item)
        {
            TreeViewItem treeViewItem = ContainerFromItemRecursive(treeView.ItemContainerGenerator, item);
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

        private static TreeViewItem ContainerFromItemRecursive(ItemContainerGenerator root, object item)
        {
            if (root.ContainerFromItem(item) is TreeViewItem treeViewItem)
            {
                return treeViewItem;
            }

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
