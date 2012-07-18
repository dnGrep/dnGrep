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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for SyntaxHighlighter.xaml
    /// </summary>
    public partial class SyntaxHighlighter : UserControl
    {
        private Dictionary<string, IHighlightingDefinition> highlightDefinitions = new Dictionary<string, IHighlightingDefinition>();

        public SyntaxHighlighter()
        {
            InitializeComponent();
            highlightDefinitions["SQL"] = loadHighlightingDefinition("sqlmode.xshd");
            this.DataContextChanged += SyntaxHighlighter_DataContextChanged;
        }

        private IHighlightingDefinition loadHighlightingDefinition(
            string resourceName)
        {
            var type = typeof(PreviewView);
            var fullName = type.Namespace + "." + resourceName;
            using (var stream = type.Assembly.GetManifestResourceStream(fullName))
            using (var reader = new XmlTextReader(stream))
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        void SyntaxHighlighter_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && e.NewValue is SyntaxHighlighterViewModel)
            {
                int lineNumber = ((SyntaxHighlighterViewModel)this.DataContext).LineNumber;
                textEditor.TextArea.LeftMargins.Clear();
                textEditor.TextArea.LeftMargins.Add(DottedLineMargin.Create());
                textEditor.TextArea.LeftMargins.Add(new SnippetLineNumber(lineNumber));
                textEditor.TextArea.LeftMargins.Add(DottedLineMargin.Create());
                textEditor.TextArea.TextView.LineTransformers.Add(new PreviewHighlighter(((SyntaxHighlighterViewModel)this.DataContext).SearchResult, lineNumber));

                string ext = ".txt";
                if (!string.IsNullOrWhiteSpace(((SyntaxHighlighterViewModel)this.DataContext).FileName))
                    ext = System.IO.Path.GetExtension((((SyntaxHighlighterViewModel)this.DataContext).FileName)).ToLower();

                IHighlightingDefinition def = null;
                if (ext == ".sql")
                    def = loadHighlightingDefinition("sqlmode.xshd");
                else
                    def = HighlightingManager.Instance.GetDefinitionByExtension(ext);
                textEditor.SyntaxHighlighting = def;
            }
        }
    }
}
