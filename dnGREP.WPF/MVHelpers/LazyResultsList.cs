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

        public event EventHandler<PropertyChangedEventArgs>? LineNumberColumnWidthChanged;
        public event EventHandler? LoadFinished;

        public bool IsLoaded { get; private set; }
        public bool IsLoading { get; private set; }

        public LazyResultsList(GrepSearchResult result, FormattedGrepResult formattedResult)
        {
            this.result = result;
            this.formattedResult = formattedResult;

            if ((result.Matches != null && result.Matches.Count > 0) || !result.IsSuccess)
            {
                GrepLine emptyLine = new(-1, "", true, null);
                var dummyLine = new FormattedGrepLine(emptyLine, formattedResult, 30, false);
                Add(dummyLine);
                IsLoaded = false;
            }
        }

        private int lineNumberColumnWidth = 30;
        public int LineNumberColumnWidth
        {
            get { return lineNumberColumnWidth; }
            set { lineNumberColumnWidth = value; OnPropertyChanged(nameof(LineNumberColumnWidth)); }
        }

        public bool Load()
        {
            if (IsLoaded || IsLoading)
                return false;

            IsLoading = true;

            List<GrepLine> linesWithContext;
            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext))
            {
                linesWithContext = result.GetLinesWithContext(GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore),
                        GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter));
            }
            else
            {
                linesWithContext = result.GetLinesWithContext(0, 0);
            }

            FormatAndLoadLines(linesWithContext);

            IsLoaded = true;
            IsLoading = false;
            LoadFinished?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public async Task<bool> LoadAsync()
        {
            if (IsLoaded || IsLoading)
                return false;

            IsLoading = true;

            List<GrepLine> linesWithContext = await Task.Run(() =>
            {
                List<GrepLine> list;
                if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowLinesInContext))
                {
                    list = result.GetLinesWithContext(
                        GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore),
                        GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter));
                }
                else
                {
                    list = result.GetLinesWithContext(0, 0);
                }

                return list;
            });

            FormatAndLoadLines(linesWithContext);

            IsLoaded = true;
            IsLoading = false;
            LoadFinished?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private void FormatAndLoadLines(List<GrepLine> linesWithContext)
        {
            int currentLine = -1;

            if (Count == 1 && this[0].GrepLine.LineNumber == -1)
            {
                Application.Current?.Dispatcher.Invoke(new Action(Clear));
            }

            List<FormattedGrepLine> tempList = new();
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
                if (line.IsHexFile)
                    LineNumberColumnWidth = 80;
                else if (currentLine <= 999 && LineNumberColumnWidth < 30)
                    LineNumberColumnWidth = 30;
                else if (currentLine > 999 && LineNumberColumnWidth < 35)
                    LineNumberColumnWidth = 35;
                else if (currentLine > 9999 && LineNumberColumnWidth < 47)
                    LineNumberColumnWidth = 47;
                else if (currentLine > 99999 && LineNumberColumnWidth < 50)
                    LineNumberColumnWidth = 50;

                tempList.Add(new FormattedGrepLine(line, formattedResult, LineNumberColumnWidth, isSectionBreak));
            }

            Application.Current?.Dispatcher.Invoke(new(() =>
                {
                    foreach (var l in tempList)
                    {
                        Add(l);
                    }
                }
            ));
        }

        protected void OnPropertyChanged(string name)
        {
            LineNumberColumnWidthChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
