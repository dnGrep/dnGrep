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
		public GrepEngineWord() : this(false, 0, 0) { }

		public GrepEngineWord(bool showLinesInContext, int linesBefore, int linesAfter)
			:
			base(showLinesInContext, linesBefore, linesAfter)
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

		//public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, bool isCaseSensitive, bool isMultiline, Encoding encoding)
		//{
		//    load();
		//    List<GrepSearchResult> results = new List<GrepSearchResult>();
		//    if (isAvailable && isLoaded)
		//    {
		//        try
		//        {
		//            if (File.Exists(file))
		//            {
		//                int count = 0;
		//                GrepSearchResult hit = new GrepSearchResult();
		//                int prevLine = 0;
		//                int prevPage = 0;

		//                // Open a given Word document as readonly
		//                object wordDocument = openDocument(file, true);

		//                // Get Selection Property
		//                wordSelection = wordApplication.GetType().InvokeMember("Selection", BindingFlags.GetProperty,
		//                    null, wordApplication, null);

		//                // create range and find objects
		//                object range = getProperty(wordDocument, "Content");
		//                object find = getProperty(range, "Find");

		//                // setup find
		//                runRoutine(find, "ClearFormatting", null);
		//                setProperty(find, "Forward", true);
		//                setProperty(find, "Text", searchPattern);
		//                //SetProperty(find, "MatchWholeWord", __wholeWordMatch);
		//                setProperty(find, "MatchCase", isCaseSensitive);

		//                // start find
		//                findExecute(find);

		//                // keep finding text
		//                while ((bool)getProperty(find, "Found") == true)
		//                {
		//                    count += 1;

		//                    if (count == 1)
		//                    {
		//                        // create hit object
		//                        hit = new GrepSearchResult(file, new List<GrepSearchResult.GrepLine>());
		//                        results.Add(hit);
		//                    }

		//                    // retrieve find information
		//                    int start = (int)getProperty(range, "Start");
		//                    int colNum = (int)information(range, WdInformation.wdFirstCharacterColumnNumber);
		//                    int lineNum = (int)information(range, WdInformation.wdFirstCharacterLineNumber);
		//                    int pageNum = (int)information(range, WdInformation.wdActiveEndPageNumber);
		//                    string line = getFindTextLine(start);

		//                    // don't add a hit if (on same line
		//                    if (!(prevLine == lineNum && prevPage == pageNum))
		//                    {
		//                        hit.SearchResults.Add(new GrepSearchResult.GrepLine(lineNum, line, false));
								
		//                        // add context lines before
		//                        // if (__contextLines > 0){
		//                        //    For i As int = __contextLines To 1 Step -1
		//                        //       SetProperty(__WordSelection, "Start", start)
		//                        //       SelectionMoveUp(WdUnits.wdLine, i, WdMovementType.wdMove)
		//                        //       Dim cxt As string = GetFindTextLine()
		//                        //       cxt = RemoveSpecialCharacters(cxt)

		//                        //       if (Not HitExists(cxt, hit)){
		//                        //          hit.Add(_contextSpacer & cxt & NEW_LINE, lineNum - i, 1)
		//                        //       End If
		//                        //    Next
		//                        // End If


		//                        // add context lines after
		//                        // if (__contextLines > 0){
		//                        //    For i As int = 1 To __contextLines
		//                        //       SetProperty(__WordSelection, "Start", start)
		//                        //       SelectionMoveDown(WdUnits.wdLine, i, WdMovementType.wdMove)
		//                        //       Dim cxt As string = GetFindTextLine()
		//                        //       cxt = RemoveSpecialCharacters(cxt)

		//                        //       if (Not HitExists(cxt, hit)){
		//                        //          hit.Add(_contextSpacer & cxt & NEW_LINE, lineNum + i, 1)
		//                        //       End If
		//                        //    Next
		//                        // End If
		//                    }

		//                    prevLine = lineNum;
		//                    prevPage = pageNum;

		//                    // find again
		//                    findExecute(find);
		//                }

		//                releaseSelection();
		//                closeDocument(wordDocument);						
		//            }
		//            else
		//            {
		//                logger.Log(LogLevel.Error, "File to perform search. File not found.");
		//            }
		//        }
		//        catch (Exception ex)
		//        {
		//            logger.LogException(LogLevel.Error, "Word search failed.", ex);
		//        } finally {
		//            unload();
		//        }
		//    }
		//    else
		//    {
		//        // Plugin not available or not loaded
		//    }
		//    return results;
		//}

		public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, bool isCaseSensitive, bool isMultiline, Encoding encoding)
		{
			load();
			SearchDelegates.DoSearchMultiline searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
			switch (searchType)
			{
				case SearchType.PlainText:
				case SearchType.XPath:
					if (isCaseSensitive)
					{
						searchMethodMultiline = doTextSearchCaseSensitiveMultiline;
					}
					else
					{
						searchMethodMultiline = doTextSearchCaseInsensitiveMultiline;
					}
					break;
				case SearchType.Regex:
					if (isCaseSensitive)
					{
						searchMethodMultiline = doRegexSearchCaseSensitiveMultiline;
					}
					else
					{
						searchMethodMultiline = doRegexSearchCaseInsensitiveMultiline;
					}
					break;
			}

			List<GrepSearchResult> result = searchMultiline(file, searchPattern, searchMethodMultiline);
			unload();
			return result;
		}

		private List<GrepSearchResult> searchMultiline(string file, string searchPattern, SearchDelegates.DoSearchMultiline searchMethod)
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


				List<GrepSearchResult.GrepLine> lines = new List<GrepSearchResult.GrepLine>();
				lines = searchMethod(Utils.CleanLineBreaks(text.ToString()), searchPattern);
				Utils.CleanResults(ref lines);
				if (lines.Count > 0)
				{
					searchResults.Add(new GrepSearchResult(file, lines));
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

		public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, bool isCaseSensitive, bool isMultiline, Encoding encoding)
		{
			throw new Exception("The method or operation is not implemented.");
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

			base.Initialize(showLinesInContext, linesBefore, linesAfter);
		}

		/// <summary>
		/// Unloads Microsoft Word.
		/// </summary>
		private void unload()
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

		///// <summary>Units enum [for line selection]</summary>
		//private enum WdUnits
		//{
		//    wdLine = 5
		//}

		///// <summary>MovementType enum [for line movement]</summary>
		//private enum WdMovementType
		//{
		//    wdMove = 0,
		//    wdExtend = 1
		//}

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

		///// <summary>
		///// Executes the Word find method.
		///// </summary>
		///// <param name="find">Word's find object</param>
		//private void findExecute(object find)
		//{
		//    if (isAvailable && find != null)
		//    {
		//        find.GetType().InvokeMember("Execute", BindingFlags.InvokeMethod, null, find, new object[] {});
		//    }
		//}

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

		///// <summary>
		///// Simulates pressing the home key.
		///// </summary>
		///// <param name="unit">Unit to select on</param>
		//private void selectionHomeKey(WdUnits unit)
		//{
		//    runRoutine(wordSelection, "HomeKey", new object[1] {(int)unit});
		//}

		///// <summary>
		///// Simulates pressing the home key.
		///// </summary>
		///// <param name="unit">Unit to select on</param>
		///// <param name="extend">Movement type</param>
		//private void selectionHomeKey(WdUnits unit, WdMovementType extend)
		//{
		//    runRoutine(wordSelection, "HomeKey", new object[2] {(int)unit, (int)extend});
		//}

		///// <summary>
		///// Simulates pressing the end key.
		///// </summary>
		///// <param name="unit">Unit to select on</param>
		//private void selectionEndKey(WdUnits unit)
		//{
		//    runRoutine(wordSelection, "EndKey", new object[1] {(int)unit});
		//}

		///// <summary>
		///// Simulates pressing the end key.
		///// </summary>
		///// <param name="unit">Unit to select on</param>
		///// <param name="extend">Movement type</param>
		//private void selectionEndKey(WdUnits unit, WdMovementType extend)
		//{
		//    runRoutine(wordSelection, "EndKey", new object[2] {(int)unit, (int)extend});
		//}

		///// <summary>
		///// Simulates pressing the up arrow key.
		///// </summary>
		///// <param name="unit">Unit to select on</param>
		///// <param name="count">number of presses</param>
		///// <param name="extend">Movement type</param>
		//private void selectionMoveUp(WdUnits unit, int count, WdMovementType extend)
		//{
		//    runRoutine(wordSelection, "MoveUp", new object[3] {(int)unit, count, (int)extend});
		//}

		///// <summary>
		///// Simulates pressing the up arrow key.
		///// </summary>
		///// <param name="unit">Unit to select on</param>
		///// <param name="count">number of presses</param>
		///// <param name="extend">Movement type</param>
		//private void selectionMoveDown(WdUnits unit, int count, WdMovementType extend)
		//{
		//    runRoutine(wordSelection, "MoveDown", new object[3] {(int)unit, count, (int)extend});
		//}

		///// <summary>
		///// Returns the next line that contains the text to find.
		///// </summary>
		///// <returns>line containing the search string</returns>
		//private string getFindTextLine()
		//{
		//    try
		//    {
		//        selectionHomeKey(WdUnits.wdLine);
		//        selectionEndKey(WdUnits.wdLine, WdMovementType.wdExtend);

		//        return getProperty(wordSelection, "Text").ToString();
		//    }
		//    catch (Exception ex)
		//    {
		//        logger.LogException(LogLevel.Error, "Failed to find next line to search.", ex);
		//    }

		//    return string.Empty;
		//}

		///// <summary>
		///// Returns the next line that contains the text to find.
		///// </summary>
		///// <param name="start">start position in line</param>
		///// <returns>line containing the search string</returns>
		//private string getFindTextLine(int start)
		//{
		//    try
		//    {
		//        setProperty(wordSelection, "Start", start);
		//        selectionHomeKey(WdUnits.wdLine);
		//        selectionEndKey(WdUnits.wdLine, WdMovementType.wdExtend);

		//        return getProperty(wordSelection, "Text").ToString();
		//    }
		//    catch (Exception ex)
		//    {
		//        logger.LogException(LogLevel.Error, "Failed to find next line to search.", ex);
		//    }

		//    return string.Empty;
		//}

		///// <summary>
		///// Removes extra and unneeded characters from the given line.
		///// </summary>
		///// <param name="line">line to clean</param>
		///// <returns>cleaned line</returns>
		//private string removeSpecialCharacters(string line)
		//{
		//    string cleanLine = line;

		//    if (cleanLine.EndsWith("\r\n"))
		//        cleanLine = cleanLine.Substring(0, cleanLine.LastIndexOf("\r\n"));
		//    else if (cleanLine.EndsWith("\r"))
		//        cleanLine = cleanLine.Substring(0, cleanLine.LastIndexOf("\r"));
		//    else if (cleanLine.EndsWith("\n"))
		//        cleanLine = cleanLine.Substring(0, cleanLine.LastIndexOf("\n"));

		//    if (cleanLine.EndsWith("\a"))
		//        cleanLine = cleanLine.Substring(0, cleanLine.LastIndexOf("\a"));

		//    if (cleanLine.EndsWith("\r\n"))
		//        cleanLine = cleanLine.Substring(0, cleanLine.LastIndexOf("\r\n"));
		//    else if (cleanLine.EndsWith("\r"))
		//        cleanLine = cleanLine.Substring(0, cleanLine.LastIndexOf("\r"));
		//    else if (cleanLine.EndsWith("\n"))
		//        cleanLine = cleanLine.Substring(0, cleanLine.LastIndexOf("\n"));

		//    return cleanLine;
		//}

		#endregion
	}
}
