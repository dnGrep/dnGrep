using System;
using System.Globalization;
using System.Reflection;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class AboutViewModel : ViewModelBase
    {
        public AboutViewModel()
        {
            Version = $"{Properties.Resources.Version} {AssemblyVersion}";
            BuildDate = $"{Properties.Resources.Built_on} {AssemblyBuildDate.ToString(CultureInfo.CurrentUICulture)}";
            Copyright = AssemblyCopyright;
            Description = AssemblyDescription;
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);
        }

        private string applicationFontFamily;
        public string ApplicationFontFamily
        {
            get { return applicationFontFamily; }
            set
            {
                if (applicationFontFamily == value)
                    return;

                applicationFontFamily = value;
                base.OnPropertyChanged(() => ApplicationFontFamily);
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
                base.OnPropertyChanged(() => DialogFontSize);
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
                OnPropertyChanged(() => Version);
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
                OnPropertyChanged(() => BuildDate);
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
                OnPropertyChanged(() => Copyright);
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
                OnPropertyChanged(() => Description);
            }
        }


        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
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

        public string AssemblyCopyright
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

        public DateTime AssemblyBuildDate
        {
            get { return GetLinkerTime(Assembly.GetExecutingAssembly()); }
        }

        // http://stackoverflow.com/questions/1600962/displaying-the-build-date
        public static DateTime GetLinkerTime(Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = File.Open(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }
    }
}
