using System;
using System.Collections.Generic;
using System.IO;

namespace dnGREP.Everything
{
    public sealed class EverythingSearch
    {
        private EverythingSearch()
        {
            // cannot construct...
        }

        // #TODO: does Everything support longer paths?
        private const int maxPath = 260;

        private static bool? isAvailable;

        public static bool IsAvailable
        {
            get
            {
                if (!isAvailable.HasValue)
                {
                    try
                    {
                        UInt32 major = NativeMethods.Everything_GetMajorVersion();
                        UInt32 minor = NativeMethods.Everything_GetMinorVersion();
                        UInt32 revision = NativeMethods.Everything_GetRevision();

                        // we need version 1.4.1 or higher
                        if (major < 1)
                            isAvailable = false;
                        else if (major > 1)
                            isAvailable = true;
                        else
                        {
                            if (minor < 4)
                                isAvailable = false;
                            else if (minor > 4)
                                isAvailable = true;
                            else
                                isAvailable = revision >= 1;
                        }
                    }
                    catch (Exception)
                    {
                        isAvailable = false;
                    }
                }
                return isAvailable.Value;
            }
        }

        public static bool IsDbLoaded
        {
            get
            {
                return IsAvailable ? NativeMethods.Everything_IsDBLoaded() : false;
            }
        }

        //public static string GetPathPart(string searchText)
        //{
        //    string path = string.Empty;
        //    if (searchText.StartsWith("\""))
        //    {
        //        int endIndex = searchText.IndexOf('\"', 1);
        //        if (endIndex > -1)
        //        {
        //            endIndex++;

        //            path = searchText.Substring(0, endIndex).Trim();
        //        }
        //    }
        //    else
        //    {
        //        int endIndex = searchText.IndexOf(' ');
        //        if (endIndex > -1)
        //        {
        //            path = searchText.Substring(0, endIndex).Trim();
        //        }
        //    }

        //    if (!string.IsNullOrWhiteSpace(path))
        //    {

        //    }
        //}

        public static IEnumerable<EverythingFileInfo> FindFiles(string searchString)
        {


            NativeMethods.Everything_SetSort((UInt32)SortType.NameAscending);

            NativeMethods.Everything_SetSearchW(searchString);

            NativeMethods.Everything_SetRequestFlags((UInt32)(
                RequestFlags.FullPathAndFileName |
                RequestFlags.Attributes |
                RequestFlags.Size |
                RequestFlags.DateCreated |
                RequestFlags.DateModified));

            NativeMethods.Everything_QueryW(true);

            UInt32 count = NativeMethods.Everything_GetNumResults();
            for (UInt32 idx = 0; idx < count; idx++)
            {
                string fullName = NativeMethods.Everything_GetResultFullPathName(idx, maxPath);

                FileAttributes attr = (FileAttributes)NativeMethods.Everything_GetResultAttributes(idx);

                if (!attr.HasFlag(FileAttributes.Directory))
                {
                    long length = NativeMethods.Everything_GetResultSize(idx);

                    DateTime createdTime = NativeMethods.Everything_GetResultDateCreated(idx);

                    DateTime lastWriteTime = NativeMethods.Everything_GetResultDateModified(idx);

                    EverythingFileInfo fileInfo = new EverythingFileInfo(fullName, attr, length, createdTime, lastWriteTime);

                    yield return fileInfo;
                }
            }
        }
    }
}
