using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using dnGREP.Localization;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Shell;
using Resources = dnGREP.Localization.Properties.Resources;

namespace dnGREP.Common
{
    public static class NativeMethods
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Converts a numeric value into a string that represents the number expressed as a size value in bytes, kilobytes, megabytes, or gigabytes, depending on the size.
        /// </summary>
        /// <param name="filelength">The numeric value to be converted.</param>
        /// <returns>the converted string</returns>
        unsafe public static string StrFormatByteSize(long filesize)
        {
            string text = new(' ', 11);
            fixed (char* pstr = text)
            {
                PInvoke.StrFormatByteSize(filesize, new PWSTR(pstr), 11);
            }
            text = text.Replace('\0', ' ').TrimEnd();
            return text;
        }

        unsafe public static string GetFileTypeDescription(string fileNameOrExtension)
        {
            var fileInfoSize = Marshal.SizeOf<SHFILEINFOW>();
            var fileInfoPtr = Marshal.AllocHGlobal(fileInfoSize); // Allocate unmanaged memory
            try
            {
                PInvoke.SHGetFileInfo(fileNameOrExtension, FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                    (SHFILEINFOW*)fileInfoPtr,
                    (uint)fileInfoSize,
                    SHGFI_FLAGS.SHGFI_TYPENAME | SHGFI_FLAGS.SHGFI_USEFILEATTRIBUTES);

                if (Marshal.PtrToStructure(fileInfoPtr, typeof(SHFILEINFOW)) is SHFILEINFOW fileInfo)
                {
                    return new(fileInfo.szTypeName.Value);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"error while trying to get type name for {fileNameOrExtension}: {e.Message}");
            }
            finally
            {
                Marshal.FreeHGlobal(fileInfoPtr);
            }
            return string.Empty;
        }

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

        unsafe private static Process? ProcessHoldingClipboard()
        {
            Process? process = null;

            HWND hwnd = PInvoke.GetOpenClipboardWindow();

            if (hwnd != IntPtr.Zero)
            {
                uint processId = 0;
                _ = PInvoke.GetWindowThreadProcessId(hwnd, &processId);

                Process[] procs = Process.GetProcesses();
                foreach (Process proc in procs)
                {
                    IntPtr handle = proc.MainWindowHandle;

                    if (handle == hwnd.Value)
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
