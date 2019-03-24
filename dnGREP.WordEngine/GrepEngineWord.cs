using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using dnGREP.Common;
using NLog;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Engines.Word
{
    /// <summary>
    /// Based on a MicrosoftWordPlugin class for AstroGrep by Curtis Beard. Thank you!
    /// </summary>
    public class GrepEngineWord : GrepEngineBase, IGrepEngine, IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private bool isAvailable = false;
        private bool isLoaded = false;
        private Type wordType;
        private object wordApplication;
        private object wordDocuments;

        private readonly object MISSING_VALUE = Missing.Value;

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
                logger.Log<Exception>(LogLevel.Error, "Failed to initialize Word.", ex);
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
                    wordApplication, new object[] { });
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

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            Load();
            SearchDelegates.DoSearch searchMethodMultiline = DoTextSearch;
            switch (searchType)
            {
                case SearchType.PlainText:
                case SearchType.XPath:
                    searchMethodMultiline = DoTextSearch;
                    break;
                case SearchType.Regex:
                    searchMethodMultiline = DoRegexSearch;
                    break;
                case SearchType.Soundex:
                    searchMethodMultiline = DoFuzzySearch;
                    break;
            }

            List<GrepSearchResult> result = SearchMultiline(file, searchPattern, searchOptions, searchMethodMultiline);
            return result;
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            // write the stream to a temp folder, and run the file version of the search
            string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-WORD");
            // the fileName may contain the partial path of the directory structure in the archive
            string filePath = Path.Combine(tempFolder, fileName);

            // use the directory name to also include folders within the archive
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var fileStream = File.Create(filePath))
            {
                input.Seek(0, SeekOrigin.Begin);
                input.CopyTo(fileStream);
            }

            return Search(filePath, searchPattern, searchType, searchOptions, encoding);
        }

        private List<GrepSearchResult> SearchMultiline(string file, string searchPattern, GrepSearchOption searchOptions, SearchDelegates.DoSearch searchMethod)
        {
            List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

            try
            {
                // Open a given Word document as readonly
                object wordDocument = OpenDocument(file, true);
                if (wordDocument != null)
                {
                    // create range and find objects
                    object range = GetProperty(wordDocument, "Content");

                    // create text
                    object text = GetProperty(range, "Text");

                    string docText = Utils.CleanLineBreaks(text.ToString());
                    var lines = searchMethod(-1, 0, docText, searchPattern, searchOptions, true);
                    if (lines.Count > 0)
                    {
                        GrepSearchResult result = new GrepSearchResult(file, searchPattern, lines, Encoding.Default);
                        using (StringReader reader = new StringReader(docText))
                        {
                            result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                        }
                        result.ReadOnly = true;
                        searchResults.Add(result);
                    }
                    CloseDocument(wordDocument);
                }
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, "Failed to search inside Word file", ex);
            }
            return searchResults;
        }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Version FrameworkVersion
        {
            get { return Assembly.GetAssembly(typeof(IGrepEngine)).GetName().Version; }
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
        private void Load()
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
                logger.Log<Exception>(LogLevel.Error, "Failed to load Word and create Document.", ex);
            }

            base.Initialize(initParams, FileFilter);
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
                        wordApplication, new object[] { });
                }
                catch (Exception ex)
                {
                    logger.Log<Exception>(LogLevel.Error, "Failed to unload Word.", ex);
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
                    logger.Log<Exception>(LogLevel.Error, "Failed to release Word object.", ex);
                }
            }

            wordApplication = null;
            isLoaded = false;
        }

        /// <summary>
        /// Opens and returns the Word's document object for the given file.
        /// </summary>
        /// <param name="path">Full path to file.</param>
        /// <param name="readOnly">True for readonly, False for full access.</param>
        /// <returns>Word's Document object if success, null otherwise</returns>
        private object OpenDocument(string path, bool readOnly)
        {
            if (isAvailable && wordDocuments != null && wordDocuments != null)
            {
                if (path.Length > 255)  // 255 for Word!
                    path = Path.GetShort83Path(path);

                bool addToRecentFiles = false;
                return wordDocuments.GetType().InvokeMember("Open", BindingFlags.InvokeMethod,
                    null, wordDocuments, new object[4] { path, MISSING_VALUE, readOnly, addToRecentFiles });
            }

            return null;
        }

        /// <summary>
        /// Closes the given Word Document object.
        /// </summary>
        /// <param name="doc">Word Document object</param>
        private void CloseDocument(object doc)
        {
            if (isAvailable && doc != null)
                doc.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, doc, new object[] { });
        }

        /// <summary>
        /// Gets the specified property from the given object.
        /// </summary>
        /// <param name="obj">Object to get property from</param>
        /// <param name="prop">name of property to retrieve</param>
        /// <returns>Property object</returns>
        private object GetProperty(object obj, string prop)
        {
            if (isAvailable && obj != null)
                return obj.GetType().InvokeMember(prop, BindingFlags.GetProperty, null, obj, new object[] { });

            return null;
        }

        #endregion
    }
}
