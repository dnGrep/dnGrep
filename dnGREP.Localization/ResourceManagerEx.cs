using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace dnGREP.Localization
{
    public class ResourceManagerEx : ResourceManager
    {
        public static ResourceManagerEx Instance { get; private set; }

        internal ResxFile FileResources { get; set; }

        public static void Initialize()
        {
            Type resourcesType = typeof(dnGREP.Localization.Properties.Resources);
            var manager = new ResourceManagerEx("dnGREP.Localization.Properties.Resources", resourcesType.Assembly);

            FieldInfo fi = resourcesType.GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
            fi.SetValue(null, manager);

            Instance = manager;
        }

        public ResourceManagerEx(string baseName, Assembly assembly)
            : base(baseName, assembly)
        {
        }

        public override string GetString(string name, CultureInfo culture)
        {
            string result = null;
            if (FileResources != null)
            {
                if (FileResources.Resources.TryGetValue(name, out string value))
                {
                    result = value;
                }

                if (string.IsNullOrEmpty(result))
                {
                    // no need to check for RTL language
                    return base.GetString(name, CultureInfo.InvariantCulture);
                }
            }
            else
            {
                if (culture != null && culture.Name != null && culture.Name.Equals("en"))
                {
                    // no need to check for RTL language
                    return base.GetString(name, CultureInfo.InvariantCulture);
                }

                result = base.GetString(name, culture);
                if (string.IsNullOrEmpty(result))
                {
                    // no need to check for RTL language
                    return base.GetString(name, CultureInfo.InvariantCulture);
                }
            }

            if (!string.IsNullOrWhiteSpace(result) && TranslationSource.Instance.CurrentCulture.TextInfo.IsRightToLeft)
            {
                result = result.Replace($"\\u200e", char.ConvertFromUtf32(0x200e));
                result = result.Replace($"\\u200f", char.ConvertFromUtf32(0x200f));
            }

            return result;
        }
    }
}

