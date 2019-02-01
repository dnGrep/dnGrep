using System.Collections.Generic;

namespace dnGREP.Common
{
    public class SearchDelegates
    {
        public delegate List<GrepSearchResult.GrepMatch> DoSearch(int lineNumber, string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext);
        public delegate string DoReplace(string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions, IEnumerable<GrepSearchResult.GrepMatch> replaceItems);
    }
}
