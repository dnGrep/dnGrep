using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using dnGREP.Common;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnGREP.WPF
{
    /// <summary>
    /// View model for the replace dialog
    /// </summary>
    public class ReplaceViewModel : CultureAwareViewModel
    {
        public event EventHandler LoadFile;
        public event EventHandler ReplaceMatch;
        public event EventHandler CloseTrue;

        private int fileIndex = -1;
        private int matchIndex = -1;

        public ReplaceViewModel()
        {
            Highlighters = ThemedHighlightingManager.Instance.HighlightingNames.ToList();
            Highlighters.Sort();
            Highlighters.Insert(0, Resources.Replace_SyntaxNone);
            CurrentSyntax = Resources.Replace_SyntaxNone;
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            ReplaceFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.ReplaceFormFontSize);
            ResultsFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ResultsFontFamily);

            IsFullDialog = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFullReplaceDialog);
        }

        public void SelectNextFile()
        {
            if (SearchResults != null && fileIndex < SearchResults.Count - 1)
            {
                fileIndex++;
                FileNumber++;

                SelectedSearchResult = SearchResults[fileIndex];

                FormatFileStatus();
                FormatFileReplaceStatus();

                matchIndex = -1;
                if (IsFullDialog)
                {
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

                FormatFileStatus();
                FormatFileReplaceStatus();

                matchIndex = -1;
                if (IsFullDialog)
                {
                    SelectNextMatch();
                }
            }
        }

        private void FormatFileStatus()
        {
            var item = SelectedSearchResult;
            if (item != null)
            {
                string matchStr = string.Empty;
                string lineStr = string.Empty;
                int matchCount = item.Matches == null ? 0 : item.Matches.Count;
                if (matchCount > 0)
                {
                    var lineCount = item.Matches.Where(r => r.LineNumber > 0)
                       .Select(r => r.LineNumber).Distinct().Count();

                    matchStr = matchCount.ToString("N0", CultureInfo.CurrentCulture);
                    lineStr = lineCount.ToString("N0", CultureInfo.CurrentCulture);
                }

                string formattedText = TranslationSource.Format(Resources.Replace_FileNumberOfCountName,
                   FileNumber, FileCount, item.FileNameReal, matchStr, lineStr);

                if (Utils.IsReadOnly(item))
                {
                    formattedText += " " + Resources.Replace_ReadOnly;
                }

                FileStatus = formattedText;
            }
            else
            {
                FileStatus = string.Empty;
            }
        }

        private void FormatFileReplaceStatus()
        {
            var item = SelectedSearchResult;
            if (item != null)
            {
                string matchStr = string.Empty;
                string replaceStr = string.Empty;
                int matchCount = item.Matches == null ? 0 : item.Matches.Count;
                int replaceCount = 0;
                if (matchCount > 0)
                {
                    replaceCount = item.Matches.Count(r => r.ReplaceMatch);
                }

                matchStr = matchCount.ToString("N0", CultureInfo.CurrentCulture);
                replaceStr = replaceCount.ToString("N0", CultureInfo.CurrentCulture);

                string formattedText = TranslationSource.Format(Resources.Replace_NumberOfMatchesMarkedForReplacement,
                   replaceStr, matchStr);

                if (Utils.IsReadOnly(item))
                {
                    formattedText += " " + Resources.Replace_ReadOnly;
                }

                FileReplaceStatus = formattedText;
            }
            else
            {
                FileReplaceStatus = string.Empty;
            }
        }

        private void SelectNextMatch()
        {
            if (SelectedGrepMatch != null)
                SelectedGrepMatch = null;

            if (SelectedSearchResult != null && IndividualReplaceEnabled)
            {
                if (matchIndex < SelectedSearchResult.Matches.Count - 1)
                {
                    matchIndex++;
                    SelectedGrepMatch = SelectedSearchResult.Matches[matchIndex];
                }
                else
                {
                    matchIndex = 0;
                    SelectedGrepMatch = SelectedSearchResult.Matches[matchIndex];
                }
            }
        }

        private void SelectPrevMatch()
        {
            if (SelectedSearchResult != null && IndividualReplaceEnabled)
            {
                if (matchIndex > 0)
                {
                    matchIndex--;
                    SelectedGrepMatch = SelectedSearchResult.Matches[matchIndex];
                }
                else
                {
                    matchIndex = SelectedSearchResult.Matches.Count - 1;
                    SelectedGrepMatch = SelectedSearchResult.Matches[matchIndex];
                }
            }
        }

        internal void MoveToMatch(int line, int column)
        {
            if (SelectedSearchResult != null && IndividualReplaceEnabled)
            {
                var lineMatch = SelectedSearchResult.SearchResults.Where(sr => sr.LineNumber == line)
                    .SelectMany(sr => sr.Matches)
                    .FirstOrDefault(m => m.StartLocation <= column && column <= m.EndPosition);

                if (lineMatch != null)
                {
                    var match = SelectedSearchResult.Matches.FirstOrDefault(m => m.FileMatchId == lineMatch.FileMatchId);
                    if (match != null)
                    {
                        matchIndex = SelectedSearchResult.Matches.IndexOf(match);
                        SelectedGrepMatch = match;
                    }
                }
            }
        }

        private List<GrepSearchResult> _searchResults = null;
        public List<GrepSearchResult> SearchResults
        {
            get { return _searchResults; }
            set
            {
                if (_searchResults == value)
                    return;

                _searchResults = value;

                FileNumber = 0;
                FileCount = _searchResults.Count;
                fileIndex = -1;

                base.OnPropertyChanged(nameof(SearchResults));
            }
        }

        private GrepSearchResult _selectedSearchResult = null;
        public GrepSearchResult SelectedSearchResult
        {
            get { return _selectedSearchResult; }
            set
            {
                if (_selectedSearchResult == value)
                    return;

                _selectedSearchResult = value;
                base.OnPropertyChanged(nameof(SelectedSearchResult));

                IndividualReplaceEnabled = false;

                if (IsFullDialog)
                {
                    CurrentSyntax = Resources.Replace_SyntaxNone; // by default, turn off syntax highlighting (easier to see the match highlights)
                    Encoding = _selectedSearchResult.Encoding;
                    LineNumbers.Clear();
                    FileText = string.Empty;
                    FilePath = string.Empty;
                    IndividualReplaceEnabled = true;

                    FileInfo fileInfo = new FileInfo(_selectedSearchResult.FileNameReal);
                    if (Utils.IsBinary(_selectedSearchResult.FileNameReal))
                    {
                        FileText = Resources.Replace_ErrorThisIsABinaryFile;
                        IndividualReplaceEnabled = false;
                    }
                    else if (_selectedSearchResult.Matches.Count > 5000)
                    {
                        FileText = Resources.Replace_ThisFileContainsTooManyMatchesForIndividualReplace;
                        IndividualReplaceEnabled = false;
                    }
                    else
                    {
                        if (fileInfo.Length > 50000)
                        {
                            // Large files:  
                            // Use the already parsed matches and context lines only
                            // and map the original line numbers to clipped file

                            StringBuilder sb = new StringBuilder();
                            int tempLineNum = 1;
                            foreach (var line in _selectedSearchResult.SearchResults)
                            {
                                line.ClippedFileLineNumber = tempLineNum;
                                sb.Append(line.LineText).Append(_selectedSearchResult.EOL);
                                LineNumbers.Add(line.LineNumber);

                                tempLineNum++;
                            }
                            FileText = sb.ToString();
                        }
                        else
                        {
                            FilePath = _selectedSearchResult.FileNameReal;
                        }
                    }

                    LoadFile?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public List<string> Highlighters { get; private set; }

        public Encoding Encoding { get; private set; }

        public IList<int> LineNumbers { get; } = new List<int>();

        public string FilePath { get; private set; }

        public string FileText { get; private set; }

        private GrepMatch _selectedGrepMatch = null;
        public GrepMatch SelectedGrepMatch
        {
            get { return _selectedGrepMatch; }
            set
            {
                if (_selectedGrepMatch == value)
                    return;

                _selectedGrepMatch = value;

                if (_selectedGrepMatch != null)
                {
                    var lineMatch = _selectedSearchResult.SearchResults
                        .FirstOrDefault(sr => sr.Matches.Any(m => m.FileMatchId == _selectedGrepMatch.FileMatchId));

                    if (lineMatch != null)
                    {
                        LineNumber = lineMatch.ClippedFileLineNumber;

                        ColNumber = lineMatch.Matches
                            .Where(m => m.FileMatchId == _selectedGrepMatch.FileMatchId)
                            .Select(m => m.StartLocation).FirstOrDefault();
                    }
                }

                base.OnPropertyChanged(nameof(SelectedGrepMatch));
            }
        }

        private bool isFullDialog = true;
        public bool IsFullDialog
        {
            get { return isFullDialog; }
            set
            {
                if (isFullDialog == value)
                    return;

                isFullDialog = value;

                base.OnPropertyChanged(nameof(IsFullDialog));

                DialogSize = isFullDialog ? new Size(980, 800) : new Size(680, 340);
            }
        }

        private Size dialogSize = new Size(980, 800);
        public Size DialogSize
        {
            get { return dialogSize; }
            set
            {
                if (dialogSize == value)
                    return;

                dialogSize = value;

                base.OnPropertyChanged(nameof(DialogSize));
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

                base.OnPropertyChanged(nameof(SearchFor));
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

                base.OnPropertyChanged(nameof(ReplaceWith));
            }
        }

        private string fileStatus;
        public string FileStatus
        {
            get { return fileStatus; }
            set
            {
                if (value == fileStatus)
                    return;

                fileStatus = value;

                base.OnPropertyChanged(nameof(FileStatus));
            }
        }

        private string fileReplaceStatus;
        public string FileReplaceStatus
        {
            get { return fileReplaceStatus; }
            set
            {
                if (value == fileReplaceStatus)
                    return;

                fileReplaceStatus = value;

                base.OnPropertyChanged(nameof(FileReplaceStatus));
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
                base.OnPropertyChanged(nameof(FileCount));
                FormatFileStatus();
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
                base.OnPropertyChanged(nameof(FileNumber));
                FormatFileStatus();
            }
        }

        private string currentSyntax;
        public string CurrentSyntax
        {
            get { return currentSyntax; }
            set
            {
                if (value == currentSyntax)
                    return;

                currentSyntax = value;

                base.OnPropertyChanged(nameof(CurrentSyntax));
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

                base.OnPropertyChanged(nameof(LineNumber));
            }
        }

        private int colNumber;
        public int ColNumber
        {
            get { return colNumber; }
            set
            {
                if (value == colNumber)
                    return;

                colNumber = value;

                base.OnPropertyChanged(nameof(ColNumber));
            }
        }

        private bool individualReplaceEnabled;
        public bool IndividualReplaceEnabled
        {
            get { return individualReplaceEnabled; }
            set
            {
                if (value == individualReplaceEnabled)
                    return;

                individualReplaceEnabled = value;

                base.OnPropertyChanged(nameof(IndividualReplaceEnabled));
            }
        }

        public IHighlightingDefinition HighlightingDefinition
        {
            get
            {
                return ThemedHighlightingManager.Instance.GetDefinition(CurrentSyntax);
            }
        }

        private string applicationFontFamily;
        public string ApplicationFontFamily
        {
            get { return applicationFontFamily; }
            set
            {
                if (applicationFontFamily == value)
                    return;

                applicationFontFamily = value;
                base.OnPropertyChanged(nameof(ApplicationFontFamily));
            }
        }

        private double replaceFormfontSize;
        public double ReplaceFormFontSize
        {
            get { return replaceFormfontSize; }
            set
            {
                if (replaceFormfontSize == value)
                    return;

                replaceFormfontSize = value;
                base.OnPropertyChanged(nameof(ReplaceFormFontSize));
            }
        }

        private string resultsFontFamily;
        public string ResultsFontFamily
        {
            get { return resultsFontFamily; }
            set
            {
                if (resultsFontFamily == value)
                    return;

                resultsFontFamily = value;
                base.OnPropertyChanged(nameof(ResultsFontFamily));
            }
        }

        #region Commands

        private void ReplaceAll()
        {
            if (SearchResults != null)
            {
                foreach (GrepSearchResult gsr in SearchResults)
                {
                    foreach (var m in gsr.Matches)
                        m.ReplaceMatch = true;
                }
                FormatFileReplaceStatus();

                ReplaceMatch?.Invoke(this, EventArgs.Empty);
            }

            CloseTrue?.Invoke(this, EventArgs.Empty);
        }

        private void MarkAllInFile()
        {
            if (SelectedSearchResult != null)
            {
                foreach (var match in SelectedSearchResult.Matches)
                {
                    match.ReplaceMatch = true;
                }
                FormatFileReplaceStatus();

                ReplaceMatch?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UndoAllMarksInFile()
        {
            if (SelectedSearchResult != null)
            {
                foreach (var match in SelectedSearchResult.Matches)
                {
                    match.ReplaceMatch = false;
                }
                FormatFileReplaceStatus();

                ReplaceMatch?.Invoke(this, EventArgs.Empty);
            }
        }

        private void MarkMatchForReplace()
        {
            if (SelectedGrepMatch != null)
            {
                SelectedGrepMatch.ReplaceMatch = true;
                FormatFileReplaceStatus();
                ReplaceMatch?.Invoke(this, EventArgs.Empty);
            }

            SelectNextMatch();
        }

        private void UndoMarkMatchForReplace()
        {
            if (SelectedGrepMatch != null)
            {
                SelectedGrepMatch.ReplaceMatch = false;
                FormatFileReplaceStatus();
                ReplaceMatch?.Invoke(this, EventArgs.Empty);
            }
        }

        public ICommand ReplaceAllCommand => new RelayCommand(
            p => ReplaceAll(),
            q => SearchResults != null);

        public ICommand NextFileCommand => new RelayCommand(
            p => SelectNextFile(),
            q => SearchResults != null && SearchResults.Count > 1 && fileIndex < SearchResults.Count - 1);

        public ICommand PrevFileCommand => new RelayCommand(
            p => SelectPrevFile(),
            q => SearchResults != null && SearchResults.Count > 1 && fileIndex > 0);

        public ICommand PrevMatchCommand => new RelayCommand(
            p => SelectPrevMatch(),
            q => SelectedSearchResult != null && SelectedSearchResult.Matches.Count > 1 && IndividualReplaceEnabled);

        public ICommand NextMatchCommand => new RelayCommand(
            p => SelectNextMatch(),
            q => SelectedSearchResult != null && SelectedSearchResult.Matches.Count > 1 && IndividualReplaceEnabled);

        public ICommand ReplaceAllInFileCommand => new RelayCommand(
            p => MarkAllInFile(),
            q => SelectedSearchResult != null && !SelectedSearchResult.Matches.All(m => m.ReplaceMatch));

        public ICommand UndoFileCommand => new RelayCommand(
            p => UndoAllMarksInFile(),
            q => SelectedSearchResult != null && SelectedSearchResult.Matches.Any(m => m.ReplaceMatch));

        public ICommand ReplaceMatchCommand => new RelayCommand(
            p => MarkMatchForReplace(),
            q => SelectedGrepMatch != null && !SelectedGrepMatch.ReplaceMatch);

        public ICommand UndoMatchCommand => new RelayCommand(
            p => UndoMarkMatchForReplace(),
            q => SelectedGrepMatch != null && SelectedGrepMatch.ReplaceMatch);

        #endregion
    }
}
