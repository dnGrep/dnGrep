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
                    wordApplication, []);
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

        public List<string> DefaultFileExtensions => ["doc"];

        public bool IsSearchOnly => true;

        public bool PreviewPlainText { get; set; }

        public bool ApplyStringMap { get; set; }

        public List<GrepSearchResult> Search(string wordFilePath, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            string cacheFolder = Path.Combine(Utils.GetCacheFolder(), "dnGREP-Word");
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);

            // get the unique filename for this file using SHA256
            // if the same file exists multiple places in the search tree, all will use the same temp file
            HashOption hashOption = GrepSettings.Instance.Get<HashOption>(GrepSettings.Key.CacheFileHashType);
            string cacheFileName = hashOption == HashOption.SizeTimestamp ?
                Utils.GetTempTextFileName(new FileData(wordFilePath)) :
                Utils.GetTempTextFileName(wordFilePath);
            string cacheFilePath = Path.Combine(cacheFolder, cacheFileName);
            try
            {
                // Extract text
                if (!ExtractText(wordFilePath, cacheFilePath) || !File.Exists(cacheFilePath))
                {
                    logger.Error(Resources.Error_DocumentReadFailed + $": '{wordFilePath}'");
                    return
                    [
                        new(wordFilePath, searchPattern, Resources.Error_DocumentReadFailed, false)
                    ];
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to search inside Word file '{0}'", wordFilePath);
                return
                [
                    new(wordFilePath, searchPattern, ex.Message, false)
                ];
            }

            return SearchPlainTextFile(wordFilePath, cacheFilePath, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, FileData fileData, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            string cacheFolder = Path.Combine(Utils.GetCacheFolder(), "dnGREP-Word");
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);

            // the filePath may contain the partial path of the directory structure in the archive
            string fileName = Path.GetFileName(fileData.FullName);

            // get the unique filename for this file using SHA256
            // if the same file exists multiple places in the search tree, all will use the same temp file
            HashOption hashOption = GrepSettings.Instance.Get<HashOption>(GrepSettings.Key.CacheFileHashType);
            string cacheFileName = hashOption == HashOption.SizeTimestamp ?
                Utils.GetTempTextFileName(fileData) :
                Utils.GetTempTextFileName(input, fileName);
            string cacheFilePath = Path.Combine(cacheFolder, cacheFileName);

            List<GrepSearchResult> results = [];
            if (!File.Exists(cacheFilePath))
            {
                // write the stream to a temp folder, and run the file version of the search
                string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-Word");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                // ensure each temp file is unique, even if the file name exists elsewhere in the search tree
                string extractFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".pdf";
                string tempPdfFilePath = Path.Combine(tempFolder, extractFileName);

                using (var fileStream = File.Create(tempPdfFilePath))
                {
                    input.Seek(0, SeekOrigin.Begin);
                    input.CopyTo(fileStream);
                }

                // Extract text
                try
                {
                    if (!ExtractText(tempPdfFilePath, cacheFilePath) || !File.Exists(cacheFilePath))
                    {
                        logger.Error(Resources.Error_DocumentReadFailed + $": '{fileData.FullName}'");
                        return
                        [
                            new(fileData.FullName, searchPattern, Resources.Error_DocumentReadFailed, false)
                        ];
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to search inside Word file '{0}'", fileData.FullName);
                    return
                    [
                        new(fileData.FullName, searchPattern, ex.Message, false)
                    ];
                }
            }

            results = SearchPlainTextFile(fileData.FullName, cacheFilePath, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);

            bool isInArchive = fileData.FullName.Contains(ArchiveDirectory.ArchiveSeparator, StringComparison.Ordinal);
            if (isInArchive && results.Count > 0)
            {
                foreach (GrepSearchResult gsr in results)
                {
                    gsr.FileNameDisplayed = fileData.FullName;
                }
            }
            return results;
        }

        private List<GrepSearchResult> SearchPlainTextFile(string wordFilePath, string textFilePath, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            IGrepEngine? engine = null;
            try
            {
                // GrepCore cannot check encoding of the original word file. If the encoding parameter is not default
                // then it is the user-specified code page.  If the encoding parameter *is* the default,
                // then it most likely not been set, so get the encoding of the extracted text file:
                if (encoding == Encoding.Default)
                    encoding = Utils.GetFileEncoding(textFilePath);

                string text;
                using (StreamReader sr = new(textFilePath, encoding, detectEncodingFromByteOrderMarks: false))
                {
                    text = sr.ReadToEnd();
                }

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                engine = GrepEngineFactory.GetSearchEngine(textFilePath, initParams, FileFilter, searchType);
                if (engine != null)
                {
                    using Stream inputStream = new MemoryStream(encoding.GetBytes(text));
                    List<GrepSearchResult> results = engine.Search(inputStream, new(textFilePath), searchPattern,
                        searchType, searchOptions, encoding, pauseCancelToken);

                    if (results.Count > 0)
                    {
                        inputStream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader streamReader = new(inputStream, encoding, false, 4096, true))
                        {
                            foreach (var result in results)
                            {
                                result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, initParams.LinesBefore, initParams.LinesAfter, true);
                            }
                        }

                        foreach (GrepSearchResult result in results)
                        {
                            result.FileInfo = new(wordFilePath);
                            result.IsReadOnlyFileType = true;
                            result.FileNameDisplayed = wordFilePath;
                            if (PreviewPlainText)
                            {
                                result.FileInfo.TempFile = textFilePath;
                            }
                            result.FileNameReal = wordFilePath;
                        }
                    }

                    return results;
                }
            }
            catch (OperationCanceledException)
            {
                // expected exception
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to search inside Word file: '{wordFilePath}'");
                return
                [
                    new(wordFilePath, searchPattern, ex.Message, false)
                ];
            }
            finally
            {
                if (engine != null)
                {
                    GrepEngineFactory.ReturnToPool(textFilePath, engine);
                }
            }
            return [];
        }

        private bool ExtractText(string wordFilePath, string cacheFilePath)
        {
            if (string.IsNullOrEmpty(cacheFilePath))
            {
                return false;
            }

            if (File.Exists(cacheFilePath))
            {
                // it is already extracted!
                return true;
            }

            Load();
            if (!isLoaded)
            {
                return false;
            }

            // Open a given Word document as readonly
            object? wordDocument = OpenDocument(wordFilePath, true);
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

                        if (ApplyStringMap)
                        {
                            StringMap subs = GrepSettings.Instance.GetSubstitutionStrings();
                            docText = subs.ReplaceAllKeys(docText);
                        }

                        File.WriteAllText(cacheFilePath, docText);
                    }
                }
                CloseDocument(wordDocument);
            }

            return true;
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

            base.Initialize(initParams, FileFilter, PasswordService);
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
                BindingFlags.GetProperty, null, obj, []);
        }

        #endregion
    }
}
