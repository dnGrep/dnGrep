using System;
using System.IO;
using System.Xml.Serialization;
using NLog;

namespace dnGREP.Common
{
    public class DirectoryConfiguration
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static DirectoryConfiguration? instance;
        private static string configFilePath = string.Empty;
        private static bool? canUseCurrentFolder = null;

        public static DirectoryConfiguration Instance
        {
            get
            {
                instance ??= Load();
                return instance;
            }
        }

        private static DirectoryConfiguration Load()
        {
            string defaultDir = GetDataFolderPath();

            configFilePath = Path.Combine(defaultDir, "dnGrep.config.xml");
            if (File.Exists(configFilePath))
            {
                try
                {
                    // load and parse the xml file
                    var config = LoadXml<DirectoryConfiguration>(configFilePath);
                    if (config != null)
                    {
                        config.DefaultDataDirectory = defaultDir;
                        config.DefaultLogDirectory = Path.Combine(defaultDir, "logs");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }

            DirectoryConfiguration defaultConfig = new()
            {
                DefaultDataDirectory = defaultDir,
                DefaultLogDirectory = Path.Combine(defaultDir, "logs"),
                DataDirectory = defaultDir,
                LogDirectory = Path.Combine(defaultDir, "logs"),
            };
            return defaultConfig;
        }

        private static DirectoryConfiguration? LoadXml<T>(string path)
        {
            using TextReader reader = new StreamReader(path);
            XmlSerializer serializer = new(typeof(DirectoryConfiguration));
            return serializer.Deserialize(reader) as DirectoryConfiguration;
        }

        /// <summary>
        /// Returns path to folder where user has write access to. Either current folder or user APP_DATA.
        /// </summary>
        /// <returns></returns>
        private static string GetDataFolderPath()
        {
            string currentFolder = Utils.GetCurrentPath(typeof(Utils));
            if (!canUseCurrentFolder.HasValue)
            {
                // if started in Admin mode, the user can write to these directories
                // so filter them out first...
                if (currentFolder.IsSubDirectoryOf(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)) ||
                    currentFolder.IsSubDirectoryOf(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) ||
                    currentFolder.IsSubDirectoryOf(Environment.GetFolderPath(Environment.SpecialFolder.Windows)))
                {
                    canUseCurrentFolder = false;
                }
                else
                {
                    canUseCurrentFolder = HasWriteAccessToFolder(currentFolder);
                }
            }

            if (canUseCurrentFolder == true)
            {
                return currentFolder;
            }
            else
            {
                string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnGREP");
                if (!Directory.Exists(dataFolder))
                    Directory.CreateDirectory(dataFolder);
                return dataFolder;
            }
        }

        private static bool HasWriteAccessToFolder(string folderPath)
        {
            string filename = Path.Combine(folderPath, "~temp.dat");
            bool canAccess = true;

            //2. Attempt the action but handle permission changes.
            try
            {
                using FileStream fileStream = File.Open(filename, FileMode.Create);
                using TextWriter writer = new StreamWriter(fileStream);
                writer.WriteLine("sometext");
            }
            catch
            {
                //No permission. 
                canAccess = false;
            }

            // Cleanup
            try
            {
                Utils.DeleteFile(filename);
            }
            catch
            {
                // Ignore
            }

            return canAccess;
        }

        private DirectoryConfiguration()
        {
        }

        [XmlIgnore]
        public bool IsDataDirectoryDefault => DefaultDataDirectory.Equals(DataDirectory, StringComparison.OrdinalIgnoreCase);

        [XmlIgnore]
        public bool IsLogDirectoryDefault => DefaultLogDirectory.Equals(LogDirectory, StringComparison.OrdinalIgnoreCase);

        [XmlIgnore]
        public bool PathsAreDefault => IsDataDirectoryDefault && IsLogDirectoryDefault;

        [XmlIgnore]
        public bool IsPortableMode
        {
            get
            {
                if (!canUseCurrentFolder.HasValue)
                {
                    GetDataFolderPath();
                }
                return IsDataDirectoryDefault && (canUseCurrentFolder ?? false);
            }
        }

        [XmlIgnore]
        public string DefaultDataDirectory { get; private set; } = string.Empty;

        [XmlIgnore]
        public string DefaultLogDirectory { get; private set; } = string.Empty;


        public string DataDirectory { get; set; } = string.Empty;

        public string LogDirectory { get; set; } = string.Empty;

        public void Save()
        {
            try
            {
                using TextWriter writer = new StreamWriter(configFilePath);
                XmlSerializer serializer = new(typeof(DirectoryConfiguration));
                serializer.Serialize(writer, this);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public void RemoveConfigFile()
        {
            DataDirectory = DefaultDataDirectory;
            LogDirectory = DefaultLogDirectory;

            if (File.Exists(configFilePath))
            {
                File.Delete(configFilePath);
            }
        }
    }
}
