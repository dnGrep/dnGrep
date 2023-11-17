using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using dnGREP.Common;
using dnGREP.Localization;
using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    public partial class CopyCommand
    {
        public CopyCommand(string commandLine)
        {
            string[] args = SplitCommandLine(commandLine);
            if (args.Length > 0)
            {
                EvaluateArgs(args);
            }
        }

        private static string[] SplitCommandLine(string commandLine)
        {
            List<string> result = new();
            foreach (string arg in CommandLineArgs.ParseLine(commandLine))
            {
                string s = arg.Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    result.Add(s);
                }
            }
            return result.ToArray();
        }

        private void EvaluateArgs(string[] args)
        {
            TextInfo ti = CultureInfo.InvariantCulture.TextInfo;

            for (int idx = 0; idx < args.Length; idx++)
            {
                string arg = args[idx];
                string value = string.Empty;
                if (idx + 1 < args.Length && !string.IsNullOrEmpty(args[idx + 1]))
                {
                    value = CommandLineArgs.StripQuotes(args[idx + 1]);
                }

                if (!string.IsNullOrEmpty(arg))
                {
                    switch (arg.ToLowerInvariant())
                    {
                        case "-match":
                        case "/match":
                            if (!string.IsNullOrWhiteSpace(value) && IsNotAnArgument(value))
                            {
                                var (success, message) = IsValidRegex(value, arg);
                                if (success)
                                {
                                    MatchPattern = value;
                                    idx++;
                                }
                                else
                                {
                                    Errors.Add(message);
                                }
                            }
                            else
                            {
                                Errors.Add(TranslationSource.Format(Resources.CopyCommand_ErrorPatternIsRequiredForArgument0, arg));
                            }
                            break;

                        case "-rename":
                        case "/rename":
                            if (!string.IsNullOrWhiteSpace(value) && IsNotAnArgument(value))
                            {
                                var (success, message) = IsValidRegex(value, arg);
                                if (success)
                                {
                                    RenamePattern = value;
                                    idx++;
                                }
                                else
                                {
                                    Errors.Add(message);
                                }
                            }
                            else
                            {
                                Errors.Add(TranslationSource.Format(Resources.CopyCommand_ErrorPatternIsRequiredForArgument0, arg));
                            }
                            break;

                        case "-overwrite":
                        case "/overwrite":
                            if (!string.IsNullOrWhiteSpace(value) && IsNotAnArgument(value) &&
                                bool.TryParse(ti.ToTitleCase(value), out bool overwrite))
                            {
                                Overwrite = overwrite;
                                idx++;
                            }
                            else
                            {
                                Errors.Add(TranslationSource.Format(Resources.CopyCommand_ErrorTrueOrFalseIsRequiredForArgument0, arg));
                            }
                            break;


                        case "-out":
                        case "/out":
                            if (!string.IsNullOrWhiteSpace(value) && IsNotAnArgument(value))
                            {
                                Destination = value;
                            }
                            else
                            {
                                Errors.Add(TranslationSource.Format(Resources.CopyCommand_ErrorPathIsRequiredForArgument0, arg));
                            }
                            break;
                    }
                }
            }
        }

        private static readonly List<string> arguments = new()
        {
            "-match", "/match",
            "-rename", "/rename",
            "-overwrite", "/overwrite",
            "-out", "/out",
        };

        private bool IsNotAnArgument(string value)
        {
            return !arguments.Contains(value);
        }

        private static (bool success, string message) IsValidRegex(string regex, string arg)
        {
            try
            {
                Regex pattern = new(regex);
            }
            catch (ArgumentException ex)
            {
                return (false, TranslationSource.Format(Resources.CopyCommand_Error0ForArgument1, ex.Message, arg));
            }
            return (true, string.Empty);
        }

        public List<string> Errors { get; } = new();
        public List<string> Messages { get; } = new();
        public string? MatchPattern { get; private set; }
        public string? RenamePattern { get; private set; }
        public bool Overwrite { get; private set; } = false;
        public string? Destination { get; private set; }

        internal bool Execute(List<GrepSearchResult> source)
        {
            Errors.Clear();
            Messages.Clear();
            HashSet<string> files = new();

            foreach (GrepSearchResult result in source)
            {
                if (!files.Contains(result.FileNameReal))
                {
                    files.Add(result.FileNameReal);
                    try
                    {

                        string? sourceDirectory = Path.GetDirectoryName(result.FileNameReal);
                        if (string.IsNullOrWhiteSpace(sourceDirectory))
                        {
                            Errors.Add("Source directory is null or empty");
                            continue;
                        }
                        string? destinationDirectory = string.IsNullOrWhiteSpace(Destination) ?
                            sourceDirectory : Path.IsPathRooted(Destination) ? Destination : NormalizePath(Path.Combine(sourceDirectory, Destination));
                        if (string.IsNullOrWhiteSpace(destinationDirectory))
                        {
                            Errors.Add("Destination directory is null or empty");
                            continue;
                        }

                        if (!Directory.Exists(destinationDirectory))
                        {
                            try
                            {
                                Directory.CreateDirectory(destinationDirectory);
                            }
                            catch
                            {
                                Errors.Add(TranslationSource.Format(Resources.CopyCommand_CouldNotCreateDestinationDirectory0, destinationDirectory));
                                continue;
                            }
                        }

                        string sourceFileName = Path.GetFileName(result.FileNameReal);
                        string destinationFileName = string.Empty;

                        if (!string.IsNullOrEmpty(MatchPattern) && !string.IsNullOrEmpty(RenamePattern))
                        {
                            destinationFileName = Regex.Replace(sourceFileName, MatchPattern, (match) =>
                            {
                                return match.Result(RenamePattern);
                            },
                            RegexOptions.IgnoreCase, GrepCore.MatchTimeout);

                            if (ReferenceEquals(sourceFileName, destinationFileName))
                            {
                                Errors.Add(TranslationSource.Format(Resources.CopyCommand_TheMatchPatternFailedToMatchFilename0, sourceFileName));
                                continue;
                            }
                        }
                        else
                        {
                            destinationFileName = sourceFileName;
                        }

                        FileInfo sourceFileInfo = new(result.FileNameReal);
                        FileInfo destinationFileInfo = new(Path.Combine(destinationDirectory, destinationFileName));

                        if (sourceFileInfo.FullName != destinationFileInfo.FullName)
                        {
                            if (destinationFileInfo.Exists && !Overwrite)
                            {
                                Errors.Add(TranslationSource.Format(Resources.CopyCommand_DestinationFileExistsAndOverwriteIsFalse0, destinationFileInfo.FullName));
                                continue;
                            }

                            Utils.CopyFile(sourceFileInfo.FullName, destinationFileInfo.FullName, true);
                            Messages.Add(TranslationSource.Format(Resources.CopyCommand_Copied0To1, sourceFileInfo.FullName, destinationFileInfo.FullName));
                        }
                        else
                        {
                            Errors.Add(TranslationSource.Format(Resources.CopyCommand_DestinationFileIsTheSameAsTheSourceFile0, destinationFileInfo.FullName));
                        }
                    }
                    catch (Exception ex)
                    {
                        Errors.Add(TranslationSource.Format(Resources.CopyCommand_Error0ProcessingFile1, ex.Message, result.FileNameReal));
                    }
                }
            }
            return Errors.Count == 0;
        }

        internal static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
