using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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

            tvSearchResult.PreviewMouseWheel += TvSearchResult_PreviewMouseWheel;
            tvSearchResult.PreviewTouchDown += TvSearchResult_PreviewTouchDown;
            tvSearchResult.PreviewTouchMove += TvSearchResult_PreviewTouchMove;
            tvSearchResult.PreviewTouchUp += TvSearchResult_PreviewTouchUp;
        }

        void ResultsTree_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            inputData = ((ObservableGrepSearchResults)(this.DataContext));
        }

        private void tvSearchResult_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // keep tree view from scrolling horizontally when an item is selected
            e.Handled = true;
        }

        #region Tree right click events

        private void tvContexMenuOpening(object sender, RoutedEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                btnCopyTreeItemClipboard.Header = "Line of text to clipboard";
                btnCopyFileNameClipboard.Visibility = Visibility.Collapsed;
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                btnCopyTreeItemClipboard.Header = "Full file path to clipboard";
                btnCopyFileNameClipboard.Visibility = Visibility.Visible;
            }
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
                inputData.OpenFile(tvSearchResult.SelectedItem as FormattedGrepLine, false);
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
                inputData.OpenFile(tvSearchResult.SelectedItem as FormattedGrepResult, false);
        }

        private void btnOpenFileCustomEditor_Click(object sender, RoutedEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
                inputData.OpenFile(tvSearchResult.SelectedItem as FormattedGrepLine, true);
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
                inputData.OpenFile(tvSearchResult.SelectedItem as FormattedGrepResult, true);
        }

        private void btnOpenContainingFolder_Click(object sender, RoutedEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                //ShellIntegration.OpenFolder(selectedNode.Parent.GrepResult.FileNameReal);
                Utils.OpenContainingFolder(selectedNode.Parent.GrepResult.FileNameReal, selectedNode.GrepLine.LineNumber);
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                FormattedGrepResult selectedNode = (FormattedGrepResult)tvSearchResult.SelectedItem;
                //ShellIntegration.OpenFolder(selectedNode.GrepResult.FileNameReal);
                Utils.OpenContainingFolder(selectedNode.GrepResult.FileNameReal, -1);
            }
        }

        private void btnShowFileProperties_Click(object sender, RoutedEventArgs e)
        {
            string fileName = "";
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                fileName = selectedNode.Parent.GrepResult.FileNameReal;
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                FormattedGrepResult selectedNode = (FormattedGrepResult)tvSearchResult.SelectedItem;
                fileName = selectedNode.GrepResult.FileNameReal;
            }

            if (fileName != "" && File.Exists(fileName))
                ShellIntegration.ShowFileProperties(fileName);
        }

        private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FormattedGrepResult result in tvSearchResult.Items)
            {
                result.IsExpanded = true;
            }
        }

        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FormattedGrepResult result in tvSearchResult.Items)
            {
                result.IsExpanded = false;
            }
        }

        private void btnExclude_Click(object sender, RoutedEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                inputData.Remove(selectedNode.Parent);
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                FormattedGrepResult selectedNode = (FormattedGrepResult)tvSearchResult.SelectedItem;
                inputData.Remove(selectedNode);
            }
        }

        private void copyToClipboard()
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                Clipboard.SetText(selectedNode.GrepLine.LineText);
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
                Clipboard.SetText(result.GrepResult.FileNameDisplayed);
            }
        }

        private void treeKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                copyToClipboard();
            }
        }

        private void btnCopyTreeItemToClipboard_Click(object sender, RoutedEventArgs e)
        {
            copyToClipboard();
        }

        private void btnCopyNameToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                Clipboard.SetText(System.IO.Path.GetFileName(selectedNode.Parent.GrepResult.FileNameDisplayed));
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
                Clipboard.SetText(System.IO.Path.GetFileName(result.GrepResult.FileNameDisplayed));
            }
        }

        private void tvSearchResult_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null)
            {
                item.Focus();
                e.Handled = true;
            }
        }

        private void tvSearchResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (tvSearchResult.SelectedItem is FormattedGrepLine &&
                e.OriginalSource is TextBlock || e.OriginalSource is Run)
            {
                inputData.OpenFile(tvSearchResult.SelectedItem as FormattedGrepLine, GrepSettings.Instance.IsSet(GrepSettings.Key.CustomEditor));
            }
        }

        private void OnSelectedItemsChanged(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);

            var rect = new System.Drawing.RectangleF { Height = (float)parentWindow.ActualHeight, Width = (float)parentWindow.ActualWidth, X = (float)parentWindow.Left, Y = (float)parentWindow.Top };

            var items = tvSearchResult.GetValue(MultiSelectTreeView.SelectedItemsProperty) as IList;
            if (items != null && items.Count > 0)
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

        private void TvSearchResult_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            IInputElement ctrl = sender as IInputElement;
            if (ctrl != null && !touchIds.ContainsKey(e.TouchDevice.Id))
            {
                var pt = e.GetTouchPoint(ctrl).Position;
                touchIds.Add(e.TouchDevice.Id, pt);
            }
        }

        private void TvSearchResult_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (touchIds.ContainsKey(e.TouchDevice.Id))
                touchIds.Remove(e.TouchDevice.Id);
        }

        private void TvSearchResult_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            IInputElement ctrl = sender as IInputElement;

            // sometimes a PreviewTouchUp event is lost when the user is on the scrollbar or edge of the window
            // if our captured touches do not match the scrollviewer, resynch to the scrollviewer
            ScrollViewer scrollViewer = e.OriginalSource as ScrollViewer;
            if (scrollViewer != null)
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

        private void TvSearchResult_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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

        private void tvSearchResult_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Make this the new drag source
            _draggedElt = e.Source as UIElement;
            _dragStartPoint = e.GetPosition(getTopContainer());
            _isMouseDown = true;
        }

        private void tvSearchResult_PreviewMouseMove(object sender, MouseEventArgs e)
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

            if (tvSearchResult.SelectedItem is FormattedGrepLine)
            {
                FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
                data.SetData(DataFormats.Text, selectedNode.GrepLine.LineText);
            }
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
            {
                FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
                StringCollection files = new StringCollection();
                files.Add(result.GrepResult.FileNameReal);
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
