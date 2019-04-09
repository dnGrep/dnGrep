using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnGREP.Common;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for PreviewControl.xaml
    /// </summary>
    public partial class PreviewControl : UserControl
    {
        public PreviewControl()
        {
            InitializeComponent();

            DataContext = ViewModel;

            ViewModel.ShowPreview += ViewModel_ShowPreview;
            cbWrapText.IsChecked = GrepSettings.Instance.Get<bool?>(GrepSettings.Key.PreviewWindowWrap);
            zoomSlider.Value = GrepSettings.Instance.Get<int>(GrepSettings.Key.PreviewWindowFont);
        }

        public PreviewViewModel ViewModel { get; private set; } = new PreviewViewModel();

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

        internal void SaveSettings()
        {
            GrepSettings.Instance.Set<bool?>(GrepSettings.Key.PreviewWindowWrap, cbWrapText.IsChecked);
            GrepSettings.Instance.Set<int>(GrepSettings.Key.PreviewWindowFont, (int)zoomSlider.Value);
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
