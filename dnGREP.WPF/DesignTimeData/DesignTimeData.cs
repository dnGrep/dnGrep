using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
                    var matches = new List<Common.GrepSearchResult.GrepMatch>();
                    matches.Add(new Common.GrepSearchResult.GrepMatch(1, 6, 6));
                    var result = new Common.GrepSearchResult(file, "abc", matches, Encoding.Default);
                    result.SearchResults.Add(new Common.GrepSearchResult.GrepLine(1, "using System", true, matches));
                    var formatted = new FormattedGrepResult(result, filePath);
                    formatted.IsExpanded = true;
                    dummyObservableGrepSearchResults.Add(formatted);

                }
                return dummyObservableGrepSearchResults;
            }
        }
    }
}
