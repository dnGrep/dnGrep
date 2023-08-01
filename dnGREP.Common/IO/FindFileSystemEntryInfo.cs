/*  Copyright (C) 2008-2018 Peter Palotas, Jeffrey Jangli, Alexandr Normuradov
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy 
 *  of this software and associated documentation files (the "Software"), to deal 
 *  in the Software without restriction, including without limitation the rights 
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 *  copies of the Software, and to permit persons to whom the Software is 
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 *  THE SOFTWARE. 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace dnGREP.Common.IO
{
    /// <summary>Modified from AlphaFS: Class that retrieves file system entries (i.e. files and directories) using Win32 API FindFirst()/FindNext().</summary>
    internal sealed class FindFileSystemEntryInfo
    {
        #region Constructor

        /// <summary>Initializes a new instance of the <see cref="FindFileSystemEntryInfo"/> class.</summary>
        /// <param name="isFolder">if set to <c>true</c> the path is a folder.</param>
        /// <param name="path">The path.</param>
        /// <param name="options">The enumeration options.</param>
        /// <param name="customFilters">The custom filters.</param>
        /// <param name="typeOfT">The type of objects to be retrieved.</param>
        public FindFileSystemEntryInfo(bool isFolder, string path, DirectoryEnumerationOptions options, DirectoryEnumerationFilters customFilters, Type typeOfT)
        {
            OriginalInputPath = path;

            InputPath = PathEx.GetLongPath(path).TrimEnd(Path.DirectorySeparatorChar);

            IsRelativePath = !Path.IsPathRooted(OriginalInputPath);

            RelativeAbsolutePrefix = IsRelativePath ? InputPath.Replace(OriginalInputPath, string.Empty, StringComparison.Ordinal) : string.Empty;

            FileSystemObjectType = null;

            ContinueOnException = (options & DirectoryEnumerationOptions.ContinueOnException) != 0;

            AsLongPath = (options & DirectoryEnumerationOptions.AsLongPath) != 0;

            AsString = typeOfT == typeof(string);
            AsFileSystemInfo = !AsString && (typeOfT == typeof(FileSystemInfo) || typeOfT.BaseType == typeof(FileSystemInfo));

            LargeCache = (options & DirectoryEnumerationOptions.LargeCache) != 0 ? FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH : 0;

            // Only FileSystemEntryInfo makes use of (8.3) AlternateFileName.
            FindExInfoLevel = AsString || AsFileSystemInfo || (options & DirectoryEnumerationOptions.BasicSearch) != 0 ? FINDEX_INFO_LEVELS.FindExInfoBasic : FINDEX_INFO_LEVELS.FindExInfoStandard;


            if (null != customFilters)
            {
                InclusionFilter = customFilters.InclusionFilter;

                RecursionFilter = customFilters.RecursionFilter;

                ErrorHandler = customFilters.ErrorFilter;

                PauseCancelToken = customFilters.PauseCancelToken;
            }


            if (isFolder)
            {
                IsDirectory = true;

                Recursive = (options & DirectoryEnumerationOptions.Recursive) != 0 || null != RecursionFilter;

                SkipReparsePoints = (options & DirectoryEnumerationOptions.SkipReparsePoints) != 0;


                // Need folders or files to enumerate.
                if ((options & DirectoryEnumerationOptions.FilesAndFolders) == 0)
                    options |= DirectoryEnumerationOptions.FilesAndFolders;
            }

            else
            {
                options &= ~DirectoryEnumerationOptions.Folders; // Remove enumeration of folders.
                options |= DirectoryEnumerationOptions.Files; // Add enumeration of files.
            }


            FileSystemObjectType = (options & DirectoryEnumerationOptions.FilesAndFolders) == DirectoryEnumerationOptions.FilesAndFolders

               // Folders and files (null).
               ? null

               // Only folders (true) or only files (false).
               : (options & DirectoryEnumerationOptions.Folders) != 0;
        }

        #endregion // Constructor


        #region Properties

        /// <summary>Gets or sets the ability to return the object as a <see cref="FileSystemInfo"/> instance.</summary>
        /// <value><c>true</c> returns the object as a <see cref="FileSystemInfo"/> instance.</value>
        public bool AsFileSystemInfo { get; private set; }


        /// <summary>Gets or sets the ability to return the full path in long full path format.</summary>
        /// <value><c>true</c> returns the full path in long full path format, <c>false</c> returns the full path in regular path format.</value>
        public bool AsLongPath { get; private set; }


        /// <summary>Gets or sets the ability to return the object instance as a <see cref="string"/>.</summary>
        /// <value><c>true</c> returns the full path of the object as a <see cref="string"/></value>
        public bool AsString { get; private set; }


        /// <summary>Gets or sets the ability to skip on access errors.</summary>
        /// <value><c>true</c> suppress any Exception that might be thrown as a result from a failure, such as ACLs protected directories or non-accessible reparse points.</value>
        public bool ContinueOnException { get; private set; }


        /// <summary>Gets the file system object type.</summary>
        /// <value>
        /// <c>null</c> = Return files and directories.
        /// <c>true</c> = Return only directories.
        /// <c>false</c> = Return only files.
        /// </value>
        public bool? FileSystemObjectType { get; private set; }


        /// <summary>Gets or sets if the path is an absolute or relative path.</summary>
        /// <value>Gets a value indicating whether the specified path string contains absolute or relative path information.</value>
        public bool IsRelativePath { get; private set; }


        /// <summary>Gets or sets the initial path to the folder.</summary>
        /// <value>The initial path to the file or folder in long path format.</value>
        public string OriginalInputPath { get; private set; }


        /// <summary>Gets or sets the path to the folder.</summary>
        /// <value>The path to the file or folder in long path format.</value>
        public string InputPath { get; private set; }


        /// <summary>Gets or sets the absolute full path prefix of the relative path.</summary>
        private string RelativeAbsolutePrefix { get; set; }


        /// <summary>Gets or sets a value indicating which <see cref="FINDEX_INFO_LEVELS"/> to use.</summary>
        /// <value><c>true</c> indicates a folder object, <c>false</c> indicates a file object.</value>
        public bool IsDirectory { get; private set; }


        /// <summary>Uses a larger buffer for directory queries, which can increase performance of the find operation.</summary>
        /// <remarks>This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
        public FIND_FIRST_EX_FLAGS LargeCache { get; private set; }


        /// <summary>The FindFirstFileEx function does not query the short file name, improving overall enumeration speed.</summary>
        /// <remarks>This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
        public FINDEX_INFO_LEVELS FindExInfoLevel { get; private set; }


        /// <summary>Specifies whether the search should include only the current directory or should include all subdirectories.</summary>
        /// <value><c>true</c> to include all subdirectories.</value>
        public bool Recursive { get; private set; }


        /// <summary><c>true</c> skips ReparsePoints, <c>false</c> will follow ReparsePoints.</summary>
        public bool SkipReparsePoints { get; private set; }


        /// <summary>Gets or sets the custom enumeration in/exclusion filter.</summary>
        /// <value>The method determining if the object should be in/excluded from the output or not.</value>
        public Predicate<FileSystemEntryInfo>? InclusionFilter { get; private set; }


        /// <summary>Gets or sets the custom enumeration recursion filter.</summary>
        /// <value>The method determining if the directory should be recursively traversed or not.</value>
        public Predicate<FileSystemEntryInfo>? RecursionFilter { get; private set; }


        /// <summary>Gets or sets the handler of errors that may occur.</summary>
        /// <value>The error handler method.</value>
        public ErrorHandler? ErrorHandler { get; private set; }


        /// <summary>Gets or sets the cancellation token to abort the enumeration.</summary>
        /// <value>A <see cref="PauseCancelToken"/> instance.</value>
        private PauseCancelToken PauseCancelToken { get; set; }

        #endregion // Properties


        #region Methods

        unsafe private FindCloseSafeHandle? FindFirstFile(string pathLp, out WIN32_FIND_DATAW win32FindData, out uint lastError, bool suppressException = false)
        {

            lastError = (uint)WIN32_ERROR.NO_ERROR;

            string searchFilter = "*";
            var searchOption = null != FileSystemObjectType && (bool)FileSystemObjectType ? FINDEX_SEARCH_OPS.FindExSearchLimitToDirectories : FINDEX_SEARCH_OPS.FindExSearchNameMatch;
            FindCloseSafeHandle? handle = null;

            win32FindData = new();

            fixed (void* lpFindFileData = &win32FindData, lpSearchFilter = searchFilter)
            {
                handle = PInvoke.FindFirstFileEx(pathLp, FindExInfoLevel, lpFindFileData, searchOption, lpSearchFilter, LargeCache);
                lastError = (uint)Marshal.GetLastWin32Error();
            }

            if (!suppressException && !ContinueOnException)
            {
                if (null == handle)
                {
                    switch (lastError)
                    {
                        case (uint)WIN32_ERROR.ERROR_FILE_NOT_FOUND: // FileNotFoundException.
                        case (uint)WIN32_ERROR.ERROR_PATH_NOT_FOUND: // DirectoryNotFoundException.
                        case (uint)WIN32_ERROR.ERROR_NOT_READY:      // DeviceNotReadyException: Floppy device or network drive not ready.

                            ExistsDriveOrFolderOrFile(pathLp, IsDirectory, lastError, true, true);
                            break;
                    }


                    ThrowPossibleException(lastError, pathLp);
                }
            }

            return handle;
        }


        private FileSystemEntryInfo NewFilesystemEntry(string pathLp, string fileName, WIN32_FIND_DATAW win32FindData)
        {
            var fullPath = (IsRelativePath ? pathLp.Replace(RelativeAbsolutePrefix, string.Empty, StringComparison.Ordinal) : pathLp) + fileName;

            return new FileSystemEntryInfo(win32FindData) { FullPath = fullPath };
        }


        private T? NewFileSystemEntryType<T>(bool isFolder, FileSystemEntryInfo fsei, string fileName, string pathLp, WIN32_FIND_DATAW win32FindData)
        {
            // Determine yield, e.g. don't return files when only folders are requested and vice versa.

            if (null != FileSystemObjectType && (!(bool)FileSystemObjectType || !isFolder) && (!(bool)!FileSystemObjectType || isFolder))

                return (T?)(object?)null;


            if (null == fsei)
                fsei = NewFilesystemEntry(pathLp, fileName, win32FindData);


            // Return object instance FullPath property as string, optionally in long path format.

            return AsString ? null == InclusionFilter || InclusionFilter(fsei) ? (T)(object)(AsLongPath ? fsei.LongFullPath : fsei.FullPath) : (T?)(object?)null


               // Make sure the requested file system object type is returned.
               // null = Return files and directories.
               // true = Return only directories.
               // false = Return only files.

               : null != InclusionFilter && !InclusionFilter(fsei)
                  ? (T?)(object?)null

                  // Return object instance of type FileSystemInfo.

                  : AsFileSystemInfo
                     ? (T)(object)(fsei.IsDirectory

                        ? (FileSystemInfo)new DirectoryInfo(fsei.LongFullPath)// { EntryInfo = fsei }

                        : new FileInfo(fsei.LongFullPath)) //{ EntryInfo = fsei })

                     // Return object instance of type FileSystemEntryInfo.

                     : (T)(object)fsei;
        }


        /// <summary>Gets an enumerator that returns all of the file system objects that match both the wildcards that are in any of the directories to be searched and the custom predicate.</summary>
        /// <returns>An <see cref="IEnumerable{T}"/> instance: FileSystemEntryInfo, DirectoryInfo, FileInfo or string (full path).</returns>
        public IEnumerable<T> Enumerate<T>()
        {
            // MSDN: Queue
            // Represents a first-in, first-out collection of objects.
            // The capacity of a Queue is the number of elements the Queue can hold.
            // As elements are added to a Queue, the capacity is automatically increased as required through reallocation. The capacity can be decreased by calling TrimToSize.
            // The growth factor is the number by which the current capacity is multiplied when a greater capacity is required. The growth factor is determined when the Queue is constructed.
            // The capacity of the Queue will always increase by a minimum value, regardless of the growth factor; a growth factor of 1.0 will not prevent the Queue from increasing in size.
            // If the size of the collection can be estimated, specifying the initial capacity eliminates the need to perform a number of resizing operations while adding elements to the Queue.
            // This constructor is an O(n) operation, where n is capacity.

            var dirs = new Queue<string>(4096);

            var path = PathEx.AddTrailingDirectorySeparator(InputPath);
            if (path != null)
            {
                dirs.Enqueue(path);
            }

            while (dirs.Count > 0 && !PauseCancelToken.IsCancellationRequested)
            {
                PauseCancelToken.WaitWhilePaused();

                // Removes the object at the beginning of your Queue.
                // The algorithmic complexity of this is O(1). It doesn't loop over elements.

                var pathLp = dirs.Dequeue();

                using var handle = FindFirstFile(pathLp + PathEx.WildcardStarMatchAll, out WIN32_FIND_DATAW win32FindData, out uint lastError);

                // When the handle is null and we are still here, it means the ErrorHandler is active.
                // We hit an inaccessible folder, so break and continue with the next one.
                if (null == handle)
                    continue;

                do
                {
                    PauseCancelToken.WaitWhilePaused();

                    if (lastError == (uint)WIN32_ERROR.ERROR_NO_MORE_FILES)
                    {
                        lastError = (uint)WIN32_ERROR.NO_ERROR;
                        continue;
                    }


                    // Skip reparse points here to cleanly separate regular directories from links.
                    if (SkipReparsePoints && (win32FindData.dwFileAttributes & (uint)FileAttributes.ReparsePoint) != 0)
                        continue;


                    var fileName = win32FindData.cFileName.ToString();

                    var isFolder = (win32FindData.dwFileAttributes & (uint)FileAttributes.Directory) != 0;

                    // Skip entries ".." and "."
                    if (isFolder && (fileName.Equals(PathEx.ParentDirectoryPrefix, StringComparison.Ordinal) || fileName.Equals(PathEx.CurrentDirectoryPrefix, StringComparison.Ordinal)))
                        continue;


                    var fsei = NewFilesystemEntry(pathLp, fileName, win32FindData);

                    var res = NewFileSystemEntryType<T>(isFolder, fsei, fileName, pathLp, win32FindData);


                    // If recursion is requested, add it to the queue for later traversal.
                    if (isFolder && Recursive && (null == RecursionFilter || RecursionFilter(fsei)))
                    {
                        path = PathEx.AddTrailingDirectorySeparator(pathLp + fileName);
                        if (path != null)
                        {
                            dirs.Enqueue(path);
                        }
                    }

                    // Codacy: When constraints have not been applied to restrict a generic type parameter to be a reference type, then a value type,
                    // such as a struct, could also be passed. In such cases, comparing the type parameter to null would always be false,
                    // because a struct can be empty, but never null. If a value type is truly what's expected, then the comparison should use default().
                    // If it's not, then constraints should be added so that no value type can be passed.

                    if (Equals(res, default(T)))
                        continue;

                    if (res != null)
                    {
                        yield return res;
                    }

                } while (!PauseCancelToken.IsCancellationRequested &&
                   PInvoke.FindNextFile(handle, out win32FindData));


                lastError = (uint)Marshal.GetLastWin32Error();

                if (!ContinueOnException && !PauseCancelToken.IsCancellationRequested)
                    ThrowPossibleException(lastError, pathLp);
            }
        }


        private void ThrowPossibleException(uint lastError, string pathLp)
        {
            switch (lastError)
            {
                case (uint)WIN32_ERROR.ERROR_NO_MORE_FILES:
                    lastError = (uint)WIN32_ERROR.NO_ERROR;
                    break;


                case (uint)WIN32_ERROR.ERROR_FILE_NOT_FOUND: // On files.
                case (uint)WIN32_ERROR.ERROR_PATH_NOT_FOUND: // On folders.
                case (uint)WIN32_ERROR.ERROR_NOT_READY:      // DeviceNotReadyException: Floppy device or network drive not ready.
                                                             // MSDN: .NET 3.5+: DirectoryNotFoundException: Path is invalid, such as referring to an unmapped drive.
                                                             // Directory.Delete()

                    lastError = IsDirectory ? (uint)WIN32_ERROR.ERROR_PATH_NOT_FOUND : (uint)WIN32_ERROR.ERROR_FILE_NOT_FOUND;
                    break;
            }

            if (lastError != (uint)WIN32_ERROR.NO_ERROR)
            {
                var regularPath = PathEx.GetRegularPath(pathLp);

                // Pass control to the ErrorHandler when set.

                if (null == ErrorHandler || !ErrorHandler((int)lastError, new Win32Exception((int)lastError).Message, regularPath))
                {
                    // When the ErrorHandler returns false, thrown the Exception.

                    ThrowException(lastError, regularPath);
                }
            }
        }

        /// <summary>[AlphaFS] Checks if specified <paramref name="path"/> is a local- or network drive.</summary>
        /// <returns><c>true</c> if the drive exists, <c>false</c> otherwise.</returns>
        private static bool ExistsDriveOrFolderOrFile(string path, bool isFolder, uint lastError, bool throwIfDriveNotExists, bool throwIfFolderOrFileNotExists)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var drive = DirectoryEx.GetDirectoryRoot(path);

            var driveExists = null != drive && Directory.Exists(drive);

            var regularPath = PathEx.GetRegularPath(path);


            if (!driveExists && throwIfDriveNotExists || lastError == (uint)WIN32_ERROR.ERROR_NOT_READY)
                throw new Exception($"Device not ready: {drive}"); //new DeviceNotReadyException(drive, true);


            throwIfFolderOrFileNotExists = throwIfFolderOrFileNotExists && lastError != (uint)WIN32_ERROR.NO_ERROR;

            if (throwIfFolderOrFileNotExists)
            {
                if (lastError != (uint)WIN32_ERROR.NO_ERROR)
                {
                    if (lastError == (uint)WIN32_ERROR.ERROR_PATH_NOT_FOUND)
                        throw new DirectoryNotFoundException(regularPath);


                    if (lastError == (uint)WIN32_ERROR.ERROR_FILE_NOT_FOUND)
                    {
                        if (isFolder)
                            throw new DirectoryNotFoundException(regularPath);

                        throw new FileNotFoundException(regularPath);
                    }
                }
            }


            return driveExists;
        }

        private static void ThrowException(uint errorCode, string readPath)
        {
            if (null != readPath)
                readPath = PathEx.GetRegularPath(readPath);

            var errorMessage = string.Format(CultureInfo.InvariantCulture, "({0}) {1}.", errorCode, new Win32Exception((int)errorCode).Message.Trim().TrimEnd('.').Trim());


            if (!string.IsNullOrWhiteSpace(readPath))
                errorMessage = string.Format(CultureInfo.InvariantCulture, "{0} | Read: [{1}]", errorMessage, readPath);

            else
            {
                // Prevent messages like: "(87) The parameter is incorrect: []"
                if (!string.IsNullOrWhiteSpace(readPath))
                    errorMessage = string.Format(CultureInfo.InvariantCulture, "{0}: [{1}]", errorMessage.TrimEnd('.'), readPath);
            }


            throw errorCode switch
            {
                (uint)WIN32_ERROR.ERROR_INVALID_DRIVE => new DriveNotFoundException(errorMessage),
                (uint)WIN32_ERROR.ERROR_OPERATION_ABORTED => new OperationCanceledException(errorMessage),
                (uint)WIN32_ERROR.ERROR_FILE_NOT_FOUND => new FileNotFoundException(errorMessage),
                (uint)WIN32_ERROR.ERROR_PATH_NOT_FOUND => new DirectoryNotFoundException(errorMessage),
                (uint)WIN32_ERROR.ERROR_BAD_RECOVERY_POLICY => new System.Security.Policy.PolicyException(errorMessage),
                (uint)WIN32_ERROR.ERROR_FILE_READ_ONLY or (uint)WIN32_ERROR.ERROR_ACCESS_DENIED or (uint)WIN32_ERROR.ERROR_NETWORK_ACCESS_DENIED => new UnauthorizedAccessException(errorMessage),
                (uint)WIN32_ERROR.ERROR_ALREADY_EXISTS or (uint)WIN32_ERROR.ERROR_FILE_EXISTS => new Exception($"{readPath} already exists"),
                (uint)WIN32_ERROR.ERROR_DIR_NOT_EMPTY => new Exception($"Directory not empty: {errorMessage}"),
                (uint)WIN32_ERROR.ERROR_NOT_READY => new Exception($"Device not ready: {errorMessage}"),
                (uint)WIN32_ERROR.ERROR_NOT_SAME_DEVICE => new Exception($"Not same Device: {errorMessage}"),
                // We should really never get here, throwing an exception for a successful operation.
                (uint)WIN32_ERROR.ERROR_SUCCESS or (uint)WIN32_ERROR.ERROR_SUCCESS_REBOOT_INITIATED or (uint)WIN32_ERROR.ERROR_SUCCESS_REBOOT_REQUIRED or (uint)WIN32_ERROR.ERROR_SUCCESS_RESTART_REQUIRED => new NotImplementedException(string.Format(CultureInfo.InvariantCulture, "Incorrectly implemented function attempting to generate exception from successful operation {0}", errorMessage)),
                // We don't have a specific exception to generate for this error.
                _ => new IOException(errorMessage, GetHrFromWin32Error(errorCode)),
            };
        }

        private static int GetHrFromWin32Error(uint errorCode)
        {
            return (int)unchecked((int)0x80070000 | errorCode);
        }
        #endregion // Methods
    }
}
