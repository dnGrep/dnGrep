using System;
using System.Collections.Generic;
using System.Text;

namespace dnGREP.Common
{
	public class SearchDelegates
	{
		public delegate bool DoSearch(string text, string searchPattern, GrepSearchOption searchOptions);
        public delegate List<GrepSearchResult.GrepLine> DoSearchMultiline(string text, string searchPattern, GrepSearchOption searchOptions);
        public delegate string DoReplace(string text, string searchPattern, string replacePattern, GrepSearchOption searchOptions);
	}
}
