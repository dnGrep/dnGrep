using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using dnGREP.Common.UI;
using NLog;

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
            : this(true)
        {
        }

        public MainFormEx(bool isVisible)
        {
            InitializeComponent();

            this.Width = Properties.Settings.Default.MainFormExBounds.Width;
            this.Height = Properties.Settings.Default.MainFormExBounds.Height;
            this.Top = Properties.Settings.Default.MainFormExBounds.Y;
            this.Left = Properties.Settings.Default.MainFormExBounds.X;
            this.WindowState = Properties.Settings.Default.WindowState;

            this.Loaded += delegate
            {
                if (!UiUtils.IsOnScreen(this))
                    UiUtils.CenterWindow(this);
            };
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
            inputData.ParentWindow = this;
            DataObject.AddPastingHandler(tbSearchFor, new DataObjectPastingEventHandler(onPaste));
            DataObject.AddPastingHandler(tbReplaceWith, new DataObjectPastingEventHandler(onPaste));
        }

        /// <summary>
        /// Workaround to enable pasting tabs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onPaste(object sender, DataObjectPastingEventArgs e)
        {
            var isText = e.SourceDataObject.GetDataPresent(System.Windows.DataFormats.Text, true);
            if (!isText) return;
            var senderControl = (Control)sender;
            var textBox = (TextBox)senderControl.Template.FindName("PART_EditableTextBox", senderControl);
            textBox.AcceptsTab = true;
            var text = e.SourceDataObject.GetData(DataFormats.Text) as string;
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                textBox.AcceptsTab = false;
            }), null);
        }

        private void MainForm_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.MainFormExBounds = new System.Drawing.Rectangle(
                (int)Left,
                (int)Top,
                (int)ActualWidth,
                (int)ActualHeight);
            Properties.Settings.Default.WindowState = System.Windows.WindowState.Normal;
            if (this.WindowState == System.Windows.WindowState.Maximized)
                Properties.Settings.Default.WindowState = System.Windows.WindowState.Maximized;
            Properties.Settings.Default.Save();

            inputData.CloseCommand.Execute(null);
        }

        #region UI fixes
        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source is TextBox)
            {
                ((TextBox)e.Source).SelectAll();
            }
        }

        //private void btnSearchFastBookmarks_Click(object sender, RoutedEventArgs e)
        //{
        //    cbSearchFastBookmark.IsDropDownOpen = true;
        //    cbSearchFastBookmark.Focus();
        //}

        //private void btnReplaceFastBookmarks_Click(object sender, RoutedEventArgs e)
        //{
        //    cbReplaceFastBookmark.IsDropDownOpen = true;
        //    cbReplaceFastBookmark.Focus();
        //    cbReplaceFastBookmark.SelectAll();
        //}

        private void Window_Activated(object sender, EventArgs e)
        {
            inputData.ActivatePreviewWindow();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            inputData.ChangePreviewWindowState(this.WindowState);
        }

        //private void tbPreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Up || e.Key == Key.Down)
        //    {
        //        if (!inputData.Multiline)
        //        {
        //            if (sender != null && sender == tbSearchFor)
        //            {
        //                if (e.Key == Key.Down)
        //                    cbSearchFastBookmark.SelectedIndex++;
        //                else
        //                {
        //                    if (cbSearchFastBookmark.SelectedIndex > 0)
        //                        cbSearchFastBookmark.SelectedIndex--;
        //                }
        //            }
        //            else if (sender != null && sender == tbReplaceWith)
        //            {
        //                if (e.Key == Key.Down)
        //                    cbReplaceFastBookmark.SelectedIndex++;
        //                else
        //                {
        //                    if (cbReplaceFastBookmark.SelectedIndex > 0)
        //                        cbReplaceFastBookmark.SelectedIndex--;
        //                }
        //            }
        //        }
        //    }
        //}
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

        private void btnOtherActions_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            advanceContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            advanceContextMenu.PlacementTarget = (UIElement)sender;
            advanceContextMenu.IsOpen = true;
        }

        private void btnOtherActions_Click(object sender, RoutedEventArgs e)
        {
            advanceContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            advanceContextMenu.PlacementTarget = (UIElement)sender;
            advanceContextMenu.IsOpen = true;
        }
    }
}
