using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using dnGREP.Localization;
using NLog;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Common
{
    public static class NativeMethods
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [DllImport("user32.dll")]
        static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern long StrFormatByteSize(long fileSize, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer, int bufferSize);

        /// <summary>
        /// Converts a numeric value into a string that represents the number expressed as a size value in bytes, kilobytes, megabytes, or gigabytes, depending on the size.
        /// </summary>
        /// <param name="filelength">The numeric value to be converted.</param>
        /// <returns>the converted string</returns>
        public static string StrFormatByteSize(long filesize)
        {
            StringBuilder sb = new StringBuilder(11);
            StrFormatByteSize(filesize, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string GetFileTypeDescription(string fileNameOrExtension)
        {
            SHFILEINFO shfi;
            if (IntPtr.Zero != SHGetFileInfo(
                                fileNameOrExtension,
                                FILE_ATTRIBUTE_NORMAL,
                                out shfi,
                                (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                                SHGFI_USEFILEATTRIBUTES | SHGFI_TYPENAME))
            {
                return shfi.szTypeName;
            }
            return null;
        }

        [DllImport("shell32")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        private const uint SHGFI_TYPENAME = 0x000000400;     // get type name
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;     // use passed dwFileAttribute


        /// <summary>
        /// Sets the clipboard text and suppresses the CLIPBRD_E_CANT_OPEN error
        /// </summary>
        /// <remarks>
        /// Applications receiving clipboard notifications can lock the clipboard,  causing
        /// Clipboard.SetText to fail. 
        /// 
        /// The WPF implementation (link below) already calls the clipboard with delays
        /// and retries. Testing has shown that calling SetText in a retry loop won't help, it only 
        /// makes the failure slower.
        /// 
        /// The SetText method does two clipboard operations: first to set the data object and 
        /// second to flush the data so it is remains on the clipboard after the application exits.
        /// Testing has shown that setting the data succeeds, which raises a notification, the bad actor
        /// locks the clipboard, and the call to flush fails with CLIPBRD_E_CANT_OPEN.
        /// 
        /// The flush is nice, but not really a necessary feature.
        ///
        /// In contrast, Clipboard.SetDataObject(text) does not do the flush, and won't have
        /// to wait through the retry loops.  So if SetText fails, fall back to SetDataObject.
        /// 
        /// https://referencesource.microsoft.com/#PresentationCore/Core/CSharp/System/Windows/Clipboard.cs,8b9b56e883ff64c7
        /// </remarks>
        /// <param name="text"></param>
        public static void SetClipboardText(string text)
        {
            const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;

            try
            {
                if (useClipboardSetDataObject)
                    System.Windows.Clipboard.SetDataObject(text);
                else
                    System.Windows.Clipboard.SetText(text);
                return;
            }
            catch (COMException ex)
            {
                if ((uint)ex.ErrorCode == CLIPBRD_E_CANT_OPEN)
                {
                    useClipboardSetDataObject = true;

                    var process = ProcessHoldingClipboard();
                    if (process != null)
                    {
                        string msg = Resources.MessageBox_ErrorSettingClipboardTextTheClipboardIsLockedBy + Environment.NewLine;
                        msg += (process.MainModule != null && !string.IsNullOrEmpty(process.MainModule.FileName) ?
                            process.MainModule.FileName : process.ProcessName) + Environment.NewLine +
                            TranslationSource.Format(Resources.MessageBox_WindowTitleIsName, process.MainWindowTitle);
                        logger.Error(msg);
                        System.Windows.MessageBox.Show(msg, Resources.MessageBox_DnGrep,
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                            System.Windows.MessageBoxResult.OK, TranslationSource.Instance.FlowDirection);
                    }
                }
                else
                {
                    throw;
                }
            }
        }
        private static bool useClipboardSetDataObject;

        private static Process ProcessHoldingClipboard()
        {
            Process process = null;

            IntPtr hwnd = GetOpenClipboardWindow();

            if (hwnd != IntPtr.Zero)
            {
                _ = GetWindowThreadProcessId(hwnd, out uint processId);

                Process[] procs = Process.GetProcesses();
                foreach (Process proc in procs)
                {
                    IntPtr handle = proc.MainWindowHandle;

                    if (handle == hwnd)
                    {
                        process = proc;
                        break;
                    }
                    else if (processId == proc.Id)
                    {
                        process = proc;
                        break;
                    }
                }
            }

            return process;
        }
    }
}
