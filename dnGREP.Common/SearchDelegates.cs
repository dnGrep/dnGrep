using System.Collections.Generic;
using System.Threading;

namespace dnGREP.Common
{
    public class SearchDelegates
    {
        public delegate List<GrepMatch> DoSearch(int lineNumber, int filePosition, string text, string searchPattern, GrepSearchOption searchOptions, bool includeContext, CancellationToken cancellationToken);
        public delegate string DoReplace(int lineNumber, int filePosition, string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions, IEnumerable<GrepMatch> replaceItems, CancellationToken cancellationToken);
    }
}
