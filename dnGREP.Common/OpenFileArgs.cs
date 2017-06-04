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
        /// Line number
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Line number
        /// </summary>
        public int LineNumber { get; set; }

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

        public OpenFileArgs(GrepSearchResult searchResult, string pattern, int line, bool useCustomEditor, string customEditor, string customEditorArgs)
            : base()
        {
            SearchResult = searchResult;
            LineNumber = line;
            UseCustomEditor = useCustomEditor;
            CustomEditor = customEditor;
            CustomEditorArgs = customEditorArgs;
            UseBaseEngine = false;
            Pattern = pattern;
        }

        public OpenFileArgs()
            : this(null, null, -1, false, null, null)
        { }
    }
}
