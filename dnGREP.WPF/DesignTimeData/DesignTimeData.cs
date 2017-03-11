using System;
using System.Collections.Generic;
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
                    dummyObservableGrepSearchResults = new ObservableGrepSearchResults();
                    var matches = new List<Common.GrepSearchResult.GrepMatch>();
                    matches.Add(new Common.GrepSearchResult.GrepMatch(2, 1, 5));
                    var result = new Common.GrepSearchResult(@"D:\Sandbox\dnGrep\dnGREP.WPF\DesignTimeData\DesignTimeData.cs", "abc", matches, Encoding.Default);
                    result.SearchResults.Add(new Common.GrepSearchResult.GrepLine(2, "hello world", false, matches));
                    var formatted = new FormattedGrepResult(result, @"D:\Sandbox\dnGrep\dnGREP.WPF\DesignTimeData");
                    formatted.IsExpanded = true;
                    dummyObservableGrepSearchResults.Add(formatted);

                }
                return dummyObservableGrepSearchResults;
            }
        }
    }
}
