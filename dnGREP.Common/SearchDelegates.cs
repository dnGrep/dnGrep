using System;
using System.Collections.Generic;
using System.Text;

namespace dnGREP.Common
{
	public class SearchDelegates
	{
		public delegate bool DoSearch(string text, string searchPattern);
		public delegate List<GrepSearchResult.GrepLine> DoSearchMultiline(string text, string searchPattern);
		public delegate string DoReplace(string text, string searchPattern, string replacePattern);
	}
}
