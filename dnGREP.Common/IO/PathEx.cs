using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace dnGREP.Common.IO
{
    /// <summary>
    /// Re-implementation of some AlphaFS Path extensions 
    /// </summary>
    public static class PathEx
    {
        /// <summary>Retrieves the short path form of the specified path.</summary>
        /// <returns>A path that has the 8.3 path form.</returns>
        /// <remarks>Will fail on NTFS volumes with disabled 8.3 name generation.</remarks>
        /// <remarks>The path must actually exist to be able to get the short path name.</remarks>
        /// <param name="path">An existing path to a folder or file.</param>
        unsafe public static string GetShort83Path(string path)
        {
            uint num = PInvoke.GetShortPathName(path, null, 0u);
            if (num > 0)
            {
                string buffer = new(' ', (int)num);
                fixed (char* p = buffer)
                {
                    num = PInvoke.GetShortPathName(path, new(p), num);
                }
                int lastWin32Error = Marshal.GetLastWin32Error();
                if (num != 0)
                {
                    return buffer.Replace('\0', ' ').Trim();
                }
            }

            return string.Empty;
        }

        /// <summary>[AlphaFS] CurrentDirectoryPrefix = "." Provides a current directory string.</summary>
        public static readonly string CurrentDirectoryPrefix = ".";

        /// <summary>[AlphaFS] ParentDirectoryPrefix = ".." Provides a parent directory string.</summary>
        public const string ParentDirectoryPrefix = "..";

        /// <summary>[AlphaFS] WildcardStarMatchAll = '*' Provides a match-all-items character.</summary>
        public const char WildcardStarMatchAllChar = '*';

        /// <summary>[AlphaFS] WildcardStarMatchAll = "*" Provides a match-all-items string.</summary>
        public static readonly string WildcardStarMatchAll = WildcardStarMatchAllChar.ToString(CultureInfo.InvariantCulture);

        /// <summary>[AlphaFS] WildcardQuestion = '?' Provides a replace-item string.</summary>
        public const char WildcardQuestionChar = '?';

        /// <summary>[AlphaFS] WildcardQuestion = "?" Provides a replace-item string.</summary>
        public static readonly string WildcardQuestion = WildcardQuestionChar.ToString(CultureInfo.InvariantCulture);

        /// <summary>[AlphaFS] Win32 File Namespace. The "\\?\" prefix to a path string tells the Windows APIs to disable all string parsing and to send the string that follows it straight to the file system.</summary>
        public static readonly string LongPathPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{0}{1}{0}", Path.DirectorySeparatorChar, WildcardQuestion);

        /// <summary>[AlphaFS] Win32 Device Namespace. The "\\.\"prefix is how to access physical disks and volumes, without going through the file system, if the API supports this type of access.</summary>
        public static readonly string LogicalDrivePrefix = string.Format(CultureInfo.InvariantCulture, "{0}{0}.{0}", Path.DirectorySeparatorChar);

        /// <summary>[AlphaFS] NonInterpretedPathPrefix = "\??\" Provides a non-interpreted path prefix.</summary>
        public static readonly string NonInterpretedPathPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{1}{0}", Path.DirectorySeparatorChar, WildcardQuestion);

        /// <summary>[AlphaFS] UncPrefix = "\\" Provides standard Windows Path UNC prefix.</summary>
        public static readonly string UncPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{0}", Path.DirectorySeparatorChar);

        /// <summary>[AlphaFS] LongPathUncPrefix = "\\?\UNC\" Provides standard Windows Long Path UNC prefix.</summary>
        public static readonly string LongPathUncPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", LongPathPrefix, "UNC", Path.DirectorySeparatorChar);
        
        /// <summary>[AlphaFS] DosDeviceUncPrefix = "\??\UNC\" Provides a SUBST.EXE Path UNC prefix to a network share.</summary>
        public static readonly string DosDeviceUncPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", NonInterpretedPathPrefix, "UNC", Path.DirectorySeparatorChar);

        /// <summary>[AlphaFS] GlobalRootPrefix = "\\?\GlobalRoot\" Provides standard Windows Volume prefix.</summary>
        public static readonly string GlobalRootPrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", LongPathPrefix, "GlobalRoot", Path.DirectorySeparatorChar);

        /// <summary>[AlphaFS] VolumePrefix = "\\?\Volume" Provides standard Windows Volume prefix.</summary>
        public static readonly string VolumePrefix = string.Format(CultureInfo.InvariantCulture, "{0}{1}", LongPathPrefix, "Volume");


        /// <summary>[AlphaFS] Makes an extended long path from the specified <paramref name="path"/> by prefixing <see cref="LongPathPrefix"/>.</summary>
        /// <returns>The <paramref name="path"/> prefixed with a <see cref="LongPathPrefix"/>, the minimum required full path is: "C:\".</returns>
        /// <remarks>This method does not verify that the resulting path and file name are valid, or that they see an existing file on the associated volume.</remarks>
        /// <exception cref="ArgumentException"/>
        /// <param name="path">The path to the file or directory, this can also be an UNC path.</param>
        public static string GetLongPath(string path)
        {
            if (path.Trim().Length == 0)
            {
                throw new ArgumentException("Argument is empty", nameof(path));
            }

            if (path.Length <= 2 || path.StartsWith(LongPathPrefix, StringComparison.Ordinal) || path.StartsWith(LogicalDrivePrefix, StringComparison.Ordinal) || path.StartsWith(NonInterpretedPathPrefix, StringComparison.Ordinal))
            {
                return path;
            }

            if (path.StartsWith(UncPrefix, StringComparison.Ordinal))
            {
                return string.Concat(LongPathUncPrefix, path.AsSpan(UncPrefix.Length));
            }

            if (!Path.IsPathRooted(path) || !IsLogicalDrive(path))
            {
                return path;
            }

            return LongPathPrefix + path;
        }


#pragma warning disable IDE0057
        /// <summary>Gets the regular path from a long path.</summary>
        /// <returns>
        ///   Returns the regular form of a long <paramref name="path"/>.
        ///   For example: "\\?\C:\Temp\file.txt" to: "C:\Temp\file.txt", or: "\\?\UNC\Server\share\file.txt" to: "\\Server\share\file.txt".
        /// </returns>
        /// <remarks>
        ///   MSDN: String.TrimEnd Method notes to Callers: http://msdn.microsoft.com/en-us/library/system.string.trimend%28v=vs.110%29.aspx
        /// </remarks>
        /// <exception cref="ArgumentException"/>
        /// <param name="path">The path.</param>
        internal static string GetRegularPath(string path)
        {
            if (path.Trim().Length == 0 || string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is zero length or only whitespace", nameof(path));

            if (path.StartsWith(DosDeviceUncPrefix, StringComparison.OrdinalIgnoreCase))
                return string.Concat(UncPrefix, path.AsSpan(DosDeviceUncPrefix.Length));


            if (path.StartsWith(LogicalDrivePrefix, StringComparison.Ordinal))
                return path.Substring(LogicalDrivePrefix.Length);


            if (path.StartsWith(NonInterpretedPathPrefix, StringComparison.Ordinal))
                return path.Substring(NonInterpretedPathPrefix.Length);


            return path.StartsWith(GlobalRootPrefix, StringComparison.OrdinalIgnoreCase) || path.StartsWith(VolumePrefix, StringComparison.OrdinalIgnoreCase) ||
                   !path.StartsWith(LongPathPrefix, StringComparison.Ordinal)

               ? path
               : (path.StartsWith(LongPathUncPrefix, StringComparison.OrdinalIgnoreCase) ? string.Concat(UncPrefix, path.AsSpan(LongPathUncPrefix.Length)) : path.Substring(LongPathPrefix.Length));
        }

        /// <summary>[AlphaFS] Checks if <paramref name="path"/> is in a logical drive format, such as "C:", "D:".</summary>
        /// <returns>true when <paramref name="path"/> is in a logical drive format, such as "C:", "D:".</returns>
        /// <exception cref="ArgumentException"/>
        /// <param name="path">The absolute path to check.</param>
        public static bool IsLogicalDrive(string path)
        {
            string obj = path.StartsWith(LogicalDrivePrefix, StringComparison.OrdinalIgnoreCase) ? path.Substring(LogicalDrivePrefix.Length) : path;
            char c = obj.ToUpperInvariant()[0];
            if (obj[1] == Path.VolumeSeparatorChar && c >= 'A')
            {
                return c <= 'Z';
            }

            return false;
        }
#pragma warning restore IDE0057

        /// <summary>Adds a trailing <see cref="DirectorySeparatorChar"/> character to the string, when absent.</summary>
        /// <returns>A text string with a trailing <see cref="DirectorySeparatorChar"/> character. The function returns <c>null</c> when <paramref name="path"/> is <c>null</c>.</returns>
        /// <param name="path">A text string to which the trailing <see cref="DirectorySeparatorChar"/> is to be added, when absent.</param>
        internal static string? AddTrailingDirectorySeparator(string path)
        {
            return null == path ? null : 
                (Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar);
        }

    }
}
