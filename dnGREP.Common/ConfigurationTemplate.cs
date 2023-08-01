using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace dnGREP.Common
{
    public class ConfigurationTemplate
    {
        public static Dictionary<string, ConfigurationTemplate?> EditorConfigurationTemplates =>
            new()
            {
            {string.Empty, null},
            {"Atom", new ConfigurationTemplate("atom.exe", @"%file:%line:%column", "AppDataLocal") },
            {"GVim", new ConfigurationTemplate("gvim.exe", @"+/""%match"" +%line %file", "ProgramFilesx86") },
            {"Notepad++", new ConfigurationTemplate("notepad++.exe", @"-n%line -c%column %file", "ProgramFilesx86") },
            {"VSCode", new ConfigurationTemplate("code.exe", @"-r -g %file:%line:%column", "AppDataLocal", "ProgramFiles") },
        };

        public static Dictionary<string, ConfigurationTemplate?> CompareConfigurationTemplates =>
            new()
            {
            {string.Empty, null},
            {"Beyond Compare", new ConfigurationTemplate("BComp.exe", string.Empty, "ProgramFiles") },
            {"KDiff3", new ConfigurationTemplate("kdiff3.exe", string.Empty, "ProgramFiles") },
            {"Meld", new ConfigurationTemplate("meld.exe", string.Empty, "ProgramFilesx86") },
            {"P4Merge", new ConfigurationTemplate("p4merge.exe", string.Empty, "ProgramFiles") },
            {"WinMerge", new ConfigurationTemplate("WinMergeU.exe", @"/e /u /x", "ProgramFiles") },
            {"VSCode", new ConfigurationTemplate("code.exe", @"-d", "AppDataLocal", "ProgramFiles") },
            {"VsDiffMerge", new ConfigurationTemplate("vsDiffMerge.exe", string.Empty, "ProgramFiles", "ProgramFilesx86") },
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
            List<string> triedPaths = new();
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

                    var exePath = SafeDirectory.EnumerateFiles(path, new string[] { template.ExeFileName },
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

        public ConfigurationTemplate(string exeFileName, string arguments, params string[] hintPath)
        {
            ExeFileName = exeFileName;
            HintPath = hintPath;
            Arguments = arguments;
        }

        public string ExeFileName { get; private set; }

        public string[] HintPath { get; private set; }

        public string Arguments { get; private set; }
    }


}
