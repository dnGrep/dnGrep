using System.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class CodeSnippet : INotifyPropertyChanged
    {
        private int[] lineNumbers;
        private string text;
        private GrepSearchResult result;

        public CodeSnippet(int[] lines, string text, GrepSearchResult result)
        {
            lineNumbers = lines;
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
                previewViewModel.LineNumbers = lineNumbers;
                previewViewModel.SearchResult = result;
                previewViewModel.FileName = result.FileNameDisplayed;
                return previewViewModel;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }
    }
}
