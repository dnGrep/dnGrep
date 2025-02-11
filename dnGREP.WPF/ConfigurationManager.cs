using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using NLog;

namespace dnGREP.WPF
{
    internal class ConfigurationManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static ConfigurationManager? instance;
        public static ConfigurationManager Instance
        {
            get
            {
                instance ??= new();
                return instance;
            }
        }

        internal bool SaveDirectoryChanges(string oldDataDirectory, string newDataDirectory,
            string oldLogDirectory, string newLogDirectory)
        {
            bool isDataDirectoryChanged = !oldDataDirectory.Equals(newDataDirectory, StringComparison.OrdinalIgnoreCase);
            bool isLogDirectoryChanged = !oldLogDirectory.Equals(newLogDirectory, StringComparison.OrdinalIgnoreCase);

            if (isDataDirectoryChanged || isLogDirectoryChanged)
            {
                if (!Directory.Exists(newDataDirectory))
                {
                    Directory.CreateDirectory(newDataDirectory);
                }

                var scripts = Path.Combine(newDataDirectory, "Scripts");
                if (!Directory.Exists(scripts))
                {
                    Directory.CreateDirectory(scripts);
                }

                var filters = Path.Combine(newDataDirectory, "Filters");
                if (!Directory.Exists(filters))
                {
                    Directory.CreateDirectory(filters);
                }

                if (!Directory.Exists(newLogDirectory))
                {
                    Directory.CreateDirectory(newLogDirectory);
                }

                if (isDataDirectoryChanged && !ValidateDirectory(newDataDirectory, true))
                {
                    return false;
                }
                if (isLogDirectoryChanged && !ValidateDirectory(newLogDirectory, false))
                {
                    return false;
                }

                if (!CopyFiles(isDataDirectoryChanged, oldDataDirectory, newDataDirectory,
                        isLogDirectoryChanged, oldLogDirectory, newLogDirectory))
                {
                    MessageBox.Show(Resources.MessageBox_CopySettingsFileError + App.LogDir,
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK,
                        TranslationSource.Instance.FlowDirection);
                }
            }

            DirectoryConfiguration.Instance.DataDirectory = newDataDirectory;
            DirectoryConfiguration.Instance.LogDirectory = newLogDirectory;

            if (DirectoryConfiguration.Instance.IsDataDirectoryDefault &&
                DirectoryConfiguration.Instance.IsLogDirectoryDefault)
            {
                DirectoryConfiguration.Instance.RemoveConfigFile();
            }
            else
            {
                DirectoryConfiguration.Instance.Save();
            }

            if (isLogDirectoryChanged)
            {
                GlobalDiagnosticsContext.Set("logDir", DirectoryConfiguration.Instance.LogDirectory);
            }

            if (isDataDirectoryChanged)
            {
                GrepSettings.Instance.Load();
                TranslationSource.Instance.SetCulture(GrepSettings.Instance.Get<string>(GrepSettings.Key.CurrentCulture));
                BookmarkLibrary.Load();
                ThemedHighlightingManager.Instance.Initialize();
                AppTheme.Instance.Initialize();

                if (Application.Current.MainWindow is MainForm window)
                    window.ViewModel.OnConfigurationFoldersChanged();
            }

            return true;
        }

        private bool ValidateDirectory(string path, bool isData)
        {
            if (isData && DirectoryConfiguration.Instance.IsApplicationDirectory(path))
            {
                return true;
            }

            string fileName = isData ? "dnGREP.Settings.dat|bookmarks.xml|*.xaml" : "Grep_Error_Log.xml";

            string[] allFiles = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);

            if (allFiles.Length > 0)
            {
                int count = 0;
                string[] searchPatterns = fileName.Split('|');
                foreach (string searchPattern in searchPatterns)
                {
                    count += Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly).Length;
                }

                if (count == 0)
                {
                    if (MessageBoxResult.No == MessageBox.Show(
                        TranslationSource.Format(Resources.MessageBox_DirectoryNotEmptyWarning, path),
                        Resources.MessageBox_DnGrep,
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes,
                        TranslationSource.Instance.FlowDirection))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CopyFiles(bool isDataDirectoryChanged, string oldDataDirectory, string newDataDirectory,
            bool isLogDirectoryChanged, string oldLogDirectory, string newLogDirectory)
        {
            var answer = MessageBox.Show(Resources.MessageBox_CopyApplicationDataFilesQuestion,
                Resources.MessageBox_DnGrep,
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No,
                TranslationSource.Instance.FlowDirection);

            // backup the new locations before new files are copied
            // or before it is used if not copying files
            if (isDataDirectoryChanged)
            {
                BackupDataDirectory(newDataDirectory, newDataDirectory, "dnGrepSettingsBackup.zip");
            }

            if (isLogDirectoryChanged)
            {
                BackupLogDirectory(newLogDirectory, newLogDirectory, "dnGrepSettingsBackup.zip");
            }

            if (answer == MessageBoxResult.Yes)
            {
                if (isDataDirectoryChanged)
                {
                    string oldScriptsDir = Path.Combine(oldDataDirectory, "Scripts");
                    string oldFiltersDir = Path.Combine(oldDataDirectory, "Filters");
                    string newScriptsDir = Path.Combine(newDataDirectory, "Scripts");
                    string newFiltersDir = Path.Combine(newDataDirectory, "Filters");

                    DirectoryCompareResult files = new();

                    var files1 = CompareFolders.GetFolderDifferences(
                        oldDataDirectory, oldDataDirectory,
                        newDataDirectory, newDataDirectory,
                        @"dnGREP.Settings.dat|bookmarks.xml|*.xshd", SearchOption.TopDirectoryOnly);
                    files.Merge(files1);

                    bool copyFromAppDir = DirectoryConfiguration.Instance.IsApplicationDirectory(oldDataDirectory);
                    bool copyToAppDir = DirectoryConfiguration.Instance.IsApplicationDirectory(newDataDirectory);
                    if (copyFromAppDir)
                    {
                        var files2 = CompareFolders.GetFolderDifferences(
                            Path.Combine(oldDataDirectory, "Themes"), Path.Combine(oldDataDirectory, "Themes"),
                            newDataDirectory, newDataDirectory,
                            @"*.xaml", SearchOption.TopDirectoryOnly);
                        files.Merge(files2);
                    }
                    else if (copyToAppDir)
                    {
                        var files2 = CompareFolders.GetFolderDifferences(
                            oldDataDirectory, oldDataDirectory,
                            Path.Combine(newDataDirectory, "Themes"), Path.Combine(newDataDirectory, "Themes"),
                            "Themes",
                            @"*.xaml", SearchOption.TopDirectoryOnly);
                        files.Merge(files2);
                    }
                    else
                    {
                        var files2 = CompareFolders.GetFolderDifferences(
                            oldDataDirectory, oldDataDirectory,
                            newDataDirectory, newDataDirectory,
                            @"*.xaml", SearchOption.TopDirectoryOnly);
                        files.Merge(files2);
                    }

                    var files3 = CompareFolders.GetFolderDifferences(
                        oldDataDirectory, oldScriptsDir,
                        newDataDirectory, newScriptsDir,
                        @"*.*", SearchOption.AllDirectories);
                    files.Merge(files3);

                    var files4 = CompareFolders.GetFolderDifferences(
                        oldDataDirectory, oldFiltersDir,
                        newDataDirectory, newFiltersDir,
                        @"*.*", SearchOption.AllDirectories);
                    files.Merge(files4);

                    if (!CopyFiles(files, newDataDirectory))
                        return false;
                }

                if (isLogDirectoryChanged)
                {
                    var logFiles = CompareFolders.GetFolderDifferences(
                        oldLogDirectory, oldLogDirectory,
                        newLogDirectory, newLogDirectory,
                        @"*.xml", SearchOption.TopDirectoryOnly);

                    if (!CopyFiles(logFiles, newLogDirectory))
                        return false;
                }
            }

            return true;
        }

        private static bool BackupDataDirectory(string directoryToBackup, string saveToDirectory, string zipFileName)
        {
            try
            {
                // selectively choose backup files
                int count = 0;
                string fullFilePath = GetAvailableFileName(saveToDirectory, zipFileName);
                using (ZipArchive zipArchive = ZipFile.Open(fullFilePath, ZipArchiveMode.Create))
                {
                    var files = Directory.GetFiles(directoryToBackup, "*.dat", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                        count++;
                    }

                    files = Directory.GetFiles(directoryToBackup, "bookmarks.xml", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                        count++;
                    }

                    files = Directory.GetFiles(directoryToBackup, "*.xaml", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                        count++;
                    }

                    files = Directory.GetFiles(directoryToBackup, "*.xshd", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                        count++;
                    }

                    var dir = Path.Combine(directoryToBackup, "Scripts");
                    files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetRelativePath(directoryToBackup, file));
                        count++;
                    }

                    dir = Path.Combine(directoryToBackup, "Filters");
                    files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetRelativePath(directoryToBackup, file));
                        count++;
                    }
                }

                if (count == 0)
                {
                    File.Delete(fullFilePath);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Backup configuration: Failed to backup data directories");
                return false;
            }
            return true;
        }

        private static bool BackupLogDirectory(string directoryToBackup, string saveToDirectory, string zipFileName)
        {
            try
            {
                var hasBackupFiles = Directory.GetFiles(directoryToBackup, "*.xml", SearchOption.AllDirectories).Length > 0;
                if (hasBackupFiles)
                {
                    string fullFilePath = GetAvailableFileName(saveToDirectory, zipFileName);
                    if (!string.IsNullOrEmpty(fullFilePath))
                        ZipFile.CreateFromDirectory(directoryToBackup, fullFilePath);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Backup configuration: Failed to backup log directory");
                return false;
            }
            return true;
        }

        private static bool CopyFiles(DirectoryCompareResult files, string targetDirectory)
        {
            foreach (var file in files.SourceOnly)
            {
                string destFile = Path.Combine(targetDirectory, file.RelativeTargetPath);
                try
                {
                    string? dir = Path.GetDirectoryName(destFile);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.Copy(file.FileInfo.FullName, destFile, false);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Copy configuration: Failed to copy file '{file.FileInfo.FullName}' to '{destFile}'");
                    return false;
                }
            }

            // ask the user what to do with conflicting files
            bool askForEach = true;
            MessageBoxResultEx allResult = MessageBoxResultEx.None;
            foreach (var file in files.Conflicting)
            {
                CustomMessageBoxResult answer = new(allResult, false);
                if (askForEach)
                {
                    answer = CustomMessageBox.Show(
                        TranslationSource.Format(Resources.MessageBox_OverwriteExistingFileQuestion, file.RelativeTargetPath),
                        Resources.MessageBox_DnGrep,
                        MessageBoxButtonEx.YesAllNoAll, MessageBoxImage.Question,
                        MessageBoxResultEx.No, MessageBoxCustoms.None,
                        TranslationSource.Instance.FlowDirection);

                    if (answer.Result == MessageBoxResultEx.YesToAll || answer.Result == MessageBoxResultEx.NoToAll)
                    {
                        askForEach = false;
                        allResult = answer.Result;
                    }
                }
                else
                {
                    answer = new CustomMessageBoxResult(allResult, false);
                }

                if (answer.Result == MessageBoxResultEx.Yes || answer.Result == MessageBoxResultEx.YesToAll)
                {
                    // if yes, overwrite the existing file
                    string destFile = Path.Combine(targetDirectory, file.RelativeTargetPath);
                    try
                    {
                        string? dir = Path.GetDirectoryName(destFile);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        File.Copy(file.FileInfo.FullName, destFile, true);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Copy configuration: Failed to copy file '{file.FileInfo.FullName}' to '{destFile}'");
                        return false;
                    }
                }
            }

            return true;
        }

        private static string GetAvailableFileName(string path, string fileName)
        {
            string destFile = string.Empty;
            for (int idx = 1; idx < 10000; idx++)
            {
                string baseName = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                string temp = Path.Combine(path, $"{baseName}-{idx}.{extension}");
                if (!File.Exists(temp))
                {
                    destFile = temp;
                    break;
                }
            }
            return destFile;
        }
    }
}
