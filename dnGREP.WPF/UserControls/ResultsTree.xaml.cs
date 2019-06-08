using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;

namespace dnGREP.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for ResultsTree.xaml
    /// </summary>
    public partial class ResultsTree : UserControl
    {
        ObservableGrepSearchResults inputData = new ObservableGrepSearchResults();

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
            // keep tree view from scrolling horizontally when an item is selected
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

        private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FormattedGrepResult result in treeView.Items)
            {
                result.IsExpanded = true;
            }
        }

        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FormattedGrepResult result in treeView.Items)
            {
                result.IsExpanded = false;
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

                    if (dist1 < dist2 && inputData.ResultsMenuScale < 1.5)
                    {
                        inputData.ResultsMenuScale *= 1.0025;
                    }

                    if (dist1 > dist2 && inputData.ResultsMenuScale > 1.0)
                    {
                        inputData.ResultsMenuScale /= 1.0025;
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

            if (e.Delta > 0 && inputData.ResultsMenuScale < 1.5)
            {
                inputData.ResultsMenuScale *= 1.025;
            }

            if (e.Delta < 0 && inputData.ResultsMenuScale > 1.0)
            {
                inputData.ResultsMenuScale /= 1.025;
            }

            e.Handled = true;
        }

        private void btnResetZoom_Click(object sender, RoutedEventArgs e)
        {
            if (inputData != null)
            {
                inputData.ResultsScale = 1.0;
                inputData.ResultsMenuScale = 1.0;
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

    }
}
