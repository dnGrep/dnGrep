using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnGREP.WPF
{
    /// <summary>
    /// View model for the replace dialog
    /// </summary>
    public partial class ReplaceViewModel : CultureAwareViewModel
    {
        public event EventHandler? LoadFile;
        public event EventHandler? ReplaceMatch;
        public event EventHandler? CloseTrue;

        private int fileIndex = -1;
        private int matchIndex = -1;

        public ReplaceViewModel()
        {
            var items = ThemedHighlightingManager.Instance.HighlightingNames;
            var grouping = items.OrderBy(s => s)
                .GroupBy(s => s[0])
                .Select(g => new { g.Key, Items = g.ToArray() });

            string noneItem = Resources.Replace_SyntaxNone;
            SyntaxItems.Add(new MenuItemViewModel(noneItem, true,
                new RelayCommand(p => CurrentSyntax = noneItem)));

            foreach (var group in grouping)
            {
                var parent = new MenuItemViewModel(group.Key.ToString(), null);
                SyntaxItems.Add(parent);

                foreach (var child in group.Items)
                {
                    parent.Children.Add(new MenuItemViewModel(child, true,
                        new RelayCommand(p => CurrentSyntax = child)));
                }
            }

            CurrentSyntax = Resources.Replace_SyntaxNone;
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            ReplaceFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.ReplaceFormFontSize);
            ResultsFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ResultsFontFamily);

            IsFullDialog = GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFullReplaceDialog);

            RestoreLastModifiedDate = GrepSettings.Instance.Get<bool>(GrepSettings.Key.RestoreLastModifiedDate);
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
                int matchCount = item.Matches.Count;
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
                int matchCount = item.Matches.Count;
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

        public IHighlightingDefinition? HighlightingDefinition =>
            ThemedHighlightingManager.Instance.GetDefinition(CurrentSyntax);

        public ObservableCollection<MenuItemViewModel> SyntaxItems { get; } = new();

        public Encoding? Encoding { get; private set; }

        public IList<int> LineNumbers { get; } = new List<int>();

        public string FilePath { get; private set; } = string.Empty;

        public string FileText { get; private set; } = string.Empty;

        [ObservableProperty]
        private List<GrepSearchResult>? searchResults = null;
        partial void OnSearchResultsChanged(List<GrepSearchResult>? value)
        {
            FileNumber = 0;
            FileCount = SearchResults?.Count ?? 0;
            fileIndex = -1;
        }

        [ObservableProperty]
        private GrepSearchResult? selectedSearchResult = null;
        partial void OnSelectedSearchResultChanged(GrepSearchResult? value)
        {
            IndividualReplaceEnabled = false;

            if (IsFullDialog && SelectedSearchResult != null)
            {
                CurrentSyntax = Resources.Replace_SyntaxNone; // by default, turn off syntax highlighting (easier to see the match highlights)
                Encoding = SelectedSearchResult.Encoding;
                LineNumbers.Clear();
                FileText = string.Empty;
                FilePath = string.Empty;
                IndividualReplaceEnabled = true;

                FileInfo fileInfo = new(SelectedSearchResult.FileNameReal);
                if (Utils.IsBinary(SelectedSearchResult.FileNameReal))
                {
                    FileText = Resources.Replace_ErrorThisIsABinaryFile;
                    IndividualReplaceEnabled = false;
                }
                else if (SelectedSearchResult.Matches.Count > 5000)
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

                        StringBuilder sb = new();
                        int tempLineNum = 1;
                        foreach (var line in SelectedSearchResult.SearchResults)
                        {
                            line.ClippedFileLineNumber = tempLineNum;
                            sb.Append(line.LineText).Append(SelectedSearchResult.EOL);
                            LineNumbers.Add(line.LineNumber);

                            tempLineNum++;
                        }
                        FileText = sb.ToString();
                    }
                    else
                    {
                        FilePath = SelectedSearchResult.FileNameReal;
                    }
                }

                LoadFile?.Invoke(this, EventArgs.Empty);
            }
        }

        [ObservableProperty]
        private GrepMatch? selectedGrepMatch = null;
        partial void OnSelectedGrepMatchChanged(GrepMatch? value)
        {
            if (SelectedGrepMatch != null && SelectedSearchResult != null)
            {
                var lineMatch = SelectedSearchResult.SearchResults
                    .FirstOrDefault(sr => sr.Matches.Any(m => m.FileMatchId == SelectedGrepMatch.FileMatchId));

                if (lineMatch != null)
                {
                    LineNumber = lineMatch.ClippedFileLineNumber;

                    ColNumber = lineMatch.Matches
                        .Where(m => m.FileMatchId == SelectedGrepMatch.FileMatchId)
                        .Select(m => m.StartLocation).FirstOrDefault();
                }
            }
        }

        [ObservableProperty]
        private bool isFullDialog = true;
        partial void OnIsFullDialogChanged(bool value)
        {
            DialogSize = IsFullDialog ? new Size(980, 800) : new Size(680, 340);
        }

        [ObservableProperty]
        private Size dialogSize = new(980, 800);

        [ObservableProperty]
        private string searchFor = string.Empty;

        [ObservableProperty]
        private string replaceWith = string.Empty;

        [ObservableProperty]
        private string fileStatus = string.Empty;

        [ObservableProperty]
        private string fileReplaceStatus = string.Empty;

        [ObservableProperty]
        private int fileCount = 0;
        partial void OnFileCountChanged(int value)
        {
            FormatFileStatus();
        }

        [ObservableProperty]
        private int fileNumber = 0;
        partial void OnFileNumberChanged(int value)
        {
            FormatFileStatus();
        }

        [ObservableProperty]
        private string currentSyntax = string.Empty;
        partial void OnCurrentSyntaxChanged(string value)
        {
            SelectCurrentSyntax(value);
        }

        [ObservableProperty]
        private int lineNumber;

        [ObservableProperty]
        private int colNumber;

        [ObservableProperty]
        private bool individualReplaceEnabled;

        [ObservableProperty]
        private bool restoreLastModifiedDate = false;
        partial void OnRestoreLastModifiedDateChanged(bool value)
        {
            GrepSettings.Instance.Set(GrepSettings.Key.RestoreLastModifiedDate, RestoreLastModifiedDate);
        }

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double replaceFormFontSize;

        [ObservableProperty]
        private string resultsFontFamily = GrepSettings.DefaultMonospaceFontFamily;

        private void SelectCurrentSyntax(string syntaxName)
        {
            // creates a radio group for all the syntax context menu items
            foreach (var item in SyntaxItems)
            {
                if (item.IsCheckable)
                {
                    item.IsChecked = item.Header.Equals(syntaxName, StringComparison.Ordinal);
                }

                foreach (var child in item.Children)
                {
                    child.IsChecked = child.Header.Equals(syntaxName, StringComparison.Ordinal);
                }
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
