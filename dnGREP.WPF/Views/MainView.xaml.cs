using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using dnGREP.Common.UI;
using NLog;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class MainForm : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private MainViewModel inputData;
        private bool isVisible = true;

        public MainForm()
            : this(true)
        {
        }

        public MainForm(bool isVisible)
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

            this.PreviewKeyDown += MainFormEx_PreviewKeyDown;
            this.PreviewKeyUp += MainFormEx_PreviewKeyUp;
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

            var textBox = (tbSearchFor.Template.FindName("PART_EditableTextBox", tbSearchFor) as TextBox);
            if (textBox != null && !tbSearchFor.IsDropDownOpen)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.SelectAll();
                    textBox.Focus();
                }));
            }
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

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("\\d+"); //regex that matches allowed text
            return regex.IsMatch(text);
        }

        // Use the DataObject.Pasting Handler 
        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            inputData.ChangePreviewWindowState(this.WindowState);
        }

        #endregion

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

        private void ManipulationBoundaryFeedbackHandler(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            // disable feedback that list scroll has reached the limit
            // -- the feedback is that the whole window moves
            e.Handled = true;
        }

        private void cbEncoding_Initialized(object sender, EventArgs e)
        {
            // SelectedIndex="0" isn't working on the XAML for cbEncoding, but this seems to work. It would be nice to get the XAML working, instead.
            var model = (MainViewModel)this.DataContext;
            if (model != null)
                ((ComboBox)sender).SelectedValue = model.CodePage;
            else  // design time
                ((ComboBox)sender).SelectedIndex = 0;
        }

        private void WrapPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var panel = (WrapPanel)sender;

            var maxWidth = panel.ActualWidth -
                LeftFileOptions.ActualWidth - LeftFileOptions.Margin.Left - LeftFileOptions.Margin.Right -
                MiddleFileOptions.ActualWidth - MiddleFileOptions.Margin.Left - MiddleFileOptions.Margin.Right -
                RightFileOptions.ActualWidth - RightFileOptions.Margin.Left - RightFileOptions.Margin.Right;
            SpacerFileOptions.Width = Math.Max(0, maxWidth);
        }

        private void Expander_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // set the max width of the File Filters Summary text block so the Search In Archives checkbox does not overlap it
            var panel = (Expander)sender;

            var iconWidth = 40;
            var maxWidth = panel.ActualWidth - iconWidth -
                fileOptions.ActualWidth - fileOptions.Margin.Left - fileOptions.Margin.Right -
                cbIncludeArchives.ActualWidth - cbIncludeArchives.Margin.Left - cbIncludeArchives.Margin.Right -
                pathsToIgnoreLabel.ActualWidth - pathsToIgnoreLabel.Margin.Left - pathsToIgnoreLabel.Margin.Right -
                tbFilePatternIgnore.ActualWidth - tbFilePatternIgnore.Margin.Left - tbFilePatternIgnore.Margin.Right;

            inputData.MaxFileFiltersSummaryWidth = Math.Max(0, maxWidth);
        }

        void MainFormEx_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                fileOptions.Inlines.Clear();
                fileOptions.Inlines.Add(new Run("Mor"));
                fileOptions.Inlines.Add(new Underline(new Run("e")));
                fileOptions.Inlines.Add(new Run("..."));
            }
        }

        void MainFormEx_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyUp(Key.LeftAlt) && Keyboard.IsKeyUp(Key.RightAlt))
            {
                fileOptions.Text = "More...";
            }
        }
    }
}
