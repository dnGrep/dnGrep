using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using dnGREP.Common;

namespace dnGREP.WPF.MVHelpers
{
    public class LazyResultsList : ObservableCollection<FormattedGrepLine>, INotifyPropertyChanged
    {
        private readonly GrepSearchResult result;
        private readonly FormattedGrepResult formattedResult;

        public bool IsLoaded { get; private set; }
        public bool IsLoading { get; private set; }

        public LazyResultsList(GrepSearchResult result, FormattedGrepResult formattedResult)
        {
            this.result = result;
            this.formattedResult = formattedResult;

            if ((result.Matches != null && result.Matches.Count > 0) || !result.IsSuccess)
            {
                GrepLine emptyLine = new GrepLine(-1, "", true, null);
                var dummyLine = new FormattedGrepLine(emptyLine, formattedResult, 30, false);
                Add(dummyLine);
                IsLoaded = false;
            }
        }

        private int lineNumberColumnWidth = 30;
        public int LineNumberColumnWidth
        {
            get { return lineNumberColumnWidth; }
            set { lineNumberColumnWidth = value; OnPropertyChanged("LineNumberColumnWidth"); }
        }

        public void Load(bool isAsync)
        {
            if (IsLoaded || IsLoading)
                return;

            IsLoading = true;
            if (!isAsync)
            {
                int currentLine = -1;
                List<GrepLine> linesWithContext = new List<GrepLine>();
                if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext))
                    linesWithContext = result.GetLinesWithContext(GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore),
                    GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter));
                else
                    linesWithContext = result.GetLinesWithContext(0, 0);

                if (this.Count == 1 && this[0].GrepLine.LineNumber == -1)
                {
                    this.Clear();
                }

                for (int i = 0; i < linesWithContext.Count; i++)
                {
                    GrepLine line = linesWithContext[i];
                    bool isSectionBreak = false;

                    // Adding separator
                    if (this.Count > 0 && GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext) &&
                        (currentLine != line.LineNumber && currentLine + 1 != line.LineNumber))
                    {
                        isSectionBreak = true;
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

                    this.Add(new FormattedGrepLine(line, formattedResult, LineNumberColumnWidth, isSectionBreak));
                }
                IsLoaded = true;
                IsLoading = false;
            }
            else
            {
                int currentLine = -1;
                var asyncTask = Task.Factory.StartNew<List<GrepLine>>(() =>
                {
                    List<GrepLine> linesWithContext = new List<GrepLine>();
                    if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext))
                        linesWithContext = result.GetLinesWithContext(GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore),
                        GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter));
                    else
                        linesWithContext = result.GetLinesWithContext(0, 0);

                    return linesWithContext;
                }).ContinueWith(task =>
                {
                    if (this.Count == 1 && this[0].GrepLine.LineNumber == -1)
                    {
                        if (Application.Current != null)
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                                this.Clear()
                            ));
                    }

                    List<GrepLine> linesWithContext = task.Result;
                    List<FormattedGrepLine> tempList = new List<FormattedGrepLine>();
                    for (int i = 0; i < linesWithContext.Count; i++)
                    {
                        GrepLine line = linesWithContext[i];
                        bool isSectionBreak = false;

                        // Adding separator
                        if (tempList.Count > 0 && GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext) &&
                            (currentLine != line.LineNumber && currentLine + 1 != line.LineNumber))
                        {
                            isSectionBreak = true;
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
                        tempList.Add(new FormattedGrepLine(line, formattedResult, LineNumberColumnWidth, isSectionBreak));
                    }

                    if (Application.Current != null)
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            foreach (var l in tempList) this.Add(l);
                        }
                    ));
                    IsLoaded = true;
                    IsLoading = false;
                    LoadFinished?.Invoke(this, EventArgs.Empty);
                });
            }
        }

        public event EventHandler<PropertyChangedEventArgs> LineNumberColumnWidthChanged;
        public event EventHandler LoadFinished;

        protected void OnPropertyChanged(string name)
        {
            LineNumberColumnWidthChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
