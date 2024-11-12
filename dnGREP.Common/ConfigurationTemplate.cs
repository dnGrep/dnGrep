using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace dnGREP.Common
{
    public class ConfigurationTemplate(string  label, string exeFileName, string arguments, params string[] hintPath)
    {
        public static Dictionary<string, ConfigurationTemplate?> EditorConfigurationTemplates => new()
        {
            {string.Empty, null},
            {"Acrobat", new ConfigurationTemplate("Acrobat", "acrobat.exe", @"/A ""page=%page"" %file", "ProgramFiles") },
            {"Atom", new ConfigurationTemplate("Atom", "atom.exe", @"%file:%line:%column", "AppDataLocal") },
            {"GVim", new ConfigurationTemplate("GVim", "gvim.exe", @"+/""%match"" +%line %file", "ProgramFilesx86") },
            {"Notepad++", new ConfigurationTemplate("Notepad++", "notepad++.exe", @"-n%line -c%column %file", "ProgramFilesx86") },
            {"Notepad3", new ConfigurationTemplate("Notepad3", "notepad3.exe", @"/m ""%match"" %file", "ProgramFilesx86") },
            {"Sublime Text", new ConfigurationTemplate("Sublime Text", "subl.exe", @"%file:%line:%column", "ProgramFiles") },
            {"Sumatra PDF", new ConfigurationTemplate("Sumatra PDF", "sumatrapdf.exe", @"-reuse-instance -page %page %file", "AppDataLocal", "ProgramFiles") },
            {"TextPad", new ConfigurationTemplate("TextPad", "TextPad.exe", @"%file(%line,%column)", "ProgramFiles", "AppDataLocal") },
            {"VSCode", new ConfigurationTemplate("VSCode", "code.exe", @"-r -g %file:%line:%column", "AppDataLocal", "ProgramFiles") },
        };

        public static Dictionary<string, ConfigurationTemplate?> CompareConfigurationTemplates => new()
        {
            {string.Empty, null},
            {"Beyond Compare", new ConfigurationTemplate("Beyond Compare", "BComp.exe", string.Empty, "ProgramFiles") },
            {"KDiff3", new ConfigurationTemplate("KDiff3", "kdiff3.exe", string.Empty, "ProgramFiles") },
            {"Meld", new ConfigurationTemplate("Meld", "meld.exe", string.Empty, "ProgramFilesx86") },
            {"P4Merge", new ConfigurationTemplate("P4Merge", "p4merge.exe", string.Empty, "ProgramFiles") },
            {"WinMerge", new ConfigurationTemplate("WinMerge", "WinMergeU.exe", @"/e /u /x", "ProgramFiles") },
            {"VSCode", new ConfigurationTemplate("VSCode", "code.exe", @"-d", "AppDataLocal", "ProgramFiles") },
            {"VsDiffMerge", new ConfigurationTemplate("VsDiffMerge", "vsDiffMerge.exe", string.Empty, "ProgramFiles", "ProgramFilesx86") },
        };

        private static readonly Dictionary<string, string> SearchPaths = new()
        {
            { "ProgramFiles", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) },
            { "ProgramFilesx86", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) },
            { "AppDataLocal", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) },
        };

        static ConfigurationTemplate()
        {
            var root = Directory.GetDirectoryRoot(Assembly.GetEntryAssembly()?.Location ?? string.Empty);
            if (!string.IsNullOrEmpty(root) && !root.Equals("C:\\", StringComparison.OrdinalIgnoreCase))
            {
                SearchPaths.Add("Portable", root);
            }
        }


        private static IEnumerable<string> GetSearchPaths(string[] hintPaths)
        {
            List<string> triedPaths = [];
            foreach (string hint in hintPaths)
            {
                if (SearchPaths.TryGetValue(hint, out string? path))
                {
                    triedPaths.Add(path);
                    yield return path;
                }
            }

            foreach (string searchPath in SearchPaths.Values.Except(triedPaths))
            {
                yield return searchPath;
            }
        }

        public static string FindExePath(ConfigurationTemplate template)
        {
            if (template != null)
            {
                foreach (string path in GetSearchPaths(template.HintPath))
                {
                    FileFilter fileParams = new(path, template.ExeFileName, string.Empty, false, false, false, true, -1, true, true, false, false, 0, 0, FileDateFilter.None, null, null, true);

                    var exePath = SafeDirectory.EnumerateFiles(path, [template.ExeFileName],
                        null, null, fileParams, default).FirstOrDefault();

                    if (!string.IsNullOrEmpty(exePath))
                    {
                        return exePath;
                    }
                }
                return template.ExeFileName;
            }
            return string.Empty;
        }

        public string Label { get; private set; } = label;

        public string ExeFileName { get; private set; } = exeFileName;

        public string[] HintPath { get; private set; } = hintPath;

        public string Arguments { get; private set; } = arguments;
    }


}
