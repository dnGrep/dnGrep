using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace dnGREP.Common.UI
{
    public class FileFolderDialogWin32 : CommonDialog
    {
        private OpenFileDialog dialog = new();

        public OpenFileDialog Dialog
        {
            get { return dialog; }
            set { dialog = value; }
        }

        public override bool? ShowDialog()
        {
            return ShowDialog(null);
        }

        public new bool? ShowDialog(System.Windows.Window? owner)
        {
            // Set validate names to false otherwise windows will not let you select "Folder Selection."
            dialog.ValidateNames = false;
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;

            try
            {
                // Set initial directory (used when dialog.FileName is set from outside)
                if (!string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    if (Directory.Exists(dialog.FileName))
                        dialog.InitialDirectory = dialog.FileName;
                    else
                        dialog.InitialDirectory = UiUtils.GetBaseFolder("\"" + dialog.FileName + "\"");
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
                        (dialog.FileName.EndsWith("Folder Selection.", StringComparison.Ordinal) || !File.Exists(dialog.FileName)) &&
                        !Directory.Exists(dialog.FileName))
                    {
                        return UiUtils.QuoteIfNeeded(Path.GetDirectoryName(dialog.FileName) ?? string.Empty);
                    }
                    else
                    {
                        return UiUtils.QuoteIfNeeded(dialog.FileName ?? string.Empty);
                    }
                }
                catch
                {
                    return UiUtils.QuoteIfNeeded(dialog.FileName);
                }
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    dialog.FileName = value;
                }
            }
        }

        public bool HasMultiSelectedFiles
        {
            get { return dialog.FileNames != null && dialog.FileNames.Length > 1; }
        }

        /// <summary>
        /// When multiple files are selected returns them as separated string with the specified separator
        /// </summary>
        /// <param name="separator"></param>
        public string? GetSelectedPaths(string separator)
        {
            if (dialog.FileNames != null && dialog.FileNames.Length > 1)
            {
                StringBuilder sb = new();
                foreach (string fileName in dialog.FileNames)
                {
                    try
                    {
                        if (File.Exists(fileName))
                            sb.Append(UiUtils.QuoteIfNeeded(fileName) + separator);
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
