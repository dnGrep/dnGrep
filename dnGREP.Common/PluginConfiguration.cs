using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace dnGREP.Common
{
    public record PluginConfiguration(string Name, bool Enabled, bool PreviewText,
        string Extensions, bool ApplyStringMap)
    {
        private static readonly char[] separators = [',', ';', ' '];

        public List<string> ExtensionList
        {
            get
            {
                if (string.IsNullOrEmpty(Extensions))
                {
                    return [];
                }

                return [.. Extensions.Split(separators, StringSplitOptions.RemoveEmptyEntries)];
            }
        }

        public static string Serialize(List<PluginConfiguration> items)
        {
            if (items.Count > 0)
            {
                XElement root = new("pluginArray");
                foreach (PluginConfiguration value in items)
                {
                    var elem = new XElement("plugin");
                    elem.Add(new XElement("name", value.Name));
                    elem.Add(new XElement("enabled", value.Enabled));
                    elem.Add(new XElement("previewText", value.PreviewText));
                    elem.Add(new XElement("extensions", value.Extensions));
                    elem.Add(new XElement("applyStringMap", value.ApplyStringMap));
                    root.Add(elem);
                }

                return root.ToString();
            }
            return string.Empty;
        }

        public static List<PluginConfiguration> Deserialize(string xmlContent)
        {
            List<PluginConfiguration> list = [];

            if (!string.IsNullOrEmpty(xmlContent))
            {
                XElement root = XElement.Parse(xmlContent, LoadOptions.PreserveWhitespace);
                if (root != null)
                {
                    foreach (var elem in root.Descendants("plugin"))
                    {
                        string name = elem.GetElementString("name");
                        bool enabled = elem.GetElementBoolean("enabled");
                        bool previewText = elem.GetElementBoolean("previewText");
                        string extensions = elem.GetElementString("extensions");
                        bool applyStringMap = elem.GetElementBoolean("applyStringMap");

                        if (!string.IsNullOrEmpty(name))
                        {
                            list.Add(new PluginConfiguration(name, enabled, previewText, extensions, applyStringMap));
                        }
                    }
                }
            }

            return list;
        }

    }
}
