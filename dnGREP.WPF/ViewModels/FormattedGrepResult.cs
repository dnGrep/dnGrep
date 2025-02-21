using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using dnGREP.WPF.MVHelpers;
using dnGREP.WPF.UserControls;

namespace dnGREP.WPF
{
    public partial class FormattedGrepResult : CultureAwareViewModel, ITreeItem
    {
        public GrepSearchResult GrepResult { get; private set; } = new();

        public int Matches
        {
            get { return GrepResult.Matches.Count; }
        }

        public string Style { get; private set; } = string.Empty;

        [ObservableProperty]
        private string label = string.Empty;

        private readonly List<string> matchIdx = [];

        /// <summary>
        /// Returns an ordinal based on the match id
        /// </summary>
        internal int GetMatchNumber(string id)
        {
            int index = matchIdx.IndexOf(id);
            if (index > -1)
                return index + 1;

            matchIdx.Add(id);
            return matchIdx.Count;
        }

        internal Dictionary<string, string> GroupColors { get; } = [];

        public static bool ShowFileInfoTooltips
        {
            get { return GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFileInfoTooltips); }
        }

        // some settings have changed, raise property changed events to update the UI
        public void RaiseSettingsPropertiesChanged()
        {
            OnPropertyChanged(nameof(ShowFileInfoTooltips));
        }

        public async Task ExpandTreeNode()
        {
            if (!FormattedLines.IsLoaded)
            {
                IsLoading = true;
                await FormattedLines.LoadAsync();
                IsLoading = false;
            }
            IsExpanded = true;
        }

        internal void CollapseTreeNode()
        {
            IsExpanded = false;
        }

        [ObservableProperty]
        private bool isExpanded = false;
        partial void OnIsExpandedChanged(bool value)
        {
            if (value == true && !FormattedLines.IsLoaded && !FormattedLines.IsLoading)
            {
                IsLoading = true;
                Task.Run(() => FormattedLines.LoadAsync());
            }
        }

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isSelected;
        partial void OnIsSelectedChanged(bool value)
        {
            GrepSearchResultsViewModel.SearchResultsMessenger.NotifyColleagues("IsSelectedChanged", this);
        }

        [ObservableProperty]
        private int lineNumberColumnWidth = 30;

        public BitmapSource? Icon { get; set; }

        public LazyResultsList FormattedLines { get; private set; }

        private readonly string searchFolderPath;

        public FormattedGrepResult(GrepSearchResult result, string folderPath)
        {
            GrepResult = result;

            searchFolderPath = folderPath;
            SetLabel();

            FormattedLines = new LazyResultsList(result, this);
            FormattedLines.LineNumberColumnWidthChanged += FormattedLines_PropertyChanged;
            FormattedLines.LoadFinished += FormattedLines_LoadFinished;
        }

        internal void SetLabel()
        {
            bool isFileReadOnly = Utils.IsReadOnly(GrepResult);

            string basePath = string.IsNullOrWhiteSpace(searchFolderPath) ? string.Empty : searchFolderPath;
            string displayedName = Path.GetFileName(GrepResult.FileNameDisplayed);

            if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowFilePathInResults) &&
                GrepResult.FileNameDisplayed.Contains(basePath, StringComparison.CurrentCultureIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(basePath))
                    displayedName = GrepResult.FileNameDisplayed[basePath.Length..].TrimStart('\\');
                else
                    displayedName = GrepResult.FileNameDisplayed;
            }
            if (!string.IsNullOrWhiteSpace(GrepResult.AdditionalInformation))
            {
                displayedName += " " + GrepResult.AdditionalInformation + " ";
            }
            int matchCount = GrepResult.Matches == null ? 0 : GrepResult.Matches.Count;
            if (matchCount > 0)
            {
                if (GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowVerboseMatchCount) && !GrepResult.IsHexFile)
                {
                    var lineCount = GrepResult.Matches?.Where(r => r.LineNumber > 0)
                       .Select(r => r.LineNumber).Distinct().Count() ?? 0;
                    displayedName = TranslationSource.Format(Resources.Main_ResultList_CountMatchesOnLines, displayedName, matchCount, lineCount);
                }
                else
                {
                    displayedName = string.Format(Resources.Main_ResultList_CountMatches, displayedName, matchCount);
                }
            }
            if (isFileReadOnly)
            {
                displayedName = displayedName + " " + Resources.Main_ResultList_ReadOnly;
            }

            Label = displayedName;

            Style = string.Empty;
            if (isFileReadOnly)
            {
                Style = "ReadOnly";
            }

            if (!GrepResult.IsSuccess)
            {
                Style = "Error";
            }

            if (!string.IsNullOrEmpty(GrepResult.FileInfo.ErrorMsg))
            {
                Style = "Error";
            }
            OnPropertyChanged(nameof(Style));
        }

        void FormattedLines_LoadFinished(object? sender, EventArgs e)
        {
            IsLoading = false;
        }

        void FormattedLines_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LineNumberColumnWidth")
                LineNumberColumnWidth = FormattedLines.LineNumberColumnWidth;
        }

        public ObservableCollection<FormattedGrepMatch> FormattedMatches { get; } = [];

        [ObservableProperty]
        private bool wrapText;
        partial void OnWrapTextChanged(bool value)
        {
            foreach (var item in FormattedLines)
            {
                item.WrapText = value;
            }
        }

        public int Level => 0;

        public IEnumerable<ITreeItem> Children => FormattedLines;

        /// <summary>
        /// Dummy property to satisfy tree binding
        /// </summary>
        public GrepLine GrepLine { get; private set; } = new(-1, string.Empty, false, null);


        public override bool Equals(object? obj)
        {
            if (obj is FormattedGrepResult other)
                return GrepResult.Equals(other.GrepResult);
            return false;
        }

        public override int GetHashCode()
        {
            return GrepResult.GetHashCode();
        }
    }

}
