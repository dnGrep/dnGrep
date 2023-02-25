using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace dnGREP.Localization
{
    public class ResourceManagerEx : ResourceManager
    {
        private static ResourceManagerEx? instance = null;
        public static ResourceManagerEx Instance
        {
            get
            {
                if (instance == null)
                {
                    Initialize();
                }
                return instance;
            }
        }

        internal ResxFile? FileResources { get; set; }

        [MemberNotNull(nameof(instance))]
        public static void Initialize()
        {
            Type resourcesType = typeof(dnGREP.Localization.Properties.Resources);
            ResourceManagerEx manager = new("dnGREP.Localization.Properties.Resources", resourcesType.Assembly);

            // set this class as the public static dnGREP.Localization.Properties.Resources.ResourceManager
            FieldInfo? fi = resourcesType.GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
            fi?.SetValue(null, manager);

            instance = manager;
        }

        public ResourceManagerEx(string baseName, Assembly assembly)
            : base(baseName, assembly)
        {
        }

        public override string? GetString(string name, CultureInfo? culture)
        {
            string? result = null;
            if (FileResources != null)
            {
                if (FileResources.Resources.TryGetValue(name, out string? value))
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
                if (culture?.Name.Equals("en") is true)
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

