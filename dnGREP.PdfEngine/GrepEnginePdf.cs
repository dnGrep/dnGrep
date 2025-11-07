using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using dnGREP.Common;
using dnGREP.Common.IO;
using dnGREP.Localization;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Engines.Pdf
{
    /// <summary>
    /// Plug-in for searching PDF documents
    /// </summary>
    public class GrepEnginePdf : GrepEngineBase, IGrepPluginEngine
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private string pathToPdfToText = string.Empty;

        #region Initialization and disposal
        public override bool Initialize(GrepEngineInitParams param, FileFilter filter)
        {
            base.Initialize(param, filter);
            try
            {
                // Make sure pdftotext.exe exists
                pathToPdfToText = Path.Combine(Utils.GetCurrentPath(typeof(GrepEnginePdf)), "xpdf", "pdftotext.exe");
                if (File.Exists(pathToPdfToText))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to find pdftotext.exe.");
                return false;
            }
        }

        #endregion

        public List<string> DefaultFileExtensions => ["pdf"];

        public bool IsSearchOnly => true;

        public bool PreviewPlainText { get; set; }

        public bool ApplyStringMap { get; set; }

        public List<GrepSearchResult> Search(string pdfFile, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            string cacheFolder = Path.Combine(Utils.GetCacheFolder(), "dnGREP-PDF");
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);

            // get the unique filename for this file using SHA256
            // if the same file exists multiple places in the search tree, all will use the same temp file
            HashOption hashOption = GrepSettings.Instance.Get<HashOption>(GrepSettings.Key.CacheFileHashType);
            string cacheFileName = hashOption == HashOption.SizeTimestamp ?
                Utils.GetTempTextFileName(new FileData(pdfFile)) :
                Utils.GetTempTextFileName(pdfFile);
            string cacheFilePath = Path.Combine(cacheFolder, cacheFileName);
            ExtractTextResults extracted;
            try
            {
                // Extract text
                extracted = ExtractText(pdfFile, cacheFilePath, encoding, pauseCancelToken);
                if (string.IsNullOrEmpty(extracted.Text) || !File.Exists(cacheFilePath))
                {
                    string message = Resources.Error_PdftotextFailedToCreateTextFile;
                    logger.Error(message + $": '{pdfFile}'");
                    return
                    [
                        new(pdfFile, searchPattern, message, false)
                    ];
                }
            }
            catch (OperationCanceledException)
            {
                // expected exception
                return [];
            }
            catch (PdfToTextException ex)
            {
                logger.Error(ex.Message); // message is sufficient, no need for stack trace
                return
                [
                    new(pdfFile, searchPattern, ex.Message, false)
                ];
            }

            return SearchPlainTextFile(pdfFile, cacheFilePath, extracted.Text, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, FileData fileData, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            string cacheFolder = Path.Combine(Utils.GetCacheFolder(), "dnGREP-PDF");
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
            ExtractTextResults extracted;
            if (File.Exists(cacheFilePath))
            {
                extracted = ReadCacheFile(cacheFilePath, encoding, false);
            }
            else
            {
                // write the stream to a temp folder, and run the file version of the search
                string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-PDF");
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

                try
                {
                    extracted = ExtractText(tempPdfFilePath, cacheFilePath, encoding, pauseCancelToken);
                    if (string.IsNullOrEmpty(extracted.Text) || !File.Exists(cacheFilePath))
                    {
                        string message = Resources.Error_PdftotextFailedToCreateTextFile;
                        logger.Error(message + $": '{fileData.FullName}'");
                        return
                        [
                            new(fileData.FullName, searchPattern, message, false)
                        ];
                    }
                }
                catch (OperationCanceledException)
                {
                    // expected exception
                    return [];
                }
                catch (PdfToTextException ex)
                {
                    logger.Error(ex.Message); // message is sufficient, no need for stack trace
                    return
                    [
                        new(fileData.FullName, searchPattern, ex.Message, false)
                    ];
                }
            }

            results = SearchPlainTextFile(fileData.FullName, cacheFilePath, extracted.Text, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);

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

        private List<GrepSearchResult> SearchPlainTextFile(string pdfFilePath, string textFilePath, string plainText, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            IGrepEngine? engine = null;
            try
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                if (!HasSearchableText(plainText))
                {
                    string message = Resources.Error_ThisPDFFileContainsNoText;
                    logger.Error(message + $": '{pdfFilePath}'");
                    return
                    [
                        new(pdfFilePath, searchPattern, message, false)
                    ];
                }

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                engine = GrepEngineFactory.GetSearchEngine(textFilePath, initParams, FileFilter, searchType);
                if (engine != null)
                {
                    bool join = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PdfJoinLines);

                    if (join)
                    {
                        plainText = TrimAllLines(plainText);
                        File.WriteAllText(textFilePath, plainText);
                    }

                    string joinedText = join ? JoinLines(plainText) : plainText;

                    using Stream inputStream = new MemoryStream(encoding.GetBytes(joinedText));
                    List<GrepSearchResult> results = engine.Search(inputStream, new(textFilePath), searchPattern,
                        searchType, searchOptions, encoding, pauseCancelToken);

                    if (results.Count > 0)
                    {
                        using Stream inputStream2 = new MemoryStream(encoding.GetBytes(plainText));
                        using (StreamReader streamReader = new(inputStream2, encoding, false, 4096, true))
                        {
                            foreach (var result in results)
                            {
                                result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, initParams.LinesBefore, initParams.LinesAfter, true);
                            }
                        }

                        foreach (GrepSearchResult result in results)
                        {
                            result.FileInfo = new(pdfFilePath);
                            result.IsReadOnlyFileType = true;
                            result.FileNameDisplayed = pdfFilePath;
                            if (PreviewPlainText)
                            {
                                result.FileInfo.TempFile = textFilePath;
                            }
                            result.FileNameReal = pdfFilePath;
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
                logger.Error(ex, $"Failed to search inside PDF file: '{pdfFilePath}'");
                return
                [
                    new(pdfFilePath, searchPattern, ex.Message, false)
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

        private ExtractTextResults ExtractText(string pdfFilePath, string cacheFilePath,
            Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            if (string.IsNullOrEmpty(cacheFilePath))
            {
                return new(string.Empty, encoding);
            }

            if (File.Exists(cacheFilePath))
            {
                // it is already extracted!
                return ReadCacheFile(cacheFilePath, encoding, false);
            }

            string longPdfFilePath = PathEx.GetLongPath(pdfFilePath);
            string options = GrepSettings.Instance.Get<string>(GrepSettings.Key.PdfToTextOptions) ?? "-layout -enc UTF-8 -bom -cfg xpdfrc";
            if (options.Contains("-eol", StringComparison.OrdinalIgnoreCase))
            {
                options = options.Replace("dos", "unix", StringComparison.OrdinalIgnoreCase);
                options = options.Replace("mac", "unix", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                options = "-eol unix " + options;
            }

            using Process process = new();
            // use command prompt
            process.StartInfo.FileName = pathToPdfToText;
            process.StartInfo.Arguments = string.Format("{0} \"{1}\" \"{2}\"", options, longPdfFilePath, cacheFilePath);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = Path.Combine(Utils.GetCurrentPath(typeof(GrepEnginePdf)), "xpdf");
            process.StartInfo.CreateNoWindow = true;
            // start cmd prompt, execute command
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return ReadCacheFile(cacheFilePath, encoding, ApplyStringMap);
            }
            else
            {
                string errorMessage = string.Empty;
                errorMessage = process.ExitCode switch
                {
                    1 => Resources.Error_ErrorOpeningPDFFile,
                    2 => Resources.Error_ErrorOpeningAnOutputFile,
                    3 => Resources.Error_ErrorRelatedToPDFPermissions,
                    _ => Resources.Error_OtherError,
                };
                throw new PdfToTextException(TranslationSource.Format(Resources.Error_PdftotextReturned0, errorMessage));
            }
        }

        private static ExtractTextResults ReadCacheFile(string cacheFilePath, Encoding encoding, bool applyStringMap)
        {
            Encoding fileEncoding = encoding;
            string textOut;
            // GrepCore cannot check encoding of the original pdf file. If the encoding parameter is
            // not default then it is the user-specified code page.  If the encoding parameter *is*
            // the default, then it most likely not been set, so get the encoding of the extracted
            // text file:
            if (encoding == Encoding.Default)
                fileEncoding = Utils.GetFileEncoding(cacheFilePath);

            using (StreamReader streamReader = new(cacheFilePath, fileEncoding, detectEncodingFromByteOrderMarks: false))
                textOut = streamReader.ReadToEnd();

            if (applyStringMap)
            {
                StringMap subs = GrepSettings.Instance.GetSubstitutionStrings();
                textOut = subs.ReplaceAllKeys(textOut);

                // and write it back to cache
                using StreamWriter writer = new(cacheFilePath, false, fileEncoding, 4096);
                writer.Write(textOut);
            }

            return new ExtractTextResults(textOut, fileEncoding);
        }

        private static bool HasSearchableText(string text)
        {
            foreach (char c in text.AsSpan())
            {
                if (char.IsLetterOrDigit(c) || char.IsBetween(c, ' ', (char)255))
                {
                    return true;
                }
            }
            return false;
        }

        private static string TrimAllLines(string text)
        {
            StringBuilder sb = new(text.Length);
            string[] lines = text.Split('\n');

            foreach (string line in lines)
            {
                sb.Append(line.Trim(' ')).Append('\n');
            }

            string result = sb.ToString();
            return result;
        }

        private static string JoinLines(string text)
        {
            char[] chars = text.ToCharArray();
            for (int idx = 1; idx < chars.Length - 1; idx++)
            {
                if (chars[idx] == '\n' && chars[idx + 1] != '\n' && chars[idx - 1] != '\n')
                {
                    chars[idx] = ' ';
                }
            }

            string result = new(chars);
            return result;
        }

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems, PauseCancelToken pauseCancelToken)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public Version? FrameworkVersion => Assembly.GetAssembly(typeof(IGrepEngine))?.GetName()?.Version;

        public void Unload()
        {
            //Do nothing
        }

        public override void OpenFile(OpenFileArgs args)
        {
            Utils.OpenFile(args);
        }
    }
}
