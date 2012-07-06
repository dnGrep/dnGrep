using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using dnGREP.Common;
using dnGREP.Engines;
using NLog;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using dnGREP.Common.UI;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Blue.Windows;

namespace dnGREP.WPF
{
    /// <summary>
	/// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class MainFormEx : Window
    {
		private static Logger logger = LogManager.GetCurrentClassLogger();
        private MainViewModel inputData;
        private bool isVisible = true;

        public MainFormEx()
            : this (true)
        {            
        }

        public MainFormEx(bool isVisible)
        {
            InitializeComponent();
            this.Width = Properties.Settings.Default.Width;
            this.Height = Properties.Settings.Default.Height;
            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            if (!UiUtils.IsOnScreen(this))
                UiUtils.CenterWindow(this);
            this.isVisible = isVisible;
            inputData = new MainViewModel();
            this.DataContext = inputData;
        }

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwndNewParent);

        private const int HWND_MESSAGE = -3;

        private IntPtr hwnd;
        private IntPtr oldParent;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (!isVisible)
            {
                HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;

                if (hwndSource != null)
                {
                    hwnd = hwndSource.Handle;
                    oldParent = SetParent(hwnd, (IntPtr)HWND_MESSAGE);
                    Visibility = Visibility.Hidden;
                }
            }
        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            inputData.StickyWindow = new StickyWindow(this);
            inputData.StickyWindow.StickToScreen = true;
            inputData.StickyWindow.StickToOther = true;
            inputData.StickyWindow.StickOnResize = true;
            inputData.StickyWindow.StickOnMove = true;
            //gridMain.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Auto);
            //gridMain.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
		}		

		private void MainForm_Closing(object sender, CancelEventArgs e)
		{
			Properties.Settings.Default.Width = (int)this.ActualWidth;
            Properties.Settings.Default.Height = (int)this.ActualHeight;
            Properties.Settings.Default.Top = (int)this.Top;
            Properties.Settings.Default.Left = (int)this.Left;
            Properties.Settings.Default.Save();
            inputData.CloseCommand.Execute(null);
		}
        
		#region Tree right click events

        //private void tvContexMenuOpening(object sender, RoutedEventArgs e)
        //{
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine)
        //    {
        //        btnCopyTreeItemClipboard.Header = "Line of text to clipboard";
        //        btnCopyFileNameClipboard.Visibility = Visibility.Collapsed;
        //    }
        //    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
        //    {
        //        btnCopyTreeItemClipboard.Header = "Full file path to clipboard";
        //        btnCopyFileNameClipboard.Visibility = Visibility.Visible;
        //    }
        //}

        //private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        //{
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine)
        //        inputData.OpenFile(tvSearchResult.SelectedItem as FormattedGrepLine);
        //    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
        //        inputData.OpenFile(tvSearchResult.SelectedItem as FormattedGrepResult);
        //}

        //private void btnOpenContainingFolder_Click(object sender, RoutedEventArgs e)
        //{
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine)
        //    {
        //        FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
        //        //ShellIntegration.OpenFolder(selectedNode.Parent.GrepResult.FileNameReal);
        //        Utils.OpenContainingFolder(selectedNode.Parent.GrepResult.FileNameReal, selectedNode.GrepLine.LineNumber);				
        //    }
        //    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
        //    {
        //        FormattedGrepResult selectedNode = (FormattedGrepResult)tvSearchResult.SelectedItem;
        //        //ShellIntegration.OpenFolder(selectedNode.GrepResult.FileNameReal);
        //        Utils.OpenContainingFolder(selectedNode.GrepResult.FileNameReal, -1);
        //    }
        //}

        //private void btnShowFileProperties_Click(object sender, RoutedEventArgs e)
        //{
        //    string fileName = "";
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine)
        //    {
        //        FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
        //        fileName = selectedNode.Parent.GrepResult.FileNameReal;
        //    }
        //    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
        //    {
        //        FormattedGrepResult selectedNode = (FormattedGrepResult)tvSearchResult.SelectedItem;
        //        fileName = selectedNode.GrepResult.FileNameReal;
        //    }

        //    if (fileName != "" && File.Exists(fileName))
        //        ShellIntegration.ShowFileProperties(fileName);
        //}

        //private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        //{
        //    foreach (FormattedGrepResult result in tvSearchResult.Items)
        //    {
        //        result.IsExpanded = true;
        //    }
        //}

        //private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        //{
        //    foreach (FormattedGrepResult result in tvSearchResult.Items)
        //    {
        //        result.IsExpanded = false;
        //    }
        //}

        //private void btnExclude_Click(object sender, RoutedEventArgs e)
        //{
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine)
        //    {
        //        FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
        //        inputData.SearchResults.Remove(selectedNode.Parent);
        //    }
        //    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
        //    {
        //        FormattedGrepResult selectedNode = (FormattedGrepResult)tvSearchResult.SelectedItem;
        //        inputData.SearchResults.Remove(selectedNode);
        //    }
        //}

        //private void copyToClipboard()
        //{
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine)
        //    {
        //        FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
        //        Clipboard.SetText(selectedNode.GrepLine.LineText);
        //    }
        //    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
        //    {
        //        FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
        //        Clipboard.SetText(result.GrepResult.FileNameDisplayed);
        //    }
        //}

        //private void treeKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        //    {
        //        copyToClipboard();
        //    }
        //}

        //private void btnCopyTreeItemToClipboard_Click(object sender, RoutedEventArgs e)
        //{
        //    copyToClipboard();
        //}

        //private void btnCopyNameToClipboard_Click(object sender, RoutedEventArgs e)
        //{			
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine)
        //    {
        //        FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
        //        Clipboard.SetText(System.IO.Path.GetFileName(selectedNode.Parent.GrepResult.FileNameDisplayed));
        //    }
        //    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
        //    {
        //        FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
        //        Clipboard.SetText(System.IO.Path.GetFileName(result.GrepResult.FileNameDisplayed));
        //    }
        //}

        //private void tvSearchResult_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    TreeViewItem item = sender as TreeViewItem;
        //    if (item != null)
        //    {
        //        item.Focus();
        //        e.Handled = true;
        //    }
        //}

        //private void tvSearchResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine &&
        //        e.OriginalSource is TextBlock || e.OriginalSource is Run)
        //    {
        //        inputData.OpenFile(tvSearchResult.SelectedItem as FormattedGrepLine);
        //    }
        //}

        //private void tvSearchResults_SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    var rect = new System.Drawing.RectangleF { Height = (float)this.ActualHeight, Width = (float)this.ActualWidth, X = (float)this.Left, Y = (float)this.Top };
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine)
        //        inputData.PreviewFile(tvSearchResult.SelectedItem as FormattedGrepLine, rect);
        //    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
        //        inputData.PreviewFile(tvSearchResult.SelectedItem as FormattedGrepResult, rect);
        //}

		#endregion

        #region UI fixes
        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source is TextBox)
            {
                ((TextBox)e.Source).SelectAll();
            }
        }

        private void btnSearchFastBookmarks_Click(object sender, RoutedEventArgs e)
        {
            cbSearchFastBookmark.IsDropDownOpen = true;
            cbSearchFastBookmark.Focus();
        }

        private void btnReplaceFastBookmarks_Click(object sender, RoutedEventArgs e)
        {
            cbReplaceFastBookmark.IsDropDownOpen = true;
            cbReplaceFastBookmark.Focus();
            tbReplaceWith.SelectAll();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            inputData.ActivatePreviewWindow();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            inputData.ChangePreviewWindowState(this.WindowState);
        }

        private void tbPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (!inputData.Multiline)
                {
                    if (sender != null && sender == tbSearchFor)
                    {
                        if (e.Key == Key.Down)
                            cbSearchFastBookmark.SelectedIndex++;
                        else
                        {
                            if (cbSearchFastBookmark.SelectedIndex > 0)
                                cbSearchFastBookmark.SelectedIndex--;
                        }
                    }
                    else if (sender != null && sender == tbReplaceWith)
                    {
                        if (e.Key == Key.Down)
                            cbReplaceFastBookmark.SelectedIndex++;
                        else
                        {
                            if (cbReplaceFastBookmark.SelectedIndex > 0)
                                cbReplaceFastBookmark.SelectedIndex--;
                        }
                    }
                }
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

        //private void tvSearchResult_PreviewMouseMove(object sender, MouseEventArgs e)
        //{
        //    if (_isMouseDown && isDragGesture(e.GetPosition(getTopContainer())))
        //    {
        //        treeDragStarted(sender as UIElement);
        //    }
        //}

        //private void treeDragStarted(UIElement uiElt)
        //{
        //    _isMouseDown = false;
        //    Mouse.Capture(uiElt);

        //    DataObject data = new DataObject();
			
        //    if (tvSearchResult.SelectedItem is FormattedGrepLine)
        //    {
        //        FormattedGrepLine selectedNode = (FormattedGrepLine)tvSearchResult.SelectedItem;
        //        data.SetData(DataFormats.Text, selectedNode.GrepLine.LineText);
        //    }
        //    else if (tvSearchResult.SelectedItem is FormattedGrepResult)
        //    {
        //        FormattedGrepResult result = (FormattedGrepResult)tvSearchResult.SelectedItem;
        //        StringCollection files = new StringCollection();
        //        files.Add(result.GrepResult.FileNameReal);
        //        data.SetFileDropList(files);
        //    }


        //    DragDropEffects supportedEffects = DragDropEffects.Move | DragDropEffects.Copy;
        //    // Perform DragDrop
        //    DragDropEffects effects = System.Windows.DragDrop.DoDragDrop(_draggedElt, data, supportedEffects);

        //    // Clean up
        //    Mouse.Capture(null);
        //    _draggedElt = null;
        //}

        //private bool isDragGesture(Point point)
        //{
        //    bool hGesture = Math.Abs(point.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance;
        //    bool vGesture = Math.Abs(point.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;

        //    return (hGesture | vGesture);
        //}

		private UIElement getTopContainer()
		{
			return Application.Current.MainWindow.Content as UIElement;
		}

		private void tbFolderName_DragOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.All;
			e.Handled = true;
		}

		private void tbFolderName_Drop(object sender, DragEventArgs e)
		{
			if (e.Data is System.Windows.DataObject &&
			((System.Windows.DataObject)e.Data).ContainsFileDropList())
			{
				inputData.FileOrFolderPath = "";
				StringCollection fileNames = ((System.Windows.DataObject)e.Data).GetFileDropList();
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < fileNames.Count; i++)
				{
					sb.Append(fileNames[i]);
					if (i < (fileNames.Count - 1))
						sb.Append(";");
				}
				inputData.FileOrFolderPath = sb.ToString();
			}
		}
		#endregion

        private void cbMultiline_Unchecked(object sender, RoutedEventArgs e)
        {
            //gridMain.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Auto);
            //gridMain.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
        }

        private void FilesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = (ListView)e.Source;
            var items = new List<FormattedGrepResult>();
            foreach (FormattedGrepResult item in listView.SelectedItems)
            {
                items.Add(item);
            }
            inputData.SetCodeSnippets(items);
        }        
	}
}
