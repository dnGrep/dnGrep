using System;
using System.Collections.Generic;
using System.Text;
using dnGREP.Engines;
using NLog;
using dnGREP.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;

namespace dnGREP.Engines.Word
{
	/// <summary>
	/// Based on a MicrosoftWordPlugin class for AstroGrep by Curtis Beard. Thank you!
	/// </summary>
	public class GrepEngineWord : GrepEngineBase, IGrepEngine,IDisposable
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private bool isAvailable = false;
		private bool isLoaded = false;
		private Type wordType;
		private object wordApplication;
		private object wordDocuments;
		private object wordSelection;

		private object MISSING_VALUE = System.Reflection.Missing.Value;

		#region Initialization and disposal
		public GrepEngineWord()
		{
			try
			{
				wordType = Type.GetTypeFromProgID("Word.Application");

				if (wordType != null)
					isAvailable = true;
			}
			catch (Exception ex)
			{
				isAvailable = false;
				logger.LogException(LogLevel.Error, "Failed to initialize Word.", ex);
			}
		}

		/// <summary>
		/// Handles disposing of the object.
		/// </summary>
		/// <history>
		/// </history>
		public void Dispose()
		{
			Unload();
			if (wordType != null && wordApplication != null)
			{
				// Close the application.
				wordApplication.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null,
					wordApplication, new object[] {});
			}

			if (wordApplication != null)
				Marshal.ReleaseComObject(wordApplication);

			wordApplication = null;
			wordType = null;

			isAvailable = false;
		}

		/// <summary>
		/// Destructor. Calls Dispose().
		/// </summary>
		~GrepEngineWord()
		{
			this.Dispose();
		}

		#endregion

		public bool IsSearchOnly
		{
			get { return true; }
		}

		public string Description
		{
			get { return "Searches inside Microsoft Word files. File types supported include: doc, docx. Search only."; }
		}

		public List<string> SupportedFileExtensions
		{
			get { return new List<string> ( new string[] { "doc", "docx" }); }
		}

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
		{
			load();
			SearchDelegates.DoSearch searchMethodMultiline = doTextSearchCaseSensitive;
			switch (searchType)
			{
				case SearchType.PlainText:
				case SearchType.XPath:
                    if ((searchOptions & GrepSearchOption.CaseSensitive) == GrepSearchOption.CaseSensitive)
					{
						searchMethodMultiline = doTextSearchCaseSensitive;
					}
					else
					{
						searchMethodMultiline = doTextSearchCaseInsensitive;
					}
					break;
				case SearchType.Regex:
                    searchMethodMultiline = doRegexSearch;
					break;
				case SearchType.Soundex:
					searchMethodMultiline = doFuzzySearchMultiline;
					break;
			}

            List<GrepSearchResult> result = searchMultiline(file, searchPattern, searchOptions, searchMethodMultiline);
			return result;
		}

        private List<GrepSearchResult> searchMultiline(string file, string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod)
		{
			List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

			try
			{
				// Open a given Word document as readonly
				object wordDocument = openDocument(file, true);

				// Get Selection Property
				wordSelection = wordApplication.GetType().InvokeMember("Selection", BindingFlags.GetProperty,
					null, wordApplication, null);

				// create range and find objects
				object range = getProperty(wordDocument, "Content");

				// create text
				object text = getProperty(range, "Text");

				var lines = searchMethod(Utils.CleanLineBreaks(text.ToString()), searchPattern, searchOptions, true);
				if (lines.Count > 0)
				{
                    GrepSearchResult result = new GrepSearchResult(file, searchPattern, lines);
                    using (StringReader reader = new StringReader(text.ToString()))
                    {
                        result.SearchResults = Utils.GetLinesEx(reader, result.Matches, linesBefore, linesAfter);
                    }
					result.ReadOnly = true;
					searchResults.Add(result);
				}
				closeDocument(wordDocument);
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, "Failed to search inside Word file", ex);
			}
			finally
			{
				releaseSelection();
			}
			return searchResults;
		}

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public Version FrameworkVersion
		{
			get
			{
				return new Version(2,7,0,0);
			}
		}

		public override void OpenFile(OpenFileArgs args)
		{
			args.UseCustomEditor = false;
			Utils.OpenFile(args);
		}

		#region Private Members
		/// <summary>
		/// Loads Microsoft Word.
		/// </summary>
		private void load()
		{
			bool visible = false;
			try
			{
				if (isAvailable && !isLoaded)
				{
					// load word
					wordApplication = Activator.CreateInstance(wordType);

					// set visible state
					wordApplication.GetType().InvokeMember("Visible", BindingFlags.SetProperty, null,
						wordApplication, new object[1] { visible });

					// get Documents Property
					wordDocuments = wordApplication.GetType().InvokeMember("Documents", BindingFlags.GetProperty,
						null, wordApplication, null);

					// if all is good, then say we are usable
					if (wordDocuments != null)
					{
						isLoaded = true;
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, "Failed to load Word and create Document.", ex);
			}

			base.Initialize(new GrepEngineInitParams(showLinesInContext, linesBefore, linesAfter, fuzzyMatchThreshold));
		}

		/// <summary>
		/// Unloads Microsoft Word.
		/// </summary>
		public void Unload()
		{
			if (wordType != null && wordApplication != null)
			{
				// Close the application.
				try
				{
					wordApplication.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, 
						wordApplication, new object[] {});
				}
				catch (Exception ex)
				{
					logger.LogException(LogLevel.Error, "Failed to unload Word.", ex);
				}
			}

			if (wordApplication != null)
			{
				try
				{
					Marshal.ReleaseComObject(wordApplication);
				}
				catch (Exception ex)
				{
					logger.LogException(LogLevel.Error, "Failed to release Word object.", ex);
				}
			}

			wordApplication = null;
			isLoaded = false;
		}

		/// <summary>Information enum [for selection]</summary>
		private enum WdInformation
		{
			wdActiveEndPageNumber = 3,
			wdFirstCharacterColumnNumber = 9,
			wdFirstCharacterLineNumber = 10
		}

		/// <summary>
		/// Releases the selection object from memory.
		/// </summary>
		private void releaseSelection()
		{
			if (wordSelection != null)
			{
				Marshal.ReleaseComObject(wordSelection);
			}
			wordSelection = null;
		}

		/// <summary>
		/// Opens and returns the Word's document object for the given file.
		/// </summary>
		/// <param name="path">Full path to file.</param>
		/// <param name="bReadOnly">True for readonly, False for full access.</param>
		/// <returns>Word's Document object if success, null otherwise</returns>
		private object openDocument(string path, bool bReadOnly)
		{
			if (isAvailable && wordDocuments != null && wordDocuments != null)
			{
				return wordDocuments.GetType().InvokeMember("Open", BindingFlags.InvokeMethod,
					null, wordDocuments, new object[3] {path, MISSING_VALUE, bReadOnly});
			}

			return null;
		}

		/// <summary>
		/// Closes the given Word Document object.
		/// </summary>
		/// <param name="doc">Word Document object</param>
		private void closeDocument(object doc)
		{
			if (isAvailable && doc != null)
				doc.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, doc, new object[] {});
		}

		/// <summary>
		/// Returns the Information object from the given object.
		/// </summary>
		/// <param name="obj">Object to retrieve information object from</param>
		/// <param name="type">Information type to retrieve</param>
		/// <returns>Information object</returns>
		private object information(object obj, WdInformation type)
		{
			if (isAvailable && obj != null)
				return obj.GetType().InvokeMember("Information", BindingFlags.GetProperty, null, obj, new object[1] {(int)type});

			return null;
		}

		/// <summary>
		/// Gets the specified property from the given object.
		/// </summary>
		/// <param name="obj">Object to get property from</param>
		/// <param name="prop">name of property to retrieve</param>
		/// <returns>Property object</returns>
		private object getProperty(object obj, string prop)
		{
			if (isAvailable && obj != null)
				return obj.GetType().InvokeMember(prop, BindingFlags.GetProperty, null, obj, new object[] {});

			return null;
		}

		/// <summary>
		/// Sets the given object's property to the given value.
		/// </summary>
		/// <param name="obj">object to set property</param>
		/// <param name="prop">name of property</param>
		/// <param name="value">value to set</param>
		private void setProperty(object obj, string prop, object value)
		{
			if (isAvailable && obj != null)
				obj.GetType().InvokeMember(prop, BindingFlags.SetProperty, null, obj, new object[1] {value});
		}

		/// <summary>
		/// Runs the given routine on the object.
		/// </summary>
		/// <param name="obj">object to run routine on</param>
		/// <param name="routine">name of routine</param>
		/// <param name="parms">any parameters to routine</param>
		private void runRoutine(object obj, string routine, object[] parms)
		{
			if (isAvailable && obj != null)
				obj.GetType().InvokeMember(routine, BindingFlags.InvokeMethod, null, obj, parms);
		}

		#endregion
	}
}
