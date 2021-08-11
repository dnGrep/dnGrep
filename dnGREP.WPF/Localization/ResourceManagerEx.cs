using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace dnGREP.WPF
{
    public class ResourceManagerEx : ResourceManager
    {
        public static ResourceManagerEx Instance { get; private set; }

        internal ResxFile FileResources { get; set; }

        public static void Initialize()
        {
            Type resourcesType = typeof(dnGREP.WPF.Properties.Resources);
            var manager = new ResourceManagerEx("dnGREP.WPF.Properties.Resources", resourcesType.Assembly);

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
                    return value;
                }
                else
                {
                    return null;
                } 
                    
            }
            else
            {
                return base.GetString(name, culture);
            }
        }
    }
}

