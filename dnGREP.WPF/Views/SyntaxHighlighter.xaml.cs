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

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for SyntaxHighlighter.xaml
    /// </summary>
    public partial class SyntaxHighlighter : UserControl
    {
        public SyntaxHighlighter()
        {
            InitializeComponent();
            this.DataContextChanged += SyntaxHighlighter_DataContextChanged;
        }

        void SyntaxHighlighter_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && e.NewValue is SyntaxHighlighterViewModel)
            {
                int lineNumber = ((SyntaxHighlighterViewModel)this.DataContext).LineNumber;
                textArea.LeftMargins.Clear();
                textArea.LeftMargins.Add(new SnippetLineNumber(lineNumber));
                textArea.LeftMargins.Add(DottedLineMargin.Create());
                textArea.TextView.LineTransformers.Add(new PreviewHighlighter(((SyntaxHighlighterViewModel)this.DataContext).SearchResult, lineNumber));
            }
        }
    }
}
