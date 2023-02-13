using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;
using dnGREP.Common;
using dnGREP.WPF.Properties;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public class AboutViewModel : CultureAwareViewModel
    {
        public AboutViewModel()
        {
            Version = $"{Resources.About_Version} {AssemblyVersion}";
            BuildDate = $"{Resources.About_BuiltOn} {AssemblyBuildDate?.ToString(CultureInfo.CurrentCulture)}";
            Copyright = AssemblyCopyright;
            Description = AssemblyDescription;
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
        }

        public ICommand CopyVersionCommand => new RelayCommand(
            p => NativeMethods.SetClipboardText(Version));


        private string applicationFontFamily;
        public string ApplicationFontFamily
        {
            get { return applicationFontFamily; }
            set
            {
                if (applicationFontFamily == value)
                    return;

                applicationFontFamily = value;
                base.OnPropertyChanged(nameof(ApplicationFontFamily));
            }
        }

        private double dialogfontSize;
        public double DialogFontSize
        {
            get { return dialogfontSize; }
            set
            {
                if (dialogfontSize == value)
                    return;

                dialogfontSize = value;
                base.OnPropertyChanged(nameof(DialogFontSize));
            }
        }

        private string _version = string.Empty;
        public string Version
        {
            get { return _version; }

            set
            {
                if (_version == value)
                    return;

                _version = value;
                OnPropertyChanged(nameof(Version));
            }
        }

        private string _buildDate = string.Empty;
        public string BuildDate
        {
            get { return _buildDate; }
            set
            {
                if (_buildDate == value)
                    return;

                _buildDate = value;
                OnPropertyChanged(nameof(BuildDate));
            }
        }

        private string _copyright = string.Empty;
        public string Copyright
        {
            get { return _copyright; }
            set
            {
                if (_copyright == value)
                    return;

                _copyright = value;
                OnPropertyChanged(nameof(Copyright));
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description == value)
                    return;

                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }


        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public static string AssemblyDescription
        {
            get
            {
                // Get all Description attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                // If there aren't any Description attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Description attribute, return its value
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public static string AssemblyCopyright
        {
            get
            {
                // Get all Copyright attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                // If there aren't any Copyright attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Copyright attribute, return its value
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public static DateTime? AssemblyBuildDate => GetAssemblyBuildDateTime(Assembly.GetExecutingAssembly());

        // https://stackoverflow.com/questions/1600962/displaying-the-build-date
        private static DateTime? GetAssemblyBuildDateTime(Assembly assembly)
        {
            var attr = Attribute.GetCustomAttribute(assembly, typeof(BuildDateTimeAttribute)) as BuildDateTimeAttribute;
            if (DateTime.TryParse(attr?.Date, out DateTime dt))
                return dt.ToLocalTime();
            else
                return null;
        }
    }
}
