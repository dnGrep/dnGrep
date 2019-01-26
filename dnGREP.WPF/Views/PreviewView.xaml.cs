using System;
using System.Windows;
using System.Windows.Input;
using dnGREP.Common;
using dnGREP.Common.UI;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for Preview.xaml
    /// </summary>
    public partial class PreviewView : Window
    {
        private bool forceClose = false;
        private PreviewViewModel viewModel;

        public PreviewView()
        {
            InitializeComponent();

            Loaded += PreviewView_Loaded;
            DataContextChanged += PreviewView_DataContextChanged;
            textEditor.Loaded += TextEditor_Loaded;
            cbWrapText.IsChecked = GrepSettings.Instance.Get<bool?>(GrepSettings.Key.PreviewWindowWrap);
            zoomSlider.Value = GrepSettings.Instance.Get<int>(GrepSettings.Key.PreviewWindowFont);
        }

        void PreviewView_Loaded(object sender, RoutedEventArgs e)
        {
            Left = Properties.Settings.Default.PreviewBounds.Left;
            Top = Properties.Settings.Default.PreviewBounds.Top;
            Width = Properties.Settings.Default.PreviewBounds.Width;
            Height = Properties.Settings.Default.PreviewBounds.Height;

            if (!UiUtils.IsOnScreen(this))
                UiUtils.CenterWindow(this);
        }

        void PreviewView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            viewModel = DataContext as PreviewViewModel;
            viewModel.ShowPreview += ViewModel_ShowPreview;
        }

        void ViewModel_ShowPreview(object sender, ShowEventArgs e)
        {
            if (!e.ClearContent)
            {
                if (textEditor.IsLoaded)
                {
                    textEditor.ScrollTo(viewModel.LineNumber, 0);
                }
            }
            else
            {
                textEditor.Clear();
                textEditor.Encoding = viewModel.Encoding;
                textEditor.SyntaxHighlighting = viewModel.HighlightingDefinition;
                for (int i = textEditor.TextArea.TextView.LineTransformers.Count - 1; i >= 0; i--)
                {
                    if (textEditor.TextArea.TextView.LineTransformers[i] is PreviewHighlighter)
                        textEditor.TextArea.TextView.LineTransformers.RemoveAt(i);
                }
                textEditor.TextArea.TextView.LineTransformers.Add(new PreviewHighlighter(viewModel.GrepResult));

                try
                {
                    if (viewModel.IsLargeOrBinary != Visibility.Visible)
                    {
                        if (!string.IsNullOrWhiteSpace(viewModel.FilePath))
                        {
                            Title = string.Format("Previewing \"{0}\"", viewModel.DisplayFileName);
                            textEditor.Load(viewModel.FilePath);
                            if (textEditor.IsLoaded)
                            {
                                textEditor.ScrollTo(viewModel.LineNumber, 0);
                            }
                        }
                        else
                        {
                            Title = "No files to preview.";
                            textEditor.Text = "";
                        }
                    }
                    Show();
                }
                catch (Exception ex)
                {
                    textEditor.Text = "Error opening the file: " + ex.Message;
                }
            }
        }

        void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            textEditor.ScrollTo(viewModel.LineNumber, 0);
        }

        public void ResetTextEditor()
        {
            viewModel.FilePath = null;
        }

        internal void SaveSettings()
        {
            GrepSettings.Instance.Set<bool?>(GrepSettings.Key.PreviewWindowWrap, cbWrapText.IsChecked);
            GrepSettings.Instance.Set<int>(GrepSettings.Key.PreviewWindowFont, (int)zoomSlider.Value);
        }

        public void ForceClose()
        {
            forceClose = true;

            if (viewModel != null)
                viewModel.ShowPreview -= ViewModel_ShowPreview;

            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.PreviewBounds = new System.Drawing.Rectangle(
                (int)Left,
                (int)Top,
                (int)ActualWidth,
                (int)ActualHeight);
            Properties.Settings.Default.Save();

            if (!forceClose)
            {
                Hide();
                e.Cancel = true;
            }
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
            Title = string.Format("Previewing \"{0}\"", viewModel.DisplayFileName);
            textEditor.Load(viewModel.FilePath);
            viewModel.IsLargeOrBinary = Visibility.Collapsed;
            textEditor.ScrollTo(viewModel.LineNumber, 0);
        }
    }
}
