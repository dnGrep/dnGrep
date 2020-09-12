using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class CommandLineArgs
    {
        public CommandLineArgs(string commandLine)
        {
            // Getting the arguments from Environment.GetCommandLineArgs() or StartupEventArgs 
            // does strange things with quoted strings, so parse them here:
            string[] args = SplitCommandLine(commandLine);
            Count = args.Length;
            if (args.Length > 0)
            {
                EvaluateArgs(args);
            }
        }

        public int Count { get; private set; }

        private readonly static Regex cmdRegex = new Regex("(?:^| )(\"(?:[^\"])*\"|[^ ]*)",
            RegexOptions.Compiled);

        private static string[] SplitCommandLine(string line)
        {
            List<string> result = new List<string>();
            MatchCollection matches = cmdRegex.Matches(line);
            foreach (Match m in matches)
            {
                string s = m.Value.Trim();
                if (s.Length > 2 && s.StartsWith("\"", StringComparison.InvariantCulture) && s.EndsWith("\"", StringComparison.InvariantCulture))
                {
                    s = s.Substring(1, s.Length - 2);
                }
                result.Add(s);
            }
            return result.Skip(1).ToArray(); // Drop the program path, and return array of all strings
        }

        private void EvaluateArgs(string[] args)
        {
            TextInfo ti = CultureInfo.InvariantCulture.TextInfo;

            for (int idx = 0; idx < args.Length; idx++)
            {
                string arg = args[idx];

                if (!string.IsNullOrWhiteSpace(arg))
                {
                    // old style command line args
                    if (idx < 2 && !(arg.StartsWith("/", StringComparison.InvariantCulture) ||
                          arg.StartsWith("-", StringComparison.InvariantCulture)))
                    {
                        if (idx == 0)
                        {
                            SearchPath = arg;
                        }
                        else if (idx == 1)
                        {
                            SearchFor = arg;
                            TypeOfSearch = SearchType.Regex;
                            ExecuteSearch = true;
                        }
                    }
                    else
                    {
                        string value = string.Empty;
                        if (idx + 1 < args.Length && !string.IsNullOrWhiteSpace(args[idx + 1]))
                        {
                            value = args[idx + 1];
                        }

                        switch (arg.ToLowerInvariant())
                        {
                            case "/warmup":
                                WarmUp = true;
                                break;

                            case "/f":
                            case "-f":
                            case "-folder":
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    SearchPath = Utils.QuoteIfNeeded(value);
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }

                                break;

                            case "/pm":
                            case "-pm":
                            case "-pathtomatch":
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    NamePatternToInclude = value;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }

                                break;

                            case "/pi":
                            case "-pi":
                            case "-pathtoignore":
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    NamePatternToExclude = value;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }

                                break;

                            case "/pt":
                            case "-pt":
                            case "-patterntype":
                                if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, out FileSearchType tofs))
                                {
                                    TypeOfFileSearch = tofs;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }
                                break;


                            case "/s":
                            case "-s":
                            case "-searchfor":
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    SearchFor = value;
                                    ExecuteSearch = true;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }

                                break;

                            case "/st":
                            case "-st":
                            case "-searchtype":
                                if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, out SearchType tos))
                                {
                                    TypeOfSearch = tos;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }
                                break;

                            case "/cs":
                            case "-cs":
                            case "-casesensitive":
                                if (!string.IsNullOrWhiteSpace(value) && bool.TryParse(ti.ToTitleCase(value), out bool caseSensitive))
                                {
                                    CaseSensitive = caseSensitive;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }
                                break;

                            case "/ww":
                            case "-ww":
                            case "-wholeword":
                                if (!string.IsNullOrWhiteSpace(value) && bool.TryParse(ti.ToTitleCase(value), out bool wholeWord))
                                {
                                    WholeWord = wholeWord;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }
                                break;

                            case "/ml":
                            case "-ml":
                            case "-multiline":
                                if (!string.IsNullOrWhiteSpace(value) && bool.TryParse(ti.ToTitleCase(value), out bool multiline))
                                {
                                    Multiline = multiline;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }
                                break;

                            case "/dn":
                            case "-dn":
                            case "-dotasnewline":
                                if (!string.IsNullOrWhiteSpace(value) && bool.TryParse(ti.ToTitleCase(value), out bool dotAsNewline))
                                {
                                    DotAsNewline = dotAsNewline;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }
                                break;

                            case "/bo":
                            case "-bo":
                            case "-booleanoperators":
                                if (!string.IsNullOrWhiteSpace(value) && bool.TryParse(ti.ToTitleCase(value), out bool booleanOperators))
                                {
                                    BooleanOperators = booleanOperators;
                                    idx++;
                                }
                                else
                                {
                                    InvalidArgument = true;
                                    ShowHelp = true;
                                }
                                break;

                            case "/h":
                            case "-h":
                            case "-help":
                            case "/v":
                            case "-v":
                            case "-version":
                                ShowHelp = true;
                                break;

                            default:
                                InvalidArgument = true;
                                ShowHelp = true;
                                break;
                        }
                    }
                }
                if (ShowHelp || WarmUp)
                    break;
            }
        }

        public bool InvalidArgument { get; private set; }
        public bool WarmUp { get; private set; }
        public bool ShowHelp { get; private set; }
        public string SearchFor { get; private set; }
        public string SearchPath { get; private set; }
        public SearchType? TypeOfSearch { get; private set; }
        public string NamePatternToInclude { get; private set; }
        public string NamePatternToExclude { get; private set; }
        public FileSearchType? TypeOfFileSearch { get; private set; }
        public bool? CaseSensitive { get; private set; }
        public bool? WholeWord { get; private set; }
        public bool? Multiline { get; private set; }
        public bool? DotAsNewline { get; private set; }
        public bool? BooleanOperators { get; private set; }
        public bool ExecuteSearch { get; private set; }

        public void ApplyArgs()
        {
            if (!string.IsNullOrWhiteSpace(SearchPath))
            {
                GrepSettings.Instance.Set<string>(GrepSettings.Key.SearchFolder, SearchPath);
            }

            if (!string.IsNullOrWhiteSpace(SearchFor))
            {
                GrepSettings.Instance.Set<string>(GrepSettings.Key.SearchFor, SearchFor);
            }

            if (!string.IsNullOrWhiteSpace(NamePatternToInclude))
            {
                GrepSettings.Instance.Set<string>(GrepSettings.Key.FilePattern, NamePatternToInclude);
            }

            if (!string.IsNullOrWhiteSpace(NamePatternToExclude))
            {
                GrepSettings.Instance.Set<string>(GrepSettings.Key.FilePatternIgnore, NamePatternToExclude);
            }

            if (TypeOfSearch.HasValue)
            {
                GrepSettings.Instance.Set<SearchType>(GrepSettings.Key.TypeOfSearch, TypeOfSearch.Value);
            }

            if (TypeOfFileSearch.HasValue)
            {
                GrepSettings.Instance.Set<FileSearchType>(GrepSettings.Key.TypeOfFileSearch, TypeOfFileSearch.Value);
            }

            if (CaseSensitive.HasValue)
            {
                GrepSettings.Instance.Set<bool>(GrepSettings.Key.CaseSensitive, CaseSensitive.Value);
            }

            if (WholeWord.HasValue)
            {
                GrepSettings.Instance.Set<bool>(GrepSettings.Key.WholeWord, WholeWord.Value);
            }

            if (Multiline.HasValue)
            {
                GrepSettings.Instance.Set<bool>(GrepSettings.Key.Multiline, Multiline.Value);
            }

            if (DotAsNewline.HasValue)
            {
                GrepSettings.Instance.Set<bool>(GrepSettings.Key.Singleline, DotAsNewline.Value);
            }

            if (BooleanOperators.HasValue)
            {
                GrepSettings.Instance.Set<bool>(GrepSettings.Key.BooleanOperators, BooleanOperators.Value);
            }
        }

        public string GetHelpString()
        {
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string buildDate = AboutViewModel.GetLinkerTime(Assembly.GetExecutingAssembly()).ToString(CultureInfo.CurrentUICulture);

            return string.Join(Environment.NewLine, new string[] {
                    "dnGrep:  Search through text files, Word and Excel documents, PDFs, and archives using text, regular expression, XPath, and phonetic queries.",
                    string.Empty,
                    $"Version {assemblyVersion} built on {buildDate}",
                    string.Empty,
                    "Usage:",
                    string.Empty,
                    "dnGrep [arguments]",
                    "Using any combination of arguments from the list below.",
                    "Example:",
                    "dnGrep /f \"C:\\Program Files\\dnGrep\" /pm *.txt;*.xml /s t\\w*t /st Regex",
                    string.Empty,
                    "dnGrep [folderPath] [searchFor]",
                    "One or two arguments without flags: the first argument is the folder to search, the second the regular expression to search for.",
                    "Example:",
                    "dnGrep \"C:\\Program Files\\dnGrep\" t\\w*t",
                    string.Empty,
                    "Any arguments not specified will default to the last saved user settings.",
                    string.Empty,
                    "Arguments:",
                    string.Empty,
                    "/f [folderPath]",
                    "-f [folderPath]",
                    "-folder [folderPath]",
                    "    Sets the folder path to search.  May be a comma or semi-colon separated list of folder paths.",
                    string.Empty,
                    "/pm [pattern]",
                    "-pm [pattern]",
                    "-pathToMatch [pattern]",
                    "    Sets the file name pattern to match files in the search path. May be a comma or semi-colon separated list of patterns.",
                    string.Empty,
                    "/pi [pattern]",
                    "-pi [pattern]",
                    "-pathToIgnore [pattern]",
                    "    Sets the file or directory name pattern to exclude files or folders in the search path. May be a comma or semi-colon separated list of patterns.",
                    string.Empty,
                    "/pt [type]",
                    "-pt [type]",
                    "-patternType [type]",
                    "    Sets the type of match and ignore pattern - must be one of: Asterisk, Regex, or Everything (if the Everything search tool is installed).",
                    string.Empty,
                    "/s [searchFor]",
                    "-s [searchFor]",
                    "-searchFor [searchFor]",
                    "    Sets the pattern or text to search for within the set of files.  When this argument is set, the search will be run automatically.",
                    string.Empty,
                    "/st [type]",
                    "-st [type]",
                    "-searchType [type]",
                    "    Sets the type of search - must be one of: PlainText, Regex, XPath, or Soundex.",
                    string.Empty,
                    "/cs [True/False]",
                    "-cs [True/False]",
                    "-caseSensitive [True/False]",
                    "    Sets case sensitive search flag - True or False.",
                    string.Empty,
                    "/ww [True/False]",
                    "-ww [True/False]",
                    "-wholeWord [True/False]",
                    "    Sets the whole word search flag - True or False.",
                    string.Empty,
                    "/ml [True/False]",
                    "-ml [True/False]",
                    "-multiline [True/False]",
                    "    Sets the multiline search flag - True or False.",
                    string.Empty,
                    "/dn [True/False]",
                    "-dn [True/False]",
                    "-dotAsNewline [True/False]",
                    "    Sets the dot as newline search flag - True or False.",
                    string.Empty,
                    "/bo [True/False]",
                    "-bo [True/False]",
                    "-booleanOperators [True/False]",
                    "    Sets the Boolean operators search flag - True or False.",
                    });
        }
    }
}
