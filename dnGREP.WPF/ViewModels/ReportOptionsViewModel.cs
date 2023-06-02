using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public enum ReportOutputType { Text, CSV }

    public partial class ReportOptionsViewModel : CultureAwareViewModel
    {
        public event EventHandler? RequestClose;

        private readonly List<GrepSearchResult> searchResults;
        private readonly SearchType typeOfSearch;

        public ReportOptionsViewModel(GrepSearchResultsViewModel searchResults)
        {
            this.searchResults = searchResults.GetList();
            typeOfSearch = searchResults.TypeOfSearch;

            LoadSettings();
            UpdateState();
            FormatSampleText();

            PropertyChanged += ReportOptionsViewModel_PropertyChanged;
        }

        private void UpdateState()
        {
            if (typeOfSearch != SearchType.Regex)
            {
                ReportMode = ReportMode.FullLine;
                ReportModeEditable = false;
            }
            else
            {
                ReportModeEditable = true;
            }

            FilterUniqueValuesEditable = ReportMode != ReportMode.FullLine;
            OutputOnSeparateLinesEditable = ReportMode != ReportMode.FullLine;
            UniqueScopeEditable = FilterUniqueValues;
        }

        private void LoadSettings()
        {
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
            ResultsFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ResultsFontFamily);
            ResultsFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.ResultsFontSize);

            ReportMode = GrepSettings.Instance.Get<ReportMode>(GrepSettings.Key.ReportMode);
            IncludeFileInformation = GrepSettings.Instance.Get<bool>(GrepSettings.Key.IncludeFileInformation);
            TrimWhitespace = GrepSettings.Instance.Get<bool>(GrepSettings.Key.TrimWhitespace);
            FilterUniqueValues = GrepSettings.Instance.Get<bool>(GrepSettings.Key.FilterUniqueValues);
            UniqueScope = GrepSettings.Instance.Get<UniqueScope>(GrepSettings.Key.UniqueScope);
            OutputOnSeparateLines = GrepSettings.Instance.Get<bool>(GrepSettings.Key.OutputOnSeparateLines);
            ListItemSeparator = GrepSettings.Instance.Get<string>(GrepSettings.Key.ListItemSeparator);
        }

        private void SaveSettings()
        {
            GrepSettings.Instance.Set(GrepSettings.Key.ReportMode, ReportMode);
            GrepSettings.Instance.Set(GrepSettings.Key.IncludeFileInformation, IncludeFileInformation);
            GrepSettings.Instance.Set(GrepSettings.Key.TrimWhitespace, TrimWhitespace);
            GrepSettings.Instance.Set(GrepSettings.Key.FilterUniqueValues, FilterUniqueValues);
            GrepSettings.Instance.Set(GrepSettings.Key.UniqueScope, UniqueScope);
            GrepSettings.Instance.Set(GrepSettings.Key.OutputOnSeparateLines, OutputOnSeparateLines);
            GrepSettings.Instance.Set(GrepSettings.Key.ListItemSeparator, ListItemSeparator);

            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Returns a command that saves the form
        /// </summary>
        public ICommand SaveCommand => new RelayCommand(
            param => SaveSettings(),
            param => CanSave);

        public bool CanSave
        {
            get
            {
                return
                    ReportMode != GrepSettings.Instance.Get<ReportMode>(GrepSettings.Key.ReportMode) ||
                    IncludeFileInformation != GrepSettings.Instance.Get<bool>(GrepSettings.Key.IncludeFileInformation) ||
                    TrimWhitespace != GrepSettings.Instance.Get<bool>(GrepSettings.Key.TrimWhitespace) ||
                    FilterUniqueValues != GrepSettings.Instance.Get<bool>(GrepSettings.Key.FilterUniqueValues) ||
                    UniqueScope != GrepSettings.Instance.Get<UniqueScope>(GrepSettings.Key.UniqueScope) ||
                    OutputOnSeparateLines != GrepSettings.Instance.Get<bool>(GrepSettings.Key.OutputOnSeparateLines) ||
                    ListItemSeparator != GrepSettings.Instance.Get<string>(GrepSettings.Key.ListItemSeparator);
            }
        }

        private void ReportOptionsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SampleText))
            {
                FormatSampleText();
            }
        }

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private string resultsFontFamily = GrepSettings.DefaultMonospaceFontFamily;

        [ObservableProperty]
        private double resultsFontSize;

        [ObservableProperty]
        private ReportMode reportMode = ReportMode.FullLine;
        partial void OnReportModeChanged(ReportMode value)
        {
            if (value == ReportMode.FullLine)
            {
                FilterUniqueValues = false;
                OutputOnSeparateLines = false;
            }
            FilterUniqueValuesEditable = value != ReportMode.FullLine;
            OutputOnSeparateLinesEditable = value != ReportMode.FullLine;
        }

        [ObservableProperty]
        private bool reportModeEditable;

        [ObservableProperty]
        private bool includeFileInformation = true;

        [ObservableProperty]
        private bool trimWhitespace = false;

        [ObservableProperty]
        private bool filterUniqueValues = false;
        partial void OnFilterUniqueValuesChanged(bool value)
        {
            UniqueScopeEditable = value;
        }

        [ObservableProperty]
        private bool filterUniqueValuesEditable = false;

        [ObservableProperty]
        private UniqueScope uniqueScope;

        [ObservableProperty]
        private bool uniqueScopeEditable;

        [ObservableProperty]
        private bool outputOnSeparateLines = false;

        [ObservableProperty]
        private bool outputOnSeparateLinesEditable = false;

        [ObservableProperty]
        private string listItemSeparator = string.Empty;

        [ObservableProperty]
        private ReportOutputType reportType = ReportOutputType.Text;

        [ObservableProperty]
        private string sampleText = string.Empty;

        private void FormatSampleText()
        {
            var options = new ReportOptions(typeOfSearch, ReportMode, IncludeFileInformation,
                TrimWhitespace, FilterUniqueValues, UniqueScope,
                OutputOnSeparateLines, ListItemSeparator);

            switch (ReportType)
            {
                case ReportOutputType.Text:
                    SampleText = ReportWriter.GetResultsAsText(searchResults, options, 16);
                    break;

                case ReportOutputType.CSV:
                    SampleText = ReportWriter.GetResultsAsCSV(searchResults, options, 16);
                    break;
            }
        }
    }
}
