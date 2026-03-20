using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace dnGREP.Everything
{
    internal static unsafe partial class NativeMethods3
    {
        // Do not include the Everything3 dlls in the project,
        // MSI or portable zip, it must be installed separately by the user.
#if x86
        internal const string EverythingDLL = "Everything3_x86.dll";
#elif x64
        internal const string EverythingDLL = "Everything3_x64.dll";
#else
        internal const string EverythingDLL = "Everything3_ARM64.dll";
#endif

        // Everything3 SDK uses __stdcall calling convention
        // On x64/ARM64 this is ignored (only one calling convention), but on x86 it is required
        // to avoid stack corruption.

        // Everything3_GetLastError()
        internal const uint EVERYTHING3_OK = 0;                                         // No error detected.
        internal const uint EVERYTHING3_ERROR_OUT_OF_MEMORY = 0xE0000001;               // Out of memory.
        internal const uint EVERYTHING3_ERROR_IPC_PIPE_NOT_FOUND = 0xE0000002;          // IPC pipe server not found. (Everything search client is not running)
        internal const uint EVERYTHING3_ERROR_DISCONNECTED = 0xE0000003;                // Disconnected from pipe server.
        internal const uint EVERYTHING3_ERROR_INVALID_PARAMETER = 0xE0000004;           // Invalid parameter.
        internal const uint EVERYTHING3_ERROR_BAD_REQUEST = 0xE0000005;                 // Bad request.
        internal const uint EVERYTHING3_ERROR_CANCELLED = 0xE0000006;                   // User cancelled.
        internal const uint EVERYTHING3_ERROR_PROPERTY_NOT_FOUND = 0xE0000007;          // Property not found.
        internal const uint EVERYTHING3_ERROR_SERVER = 0xE0000008;                      // Server error. (server out of memory)
        internal const uint EVERYTHING3_ERROR_INVALID_COMMAND = 0xE0000009;             // Invalid command.
        internal const uint EVERYTHING3_ERROR_BAD_RESPONSE = 0xE000000A;                // Bad server response.
        internal const uint EVERYTHING3_ERROR_INSUFFICIENT_BUFFER = 0xE000000B;         // Not enough room to store response data.
        internal const uint EVERYTHING3_ERROR_SHUTDOWN = 0xE000000C;                    // Shutdown initiated by user.
        internal const uint EVERYTHING3_ERROR_INVALID_PROPERTY_VALUE_TYPE = 0xE000000D; // Property value type is incorrect.

        #region Error handling helpers

        /// <summary>
        /// Retrieves the last-error code value.
        /// </summary>
        /// <returns>Error Code: 0 for OK, otherwise an error</returns>
        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial uint Everything3_GetLastError();

        internal static string Everything3_GetLastErrorString()
        {
            uint code = Everything3_GetLastError();
            return GetErrorString(code);
        }

        internal static string GetErrorString(uint code)
        {
            return code switch
            {
                EVERYTHING3_OK => "The operation completed successfully.",
                EVERYTHING3_ERROR_OUT_OF_MEMORY => "Failed to allocate memory for the search query.",
                EVERYTHING3_ERROR_IPC_PIPE_NOT_FOUND => "Everything search client is not running.",
                EVERYTHING3_ERROR_DISCONNECTED => "Disconnected from pipe server.",
                EVERYTHING3_ERROR_INVALID_PARAMETER => "Invalid parameter.",
                EVERYTHING3_ERROR_BAD_REQUEST => "Bad request.",
                EVERYTHING3_ERROR_CANCELLED => "User cancelled.",
                EVERYTHING3_ERROR_PROPERTY_NOT_FOUND => "Property not found.",
                EVERYTHING3_ERROR_SERVER => "Server error. (server out of memory)",
                EVERYTHING3_ERROR_INVALID_COMMAND => "Invalid command.",
                EVERYTHING3_ERROR_BAD_RESPONSE => "Bad server response.",
                EVERYTHING3_ERROR_INSUFFICIENT_BUFFER => "Not enough room to store response data.",
                EVERYTHING3_ERROR_SHUTDOWN => "Shutdown initiated by user.",
                EVERYTHING3_ERROR_INVALID_PROPERTY_VALUE_TYPE => "Property value type is incorrect.",
                _ => $"Unknown error code: 0x{code:X8}.",
            };
        }

        /// <summary>
        /// Checks the Everything3 last error and throws an <see cref="EverythingException"/> if it is not OK.
        /// </summary>
        /// <param name="callerName">Automatically populated with the calling method name.</param>
        internal static void ThrowIfError([CallerMemberName] string? callerName = null)
        {
            uint code = Everything3_GetLastError();
            if (code != EVERYTHING3_OK)
            {
                throw new EverythingException(code,
                    $"{callerName} failed: {GetErrorString(code)}");
            }
        }

        /// <summary>
        /// If <paramref name="result"/> is false, checks the Everything3 last error and throws.
        /// </summary>
        internal static void ThrowIfFalse(bool result, [CallerMemberName] string? callerName = null)
        {
            if (!result)
            {
                uint code = Everything3_GetLastError();
                throw new EverythingException(code,
                    $"{callerName} returned false: {GetErrorString(code)}");
            }
        }

        /// <summary>
        /// If <paramref name="ptr"/> is <see cref="IntPtr.Zero"/>, checks the Everything3 last error and throws.
        /// </summary>
        internal static IntPtr ThrowIfNull(IntPtr ptr, [CallerMemberName] string? callerName = null)
        {
            if (ptr == IntPtr.Zero)
            {
                uint code = Everything3_GetLastError();
                throw new EverythingException(code,
                    $"{callerName} returned null: {GetErrorString(code)}");
            }
            return ptr;
        }

        #endregion

        #region Raw P/Invoke declarations

        [LibraryImport(EverythingDLL, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial IntPtr Everything3_ConnectW(string instanceName);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything3_ShutdownClient(IntPtr client);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything3_DestroyClient(IntPtr client);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial uint Everything3_GetMajorVersion(IntPtr client);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial uint Everything3_GetMinorVersion(IntPtr client);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial uint Everything3_GetRevision(IntPtr client);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial uint Everything3_GetBuildNumber(IntPtr client);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial uint Everything3_GetTargetMachine(IntPtr client);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything3_IsDBLoaded(IntPtr client);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial IntPtr Everything3_CreateSearchState();

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything3_DestroySearchState(IntPtr searchState);

        [LibraryImport(EverythingDLL, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything3_SetSearchTextW(IntPtr searchState, string search);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything3_AddSearchSort(IntPtr searchState, uint propertyId, [MarshalAs(UnmanagedType.Bool)] bool ascending);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything3_AddSearchPropertyRequest(IntPtr searchState, uint propertyId);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial IntPtr Everything3_Search(IntPtr client, IntPtr searchState);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Everything3_DestroyResultList(IntPtr resultList);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial nuint Everything3_GetResultListViewportCount(IntPtr resultList);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial nuint Everything3_GetResultFullPathNameW(IntPtr resultList, nuint resultIndex,
            char* out_wbuf, nuint wbuf_size_in_wchars);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial ulong Everything3_GetResultSize(IntPtr resultList, nuint resultIndex);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial ulong Everything3_GetResultDateCreated(IntPtr resultList, nuint resultIndex);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial ulong Everything3_GetResultDateModified(IntPtr resultList, nuint resultIndex);

        [LibraryImport(EverythingDLL)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        internal static partial uint Everything3_GetResultAttributes(IntPtr resultList, nuint resultIndex);

        #endregion

        #region Checked wrapper methods

        /// <summary>
        /// Connects to the Everything 3 service. Throws <see cref="EverythingException"/> on failure.
        /// </summary>
        internal static IntPtr ConnectOrThrow(string instanceName)
        {
            IntPtr client = Everything3_ConnectW(instanceName);
            return ThrowIfNull(client, nameof(Everything3_ConnectW));
        }

        /// <summary>
        /// Creates a new search state. Throws <see cref="EverythingException"/> on failure.
        /// </summary>
        internal static IntPtr CreateSearchStateOrThrow()
        {
            IntPtr state = Everything3_CreateSearchState();
            return ThrowIfNull(state, nameof(Everything3_CreateSearchState));
        }

        /// <summary>
        /// Executes a search. Throws <see cref="EverythingException"/> on failure.
        /// </summary>
        internal static IntPtr SearchOrThrow(IntPtr client, IntPtr searchState)
        {
            IntPtr result = Everything3_Search(client, searchState);
            return ThrowIfNull(result, nameof(Everything3_Search));
        }

        /// <summary>
        /// Sets the search text. Throws <see cref="EverythingException"/> on failure.
        /// </summary>
        internal static void SetSearchTextOrThrow(IntPtr searchState, string search)
        {
            ThrowIfFalse(Everything3_SetSearchTextW(searchState, search), nameof(Everything3_SetSearchTextW));
        }

        /// <summary>
        /// Adds a sort criterion. Throws <see cref="EverythingException"/> on failure.
        /// </summary>
        internal static void AddSearchSortOrThrow(IntPtr searchState, uint propertyId, bool ascending)
        {
            ThrowIfFalse(Everything3_AddSearchSort(searchState, propertyId, ascending), nameof(Everything3_AddSearchSort));
        }

        /// <summary>
        /// Adds a property request. Throws <see cref="EverythingException"/> on failure.
        /// </summary>
        internal static void AddSearchPropertyRequestOrThrow(IntPtr searchState, uint propertyId)
        {
            ThrowIfFalse(Everything3_AddSearchPropertyRequest(searchState, propertyId), nameof(Everything3_AddSearchPropertyRequest));
        }

        internal static string Everything3_GetResultFullPathName(IntPtr resultList, nuint resultIndex, int maxCount)
        {
            char* buffer = stackalloc char[maxCount];
            nuint len = Everything3_GetResultFullPathNameW(resultList, resultIndex, buffer, (nuint)maxCount);
            if (len == 0)
                return string.Empty;

            return new string(buffer, 0, (int)len);
        }

        #endregion

        #region Property IDs
        // Property IDs
        // These will not change.
        // The value type will not change.
        internal const uint EVERYTHING3_INVALID_PROPERTY_ID = 0xffffffff;
        internal const uint EVERYTHING3_PROPERTY_ID_NAME = 0; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_PATH = 1; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_FOLDER_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_SIZE = 2; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_EXTENSION = 3; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_TYPE = 4; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_MODIFIED = 5; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_CREATED = 6; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_ACCESSED = 7; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_ATTRIBUTES = 8; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_RECENTLY_CHANGED = 9; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_RUN_COUNT = 10; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_RUN = 11; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_FILE_LIST_NAME = 12; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_WIDTH = 13; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_HEIGHT = 14; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DIMENSIONS = 15; // EVERYTHING3_PROPERTY_VALUE_TYPE_DIMENSIONS,
        internal const uint EVERYTHING3_PROPERTY_ID_ASPECT_RATIO = 16; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_BIT_DEPTH = 17; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE,
        internal const uint EVERYTHING3_PROPERTY_ID_LENGTH = 18; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_AUDIO_SAMPLE_RATE = 19; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_AUDIO_CHANNELS = 20; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_AUDIO_BITS_PER_SAMPLE = 21; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_AUDIO_BIT_RATE = 22; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_AUDIO_FORMAT = 23; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_FILE_SIGNATURE = 24; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_TITLE = 25; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ARTIST = 26; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ALBUM = 27; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_YEAR = 28; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_COMMENT = 29; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_TRACK = 30; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_GENRE = 31; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_FRAME_RATE = 32; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_VIDEO_BIT_RATE = 33; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_VIDEO_FORMAT = 34; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_RATING = 35; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE,
        internal const uint EVERYTHING3_PROPERTY_ID_TAGS = 36; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_MD5 = 37; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA1 = 38; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA256 = 39; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_CRC32 = 40; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_SIZE_ON_DISK = 41; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_DESCRIPTION = 42; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_VERSION = 43; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PRODUCT_NAME = 44; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PRODUCT_VERSION = 45; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPANY = 46; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_KIND = 47; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_NAME_LENGTH = 48; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_PATH_AND_NAME_LENGTH = 49; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_SUBJECT = 50; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_AUTHORS = 51; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_TAKEN = 52; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_SOFTWARE = 53; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_ACQUIRED = 54; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_COPYRIGHT = 55; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_IMAGE_ID = 56; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_HORIZONTAL_RESOLUTION = 57; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_VERTICAL_RESOLUTION = 58; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPRESSION = 59; // EVERYTHING3_PROPERTY_VALUE_TYPE_WORD,
        internal const uint EVERYTHING3_PROPERTY_ID_RESOLUTION_UNIT = 60; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_COLOR_REPRESENTATION = 61; // EVERYTHING3_PROPERTY_VALUE_TYPE_WORD,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPRESSED_BITS_PER_PIXEL = 62; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_CAMERA_MAKER = 63; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CAMERA_MODEL = 64; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_F_STOP = 65; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_EXPOSURE_TIME = 66; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_ISO_SPEED = 67; // EVERYTHING3_PROPERTY_VALUE_TYPE_WORD,
        internal const uint EVERYTHING3_PROPERTY_ID_EXPOSURE_BIAS = 68; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_FOCAL_LENGTH = 69; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_MAX_APERTURE = 70; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1M,
        internal const uint EVERYTHING3_PROPERTY_ID_METERING_MODE = 71; // EVERYTHING3_PROPERTY_VALUE_TYPE_WORD,
        internal const uint EVERYTHING3_PROPERTY_ID_SUBJECT_DISTANCE = 72; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_FLASH_MODE = 73; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_FLASH_ENERGY = 74; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_35MM_FOCAL_LENGTH = 75; // EVERYTHING3_PROPERTY_VALUE_TYPE_WORD,
        internal const uint EVERYTHING3_PROPERTY_ID_LENS_MAKER = 76; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_LENS_MODEL = 77; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_FLASH_MAKER = 78; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_FLASH_MODEL = 79; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CAMERA_SERIAL_NUMBER = 80; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CONTRAST = 81; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_BRIGHTNESS = 82; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1M,
        internal const uint EVERYTHING3_PROPERTY_ID_LIGHT_SOURCE = 83; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_EXPOSURE_PROGRAM = 84; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_SATURATION = 85; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_SHARPNESS = 86; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_WHITE_BALANCE = 87; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_PHOTOMETRIC_INTERPRETATION = 88; // EVERYTHING3_PROPERTY_VALUE_TYPE_WORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DIGITAL_ZOOM = 89; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_EXIF_VERSION = 90; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_LATITUDE = 91; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1M,
        internal const uint EVERYTHING3_PROPERTY_ID_LONGITUDE = 92; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1M,
        internal const uint EVERYTHING3_PROPERTY_ID_ALTITUDE = 93; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1M,
        internal const uint EVERYTHING3_PROPERTY_ID_SUBTITLE = 94; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_TOTAL_BIT_RATE = 95; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DIRECTORS = 96; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PRODUCERS = 97; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_WRITERS = 98; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PUBLISHER = 99; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CONTENT_DISTRIBUTOR = 100; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_ENCODED = 101; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_ENCODED_BY = 102; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_AUTHOR_URL = 103; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PROMOTION_URL = 104; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_OFFLINE_AVAILABILITY = 105; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_OFFLINE_STATUS = 106; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_SHARED_WITH = 107; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_OWNER = 108; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPUTER = 109; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ALBUM_ARTIST = 110; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PARENTAL_RATING_REASON = 111; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPOSER = 112; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CONDUCTOR = 113; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CONTENT_GROUP_DESCRIPTION = 114; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_MOOD = 115; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PART_OF_SET = 116; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_INITIAL_KEY = 117; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_BEATS_PER_MINUTE = 118; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PROTECTED = 119; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_PART_OF_A_COMPILATION = 120; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_PARENTAL_RATING = 121; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PERIOD = 122; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PEOPLE = 123; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CATEGORY = 124; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CONTENT_STATUS = 125; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DOCUMENT_CONTENT_TYPE = 126; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PAGE_COUNT = 127; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_WORD_COUNT = 128; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_CHARACTER_COUNT = 129; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_LINE_COUNT = 130; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_PARAGRAPH_COUNT = 131; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_TEMPLATE = 132; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_SCALE = 133; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_LINKS_DIRTY = 134; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_LANGUAGE = 135; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_AUTHOR = 136; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_REVISION_NUMBER = 137; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_VERSION_NUMBER = 138; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_MANAGER = 139; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_CONTENT_CREATED = 140; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_SAVED = 141; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_PRINTED = 142; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_TOTAL_EDITING_TIME = 143; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_ORIGINAL_FILE_NAME = 144; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_RELEASED = 145; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_SLIDE_COUNT = 146; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_NOTE_COUNT = 147; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_HIDDEN_SLIDE_COUNT = 148; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_PRESENTATION_FORMAT = 149; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_TRADEMARKS = 150; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DISPLAY_NAME = 151; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_NAME_LENGTH_IN_UTF8_BYTES = 152; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_PATH_AND_NAME_LENGTH_IN_UTF8_BYTES = 153; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_CHILD_COUNT = 154; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_CHILD_FOLDER_COUNT = 155; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_CHILD_FILE_COUNT = 156; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_CHILD_COUNT_FROM_DISK = 157; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_CHILD_FOLDER_COUNT_FROM_DISK = 158; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_CHILD_FILE_COUNT_FROM_DISK = 159; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DEPTH = 160; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_TOTAL_SIZE = 161; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_TOTAL_SIZE_ON_DISK = 162; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_CHANGED = 163; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_HARD_LINK_COUNT = 164; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DELETE_PENDING = 165; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_IS_DIRECTORY = 166; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_ALTERNATE_DATA_STREAM_COUNT = 167; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_ALTERNATE_DATA_STREAM_NAMES = 168; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_TOTAL_ALTERNATE_DATA_STREAM_SIZE = 169; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_TOTAL_ALTERNATE_DATA_STREAM_SIZE_ON_DISK = 170; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPRESSED_SIZE = 171; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPRESSION_FORMAT = 172; // EVERYTHING3_PROPERTY_VALUE_TYPE_WORD,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPRESSION_UNIT_SHIFT = 173; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPRESSION_CHUNK_SHIFT = 174; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPRESSION_CLUSTER_SHIFT = 175; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE,
        internal const uint EVERYTHING3_PROPERTY_ID_COMPRESSION_RATIO = 176; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE,
        internal const uint EVERYTHING3_PROPERTY_ID_REPARSE_TAG = 177; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_REMOTE_PROTOCOL = 178; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_REMOTE_PROTOCOL_VERSION = 179; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_REMOTE_PROTOCOL_FLAGS = 180; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_LOGICAL_BYTES_PER_SECTOR = 181; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_PHYSICAL_BYTES_PER_SECTOR_FOR_ATOMICITY = 182; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_PHYSICAL_BYTES_PER_SECTOR_FOR_PERFORMANCE = 183; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_EFFECTIVE_PHYSICAL_BYTES_PER_SECTOR_FOR_ATOMICITY = 184; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_FILE_STORAGE_INFO_FLAGS = 185; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_BYTE_OFFSET_FOR_SECTOR_ALIGNMENT = 186; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_BYTE_OFFSET_FOR_PARTITION_ALIGNMENT = 187; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_ALIGNMENT_REQUIREMENT = 188; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_VOLUME_SERIAL_NUMBER = 189; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_FILE_ID = 190; // EVERYTHING3_PROPERTY_VALUE_TYPE_OWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_FRAME_COUNT = 191; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_CLUSTER_SIZE = 192; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_SECTOR_SIZE = 193; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_AVAILABLE_FREE_DISK_SIZE = 194; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_FREE_DISK_SIZE = 195; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_TOTAL_DISK_SIZE = 196; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint _EVERYTHING3_PROPERTY_ID_UNUSED197 = 197; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_MAXIMUM_COMPONENT_LENGTH = 198; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_FILE_SYSTEM_FLAGS = 199; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_FILE_SYSTEM = 200; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ORIENTATION = 201; // EVERYTHING3_PROPERTY_VALUE_TYPE_WORD,
        internal const uint EVERYTHING3_PROPERTY_ID_END_OF_FILE = 202; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_SHORT_NAME = 203; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_SHORT_PATH_AND_NAME = 204; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ENCRYPTION_STATUS = 205; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_HARD_LINK_FILE_NAMES = 206; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_INDEX_TYPE = 207; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_DRIVE_TYPE = 208; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_BINARY_TYPE = 209; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_0 = 210; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_1 = 211; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_2 = 212; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_3 = 213; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_4 = 214; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_5 = 215; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_6 = 216; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_7 = 217; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_8 = 218; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCH_9 = 219; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_SIBLING_COUNT = 220; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_SIBLING_FOLDER_COUNT = 221; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_SIBLING_FILE_COUNT = 222; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_INDEX_NUMBER = 223; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_SHORTCUT_TARGET = 224; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_OUT_OF_DATE = 225; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_INCUR_SEEK_PENALTY = 226; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_PLAIN_TEXT_LINE_COUNT = 227; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_APERTURE = 228; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1M,
        internal const uint EVERYTHING3_PROPERTY_ID_MAKER_NOTE = 229; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_RELATED_SOUND_FILE = 230; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_SHUTTER_SPEED = 231; // EVERYTHING3_PROPERTY_VALUE_TYPE_INT32_FIXED_Q1K,
        internal const uint EVERYTHING3_PROPERTY_ID_TRANSCODED_FOR_SYNC = 232; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_CASE_SENSITIVE_DIR = 233; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_INDEXED = 234; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_NAME_FREQUENCY = 235; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_SIZE_FREQUENCY = 236; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_EXTENSION_FREQUENCY = 237; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_REGEX_MATCHES = 238; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_URL = 239; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PATH_AND_NAME = 240; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_FILE_OR_FOLDER_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_PARENT_FILE_ID = 241; // EVERYTHING3_PROPERTY_VALUE_TYPE_OWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA512 = 242; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA384 = 243; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_CRC64 = 244; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_BYTE = 245; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_2_BYTES = 246; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_4_BYTES = 247; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_8_BYTES = 248; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_16_BYTES = 249; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_32_BYTES = 250; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_64_BYTES = 251; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_128_BYTES = 252; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_BYTE = 253; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_2_BYTES = 254; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_4_BYTES = 255; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_8_BYTES = 256; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_16_BYTES = 257; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_32_BYTES = 258; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_64_BYTES = 259; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_128_BYTES = 260; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_BYTE_ORDER_MARK = 261; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_VOLUME_LABEL = 262; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_FILE_LIST_PATH_AND_NAME = 263; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_DISPLAY_PATH_AND_NAME = 264; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PARSE_NAME = 265; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PARSE_PATH_AND_NAME = 266; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_STEM = 267; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_SHELL_ATTRIBUTES = 268; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_IS_FOLDER = 269; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_VALID_UTF8 = 270; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_STEM_LENGTH = 271; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_EXTENSION_LENGTH = 272; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_PATH_PART_LENGTH = 273; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_MODIFIED_TIME = 274; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_CREATED_TIME = 275; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_ACCESSED_TIME = 276; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_MODIFIED_DATE = 277; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_CREATED_DATE = 278; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_ACCESSED_DATE = 279; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_PARENT_NAME = 280; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_REPARSE_TARGET = 281; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DESCENDANT_COUNT = 282; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_DESCENDANT_FOLDER_COUNT = 283; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_DESCENDANT_FILE_COUNT = 284; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_FROM = 285; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_TO = 286; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_RECEIVED = 287; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_SENT = 288; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_CONTAINER_FILENAMES = 289; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CONTAINER_FILE_COUNT = 290; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_0 = 291; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_1 = 292; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_2 = 293; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_3 = 294; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_4 = 295; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_5 = 296; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_6 = 297; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_7 = 298; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_8 = 299; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CUSTOM_PROPERTY_9 = 300; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ALLOCATION_SIZE = 301; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_SFV_CRC32 = 302; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_MD5SUM_MD5 = 303; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA1SUM_SHA1 = 304; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA256SUM_SHA256 = 305; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_SFV_PASS = 306; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_MD5SUM_PASS = 307; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA1SUM_PASS = 308; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA256SUM_PASS = 309; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_ALTERNATE_DATA_STREAM_ANSI = 310; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ALTERNATE_DATA_STREAM_UTF8 = 311; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ALTERNATE_DATA_STREAM_UTF16LE = 312; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ALTERNATE_DATA_STREAM_UTF16BE = 313; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ALTERNATE_DATA_STREAM_TEXT_PLAIN = 314; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_ALTERNATE_DATA_STREAM_HEX = 315; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PERCEIVED_TYPE = 316; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CONTENT_TYPE = 317; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_OPENED_BY = 318; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_TARGET_MACHINE = 319; // EVERYTHING3_PROPERTY_VALUE_TYPE_WORD_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA512SUM_SHA512 = 320; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_SHA512SUM_PASS = 321; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_PARENT_PATH = 322; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_FOLDER_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_256_BYTES = 323; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB16,
        internal const uint EVERYTHING3_PROPERTY_ID_FIRST_512_BYTES = 324; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB16,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_256_BYTES = 325; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB16,
        internal const uint EVERYTHING3_PROPERTY_ID_LAST_512_BYTES = 326; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB16,
        internal const uint EVERYTHING3_PROPERTY_ID_INDEX_ONLINE = 327; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_0 = 328; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_1 = 329; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_2 = 330; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_3 = 331; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_4 = 332; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_5 = 333; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_6 = 334; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_7 = 335; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_8 = 336; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_9 = 337; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_A = 338; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_B = 339; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_C = 340; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_D = 341; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_E = 342; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_COLUMN_F = 343; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_ZONE_ID = 344; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_REFERRER_URL = 345; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_HOST_URL = 346; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_CHARACTER_ENCODING = 347; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE_GET_TEXT,
        internal const uint EVERYTHING3_PROPERTY_ID_ROOT_NAME = 348; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_USED_DISK_SIZE = 349; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_VOLUME_PATH = 350; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_MAX_CHILD_DEPTH = 351; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_TOTAL_CHILD_SIZE = 352; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_ROW = 353; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_CHILD_OCCURRENCE_COUNT = 354; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_VOLUME_NAME = 355; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DESCENDANT_OCCURRENCE_COUNT = 356; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_OBJECT_ID = 357; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_BIRTH_VOLUME_ID = 358; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_BIRTH_OBJECT_ID = 359; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_DOMAIN_ID = 360; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_CRC32 = 361; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_CRC64 = 362; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_MD5 = 363; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_SHA1 = 364; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_SHA256 = 365; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_SHA512 = 366; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_CRC32 = 367; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_CRC64 = 368; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_MD5 = 369; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_SHA1 = 370; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_SHA256 = 371; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_SHA512 = 372; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_CRC32 = 373; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_CRC64 = 374; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_MD5 = 375; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_SHA1 = 376; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_SHA256 = 377; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_SHA512 = 378; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_CRC32_FROM_DISK = 379; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_CRC64_FROM_DISK = 380; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_MD5_FROM_DISK = 381; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_SHA1_FROM_DISK = 382; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_SHA256_FROM_DISK = 383; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_SHA512_FROM_DISK = 384; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_CRC32_FROM_DISK = 385; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_CRC64_FROM_DISK = 386; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_MD5_FROM_DISK = 387; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_SHA1_FROM_DISK = 388; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_SHA256_FROM_DISK = 389; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_DATA_AND_NAMES_SHA512_FROM_DISK = 390; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_CRC32_FROM_DISK = 391; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_CRC64_FROM_DISK = 392; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_MD5_FROM_DISK = 393; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_SHA1_FROM_DISK = 394; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_SHA256_FROM_DISK = 395; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_FOLDER_NAMES_SHA512_FROM_DISK = 396; // EVERYTHING3_PROPERTY_VALUE_TYPE_BLOB8,
        internal const uint EVERYTHING3_PROPERTY_ID_LONG_NAME = 397; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_LONG_PATH_AND_NAME = 398; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DIGITAL_SIGNATURE_NAME = 399; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DIGITAL_SIGNATURE_TIMESTAMP = 400; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_AUDIO_TRACK_COUNT = 401; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_VIDEO_TRACK_COUNT = 402; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_SUBTITLE_TRACK_COUNT = 403; // EVERYTHING3_PROPERTY_VALUE_TYPE_DWORD,
        internal const uint EVERYTHING3_PROPERTY_ID_NETWORK_INDEX_HOST = 404; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_STRING_REFERENCE,
        internal const uint EVERYTHING3_PROPERTY_ID_ORIGINAL_LOCATION = 405; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_DATE_DELETED = 406; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_STATUS = 407; // EVERYTHING3_PROPERTY_VALUE_TYPE_BYTE,
        internal const uint EVERYTHING3_PROPERTY_ID_VORBIS_COMMENT = 408; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_QUICKTIME_METADATA = 409; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING_MULTISTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_PARENT_SIZE = 410; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_ROOT_SIZE = 411; // EVERYTHING3_PROPERTY_VALUE_TYPE_UINT64,
        internal const uint EVERYTHING3_PROPERTY_ID_OPENS_WITH = 412; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_RANDOMIZE = 413; // EVERYTHING3_PROPERTY_VALUE_TYPE_SIZE_T,
        internal const uint EVERYTHING3_PROPERTY_ID_ICON = 414; // EVERYTHING3_PROPERTY_VALUE_TYPE_NULL, 
        internal const uint EVERYTHING3_PROPERTY_ID_THUMBNAIL = 415; // EVERYTHING3_PROPERTY_VALUE_TYPE_NULL,
        internal const uint EVERYTHING3_PROPERTY_ID_CONTENT = 416; // EVERYTHING3_PROPERTY_VALUE_TYPE_PSTRING,
        internal const uint EVERYTHING3_PROPERTY_ID_SEPARATOR = 417; // EVERYTHING3_PROPERTY_VALUE_TYPE_NULL,
        internal const uint EVERYTHING3_PROPERTY_ID_BUILTIN_COUNT = 418; // total number of built-in properties,
        #endregion
    }
}