using System.IO;
using System.Linq;

namespace dnGREP.Common.IO
{
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
    }
}
