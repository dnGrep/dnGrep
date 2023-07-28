using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dnGREP.Common.IO
{
    /// <summary>
    /// Re-implementation of some AlphaFS Directory extensions 
    /// </summary>
    public static class DirectoryEx
    {
        /// <summary>Determines whether the given directory is empty; i.e. it contains no files and no subdirectories.</summary>
        /// <returns>
        ///   <para>Returns <c>true</c> when the directory contains no file system objects.</para>
        ///   <para>Returns <c>false</c> when directory contains at least one file system object.</para>
        /// </returns>
        /// <param name="directoryPath">The path to the directory.</param>
        public static bool IsEmpty(string directoryPath)
        {
            return !Directory.EnumerateFileSystemEntries(directoryPath, "*").Any();
        }

        ///// <summary>[AlphaFS] Checks if specified <paramref name="path"/> is a local- or network drive.</summary>
        ///// <param name="path">The path to check, such as: "C:" or "\\server\c$".</param>
        ///// <returns><c>true</c> if the drive exists, <c>false</c> otherwise.</returns>
        //public static bool ExistsDrive(string path)
        //{
        //    string pathRoot = Path.GetPathRoot(path);
        //    return !string.IsNullOrEmpty(pathRoot);
        //}

        /// <summary>
        /// Copies the directory recursively.
        /// </summary>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationDirectory"></param>
        public static void Copy(string sourceDirectory, string destinationDirectory)
        {
            Utils.CopyFiles(sourceDirectory, destinationDirectory, string.Empty, string.Empty);
        }

        /// <summary>Returns the volume information, root information, or both for the specified path.</summary>
        /// <returns>The volume information, root information, or both for the specified path, or <c>null</c> if <paramref name="path"/> path does not contain root directory information.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="path">The path of a file or directory.</param>
        internal static string? GetDirectoryRoot(string path)
        {
            var pathLp = PathEx.GetRegularPath(path);

            var rootPath = Path.GetPathRoot(pathLp);

            return string.IsNullOrWhiteSpace(rootPath) ? null : rootPath;
        }

        /// <summary>[AlphaFS] Returns an enumerable collection of file names in a specified <paramref name="path"/>.</summary>
        /// <returns>An enumerable collection of the full names (including paths) for the files in the directory specified by <paramref name="path"/>.</returns>
        /// <param name="path">The directory to search.</param>
        /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
        /// <param name="filters">The specification of custom filters to be used in the process.</param>
        internal static IEnumerable<string> EnumerateFiles(string path, DirectoryEnumerationOptions fileOptions, DirectoryEnumerationFilters fileFilters)
        {
            return EnumerateFileSystemEntryInfosCore<string>(false, path, fileOptions, fileFilters);
        }

        internal static IEnumerable<string> EnumerateDirectories(string path, DirectoryEnumerationOptions dirOptions, DirectoryEnumerationFilters dirFilters)
        {
            return EnumerateFileSystemEntryInfosCore<string>(true, path, dirOptions, dirFilters);
        }

        /// <summary>[AlphaFS] Returns an enumerable collection of file system entries in a specified path using <see cref="DirectoryEnumerationOptions"/> and <see cref="DirectoryEnumerationFilters"/>.</summary>
        /// <returns>The matching file system entries. The type of the items is determined by the type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <typeparam name="T">The type to return. This may be one of the following types:
        ///    <list type="definition">
        ///    <item>
        ///       <term><see cref="FileSystemEntryInfo"/></term>
        ///       <description>This method will return instances of <see cref="FileSystemEntryInfo"/> instances.</description>
        ///    </item>
        ///    <item>
        ///       <term><see cref="FileSystemInfo"/></term>
        ///       <description>This method will return instances of <see cref="DirectoryInfo"/> and <see cref="FileInfo"/> instances.</description>
        ///    </item>
        ///    <item>
        ///       <term><see cref="string"/></term>
        ///       <description>This method will return the full path of each item.</description>
        ///    </item>
        /// </list>
        /// </typeparam>
        /// <param name="onlyFolders"></param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="path">The directory to search.</param>
        /// <param name="searchPattern">
        ///    The search string to match against the names of directories in <paramref name="path"/>.
        ///    This parameter can contain a combination of valid literal path and wildcard
        ///    (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
        /// </param>
        /// <param name="searchOption"></param>
        /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
        /// <param name="filters">The specification of custom filters to be used in the process.</param>
        /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
        internal static IEnumerable<T> EnumerateFileSystemEntryInfosCore<T>(bool? onlyFolders, string path, DirectoryEnumerationOptions options, DirectoryEnumerationFilters filters)
        {
            if (null != onlyFolders)
            {
                // Adhere to the method name by validating the DirectoryEnumerationOptions value.
                // For example, method Directory.EnumerateDirectories() should only return folders
                // and method Directory.EnumerateFiles() should only return files.


                // Folders only.
                if ((bool)onlyFolders)
                {
                    options &= ~DirectoryEnumerationOptions.Files;  // Remove enumeration of files.
                    options |= DirectoryEnumerationOptions.Folders; // Add enumeration of folders.
                }

                // Files only.
                else
                {
                    options &= ~DirectoryEnumerationOptions.Folders; // Remove enumeration of folders.
                    options |= DirectoryEnumerationOptions.Files;    // Add enumeration of files.
                }
            }


            return new FindFileSystemEntryInfo(true, path, options, filters, typeof(T)).Enumerate<T>();
        }

    }
}
