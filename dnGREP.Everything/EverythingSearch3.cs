using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using NLog;

namespace dnGREP.Everything
{
    public class EverythingSearch3 : IEverythingSearch
    {
        private const int maxPath = 1024;
        private const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private bool? isAvailable;

        public bool IsAvailable
        {
            get
            {
                if (!isAvailable.HasValue)
                {
                    try
                    {
                        isAvailable = false;

                        // Check if the Everything DLL is available in the same directory as the executable.
                        // It is not included with dnGrep, it must be installed separately by the user.
                        var dllFile = Path.Combine(
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                            NativeMethods3.EverythingDLL);

                        if (File.Exists(dllFile))
                        {
                            IntPtr client = NativeMethods3.ConnectOrThrow("1.5a");
                            try
                            {
                                uint major = NativeMethods3.Everything3_GetMajorVersion(client);
                                uint minor = NativeMethods3.Everything3_GetMinorVersion(client);
                                uint revision = NativeMethods3.Everything3_GetRevision(client);

                                // we need version 1.5.0 or higher
                                if (major < 1)
                                    isAvailable = false;
                                else if (major > 1)
                                    isAvailable = true;
                                else
                                {
                                    if (minor < 5)
                                        isAvailable = false;
                                    else
                                        isAvailable = true;
                                }
                            }
                            finally
                            {
                                // Disconnect and free the client.
                                NativeMethods3.Everything3_ShutdownClient(client);
                                NativeMethods3.Everything3_DestroyClient(client);
                            }
                        }
                    }
                    catch (EverythingException ex)
                    {
                        logger.Error(ex, "Everything3 SDK error while checking availability");
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

            CountMissingFiles = 0;
            List<string> invalidDrives = [];

            IntPtr client = IntPtr.Zero;
            IntPtr state = IntPtr.Zero;
            IntPtr result = IntPtr.Zero;

            try
            {
                client = NativeMethods3.ConnectOrThrow("1.5a");

                if (!NativeMethods3.Everything3_IsDBLoaded(client))
                {
                    logger.Warn("Everything database is not loaded");
                    return results;
                }

                state = NativeMethods3.CreateSearchStateOrThrow();

                NativeMethods3.AddSearchSortOrThrow(state, NativeMethods3.EVERYTHING3_PROPERTY_ID_NAME, true);
                NativeMethods3.SetSearchTextOrThrow(state, searchString);

                // Request the properties we need from the result list
                NativeMethods3.AddSearchPropertyRequestOrThrow(state, NativeMethods3.EVERYTHING3_PROPERTY_ID_PATH_AND_NAME);
                NativeMethods3.AddSearchPropertyRequestOrThrow(state, NativeMethods3.EVERYTHING3_PROPERTY_ID_SIZE);
                NativeMethods3.AddSearchPropertyRequestOrThrow(state, NativeMethods3.EVERYTHING3_PROPERTY_ID_DATE_CREATED);
                NativeMethods3.AddSearchPropertyRequestOrThrow(state, NativeMethods3.EVERYTHING3_PROPERTY_ID_DATE_MODIFIED);
                NativeMethods3.AddSearchPropertyRequestOrThrow(state, NativeMethods3.EVERYTHING3_PROPERTY_ID_ATTRIBUTES);

                result = NativeMethods3.SearchOrThrow(client, state);

                nuint count = NativeMethods3.Everything3_GetResultListViewportCount(result);
                for (nuint idx = 0; idx < count; idx++)
                {
                    try
                    {
                        string fullName = NativeMethods3.Everything3_GetResultFullPathName(result, idx, maxPath);
                        if (string.IsNullOrEmpty(fullName))
                            continue;

                        uint rawAttr = NativeMethods3.Everything3_GetResultAttributes(result, idx);
                        if (rawAttr == INVALID_FILE_ATTRIBUTES)
                        {
                            logger.Debug("Everything3_GetResultAttributes returned INVALID_FILE_ATTRIBUTES for '{0}'", fullName);
                            CountMissingFiles++;
                            continue;
                        }
                        FileAttributes attr = (FileAttributes)rawAttr;

                        if (attr.HasFlag(FileAttributes.Directory))
                            continue;

                        ulong fileSize = NativeMethods3.Everything3_GetResultSize(result, idx);

                        ulong ftCreated = NativeMethods3.Everything3_GetResultDateCreated(result, idx);
                        DateTime createdTime = SafeFromFileTimeUtc(ftCreated);

                        ulong ftModified = NativeMethods3.Everything3_GetResultDateModified(result, idx);
                        DateTime lastWriteTime = SafeFromFileTimeUtc(ftModified);

                        EverythingFileInfo fileInfo = new(fullName, attr, (long)fileSize, createdTime, lastWriteTime);

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
                logger.Error(ex, "Everything3 SDK error in FindFiles");
                if (ex.ErrorCode == NativeMethods3.EVERYTHING3_ERROR_IPC_PIPE_NOT_FOUND)
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
                if (result != IntPtr.Zero)
                    NativeMethods3.Everything3_DestroyResultList(result);

                if (state != IntPtr.Zero)
                    NativeMethods3.Everything3_DestroySearchState(state);

                if (client != IntPtr.Zero)
                {
                    NativeMethods3.Everything3_ShutdownClient(client);
                    NativeMethods3.Everything3_DestroyClient(client);
                }
            }

            return results;
        }

        private static DateTime SafeFromFileTimeUtc(ulong fileTime)
        {
            // 0 and max value indicate the property is not indexed
            if (fileTime == 0 || fileTime == ulong.MaxValue)
                return DateTime.MinValue;

            try
            {
                return DateTime.FromFileTimeUtc((long)fileTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                return DateTime.MinValue;
            }
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
