using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class ReplaceViewModel : ViewModelBase
    {
        public event EventHandler<LineChangedEventArgs> LineChanged;
        public event EventHandler CloseTrue;

        private int fileIndex = -1;
        private int matchIndex = -1;

        private void SelectNextFile()
        {
            if (SearchResults != null && fileIndex < SearchResults.Count - 1)
            {
                fileIndex++;
                FileNumber++;

                SelectedSearchResult = SearchResults[fileIndex];
                if (!SelectedSearchResult.FormattedLines.IsLoaded && !SelectedSearchResult.FormattedLines.IsLoading)
                {
                    SelectedSearchResult.FormattedLines.LoadFinished += FormattedLines_LoadFinished;
                    SelectedSearchResult.IsLoading = true;
                    SelectedSearchResult.FormattedLines.Load(true);
                }
                else
                {
                    FileLabel = SelectedSearchResult.Label;

                    matchIndex = -1;
                    SelectNextMatch();
                }
            }
        }

        private void SelectPrevFile()
        {
            if (SearchResults != null && fileIndex > 0)
            {
                fileIndex--;
                FileNumber--;

                SelectedSearchResult = SearchResults[fileIndex];
                if (!SelectedSearchResult.FormattedLines.IsLoaded && !SelectedSearchResult.FormattedLines.IsLoading)
                {
                    SelectedSearchResult.FormattedLines.LoadFinished += FormattedLines_LoadFinished;
                    SelectedSearchResult.IsLoading = true;
                    SelectedSearchResult.FormattedLines.Load(true);
                }
                else
                {
                    FormatLabel();

                    matchIndex = -1;
                    SelectNextMatch();
                }
            }
        }

        private void FormattedLines_LoadFinished(object sender, EventArgs e)
        {
            FormatLabel();

            matchIndex = -1;
            SelectNextMatch();
        }

        private void FormatLabel()
        {
            string label = string.Empty;

            var item = SelectedSearchResult;
            if (item != null)
            {
                int matchCount = (item.GrepResult.Matches == null ? 0 : item.GrepResult.Matches.Count);
                if (matchCount > 0)
                {
                        var lineCount = item.GrepResult.Matches.Where(r => r.LineNumber > 0)
                           .Select(r => r.LineNumber).Distinct().Count();
                        label = $"{item.FilePath}  ({matchCount} matches on {lineCount} lines)";
                }

                if (Utils.IsReadOnly(item.GrepResult))
                {
                    label += " [read-only]";
                }
            }

            FileLabel = label;
        }

        private void SelectNextMatch()
        {
            if (SelectedGrepMatch != null)
                SelectedGrepMatch = null;

            if (SelectedSearchResult != null)
            {
                if (matchIndex < SelectedSearchResult.FormattedMatches.Count - 1)
                {
                    matchIndex++;
                    SelectedGrepMatch = SelectedSearchResult.FormattedMatches[matchIndex];
                }
                else
                {
                    matchIndex = 0;
                    SelectedGrepMatch = SelectedSearchResult.FormattedMatches[matchIndex];
                }
                //else  ?? advance to next file?
                //{
                //    matchIndex = -1;
                //    SelectNextFile();
                //}
            }
        }

        private void SelectPrevMatch()
        {
            if (SelectedSearchResult != null)
            {
                if (matchIndex > 0)
                {
                    matchIndex--;
                    SelectedGrepMatch = SelectedSearchResult.FormattedMatches[matchIndex];
                }
                else
                {
                    matchIndex = SelectedSearchResult.FormattedMatches.Count - 1;
                    SelectedGrepMatch = SelectedSearchResult.FormattedMatches[matchIndex];
                }
            }
        }

        private ObservableGrepSearchResults _searchResults = null;
        public ObservableGrepSearchResults SearchResults
        {
            get { return _searchResults; }
            set
            {
                if (_searchResults == value)
                    return;

                _searchResults = value;

                foreach (var result in _searchResults)
                {
                    result.InitializeReplace();
                }

                FileNumber = 0;
                FileCount = _searchResults.Count;
                fileIndex = -1;
                SelectNextFile();

                base.OnPropertyChanged(() => SearchResults);
            }
        }

        private FormattedGrepResult _selectedSearchResult = null;
        public FormattedGrepResult SelectedSearchResult
        {
            get { return _selectedSearchResult; }
            set
            {
                if (_selectedSearchResult == value)
                    return;

                _selectedSearchResult = value;
                base.OnPropertyChanged(() => SelectedSearchResult);
            }
        }

        private FormattedGrepMatch _selectedGrepMatch = null;
        public FormattedGrepMatch SelectedGrepMatch
        {
            get { return _selectedGrepMatch; }
            set
            {
                if (_selectedGrepMatch == value)
                    return;

                if (_selectedGrepMatch != null)
                {
                    _selectedGrepMatch.IsSelected = false;
                }

                _selectedGrepMatch = value;

                if (_selectedGrepMatch != null)
                {
                    _selectedGrepMatch.IsSelected = true;

                    var item = SelectedSearchResult.FormattedLines.Select((fmtLine, Index) => new { fmtLine, Index })
                        .FirstOrDefault(a => a.fmtLine.GrepLine.LineNumber == _selectedGrepMatch.Match.LineNumber);

                    if (item != null)
                        LineChanged?.Invoke(this, new LineChangedEventArgs { LineIndex = item.Index });
                }

                base.OnPropertyChanged(() => SelectedGrepMatch);
            }
        }

        private string searchFor = string.Empty;
        public string SearchFor
        {
            get { return searchFor; }
            set
            {
                if (value == searchFor)
                    return;

                searchFor = value;

                base.OnPropertyChanged(() => SearchFor);
            }
        }

        private string replaceWith = string.Empty;
        public string ReplaceWith
        {
            get { return replaceWith; }
            set
            {
                if (value == replaceWith)
                    return;

                replaceWith = value;

                base.OnPropertyChanged(() => ReplaceWith);
            }
        }

        private int fileCount = 0;
        public int FileCount
        {
            get { return fileCount; }
            set
            {
                if (fileCount == value)
                    return;

                fileCount = value;
                base.OnPropertyChanged(() => FileCount);
            }
        }

        private int fileNumber = 0;
        public int FileNumber
        {
            get { return fileNumber; }
            set
            {
                if (fileNumber == value)
                    return;

                fileNumber = value;
                base.OnPropertyChanged(() => FileNumber);
            }
        }

        private string fileLabel = string.Empty;
        public string FileLabel
        {
            get { return fileLabel; } 
            set
            {
                if (fileLabel == value)
                {
                    return;
                }

                fileLabel = value;
                base.OnPropertyChanged(() => FileLabel);
            }
        }

        private void ReplaceAll()
        {
            if (SearchResults != null)
            {
                foreach (FormattedGrepResult fmtResult in SearchResults)
                {
                    foreach (var m in fmtResult.GrepResult.Matches)
                        m.ReplaceMatch = true;
                }
            }

            CloseTrue?.Invoke(this, EventArgs.Empty);
        }

        private void ReplaceAllInFile()
        {
            if (SelectedSearchResult != null)
            {
                foreach (var match in SelectedSearchResult.FormattedMatches)
                {
                    match.ReplaceMatch = true;
                }
            }

            //SelectNextFile();  // auto advance?
        }

        private void UndoFile()
        {
            if (SelectedSearchResult != null)
            {
                foreach (var match in SelectedSearchResult.FormattedMatches)
                {
                    match.ReplaceMatch = false;
                }
            }
        }

        private void ReplaceOccurance()
        {
            if (SelectedGrepMatch != null)
            {
                SelectedGrepMatch.ReplaceMatch = true;
            }

            SelectNextMatch();
        }

        private void UndoOccurance()
        {
            if (SelectedGrepMatch != null)
            {
                SelectedGrepMatch.ReplaceMatch = false;
            }
        }

        private RelayCommand _replaceAllCommand;
        public ICommand ReplaceAllCommand
        {
            get
            {
                if (_replaceAllCommand == null)
                {
                    _replaceAllCommand = new RelayCommand(
                        p => ReplaceAll(),
                        q => SearchResults != null
                        );
                }
                return _replaceAllCommand;
            }
        }

        private RelayCommand _nextFileCommand;
        public ICommand NextFileCommand
        {
            get
            {
                if (_nextFileCommand == null)
                {
                    _nextFileCommand = new RelayCommand(
                        p => SelectNextFile(),
                        q => SearchResults != null && SearchResults.Count > 1 && fileIndex < SearchResults.Count - 1
                        );
                }
                return _nextFileCommand;
            }
        }

        private RelayCommand _prevFileCommand;
        public ICommand PrevFileCommand
        {
            get
            {
                if (_prevFileCommand == null)
                {
                    _prevFileCommand = new RelayCommand(
                        p => SelectPrevFile(),
                        q => SearchResults != null && SearchResults.Count > 1 && fileIndex > 0
                        );
                }
                return _prevFileCommand;
            }
        }

        private RelayCommand _prevOccuranceCommand;
        public ICommand PrevOccuranceCommand
        {
            get
            {
                if (_prevOccuranceCommand == null)
                {
                    _prevOccuranceCommand = new RelayCommand(
                        p => SelectPrevMatch(),
                        q => SelectedSearchResult != null && SelectedSearchResult.FormattedMatches.Count > 1 //&& matchIndex > 0
                        );
                }
                return _prevOccuranceCommand;
            }
        }

        private RelayCommand _nextOccuranceCommand;
        public ICommand NextOccuranceCommand
        {
            get
            {
                if (_nextOccuranceCommand == null)
                {
                    _nextOccuranceCommand = new RelayCommand(
                        p => SelectNextMatch(),
                        q => SelectedSearchResult != null && SelectedSearchResult.FormattedMatches.Count > 1 //&& matchIndex < SelectedSearchResult.FormattedMatches.Count - 1
                        );
                }
                return _nextOccuranceCommand;
            }
        }

        private RelayCommand _replaceAllInFileCommand;
        public ICommand ReplaceAllInFileCommand
        {
            get
            {
                if (_replaceAllInFileCommand == null)
                {
                    _replaceAllInFileCommand = new RelayCommand(
                        p => ReplaceAllInFile(),
                        q => SelectedSearchResult != null && !SelectedSearchResult.FormattedMatches.All(m => m.ReplaceMatch)
                        );
                }
                return _replaceAllInFileCommand;
            }
        }

        RelayCommand _undoFileCommand;
        public ICommand UndoFileCommand
        {
            get
            {
                if (_undoFileCommand == null)
                {
                    _undoFileCommand = new RelayCommand(
                        param => this.UndoFile(),
                        param => SelectedSearchResult != null && SelectedSearchResult.FormattedMatches.Any(m => m.ReplaceMatch)
                        );
                }
                return _undoFileCommand;
            }
        }

        private RelayCommand _replaceOccuranceCommand;
        public ICommand ReplaceOccuranceCommand
        {
            get
            {
                if (_replaceOccuranceCommand == null)
                {
                    _replaceOccuranceCommand = new RelayCommand(
                        p => ReplaceOccurance(),
                        q => SelectedGrepMatch != null && !SelectedGrepMatch.ReplaceMatch
                        );
                }
                return _replaceOccuranceCommand;
            }
        }

        RelayCommand _undoOccuranceCommand;
        public ICommand UndoOccuranceCommand
        {
            get
            {
                if (_undoOccuranceCommand == null)
                {
                    _undoOccuranceCommand = new RelayCommand(
                        param => UndoOccurance(),
                        param => SelectedGrepMatch != null && SelectedGrepMatch.ReplaceMatch
                        );
                }
                return _undoOccuranceCommand;
            }
        }
    }

    public class LineChangedEventArgs : EventArgs
    {
        public int LineIndex { get; set; }
    }
}
