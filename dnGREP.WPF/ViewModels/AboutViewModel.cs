using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.WPF.Properties;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.WPF
{
    public partial class AboutViewModel : CultureAwareViewModel
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


        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private string _version = string.Empty;

        [ObservableProperty]
        private string _buildDate = string.Empty;

        [ObservableProperty]
        private string _copyright = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;


        public static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

        public static string AssemblyDescription
        {
            get
            {
                // Get all Description attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                // If there aren't any Description attributes, return an empty string
                if (attributes.Length == 0)
                    return string.Empty;
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
                    return string.Empty;
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
