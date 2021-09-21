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
            if (FileResources != null)
            {
                if (FileResources.Resources.TryGetValue(name, out string value))
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return base.GetString(name, CultureInfo.InvariantCulture);
                    }
                    return value;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (culture != null && culture.Name != null && culture.Name.Equals("en"))
                {
                    return base.GetString(name, CultureInfo.InvariantCulture);
                }

                string str = base.GetString(name, culture);
                if (string.IsNullOrEmpty(str))
                {
                    str = base.GetString(name, CultureInfo.InvariantCulture);
                }
                return str;
            }
        }
    }
}

