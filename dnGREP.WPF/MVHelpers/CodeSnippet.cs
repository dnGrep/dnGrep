using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class CodeSnippet : INotifyPropertyChanged
    {
        private int lineNumber;
        private string text;
        private GrepSearchResult result;

        public CodeSnippet(int line, string text, GrepSearchResult result)
        {
            lineNumber = line;
            this.text = text;
            this.result = result;
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
                previewViewModel.SearchResult = result;
                return previewViewModel;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
