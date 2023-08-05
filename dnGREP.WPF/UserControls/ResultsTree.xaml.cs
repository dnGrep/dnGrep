using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using dnGREP.Common;
using dnGREP.Common.UI;
using Microsoft.VisualBasic.FileIO;

namespace dnGREP.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for ResultsTree.xaml
    /// </summary>
    public partial class ResultsTree : UserControl
    {
        private enum SearchDirection { Down = 0, Up };
        private GrepSearchResultsViewModel viewModel = new();

        public ResultsTree()
        {
            InitializeComponent();
            DataContextChanged += ResultsTree_DataContextChanged;

            treeView.PreviewMouseWheel += TreeView_PreviewMouseWheel;
            treeView.PreviewTouchDown += TreeView_PreviewTouchDown;
            treeView.PreviewTouchMove += TreeView_PreviewTouchMove;
            treeView.PreviewTouchUp += TreeView_PreviewTouchUp;
        }

        void ResultsTree_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (viewModel != null && viewModel.SelectedNodes != null)
            {
                viewModel.SelectedNodes.CollectionChanged -= SelectedNodes_CollectionChanged;
            }

            viewModel = (GrepSearchResultsViewModel)DataContext;
            viewModel.SelectedNodes.CollectionChanged += SelectedNodes_CollectionChanged;
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

        private void TreeView_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // keep tree view from scrolling horizontally when an item is (mouse) selected
            if (sender is TreeViewItem treeViewItem &&
                treeView.Template.FindName("_tv_scrollviewer_", treeView) is ScrollViewer scrollViewer)
            {
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
        }

        #region Tree right click events

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFiles(false);
        }

        private void BtnOpenFileCustomEditor_Click(object sender, RoutedEventArgs e)
        {
            OpenFiles(true);
        }

        private void BtnOpenContainingFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolders();
        }

        private void BtnOpenExplorerMenu_Click(object sender, RoutedEventArgs e)
        {
            OpenExplorerMenu();
        }

        private void TreeKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (viewModel.HasGrepLineSelection)
                {
                    CopyGrepLines();
                    e.Handled = true;
                }
                else if (viewModel.HasGrepResultSelection)
                {
                    CopyFileNames(true);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.A && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Home && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                SelectToStart();
            }
            else if (e.Key == Key.End && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                SelectToEnd();
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
            else if (e.Key == Key.F3 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                NextFile();
                e.Handled = true;
            }
            else if (e.Key == Key.F4 && Keyboard.Modifiers == ModifierKeys.None)
            {
                Previous();
                e.Handled = true;
            }
            else if (e.Key == Key.F4 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                PreviousFile();
                e.Handled = true;
            }
            else if (e.Key == Key.F6 && Keyboard.Modifiers == ModifierKeys.None)
            {
                BtnExpandAll_Click(this, e);
                e.Handled = true;
            }
            else if (e.Key == Key.F6 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                BtnCollapseAll_Click(this, e);
                e.Handled = true;
            }
        }

        private void SelectAll()
        {
            treeView.DeselectAllChildItems();

            foreach (var item in viewModel.SearchResults)
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
            var startTreeViewItem = treeView.StartTreeViewItem;
            if (startTreeViewItem != null && startTreeViewItem.DataContext is ITreeItem startItem)
            {
                treeView.DeselectAllChildItems();

                if (startItem is FormattedGrepLine line)
                {
                    startItem = line.Parent;
                    if (!startItem.IsSelected)
                        startItem.IsSelected = true;
                }

                bool isSelecting = false;
                foreach (var item in viewModel.SearchResults.Reverse())
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
            var startTreeViewItem = treeView.StartTreeViewItem;
            if (startTreeViewItem != null && startTreeViewItem.DataContext is ITreeItem startItem)
            {
                treeView.DeselectAllChildItems();

                if (startItem is FormattedGrepLine line)
                {
                    startItem = line.Parent;
                    if (!startItem.IsSelected)
                        startItem.IsSelected = true;
                }

                bool isSelecting = false;
                foreach (var item in viewModel.SearchResults)
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

        internal void SetFocus()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                treeView.Focus();
            }), System.Windows.Threading.DispatcherPriority.Normal);
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

        internal async void NextFile()
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

        internal async void PreviousFile()
        {
            try
            {
                Cursor = Cursors.Wait;
                await PreviousFileMatch();
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private async Task NextLineMatch()
        {
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
            else if (selectedResult != null)
            {
                await SelectFirstChild(selectedResult);
            }
        }

        private async Task NextFileMatch()
        {
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
                if (firstResult != null)
                {
                    await SelectFirstChild(firstResult);
                }
            }
            else if (selectedLine != null)
            {
                await SelectNextResult(selectedLine);
            }
            else if (selectedResult != null)
            {
                await SelectNextResult(selectedResult.FormattedLines.First());
            }
        }

        private async Task PreviousLineMatch()
        {
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
            else if (selectedResult != null)
            {
                await SelectPreviousResult(selectedResult);
            }
        }

        private async Task PreviousFileMatch()
        {
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
                if (lastResult != null)
                {
                    await SelectLastChild(lastResult);
                }
            }
            else if (selectedLine != null)
            {
                await SelectPreviousResult(selectedLine);
            }
            else if (selectedResult != null)
            {
                await SelectPreviousResult(selectedResult);
            }
        }

        private void BtnRenameFile_Click(object sender, RoutedEventArgs e)
        {
            FormattedGrepResult? searchResult = null;
            var node = viewModel.SelectedNodes.FirstOrDefault();

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
                            OnSelectedItemsChanged();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(Localization.Properties.Resources.MessageBox_RenameFailed + ex.Message,
                                Localization.Properties.Resources.MessageBox_DnGrep + " " + Localization.Properties.Resources.MessageBox_RenameFile,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, Localization.TranslationSource.Instance.FlowDirection);
                        }
                    }
                }
            }
        }

        private void BtnCopyFiles_Click(object sender, RoutedEventArgs e)
        {
            var (files, indexOfFirst) = GetSelectedFiles();
            if (files.Count > 0)
            {
                var fileList = files.Select(f => f.GrepResult).ToList();
                var (success, message) = FileOperations.CopyFiles(
                    fileList, viewModel.PathSearchText, null, false);
            }
        }

        private void BtnMoveFiles_Click(object sender, RoutedEventArgs e)
        {
            var (files, indexOfFirst) = GetSelectedFiles();
            if (files.Count > 0)
            {
                var fileList = files.Select(f => f.GrepResult).ToList();
                var (success, message) = FileOperations.MoveFiles(
                    fileList, viewModel.PathSearchText, null, false);

                if (success)
                {
                    viewModel.DeselectAllItems();
                    foreach (var gr in files)
                    {
                        viewModel.SearchResults.Remove(gr);
                    }

                    if (indexOfFirst > -1 && viewModel.SearchResults.Count > 0)
                    {
                        // the first item was removed, select the new item in that position
                        int idx = indexOfFirst;
                        if (idx >= viewModel.SearchResults.Count) idx = viewModel.SearchResults.Count - 1;

                        var nextResult = viewModel.SearchResults[idx];
                        var tvi = GetTreeViewItem(treeView, nextResult, null, SearchDirection.Down, 1);
                        if (tvi != null)
                        {
                            tvi.IsSelected = false;
                            tvi.IsSelected = true;
                        }
                    }
                }
            }
        }

        private void BtnDeleteFiles_Click(object sender, RoutedEventArgs e)
        {
            var (files, indexOfFirst) = GetSelectedFiles();
            if (files.Count > 0)
            {
                var fileList = files.Select(f => f.GrepResult).ToList();
                var (success, message) = FileOperations.DeleteFiles(
                    fileList, false, false);

                if (success)
                {
                    viewModel.DeselectAllItems();
                    foreach (var gr in files)
                    {
                        viewModel.SearchResults.Remove(gr);
                    }

                    if (indexOfFirst > -1 && viewModel.SearchResults.Count > 0)
                    {
                        // the first item was removed, select the new item in that position
                        int idx = indexOfFirst;
                        if (idx >= viewModel.SearchResults.Count) idx = viewModel.SearchResults.Count - 1;

                        var nextResult = viewModel.SearchResults[idx];
                        var tvi = GetTreeViewItem(treeView, nextResult, null, SearchDirection.Down, 1);
                        if (tvi != null)
                        {
                            tvi.IsSelected = false;
                            tvi.IsSelected = true;
                        }
                    }
                }
            }
        }

        private void BtnRecycleFiles_Click(object sender, RoutedEventArgs e)
        {
            var (files, indexOfFirst) = GetSelectedFiles();
            viewModel.DeselectAllItems();
            foreach (var gr in files)
            {
                FileSystem.DeleteFile(gr.GrepResult.FileNameReal,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);


                viewModel.SearchResults.Remove(gr);
            }

            if (indexOfFirst > -1 && viewModel.SearchResults.Count > 0)
            {
                // the first item was removed, select the new item in that position
                int idx = indexOfFirst;
                if (idx >= viewModel.SearchResults.Count) idx = viewModel.SearchResults.Count - 1;

                var nextResult = viewModel.SearchResults[idx];
                var tvi = GetTreeViewItem(treeView, nextResult, null, SearchDirection.Down, 1);
                if (tvi != null)
                {
                    tvi.IsSelected = false;
                    tvi.IsSelected = true;
                }
            }
        }

        private (IList<FormattedGrepResult> files, int indexOfFirst) GetSelectedFiles()
        {
            // get the unique set of files from the selections
            List<FormattedGrepResult> files = new();
            int indexOfFirst = -1;
            foreach (var item in viewModel.SelectedItems)
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
                    indexOfFirst = viewModel.SearchResults.IndexOf(files.First());
                }
            }
            return (files, indexOfFirst);
        }

        private void BtnCopyFileNames_Click(object sender, RoutedEventArgs e)
        {
            CopyFileNames(false);
        }

        private void BtnCopyFullFilePath_Click(object sender, RoutedEventArgs e)
        {
            CopyFileNames(true);
        }

        private void BtnCopyGrepLine_Click(object sender, RoutedEventArgs e)
        {
            CopyGrepLines();
        }

        private void BtnShowFileProperties_Click(object sender, RoutedEventArgs e)
        {
            ShowFileProperties();
        }

        private void BtnMakeWritable_Click(object sender, RoutedEventArgs e)
        {
            MakeFilesWritable();
        }

        private void BtnExclude_Click(object sender, RoutedEventArgs e)
        {
            ExcludeLines();
        }

        private void BtnNextMatch_Click(object sender, RoutedEventArgs e)
        {
            Next();
        }

        private void BtnNextFile_Click(object sender, RoutedEventArgs e)
        {
            NextFile();
        }

        private void BtnPreviousMatch_Click(object sender, RoutedEventArgs e)
        {
            Previous();
        }

        private void BtnPreviousFile_Click(object sender, RoutedEventArgs e)
        {
            PreviousFile();
        }

        private async void BtnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FormattedGrepResult result in treeView.Items)
            {
                await result.ExpandTreeNode();
            }
        }

        private void BtnCollapseAll_Click(object sender, RoutedEventArgs e)
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

            List<string> fileNames = new();
            List<FormattedGrepLine> lines = new();
            List<FormattedGrepResult> files = new();
            foreach (var item in viewModel.SelectedItems)
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

            foreach (var item in viewModel.SelectedItems)
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
                viewModel.OpenFile(item, useCustomEditor);

            foreach (var item in files)
                viewModel.OpenFile(item, useCustomEditor);
        }

        private void OpenFolders()
        {
            // get the unique set of folders from the selections
            // keep the first file from each folder to open the folder

            List<string> folders = new();
            List<string> files = new();
            foreach (var item in viewModel.SelectedItems)
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
            List<string> files = new();
            foreach (var item in viewModel.SelectedItems)
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

            if (files.Count > 0)
            {
                ShellContextMenu menu = new();
                menu.ShowContextMenu(files.Select(f => new FileInfo(f)).ToArray(),
                    PointToScreen(Mouse.GetPosition(this)));
            }
        }

        private void ShowFileProperties()
        {
            // get the unique set of files from the selections
            List<string> files = new();
            foreach (var item in viewModel.SelectedItems)
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
            List<string> list = new();
            foreach (var item in viewModel.SelectedItems)
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
                NativeMethods.SetClipboardText(string.Join(Environment.NewLine, list.ToArray()));
        }

        private string GetSelectedGrepLineText()
        {
            if (viewModel.HasGrepLineSelection)
            {
                StringBuilder sb = new();
                foreach (var item in viewModel.SelectedItems)
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
                NativeMethods.SetClipboardText(lines);
        }

        private void MakeFilesWritable()
        {
            List<FormattedGrepResult> files = new();
            foreach (var item in viewModel.SelectedItems)
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

        private void ExcludeLines()
        {
            List<FormattedGrepResult> files = new();
            int indexOfFirst = -1;
            foreach (var item in viewModel.SelectedItems)
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
                    indexOfFirst = viewModel.SearchResults.IndexOf(files.First());
                }
            }

            viewModel.DeselectAllItems();

            foreach (var item in files)
            {
                viewModel.SearchResults.Remove(item);
            }

            if (indexOfFirst > -1 && viewModel.SearchResults.Count > 0)
            {
                // the first item was removed, select the new item in that position
                int idx = indexOfFirst;
                if (idx >= viewModel.SearchResults.Count) idx = viewModel.SearchResults.Count - 1;

                var nextResult = viewModel.SearchResults[idx];
                var tvi = GetTreeViewItem(treeView, nextResult, null, SearchDirection.Down, 1);
                if (tvi != null)
                {
                    tvi.IsSelected = false;
                    tvi.IsSelected = true;
                }
            }
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
            // middle button click on a file node or line node opens file with custom editor
            if (e.ChangedButton == MouseButton.Middle)
            {
                if (treeView.SelectedItem is FormattedGrepResult file)
                {
                    viewModel.OpenFile(file, GrepSettings.Instance.IsSet(GrepSettings.Key.CustomEditor));
                }
                else if (treeView.SelectedItem is FormattedGrepLine line)
                {
                    viewModel.OpenFile(line, GrepSettings.Instance.IsSet(GrepSettings.Key.CustomEditor));
                }
                e.Handled = true;
            }
        }

        private void TreeView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // alt+double click on a file node or line node opens file with associated application
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (treeView.SelectedItem is FormattedGrepResult fileNode)
                {
                    viewModel.OpenFile(fileNode, false);
                }
                else if (treeView.SelectedItem is FormattedGrepLine line)
                {
                    viewModel.OpenFile(line, false);
                }
                e.Handled = true;
            }
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // double click on a line node opens file
            if (treeView.SelectedItem is FormattedGrepLine line && 
                (e.OriginalSource is TextBlock || e.OriginalSource is Run))
            {
                bool useCustomEditor = !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) &&
                    GrepSettings.Instance.IsSet(GrepSettings.Key.CustomEditor);
                viewModel.OpenFile(line, useCustomEditor);
                e.Handled = true;
            }
        }

        private void OnSelectedItemsChanged()
        {
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

        private readonly Dictionary<int, Point> touchIds = new();

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
                    touchIds.ContainsKey(e.TouchDevice.Id) && touchIds.Count == 2)
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

        private void BtnResetZoom_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.ResultsScale = 1.0;
            }
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
            var lines = GetSelectedGrepLineText();
            if (!string.IsNullOrWhiteSpace(lines))
            {
                data.SetData(DataFormats.Text, lines);
            }
            else if (viewModel.HasGrepResultSelection)
            {
                var list = GetSelectedFileNames(true);
                StringCollection files = new();
                files.AddRange(list.ToArray());
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

        private async Task SelectNextResult(FormattedGrepLine currentLine)
        {
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

        private async Task SelectPreviousResult(FormattedGrepResult result)
        {
            int idx = viewModel.SearchResults.IndexOf(result) - 1;
            if (idx < 0) idx = viewModel.SearchResults.Count - 1;

            var previousResult = viewModel.SearchResults[idx];
            await SelectLastChild(previousResult);
        }

        private async Task SelectPreviousResult(FormattedGrepLine currentLine)
        {
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
        private static TreeViewItem? GetTreeViewItem(ItemsControl container, object item, object? selectedItem, SearchDirection dir, int depth)
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

        //private static TreeViewItem ContainerFromItem(ItemContainerGenerator root, object item)
        //{
        //    if (root.ContainerFromItem(item) is TreeViewItem treeViewItem)
        //    {
        //        return treeViewItem;
        //    }

        //    return null;
        //}

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
