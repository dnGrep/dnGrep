using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public enum ReportOutputType { Text, CSV }

    public class ReportOptionsViewModel : CultureAwareViewModel
    {
        public event EventHandler RequestClose;

        private readonly List<GrepSearchResult> searchResults;
        private readonly SearchType typeOfSearch;

        public ReportOptionsViewModel(ObservableGrepSearchResults searchResults)
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

            FilterUniqueValuesEditable = reportMode != ReportMode.FullLine;
            OutputOnSeparateLinesEditable = reportMode != ReportMode.FullLine;
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

            RequestClose(this, EventArgs.Empty);
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

        private void ReportOptionsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ReportMode):

                    if (ReportMode == ReportMode.FullLine)
                    {
                        FilterUniqueValues = false;
                        OutputOnSeparateLines = false;
                    }
                    FilterUniqueValuesEditable = reportMode != ReportMode.FullLine;
                    OutputOnSeparateLinesEditable = reportMode != ReportMode.FullLine;

                    break;

                case nameof(FilterUniqueValues):

                    UniqueScopeEditable = FilterUniqueValues;
                    break;
            }

            if (e.PropertyName != nameof(SampleText))
            {
                FormatSampleText();
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

        private double dialogFontSize;
        public double DialogFontSize
        {
            get { return dialogFontSize; }
            set
            {
                if (dialogFontSize == value)
                    return;

                dialogFontSize = value;
                base.OnPropertyChanged(nameof(DialogFontSize));
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

        private double resultsfontSize;
        public double ResultsFontSize
        {
            get { return resultsfontSize; }
            set
            {
                if (resultsfontSize == value)
                    return;

                resultsfontSize = value;
                base.OnPropertyChanged(nameof(ResultsFontSize));
            }
        }


        private ReportMode reportMode = ReportMode.FullLine;
        public ReportMode ReportMode
        {
            get { return reportMode; }
            set
            {
                if (reportMode == value)
                {
                    return;
                }

                reportMode = value;
                OnPropertyChanged(nameof(ReportMode));
            }
        }


        private bool reportModeEditable;
        public bool ReportModeEditable
        {
            get { return reportModeEditable; }
            set
            {
                if (reportModeEditable == value)
                {
                    return;
                }

                reportModeEditable = value;
                OnPropertyChanged(nameof(ReportModeEditable));
            }
        }


        private bool includeFileInformation = true;
        public bool IncludeFileInformation
        {
            get { return includeFileInformation; }
            set
            {
                if (includeFileInformation == value)
                {
                    return;
                }

                includeFileInformation = value;
                OnPropertyChanged(nameof(IncludeFileInformation));
            }
        }


        private bool trimWhitespace = false;
        public bool TrimWhitespace
        {
            get { return trimWhitespace; }
            set
            {
                if (trimWhitespace == value)
                {
                    return;
                }

                trimWhitespace = value;
                OnPropertyChanged(nameof(TrimWhitespace));
            }
        }


        private bool filterUniqueValues = false;
        public bool FilterUniqueValues
        {
            get { return filterUniqueValues; }
            set
            {
                if (filterUniqueValues == value)
                {
                    return;
                }

                filterUniqueValues = value;
                OnPropertyChanged(nameof(FilterUniqueValues));
            }
        }


        private bool filterUniqueValuesEditable = false;
        public bool FilterUniqueValuesEditable
        {
            get { return filterUniqueValuesEditable; }
            set
            {
                if (filterUniqueValuesEditable == value)
                {
                    return;
                }

                filterUniqueValuesEditable = value;
                OnPropertyChanged(nameof(FilterUniqueValuesEditable));
            }
        }


        private UniqueScope uniqueScope;
        public UniqueScope UniqueScope
        {
            get { return uniqueScope; }
            set
            {
                if (uniqueScope == value)
                {
                    return;
                }

                uniqueScope = value;
                OnPropertyChanged(nameof(UniqueScope));
            }
        }


        private bool uniqueScopeEditable;
        public bool UniqueScopeEditable
        {
            get { return uniqueScopeEditable; }
            set
            {
                if (uniqueScopeEditable == value)
                {
                    return;
                }

                uniqueScopeEditable = value;
                OnPropertyChanged(nameof(UniqueScopeEditable));
            }
        }

        private bool outputOnSeparateLines = false;
        public bool OutputOnSeparateLines
        {
            get { return outputOnSeparateLines; }
            set
            {
                if (outputOnSeparateLines == value)
                {
                    return;
                }

                outputOnSeparateLines = value;
                OnPropertyChanged(nameof(OutputOnSeparateLines));
            }
        }

        private bool outputOnSeparateLinesEditable = false;
        public bool OutputOnSeparateLinesEditable
        {
            get { return outputOnSeparateLinesEditable; }
            set
            {
                if (outputOnSeparateLinesEditable == value)
                {
                    return;
                }

                outputOnSeparateLinesEditable = value;
                OnPropertyChanged(nameof(OutputOnSeparateLinesEditable));
            }
        }


        private string listItemSeparator = string.Empty;
        public string ListItemSeparator
        {
            get { return listItemSeparator; }
            set
            {
                if (listItemSeparator == value)
                {
                    return;
                }

                listItemSeparator = value;
                OnPropertyChanged(nameof(ListItemSeparator));
            }
        }


        private ReportOutputType reportType = ReportOutputType.Text;
        public ReportOutputType ReportType
        {
            get { return reportType; }
            set
            {
                if (reportType == value)
                {
                    return;
                }

                reportType = value;
                OnPropertyChanged(nameof(ReportType));
            }
        }


        private string sampleText = string.Empty;
        public string SampleText
        {
            get { return sampleText; }
            set
            {
                if (sampleText == value)
                {
                    return;
                }

                sampleText = value;
                OnPropertyChanged(nameof(SampleText));
            }
        }

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
