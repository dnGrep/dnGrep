using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace dnGREP.Common
{
    public record CustomEditor(string Label, string Path, string Args,
        bool EscapeQuotes, string Extensions)
    {
        public List<string> ExtensionList
        {
            get
            {
                if (string.IsNullOrEmpty(Extensions))
                {
                    return [];
                }

                return [.. Extensions.Split(',', StringSplitOptions.RemoveEmptyEntries)];
            }
        }

        public static string Serialize(List<CustomEditor> items)
        {
            if (items.Count > 0)
            {
                XElement root = new("customEditorArray");
                foreach (CustomEditor value in items)
                {
                    var elem = new XElement("customEditor");
                    elem.Add(new XElement("label", value.Label));
                    elem.Add(new XElement("path", value.Path));
                    elem.Add(new XElement("args", value.Args));
                    elem.Add(new XElement("escQuotes", value.EscapeQuotes));
                    elem.Add(new XElement("extensions", value.Extensions));
                    root.Add(elem);
                }

                return root.ToString();
            }
            return string.Empty;
        }

        public static List<CustomEditor> Deserialize(string xmlContent)
        {
            List<CustomEditor> list = [];

            if (!string.IsNullOrEmpty(xmlContent))
            {
                XElement root = XElement.Parse(xmlContent, LoadOptions.PreserveWhitespace);
                if (root != null)
                {
                    foreach (var elem in root.Descendants("customEditor"))
                    {
                        string label = elem.GetElementString("label");
                        string path = elem.GetElementString("path");
                        string args = elem.GetElementString("args");
                        string extensions = elem.GetElementString("extensions");
                        bool escQuotes = elem.GetElementBoolean("escQuotes");

                        if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(path))
                        {
                            list.Add(new CustomEditor(label, path, args, escQuotes, extensions));
                        }
                    }
                }
            }

            return list;
        }
    }
}
