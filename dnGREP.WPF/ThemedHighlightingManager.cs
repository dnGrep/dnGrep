using System.Linq;
using System.Windows;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace dnGREP.WPF
{
    public class ThemedHighlightingManager
    {
        public static ThemedHighlightingManager Instance { get; } = new ThemedHighlightingManager();

        private ThemedHighlightingManager()
        {
        }

        public void Initialize()
        {
            if (!HighlightingManager.Instance.HighlightingDefinitions.Any(d => d.Name == "SQL"))
            {
                var sql = LoadHighlightingDefinition("sqlmode.xshd");
                HighlightingManager.Instance.RegisterHighlighting("SQL", new string[] { ".sql" }, sql);
            }

            bool invertColors = (bool)Application.Current.Resources["AvalonEdit.SyntaxColor.Invert"];
            if (invertColors)
            {
                foreach (var hl in HighlightingManager.Instance.HighlightingDefinitions)
                {
                    ColorInverter.TranslateThemeColors(hl);
                }
            }
        }

        private IHighlightingDefinition LoadHighlightingDefinition(string resourceName)
        {
            var type = typeof(ThemedHighlightingManager);
            var fullName = type.Namespace + "." + resourceName;
            using (var stream = type.Assembly.GetManifestResourceStream(fullName))
            using (var reader = new XmlTextReader(stream))
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
    }
}
