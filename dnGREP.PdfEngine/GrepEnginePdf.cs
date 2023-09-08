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

        public IList<string> DefaultFileExtensions => new string[] { "pdf" };

        public bool IsSearchOnly => true;

        public bool PreviewPlainText { get; set; }

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            string tempFile = string.Empty;
            IGrepEngine? engine = null;
            try
            {
                // Extract text
                tempFile = ExtractText(file);
                if (!File.Exists(tempFile))
                    throw new ApplicationException("pdftotext failed to create text file.");

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                // GrepCore cannot check encoding of the original pdf file. If the encoding parameter is not default
                // then it is the user-specified code page.  If the encoding parameter *is* the default,
                // then it most likely not been set, so get the encoding of the extracted text file:
                if (encoding == Encoding.Default)
                    encoding = Utils.GetFileEncoding(tempFile);

                engine = GrepEngineFactory.GetSearchEngine(tempFile, initParams, FileFilter, searchType);
                if (engine != null)
                {
                    List<GrepSearchResult> results = engine.Search(tempFile, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);

                    if (results.Count > 0)
                    {
                        using (FileStream reader = new(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan))
                        using (StreamReader streamReader = new(reader, encoding, false, 4096, true))
                        {
                            foreach (var result in results)
                            {
                                result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, initParams.LinesBefore, initParams.LinesAfter, true);
                            }
                        }

                        foreach (GrepSearchResult result in results)
                        {
                            result.IsReadOnlyFileType = true;
                            result.FileNameDisplayed = file;
                            if (PreviewPlainText)
                            {
                                result.FileInfo.TempFile = tempFile;
                            }
                            result.FileNameReal = file;
                        }
                    }

                    return results;
                }
            }
            catch (OperationCanceledException)
            {
                // expected exception
            }
            catch (PdfToTextException ex)
            {
                return new List<GrepSearchResult>()
                {
                    new GrepSearchResult(file, searchPattern, ex.Message, false)
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to search inside PDF file: [{file}]");
                return new List<GrepSearchResult>()
                {
                    new GrepSearchResult(file, searchPattern, ex.Message, false)
                };
            }
            finally
            {
                if (engine != null)
                {
                    GrepEngineFactory.ReturnToPool(tempFile, engine);
                }
            }
            return new List<GrepSearchResult>();
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            // write the stream to a temp folder, and run the file version of the search
            string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-PDF");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            // ensure each temp file is unique, even if the file name exists elsewhere in the search tree
            string extractFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".pdf";
            string filePath = Path.Combine(tempFolder, extractFileName);

            using (var fileStream = File.Create(filePath))
            {
                input.Seek(0, SeekOrigin.Begin);
                input.CopyTo(fileStream);
            }

            var results = Search(filePath, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);

            bool isInArchive = fileName.Contains(ArchiveDirectory.ArchiveSeparator, StringComparison.Ordinal);
            if (isInArchive && results.Count > 0)
            {
                foreach (GrepSearchResult gsr in results)
                {
                    gsr.FileNameDisplayed = fileName;
                }
            }
            return results;
        }

        private string ExtractText(string pdfFilePath)
        {
            string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-PDF");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);
            // ensure each temp file is unique, even if the file name exists elsewhere in the search tree
            string fileName = Path.GetFileNameWithoutExtension(pdfFilePath) + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".txt";
            string tempFileName = Path.Combine(tempFolder, fileName);

            string longPdfFilePath = PathEx.GetLongPath(pdfFilePath);
            string options = GrepSettings.Instance.Get<string>(GrepSettings.Key.PdfToTextOptions) ?? "-layout -enc UTF-8 -bom -cfg xpdfrc";

            using Process process = new();
            // use command prompt
            process.StartInfo.FileName = pathToPdfToText;
            process.StartInfo.Arguments = string.Format("{0} \"{1}\" \"{2}\"", options, longPdfFilePath, tempFileName);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = Path.Combine(Utils.GetCurrentPath(typeof(GrepEnginePdf)), "xpdf");
            process.StartInfo.CreateNoWindow = true;
            // start cmd prompt, execute command
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
                return tempFileName;
            else
            {
                string errorMessage = string.Empty;
                errorMessage = process.ExitCode switch
                {
                    1 => Localization.Properties.Resources.Error_ErrorOpeningPDFFile,
                    2 => Localization.Properties.Resources.Error_ErrorOpeningAnOutputFile,
                    3 => Localization.Properties.Resources.Error_ErrorRelatedToPDFPermissions,
                    _ => Localization.Properties.Resources.Error_OtherError,
                };
                throw new PdfToTextException(TranslationSource.Format(Localization.Properties.Resources.Error_PdftotextReturned0Reading1, errorMessage, pdfFilePath));
            }
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
