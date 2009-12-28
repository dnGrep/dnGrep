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

	public enum GrepOperation
	{
		Search,
		SearchInResults,
		Replace,
		None
	}
}
