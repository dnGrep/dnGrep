using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace dnGREP.Common.IO
{
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

        /// <summary>[AlphaFS] Makes an extended long path from the specified <paramref name="path"/> by prefixing <see cref="LongPathPrefix"/>.</summary>
        /// <returns>The <paramref name="path"/> prefixed with a <see cref="LongPathPrefix"/>, the minimum required full path is: "C:\".</returns>
        /// <remarks>This method does not verify that the resulting path and file name are valid, or that they see an existing file on the associated volume.</remarks>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        /// <param name="path">The path to the file or directory, this can also be an UNC path.</param>
        public static string GetLongPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

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
                return LongPathUncPrefix + path.Substring(UncPrefix.Length);
            }

            if (!Path.IsPathRooted(path) || !IsLogicalDrive(path))
            {
                return path;
            }

            return LongPathPrefix + path;
        }

        /// <summary>[AlphaFS] Checks if <paramref name="path"/> is in a logical drive format, such as "C:", "D:".</summary>
        /// <returns>true when <paramref name="path"/> is in a logical drive format, such as "C:", "D:".</returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
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
    }
}
