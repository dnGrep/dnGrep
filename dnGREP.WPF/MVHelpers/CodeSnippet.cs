using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace dnGREP.WPF
{
    public class CodeSnippet : INotifyPropertyChanged
    {
        private int lineNumber;
        private string text;

        public CodeSnippet(int line, string text)
        {
            lineNumber = line;
            this.text = text;
        }

        private SyntaxHighlighterViewModel previewViewModel;
        public SyntaxHighlighterViewModel PreviewViewModel
        {
            get
            {
                if (previewViewModel == null)
                    previewViewModel = new SyntaxHighlighterViewModel();
                previewViewModel.Text = text;
                previewViewModel.LineNumber = lineNumber;
                return previewViewModel;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
