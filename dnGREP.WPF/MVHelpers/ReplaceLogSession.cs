using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using dnGREP.Common;
using dnGREP.Engines;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using NLog;

namespace dnGREP.WPF.MVHelpers
{
    /// <summary>
    /// Builds the replacement log live while the Replace window is open and shows it in the
    /// Results pane.  A match is "decided" once the user has marked it, un-marked it, or moved
    /// past it; only decided matches appear in the log.  The log is rebuilt from scratch on
    /// every change, which keeps it simple and always consistent with the marks.
    /// </summary>
    internal class ReplaceLogSession(GrepSearchResultsViewModel resultsViewModel, List<GrepSearchResult> files,
        string searchFor, string replaceWith, SearchType searchType, GrepSearchOption searchOptions,
        Action<string> showStatus)
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // matches compare by position, which repeats from file to file: compare by reference
        private readonly HashSet<GrepMatch> decided = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<GrepSearchResult, string> fileTexts = [];
        private GrepMatch? lastSelected;

        /// <summary>
        /// Called when a mark changes in the Replace window (replace, undo, bulk marks).
        /// Every marked match is decided, as is the match under the cursor (covers Undo).
        /// </summary>
        public void OnReplaceMatch(object? sender, EventArgs e)
        {
            foreach (GrepSearchResult file in files) {
                foreach (GrepMatch match in file.Matches) {
                    if (match.ReplaceMatch) {
                        decided.Add(match);
                    }
                }
            }

            if (lastSelected != null) {
                decided.Add(lastSelected);
            }

            // always refresh: an Undo changes the log without adding a decided match
            Refresh();
        }

        /// <summary>
        /// Called when a property of the Replace window's view model changes.  When the cursor
        /// moves off a match without marking it, that match is decided: its replacement is skipped.
        /// </summary>
        public void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ReplaceViewModel.SelectedGrepMatch) || sender is not ReplaceViewModel vm) {
                return;
            }

            GrepMatch? previous = lastSelected;
            lastSelected = vm.SelectedGrepMatch;

            if (previous == null || ReferenceEquals(previous, lastSelected)) {
                return;
            }

            bool changed = decided.Add(previous);

            // moving to another file (Next/Previous File) skips the rest of the matches in the file left
            int previousFile = files.FindIndex(f => f.Matches.Any(m => ReferenceEquals(m, previous)));
            int currentFile = lastSelected == null ? -1
                : files.FindIndex(f => f.Matches.Any(m => ReferenceEquals(m, lastSelected)));
            if (previousFile >= 0 && currentFile >= 0 && currentFile != previousFile) {
                foreach (GrepMatch match in files[previousFile].Matches) {
                    changed |= decided.Add(match);
                }
            }

            if (changed) {
                Refresh();
            }
        }

        /// <summary>
        /// Rebuilds the log for every file that has a decided match and shows it in the Results pane.
        /// Does nothing until the first match is decided, so the search results stay until then.
        /// </summary>
        private void Refresh()
        {
            if (decided.Count == 0) {
                return;
            }

            List<ReplaceLogFile> logs = [];
            foreach (GrepSearchResult file in files) {
                if (!file.Matches.Any(decided.Contains)) {
                    continue;
                }

                try {
                    ReplaceLogFile log = ReplaceLogBuilder.Build(file.FileNameReal, GetText(file), file.Matches,
                        searchFor, replaceWith, searchType, searchOptions, null, decided.Contains);
                    if (log.Entries.Count > 0) {
                        logs.Add(log);
                    }
                }
                catch (Exception ex) {
                    logger.Error(ex, "Failure building the replacement log for " + file.FileNameReal);
                }
            }

            if (logs.Count > 0) {
                resultsViewModel.ShowLog(logs, searchFor);

                // totals for the whole log go to the status bar: there is room for them there
                showStatus(TranslationSource.Format(Resources.Main_ReplaceLog_StatusSummary,
                    logs.Sum(l => l.ReplacementCount), logs.Sum(l => l.SkippedCount),
                    logs.Sum(l => l.Entries.Count), logs.Count));
            }
        }

        /// <summary>
        /// Reads the file the way the search did (same encoding), once per file.
        /// </summary>
        private string GetText(GrepSearchResult file)
        {
            if (!fileTexts.TryGetValue(file, out string? text)) {
                using FileStream stream = new(file.FileNameReal, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new(stream, file.Encoding, false);
                text = reader.ReadToEnd();
                fileTexts[file] = text;
            }
            return text;
        }
    }
}
