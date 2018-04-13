using System;
using System.Collections.Generic;
using System.IO;
using NLog;

namespace dnGREP.Everything
{
    public sealed class EverythingSearch
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

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

        public static void Initialize()
        {
            NativeMethods.Everything_RebuildDB();
        }

        public static bool IsDbLoaded
        {
            get
            {
                return IsAvailable ? NativeMethods.Everything_IsDBLoaded() : false;
            }
        }

        public static IEnumerable<EverythingFileInfo> FindFiles(string searchString)
        {
            if (!IsDbLoaded)
                yield break;

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

        public static bool HasPath(string searchText)
        {
            return !string.IsNullOrWhiteSpace(GetBaseFolder(searchText));
        }

        public static string GetBaseFolder(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return string.Empty;

            try
            {
                string path = searchText.Trim();

                if (Directory.Exists(path))
                    return path;

                path = RemovePrefixes(path);

                int pos = -1;
                if (path.StartsWith("\""))
                {
                    pos = path.IndexOf('"', 1);
                    if (pos > -1)
                        path = path.Substring(1, pos - 1).Trim();

                    path = RemovePrefixes(path);
                }
                else
                {
                    pos = path.IndexOf(' ');
                    if (pos > -1)
                        path = path.Substring(0, pos).Trim();
                }

                // Check for paths OR'd together
                pos = path.IndexOf('|');
                if (pos > -1)
                {
                    path = path.Substring(0, pos);
                }

                if (!string.IsNullOrWhiteSpace(path))
                {
                    if (Directory.Exists(path) && Path.IsPathRooted(path))
                        return path;

                    if (File.Exists(path))
                        return Path.GetDirectoryName(path);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in EverythingSearch GetBaseFolder");
            }

            return string.Empty;
        }

        public static string GetFilePattern(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return string.Empty;

            try
            {
                string path = searchText.Trim();
                string filePattern = string.Empty;

                if (Directory.Exists(path))
                    return string.Empty;

                int pos = path.LastIndexOf('"');
                if (pos > -1)
                {
                    filePattern = path.Substring(pos + 1).Trim();
                }
                else
                {
                    pos = path.IndexOf(' ');
                    if (pos > -1)
                        filePattern = path.Substring(pos + 1).Trim();
                }

                return filePattern;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in EverythingSearch GetFilePattern");
            }

            return string.Empty;
        }

        private static string RemovePrefixes(string text)
        {
            foreach (string prefix in EverythingKeywords.PathPrefixes)
            {
                if (text.StartsWith(prefix))
                    text = text.Remove(0, prefix.Length);
            }
            return text.Trim();
        }
    }
}
