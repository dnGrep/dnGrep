using System;

namespace dnGREP.Common
{
    public class OpenFileArgs : EventArgs
    {
        /// <summary>
        /// Search result containing file name
        /// </summary>
        public GrepSearchResult SearchResult { get; set; }

        /// <summary>
        /// The search pattern
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// The match page number
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// The match line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The first match string on this line
        /// </summary>
        public string FirstMatch { get; set; }

        /// <summary>
        /// The column number of the first match on this line
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// If true, CustomEditor is used to open the file
        /// </summary>
        public bool UseCustomEditor { get; set; }

        /// <summary>
        /// Path to custom editor (if UseCustomEditor is true)
        /// </summary>
        public string CustomEditor { get; set; }

        /// <summary>
        /// Command line arguments for custom editor
        /// </summary>
        public string CustomEditorArgs { get; set; }

        /// <summary>
        /// Set to true to have base engine handle the request
        /// </summary>
        public bool UseBaseEngine { get; set; }

        public OpenFileArgs(GrepSearchResult searchResult, string pattern, int page, int line, string firstMatch, int columnNumber, bool useCustomEditor, string customEditor, string customEditorArgs)
            : base()
        {
            SearchResult = searchResult;
            PageNumber = page;
            LineNumber = line;
            FirstMatch = firstMatch;
            ColumnNumber = columnNumber;
            UseCustomEditor = useCustomEditor;
            CustomEditor = customEditor;
            CustomEditorArgs = customEditorArgs;
            UseBaseEngine = false;
            Pattern = pattern;
        }
    }
}
