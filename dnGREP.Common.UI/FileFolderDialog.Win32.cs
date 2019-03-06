using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Common.UI
{
    public class FileFolderDialogWin32 : CommonDialog
    {
        private OpenFileDialog dialog = new OpenFileDialog();

        public OpenFileDialog Dialog
        {
            get { return dialog; }
            set { dialog = value; }
        }

        public override bool? ShowDialog()
        {
            return this.ShowDialog(null);
        }

        public new bool? ShowDialog(System.Windows.Window owner)
        {
            // Set validate names to false otherwise windows will not let you select "Folder Selection."
            dialog.ValidateNames = false;
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;

            try
            {
                // Set initial directory (used when dialog.FileName is set from outside)
                if (dialog.FileName != null && dialog.FileName != "")
                {
                    if (Directory.Exists(dialog.FileName))
                        dialog.InitialDirectory = dialog.FileName;
                    else
                        dialog.InitialDirectory = Utils.GetBaseFolder(dialog.FileName);
                }
            }
            catch
            {
                // Do nothing
            }

            // Always default to Folder Selection.
            dialog.FileName = "Folder Selection.";

            if (owner == null)
                return dialog.ShowDialog();
            else
                return dialog.ShowDialog(owner);
        }

        /// <summary>
        // Helper property. Parses FilePath into either folder path (if Folder Selection. is set)
        // or returns file path
        /// </summary>
        public string SelectedPath
        {
            get
            {
                try
                {
                    if (dialog.FileName != null &&
                        (dialog.FileName.EndsWith("Folder Selection.") || !File.Exists(dialog.FileName)) &&
                        !Directory.Exists(dialog.FileName))
                    {
                        return Path.GetDirectoryName(dialog.FileName);
                    }
                    else
                    {
                        return dialog.FileName;
                    }
                }
                catch
                {
                    return dialog.FileName;
                }
            }
            set
            {
                if (value != null && value != "")
                {
                    dialog.FileName = value;
                }
            }
        }

        /// <summary>
        /// When multiple files are selected returns them as semi-colon separated string
        /// </summary>
        public string SelectedPaths
        {
            get
            {
                if (dialog.FileNames != null && dialog.FileNames.Length > 1)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string fileName in dialog.FileNames)
                    {
                        try
                        {
                            if (File.Exists(fileName))
                                sb.Append(fileName + ";");
                        }
                        catch
                        {
                            // Go to next
                        }
                    }
                    return sb.ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        public override void Reset()
        {
            dialog.Reset();
        }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            return true;
        }
    }
}
