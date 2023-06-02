using Windows.Win32;

namespace dnGREP.Common.IO
{
    /// <summary>
    /// File extensions 
    /// </summary>
    public static class FileEx
    {
        /// <summary>Creates a symbolic link (similar to CMD command: "MKLINK") to a file.</summary>
        /// <remarks>See <see cref="Security.Privilege.CreateSymbolicLink"/> to run this method in an elevated state.</remarks>
        /// <param name="symlinkFileName">The name of the target for the symbolic link to be created.</param>
        /// <param name="targetFileName">The symbolic link to be created.</param>
        public static bool CreateSymbolicLink(string symlinkFileName, string targetFileName)
        {
            return PInvoke.CreateSymbolicLink(symlinkFileName, targetFileName, 0);
        }
    }
}
