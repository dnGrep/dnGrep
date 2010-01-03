using System;
using System.Collections.Generic;
using System.Text;

namespace dnGREP.Common
{
	public enum SearchType
	{
		PlainText,
		Regex,
		XPath,
		Soundex
	}

	public enum FileSearchType
	{
		Asterisk,
		Regex
	}

	public enum FileSizeFilter
	{
		Yes,
		No
	}

    [Flags]
    public enum GrepSearchOption
    {
        None = 0,
        CaseSensitive = 1,
        Multiline = 2,
        SingleLine = 4
    }

	public enum GrepOperation
	{
		Search,
		SearchInResults,
		Replace,
		None
	}
}
