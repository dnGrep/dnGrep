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
using DiffPlex.DiffBuilder.Model;
using dnGREP.Common;
using dnGREP.Engines;
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

            PreviewShowingReplacements = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreviewShowingReplacements);
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
                    SelectNextFile();
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
                    SelectPrevFile();
                }
            }
        }

        internal void MoveToMatch(int line, int column)
        {
            if (SelectedSearchResult != null && IndividualReplaceEnabled)
            {
                var lineMatch = SelectedSearchResult.SearchResults.Where(sr => sr.LineNumber == line)
                    .SelectMany(sr => sr.Matches)
                    .FirstOrDefault(m => m.DisplayStartLocation <= column && column <= m.EndPosition);

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

        public ObservableCollection<MenuItemViewModel> SyntaxItems { get; } = [];

        public Encoding Encoding { get; private set; } = Encoding.UTF8;

        public List<int> LineNumbers { get; } = [];

        public string FileText { get; private set; } = string.Empty;

        public DiffPaneModel? DiffModel { get; private set; }

        [ObservableProperty]
        private bool previewShowingReplacements = false;

        partial void OnPreviewShowingReplacementsChanged(bool value)
        {
            GrepSettings.Instance.Set(GrepSettings.Key.PreviewShowingReplacements, PreviewShowingReplacements);
            OnSelectedSearchResultChanged(SelectedSearchResult);
        }

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
                DiffModel = null;
                IndividualReplaceEnabled = true;
                string newLine = !string.IsNullOrEmpty(SelectedSearchResult.EOL) ? SelectedSearchResult.EOL : "\n";

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
                else if (PreviewShowingReplacements)
                {
                    StringBuilder sb = new();
                    using (FileStream inputStream = File.Open(SelectedSearchResult.FileNameReal, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using StreamReader readStream = new(inputStream, Encoding, false, 4096, true);
                        using EolReader eolReader = new(readStream);

                        string? lineText;
                        while (!eolReader.EndOfStream)
                        {
                            lineText = eolReader.ReadLine();
                            if (lineText != null)
                            {
                                sb.Append(lineText);
                            }
                        }
                    }

                    GenerateDiffPreview(sb.ToString());

                    if (DiffModel != null)
                    {
                        sb.Clear();
                        int lineNumber = 0;
                        int lineCount = 0;
                        for (int idx = 0; idx < DiffModel.Lines.Count; idx++)
                        {
                            DiffPiece piece = DiffModel.Lines[idx];
                            string line = piece.Text;
                            lineCount++;

                            if (piece.Position == null) // Deleted line
                                lineNumber++;
                            else
                                lineNumber = piece.Position.Value;

                            GrepLine? grepLine = SelectedSearchResult.SearchResults
                                .FirstOrDefault(sr => sr.LineNumber == lineNumber);
                            if (grepLine != null)
                            {
                                // this adjusts the location of the grep line in the newText 
                                // and accounting for all the inserted lines above it.
                                grepLine.DisplayFileLineNumber = lineCount;
                            }

                            if (line.Length > 7990 && grepLine != null)
                            {
                                sb.Append(ChopLongLines(line, grepLine, piece)).Append(newLine);
                                if (++idx < DiffModel.Lines.Count)
                                {
                                    piece = DiffModel.Lines[idx];
                                    line = piece.Text;
                                    sb.Append(ChopLongLines(line, grepLine, piece)).Append(newLine);
                                }
                            }
                            else
                            {
                                sb.Append(line).Append(newLine);
                            }
                        }
                    }

                    FileText = sb.ToString().TrimEndOfLine();
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
                            line.DisplayFileLineNumber = tempLineNum;

                            string lineText = line.LineText;
                            if (lineText.Length > 7990)
                            {
                                lineText = ChopLongLines(line.LineText, line, null);
                            }

                            sb.Append(lineText).Append(newLine);
                            LineNumbers.Add(line.LineNumber);

                            tempLineNum++;
                        }
                        FileText = sb.ToString();
                    }
                    else
                    {
                        StringBuilder sb = new();
                        using (FileStream inputStream = File.Open(SelectedSearchResult.FileNameReal, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using StreamReader readStream = new(inputStream, Encoding, false, 4096, true);
                            using EolReader eolReader = new(readStream);
                            string? lineText;
                            int lineNum = 1;
                            while (!eolReader.EndOfStream)
                            {
                                lineText = eolReader.ReadLine();
                                if (lineText != null)
                                {
                                    if (lineText.Length > 7990)
                                    {
                                        GrepLine? grepLine = SelectedSearchResult.SearchResults
                                            .FirstOrDefault(sr => sr.LineNumber == lineNum);

                                        if (grepLine != null)
                                        {
                                            lineText = ChopLongLines(lineText, grepLine, null);
                                        }
                                    }

                                    sb.Append(lineText);
                                }
                            }
                        }

                        FileText = sb.ToString();
                    }
                }

                LoadFile?.Invoke(this, EventArgs.Empty);
            }
        }

        private string ChopLongLines(string lineText, GrepLine grepLine, DiffPiece? piece)
        {
            if (grepLine.Matches.Count == 0)
            {
                return lineText;
            }

            // initial implementation, remove the sub-piece coloring
            // past where the line is chopped
            List<DiffPiece> pieces = [];
            if (piece != null)
            {
                pieces = piece.SubPieces;
            }

            StringBuilder sb = new();
            int maxLineLength = 8000 - 10;
            int matchCount = grepLine.Matches.Count;
            int matchCharacters = grepLine.Matches.Sum(m => m.Length);
            int ellipsisCharacters = BigEllipsisColorizer.ellipsis.Length * (matchCount + 1);
            int startContext = 40;
            int contextChars = Math.Min(100,
                (maxLineLength - matchCharacters - ellipsisCharacters - startContext) / matchCount / 2);

            // position of the last character captured from the lineText
            int position = 0;

            GrepMatch first = grepLine.Matches[0];
            if (first.StartLocation > contextChars)
            {
                sb.Append(lineText.AsSpan(0, startContext));
                sb.Append(BigEllipsisColorizer.ellipsis);
                position = startContext;

                RemoveSubPieces(pieces, position);
            }

            for (int idx = 0; idx < grepLine.Matches.Count; idx++)
            {
                GrepMatch match = grepLine.Matches[idx];
                GrepMatch? nextMatch = null;
                if (idx < grepLine.Matches.Count - 1)
                {
                    nextMatch = grepLine.Matches[idx + 1];
                }

                // context before
                int ctxStart = Math.Max(position, match.StartLocation - contextChars);
                sb.Append(lineText.AsSpan(ctxStart, match.StartLocation - ctxStart));
                position = match.StartLocation;

                // the match itself
                match.DisplayStartLocation = sb.Length;
                sb.Append(lineText.AsSpan(match.StartLocation, match.Length));
                position += match.Length;

                // context after
                if (nextMatch != null && nextMatch.StartLocation < position + contextChars)
                {
                    // the context between will be added by the next match
                }
                else if (nextMatch != null && nextMatch.StartLocation < position + 2 * contextChars)
                {
                    // add trailing context, but no ellipsis
                    // the rest of the context will be added by the next match
                    sb.Append(lineText.AsSpan(position, contextChars));
                    position += contextChars;
                }
                else if (position + contextChars < lineText.Length)
                {
                    sb.Append(lineText.AsSpan(position, contextChars));
                    sb.Append(BigEllipsisColorizer.ellipsis);
                    position += contextChars;

                    RemoveSubPieces(pieces, position);
                }
                else // at end of line
                {
                    sb.Append(lineText.AsSpan(position, lineText.Length - position));
                    position = lineText.Length;
                }
            }

            if (position < lineText.Length)
            {
                position = Math.Max(position, lineText.Length - 10);
                sb.Append(lineText.AsSpan(position, lineText.Length - position));
            }

            return sb.ToString();
        }

        private void RemoveSubPieces(List<DiffPiece> pieces, int position)
        {
            int last = -1;
            int endOffset = 0;
            foreach (var piece in pieces)
            {
                endOffset += string.IsNullOrEmpty(piece.Text) ? 0 : piece.Text.Length;
                if (endOffset > position && piece.Position != null)
                {
                    last = piece.Position.Value;
                    break;
                }
            }
            if (last > 0)
            {
                pieces.RemoveAll(p => p.Position >= last);
            }
        }

        public SearchType TypeOfSearch { get; set; }

        public GrepSearchOption SearchOptions { get; set; }

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
                    LineNumber = lineMatch.DisplayFileLineNumber;

                    ColNumber = lineMatch.Matches
                        .Where(m => m.FileMatchId == SelectedGrepMatch.FileMatchId)
                        .Select(m => m.DisplayStartLocation).FirstOrDefault();
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

                SelectNextFile();
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

        public ICommand ExternalDiffCommand => new RelayCommand(
            p => ExternalDiff(),
            q => SelectedSearchResult != null && CompareApplicationConfigured);

        private static bool CompareApplicationConfigured => GrepSettings.Instance.IsSet(GrepSettings.Key.CompareApplication);
        #endregion

        #region Replace Diff 

        private static GrepEngineInitParams InitParameters
        {
            get
            {
                return new GrepEngineInitParams(
                    GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesBefore),
                    GrepSettings.Instance.Get<int>(GrepSettings.Key.ContextLinesAfter),
                    GrepSettings.Instance.Get<double>(GrepSettings.Key.FuzzyMatchThreshold),
                    GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount),
                false);
            }
        }

        private void GenerateDiffPreview(string oldText)
        {
            if (SelectedSearchResult != null)
            {
                GrepEnginePlainText engine = new();
                engine.Initialize(InitParameters, FileFilter.Default);

                var replaceItems = SelectedSearchResult.Matches.Select(m => m.Copy(replaceMatch: true)).ToList();

                using Stream inputStream = new MemoryStream(Encoding.GetBytes(oldText));
                using Stream writeStream = new MemoryStream();
                engine.Replace(inputStream, writeStream, SearchFor, ReplaceWith, TypeOfSearch,
                    SearchOptions, Encoding, replaceItems);
                writeStream.Position = 0;
                using StreamReader reader = new(writeStream);
                string newText = reader.ReadToEnd();

                DiffModel = FileDifference.Diff(oldText, newText, replaceItems);

                int index = 1;
                foreach (DiffPiece line in DiffModel.Lines)
                {
                    if (line.Type == ChangeType.Inserted)
                    {
                        LineNumbers.Add(-1);
                    }
                    else
                    {
                        LineNumbers.Add(index++);
                    }
                }
            }
        }

        private void ExternalDiff()
        {
            if (SelectedSearchResult != null)
            {
                string originalFile = SelectedSearchResult.FileNameReal;
                string newFileName = Path.GetFileNameWithoutExtension(originalFile) + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(originalFile);
                string destinationFile = Path.Combine(Utils.GetTempFolder(), newFileName);

                GrepEnginePlainText engine = new();
                engine.Initialize(InitParameters, FileFilter.Default);

                var replaceItems = SelectedSearchResult.Matches.Select(m => m.Copy(replaceMatch: true)).ToList();

                using FileStream readStream = File.Open(originalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using FileStream writeStream = File.OpenWrite(destinationFile);
                engine.Replace(readStream, writeStream, SearchFor, ReplaceWith, TypeOfSearch,
                    SearchOptions, Encoding, replaceItems);

                Utils.CompareFiles([SelectedSearchResult.FileNameReal, destinationFile]);
            }
        }

        #endregion
    }
}
