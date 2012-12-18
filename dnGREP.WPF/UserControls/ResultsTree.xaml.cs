using dnGREP.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        }

        void ResultsTree_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            inputData = ((ObservableGrepSearchResults)(this.DataContext));
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

        private void tvSearchResults_SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Window parentWindow = Window.GetWindow(this);

            var rect = new System.Drawing.RectangleF { Height = (float)parentWindow.ActualHeight, Width = (float)parentWindow.ActualWidth, X = (float)parentWindow.Left, Y = (float)parentWindow.Top };
            if (tvSearchResult.SelectedItem is FormattedGrepLine)
                inputData.PreviewFile(tvSearchResult.SelectedItem as FormattedGrepLine, rect);
            else if (tvSearchResult.SelectedItem is FormattedGrepResult)
                inputData.PreviewFile(tvSearchResult.SelectedItem as FormattedGrepResult, rect);
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
