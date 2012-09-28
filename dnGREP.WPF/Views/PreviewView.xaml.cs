using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit.Highlighting;
using System.IO;
using dnGREP.Common;
using Blue.Windows;
using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using dnGREP.Common.UI;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for Preview.xaml
    /// </summary>
    public partial class PreviewView : Window
    {
        private bool forceClose = false;
        internal StickyWindow StickyWindow;
        private PreviewViewModel inputData;

        public PreviewView()
        {
            InitializeComponent();

            DataContextChanged += PreviewView_DataContextChanged;
            textEditor.Loaded += new RoutedEventHandler(textEditor_Loaded);
            Loaded += new RoutedEventHandler(window_Loaded);
            cbWrapText.IsChecked = GrepSettings.Instance.Get<bool?>(GrepSettings.Key.PreviewWindowWrap);
            zoomSlider.Value = GrepSettings.Instance.Get<int>(GrepSettings.Key.PreviewWindowFont);
        }

        void window_Loaded(object sender, RoutedEventArgs e)
        {
            StickyWindow = new StickyWindow(this);
            StickyWindow.StickToScreen = true;
            StickyWindow.StickToOther = true;
            StickyWindow.StickOnResize = true;
            StickyWindow.StickOnMove = true;
            StickyWindow.MoveStuckTogether = false;
            if (!UiUtils.IsOnScreen(this))
                UiUtils.CenterWindow(this);
        }

        void PreviewView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            inputData = this.DataContext as PreviewViewModel;
            inputData.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(inputData_PropertyChanged);
            inputData.ShowPreview += inputData_ShowPreview;
        }

        void inputData_ShowPreview(object sender, ShowEventArgs e)
        {
            if (!e.ClearContent)
            {
                if (textEditor.IsLoaded)
                {
                    textEditor.ScrollTo(inputData.LineNumber, 0);
                }

            }
            else
            {
                textEditor.Clear();
                textEditor.SyntaxHighlighting = inputData.HighlightingDefinition;
                for (int i = textEditor.TextArea.TextView.LineTransformers.Count - 1; i >= 0; i--)
                {
                    if (textEditor.TextArea.TextView.LineTransformers[i] is PreviewHighlighter)
                        textEditor.TextArea.TextView.LineTransformers.RemoveAt(i);
                }
                textEditor.TextArea.TextView.LineTransformers.Add(new PreviewHighlighter(inputData.GrepResult));

                try
                {
                    if (inputData.IsLargeOrBinary != System.Windows.Visibility.Visible)
                    {
                        this.Title = string.Format("Previewing \"{0}\"", inputData.FilePath);
                        textEditor.Load(inputData.FilePath);
                        if (textEditor.IsLoaded)
                        {
                            textEditor.ScrollTo(inputData.LineNumber, 0);
                        }
                    }
                    this.Show();
                }
                catch (Exception ex)
                {
                    textEditor.Text = "Error opening the file: " + ex.Message;
                }
            }
        }

        void inputData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }

        void textEditor_Loaded(object sender, RoutedEventArgs e)
        {
            textEditor.ScrollTo(inputData.LineNumber, 0);
        }

        public void ResetTextEditor()
        {
            this.inputData.FilePath = null;
        }

        public void ForceClose()
        {
            forceClose = true;
            GrepSettings.Instance.Set<bool?>(GrepSettings.Key.PreviewWindowWrap, cbWrapText.IsChecked);
            GrepSettings.Instance.Set<int>(GrepSettings.Key.PreviewWindowFont, (int)zoomSlider.Value);
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!forceClose)
            {
                this.Hide();
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
            this.Title = string.Format("Previewing \"{0}\"", inputData.FilePath);
            textEditor.Load(inputData.FilePath);
            inputData.IsLargeOrBinary = System.Windows.Visibility.Collapsed;
        }
    }
}
