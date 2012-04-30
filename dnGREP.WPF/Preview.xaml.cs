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

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for Preview.xaml
    /// </summary>
    public partial class Preview : Window
    {
        private int line;
        private GrepSearchResult grepResult;
        private string currentFile;
        private bool forceClose = false;
        private StickyWindow _stickyWindow;
        private PreviewState inputData = new PreviewState();
        
        public Preview()
        {
            InitializeComponent();
            this.DataContext = inputData;
            textEditor.Loaded += new RoutedEventHandler(textEditor_Loaded);
            this.Loaded += new RoutedEventHandler(window_Loaded);
        }

        void window_Loaded(object sender, RoutedEventArgs e)
        {
            _stickyWindow = new StickyWindow(this);
            _stickyWindow.StickToScreen = true;
            _stickyWindow.StickToOther = true;
            _stickyWindow.StickOnResize = true;
            _stickyWindow.StickOnMove = true;
        }

        void textEditor_Loaded(object sender, RoutedEventArgs e)
        {
            textEditor.ScrollTo(line, 0);
            cbWrapText.IsChecked = GrepSettings.Instance.Get<bool?>(GrepSettings.Key.PreviewWindowWrap);
            zoomSlider.Value = GrepSettings.Instance.Get<int>(GrepSettings.Key.PreviewWindowFont);
        }

        public void ResetTextEditor()
        {
            currentFile = null;
            textEditor.TextArea.TextView.LineTransformers.Clear();
            textEditor.Text = "";
            this.Hide();
        }

        public void Show(string pathToFile, GrepSearchResult grepResult, int line)
        {
            this.grepResult = grepResult;
            this.line = line;
            if (pathToFile != currentFile)
            {
                currentFile = pathToFile;
                textEditor.TextArea.TextView.LineTransformers.Clear();
                textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension("txt");

                var fileInfo = new FileInfo(pathToFile);
                // Do not preview files over 1MB or binary
                if (fileInfo.Length > 1024000 ||
                    Utils.IsBinary(pathToFile))
                {
                    inputData.IsLargeOrBinary = System.Windows.Visibility.Visible;
                }
                else
                {
                    inputData.IsLargeOrBinary = System.Windows.Visibility.Collapsed;
                    loadFile(pathToFile);
                }
            }
            if (textEditor.IsLoaded)
            {
                textEditor.ScrollTo(line, 0);
            }
            this.Show();
        }

        private void loadFile(string pathToFile) 
        {
            textEditor.Load(pathToFile);
            string extension = Path.GetExtension(pathToFile);

            if (!string.IsNullOrEmpty(extension))
            {
                if (extension.ToLower() == ".vbs")
                    extension = ".vb";
                if (extension.ToLower() == ".sql")
                    textEditor.SyntaxHighlighting = loadHighlightingDefinition("sqlmode.xshd");
                else
                    textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(extension);
            }
            textEditor.TextArea.TextView.LineTransformers.Add(new PreviewHighlighter(grepResult));
        }

        private IHighlightingDefinition loadHighlightingDefinition(
            string resourceName)
        {
            var type = typeof(Preview);
            var fullName = type.Namespace + "." + resourceName;
            using (var stream = type.Assembly.GetManifestResourceStream(fullName))
            using (var reader = new XmlTextReader(stream))
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
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
            loadFile(currentFile);
            inputData.IsLargeOrBinary = System.Windows.Visibility.Collapsed;
        }
    }
}
