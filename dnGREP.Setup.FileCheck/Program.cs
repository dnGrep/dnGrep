using System.Xml.Linq;

namespace dnGREP.Setup.FileCheck
{
    internal class Program
    {
        /// <summary>
        /// This tool does two things:
        /// 1) Compares the components in the *.wxs fragment files to the files in the publish directory,
        ///    and lists the differences in the console window.
        /// 2) Builds exclude file lists for the input to 7zip when creating the portable packages.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string solutionDir = args[0];

                string[] platforms = new[] { "win-x64", "win-x86" };

                foreach (string platform in platforms)
                {
                    bool is32bit = platform.EndsWith("x86");
                    string publishDir = Path.Combine(solutionDir, "publish", platform);
                    string fragmentsDir = Path.Combine(solutionDir, "dnGREP.Setup", "Fragments");

                    List<string> allFiles = GetAllFiles(publishDir);
                    List<string> includeFiles = GetIncludeFiles(publishDir);
                    List<string> components = ReadFragmentFiles(fragmentsDir, publishDir, is32bit);

                    var list1 = components.Except(includeFiles).ToList();
                    var list2 = includeFiles.Except(components).ToList();

                    if (list1.Count > 0)
                    {
                        Console.WriteLine("{0}: Installer files not found in publish directory:", platform);
                        foreach (var file in list1)
                        {
                            Console.WriteLine(file);
                        }
                    }
                    if (list2.Count > 0)
                    {
                        Console.WriteLine("{0}: Published files missing from installer:", platform);
                        foreach (var file in list2)
                        {
                            var fileName = file.Replace(publishDir, string.Empty).TrimStart('\\');
                            var item = string.Format(wixFormat,
                                "cmp" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
                                "{" + Guid.NewGuid().ToString("D").ToUpperInvariant() + "}",
                                "fil" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
                                fileName);
                            Console.WriteLine(item);
                        }
                    }
                    if (list2.Count == 0 && list1.Count == 0)
                    {
                        Console.WriteLine("{0}: Published files and components match exactly", platform);
                    }
                    Console.WriteLine();

                    
                    string outFile = Path.Combine(solutionDir, "excludeFiles_" + platform + ".txt");
                    using FileStream stream = File.Open(outFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    using StreamWriter writer = new(stream);
                    var list3 = allFiles.Except(includeFiles);
                    foreach (var file in list3)
                    {
                        var fileName = file.Replace(publishDir, string.Empty).TrimStart('\\');

                        writer.WriteLine(fileName);
                    }
                }
            }
        }

        private static readonly List<string> includeExtensions = new() { ".exe", ".dll", ".pdb", ".json", ".config", ".plugin", ".txt" };
        private static readonly Dictionary<string, string> x86Map = new()
        {
            {"$(var.App.PlatformShort)",  "32" },
            {"$(var.Platform.Id)", "x86" },
        };
        private static readonly Dictionary<string, string> x64Map = new()
        {
            {"$(var.App.PlatformShort)",  "64" },
            {"$(var.Platform.Id)", "amd64" },
        };
        private static readonly string wixFormat =
            @"<Component Id=""{0}"" Guid=""{1}"">
        <File Id=""{2}"" KeyPath=""yes"" Source=""$(var.PublishDir)\{3}"" />
      </Component>";

        private static List<string> GetAllFiles(string publishDir)
        {
            List<string> files = new();

            var allFiles = Directory.GetFiles(publishDir, "*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                files.Add(file);
            }

            return files;
        }

        private static List<string> GetIncludeFiles(string publishDir)
        {
            List<string> files = new();

            var root = Directory.GetFiles(publishDir, "*", SearchOption.TopDirectoryOnly);
            foreach (var file in root)
            {
                var ext = Path.GetExtension(file);
                if (includeExtensions.Contains(ext))
                {
                    files.Add(file);
                }
            }

            var resources = Directory.GetFiles(publishDir, "dnGREP.Localization.resources.dll", SearchOption.AllDirectories);
            foreach (var file in resources)
            {
                files.Add(file);
            }

            var plugins = Directory.GetFiles(Path.Combine(publishDir, "Plugins"), "*", SearchOption.AllDirectories);
            foreach (var file in plugins)
            {
                var ext = Path.GetExtension(file);
                if (includeExtensions.Contains(ext) || string.IsNullOrEmpty(ext))
                {
                    files.Add(file);
                }
            }

            var runtimes = Directory.GetFiles(Path.Combine(publishDir, "runtimes"), "*", SearchOption.AllDirectories);
            foreach (var file in runtimes)
            {
                var ext = Path.GetExtension(file);
                if (includeExtensions.Contains(ext))
                {
                    files.Add(file);
                }
            }

            var themes = Directory.GetFiles(Path.Combine(publishDir, "Themes"), "*", SearchOption.AllDirectories);
            foreach (var file in themes)
            {
                files.Add(file);
            }

            return files;
        }

        private static List<string> ReadFragmentFiles(string fragmentsDir, string publishDir, bool is32bit)
        {
            XNamespace wi = "http://schemas.microsoft.com/wix/2006/wi";
            var map = is32bit ? x86Map : x64Map;
            List<string> components = new();
            var root = Directory.GetFiles(fragmentsDir, "*.wxs", SearchOption.TopDirectoryOnly);
            foreach (var file in root)
            {
                using FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                XDocument doc = XDocument.Load(stream);
                if (doc != null && doc.Root != null)
                {
                    foreach (XElement fileElem in doc.Root.Descendants(wi + "File"))
                    {
                        XAttribute? source = fileElem.Attribute("Source");
                        if (source != null)
                        {
                            var sourceFile = source.Value.Replace("$(var.PublishDir)", publishDir);
                            foreach (string key in map.Keys)
                            {
                                if (sourceFile.Contains(key))
                                {
                                    sourceFile = sourceFile.Replace(key, map[key]);
                                }
                            }
                            components.Add(sourceFile);
                        }
                    }
                }
            }

            return components;
        }
    }
}