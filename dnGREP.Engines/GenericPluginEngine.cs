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

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType,
            GrepSearchOption searchOptions, Encoding encoding, PauseCancelToken pauseCancelToken)
        {
            string tempFile = string.Empty;
            string text = string.Empty;
            IGrepEngine? engine = null;
            try
            {
                // Extract text
                ExtractTextResults extracted = ExtractText(file, encoding, pauseCancelToken);
                text = extracted.Text;
                tempFile = extracted.TempFile;
                encoding = extracted.Encoding;

                if (string.IsNullOrEmpty(text))
                {
                    string message = TranslationSource.Format(Resources.Error_ThePluginFailedToExtractAnyText, Name);
                    logger.Error(message + $": '{file}'");
                    return
                    [
                        new(file, searchPattern, message, false)
                    ];
                }

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                if (!HasSearchableText(text))
                {
                    string message = TranslationSource.Format(
                        Resources.Error_TheFileCreatedByThePluginHasNoSearchableText, Name);

                    logger.Error(message + $": '{file}'");
                    return
                    [
                        new(file, searchPattern, message, false)
                    ];
                }

                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();

                engine = GrepEngineFactory.GetSearchEngine(tempFile, initParams, FileFilter, searchType);
                if (engine != null)
                {
                    using Stream inputStream = new MemoryStream(encoding.GetBytes(text));
                    List<GrepSearchResult> results = engine.Search(inputStream, tempFile, searchPattern,
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
            catch (GenericPluginException ex)
            {
                logger.Error(ex.Message); // message is sufficient, no need for stack trace
                return
                [
                    new(file, searchPattern, ex.Message, false)
                ];
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to search inside the file: '{file}'");
                return
                [
                    new(file, searchPattern, ex.Message, false)
                ];
            }
            finally
            {
                if (engine != null)
                {
                    GrepEngineFactory.ReturnToPool(tempFile, engine);
                }
            }
            return [];
        }

        // the stream version will get called if the file is in an archive
        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern,
            SearchType searchType, GrepSearchOption searchOptions, Encoding encoding,
            PauseCancelToken pauseCancelToken)
        {
            // write the stream to a temp folder, and run the file version of the search
            string tempFolder = Path.Combine(Utils.GetTempFolder(), $"dnGREP-{Name}");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            // ensure each temp file is unique, even if the file name exists elsewhere in the search tree
            string extractFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(fileName);
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

        private ExtractTextResults ExtractText(string sourceFilePath, Encoding encoding,
            PauseCancelToken pauseCancelToken)
        {
            string textOut = string.Empty;
            string errorOut = string.Empty;
            Encoding fileEncoding = encoding == Encoding.Default ? Encoding.UTF8 : encoding;
            string tempFolder = Path.Combine(Utils.GetTempFolder(), $"dnGREP-{Name}");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            // ensure each temp file is unique, even if the file name exists elsewhere in the search tree
            string fileName = Path.GetFileNameWithoutExtension(sourceFilePath) + "_" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".txt";
            string tempFileName = Path.Combine(tempFolder, fileName);

            bool readStandardOutput = !Arguments.Contains("{outputFile}", StringComparison.OrdinalIgnoreCase);

            string arguments = Arguments.Replace("{inputFile}", sourceFilePath, StringComparison.OrdinalIgnoreCase)
                .Replace("{outputFile}", tempFileName, StringComparison.OrdinalIgnoreCase);

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

                fileEncoding = Utils.GetFileEncoding(ms);

                using var streamReader = new StreamReader(ms, fileEncoding, detectEncodingFromByteOrderMarks: false);
                textOut = streamReader.ReadToEnd();

                if (!string.IsNullOrEmpty(textOut))
                {
                    File.WriteAllText(tempFileName, textOut, fileEncoding);
                }
            }

            process.WaitForExit();

            if (!readStandardOutput && File.Exists(tempFileName))
            {
                // GrepCore cannot check encoding of the original source file. If the encoding
                // parameter is not default then it is the user-specified code page.  If the
                // encoding parameter *is* the default, then it most likely not been set, so get
                // the encoding of the extracted text file:
                if (encoding == Encoding.Default)
                    fileEncoding = Utils.GetFileEncoding(tempFileName);
                else
                    fileEncoding = encoding;

                using StreamReader sr = new(tempFileName, fileEncoding, detectEncodingFromByteOrderMarks: false);
                textOut = sr.ReadToEnd();
            }

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(textOut))
            {
                return new ExtractTextResults(textOut, tempFileName, fileEncoding);
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

                return new(string.Empty, string.Empty, fileEncoding);
            }
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

        private record ExtractTextResults(string Text, string TempFile, Encoding Encoding) { }
    }
}
