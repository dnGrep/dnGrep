using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NLog;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Common.UI
{
    public class FileIcons
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

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
                    EncoderParameter ratio = new EncoderParameter(qualityEncoder, quality);
                    EncoderParameters codecParams = new EncoderParameters(1);
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
                    logger.Log<Exception>(LogLevel.Error, "Failed to create icon", ex);
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

    struct SHFILEINFO
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    public enum IconSize : uint
    {
        Large = 0x0,  //32x32
        Small = 0x1 //16x16        
    }



    //the function that will extract the icons from a file
    public class IconHandler
    {
        const uint SHGFI_ICON = 0x100;
        const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        [DllImport("Shell32", CharSet = CharSet.Auto)]
        internal extern static int ExtractIconEx(
            [MarshalAs(UnmanagedType.LPTStr)]
            string lpszFile,      //size of the icon
            int nIconIndex,       //index of the icon
                                  // (in case we have more
                                  // then 1 icon in the file
            IntPtr[] phIconLarge, //32x32 icon
            IntPtr[] phIconSmall, //16x16 icon
            int nIcons);          //how many to get

        [DllImport("shell32.dll")]
        static extern IntPtr SHGetFileInfo(
            string pszPath,            //path
            uint dwFileAttributes,    //attributes
            ref SHFILEINFO psfi,    //struct pointer
            uint cbSizeFileInfo,    //size
            uint uFlags);    //flags

        [DllImport("User32.dll")]
        private static extern int
                DestroyIcon(System.IntPtr hIcon);
        // free up the icon pointers.

        //will return an array of icons 
        public static Icon[] IconsFromFile(string Filename, IconSize Size)
        {
            int IconCount = ExtractIconEx(Filename, -1,
                            null, null, 0); //checks how many icons.
            IntPtr[] IconPtr = new IntPtr[IconCount];

            //extracts the icons by the size that was selected.
            if (Size == IconSize.Small)
                ExtractIconEx(Filename, 0, null, IconPtr, IconCount);
            else
                ExtractIconEx(Filename, 0, IconPtr, null, IconCount);

            Icon[] IconList = new Icon[IconCount];

            //gets the icons in a list.
            for (int i = 0; i < IconCount; i++)
            {
                IconList[i] = (Icon)Icon.FromHandle(IconPtr[i]).Clone();
                DestroyIcon(IconPtr[i]);
            }

            return IconList;
        }

        //extract one selected by index icon from a file.
        public static Icon IconFromFile(string Filename, IconSize Size, int Index)
        {
            int IconCount = ExtractIconEx(Filename, -1,
                            null, null, 0); //checks how many icons.
            if (IconCount < Index) return null; // no icons was found.

            IntPtr[] IconPtr = new IntPtr[1];

            //extracts the icon that we want in the selected size.
            if (Size == IconSize.Small)
                ExtractIconEx(Filename, Index, null, IconPtr, 1);
            else
                ExtractIconEx(Filename, Index, IconPtr, null, 1);

            return GetManagedIcon(IconPtr[0]);
        }

        //this will look throw the registry to find if the Extension have an icon.
        public static Bitmap IconFromExtension(string Extension, IconSize Size)
        {
            try
            {
                //add '.' if nessesry
                if (Extension[0] != '.') Extension = '.' + Extension;

                //opens the registry for the wanted key.
                RegistryKey Root = Registry.ClassesRoot;
                RegistryKey ExtensionKey = Root.OpenSubKey(Extension);
                ExtensionKey.GetValueNames();
                RegistryKey ApplicationKey =
                  Root.OpenSubKey(ExtensionKey.GetValue("").ToString());

                //gets the name of the file that have the icon.
                string IconLocation =
                  ApplicationKey.OpenSubKey("DefaultIcon").GetValue("").ToString();
                string[] IconPath = IconLocation.Split(',');

                if (IconPath[1] == null) IconPath[1] = "0";
                IntPtr[] Large = new IntPtr[1], Small = new IntPtr[1];

                //extracts the icon from the file.
                ExtractIconEx(IconPath[0],
                  Convert.ToInt16(IconPath[1]), Large, Small, 1);

                return GetManagedIcon(Size == IconSize.Large ? Large[0] : Small[0]).ToBitmap();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("error while" +
                       " trying to get icon for " +
                       Extension + " :" + e.Message);
                return null;
            }
        }
        public static Bitmap IconFromExtensionShell(string Extension, IconSize Size)
        {
            try
            {
                //add '.' if nessesry
                if (Extension[0] != '.') Extension = '.' + Extension;

                //temp struct for getting file shell info
                SHFILEINFO TempFileInfo = new SHFILEINFO();

                SHGetFileInfo(
                    Extension,
                    0,
                    ref TempFileInfo,
                    (uint)Marshal.SizeOf(TempFileInfo),
                    SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | (uint)Size);

                return GetManagedIcon(TempFileInfo.hIcon).ToBitmap();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("error while" +
                  " trying to get icon for " + Extension +
                  " :" + e.Message);
                return null;
            }
        }
        public static Icon IconFromResource(string resourceName)
        {
            Assembly assembly = Assembly.GetCallingAssembly();

            return new Icon(assembly.GetManifestResourceStream(resourceName));
        }

        public static void SaveIconFromImage(Image sourceImage,
               string iconFilename, IconSize destenationIconSize)
        {
            Size newIconSize = destenationIconSize ==
                 IconSize.Large ? new Size(32, 32) : new Size(16, 16);

            using (Bitmap rawImage = new Bitmap(sourceImage, newIconSize))
            {
                using (Icon tempIcon = Icon.FromHandle(rawImage.GetHicon()))
                {
                    using (FileStream newIconStream = File.Open(iconFilename, FileMode.Create))
                    {
                        tempIcon.Save(newIconStream);
                    }
                }
            }
        }

        private static Icon GetManagedIcon(IntPtr hIcon)
        {
            Icon clone = (Icon)Icon.FromHandle(hIcon).Clone();

            DestroyIcon(hIcon);

            return clone;
        }
    }
}
