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
    public partial class Preview : Window
    {
        private int line;
        private GrepSearchResult grepResult;
        private Dictionary<string, IHighlightingDefinition> highlightDefinitions = new Dictionary<string,IHighlightingDefinition>();
        private string currentFile;
        private bool forceClose = false;
        internal StickyWindow StickyWindow;
        private PreviewState inputData = new PreviewState();
        
        public Preview()
        {
            InitializeComponent();
            inputData.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(inputData_PropertyChanged);
            inputData.Highlighters = new List<string>();
            foreach (var hl in HighlightingManager.Instance.HighlightingDefinitions)
            {
                highlightDefinitions[hl.Name] = hl;
                inputData.Highlighters.Add(hl.Name);
            }
            inputData.Highlighters.Add("SQL");
            highlightDefinitions["SQL"] = loadHighlightingDefinition("sqlmode.xshd");
            inputData.Highlighters.Sort();
            inputData.Highlighters.Insert(0, "None");
            inputData.CurrentSyntax = "None";

            this.DataContext = inputData;
            textEditor.Loaded += new RoutedEventHandler(textEditor_Loaded);
            this.Loaded += new RoutedEventHandler(window_Loaded);            
        }

        void inputData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentSyntax")
            {
                textEditor.TextArea.TextView.LineTransformers.Clear();
                if (highlightDefinitions.ContainsKey(inputData.CurrentSyntax))
                    textEditor.SyntaxHighlighting = highlightDefinitions[inputData.CurrentSyntax];
                textEditor.TextArea.TextView.LineTransformers.Add(new PreviewHighlighter(grepResult));
            }
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
                    textEditor.Clear();
                }
                else
                {
                    inputData.IsLargeOrBinary = System.Windows.Visibility.Collapsed;
                    try
                    {
                        loadFile(pathToFile);
                    }
                    catch (Exception ex)
                    {
                        textEditor.Text = "Error opening the file: " + ex.Message;
                    }
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
            this.Title = string.Format("Previewing \"{0}\"", pathToFile);
            textEditor.Load(pathToFile);
            string extension = Path.GetExtension(pathToFile);

            if (!string.IsNullOrEmpty(extension))
            {
                if (extension.ToLower() == ".vbs")
                    extension = ".vb";

                IHighlightingDefinition def = null;
                if (extension.ToLower() == ".sql")
                    def = loadHighlightingDefinition("sqlmode.xshd");
                else
                    def = HighlightingManager.Instance.GetDefinitionByExtension(extension);

                if (def != null)
                    inputData.CurrentSyntax = def.Name;
                else
                    inputData.CurrentSyntax = "None";
            }
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
