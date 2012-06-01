using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace dnGREP.WPF
{
    public class SyntaxHighlighterViewModel : WorkspaceViewModel
    {
        public SyntaxHighlighterViewModel()
        {
            this.PropertyChanged += SyntaxHighlighterViewModel_PropertyChanged;
        }

        void SyntaxHighlighterViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Text")
                Document = new ICSharpCode.AvalonEdit.Document.TextDocument(Text);
        }

        private ICSharpCode.AvalonEdit.Document.TextDocument document;
        public ICSharpCode.AvalonEdit.Document.TextDocument Document
        {
            get { return document; }
            set
            {
                if (value == document)
                    return;

                document = value;

                base.OnPropertyChanged(() => Document);
            }
        }

        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                if (value == text)
                    return;

                text = value;

                base.OnPropertyChanged(() => Text);
            }
        }

        private int lineNumber;
        public int LineNumber
        {
            get { return lineNumber; }
            set
            {
                if (value == lineNumber)
                    return;

                lineNumber = value;

                base.OnPropertyChanged(() => LineNumber);
            }
        }
    }
}
