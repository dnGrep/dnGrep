using System;
using System.Collections.Generic;
using System.Text;
using dnGREP.Common;
using dnGREP.Engines;

namespace dnGREP.Engines
{
	public interface IGrepEngine
	{
		void Initialize(bool showLinesInContext, int linesBefore, int linesAfter);

		/// <summary>
		/// Return true if engine supports search only. Return false is engine supports replace as well.
		/// </summary>
		bool IsSearchOnly { get;}

		/// <summary>
		/// Short description of engine
		/// </summary>
		string Description { get;}

		/// <summary>
		/// List of file extensions that the engine will work with
		/// </summary>
		List<string> SupportedFileExtensions { get;}

		List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, bool isCaseSensitive, bool isMultiline, Encoding encoding);

		bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, bool isCaseSensitive, bool isMultiline, Encoding encoding);
	
		/// <summary>
		/// Method gets called when search/replace process is complete
		/// </summary>
		void Unload();
	}
}
