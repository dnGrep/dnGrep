using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using dnGREP.WPF.Properties;
using DockFloat;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class MainForm : ThemedWindow
    {
        private readonly MainViewModel viewModel;
        private readonly bool isVisible = true;
        private const double UpperThreshold = 1.4;
        private const double LowerThreshold = 1.0;


        public MainForm()
            : this(true)
        {
        }

        public MainForm(bool isVisible)
        {
            InitializeComponent();

            SizeChanged += MainForm_SizeChanged;

            // fix for placements on monitors with different DPIs:
            // initial placement on the primary monitor, then move it
            // to the saved location when the window is loaded
            Left = 0;
            Top = 0;
            Width = LayoutProperties.MainWindowBounds.Width;
            Height = LayoutProperties.MainWindowBounds.Height;
            WindowState = WindowState.Normal;

            Rect windowBounds = new Rect(
                LayoutProperties.MainWindowBounds.X,
                LayoutProperties.MainWindowBounds.Y,
                LayoutProperties.MainWindowBounds.Width,
                LayoutProperties.MainWindowBounds.Height);

            Loaded += (s, e) =>
            {
                if (windowBounds.IsOnScreen())
                {
                    // setting Left and Top does not work when
                    // moving to a monitor with a different DPI
                    // than the primary monitor
                    this.MoveWindow(
                        LayoutProperties.MainWindowBounds.X,
                        LayoutProperties.MainWindowBounds.Y);
                    WindowState = LayoutProperties.MainWindowState;
                }
                else
                {
                    this.CenterWindow();
                }

                this.ConstrainToScreen();
            };
            this.isVisible = isVisible;


            viewModel = new MainViewModel();
            viewModel.PreviewHide += ViewModel_PreviewHide;
            viewModel.PreviewShow += ViewModel_PreviewShow;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = viewModel;

            viewModel.PreviewModel = previewControl.ViewModel;

            Loaded += Window_Loaded;
            Closing += MainForm_Closing;

            PreviewKeyDown += MainFormEx_PreviewKeyDown;
            PreviewKeyUp += MainFormEx_PreviewKeyUp;
        }

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwndNewParent);

        private const int HWND_MESSAGE = -3;

        protected override void OnSourceInitialized(EventArgs e)
        {
            if (isVisible)
            {
                base.OnSourceInitialized(e);
            }

            if (!isVisible)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                {
                    // make this a message-only window
                    SetParent(hwndSource.Handle, (IntPtr)HWND_MESSAGE);
                    Visibility = Visibility.Hidden;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.ParentWindow = this;
            DataObject.AddPastingHandler(tbSearchFor, new DataObjectPastingEventHandler(OnPaste));
            DataObject.AddPastingHandler(tbReplaceWith, new DataObjectPastingEventHandler(OnPaste));

            if (tbSearchFor.Template.FindName("PART_EditableTextBox", tbSearchFor) is TextBox textBox && !tbSearchFor.IsDropDownOpen)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox.SelectAll();
                    textBox.Focus();
                }));
            }

            SetActivePreviewDockSite();
            DockSite.InitFloatingWindows();
        }

        private void SetActivePreviewDockSite()
        {
            if (viewModel.PreviewDockSide == Dock.Right)
            {
                var element = dockSiteBottom.Content;
                if (element != null)
                {
                    dockSiteBottom.Content = null;
                    dockSiteRight.Content = element;
                }
            }
            else if (viewModel.PreviewDockSide == Dock.Bottom)
            {
                var element = dockSiteRight.Content;
                if (element != null)
                {
                    dockSiteRight.Content = null;
                    dockSiteBottom.Content = element;
                }
            }
        }

        private void AutoPosistionPreviewWindow(double ratio)
        {
            if (viewModel.PreviewFileContent && viewModel.IsPreviewDocked && viewModel.PreviewAutoPosition)
            {
                if (ratio > UpperThreshold && viewModel.PreviewDockSide == Dock.Bottom)
                {
                    viewModel.PreviewDockSide = Dock.Right;
                }
                else if (ratio < LowerThreshold && viewModel.PreviewDockSide == Dock.Right)
                {
                    viewModel.PreviewDockSide = Dock.Bottom;
                }
            }
        }

        /// <summary>
        /// Workaround to enable pasting tabs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var isText = e.SourceDataObject.GetDataPresent(DataFormats.Text, true);
            if (!isText) return;
            var senderControl = (Control)sender;
            var textBox = (TextBox)senderControl.Template.FindName("PART_EditableTextBox", senderControl);
            textBox.AcceptsTab = true;
            var text = e.SourceDataObject.GetData(DataFormats.Text) as string;
            Dispatcher.BeginInvoke((Action)(() =>
            {
                textBox.AcceptsTab = false;
            }), null);
        }

        private void MainForm_Closing(object sender, CancelEventArgs e)
        {
            viewModel.CancelSearch();

            LayoutProperties.MainWindowBounds = new Rect(
                Left,
                Top,
                ActualWidth,
                ActualHeight);
            LayoutProperties.MainWindowState = WindowState.Normal;
            if (WindowState == WindowState.Maximized)
                LayoutProperties.MainWindowState = WindowState.Maximized;

            previewControl.SaveSettings();
            viewModel.SaveSettings();
        }

        private void ViewModel_PreviewShow(object sender, EventArgs e)
        {
            foreach (Window wind in DockSite.GetAllFloatWindows(this))
            {
                if (wind.WindowState == WindowState.Minimized)
                    wind.WindowState = WindowState.Normal;
                wind.Show();
                wind.Activate();
                wind.Focus(); // needs focus so the Esc key will close (hide) the preview
            }
        }

        private void ViewModel_PreviewHide(object sender, EventArgs e)
        {
            foreach (Window wind in DockSite.GetAllFloatWindows(this))
            {
                wind.Close();
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsPreviewDocked")
            {
                AutoPosistionPreviewWindow(ActualWidth / ActualHeight);
            }
            else if (e.PropertyName == "PreviewAutoPosition")
            {
                AutoPosistionPreviewWindow(ActualWidth / ActualHeight);
            }
            else if (e.PropertyName == "PreviewDockSide")
            {
                SetActivePreviewDockSite();

                // if the user manually selects the other dock location, turn off auto positioning
                double ratio = ActualWidth / ActualHeight;
                if (ratio > UpperThreshold && viewModel.PreviewDockSide == Dock.Bottom)
                {
                    viewModel.PreviewAutoPosition = false;
                }
                else if (ratio < LowerThreshold && viewModel.PreviewDockSide == Dock.Right)
                {
                    viewModel.PreviewAutoPosition = false;
                }
            }
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

        #endregion

        private void ButtonOtherActions_Click(object sender, RoutedEventArgs e)
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

        private void MainForm_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AutoPosistionPreviewWindow(e.NewSize.Width / e.NewSize.Height);
        }

        private void CbEncoding_Initialized(object sender, EventArgs e)
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

            viewModel.MaxFileFiltersSummaryWidth = Math.Max(0, maxWidth);
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
