using System;
using System.Collections.Generic;
using System.Windows;
using dnGREP.Common;
using dnGREP.Common.UI;
using dnGREP.Localization;
using dnGREP.Localization.Properties;
using NLog;

namespace dnGREP.WPF
{
    public static class FileOperations
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static (bool success, string? message) CopyFiles(
            List<GrepSearchResult> fileList, PathSearchText pathSearchText,
            string? path, bool isScriptRunning)
        {
            bool success = false;
            string? message = null;

            if (fileList.Count > 0)
            {
                string? selectedPath = null;
                if (!string.IsNullOrEmpty(path))
                {
                    selectedPath = path;
                }
                else
                {
                    FileFolderDialogWin32 dlg = new();
                    if (dlg.ShowDialog() == true)
                    {
                        selectedPath = dlg.SelectedPath;
                    }
                }

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    try
                    {
                        bool preserveFolderLayout = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreserveFolderLayoutOnCopy);
                        string destinationFolder = UiUtils.GetBaseFolder(selectedPath);
                        bool hasSingleBaseFolder = UiUtils.HasSingleBaseFolder(pathSearchText.FileOrFolderPath);
                        string baseFolder = pathSearchText.BaseFolder;

                        if (!string.IsNullOrEmpty(destinationFolder) &&
                            !Utils.CanCopyFiles(fileList, destinationFolder))
                        {
                            if (isScriptRunning)
                            {
                                logger.Error(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain);
                                message = Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain;
                            }
                            else
                            {
                                MessageBox.Show(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory + Environment.NewLine +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain,
                                    Resources.MessageBox_DnGrep + " " + Resources.MessageBox_CopyFiles,
                                    MessageBoxButton.OK, MessageBoxImage.Warning,
                                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                            }
                            return (success, message);
                        }

                        var overwritePref = GrepSettings.Instance.Get<OverwriteFile>(GrepSettings.Key.OverwriteFilesOnCopy);
                        if (isScriptRunning && overwritePref == OverwriteFile.Prompt)
                        {
                            overwritePref = OverwriteFile.No;
                        }

                        int filesCopied = 0;
                        if (preserveFolderLayout && hasSingleBaseFolder &&
                            !string.IsNullOrEmpty(destinationFolder) && !string.IsNullOrWhiteSpace(baseFolder))
                        {
                            filesCopied = Utils.CopyFiles(fileList, baseFolder, destinationFolder, overwritePref);
                            success = true;
                        }
                        else if (!string.IsNullOrEmpty(destinationFolder))
                        {
                            // without a common base path, copy all files to a single directory 
                            filesCopied = Utils.CopyFiles(fileList, destinationFolder, overwritePref);
                            success = true;
                        }

                        if (isScriptRunning)
                        {
                            logger.Info(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyCopied, filesCopied) +
                                " " + selectedPath);
                            message = TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyCopied, filesCopied) +
                                " " + selectedPath;
                        }
                        else
                        {
                            MessageBox.Show(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyCopied, filesCopied),
                                Resources.MessageBox_DnGrep + " " + Resources.MessageBox_CopyFiles,
                                MessageBoxButton.OK, MessageBoxImage.Information,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        logger.Error(ex, "Error copying files");

                        if (isScriptRunning)
                        {
                            message = Resources.Scripts_CopyFilesFailed + ex.Message;
                        }
                        else
                        {
                            MessageBox.Show(Resources.MessageBox_ThereWasAnErrorCopyingFiles + App.LogDir,
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                }
            }
            return (success, message);
        }

        public static (bool success, List<string> realFilesMoved, string? message) MoveFiles(
            List<GrepSearchResult> fileList, PathSearchText pathSearchText,
            string? path, bool isScriptRunning)
        {
            bool success = false;
            List<string> realFilesMoved = [];
            string? message = null;

            if (fileList.Count > 0)
            {
                string? selectedPath = null;
                if (!string.IsNullOrEmpty(path))
                {
                    selectedPath = path;
                }
                else
                {
                    FileFolderDialogWin32 dlg = new();
                    if (dlg.ShowDialog() == true)
                    {
                        selectedPath = dlg.SelectedPath;
                    }
                }

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    try
                    {
                        bool preserveFolderLayout = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreserveFolderLayoutOnMove);
                        string destinationFolder = UiUtils.GetBaseFolder(selectedPath);
                        bool hasSingleBaseFolder = UiUtils.HasSingleBaseFolder(pathSearchText.FileOrFolderPath);
                        string baseFolder = pathSearchText.BaseFolder;

                        if (!string.IsNullOrEmpty(destinationFolder) &&
                            !Utils.CanCopyFiles(fileList, destinationFolder))
                        {
                            if (isScriptRunning)
                            {
                                logger.Error(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain);
                                message = Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain;
                            }
                            else
                            {
                                MessageBox.Show(Resources.MessageBox_SomeOfTheFilesAreLocatedInTheSelectedDirectory + Environment.NewLine +
                                    Resources.MessageBox_PleaseSelectAnotherDirectoryAndTryAgain,
                                    Resources.MessageBox_DnGrep + " " + Resources.MessageBox_MoveFiles,
                                    MessageBoxButton.OK, MessageBoxImage.Warning,
                                    MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                            }
                            return (success, realFilesMoved, message);
                        }

                        var overwritePref = GrepSettings.Instance.Get<OverwriteFile>(GrepSettings.Key.OverwriteFilesOnMove);
                        if (isScriptRunning && overwritePref == OverwriteFile.Prompt)
                        {
                            overwritePref = OverwriteFile.No;
                        }

                        int count = 0;
                        if (preserveFolderLayout && hasSingleBaseFolder &&
                            !string.IsNullOrEmpty(destinationFolder) && !string.IsNullOrWhiteSpace(baseFolder))
                        {
                            (count, realFilesMoved) = Utils.MoveFiles(fileList, baseFolder, destinationFolder, overwritePref);
                            success = true;
                        }
                        else if (!string.IsNullOrEmpty(destinationFolder))
                        {
                            // without a common base path, move all files to a single directory 
                            (count, realFilesMoved) = Utils.MoveFiles(fileList, destinationFolder, overwritePref);
                            success = true;
                        }

                        if (isScriptRunning)
                        {
                            logger.Info(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyMoved, count) +
                                " " + selectedPath);
                            message = TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyMoved, count) +
                                " " + selectedPath;
                        }
                        else
                        {
                            MessageBox.Show(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyMoved, count),
                                Resources.MessageBox_DnGrep + " " + Resources.MessageBox_MoveFiles,
                                MessageBoxButton.OK, MessageBoxImage.Information,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        logger.Error(ex, "Error moving files");
                        if (isScriptRunning)
                        {
                            message = Resources.Scripts_MoveFilesFailed + ex.Message;
                        }
                        else
                        {
                            MessageBox.Show(Resources.MessageBox_ThereWasAnErrorMovingFiles + App.LogDir,
                                Resources.MessageBox_DnGrep,
                                MessageBoxButton.OK, MessageBoxImage.Error,
                                MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                        }
                    }
                }
            }
            return (success, realFilesMoved, message);
        }

        public static (bool success, List<string> filesDeleted, string? message) DeleteFiles(
            List<GrepSearchResult> fileList, bool isScriptRunning, bool checkForRecycleSetting)
        {
            bool success = false;
            List<string> filesDeleted = [];
            string? message = null;

            if (fileList.Count > 0)
            {
                try
                {
                    if (!isScriptRunning)
                    {
                        if (MessageBox.Show(Resources.MessageBox_YouAreAboutToDeleteFilesFoundDuringSearch + Environment.NewLine +
                                Resources.MessageBox_AreYouSureYouWantToContinue,
                                Resources.MessageBox_DnGrep + " " + Resources.MessageBox_DeleteFiles,
                                MessageBoxButton.YesNo, MessageBoxImage.Warning,
                                MessageBoxResult.No, TranslationSource.Instance.FlowDirection) != MessageBoxResult.Yes)
                        {
                            return (success, [], message);
                        }
                    }

                    if (checkForRecycleSetting && GrepSettings.Instance.Get<bool>(GrepSettings.Key.DeleteToRecycleBin))
                    {
                        filesDeleted = Utils.SendToRecycleBin(fileList);
                        success = filesDeleted.Count > 0;
                    }
                    else
                    {
                        filesDeleted = Utils.DeleteFiles(fileList);
                        success = filesDeleted.Count > 0;
                    }

                    if (isScriptRunning)
                    {
                        logger.Info(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyDeleted, filesDeleted.Count));
                        message = TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyDeleted, filesDeleted.Count);
                    }
                    else
                    {
                        MessageBox.Show(TranslationSource.Format(Resources.MessageBox_CountFilesHaveBeenSuccessfullyDeleted, filesDeleted.Count),
                            Resources.MessageBox_DnGrep + " " + Resources.MessageBox_DeleteFiles,
                            MessageBoxButton.OK, MessageBoxImage.Information,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    logger.Error(ex, "Error deleting files");
                    if (isScriptRunning)
                    {
                        message = Resources.Scripts_DeleteFilesFailed + ex.Message;
                    }
                    else
                    {
                        MessageBox.Show(Resources.MessageBox_ThereWasAnErrorDeletingFiles + App.LogDir,
                            Resources.MessageBox_DnGrep,
                            MessageBoxButton.OK, MessageBoxImage.Error,
                            MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                }
            }
            return (success, filesDeleted, message);
        }


    }
}
