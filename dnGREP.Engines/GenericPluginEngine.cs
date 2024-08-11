using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using dnGREP.Common;
using dnGREP.Localization;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Engines
{
    /// <summary>
    /// Plug-in for searching complex documents using an external command to extract plain text
    /// </summary>
    public class GenericPluginEngine(string name, string application, string arguments, string workingDirectory)
        : GrepEngineBase, IGrepPluginEngine
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private string Name { get; } = name;
        private string Application { get; } = application;
        private string Arguments { get; } = arguments;
        private string WorkingDirectory { get; } = workingDirectory;

        public List<string> DefaultFileExtensions => [];

        public bool IsSearchOnly => true;

        public bool PreviewPlainText { get; set; }

        public List<GrepSearchResult> Search(string filePath, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            string cacheFolder = Path.Combine(Utils.GetCacheFolder(), $"dnGREP-{Name}");
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);

            // get the unique filename for this file using SHA256
            // if the same file exists multiple places in the search tree, all will use the same temp file
            string cacheFileName = Utils.GetTempTextFileName(filePath);
            string cacheFilePath = Path.Combine(cacheFolder, cacheFileName);
            ExtractTextResults extracted;
            try
            {
                // Extract text
                extracted = ExtractText(filePath, cacheFilePath, encoding, pauseCancelToken);
                encoding = extracted.Encoding;

                if (string.IsNullOrEmpty(extracted.Text))
                {
                    string message = TranslationSource.Format(Resources.Error_ThePluginFailedToExtractAnyText, Name);
                    logger.Error(message + $": '{filePath}'");
                    return
                    [
                        new(filePath, searchPattern, message, false)
                    ];
                }

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                if (!HasSearchableText(extracted.Text))
                {
                    string message = TranslationSource.Format(
                        Resources.Error_TheFileCreatedByThePluginHasNoSearchableText, Name);

                    logger.Error(message + $": '{filePath}'");
                    return
                    [
                        new(filePath, searchPattern, message, false)
                    ];
                }

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                // expected exception
                return [];
            }

            return SearchPlainTextFile(filePath, cacheFilePath, extracted.Text, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, string origFilePath, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            string cacheFolder = Path.Combine(Utils.GetCacheFolder(), $"dnGREP-{Name}");
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);

            // the filePath may contain the partial path of the directory structure in the archive
            string fileName = Path.GetFileName(origFilePath);

            // get the unique filename for this file using SHA256
            // if the same file exists multiple places in the search tree, all will use the same temp file
            string cacheFileName = Utils.GetTempTextFileName(input, fileName);
            string cacheFilePath = Path.Combine(cacheFolder, cacheFileName);

            List<GrepSearchResult> results = [];
            ExtractTextResults extracted;
            if (File.Exists(cacheFilePath))
            {
                extracted = ReadCacheFile(cacheFilePath, encoding);
            }
            else
            {
                // write the stream to a temp folder, and run the file version of the search
                string tempFolder = Path.Combine(Utils.GetTempFolder(), $"dnGREP-{Name}");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                // ensure each temp file is unique, even if the file name exists elsewhere in the search tree
                string extractFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(fileName);
                string tempFilePath = Path.Combine(tempFolder, extractFileName);

                using (var fileStream = File.Create(tempFilePath))
                {
                    input.Seek(0, SeekOrigin.Begin);
                    input.CopyTo(fileStream);
                }

                try
                {
                    extracted = ExtractText(tempFilePath, cacheFilePath, encoding, pauseCancelToken);
                    encoding = extracted.Encoding;

                    if (string.IsNullOrEmpty(extracted.Text))
                    {
                        string message = TranslationSource.Format(Resources.Error_ThePluginFailedToExtractAnyText, Name);
                        logger.Error(message + $": '{origFilePath}'");
                        return
                        [
                            new(origFilePath, searchPattern, message, false)
                        ];
                    }

                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                    if (!HasSearchableText(extracted.Text))
                    {
                        string message = TranslationSource.Format(
                            Resources.Error_TheFileCreatedByThePluginHasNoSearchableText, Name);

                        logger.Error(message + $": '{origFilePath}'");
                        return
                        [
                            new(origFilePath, searchPattern, message, false)
                        ];
                    }

                    pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    // expected exception
                    return [];
                }
            }

            results = SearchPlainTextFile(origFilePath, cacheFilePath, extracted.Text, searchPattern, searchType, searchOptions, encoding, pauseCancelToken);

            bool isInArchive = origFilePath.Contains(ArchiveDirectory.ArchiveSeparator, StringComparison.Ordinal);
            if (isInArchive && results.Count > 0)
            {
                foreach (GrepSearchResult gsr in results)
                {
                    gsr.FileNameDisplayed = origFilePath;
                }
            }
            return results;
        }

        private List<GrepSearchResult> SearchPlainTextFile(string filePath, string textFilePath, string plainText, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            IGrepEngine? engine = null;
            try
            {
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                engine = GrepEngineFactory.GetSearchEngine(textFilePath, initParams, FileFilter, searchType);
                if (engine != null)
                {
                    using Stream inputStream = new MemoryStream(encoding.GetBytes(plainText));
                    List<GrepSearchResult> results = engine.Search(inputStream, textFilePath, searchPattern,
                        searchType, searchOptions, encoding, pauseCancelToken);

                    if (results.Count > 0)
                    {
                        inputStream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader streamReader = new(inputStream, encoding, false, 4096, true))
                        {
                            foreach (var result in results)
                            {
                                result.SearchResults = Utils.GetLinesEx(streamReader, result.Matches, initParams.LinesBefore, initParams.LinesAfter, false);
                            }
                        }

                        foreach (GrepSearchResult result in results)
                        {
                            result.FileInfo = new(filePath);
                            result.IsReadOnlyFileType = true;
                            result.FileNameDisplayed = filePath;
                            if (PreviewPlainText)
                            {
                                result.FileInfo.TempFile = textFilePath;
                            }
                            result.FileNameReal = filePath;
                        }
                    }

                    return results;
                }
            }
            catch (OperationCanceledException)
            {
                // expected exception
            }
            catch (GenericPluginException ex)
            {
                logger.Error(ex.Message); // message is sufficient, no need for stack trace
                return
                [
                    new(filePath, searchPattern, ex.Message, false)
                ];
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to search inside the file: '{filePath}'");
                return
                [
                    new(filePath, searchPattern, ex.Message, false)
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

        private ExtractTextResults ExtractText(string sourceFilePath, string cacheFilePath, Encoding encoding,
            PauseCancelToken pauseCancelToken)
        {
            if (string.IsNullOrEmpty(cacheFilePath))
            {
                return new(string.Empty, encoding);
            }

            if (File.Exists(cacheFilePath))
            {
                // it is already extracted!
                return ReadCacheFile(cacheFilePath, encoding);
            }

            string textOut = string.Empty;
            string errorOut = string.Empty;
            Encoding fileEncoding = encoding;

            bool readStandardOutput = !Arguments.Contains("{outputFile}", StringComparison.OrdinalIgnoreCase);

            string arguments = Arguments.Replace("{inputFile}", sourceFilePath, StringComparison.OrdinalIgnoreCase)
                .Replace("{outputFile}", cacheFilePath, StringComparison.OrdinalIgnoreCase);

            using Process process = new();
            process.StartInfo.FileName = Application;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = readStandardOutput;
            process.StartInfo.RedirectStandardError = readStandardOutput;
            process.StartInfo.WorkingDirectory = WorkingDirectory;
            process.StartInfo.CreateNoWindow = true;
            process.ErrorDataReceived += (s, e) => { errorOut += e.Data; };

            pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

            // start cmd prompt, execute command
            process.Start();

            if (readStandardOutput)
            {
                using var ms = new MemoryStream();
                process.StandardOutput.BaseStream.CopyTo(ms);
                ms.Position = 0;

                if (encoding == Encoding.Default)
                    fileEncoding = Utils.GetFileEncoding(cacheFilePath);
                else
                    fileEncoding = encoding;

                using var streamReader = new StreamReader(ms, fileEncoding, detectEncodingFromByteOrderMarks: false);
                textOut = streamReader.ReadToEnd();

                if (!string.IsNullOrEmpty(textOut))
                {
                    File.WriteAllText(cacheFilePath, textOut, fileEncoding);
                }
            }

            process.WaitForExit();

            if (!readStandardOutput && File.Exists(cacheFilePath))
            {
                // GrepCore cannot check encoding of the original source file. If the encoding
                // parameter is not default then it is the user-specified code page.  If the
                // encoding parameter *is* the default, then it most likely not been set, so get
                // the encoding of the extracted text file:
                if (encoding == Encoding.Default)
                    fileEncoding = Utils.GetFileEncoding(cacheFilePath);
                else
                    fileEncoding = encoding;

                using StreamReader sr = new(cacheFilePath, fileEncoding, detectEncodingFromByteOrderMarks: false);
                textOut = sr.ReadToEnd();
            }

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(textOut))
            {
                return new ExtractTextResults(textOut, fileEncoding);
            }
            else
            {
                if (!string.IsNullOrEmpty(errorOut))
                {
                    string message = TranslationSource.Format(
                        Resources.Error_ThePluginCommandReturnedError, Name, errorOut);
                    throw new GenericPluginException(message);
                }

                logger.Error($"The {Name} plugin command returned exit code {process.ExitCode}");

                return new(string.Empty, fileEncoding);
            }
        }

        private static ExtractTextResults ReadCacheFile(string cacheFilePath, Encoding encoding)
        {
            Encoding fileEncoding = encoding;
            string textOut;
            // GrepCore cannot check encoding of the original file. If the encoding parameter is
            // not default then it is the user-specified code page.  If the encoding parameter *is*
            // the default, then it most likely not been set, so get the encoding of the extracted
            // text file:
            if (encoding == Encoding.Default)
                fileEncoding = Utils.GetFileEncoding(cacheFilePath);

            using (StreamReader streamReader = new(cacheFilePath, fileEncoding, detectEncodingFromByteOrderMarks: false))
                textOut = streamReader.ReadToEnd();

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
