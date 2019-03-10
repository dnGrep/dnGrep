using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnGREP.Common;
using dnGREP.Common.UI;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for PreviewControl.xaml
    /// </summary>
    public partial class PreviewControl : UserControl
    {
        //private bool forceClose = false;

        public PreviewControl()
        {
            InitializeComponent();

            DataContext = ViewModel;

            Loaded += PreviewControl_Loaded;
            //DataContextChanged += PreviewControl_DataContextChanged;
            ViewModel.ShowPreview += ViewModel_ShowPreview;
            PreviewKeyDown += PreviewControl_PreviewKeyDown;
            textEditor.Loaded += TextEditor_Loaded;
            cbWrapText.IsChecked = GrepSettings.Instance.Get<bool?>(GrepSettings.Key.PreviewWindowWrap);
            zoomSlider.Value = GrepSettings.Instance.Get<int>(GrepSettings.Key.PreviewWindowFont);
        }

        public PreviewViewModel ViewModel { get; private set; } = new PreviewViewModel();

        private void PreviewControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Escape)
            //    Close();
        }

        void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Left = Properties.Settings.Default.PreviewBounds.Left;
            //Top = Properties.Settings.Default.PreviewBounds.Top;
            //Width = Properties.Settings.Default.PreviewBounds.Width;
            //Height = Properties.Settings.Default.PreviewBounds.Height;

            //if (!UiUtils.IsOnScreen(this))
            //    UiUtils.CenterWindow(this);
        }

        //void PreviewControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    ViewModel = DataContext as PreviewViewModel;
        //    ViewModel.ShowPreview += ViewModel_ShowPreview;
        //}

        void ViewModel_ShowPreview(object sender, ShowEventArgs e)
        {
            if (!e.ClearContent)
            {
                if (textEditor.IsLoaded)
                {
                    textEditor.ScrollTo(ViewModel.LineNumber, 0);
                }
            }
            else
            {
                textEditor.Clear();
                textEditor.Encoding = ViewModel.Encoding;
                textEditor.SyntaxHighlighting = ViewModel.HighlightingDefinition;
                for (int i = textEditor.TextArea.TextView.LineTransformers.Count - 1; i >= 0; i--)
                {
                    if (textEditor.TextArea.TextView.LineTransformers[i] is PreviewHighlighter)
                        textEditor.TextArea.TextView.LineTransformers.RemoveAt(i);
                }

                if (!ViewModel.HighlightDisabled)
                    textEditor.TextArea.TextView.LineTransformers.Add(new PreviewHighlighter(ViewModel.GrepResult));

                try
                {
                    if (!ViewModel.IsLargeOrBinary)
                    {
                        if (!string.IsNullOrWhiteSpace(ViewModel.FilePath))
                        {
                            //Title = string.Format("Previewing \"{0}\"", ViewModel.DisplayFileName);
                            textEditor.Load(ViewModel.FilePath);
                            if (textEditor.IsLoaded)
                            {
                                textEditor.ScrollTo(ViewModel.LineNumber, 0);
                            }
                        }
                        else
                        {
                            //Title = "No files to preview.";
                            textEditor.Text = "";
                        }
                    }
                    //Show();
                }
                catch (Exception ex)
                {
                    textEditor.Text = "Error opening the file: " + ex.Message;
                }
            }
        }

        void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
                textEditor.ScrollTo(ViewModel.LineNumber, 0);
        }

        public void ResetTextEditor()
        {
            if (ViewModel != null)
                ViewModel.FilePath = null;
        }

        internal void SaveSettings()
        {
            GrepSettings.Instance.Set<bool?>(GrepSettings.Key.PreviewWindowWrap, cbWrapText.IsChecked);
            GrepSettings.Instance.Set<int>(GrepSettings.Key.PreviewWindowFont, (int)zoomSlider.Value);
        }

        //public void ForceClose()
        //{
        //    forceClose = true;

        //    if (ViewModel != null)
        //        ViewModel.ShowPreview -= ViewModel_ShowPreview;

        //    Close();
        //}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Properties.Settings.Default.PreviewBounds = new System.Drawing.Rectangle(
            //    (int)Left,
            //    (int)Top,
            //    (int)ActualWidth,
            //    (int)ActualHeight);
            //Properties.Settings.Default.Save();

            //if (!forceClose)
            //{
            //    Hide();
            //    e.Cancel = true;
            //}
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs args)
        {
            base.OnPreviewMouseWheel(args);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl))
            {
                zoomSlider.Value += (args.Delta > 0) ? 1 : -1;
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs args)
        {
            base.OnPreviewMouseDown(args);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (args.MiddleButton == MouseButtonState.Pressed)
                {
                    zoomSlider.Value = 12;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Title = string.Format("Previewing \"{0}\"", ViewModel.DisplayFileName);
            textEditor.Load(ViewModel.FilePath);
            ViewModel.IsLargeOrBinary = false;
            textEditor.ScrollTo(ViewModel.LineNumber, 0);
        }
    }
}
