namespace dnGREP.Common
{
    public class ReportOptions
    {
        public ReportOptions(SearchType typeOfSearch)
        {
            TypeOfSearch = typeOfSearch;

            if (typeOfSearch == SearchType.Regex)
            {
                ReportMode = GrepSettings.Instance.Get<ReportMode>(GrepSettings.Key.ReportMode);
                IncludeFileInformation = GrepSettings.Instance.Get<bool>(GrepSettings.Key.IncludeFileInformation);
                TrimWhitespace = GrepSettings.Instance.Get<bool>(GrepSettings.Key.TrimWhitespace);
                FilterUniqueValues = GrepSettings.Instance.Get<bool>(GrepSettings.Key.FilterUniqueValues);
                UniqueScope = GrepSettings.Instance.Get<UniqueScope>(GrepSettings.Key.UniqueScope);
                OutputOnSeparateLines = GrepSettings.Instance.Get<bool>(GrepSettings.Key.OutputOnSeparateLines);
                ListItemSeparator = GrepSettings.Instance.Get<string>(GrepSettings.Key.ListItemSeparator) ?? string.Empty;
            }
            else
            {
                ReportMode = ReportMode.FullLine;
                IncludeFileInformation = GrepSettings.Instance.Get<bool>(GrepSettings.Key.IncludeFileInformation);
                TrimWhitespace = GrepSettings.Instance.Get<bool>(GrepSettings.Key.TrimWhitespace);
                FilterUniqueValues = false;
                UniqueScope = UniqueScope.PerFile;
                OutputOnSeparateLines = false;
                ListItemSeparator = string.Empty;
            }

            HexLineSize = GrepSettings.Instance.Get<int>(GrepSettings.Key.HexResultByteLength);
        }

        public ReportOptions(SearchType typeOfSearch, ReportMode reportMode, bool includeFileInformation, 
            bool trimWhitespace, bool filterUniqueValues, UniqueScope uniqueScope,
            bool outputOnSeparateLines, string listItemSeparator)
        {
            TypeOfSearch = typeOfSearch;

            if (typeOfSearch == SearchType.Regex)
            {
                ReportMode = reportMode;
                IncludeFileInformation = includeFileInformation;
                TrimWhitespace = trimWhitespace;
                FilterUniqueValues = filterUniqueValues;
                UniqueScope = uniqueScope;
                OutputOnSeparateLines = outputOnSeparateLines;
                ListItemSeparator = listItemSeparator;
            }
            else
            {
                ReportMode = ReportMode.FullLine;
                IncludeFileInformation = includeFileInformation;
                TrimWhitespace = trimWhitespace;
                FilterUniqueValues = false;
                UniqueScope = UniqueScope.PerFile;
                OutputOnSeparateLines = false;
                ListItemSeparator = string.Empty;
            }

            HexLineSize = GrepSettings.Instance.Get<int>(GrepSettings.Key.HexResultByteLength);
        }

        public SearchType TypeOfSearch { get; private set; }
        public ReportMode ReportMode { get; private set; }
        public bool IncludeFileInformation { get; private set; }
        public bool TrimWhitespace { get; private set; }
        public bool FilterUniqueValues { get; private set; }
        public UniqueScope UniqueScope { get; private set; }
        public bool OutputOnSeparateLines { get; private set; }
        public string ListItemSeparator { get; private set; }
        public int HexLineSize { get; private set; }
    }
}
