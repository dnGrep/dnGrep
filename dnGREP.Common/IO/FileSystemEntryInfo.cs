using System.IO;
using Windows.Win32.Storage.FileSystem;

namespace dnGREP.Common.IO
{
    /// <summary>
    /// Re-implementation of some AlphaFS FileSystemEntryInfo 
    /// </summary>
    public class FileSystemEntryInfo
    {
        private string? _fullPath;
        private string? _longFullPath;
        private WIN32_FIND_DATAW findData;

        internal FileSystemEntryInfo(WIN32_FIND_DATAW win32FindData)
        {
            findData = win32FindData;
        }
        public string FullPath
        {
            get { return _fullPath ?? string.Empty; }

            set
            {
                LongFullPath = value;
                _fullPath = PathEx.GetRegularPath(LongFullPath);
            }
        }

        public string LongFullPath
        {
            get { return _longFullPath ?? string.Empty; }

            private set { _longFullPath = PathEx.GetLongPath(value); }
        }
        public string FileName => findData.cFileName.ToString();

        public bool IsDirectory => (findData.dwFileAttributes & (uint)FileAttributes.Directory) != 0;
        public bool IsHidden => (findData.dwFileAttributes & (uint)FileAttributes.Hidden) != 0;
    }
}