using System;
using System.Collections.Generic;
using System.Text;

namespace dnGREP.Common
{
	public class OpenFileArgs : EventArgs
	{
		private GrepSearchResult searchResult;
		/// <summary>
		/// Search result containing file name
		/// </summary>
		public GrepSearchResult SearchResult
		{
			get { return searchResult; }
			set { searchResult = value; }
		}
		private int lineNumber;

		/// <summary>
		/// Line number
		/// </summary>
		public int LineNumber
		{
			get { return lineNumber; }
			set { lineNumber = value; }
		}
		private bool useCustomEditor;

		/// <summary>
		/// If true, CustomEditor is used to open the file
		/// </summary>
		public bool UseCustomEditor
		{
			get { return useCustomEditor; }
			set { useCustomEditor = value; }
		}
		private string customEditor;

		/// <summary>
		/// Path to custom editor (if UseCustomEditor is true)
		/// </summary>
		public string CustomEditor
		{
			get { return customEditor; }
			set { customEditor = value; }
		}
		private string customEditorArgs;

		/// <summary>
		/// Command line arguments for custom editor
		/// </summary>
		public string CustomEditorArgs
		{
			get { return customEditorArgs; }
			set { customEditorArgs = value; }
		}

		private bool useBaseEngine;

		/// <summary>
		/// Set to true to have base engine handle the request
		/// </summary>
		public bool UseBaseEngine
		{
			get { return useBaseEngine; }
			set { useBaseEngine = value; }
		}

		public OpenFileArgs(GrepSearchResult searchResult, int line, bool useCustomEditor, string customEditor, string customEditorArgs)
			: base()
		{
			this.searchResult = searchResult;
			this.lineNumber = line;
			this.useCustomEditor = useCustomEditor;
			this.customEditor = customEditor;
			this.customEditorArgs = customEditorArgs;
			this.useBaseEngine = false;
		}
		
		public OpenFileArgs()
			: this(null, -1, false, null, null)
		{ }
	}
}
