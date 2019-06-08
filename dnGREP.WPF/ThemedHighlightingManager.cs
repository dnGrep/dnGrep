using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace dnGREP.WPF
{
    public class ThemedHighlightingManager : IHighlightingDefinitionReferenceResolver
    {
        public static ThemedHighlightingManager Instance { get; } = new ThemedHighlightingManager();

        private enum Mode { Normal, Inverted, Runtime }
        private Mode mode;

        private readonly object lockObj = new object();
        private readonly Dictionary<string, IHighlightingDefinition> normalHighlightingsByName = new Dictionary<string, IHighlightingDefinition>();
        private readonly Dictionary<string, IHighlightingDefinition> normalHighlightingsByExtension = new Dictionary<string, IHighlightingDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IHighlightingDefinition> invertedHighlightingsByName = new Dictionary<string, IHighlightingDefinition>();
        private readonly Dictionary<string, IHighlightingDefinition> invertedHighlightingsByExtension = new Dictionary<string, IHighlightingDefinition>(StringComparer.OrdinalIgnoreCase);

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
                    return normalHighlightingsByName.Values.Select(r => r.Name);
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
                bool invertColors = mode == Mode.Normal ? false : mode == Mode.Inverted ? true :
                    (bool)Application.Current.Resources["AvalonEdit.SyntaxColor.Invert"];
                if (invertColors)
                {
                    if (invertedHighlightingsByExtension.TryGetValue(extension, out IHighlightingDefinition definition))
                        return definition;
                }
                else
                {
                    if (normalHighlightingsByExtension.TryGetValue(extension, out IHighlightingDefinition definition))
                        return definition;
                }
                return null;
            }
        }

        public void Initialize()
        {
            bool invertColors = false;
            mode = Mode.Normal;
            for (int idx = 0; idx < 2; idx++)
            {
                RegisterHighlighting("XmlDoc", null, "XmlDoc.xshd", invertColors);
                RegisterHighlighting("C#", new[] { ".cs" }, "CSharp-Mode.xshd", invertColors);

                RegisterHighlighting("JavaScript", new[] { ".js" }, "JavaScript-Mode.xshd", invertColors);
                RegisterHighlighting("HTML", new[] { ".htm", ".html" }, "HTML-Mode.xshd", invertColors);
                RegisterHighlighting("ASP/XHTML", new[] { ".asp", ".aspx", ".asax", ".asmx", ".ascx", ".master" }, "ASPX.xshd", invertColors);

                RegisterHighlighting("Boo", new[] { ".boo" }, "Boo.xshd", invertColors);
                RegisterHighlighting("Coco", new[] { ".atg" }, "Coco-Mode.xshd", invertColors);
                RegisterHighlighting("CSS", new[] { ".css" }, "CSS-Mode.xshd", invertColors);
                RegisterHighlighting("C++", new[] { ".c", ".h", ".cc", ".cpp", ".hpp" }, "CPP-Mode.xshd", invertColors);
                RegisterHighlighting("Java", new[] { ".java" }, "Java-Mode.xshd", invertColors);
                RegisterHighlighting("Patch", new[] { ".patch", ".diff" }, "Patch-Mode.xshd", invertColors);
                RegisterHighlighting("PowerShell", new[] { ".ps1", ".psm1", ".psd1" }, "PowerShell.xshd", invertColors);
                RegisterHighlighting("PHP", new[] { ".php" }, "PHP-Mode.xshd", invertColors);
                RegisterHighlighting("Python", new[] { ".py", ".pyw" }, "Python-Mode.xshd", invertColors);
                RegisterHighlighting("SQL", new[] { ".sql" }, "Sql-Mode.xshd", invertColors);
                RegisterHighlighting("TeX", new[] { ".tex" }, "Tex-Mode.xshd", invertColors);
                RegisterHighlighting("TSQL", new[] { ".sql" }, "TSQL-Mode.xshd", invertColors);
                RegisterHighlighting("VBNET", new[] { ".vb" }, "VB-Mode.xshd", invertColors);
                RegisterHighlighting("XML", (".xml;.xsl;.xslt;.xsd;.manifest;.config;.addin;" +
                                                 ".xshd;.wxs;.wxi;.wxl;.proj;.csproj;.vbproj;.ilproj;" +
                                                 ".booproj;.build;.xfrm;.targets;.xaml;.xpt;" +
                                                 ".xft;.map;.wsdl;.disco;.ps1xml;.nuspec").Split(';'),
                                         "XML-Mode.xshd", invertColors);
                RegisterHighlighting("MarkDown", new[] { ".md" }, "MarkDown-Mode.xshd", invertColors);

                invertColors = true;
                mode = Mode.Inverted;
            }
            mode = Mode.Runtime;
        }

        private IHighlightingDefinition LoadHighlightingDefinition(string resourceName)
        {
            string fullName = string.Empty;
            try
            {
                var type = typeof(ThemedHighlightingManager);
                fullName = type.Namespace + @".Resources." + resourceName;
                using (var stream = type.Assembly.GetManifestResourceStream(fullName))
                using (var reader = new XmlTextReader(stream))
                {
                    return HighlightingLoader.Load(reader, this);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
            return null;
        }

        private void RegisterHighlighting(string name, string[] extensions, string resourceName, bool invertColors)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentNullException("resourceName");

            IHighlightingDefinition highlighting = LoadHighlightingDefinition(resourceName);

            if (highlighting == null)
                return;

            if (invertColors)
            {
                ColorInverter.TranslateThemeColors(highlighting);
            }

            lock (lockObj)
            {
                if (invertColors)
                {
                    if (name != null)
                    {
                        invertedHighlightingsByName[name] = highlighting;
                    }
                    if (extensions != null)
                    {
                        foreach (string ext in extensions)
                        {
                            invertedHighlightingsByExtension[ext] = highlighting;
                        }
                    }
                }
                else
                {
                    if (name != null)
                    {
                        normalHighlightingsByName[name] = highlighting;
                    }
                    if (extensions != null)
                    {
                        foreach (string ext in extensions)
                        {
                            normalHighlightingsByExtension[ext] = highlighting;
                        }
                    }
                }
            }
        }


    }
}
