using System;
using System.Runtime.InteropServices;
using System.Text;

namespace dnGREP.Everything
{
    internal static class NativeMethods
    {
#pragma warning disable SYSLIB1054

#if x86
        private const string EverythingDLL = "Everything32.dll";
#else
        private const string EverythingDLL = "Everything64.dll";
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
        [DllImport(EverythingDLL)]
        internal static extern uint Everything_GetLastError();

        internal static string Everything_GetLastErrorString()
        {
            uint code = Everything_GetLastError();

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
                _ => "Unknown error code.",
            };
        }

        /// <summary>
        /// Checks if the database has been fully loaded.
        /// </summary>
        /// <returns>returns true if the Everything database is fully loaded; Otherwise, false. 
        /// To get extended error information, call Everything_GetLastError
        /// </returns>
        [DllImport(EverythingDLL)]
        internal static extern bool Everything_IsDBLoaded();

        /// <summary>
        /// Sets the search string for the IPC Query.
        /// </summary>
        /// <param name="searchString">The new search text</param>
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        internal static extern void Everything_SetSearchW(string searchString);

        /// <summary>
        /// Sets the desired result data.
        /// </summary>
        /// <param name="requestFlags">The request flags, can be zero or more of the following flags</param>
        [DllImport(EverythingDLL)]
        internal static extern void Everything_SetRequestFlags(uint requestFlags);


        /// <summary>
        /// Enables or disables Regular Expression searching.
        /// </summary>
        /// <param name="enable">True to enable regex searching, false to disable</param>
        [DllImport(EverythingDLL)]
        internal static extern void Everything_SetRegex(bool enable);



        /// <summary>
        /// Sets how the results should be ordered.
        /// </summary>
        /// <param name="sortType">The sort type</param>
        [DllImport(EverythingDLL)]
        internal static extern void Everything_SetSort(uint sortType);



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
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        internal static extern bool Everything_QueryW(bool wait);


        /// <summary>
        /// Sorts the current results by path, then file name. 
        /// SortResultsByPath is CPU Intensive.Sorting by path can take several seconds.
        /// </summary>
        /// <remarks>
        /// The default result list contains no results.
        /// Call Everything_Query to retrieve the result list prior to a call to Everything_SortResultsByPath.
        /// For improved performance, use Everything_SetSort
        /// </remarks>
        [DllImport(EverythingDLL)]
        internal static extern void Everything_SortResultsByPath();


        /// <summary>
        /// Returns the number of visible file results.
        /// </summary>
        /// <returns>
        /// If the function fails the return value is 0. To get extended error information, call Everything_GetLastError.
        /// </returns>
        [DllImport(EverythingDLL)]
        internal static extern uint Everything_GetNumResults();


        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern void Everything_GetResultFullPathNameW(uint nIndex, StringBuilder lpString, uint nMaxCount);

        /// <summary>
        /// Retrieves the full path and file name of the visible result.
        /// </summary>
        /// <param name="index">Zero based index of the visible result</param>
        /// <param name="maxCount">Specifies the maximum number of characters to copy to the buffer, including the NULL character. If the text exceeds this limit, it is truncated.</param>
        /// <returns>The full path</returns>
        internal static string Everything_GetResultFullPathName(uint index, int maxCount)
        {
            StringBuilder sb = new(maxCount);
            Everything_GetResultFullPathNameW(index, sb, (uint)maxCount);
            return sb.ToString();
        }

        /// <summary>
        /// Retrieves the attributes of a visible result.
        /// </summary>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        [DllImport(EverythingDLL)]
        internal static extern uint Everything_GetResultAttributes(uint nIndex);


        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetResultSize(uint nIndex, out long lpFileSize);

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



        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetResultDateModified(uint nIndex, out long lpFileTime);

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

        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetResultDateCreated(uint nIndex, out long lpFileTime);

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
        [DllImport(EverythingDLL)]
        internal static extern void Everything_Reset();



        /// <summary>
        /// Retrieves the major version number of Everything.
        /// </summary>
        /// <returns>The function returns 0 if major version information is unavailable. To get extended error information, call Everything_GetLastError</returns>
        /// <remarks>
        /// Everything uses the following version format:
        /// major.minor.revision.build
        /// The build part is incremental and unique for all Everything versions.
        /// </remarks>
        [DllImport(EverythingDLL)]
        internal static extern uint Everything_GetMajorVersion();

        /// <summary>
        /// Retrieves the minor version number of Everything.
        /// </summary>
        /// <returns>The function returns 0 if minor version information is unavailable. To get extended error information, call Everything_GetLastError</returns>
        /// <remarks>
        /// Everything uses the following version format:
        /// major.minor.revision.build
        /// The build part is incremental and unique for all Everything versions.
        /// </remarks>
        [DllImport(EverythingDLL)]
        internal static extern uint Everything_GetMinorVersion();

        /// <summary>
        /// Retrieves the revision number of Everything.
        /// </summary>
        /// <returns>The function returns 0 if revision information is unavailable. To get extended error information, call Everything_GetLastError</returns>
        /// <remarks>
        /// Everything uses the following version format:
        /// major.minor.revision.build
        /// The build part is incremental and unique for all Everything versions.
        /// </remarks>
        [DllImport(EverythingDLL)]
        internal static extern uint Everything_GetRevision();

        /// <summary>
        /// Retrieves the build number of Everything.
        /// </summary>
        /// <returns>The function returns 0 if build version information is unavailable. To get extended error information, call Everything_GetLastError</returns>
        /// <remarks>
        /// Everything uses the following version format:
        /// major.minor.revision.build
        /// The build part is incremental and unique for all Everything versions.
        /// </remarks>
        [DllImport(EverythingDLL)]
        internal static extern uint Everything_GetBuildNumber();

        /// <summary>
        /// Requests Everything to rescan all folder indexes.
        /// </summary>
        /// <returns>
        /// The function returns non-zero if the request to rescan all folder indexes was successful.
        /// The function returns 0 if an error occurred.To get extended error information, call Everything_GetLastError
        /// </returns>
        [DllImport(EverythingDLL)]
        internal static extern bool Everything_UpdateAllFolderIndexes();

        /// <summary>
        /// Requests Everything to forcefully rebuild the Everything index.
        /// </summary>
        /// <remarks>
        /// Requesting a rebuild will mark all indexes as dirty and start the rebuild process.
        /// Use Everything_IsDBLoaded to determine if the database has been rebuilt before performing a query.
        /// </remarks>
        /// <returns></returns>
        [DllImport(EverythingDLL)]
        internal static extern bool Everything_RebuildDB();


        // not used at this time:

        [DllImport(EverythingDLL)]
        private static extern void Everything_SetReplyID(uint id);
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetReplyID();
        [DllImport(EverythingDLL)]
        private static extern void Everything_SetReplyWindow(IntPtr hWnd);
        [DllImport(EverythingDLL)]
        private static extern IntPtr Everything_GetReplyWindow();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_IsQueryReply(uint message, IntPtr wParam, IntPtr lParam, uint dwId);
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern string Everything_GetResultFileNameW(uint nIndex);
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern string Everything_GetResultPathW(uint nIndex);
        [DllImport(EverythingDLL)]
        private static extern bool Everything_IsFastSort(uint sortType);
        [DllImport(EverythingDLL)]
        private static extern bool Everything_IsFileInfoIndexed(uint fileInfoType);
        [DllImport(EverythingDLL)]
        private static extern void Everything_SetMatchPath(bool bEnable);
        [DllImport(EverythingDLL)]
        private static extern void Everything_SetMatchCase(bool bEnable);
        [DllImport(EverythingDLL)]
        private static extern void Everything_SetMatchWholeWord(bool bEnable);
        [DllImport(EverythingDLL)]
        private static extern void Everything_SetMax(uint dwMax);
        [DllImport(EverythingDLL)]
        private static extern void Everything_SetOffset(uint dwOffset);
        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetMatchPath();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetMatchCase();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetMatchWholeWord();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetMax();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetOffset();
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern string Everything_GetSearchW();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetNumFileResults();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetNumFolderResults();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetTotFileResults();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetTotFolderResults();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetTotResults();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_IsVolumeResult(uint nIndex);
        [DllImport(EverythingDLL)]
        private static extern bool Everything_IsFolderResult(uint nIndex);
        [DllImport(EverythingDLL)]
        private static extern bool Everything_IsFileResult(uint nIndex);
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetSort();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetResultListSort();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetRequestFlags();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetResultListRequestFlags();
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern string Everything_GetResultExtensionW(uint nIndex);
        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetResultDateAccessed(uint nIndex, out long lpFileTime);
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern string Everything_GetResultFileListFileNameW(uint nIndex);
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetResultRunCount(uint nIndex);
        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetResultDateRun(uint nIndex, out long lpFileTime);
        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetResultDateRecentlyChanged(uint nIndex, out long lpFileTime);
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern string Everything_GetResultHighlightedFileNameW(uint nIndex);
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern string Everything_GetResultHighlightedPathW(uint nIndex);
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern string Everything_GetResultHighlightedFullPathAndFileNameW(uint nIndex);
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern uint Everything_GetRunCountFromFileNameW(string lpFileName);
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern bool Everything_SetRunCountFromFileNameW(string lpFileName, uint dwRunCount);
        [DllImport(EverythingDLL, CharSet = CharSet.Unicode)]
        private static extern uint Everything_IncRunCountFromFileNameW(string lpFileName);
        [DllImport(EverythingDLL)]
        private static extern bool Everything_GetRegex();
        [DllImport(EverythingDLL)]
        private static extern void Everything_CleanUp();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_Exit();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_IsAdmin();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_IsAppData();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_SaveDB();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_SaveRunHistory();
        [DllImport(EverythingDLL)]
        private static extern bool Everything_DeleteRunHistory();
        [DllImport(EverythingDLL)]
        private static extern uint Everything_GetTargetMachine();

#pragma warning restore SYSLIB1054
    }
}
