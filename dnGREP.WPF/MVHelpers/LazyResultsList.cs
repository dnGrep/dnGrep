using dnGREP.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace dnGREP.WPF.MVHelpers
{
    public class LazyResultsList : ObservableCollection<FormattedGrepLine>, INotifyPropertyChanged
    {
        private GrepSearchResult result;
        private FormattedGrepResult formattedResult;

        public LazyResultsList(GrepSearchResult result, FormattedGrepResult formattedResult)
        {
            this.result = result;
            this.formattedResult = formattedResult;
            if ((result.Matches != null && result.Matches.Count > 0) || !result.IsSuccess)
            {
                GrepSearchResult.GrepLine emptyLine = new GrepSearchResult.GrepLine(-1, "", true, null);
                this.Add(new FormattedGrepLine(emptyLine, formattedResult, 30));
            }
        }

        private int lineNumberColumnWidth = 30;
        public int LineNumberColumnWidth
        {
            get { return lineNumberColumnWidth; }
            set { lineNumberColumnWidth = value; OnPropertyChanged("LineNumberColumnWidth"); }
        }

        public void Load()
        {
            if (this.Count == 1 && this[0].GrepLine.LineNumber == -1)
                this.Clear();
            int currentLine = -1;
            List<GrepSearchResult.GrepLine> linesWithContext = new List<GrepSearchResult.GrepLine>();
            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext))
                linesWithContext = result.GetLinesWithContext(GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore),
                GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter));
            else
                linesWithContext = result.GetLinesWithContext(0, 0);

            for (int i = 0; i < linesWithContext.Count; i++)
            {
                GrepSearchResult.GrepLine line = linesWithContext[i];

                // Adding separator
                if (this.Count > 0 && GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext) &&
                    (currentLine != line.LineNumber && currentLine + 1 != line.LineNumber))
                {
                    GrepSearchResult.GrepLine emptyLine = new GrepSearchResult.GrepLine(-1, "", true, null);
                    this.Add(new FormattedGrepLine(emptyLine, formattedResult, 30));
                }

                currentLine = line.LineNumber;
                if (currentLine <= 999 && LineNumberColumnWidth < 30)
                    LineNumberColumnWidth = 30;
                else if (currentLine > 999 && LineNumberColumnWidth < 35)
                    LineNumberColumnWidth = 35;
                else if (currentLine > 9999 && LineNumberColumnWidth < 47)
                    LineNumberColumnWidth = 47;
                else if (currentLine > 99999 && LineNumberColumnWidth < 50)
                    LineNumberColumnWidth = 50;
                this.Add(new FormattedGrepLine(line, formattedResult, LineNumberColumnWidth));                
            }
        }

        public event EventHandler<PropertyChangedEventArgs> LineNumberColumnWidthChanged;

        protected void OnPropertyChanged(string name)
        {
            LineNumberColumnWidthChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
