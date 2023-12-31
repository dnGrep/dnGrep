using System;

namespace dnGREP.Common
{
    public class OpenFileArgs(GrepSearchResult searchResult, string pattern, int page, int line, string firstMatch, int columnNumber, bool useCustomEditor, string customEditorName) : EventArgs()
    {
        public static readonly string DefaultEditor = "DefaultEditor";

        /// <summary>
        /// Search result containing file name
        /// </summary>
        public GrepSearchResult SearchResult { get; set; } = searchResult;

        /// <summary>
        /// The search pattern
        /// </summary>
        public string Pattern { get; set; } = pattern;

        /// <summary>
        /// The match page number
        /// </summary>
        public int PageNumber { get; set; } = page;

        /// <summary>
        /// The match line number
        /// </summary>
        public int LineNumber { get; set; } = line;

        /// <summary>
        /// The first match string on this line
        /// </summary>
        public string FirstMatch { get; set; } = firstMatch;

        /// <summary>
        /// The column number of the first match on this line
        /// </summary>
        public int ColumnNumber { get; set; } = columnNumber;

        /// <summary>
        /// If true, CustomEditor is used to open the file
        /// </summary>
        public bool UseCustomEditor { get; set; } = useCustomEditor;

        /// <summary>
        /// Name of custom editor (if UseCustomEditor is true)
        /// </summary>
        public string CustomEditorName { get; set; } = customEditorName;

        /// <summary>
        /// Set to true to have base engine handle the request
        /// </summary>
        public bool UseBaseEngine { get; set; } = false;
    }
}
