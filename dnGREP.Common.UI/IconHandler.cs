using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace dnGREP.Common.UI
{
    public enum IconSize
    {
        Large, //32x32
        Small  //16x16        
    }

    public class IconHandler
    {
        unsafe public static Bitmap? IconFromExtensionShell(string extension, IconSize size)
        {
            //add '.' if necessary
            if (extension[0] != '.') extension = '.' + extension;

            var fileInfoSize = Marshal.SizeOf<SHFILEINFOW>();
            var fileInfoPtr = Marshal.AllocHGlobal(fileInfoSize); // Allocate unmanaged memory
            try
            {
                PInvoke.SHGetFileInfo(extension, 0, (SHFILEINFOW*)fileInfoPtr,
                    (uint)fileInfoSize,
                    SHGFI_FLAGS.SHGFI_ICON | SHGFI_FLAGS.SHGFI_USEFILEATTRIBUTES |
                    (size == IconSize.Large ? SHGFI_FLAGS.SHGFI_LARGEICON : SHGFI_FLAGS.SHGFI_SMALLICON));

                if (Marshal.PtrToStructure(fileInfoPtr, typeof(SHFILEINFOW)) is SHFILEINFOW fileInfo)
                {
                    return GetManagedIcon(fileInfo.hIcon)?.ToBitmap();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"error while trying to get icon for {extension}: {e.Message}");
            }
            finally
            {
                Marshal.FreeHGlobal(fileInfoPtr);
            }
            return null;
        }

        private static Icon? GetManagedIcon(HICON hIcon)
        {
            try
            {
                Icon clone = (Icon)Icon.FromHandle(hIcon.Value).Clone();

                PInvoke.DestroyIcon(hIcon);

                return clone;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error while trying to get managed icon: {ex.Message}");
            }
            return null;
        }
    }
}
