using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public partial class MainViewModel
    {
        private void OnMainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CanSearchArchives):
                    OnPropertyChanged(nameof(CompositeSearchInArchivesVisible));
                    break;

                case nameof(CanSearchInResults):
                    OnPropertyChanged(nameof(CompositeSearchInResultsVisible));
                    break;

                case nameof(IsGitInstalled):
                    OnPropertyChanged(nameof(CompositeUseGitIgnoreVisible));
                    break;

                case nameof(IsEverythingSearchMode):
                    OnPropertyChanged(nameof(CompositeBookmarksVisible));
                    break;

                case nameof(SearchParametersChanged):
                    UpdateReplaceButtonTooltip(false);
                    break;
            }
        }

        private void UpdatePersonalization()
        {
            BookmarksVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.BookmarksVisible);
            TestExpressionVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.TestExpressionVisible);
            ReplaceVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.ReplaceVisible);
            SortVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SortVisible);
            MoreVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.MoreVisible);

            SearchInArchivesVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchInArchivesVisible);
            SizeFilterVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SizeFilterVisible);
            SubfoldersFilterVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SubfoldersFilterVisible);
            HiddenFilterVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.HiddenFilterVisible);
            BinaryFilterVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.BinaryFilterVisible);
            SymbolicLinkFilterVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SymbolicLinkFilterVisible);
            DateFilterVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.DateFilterVisible);

            SearchParallelVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchParallelVisible);
            UseGitIgnoreVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.UseGitIgnoreVisible);
            SkipCloudStorageVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SkipCloudStorageVisible);
            EncodingVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.EncodingVisible);

            SearchTypeRegexVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypeRegexVisible);
            SearchTypeXPathVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypeXPathVisible);
            SearchTypeTextVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypeTextVisible);
            SearchTypePhoneticVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypePhoneticVisible);
            SearchTypeByteVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypeByteVisible);

            BooleanOperatorsVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.BooleanOperatorsVisible);
            CaptureGroupSearchVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.CaptureGroupSearchVisible);
            SearchInResultsVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchInResultsVisible);
            PreviewFileVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreviewFileVisible);
            StopAfterFirstMatchVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.StopAfterFirstMatchVisible);

            HighlightMatchesVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightMatchesVisible);
            HighlightGroupsVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightGroupsVisible);
            ShowContextLinesVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowContextLinesVisible);
            ZoomResultsTreeVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.ZoomResultsTreeVisible);
            WrapTextResultsTreeVisible = !PersonalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.WrapTextResultsTreeVisible);
        }

        [ObservableProperty]
        private bool personalizationOn = false;
        partial void OnPersonalizationOnChanged(bool value)
        {
            UpdatePersonalization();
            PreviewModel?.UpdatePersonalization(PersonalizationOn);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CompositeBookmarksVisible))]
        private bool bookmarksVisible = true;

        public bool CompositeBookmarksVisible => !IsEverythingSearchMode && BookmarksVisible;


        [ObservableProperty]
        private bool testExpressionVisible = true;

        [ObservableProperty]
        private bool replaceVisible = true;

        [ObservableProperty]
        private bool sortVisible = true;

        [ObservableProperty]
        private bool moreVisible = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CompositeSearchInArchivesVisible))]
        private bool searchInArchivesVisible = true;

        public bool CompositeSearchInArchivesVisible => CanSearchArchives && SearchInArchivesVisible;


        [ObservableProperty]
        private bool sizeFilterVisible = true;

        [ObservableProperty]
        private bool subfoldersFilterVisible = true;

        [ObservableProperty]
        private bool hiddenFilterVisible = true;

        [ObservableProperty]
        private bool binaryFilterVisible = true;

        [ObservableProperty]
        private bool symbolicLinkFilterVisible = true;

        [ObservableProperty]
        private bool dateFilterVisible = true;

        [ObservableProperty]
        private bool searchParallelVisible = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CompositeUseGitIgnoreVisible))]
        private bool useGitIgnoreVisible = true;

        public bool CompositeUseGitIgnoreVisible { get { return IsGitInstalled && UseGitIgnoreVisible; } }

        [ObservableProperty]
        private bool skipCloudStorageVisible = true;

        [ObservableProperty]
        private bool encodingVisible = true;


        [ObservableProperty]
        private bool searchTypeRegexVisible = true;

        [ObservableProperty]
        private bool searchTypeXPathVisible = true;

        [ObservableProperty]
        private bool searchTypeTextVisible = true;

        [ObservableProperty]
        private bool searchTypePhoneticVisible = true;

        [ObservableProperty]
        private bool searchTypeByteVisible = true;

        [ObservableProperty]
        private bool booleanOperatorsVisible = true;

        [ObservableProperty]
        private bool captureGroupSearchVisible = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CompositeSearchInResultsVisible))]
        private bool searchInResultsVisible = true;

        public bool CompositeSearchInResultsVisible => SearchInResultsVisible && CanSearchInResults;


        [ObservableProperty]
        private bool previewFileVisible = true;

        [ObservableProperty]
        private bool stopAfterFirstMatchVisible = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResultsTreeOptionsExpanderVisible))]
        private bool highlightMatchesVisible = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResultsTreeOptionsExpanderVisible))]
        private bool highlightGroupsVisible = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResultsTreeOptionsExpanderVisible))]
        private bool showContextLinesVisible = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResultsTreeOptionsExpanderVisible))]
        private bool zoomResultsTreeVisible = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResultsTreeOptionsExpanderVisible))]
        private bool wrapTextResultsTreeVisible = true;

        public bool ResultsTreeOptionsExpanderVisible =>
            HighlightMatchesVisible || HighlightGroupsVisible || ShowContextLinesVisible || ZoomResultsTreeVisible || WrapTextResultsTreeVisible;
    }
}