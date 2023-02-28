using System.ComponentModel;
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
            BookmarksVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.BookmarksVisible);
            TestExpressionVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.TestExpressionVisible);
            ReplaceVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.ReplaceVisible);
            SortVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SortVisible);
            MoreVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.MoreVisible);

            SearchInArchivesVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchInArchivesVisible);
            SizeFilterVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SizeFilterVisible);
            SubfoldersFilterVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SubfoldersFilterVisible);
            HiddenFilterVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.HiddenFilterVisible);
            BinaryFilterVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.BinaryFilterVisible);
            SymbolicLinkFilterVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SymbolicLinkFilterVisible);
            DateFilterVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.DateFilterVisible);

            SearchParallelVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchParallelVisible);
            UseGitIgnoreVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.UseGitIgnoreVisible);
            SkipCloudStorageVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SkipCloudStorageVisible);
            EncodingVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.EncodingVisible);

            SearchTypeRegexVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypeRegexVisible);
            SearchTypeXPathVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypeXPathVisible);
            SearchTypeTextVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypeTextVisible);
            SearchTypePhoneticVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypePhoneticVisible);
            SearchTypeByteVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchTypeByteVisible);

            BooleanOperatorsVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.BooleanOperatorsVisible);
            CaptureGroupSearchVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.CaptureGroupSearchVisible);
            SearchInResultsVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.SearchInResultsVisible);
            PreviewFileVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreviewFileVisible);
            StopAfterFirstMatchVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.StopAfterFirstMatchVisible);

            HighlightMatchesVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightMatchesVisible);
            HighlightGroupsVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.HighlightGroupsVisible);
            ShowContextLinesVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.ShowContextLinesVisible);
            ZoomResultsTreeVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.ZoomResultsTreeVisible);
            WrapTextResultsTreeVisible = !personalizationOn || GrepSettings.Instance.Get<bool>(GrepSettings.Key.WrapTextResultsTreeVisible);
        }


        private bool personalizationOn = false;
        public bool PersonalizationOn
        {
            get { return personalizationOn; }
            set
            {
                if (personalizationOn == value)
                {
                    return;
                }

                personalizationOn = value;
                OnPropertyChanged(nameof(PersonalizationOn));
                UpdatePersonalization();
                PreviewModel?.UpdatePersonalization(personalizationOn);
            }
        }


        private bool bookmarksVisible = true;
        public bool BookmarksVisible
        {
            get { return bookmarksVisible; }
            set
            {
                if (bookmarksVisible == value)
                {
                    return;
                }

                bookmarksVisible = value;
                OnPropertyChanged(nameof(BookmarksVisible));
                OnPropertyChanged(nameof(CompositeBookmarksVisible));
            }
        }

        public bool CompositeBookmarksVisible { get { return !IsEverythingSearchMode && BookmarksVisible; } }


        private bool testExpressionVisible = true;
        public bool TestExpressionVisible
        {
            get { return testExpressionVisible; }
            set
            {
                if (testExpressionVisible == value)
                {
                    return;
                }

                testExpressionVisible = value;
                OnPropertyChanged(nameof(TestExpressionVisible));
            }
        }


        private bool replaceVisible = true;
        public bool ReplaceVisible
        {
            get { return replaceVisible; }
            set
            {
                if (replaceVisible == value)
                {
                    return;
                }

                replaceVisible = value;
                OnPropertyChanged(nameof(ReplaceVisible));
            }
        }


        private bool sortVisible = true;
        public bool SortVisible
        {
            get { return sortVisible; }
            set
            {
                if (sortVisible == value)
                {
                    return;
                }

                sortVisible = value;
                OnPropertyChanged(nameof(SortVisible));
            }
        }


        private bool moreVisible = true;
        public bool MoreVisible
        {
            get { return moreVisible; }
            set
            {
                if (moreVisible == value)
                {
                    return;
                }

                moreVisible = value;
                OnPropertyChanged(nameof(MoreVisible));
            }
        }



        private bool searchInArchivesVisible = true;
        public bool SearchInArchivesVisible
        {
            get { return searchInArchivesVisible; }
            set
            {
                if (searchInArchivesVisible == value)
                {
                    return;
                }

                searchInArchivesVisible = value;
                OnPropertyChanged(nameof(SearchInArchivesVisible));
                OnPropertyChanged(nameof(CompositeSearchInArchivesVisible));
            }
        }

        public bool CompositeSearchInArchivesVisible { get { return CanSearchArchives && SearchInArchivesVisible; } }


        private bool sizeFilterVisible = true;
        public bool SizeFilterVisible
        {
            get { return sizeFilterVisible; }
            set
            {
                if (sizeFilterVisible == value)
                {
                    return;
                }

                sizeFilterVisible = value;
                OnPropertyChanged(nameof(SizeFilterVisible));
            }
        }


        private bool subfoldersFilterVisible = true;
        public bool SubfoldersFilterVisible
        {
            get { return subfoldersFilterVisible; }
            set
            {
                if (subfoldersFilterVisible == value)
                {
                    return;
                }

                subfoldersFilterVisible = value;
                OnPropertyChanged(nameof(SubfoldersFilterVisible));
            }
        }


        private bool hiddenFilterVisible = true;
        public bool HiddenFilterVisible
        {
            get { return hiddenFilterVisible; }
            set
            {
                if (hiddenFilterVisible == value)
                {
                    return;
                }

                hiddenFilterVisible = value;
                OnPropertyChanged(nameof(HiddenFilterVisible));
            }
        }


        private bool binaryFilterVisible = true;
        public bool BinaryFilterVisible
        {
            get { return binaryFilterVisible; }
            set
            {
                if (binaryFilterVisible == value)
                {
                    return;
                }

                binaryFilterVisible = value;
                OnPropertyChanged(nameof(BinaryFilterVisible));
            }
        }


        private bool symbolicLinkFilterVisible = true;
        public bool SymbolicLinkFilterVisible
        {
            get { return symbolicLinkFilterVisible; }
            set
            {
                if (symbolicLinkFilterVisible == value)
                {
                    return;
                }

                symbolicLinkFilterVisible = value;
                OnPropertyChanged(nameof(SymbolicLinkFilterVisible));
            }
        }

        private bool dateFilterVisible = true;
        public bool DateFilterVisible
        {
            get { return dateFilterVisible; }
            set
            {
                if (dateFilterVisible == value)
                {
                    return;
                }

                dateFilterVisible = value;
                OnPropertyChanged(nameof(DateFilterVisible));
            }
        }


        private bool searchParallelVisible = true;
        public bool SearchParallelVisible
        {
            get { return searchParallelVisible; }
            set
            {
                if (searchParallelVisible == value)
                {
                    return;
                }

                searchParallelVisible = value;
                OnPropertyChanged(nameof(SearchParallelVisible));
            }
        }


        private bool useGitIgnoreVisible = true;
        public bool UseGitIgnoreVisible
        {
            get { return useGitIgnoreVisible; }
            set
            {
                if (useGitIgnoreVisible == value)
                {
                    return;
                }

                useGitIgnoreVisible = value;
                OnPropertyChanged(nameof(UseGitIgnoreVisible));
                OnPropertyChanged(nameof(CompositeUseGitIgnoreVisible));
            }
        }

        public bool CompositeUseGitIgnoreVisible { get { return IsGitInstalled && UseGitIgnoreVisible; } }

        private bool skipCloudStorageVisible = true;
        public bool SkipCloudStorageVisible
        {
            get { return skipCloudStorageVisible; }
            set
            {
                if (skipCloudStorageVisible == value)
                {
                    return;
                }

                skipCloudStorageVisible = value;
                OnPropertyChanged(nameof(SkipCloudStorageVisible));
            }
        }


        private bool encodingVisible = true;
        public bool EncodingVisible
        {
            get { return encodingVisible; }
            set
            {
                if (encodingVisible == value)
                {
                    return;
                }

                encodingVisible = value;
                OnPropertyChanged(nameof(EncodingVisible));
            }
        }



        private bool searchTypeRegexVisible = true;
        public bool SearchTypeRegexVisible
        {
            get { return searchTypeRegexVisible; }
            set
            {
                if (searchTypeRegexVisible == value)
                {
                    return;
                }

                searchTypeRegexVisible = value;
                OnPropertyChanged(nameof(SearchTypeRegexVisible));
            }
        }


        private bool searchTypeXPathVisible = true;
        public bool SearchTypeXPathVisible
        {
            get { return searchTypeXPathVisible; }
            set
            {
                if (searchTypeXPathVisible == value)
                {
                    return;
                }

                searchTypeXPathVisible = value;
                OnPropertyChanged(nameof(SearchTypeXPathVisible));
            }
        }


        private bool searchTypeTextVisible = true;
        public bool SearchTypeTextVisible
        {
            get { return searchTypeTextVisible; }
            set
            {
                if (searchTypeTextVisible == value)
                {
                    return;
                }

                searchTypeTextVisible = value;
                OnPropertyChanged(nameof(SearchTypeTextVisible));
            }
        }


        private bool searchTypePhoneticVisible = true;
        public bool SearchTypePhoneticVisible
        {
            get { return searchTypePhoneticVisible; }
            set
            {
                if (searchTypePhoneticVisible == value)
                {
                    return;
                }

                searchTypePhoneticVisible = value;
                OnPropertyChanged(nameof(SearchTypePhoneticVisible));
            }
        }


        private bool searchTypeByteVisible = true;
        public bool SearchTypeByteVisible
        {
            get { return searchTypeByteVisible; }
            set
            {
                if (searchTypeByteVisible == value)
                {
                    return;
                }

                searchTypeByteVisible = value;
                OnPropertyChanged(nameof(SearchTypeByteVisible));
            }
        }


        private bool booleanOperatorsVisible = true;
        public bool BooleanOperatorsVisible
        {
            get { return booleanOperatorsVisible; }
            set
            {
                if (booleanOperatorsVisible == value)
                {
                    return;
                }

                booleanOperatorsVisible = value;
                OnPropertyChanged(nameof(BooleanOperatorsVisible));
            }
        }


        private bool captureGroupSearchVisible = true;
        public bool CaptureGroupSearchVisible
        {
            get { return captureGroupSearchVisible; }
            set
            {
                if (captureGroupSearchVisible == value)
                {
                    return;
                }

                captureGroupSearchVisible = value;
                OnPropertyChanged(nameof(CaptureGroupSearchVisible));
            }
        }


        private bool searchInResultsVisible = true;
        public bool SearchInResultsVisible
        {
            get { return searchInResultsVisible; }
            set
            {
                if (searchInResultsVisible == value)
                {
                    return;
                }

                searchInResultsVisible = value;
                OnPropertyChanged(nameof(SearchInResultsVisible));
                OnPropertyChanged(nameof(CompositeSearchInResultsVisible));
            }
        }

        public bool CompositeSearchInResultsVisible { get { return SearchInResultsVisible && CanSearchInResults; } }


        private bool previewFileVisible = true;
        public bool PreviewFileVisible
        {
            get { return previewFileVisible; }
            set
            {
                if (previewFileVisible == value)
                {
                    return;
                }

                previewFileVisible = value;
                OnPropertyChanged(nameof(PreviewFileVisible));
            }
        }


        private bool stopAfterFirstMatchVisible = true;
        public bool StopAfterFirstMatchVisible
        {
            get { return stopAfterFirstMatchVisible; }
            set
            {
                if (stopAfterFirstMatchVisible == value)
                {
                    return;
                }

                stopAfterFirstMatchVisible = value;
                OnPropertyChanged(nameof(StopAfterFirstMatchVisible));
            }
        }



        private bool highlightMatchesVisible = true;
        public bool HighlightMatchesVisible
        {
            get { return highlightMatchesVisible; }
            set
            {
                if (highlightMatchesVisible == value)
                {
                    return;
                }

                highlightMatchesVisible = value;
                OnPropertyChanged(nameof(HighlightMatchesVisible));
                OnPropertyChanged(nameof(ResultsTreeOptionsExpanderVisible));
            }
        }


        private bool highlightGroupsVisible = true;
        public bool HighlightGroupsVisible
        {
            get { return highlightGroupsVisible; }
            set
            {
                if (highlightGroupsVisible == value)
                {
                    return;
                }

                highlightGroupsVisible = value;
                OnPropertyChanged(nameof(HighlightGroupsVisible));
                OnPropertyChanged(nameof(ResultsTreeOptionsExpanderVisible));
            }
        }


        private bool showContextLinesVisible = true;
        public bool ShowContextLinesVisible
        {
            get { return showContextLinesVisible; }
            set
            {
                if (showContextLinesVisible == value)
                {
                    return;
                }

                showContextLinesVisible = value;
                OnPropertyChanged(nameof(ShowContextLinesVisible));
                OnPropertyChanged(nameof(ResultsTreeOptionsExpanderVisible));
            }
        }


        private bool zoomResultsTreeVisible = true;
        public bool ZoomResultsTreeVisible
        {
            get { return zoomResultsTreeVisible; }
            set
            {
                if (zoomResultsTreeVisible == value)
                {
                    return;
                }

                zoomResultsTreeVisible = value;
                OnPropertyChanged(nameof(ZoomResultsTreeVisible));
                OnPropertyChanged(nameof(ResultsTreeOptionsExpanderVisible));
            }
        }


        private bool wrapTextResultsTreeVisible = true;
        public bool WrapTextResultsTreeVisible
        {
            get { return wrapTextResultsTreeVisible; }
            set
            {
                if (wrapTextResultsTreeVisible == value)
                {
                    return;
                }

                wrapTextResultsTreeVisible = value;
                OnPropertyChanged(nameof(WrapTextResultsTreeVisible));
                OnPropertyChanged(nameof(ResultsTreeOptionsExpanderVisible));
            }
        }

        public bool ResultsTreeOptionsExpanderVisible
        {
            get { return HighlightMatchesVisible || HighlightGroupsVisible || ShowContextLinesVisible || ZoomResultsTreeVisible || WrapTextResultsTreeVisible; }
        }
    }
}