using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using dnGREP.Common;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using NLog;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.WPF
{
    public class ThemedHighlightingManager : IHighlightingDefinitionReferenceResolver
    {
        public static ThemedHighlightingManager Instance { get; } = new ThemedHighlightingManager();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private enum Mode { Normal, Inverted, Runtime }
        private Mode mode;

        private readonly object lockObj = new object();
        private readonly Dictionary<string, IHighlightingDefinition> normalHighlightingsByName = new Dictionary<string, IHighlightingDefinition>();
        private readonly Dictionary<string, IHighlightingDefinition> normalHighlightingsByExtension = new Dictionary<string, IHighlightingDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IHighlightingDefinition> invertedHighlightingsByName = new Dictionary<string, IHighlightingDefinition>();
        private readonly Dictionary<string, IHighlightingDefinition> invertedHighlightingsByExtension = new Dictionary<string, IHighlightingDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly List<SyntaxDefinition> syntaxDefinitions = new List<SyntaxDefinition>()
        {
            new SyntaxDefinition("XmlDoc", "XmlDoc.xshd", 0),
            new SyntaxDefinition("C#", "CSharp-Mode.xshd", 1, ".cs"),
            new SyntaxDefinition("JavaScript", "JavaScript-Mode.xshd", 2,
                ".js;.cjs;.mjs;.ls;.sjs;.eg".Split(';')),//.ts;.tsx;.es;.es6;.coffee;.litcoffee;.iced;.liticed;
            new SyntaxDefinition("HTML", "HTML-Mode.xshd", 3,
                ".htm;.html;.cshtml;.vbhtml".Split(';')),

            new SyntaxDefinition("ASP/XHTML", "ASPX.xshd", 99,
                ".asp;.aspx;.asax;.asmx;.ascx;.master".Split(';')),
            new SyntaxDefinition("Boo", "Boo.xshd", 99, ".boo"),
            new SyntaxDefinition("Coco", "Coco-Mode.xshd", 99, ".atg"),
            new SyntaxDefinition("CSS", "CSS-Mode.xshd", 99, ".css"),
            new SyntaxDefinition("C++", "CPP-Mode.xshd", 99,
                ".c;.h;.cc;.cpp;.hpp".Split(';')),
            new SyntaxDefinition("Java", "Java-Mode.xshd", 99,
                ".java;.scala".Split(';')),
            new SyntaxDefinition("Patch", "Patch-Mode.xshd", 99,
                ".patch;.diff".Split(';')),
            new SyntaxDefinition("PowerShell", "PowerShell.xshd", 99,
                ".ps1;.psm1;.psd1".Split(';')),
            new SyntaxDefinition("PHP", "PHP-Mode.xshd", 99,
                ".php;.php3;.php4;.php5;.phtml".Split(';')),
            new SyntaxDefinition("Python", "Python-Mode.xshd", 99,
                ".py;.pyw".Split(';')),
            new SyntaxDefinition("SQL", "Sql-Mode.xshd", 10, // default .sql association 
                ".sql;.pks;.pkb;.pck".Split(';')),
            new SyntaxDefinition("TeX", "Tex-Mode.xshd", 99, ".tex"),
            new SyntaxDefinition("TSQL", "TSQL-Mode.xshd", 99, ".sql"),
            new SyntaxDefinition("VBNET", "VB-Mode.xshd", 99, ".vb"),
            new SyntaxDefinition("XML", "XML-Mode.xshd", 99,
                (".xml;.xsl;.xslt;.xsd;.manifest;.config;.addin;" +
                ".xshd;.wxs;.wxi;.wxl;.proj;.csproj;.vbproj;.ilproj;" +
                ".booproj;.build;.xfrm;.targets;.xaml;.xpt;" +
                ".xft;.map;.wsdl;.disco;.ps1xml;.nuspec;" +
                ".syn;.lang;.resx;.user").Split(';')),
            new SyntaxDefinition("MarkDown", "MarkDown-Mode.xshd", 99,
                ".md;.markdown;.mdown;.mkdn;.mkd;.mdwn;.mdtxt;.mdtext;.rmd".Split(';')),
            new SyntaxDefinition("Lisp", "Lisp-Mode.xshd", 10, // default .cl association
                ".lisp;.lsp;.cl;.mnl;.dcl".Split(';')),
            new SyntaxDefinition("Json", "Json.xshd", 99, ".json"),
            new SyntaxDefinition("Yaml", "Yaml.xshd", 99,
                ".yml;.yaml".Split(';')),
            new SyntaxDefinition("Script", "Script-Mode.xshd", 99, ".gsc"),

            new SyntaxDefinition("ActionScript", "ActionScript.xshd", 99,
                ".as;.mx".Split(';')),
            new SyntaxDefinition("Ada", "Ada.xshd", 99,
                ".ada;.ads;.adb".Split(';')),
            new SyntaxDefinition("ANTLR", "ANTLR.xshd", 99,
                ".g;.g3;.g4".Split(';')),
            new SyntaxDefinition("Assembly", "Assembly.xshd", 99, ".asm"),
            new SyntaxDefinition("AutoHotkey", "AutoHotkey.xshd", 99, ".ahk"),
            new SyntaxDefinition("Batch", "Batch.xshd", 99,
                ".bat;.cmd".Split(';')),
            //new HighlightingResource("C#", "C#.xshd", 99, ".cs"),
            //new HighlightingResource("C", "C.xshd", 99, ".c"),
            //new HighlightingResource("C++", "C#.xshd", 99,
            //     ".c;.h;.cc;.C;.cpp;.cxx;.hpp".Split(';')),
            new SyntaxDefinition("Ceylon", "Ceylon.xshd", 99, ".ceylon"),
            new SyntaxDefinition("ChucK", "ChucK.xshd", 99, ".ck"),
            new SyntaxDefinition("Clojure", "Clojure.xshd", 99, ".clj"),
            //new HighlightingResource("Cocoa", "Cocoa.xshd", 99, ".atg"),
            new SyntaxDefinition("CoffeeScript", "CoffeeScript.xshd", 99,
                ".coffee;.litcoffee;.iced;.liticed".Split(';')),
            new SyntaxDefinition("Cool", "Cool.xshd", 99, ".cl"),
            //new HighlightingResource("CSS", "CSS.xshd", 99, ".css"),
            new SyntaxDefinition("D", "D.xshd", 99, ".d"),
            new SyntaxDefinition("Dart", "Dart.xshd", 99, ".dart"),
            new SyntaxDefinition("Delphi", "Delphi.xshd", 99, ".dpr"),
            new SyntaxDefinition("Eiffel", "Eiffel.xshd", 99, ".e"),
            new SyntaxDefinition("Elixir", "Elixir.xshd", 99,
                ".es;.exs".Split(';')),
            new SyntaxDefinition("Erlang", "Erlang.xshd", 99,
                ".erl;.hrl".Split(';')),
            new SyntaxDefinition("F#", "F#.xshd", 99,
                ".fs;.fsi;.fsx;.fsscript".Split(';')),
            new SyntaxDefinition("Falcon", "Falcon.xshd", 99, ".fal"),
            new SyntaxDefinition("Fantom", "Fantom.xshd", 99, ".fan"),
            new SyntaxDefinition("Fortran 95", "Fortran95.xshd", 99,
                ".f90;.f95;.f03".Split(';')),
            new SyntaxDefinition("Go", "Go.xshd", 99, ".go"),
            new SyntaxDefinition("Groovy", "Groovy.xshd", 99, ".groovy"),
            new SyntaxDefinition("Gui4Cli", "Gui4Cli.xshd", 99, ".gui;.gc".Split(';')),
            new SyntaxDefinition("Haskell", "Haskell.xshd", 99, ".hs;.lhs".Split(';')),
            new SyntaxDefinition("Haxe", "Haxe.xshd", 99, ".hx"),
            //new HighlightingResource("HTML", "HTML.xshd", 99,
            //    ".html;.htm;.xhtml;.shtml;.shtm;.xht;.hta".Split(';')),
            new SyntaxDefinition("Icon", "Icon.xshd", 99, ".icn"),
            new SyntaxDefinition("ILYC", "ILYC.xshd", 99, ".ilc"),
            new SyntaxDefinition("INI", "INI.xshd", 99,
                ".ini;.inf;.wer;.dof".Split(';')),
            new SyntaxDefinition("Io", "Io.xshd", 99, ".io"),
            //new HighlightingResource("Java", "Java.xshd", 99, ".java"),
            //new HighlightingResource("JavaScript", "JavaScript.xshd", 0, 2, ".js"),
            new SyntaxDefinition("Julia", "Julia.xshd", 99, ".jl"),
            new SyntaxDefinition("Just BASIC", "Just BASIC.xshd", 99, ".bas"),
            new SyntaxDefinition("KiXtart", "KiXtart.xshd", 99, ".kix"),
            new SyntaxDefinition("Kotlin", "Kotlin.xshd", 99, ".kt;.kts".Split(';')),
            new SyntaxDefinition("Lean", "Lean.xshd", 99, ".lean;.hlean".Split(';')),
            //new HighlightingResource("Lisp", "Lisp.xshd", 99, ".lisp;.lsp".Split(';')),
            new SyntaxDefinition("Lua", "Lua.xshd", 99, ".lua"),
            new SyntaxDefinition("Nemerle", "Nemerle.xshd", 99, ".n"),
            new SyntaxDefinition("Nim", "Nim.xshd", 99, ".nim"),
            new SyntaxDefinition("Objective-C", "Objective-C.xshd", 99, ".m"),
            new SyntaxDefinition("OCaml", "OCaml.xshd", 99,
                ".ml;.mli".Split(';')),
            new SyntaxDefinition("ParaSail", "ParaSail.xshd", 99,
                ".psi;.psl".Split(';')),
            new SyntaxDefinition("Pascal", "Pascal.xshd", 99, ".pas"),
            //new HighlightingResource("PHP", "PHP.xshd", 99,
            //    ".php;.php3;.php4;.php5;.hh".Split(';')),
            new SyntaxDefinition("Pike", "Pike.xshd", 99, ".pike"),
            new SyntaxDefinition("Prolog", "Prolog.xshd", 99, ".pl;.pro".Split(';')),
            new SyntaxDefinition("PureScript", "PureScript.xshd", 99, ".purs"),
            //new HighlightingResource("Python", "Python.xshd", 99, ".py"),
            new SyntaxDefinition("R", "R.xshd", 99, ".r"),
            new SyntaxDefinition("Registry", "Registry.xshd", 99, ".reg"),
            new SyntaxDefinition("Resource", "Resource.xshd", 99, ".rc"),
            new SyntaxDefinition("Rexx", "Rexx.xshd", 99, ".rex"),
            new SyntaxDefinition("Rust", "Rust.xshd", 99, ".rs"),
            new SyntaxDefinition("Scheme", "Scheme.xshd", 99, ".scm;.ss".Split(';')),
            new SyntaxDefinition("Solidity", "Solidity.xshd", 99, ".sol"),
            new SyntaxDefinition("Spike", "Spike.xshd", 99, ".spk"),
            new SyntaxDefinition("SQF", "SQF.xshd", 99, ".sqf;.sqs".Split(';')),
            //new HighlightingResource("SQL", "SQL.xshd", 99, ".sql"),
            new SyntaxDefinition("Swift", "Swift.xshd", 99, ".swift"),
            new SyntaxDefinition("TCL", "TCL.xshd", 99, ".tcl"),
            new SyntaxDefinition("Thrift", "Thrift.xshd", 99, ".thrift"),
            new SyntaxDefinition("TypeScript", "TypeScript.xshd", 99, ".ts;.tsx".Split(';')),
            new SyntaxDefinition("Vala", "Vala.xshd", 99, ".vala"),
            //new HighlightingResource("VB.NET", "VBNET.xshd", 99, ".vb"),
            new SyntaxDefinition("VBScript", "VBScript.xshd", 99, ".vbs"),
            new SyntaxDefinition("Verilog", "Verilog.xshd", 99, ".v;.vh".Split(';')),
            new SyntaxDefinition("VHDL", "VHDL.xshd", 99, ".vhd;.vhdl".Split(';')),
            new SyntaxDefinition("Volt", "Volt.xshd", 99, ".volt"),
            new SyntaxDefinition("VS Solution", "VS Solution.xshd", 99, ".sln"),
            new SyntaxDefinition("X10", "X10.xshd", 99, ".x10"),
            new SyntaxDefinition("XC", "XC.xshd", 99, ".xc"),
            //new HighlightingResource("XML", "XML.xshd", 99, ".xml;.xsl;.xslt;.xsd;.syn;.lang;.manifest;.config;.addin;.xshd;.wxs;.wxi;.wxl;.proj;.csproj;.vbproj;.resx;.user;.ilproj;.booproj;.build;.xfrm;.targets;.xaml;.xpt;.xft;.map;.wsdl;.disco".Split(';')),
            new SyntaxDefinition("Xtend", "Xtend.xshd", 99, ".xtend"),

        };

        private ThemedHighlightingManager()
        {
        }

        /// <summary>
        /// Gets the list of highlighting names
        /// </summary>
        public IEnumerable<string> HighlightingNames
        {
            get
            {
                lock (lockObj)
                {
                    return syntaxDefinitions
                        .Where(r => !string.IsNullOrEmpty(r.Name) && r.Extensions.Any())
                        .Select(r => r.Name);
                }
            }
        }

        /// <summary>
        /// Gets a highlighting definition by name.
        /// Returns null if the definition is not found.
        /// </summary>
        public IHighlightingDefinition GetDefinition(string name)
        {
            lock (lockObj)
            {
                bool invertColors = mode == Mode.Normal ? false : mode == Mode.Inverted ? true :
                    (bool)Application.Current.Resources["AvalonEdit.SyntaxColor.Invert"];
                if (invertColors)
                {
                    if (invertedHighlightingsByName.TryGetValue(name, out IHighlightingDefinition definition))
                        return definition;
                }
                else
                {
                    if (normalHighlightingsByName.TryGetValue(name, out IHighlightingDefinition definition))
                        return definition;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a highlighting definition by extension.
        /// Returns null if the definition is not found.
        /// </summary>
        public IHighlightingDefinition GetDefinitionByExtension(string extension)
        {
            lock (lockObj)
            {
                string key = extension.ToLowerInvariant();
                bool invertColors = mode == Mode.Normal ? false : mode == Mode.Inverted ? true :
                    (bool)Application.Current.Resources["AvalonEdit.SyntaxColor.Invert"];
                if (invertColors)
                {
                    if (invertedHighlightingsByExtension.TryGetValue(key, out IHighlightingDefinition definition))
                        return definition;
                }
                else
                {
                    if (normalHighlightingsByExtension.TryGetValue(key, out IHighlightingDefinition definition))
                        return definition;
                }
                return null;
            }
        }

        public void Initialize()
        {
            Stopwatch sw = Stopwatch.StartNew();

            InitializeUserSyntaxFiles();

            bool invertColors = false;
            mode = Mode.Normal;
            IHighlightingDefinition highlighting;

            for (int idx = 0; idx < 2; idx++)
            {
                foreach (var syntaxDefinition in syntaxDefinitions
                    .Where(s => !string.IsNullOrEmpty(s.Name) && !string.IsNullOrEmpty(s.FileName))
                    .OrderBy(r => r.Priority))
                {
                    if (syntaxDefinition.IsEmbededResource)
                    {
                        highlighting = LoadResourceHighlightingDefinition(syntaxDefinition.FileName);
                    }
                    else
                    {
                        highlighting = LoadFileHighlightingDefinition(syntaxDefinition.FileName);
                    }

                    RegisterHighlighting(syntaxDefinition, highlighting, invertColors);
                }

                invertColors = true;
                mode = Mode.Inverted;
            }

            mode = Mode.Runtime;

            sw.Stop();
            Debug.WriteLine($"ThemedHighlightingManager Initialize in {sw.ElapsedMilliseconds} ms");
        }

        private void InitializeUserSyntaxFiles()
        {
            string dataFolder = Utils.GetDataFolderPath();
            if (!Directory.Exists(dataFolder))
            {
                return;
            }

            foreach (string fileName in Directory.GetFiles(dataFolder, "*.xshd", SearchOption.AllDirectories))
            {
                try
                {
                    using (TextReader textReader = new StreamReader(fileName))
                    using (XmlReader reader = XmlReader.Create(textReader))
                    {
                        XshdSyntaxDefinition xshd = HighlightingLoader.LoadXshd(reader);
                        if (string.IsNullOrEmpty(xshd.Name))
                        {
                            logger.Error($"Failed to load user syntax file '{fileName}': SyntaxDefinition name is missing.");
                        }
                        else
                        {
                            var existing = syntaxDefinitions.FirstOrDefault(s => s.Name == xshd.Name);
                            if (existing != null)
                            {
                                syntaxDefinitions.Remove(existing);
                            }
                            var extensions = xshd.Extensions.Select(s => s.TrimStart('*')).ToArray();
                            var syntax = new SyntaxDefinition(xshd.Name, fileName, 0, extensions);
                            syntaxDefinitions.Add(syntax);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to load user syntax file '{fileName}': {ex.Message}");
                }
            }
        }

        private IHighlightingDefinition LoadFileHighlightingDefinition(string fileName)
        {
            try
            {
                using (TextReader textReader = new StreamReader(fileName))
                using (XmlReader reader = XmlReader.Create(textReader))
                {
                    return HighlightingLoader.Load(reader, this);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load user syntax file '{fileName}': {ex.Message}");
            }
            return null;
        }

        private IHighlightingDefinition LoadResourceHighlightingDefinition(string resourceName)
        {
            try
            {
                var type = typeof(ThemedHighlightingManager);
                var fullName = type.Namespace + @".Resources." + resourceName;
                using (var stream = type.Assembly.GetManifestResourceStream(fullName))
                using (var reader = new XmlTextReader(stream))
                {
                    return HighlightingLoader.Load(reader, this);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load syntax file '{resourceName}': {ex.Message}");
            }
            return null;
        }

        private void RegisterHighlighting(SyntaxDefinition syntax,
            IHighlightingDefinition highlighting, bool invertColors)
        {
            if (syntax == null || highlighting == null)
                return;

            if (invertColors)
            {
                ColorInverter.TranslateThemeColors(highlighting);
            }

            lock (lockObj)
            {
                if (invertColors)
                {
                    if (!invertedHighlightingsByName.ContainsKey(syntax.Name))
                    {
                        invertedHighlightingsByName.Add(syntax.Name, highlighting);
                    }

                    foreach (string ext in syntax.Extensions)
                    {
                        if (!invertedHighlightingsByExtension.ContainsKey(ext))
                        {
                            invertedHighlightingsByExtension.Add(ext, highlighting);
                        }
                    }
                }
                else
                {
                    if (!normalHighlightingsByName.ContainsKey(syntax.Name))
                    {
                        normalHighlightingsByName.Add(syntax.Name, highlighting);
                    }

                    foreach (string ext in syntax.Extensions)
                    {
                        if (!normalHighlightingsByExtension.ContainsKey(ext))
                        {
                            normalHighlightingsByExtension.Add(ext, highlighting);
                        }
                        else
                        {
                            Debug.WriteLine("Duplicate extension association: " + ext);
                        }
                    }
                }
            }
        }
    }

    public class SyntaxDefinition
    {
        public SyntaxDefinition(string name, string fileName, int priority,
            params string[] extensions)
        {
            Name = name;
            FileName = fileName;
            IsEmbededResource = !Path.IsPathRooted(fileName);
            Priority = priority;
            Extensions = extensions ?? new string[0];
            
        }

        public string Name { get; private set; }
        public string FileName { get; private set; }
        public bool IsEmbededResource { get; private set; }
        public string[] Extensions { get; private set; }
        public int Priority { get; private set; }
    }

}
