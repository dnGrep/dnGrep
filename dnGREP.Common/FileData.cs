using System;
using System.IO;
using dnGREP.Everything;

namespace dnGREP.Common
{
    /// <summary>
    /// FileData is a common interface wrapper for a System.IO>FileInfo or an Everything.EverythingFileInfo
    /// </summary>
    public class FileData
    {
        private FileInfo systemFileInfo;
        private EverythingFileInfo everythingFileInfo;

        public FileData(string fileName)
        {
            systemFileInfo = new FileInfo(fileName);
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
                    everythingFileInfo != null ? everythingFileInfo.FullName :
                    string.Empty;
            }
        }

        public string Name
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Name :
                    everythingFileInfo != null ? everythingFileInfo.Name :
                    string.Empty;
            }
        }

        public string DirectoryName
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.DirectoryName :
                    everythingFileInfo != null ? everythingFileInfo.DirectoryName :
                    string.Empty;
            }
        }

        public string Extension
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Extension :
                    everythingFileInfo != null ? everythingFileInfo.Extension :
                    string.Empty;
            }
        }

        public bool Exists
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Exists :
                    everythingFileInfo != null ? everythingFileInfo.Exists :
                    false;
            }
        }

        public FileAttributes Attributes
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Attributes :
                    everythingFileInfo != null ? everythingFileInfo.Attributes :
                    FileAttributes.Normal;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.IsReadOnly :
                    everythingFileInfo != null ? everythingFileInfo.IsReadOnly :
                    true;
            }
        }

        public long Length
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.Length :
                    everythingFileInfo != null ? everythingFileInfo.Length :
                    0;
            }
        }

        public DateTime CreationTimeUtc
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.CreationTimeUtc :
                    everythingFileInfo != null ? everythingFileInfo.CreationTimeUtc :
                    DateTime.MinValue;
            }
        }

        public DateTime CreationTime
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.CreationTime :
                    everythingFileInfo != null ? everythingFileInfo.CreationTime :
                    DateTime.MinValue;
            }
        }

        public DateTime LastWriteTimeUtc
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.LastWriteTimeUtc :
                    everythingFileInfo != null ? everythingFileInfo.LastWriteTimeUtc :
                    DateTime.MinValue;
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                return systemFileInfo != null ? systemFileInfo.LastWriteTime :
                    everythingFileInfo != null ? everythingFileInfo.LastWriteTime :
                    DateTime.MinValue;
            }
        }

    }
}
