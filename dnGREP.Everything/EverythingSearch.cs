using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using NLog;

namespace dnGREP.Everything
{
    public class EverythingSearch : IEverythingSearch
    {
        private const int maxPath = 1024;

        private bool? isAvailable;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool IsAvailable
        {
            get
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    // there is no Everything SDK dll for Arm64
                    return false;
                }

                if (!isAvailable.HasValue)
                {
                    try
                    {
                        isAvailable = false;

                        // Check if the Everything DLL is available in the same directory as the executable.
                        // It is not included with dnGrep, it must be installed separately by the user.
                        var dllFile = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                        NativeMethods.EverythingDLL);

                        if (File.Exists(dllFile))
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
                    }
                    catch (EverythingException ex)
                    {
                        logger.Error(ex, "Everything SDK error while checking availability");
                        isAvailable = false;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to check Everything availability");
                        isAvailable = false;
                    }
                }
                return isAvailable.Value;
            }
        }

        public int CountMissingFiles { get; private set; }

        public List<EverythingFileInfo> FindFiles(string searchString, bool includeHidden)
        {
            List<EverythingFileInfo> results = [];

            if (isAvailable == null || isAvailable == false)
                return results;

            if (!NativeMethods.Everything_IsDBLoaded())
                return results;

            List<string> invalidDrives = [];
            CountMissingFiles = 0;

            try
            {
                NativeMethods.SetSort((uint)SortType.NameAscending);

                NativeMethods.SetSearch(searchString);

                NativeMethods.SetRequestFlags((uint)(
                    RequestFlags.FullPathAndFileName |
                    RequestFlags.Attributes |
                    RequestFlags.Size |
                    RequestFlags.DateCreated |
                    RequestFlags.DateModified));

                NativeMethods.QueryOrThrow(true);

                uint count = NativeMethods.Everything_GetNumResults();
                for (uint idx = 0; idx < count; idx++)
                {
                    try
                    {
                        string fullName = NativeMethods.Everything_GetResultFullPathName(idx, maxPath);
                        if (string.IsNullOrEmpty(fullName))
                            continue;

                        FileAttributes attr = (FileAttributes)NativeMethods.Everything_GetResultAttributes(idx);

                        if (attr.HasFlag(FileAttributes.Directory))
                            continue;

                        long length = NativeMethods.Everything_GetResultSize(idx);

                        DateTime createdTime = NativeMethods.Everything_GetResultDateCreated(idx);

                        DateTime lastWriteTime = NativeMethods.Everything_GetResultDateModified(idx);

                        EverythingFileInfo fileInfo = new(fullName, attr, length, createdTime, lastWriteTime);

                        if (!includeHidden && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                            continue;

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
                            results.Add(fileInfo);
                        }
                        else
                        {
                            CountMissingFiles++;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        // invalid path characters, bad FILETIME values, etc.
                        logger.Debug(ex, "Skipping result at index {0}", idx);
                        CountMissingFiles++;
                    }
                }
            }
            catch (EverythingException ex)
            {
                logger.Error(ex, "Everything SDK error in FindFiles");
                if (ex.ErrorCode == NativeMethods.EVERYTHING_ERROR_IPC)
                    isAvailable = false;
            }
            catch (DllNotFoundException ex)
            {
                logger.Error(ex, "Everything SDK DLL not found");
                isAvailable = false;
            }
            catch (EntryPointNotFoundException ex)
            {
                logger.Error(ex, "Everything SDK function not found - DLL version mismatch");
                isAvailable = false;
            }
            catch (SEHException ex)
            {
                logger.Error(ex, "Native exception in Everything SDK");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unexpected error in Everything FindFiles");
            }
            finally
            {
                NativeMethods.Everything_Reset();
            }

            return results;
        }

        public string RemovePrefixes(string text)
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
