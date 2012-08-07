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
	}
}
