using System;
using System.IO;
using dnGREP.Everything;
using SevenZip;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Common
{
    /// <summary>
    /// FileData is a common interface wrapper for a System.IO.FileInfo, 
    /// SevenZip.ArchiveFileInfo, or an Everything.EverythingFileInfo
    /// </summary>
    public class FileData
    {
        private readonly FileInfo systemFileInfo;
        private readonly string archiveFileName;
        private readonly ArchiveFileInfo? sevenZipFileInfo;
        private readonly EverythingFileInfo everythingFileInfo;

        public FileData(string fileName)
        {
            systemFileInfo = new FileInfo(fileName);
        }

        public FileData(string srcFileName, ArchiveFileInfo fileInfo)
        {
            archiveFileName = srcFileName;
            sevenZipFileInfo = fileInfo;
        }

        public FileData(EverythingFileInfo fileInfo)
        {
            everythingFileInfo = fileInfo;
        }

        public string FullName
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.FullName :
                    sevenZipFileInfo != null ? (archiveFileName ?? string.Empty) + ArchiveDirectory.ArchiveSeparator + sevenZipFileInfo.Value.FileName :
                    everythingFileInfo != null ? everythingFileInfo.FullName :
                    string.Empty;
            }
        }

        public string Name
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Name :
                    sevenZipFileInfo != null ? Path.GetFileName(sevenZipFileInfo.Value.FileName) :
                    everythingFileInfo != null ? everythingFileInfo.Name :
                    string.Empty;
            }
        }

        public string DirectoryName
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.DirectoryName :
                    sevenZipFileInfo != null ? Path.GetDirectoryName(sevenZipFileInfo.Value.FileName) :
                    everythingFileInfo != null ? everythingFileInfo.DirectoryName :
                    string.Empty;
            }
        }

        public string Extension
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Extension :
                    sevenZipFileInfo != null ? Path.GetExtension(sevenZipFileInfo.Value.FileName) :
                    everythingFileInfo != null ? everythingFileInfo.Extension :
                    string.Empty;
            }
        }

        public bool Exists
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Exists :
                    sevenZipFileInfo != null ? true :
                    everythingFileInfo != null ? everythingFileInfo.Exists :
                    false;
            }
        }

        public FileAttributes Attributes
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Attributes :
                    sevenZipFileInfo != null ? (FileAttributes)sevenZipFileInfo.Value.Attributes :
                    everythingFileInfo != null ? everythingFileInfo.Attributes :
                    FileAttributes.Normal;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.IsReadOnly :
                    sevenZipFileInfo != null ? true :
                    everythingFileInfo != null ? everythingFileInfo.IsReadOnly :
                    true;
            }
        }

        public long Length
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Length :
                    sevenZipFileInfo != null ? ToLong(sevenZipFileInfo.Value.Size) :
                    everythingFileInfo != null ? everythingFileInfo.Length :
                    0;
            }
        }

        public DateTime CreationTimeUtc
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.CreationTimeUtc :
                    sevenZipFileInfo != null ? sevenZipFileInfo.Value.CreationTime.ToUniversalTime() :
                    everythingFileInfo != null ? everythingFileInfo.CreationTimeUtc :
                    DateTime.MinValue;
            }
        }

        public DateTime CreationTime
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.CreationTime :
                    sevenZipFileInfo != null ? sevenZipFileInfo.Value.CreationTime :
                    everythingFileInfo != null ? everythingFileInfo.CreationTime :
                    DateTime.MinValue;
            }
        }

        public DateTime LastWriteTimeUtc
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.LastWriteTimeUtc :
                    sevenZipFileInfo != null ? sevenZipFileInfo.Value.LastWriteTime.ToUniversalTime() :
                    everythingFileInfo != null ? everythingFileInfo.LastWriteTimeUtc :
                    DateTime.MinValue;
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.LastWriteTime :
                    sevenZipFileInfo != null ? sevenZipFileInfo.Value.LastWriteTime :
                    everythingFileInfo != null ? everythingFileInfo.LastWriteTime :
                    DateTime.MinValue;
            }
        }

        public bool IsBinary { get; set; }

        private static long ToLong(ulong size)
        {
            try
            {
                return Convert.ToInt64(size);
            }
            catch (OverflowException)
            {
                return long.MaxValue;
            }
        }
    }
}
