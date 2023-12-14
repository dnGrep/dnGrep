using System;
using System.IO;

namespace dnGREP.Everything
{
    public class EverythingFileInfo(string fullName, FileAttributes attributes, long length,
        DateTime createTimeUtc, DateTime lastWriteTimeUtc)
    {
        public string FullName { get; private set; } = fullName;

        public string Name { get { return Path.GetFileName(FullName); } }

        public string? DirectoryName { get { return Path.GetDirectoryName(FullName); } }

        public string Extension { get { return Path.GetExtension(FullName); } }

        public bool Exists { get { return File.Exists(FullName); } }

        public FileAttributes Attributes { get; private set; } = attributes;

        public bool IsReadOnly { get { return Attributes.HasFlag(FileAttributes.ReadOnly); } }

        public long Length { get; private set; } = length;

        public DateTime CreationTimeUtc { get; private set; } = createTimeUtc;

        public DateTime CreationTime { get { return CreationTimeUtc.ToLocalTime(); } }

        public DateTime LastWriteTimeUtc { get; private set; } = lastWriteTimeUtc;

        public DateTime LastWriteTime { get { return LastWriteTimeUtc.ToLocalTime(); } }

    }
}
