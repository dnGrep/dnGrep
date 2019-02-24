using System;
using System.Collections.Generic;
using System.Text;
using dnGREP.Common;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.WPF.DesignTimeData
{
    public class DesignTimeData
    {
        private static ObservableGrepSearchResults dummyObservableGrepSearchResults;
        public static ObservableGrepSearchResults DummyObservableGrepSearchResults
        {
            get
            {
                if (dummyObservableGrepSearchResults == null)
                {
                    string path = Environment.ExpandEnvironmentVariables("%dnGrepRepo%");
                    string file = Path.Combine(path, @"dnGREP.WPF\DesignTimeData\DesignTimeData.cs");
                    string filePath = Path.GetDirectoryName(file);

                    dummyObservableGrepSearchResults = new ObservableGrepSearchResults();
                    var matches = new List<GrepMatch>();
                    matches.Add(new GrepMatch(1, 6, 6));
                    var result = new GrepSearchResult(file, "abc", matches, Encoding.Default);
                    result.SearchResults.Add(new GrepLine(1, "using System", true, matches));
                    var formatted = new FormattedGrepResult(result, filePath);
                    formatted.IsExpanded = true;
                    dummyObservableGrepSearchResults.Add(formatted);

                }
                return dummyObservableGrepSearchResults;
            }
        }
    }
}
