using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.DockFloat;
using dnGREP.Localization;
using dnGREP.WPF.Properties;
using Windows.Win32;
using Windows.Win32.Foundation;

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

            Rect windowBounds = new(
                LayoutProperties.MainWindowBounds.X,
                LayoutProperties.MainWindowBounds.Y,
                LayoutProperties.MainWindowBounds.Width,
                LayoutProperties.MainWindowBounds.Height);

            if (isVisible)
            {
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

                        TextBoxCommands.BindCommandsToWindow(this);

                        // after window is sized and positioned, asynchronously reset the preview
                        // splitter position (which got moved during the layout)
                        Dispatcher.BeginInvoke(() =>
                        {
                            if (viewModel != null)
                            {
                                viewModel.PreviewDockedWidth = LayoutProperties.PreviewDockedWidth;
                                viewModel.PreviewDockedHeight = LayoutProperties.PreviewDockedHeight;
                            }
                        }, null);
                    }
                    else
                    {
                        this.CenterWindow();
                    }

                    this.ConstrainToScreen();
                };

                TranslationSource.Instance.CurrentCultureChanged += OnCurrentCultureChanged;
            }
            this.isVisible = isVisible;


            viewModel = new MainViewModel();
            viewModel.PreviewHide += ViewModel_PreviewHide;
            viewModel.PreviewShow += ViewModel_PreviewShow;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = viewModel;

            viewModel.PreviewModel = previewControl.ViewModel;
            DockViewModel.Instance.PropertyChanged += ViewModel_PropertyChanged;

            Loaded += Window_Loaded;
            Closing += MainForm_Closing;

            PreviewKeyDown += MainForm_PreviewKeyDown;
        }

        public MainViewModel ViewModel => viewModel;

        private void OnCurrentCultureChanged(object? s, EventArgs e)
        {
            dpFrom.Language = XmlLanguage.GetLanguage(TranslationSource.Instance.CurrentCulture.IetfLanguageTag);
            dpTo.Language = XmlLanguage.GetLanguage(TranslationSource.Instance.CurrentCulture.IetfLanguageTag);

            SetWatermark(dpFrom);
            SetWatermark(dpTo);
        }

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
                    PInvoke.SetParent(new(hwndSource.Handle), HWND.HWND_MESSAGE);
                    Visibility = Visibility.Hidden;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.ParentWindow = this;
            DataObject.AddPastingHandler(tbSearchFor, new DataObjectPastingEventHandler(OnPaste));
            DataObject.AddPastingHandler(tbReplaceWith, new DataObjectPastingEventHandler(OnPaste));

            OnCurrentCultureChanged(null, EventArgs.Empty);

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

            previewControl.PreviewKeyDown += (s, a) =>
            {
                // called when the preview control is un-docked in a Floating Window
                MainForm_PreviewKeyDown(s, a);
                if (a.Handled)
                {
                    previewControl.SetFocus();
                }
            };
        }

        private static void SetWatermark(DatePicker dp)
        {
            if (dp == null) return;

            // force visual tree to be built, even if control is not visible
            dp.ApplyTemplate();

            var tb = dp.GetChildOfType<DatePickerTextBox>();
            if (tb == null) return;

            // force visual tree to be built, even if control is not visible
            tb.ApplyTemplate();

            if (tb.Template.FindName("PART_Watermark", tb) is ContentControl wm)
            {
                wm.Content = Localization.Properties.Resources.Main_SelectADate;
            }
        }

        private void SetActivePreviewDockSite()
        {
            var dvm = DockViewModel.Instance;
            if (dvm.PreviewDockSide == Dock.Right)
            {
                var element = dockSiteBottom.Content;
                if (element != null)
                {
                    dockSiteBottom.Content = null;
                    dockSiteRight.Content = element;
                }
            }
            else if (dvm.PreviewDockSide == Dock.Bottom)
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
            var dvm = DockViewModel.Instance;
            if (viewModel.PreviewFileContent && dvm.IsPreviewDocked && dvm.PreviewAutoPosition)
            {
                if (ratio > UpperThreshold && dvm.PreviewDockSide == Dock.Bottom)
                {
                    dvm.PreviewDockSide = Dock.Right;
                    dvm.SaveSettings();
                }
                else if (ratio < LowerThreshold && dvm.PreviewDockSide == Dock.Right)
                {
                    dvm.PreviewDockSide = Dock.Bottom;
                    dvm.SaveSettings();
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

        /// <summary>
        /// Show drag-drop effects for file drop when over the search path text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchPath_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                bool append = e.KeyStates.HasFlag(DragDropKeyStates.ControlKey);
                e.Effects = append ? DragDropEffects.Copy : DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void MainForm_Closing(object? sender, CancelEventArgs e)
        {
            if (viewModel.IsReplaceRunning)
            {
                MessageBox.Show(Localization.Properties.Resources.MessageBox_ReplaceInFilesIsRunning + Environment.NewLine +
                    Localization.Properties.Resources.MessageBox_AllowItCompleteOrCancelItBeforeExiting,
                    Localization.Properties.Resources.MessageBox_DnGrep,
                    MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);

                e.Cancel = true;
                return;
            }

            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ConfirmExitScript) &&
                viewModel.IsScriptRunning)
            {
                if (!viewModel.ConfirmScriptExit())
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ConfirmExitSearch))
            {
                TimeSpan threshold = TimeSpan.FromMinutes(GrepSettings.Instance.Get<double>(GrepSettings.Key.ConfirmExitSearchDuration));

                if (viewModel.CurrentSearchDuration > threshold || viewModel.LatestSearchDuration > threshold)
                {
                    if (!viewModel.ConfirmSearchExit())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            viewModel.CancelSearch();

            if (WindowState == WindowState.Normal)
            {
                LayoutProperties.MainWindowBounds = new Rect(
                    Left,
                    Top,
                    ActualWidth,
                    ActualHeight);
            }
            else
            {
                LayoutProperties.MainWindowBounds = RestoreBounds;
            }
            LayoutProperties.MainWindowState = WindowState.Normal;
            if (WindowState == WindowState.Maximized)
                LayoutProperties.MainWindowState = WindowState.Maximized;

            previewControl.SaveSettings();
            viewModel.SaveSettings();
            if (!viewModel.Closing())
            {
                e.Cancel = true;
            }
        }

        private void ViewModel_PreviewShow(object? sender, EventArgs e)
        {
            foreach (Window wnd in DockSite.GetAllFloatWindows(this))
            {
                if (wnd.WindowState == WindowState.Minimized)
                    wnd.WindowState = WindowState.Normal;

                if (!wnd.IsVisible)
                    wnd.Show();
            }
        }

        private void ViewModel_PreviewHide(object? sender, EventArgs e)
        {
            foreach (Window wind in DockSite.GetAllFloatWindows(this))
            {
                wind.Close();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
                var dvm = DockViewModel.Instance;
                double ratio = ActualWidth / ActualHeight;
                if (ratio > UpperThreshold && dvm.PreviewDockSide == Dock.Bottom)
                {
                    dvm.PreviewAutoPosition = false;
                    dvm.SaveSettings();
                }
                else if (ratio < LowerThreshold && dvm.PreviewDockSide == Dock.Right)
                {
                    dvm.PreviewAutoPosition = false;
                    dvm.SaveSettings();
                }
            }
        }

        #region UI fixes
        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source is TextBox box)
            {
                box.SelectAll();
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            //regex that matches allowed text
            return AllowedTextRegex().IsMatch(text);
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

        private void MainForm_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F3 && Keyboard.Modifiers == ModifierKeys.None)
            {
                resultsTree.SetFocus();
                resultsTree.Next();
                e.Handled = true;
            }
            else if (e.Key == Key.F3 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                resultsTree.SetFocus();
                resultsTree.NextFile();
                e.Handled = true;
            }
            else if (e.Key == Key.F4 && Keyboard.Modifiers == ModifierKeys.None)
            {
                resultsTree.SetFocus();
                resultsTree.Previous();
                e.Handled = true;
            }
            else if (e.Key == Key.F4 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                resultsTree.SetFocus();
                resultsTree.PreviousFile();
                e.Handled = true;
            }
        }

        private DateTime timeOfLastMessage = DateTime.Now;

        /// <summary>Brings main window to foreground.</summary>
        public void BringToForeground(string searchPath)
        {
            TimeSpan fromLastMessage = DateTime.Now - timeOfLastMessage;
            timeOfLastMessage = DateTime.Now;

            bool replace = fromLastMessage > TimeSpan.FromMilliseconds(500);

            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.PassSearchFolderToSingleton) &&
                !string.IsNullOrEmpty(searchPath))
            {
                if (replace || string.IsNullOrEmpty(viewModel.FileOrFolderPath))
                {
                    viewModel.FileOrFolderPath = searchPath;
                }
                else
                {
                    bool found = false;
                    foreach (var subPath in UiUtils.SplitPath(viewModel.FileOrFolderPath, true))
                    {
                        if (subPath.Equals(searchPath, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        viewModel.FileOrFolderPath += ";" + searchPath;
                    }
                }
            }

            if (WindowState == WindowState.Minimized || Visibility == Visibility.Hidden)
            {
                Show();
                WindowState = WindowState.Normal;
            }

            // According to some sources these steps gurantee that an app will be brought to foreground.
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        [GeneratedRegex("\\d+")]
        private static partial Regex AllowedTextRegex();
    }
}
