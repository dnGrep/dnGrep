using System;
using System.Collections.Generic;
using System.IO;

namespace dnGREP.Everything
{
    public static class EverythingSearch
    {
        private const int maxPath = 32768;

        private static bool? isAvailable;

        public static bool IsAvailable
        {
            get
            {
                if (!isAvailable.HasValue)
                {
                    try
                    {
                        uint major = NativeMethods.Everything_GetMajorVersion();
                        uint minor = NativeMethods.Everything_GetMinorVersion();
                        uint revision = NativeMethods.Everything_GetRevision();

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

        public static bool IsDbLoaded => IsAvailable && NativeMethods.Everything_IsDBLoaded();

        public static int CountMissingFiles { get; private set; }

        public static IEnumerable<EverythingFileInfo> FindFiles(string searchString, bool includeHidden)
        {
            if (!IsDbLoaded)
                yield break;

            List<string> invalidDrives = new();
            CountMissingFiles = 0;

            NativeMethods.Everything_SetSort((uint)SortType.NameAscending);

            NativeMethods.Everything_SetSearchW(searchString);

            NativeMethods.Everything_SetRequestFlags((uint)(
                RequestFlags.FullPathAndFileName |
                RequestFlags.Attributes |
                RequestFlags.Size |
                RequestFlags.DateCreated |
                RequestFlags.DateModified));

            NativeMethods.Everything_QueryW(true);

            uint count = NativeMethods.Everything_GetNumResults();
            for (uint idx = 0; idx < count; idx++)
            {
                string fullName = NativeMethods.Everything_GetResultFullPathName(idx, maxPath);

                FileAttributes attr = (FileAttributes)NativeMethods.Everything_GetResultAttributes(idx);

                if (!attr.HasFlag(FileAttributes.Directory))
                {
                    long length = NativeMethods.Everything_GetResultSize(idx);

                    DateTime createdTime = NativeMethods.Everything_GetResultDateCreated(idx);

                    DateTime lastWriteTime = NativeMethods.Everything_GetResultDateModified(idx);

                    EverythingFileInfo fileInfo = new(fullName, attr, length, createdTime, lastWriteTime);

                    if (!includeHidden && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }

                    var root = Directory.GetDirectoryRoot(fullName);
                    if (invalidDrives.Contains(root))
                    {
                        CountMissingFiles++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(Path.GetPathRoot(root)))
                    {
                        CountMissingFiles++;
                        invalidDrives.Add(root);
                        continue;
                    }

                    if (File.Exists(fullName))
                    {
                        yield return fileInfo;
                    }
                    else
                    {
                        CountMissingFiles++;
                    }
                }
            }
        }

        public static string RemovePrefixes(string text)
        {
            foreach (string prefix in EverythingKeywords.PathPrefixes)
            {
                if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    text = text.Remove(0, prefix.Length);
            }
            return text.Trim();
        }
    }
}
