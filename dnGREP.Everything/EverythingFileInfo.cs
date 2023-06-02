using System;
using System.IO;

namespace dnGREP.Everything
{
    public class EverythingFileInfo
    {
        public EverythingFileInfo(string fullName, FileAttributes attributes, long length, 
            DateTime createTimeUtc, DateTime lastWriteTimeUtc)
        {
            FullName = fullName;
            Attributes = attributes;
            Length = length;
            CreationTimeUtc = createTimeUtc;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        public string FullName { get; private set; }

        public string Name { get { return Path.GetFileName(FullName); } }

        public string? DirectoryName { get { return Path.GetDirectoryName(FullName); } }

        public string Extension { get { return Path.GetExtension(FullName); } }

        public bool Exists { get { return File.Exists(FullName); } }

        public FileAttributes Attributes { get; private set; }

        public bool IsReadOnly {  get { return Attributes.HasFlag(FileAttributes.ReadOnly); } }

        public long Length { get; private set; }

        public DateTime CreationTimeUtc { get; private set; }

        public DateTime CreationTime { get { return CreationTimeUtc.ToLocalTime(); } }

        public DateTime LastWriteTimeUtc { get; private set; }

        public DateTime LastWriteTime { get { return LastWriteTimeUtc.ToLocalTime(); } }

    }
}
