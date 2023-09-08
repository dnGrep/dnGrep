using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using dnGREP.Common;
using dnGREP.Common.IO;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Engines.Word
{
    /// <summary>
    /// Based on a MicrosoftWordPlugin class for AstroGrep by Curtis Beard. Thank you!
    /// </summary>
    public class GrepEngineWord : GrepEngineBase, IGrepPluginEngine, IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private bool isLoaded = false;
        private Type? wordType;
        private object? wordApplication;
        private object? wordDocuments;

        private readonly object MISSING_VALUE = Missing.Value;
        private const int msoAutomationSecurityForceDisable = 3;
        private const int wdDoNotSaveChanges = 0;

        #region Initialization and disposal
        public GrepEngineWord()
        {
            try
            {
                wordType = Type.GetTypeFromProgID("Word.Application");
            }
            catch (Exception ex)
            {
                wordType = null;
                logger.Error(ex, "Failed to initialize Word.");
            }
        }

        /// <summary>
        /// Handles disposing of the object.
        /// </summary>
        /// <history>
        /// </history>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Unload();
            if (wordType != null && wordApplication != null)
            {
                // Close the application.
                wordApplication.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null,
                    wordApplication, Array.Empty<object>());
            }

            if (wordApplication != null)
                Marshal.ReleaseComObject(wordApplication);

            wordApplication = null;
            wordType = null;
        }

        /// <summary>
        /// Destructor. Calls Dispose().
        /// </summary>
        ~GrepEngineWord()
        {
            Dispose();
        }

        #endregion

        public IList<string> DefaultFileExtensions => new string[] { "doc" };

        public bool IsSearchOnly => true;

        public bool PreviewPlainText { get; set; }


        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            Load();
            if (!isLoaded)
            {
                return new List<GrepSearchResult>
                {
                    new GrepSearchResult(file, searchPattern, Resources.Error_DocumentReadFailed, false)
                };
            }
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

            List<GrepSearchResult> result = SearchMultiline(file, searchPattern, searchOptions,
                searchMethodMultiline, pauseCancelToken);
            return result;
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            // write the stream to a temp folder, and run the file version of the search
            string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-WORD");
            // the fileName may contain the partial path of the directory structure in the archive
            string filePath = Path.Combine(tempFolder, fileName);

            // use the directory name to also include folders within the archive
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var fileStream = File.Create(filePath))
            {
                input.Seek(0, SeekOrigin.Begin);
                input.CopyTo(fileStream);
            }

            return Search(filePath, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);
        }

        private List<GrepSearchResult> SearchMultiline(string file, string searchPattern, GrepSearchOption searchOptions,
            SearchDelegates.DoSearch searchMethod, PauseCancelToken pauseCancelToken)
        {
            List<GrepSearchResult> searchResults = new();

            try
            {
                // Open a given Word document as readonly
                object? wordDocument = OpenDocument(file, true);
                if (wordDocument != null)
                {
                    // create range and find objects
                    object? range = GetProperty(wordDocument, "Content");
                    if (range != null)
                    {
                        // create text
                        object? text = GetProperty(range, "Text");
                        if (text != null)
                        {
                            string docText = Utils.CleanLineBreaks(text.ToString() ?? string.Empty);
                            var lines = searchMethod(-1, 0, docText, searchPattern,
                                searchOptions, true, pauseCancelToken);
                            if (lines.Count > 0)
                            {
                                GrepSearchResult result = new(file, searchPattern, lines, Encoding.Default);
                                using (StringReader reader = new(docText))
                                {
                                    result.SearchResults = Utils.GetLinesEx(reader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                                }
                                result.IsReadOnlyFileType = true;
                                if (PreviewPlainText)
                                {
                                    result.FileInfo.TempFile = WriteTempFile(docText, file);
                                }
                                searchResults.Add(result);
                            }
                        }
                    }
                    CloseDocument(wordDocument);
                }
            }
            catch (OperationCanceledException)
            {
                // expected exception
                searchResults.Clear();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to search inside Word file");
                searchResults.Add(new GrepSearchResult(file, searchPattern, ex.Message, false));
            }
            return searchResults;
        }

        private static string WriteTempFile(string text, string filePath)
        {
            string tempFolder = Path.Combine(Utils.GetTempFolder(), $"dnGREP-WORD");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            // ensure each temp file is unique, even if the file name exists elsewhere in the search tree
            string fileName = Path.GetFileNameWithoutExtension(filePath) + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".txt";
            string tempFileName = Path.Combine(tempFolder, fileName);

            File.WriteAllText(tempFileName, text);
            return tempFileName;
        }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Version? FrameworkVersion => Assembly.GetAssembly(typeof(IGrepEngine))?.GetName()?.Version;

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
            try
            {
                if (wordType != null && !isLoaded)
                {
                    // load word
                    wordApplication = Activator.CreateInstance(wordType);
                    if (wordApplication != null)
                    {
                        bool visible = false;
                        // set visible state
                        wordApplication.GetType().InvokeMember("Visible", BindingFlags.SetProperty, null,
                            wordApplication, new object[1] { visible });

                        // set automation security
                        wordApplication.GetType().InvokeMember("AutomationSecurity", BindingFlags.SetProperty, null,
                            wordApplication, new object[1] { msoAutomationSecurityForceDisable });

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
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load Word and create Document.");
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
                        wordApplication, new object[] { wdDoNotSaveChanges });
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to unload Word.");
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
                    logger.Error(ex, "Failed to release Word object.");
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
        private object? OpenDocument(string path, bool readOnly)
        {
            if (wordType != null && wordDocuments != null && wordDocuments != null)
            {
                if (path.Length > 255)  // 255 for Word!
                    path = PathEx.GetShort83Path(path);

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
        private static void CloseDocument(object doc)
        {
            doc?.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, doc,
                    new object[] { wdDoNotSaveChanges });

        }

        /// <summary>
        /// Gets the specified property from the given object.
        /// </summary>
        /// <param name="obj">Object to get property from</param>
        /// <param name="prop">name of property to retrieve</param>
        /// <returns>Property object</returns>
        private static object? GetProperty(object obj, string prop)
        {
            return obj?.GetType().InvokeMember(prop,
                BindingFlags.GetProperty, null, obj, Array.Empty<object>());
        }

        #endregion
    }
}
