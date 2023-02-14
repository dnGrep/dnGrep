using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NLog;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Common.UI
{
    public class FileIcons
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void StoreIcon(string extension, string path)
        {
            StoreIcon(extension, path, GetMimeType(Path.GetExtension(path)));
        }

        public static void StoreIcon(string extension, string path, string mimeType)
        {
            if (!File.Exists(path))
            {
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    Bitmap image = IconHandler.IconFromExtension(extension, IconSize.Small);

                    Encoder qualityEncoder = Encoder.Quality;
                    long quality = 100;
                    EncoderParameter ratio = new(qualityEncoder, quality);
                    EncoderParameters codecParams = new(1);
                    codecParams.Param[0] = ratio;
                    ImageCodecInfo mimeCodecInfo = null;
                    foreach (ImageCodecInfo codecInfo in ImageCodecInfo.GetImageEncoders())
                    {
                        if (codecInfo.MimeType == mimeType)
                        {
                            mimeCodecInfo = codecInfo;
                            break;
                        }
                    }
                    if (mimeCodecInfo != null)
                        image.Save(path, mimeCodecInfo, codecParams); // Save to JPG
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to create icon");
                }
            }
        }

        private static string GetMimeType(string sExtension)
        {
            string extension = sExtension.ToLower();
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("MIME\\Database\\Content Type");
            foreach (string keyName in key.GetSubKeyNames())
            {
                RegistryKey temp = key.OpenSubKey(keyName);
                if (extension.Equals(temp.GetValue("Extension")))
                {
                    return keyName;
                }
            }
            return "";
        }
    }

    public enum IconSize
    {
        Large, //32x32
        Small  //16x16        
    }

    public class IconHandler
    {
        //will return an array of icons 
        unsafe public static Icon[] IconsFromFile(string filename, IconSize size)
        {
            //checks how many icons.
            uint iconCount = 0;
            fixed (char* pszPathLocal = filename)
            {
                iconCount = PInvoke.ExtractIconEx(pszPathLocal, -1, nIcons: 0);
            }

            //extracts the icons by the size that was selected.
            HICON[] iconPtr = new HICON[iconCount];
            fixed (char* pszPathLocal = filename)
            fixed (HICON* phIcon = iconPtr)
            {
                if (size == IconSize.Small)
                {
                    _ = PInvoke.ExtractIconEx(pszPathLocal, 0, phiconSmall: phIcon, nIcons: iconCount);
                }
                else
                {
                    _ = PInvoke.ExtractIconEx(pszPathLocal, 0, phiconLarge: phIcon, nIcons: iconCount);
                }
            }

            Icon[] iconList = new Icon[iconCount];

            //gets the icons in a list.
            for (int i = 0; i < iconCount; i++)
            {
                iconList[i] = GetManagedIcon(iconPtr[i]);
            }

            return iconList;
        }

        //extract one selected by index icon from a file.
        unsafe public static Icon IconFromFile(string filename, IconSize size, int index)
        {
            //checks how many icons.
            uint iconCount = 0;
            fixed (char* pszPathLocal = filename)
            {
                iconCount = PInvoke.ExtractIconEx(pszPathLocal, -1, nIcons: 0);
            }
            if (iconCount < index) return null; // no icons was found.

            //extracts the icon that we want in the selected size.
            HICON[] iconArray = new HICON[1];
            fixed (char* pathLocal = filename)
            fixed (HICON* phIcon = iconArray)
            {
                if (size == IconSize.Small)
                {
                    _ = PInvoke.ExtractIconEx(pathLocal, index, phiconSmall: phIcon, nIcons: 1);
                }
                else
                {
                    _ = PInvoke.ExtractIconEx(pathLocal, index, phiconLarge: phIcon, nIcons: 1);
                }
            }

            return GetManagedIcon(iconArray[0]);
        }

        //this will look throw the registry to find if the Extension have an icon.
        unsafe public static Bitmap IconFromExtension(string extension, IconSize size)
        {
            try
            {
                //add '.' if necessary
                if (extension[0] != '.') extension = '.' + extension;

                //opens the registry for the wanted key.
                RegistryKey root = Registry.ClassesRoot;
                RegistryKey extensionKey = root.OpenSubKey(extension);
                extensionKey.GetValueNames();
                RegistryKey applicationKey = root.OpenSubKey(extensionKey.GetValue("").ToString());

                //gets the name of the file that have the icon.
                string iconLocation =
                  applicationKey.OpenSubKey("DefaultIcon").GetValue("").ToString();
                string[] iconPath = iconLocation.Split(',');

                int index = iconPath[1] == null ? 0 : Convert.ToInt32(iconPath[1]);

                //extracts the icon from the file.
                HICON[] iconArray = new HICON[1];
                fixed (char* pathLocal = iconPath[0])
                fixed (HICON* phIcon = iconArray)
                {
                    if (size == IconSize.Small)
                    {
                        _ = PInvoke.ExtractIconEx(pathLocal, index, phiconSmall: phIcon, nIcons: 1);
                    }
                    else
                    {
                        _ = PInvoke.ExtractIconEx(pathLocal, index, phiconLarge: phIcon, nIcons: 1);
                    }
                }

                return GetManagedIcon(iconArray[0]).ToBitmap();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("error while" +
                       " trying to get icon for " +
                       extension + " :" + e.Message);
                return null;
            }
        }

        unsafe public static Bitmap IconFromExtensionShell(string extension, IconSize size)
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
                    return GetManagedIcon(fileInfo.hIcon).ToBitmap();
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

        public static Icon IconFromResource(string resourceName)
        {
            Assembly assembly = Assembly.GetCallingAssembly();

            return new Icon(assembly.GetManifestResourceStream(resourceName));
        }

        public static void SaveIconFromImage(Image sourceImage,
               string iconFilename, IconSize newIconSize)
        {
            Size size = newIconSize == IconSize.Large ? new(32, 32) : new(16, 16);
            using Bitmap rawImage = new(sourceImage, size);
            using Icon tempIcon = Icon.FromHandle(rawImage.GetHicon());
            using FileStream newIconStream = File.Open(iconFilename, FileMode.Create);
            tempIcon.Save(newIconStream);
        }

        private static Icon GetManagedIcon(HICON hIcon)
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
