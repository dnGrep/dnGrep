using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using dnGREP.Common;
using NLog;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Engines.Pdf
{
    /// <summary>
    /// Based on a MicrosoftWordPlugin class for AstroGrep by Curtis Beard. Thank you!
    /// </summary>
    public class GrepEnginePdf : GrepEngineBase, IGrepEngine
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private string pathToPdfToText = "";

        #region Initialization and disposal
        public override bool Initialize(GrepEngineInitParams param, FileFilter filter)
        {
            base.Initialize(param, filter);
            try
            {
                // Make sure pdftotext.exe exists
                pathToPdfToText = Utils.GetCurrentPath(typeof(GrepEnginePdf)) + "\\pdftotext.exe";
                if (File.Exists(pathToPdfToText))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, "Failed to find pdftotext.exe.", ex);
                return false;
            }
        }

        #endregion

        public bool IsSearchOnly
        {
            get { return true; }
        }

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-PDF");
            try
            {
                // Extract text
                string tempFile = ExtractText(file);
                if (!File.Exists(tempFile))
                    throw new ApplicationException("pdftotext failed to create text file.");

                // GrepCore cannot check encoding of the original pdf file. If the encoding parameter is not default
                // then it is the user-specified code page.  If the encoding parameter *is* the default,
                // then it most likely not been set, so get the encoding of the extracted text file:
                if (encoding == Encoding.Default)
                    encoding = Utils.GetFileEncoding(tempFile);

                IGrepEngine engine = GrepEngineFactory.GetSearchEngine(tempFile, initParams, FileFilter);
                List<GrepSearchResult> results = engine.Search(tempFile, searchPattern, searchType, searchOptions, encoding);

                if (results.Count > 0)
                {
                    using (FileStream reader = File.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader streamReader = new StreamReader(reader, encoding))
                    {
                        foreach (var result in results)
                        {
                            result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, initParams.LinesBefore, initParams.LinesAfter);
                        }
                    }

                    foreach (GrepSearchResult result in results)
                    {
                        result.ReadOnly = true;
                        if (file.Contains(tempFolder))
                            result.FileNameDisplayed = file.Substring(tempFolder.Length + 1);
                        else
                            result.FileNameDisplayed = file;
                        result.FileNameReal = file;
                    }
                }

                GrepEngineFactory.ReturnToPool(tempFile, engine);

                return results;
            }
            catch (Exception ex)
            {
                logger.Log<Exception>(LogLevel.Error, $"Failed to search inside PDF file: {ex.Message}", ex);
                return new List<GrepSearchResult>();
            }
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            // write the stream to a temp folder, and run the file version of the search
            string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-PDF");
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

        private string ExtractText(string pdfFilePath)
        {
            string tempFolder = Path.Combine(Utils.GetTempFolder(), "dnGREP-PDF");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);
            string tempFileName = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(pdfFilePath) + ".txt");

            if (pdfFilePath.Length > 260 && !pdfFilePath.StartsWith(@"\\?\"))
            {
                pdfFilePath = @"\\?\" + pdfFilePath;
            }

            using (Process process = new Process())
            {
                // use command prompt
                process.StartInfo.FileName = pathToPdfToText;
                process.StartInfo.Arguments = string.Format("-layout \"{0}\" \"{1}\"", pdfFilePath, tempFileName);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WorkingDirectory = Utils.GetCurrentPath(typeof(GrepEnginePdf));
                process.StartInfo.CreateNoWindow = true;
                // start cmd prompt, execute command
                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                    return tempFileName;
                else
                {
                    string errorMessage = string.Empty;
                    switch (process.ExitCode)
                    {
                        case 1:
                            errorMessage = "Error opening PDF file";
                            break;
                        case 2:
                            errorMessage = "Error opening an output file";
                            break;
                        case 3:
                            errorMessage = "Error related to PDF permissions";
                            break;
                        default:
                            errorMessage = "Unknown error";
                            break;
                    }

                    throw new Exception($"pdftotext returned '{errorMessage}' converting '{pdfFilePath}'");
                }
            }
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

        public void Unload()
        {
            //Do nothing
        }

        public override void OpenFile(OpenFileArgs args)
        {
            args.UseCustomEditor = false;
            Utils.OpenFile(args);
        }
    }
}
