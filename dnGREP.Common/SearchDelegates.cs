using System.Collections.Generic;

namespace dnGREP.Common
{
    public class SearchDelegates
    {
        public delegate List<GrepMatch> DoSearch(int lineNumber, int filePosition, string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext, PauseCancelToken pauseCancelToken);
        public delegate string DoReplace(int lineNumber, int filePosition, string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken);
    }
}
