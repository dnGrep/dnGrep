using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace dnGREP.Everything
{
    internal static unsafe partial class NativeMethods
    {
        // Do not include the Everything32.dll or Everything64.dll in the project,
        // MSI or portable zip, it must be installed separately by the user.
#if x86
        internal const string EverythingDLL = "Everything32.dll";
#else
        internal const string EverythingDLL = "Everything64.dll";
#endif

        internal const uint EVERYTHING_OK = 0;                    // The operation completed successfully.
        internal const uint EVERYTHING_ERROR_MEMORY = 1;          // Failed to allocate memory for the search query.
        internal const uint EVERYTHING_ERROR_IPC = 2;             // IPC is not available.
        internal const uint EVERYTHING_ERROR_REGISTERCLASSEX = 3; // Failed to register the search query window class.
        internal const uint EVERYTHING_ERROR_CREATEWINDOW = 4;    // Failed to create the search query window.
        internal const uint EVERYTHING_ERROR_CREATETHREAD = 5;    // Failed to create the search query thread.
        internal const uint EVERYTHING_ERROR_INVALIDINDEX = 6;    // Invalid index.The index must be greater or equal to 0 and less than the number of visible results.
        internal const uint EVERYTHING_ERROR_INVALIDCALL = 7;     // Invalid call.
        internal const uint EVERYTHING_ERROR_INVALIDREQUEST = 8;  // invalid request data, request data first.
        internal const uint EVERYTHING_ERROR_INVALIDPARAMETER = 9;// bad parameter.


        /// <summary>
        /// Retrieves the last-error code value.
        /// </summary>
        /// <returns>Error Code: 0 for OK, otherwise an error</returns>
        [LibraryImport(EverythingDLL)]
        internal static partial uint Everything_GetLastError();

        internal static string Everything_GetLastErrorString()
        {
            uint code = Everything_GetLastError();
            return GetErrorString(code);
        }

        internal static string GetErrorString(uint code)
        {
            return code switch
            {
                EVERYTHING_OK => "The operation completed successfully.",
                EVERYTHING_ERROR_MEMORY => "Failed to allocate memory for the search query.",
                EVERYTHING_ERROR_IPC => "Everything search client is not available.",
                EVERYTHING_ERROR_REGISTERCLASSEX => "Failed to register the search query window class.",
                EVERYTHING_ERROR_CREATEWINDOW => "Failed to create the search query window.",
                EVERYTHING_ERROR_CREATETHREAD => "Failed to create the search query thread.",
                EVERYTHING_ERROR_INVALIDINDEX => "Invalid index. The index must be greater or equal to 0 and less than the number of visible results.",
                EVERYTHING_ERROR_INVALIDCALL => "Invalid call.",
                EVERYTHING_ERROR_INVALIDREQUEST => "Invalid request data, request data first.",
                EVERYTHING_ERROR_INVALIDPARAMETER => "Bad parameter.",
                _ => $"Unknown error code: {code}.",
            };
        }

        #region Error handling helpers

        /// <summary>
        /// Checks the Everything last error and throws an <see cref="EverythingException"/> if it is not OK.
        /// </summary>
        /// <param name="callerName">Automatically populated with the calling method name.</param>
        internal static void ThrowIfError([CallerMemberName] string? callerName = null)
        {
            uint code = Everything_GetLastError();
            if (code != EVERYTHING_OK)
            {
                throw new EverythingException(code,
                    $"{callerName} failed: {GetErrorString(code)}");
            }
        }

        /// <summary>
        /// If <paramref name="result"/> is false, checks the Everything last error and throws.
        /// </summary>
        internal static void ThrowIfFalse(bool result, [CallerMemberName] string? callerName = null)
        {
            if (!result)
            {
                uint code = Everything_GetLastError();
                throw new EverythingException(code,
                    $"{callerName} returned false: {GetErrorString(code)}");
            }
        }

        #endregion

        #region Raw P/Invoke declarations

        /// <summary>
        /// Checks if the database has been fully loaded.
        /// </summary>
        /// <returns>returns true if the Everything database is fully loaded; Otherwise, false. 
        /// To get extended error information, call Everything_GetLastError
        /// </returns>
        [LibraryImport(EverythingDLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything_IsDBLoaded();

        /// <summary>
        /// Sets the search string for the IPC Query.
        /// </summary>
        /// <param name="searchString">The new search text</param>
        [LibraryImport(EverythingDLL, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial void Everything_SetSearchW(string searchString);

        /// <summary>
        /// Sets the desired result data.
        /// </summary>
        /// <param name="requestFlags">The request flags, can be zero or more of the following flags</param>
        [LibraryImport(EverythingDLL)]
        internal static partial void Everything_SetRequestFlags(uint requestFlags);


        /// <summary>
        /// Enables or disables Regular Expression searching.
        /// </summary>
        /// <param name="enable">True to enable regex searching, false to disable</param>
        [LibraryImport(EverythingDLL)]
        internal static partial void Everything_SetRegex([MarshalAs(UnmanagedType.Bool)] bool enable);



        /// <summary>
        /// Sets how the results should be ordered.
        /// </summary>
        /// <param name="sortType">The sort type</param>
        [LibraryImport(EverythingDLL)]
        internal static partial void Everything_SetSort(uint sortType);



        /// <summary>
        /// Executes an Everything IPC query with the current search state.
        /// </summary>
        /// <param name="wait">Should the function wait for the results or return immediately.</param>
        /// <returns>
        /// If the function succeeds, the return value is true, otherwise, false.
        /// To get extended error information, call Everything_GetLastError
        /// </returns>
        /// <remarks>
        /// If bWait is FALSE you must call Everything_SetReplyWindow before calling Everything_Query. 
        /// Use the Everything_IsQueryReply function to check for query replies.
        /// Optionally call the following functions to set the search state before calling Everything_Query:
        ///    Everything_SetSearch
        ///    Everything_SetMatchPath
        ///    Everything_SetMatchCase
        ///    Everything_SetMatchWholeWord
        ///    Everything_SetRegex
        ///    Everything_SetMax
        ///    Everything_SetOffset
        ///    Everything_SetReplyID
        ///    Everything_SetRequestFlags
        /// The search state is not modified from a call to Everything_Query.
        /// The default state is as follows: See Everything_Reset for the default search state.
        /// </remarks>
        [LibraryImport(EverythingDLL, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything_QueryW([MarshalAs(UnmanagedType.Bool)] bool wait);


        /// <summary>
        /// Sorts the current results by path, then file name. 
        /// SortResultsByPath is CPU Intensive.Sorting by path can take several seconds.
        /// </summary>
        /// <remarks>
        /// The default result list contains no results.
        /// Call Everything_Query to retrieve the result list prior to a call to Everything_SortResultsByPath.
        /// For improved performance, use Everything_SetSort
        /// </remarks>
        [LibraryImport(EverythingDLL)]
        internal static partial void Everything_SortResultsByPath();


        /// <summary>
        /// Returns the number of visible file results.
        /// </summary>
        /// <returns>
        /// If the function fails the return value is 0. To get extended error information, call Everything_GetLastError.
        /// </returns>
        [LibraryImport(EverythingDLL)]
        internal static partial uint Everything_GetNumResults();


        [LibraryImport(EverythingDLL, StringMarshalling = StringMarshalling.Utf16)]
        private static partial void Everything_GetResultFullPathNameW(uint nIndex, char* lpString, uint nMaxCount);

        /// <summary>
        /// Retrieves the full path and file name of the visible result.
        /// </summary>
        /// <param name="index">Zero based index of the visible result</param>
        /// <param name="maxCount">Specifies the maximum number of characters to copy to the buffer, including the NULL character. If the text exceeds this limit, it is truncated.</param>
        /// <returns>The full path</returns>
        internal static unsafe string Everything_GetResultFullPathName(uint index, int maxCount)
        {
            char* buffer = stackalloc char[maxCount];
            Everything_GetResultFullPathNameW(index, buffer, (uint)maxCount);
            int length = new ReadOnlySpan<char>(buffer, maxCount).IndexOf('\0');
            return length >= 0 ? new string(buffer, 0, length) : new string(buffer, 0, maxCount);
        }

        /// <summary>
        /// Retrieves the attributes of a visible result.
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        [LibraryImport(EverythingDLL)]
        internal static partial uint Everything_GetResultAttributes(uint nIndex);


        [LibraryImport(EverythingDLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool Everything_GetResultSize(uint nIndex, out long lpFileSize);

        /// <summary>
        /// Retrieves the size of a visible result.
        /// </summary>
        /// <param name="index">Zero based index of the visible result.</param>
        /// <returns>the size</returns>
        internal static long Everything_GetResultSize(uint index)
        {
            Everything_GetResultSize(index, out long size);

            return size;
        }



        [LibraryImport(EverythingDLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool Everything_GetResultDateModified(uint nIndex, out long lpFileTime);

        /// <summary>
        /// Retrieves the modified date of a visible result, in local time
        /// </summary>
        /// <param name="index">Zero based index of the visible result.</param>
        /// <returns>The modified time, or DateTime.MinValue if not successful</returns>
        internal static DateTime Everything_GetResultDateModified(uint index)
        {
            DateTime result = DateTime.MinValue;
            bool success = Everything_GetResultDateModified(index, out long fileTime);

            if (success && fileTime > 0)
                result = DateTime.FromFileTimeUtc(fileTime);
            return result;
        }

        [LibraryImport(EverythingDLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool Everything_GetResultDateCreated(uint nIndex, out long lpFileTime);

        /// <summary>
        /// Retrieves the created date of a visible result, in local time
        /// </summary>
        /// <param name="index">Zero based index of the visible result.</param>
        /// <returns>The created time, or DateTime.MinValue if not successful</returns>
        internal static DateTime Everything_GetResultDateCreated(uint index)
        {
            DateTime result = DateTime.MinValue;
            bool success = Everything_GetResultDateCreated(index, out long fileTime);

            if (success && fileTime > 0)
                result = DateTime.FromFileTimeUtc(fileTime);
            return result;
        }


        /// <summary>
        /// Resets the result list and search state to the default state, freeing any allocated memory by the library.
        /// </summary>
        /// <remarks>
        /// Calling Everything_SetSearch frees the old search and allocates the new search string.
        /// Calling Everything_Query frees the old result list and allocates the new result list.
        /// Calling Everything_Reset frees the current search and current result list.
        /// </remarks>
        [LibraryImport(EverythingDLL)]
        internal static partial void Everything_Reset();



        /// <summary>
        /// Retrieves the major version number of Everything.
        /// </summary>
        /// <returns>The function returns 0 if major version information is unavailable. To get extended error information, call Everything_GetLastError</returns>
        /// <remarks>
        /// Everything uses the following version format:
        /// major.minor.revision.build
        /// The build part is incremental and unique for all Everything versions.
        /// </remarks>
        [LibraryImport(EverythingDLL)]
        internal static partial uint Everything_GetMajorVersion();

        /// <summary>
        /// Retrieves the minor version number of Everything.
        /// </summary>
        /// <returns>The function returns 0 if minor version information is unavailable. To get extended error information, call Everything_GetLastError</returns>
        /// <remarks>
        /// Everything uses the following version format:
        /// major.minor.revision.build
        /// The build part is incremental and unique for all Everything versions.
        /// </remarks>
        [LibraryImport(EverythingDLL)]
        internal static partial uint Everything_GetMinorVersion();

        /// <summary>
        /// Retrieves the revision number of Everything.
        /// </summary>
        /// <returns>The function returns 0 if revision information is unavailable. To get extended error information, call Everything_GetLastError</returns>
        /// <remarks>
        /// Everything uses the following version format:
        /// major.minor.revision.build
        /// The build part is incremental and unique for all Everything versions.
        /// </remarks>
        [LibraryImport(EverythingDLL)]
        internal static partial uint Everything_GetRevision();

        /// <summary>
        /// Retrieves the build number of Everything.
        /// </summary>
        /// <returns>The function returns 0 if build version information is unavailable. To get extended error information, call Everything_GetLastError</returns>
        /// <remarks>
        /// Everything uses the following version format:
        /// major.minor.revision.build
        /// The build part is incremental and unique for all Everything versions.
        /// </remarks>
        [LibraryImport(EverythingDLL)]
        internal static partial uint Everything_GetBuildNumber();

        /// <summary>
        /// Requests Everything to rescan all folder indexes.
        /// </summary>
        /// <returns>
        /// The function returns non-zero if the request to rescan all folder indexes was successful.
        /// The function returns 0 if an error occurred.To get extended error information, call Everything_GetLastError
        /// </returns>
        [LibraryImport(EverythingDLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything_UpdateAllFolderIndexes();

        #endregion

        #region Checked wrapper methods

        /// <summary>
        /// Sets the search text.
        /// </summary>
        /// <remarks>
        /// The underlying SDK function is void and does not set an error code.
        /// Errors will surface when <see cref="QueryOrThrow"/> is called.
        /// </remarks>
        internal static void SetSearch(string searchString)
        {
            Everything_SetSearchW(searchString);
        }

        /// <summary>
        /// Sets the sort order.
        /// </summary>
        internal static void SetSort(uint sortType)
        {
            Everything_SetSort(sortType);
        }

        /// <summary>
        /// Sets the request flags.
        /// </summary>
        internal static void SetRequestFlags(uint requestFlags)
        {
            Everything_SetRequestFlags(requestFlags);
        }

        /// <summary>
        /// Executes a query. Throws <see cref="EverythingException"/> on failure.
        /// </summary>
        internal static void QueryOrThrow(bool wait)
        {
            ThrowIfFalse(Everything_QueryW(wait), nameof(Everything_QueryW));
        }

        /// <summary>
        /// Requests Everything to rescan all folder indexes. Throws <see cref="EverythingException"/> on failure.
        /// </summary>
        internal static void UpdateAllFolderIndexesOrThrow()
        {
            ThrowIfFalse(Everything_UpdateAllFolderIndexes(), nameof(Everything_UpdateAllFolderIndexes));
        }

        #endregion
    }
}
