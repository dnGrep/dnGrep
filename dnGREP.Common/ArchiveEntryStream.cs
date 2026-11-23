using System;
using System.IO;

namespace dnGREP.Common
{
    /// <summary>
    /// A read/write seekable stream used to hold an extracted archive entry.
    /// The data is kept in a presized <see cref="MemoryStream"/> until it grows beyond the
    /// memory threshold, then moves to a temp file that is deleted when the stream is closed.
    /// </summary>
    public sealed class ArchiveEntryStream : Stream
    {
        private const int DefaultCapacity = 4096;
        private const int FileBufferSize = 81920;

        /// <summary>The setting default, in MB.</summary>
        public const int DefaultThresholdMB = 64;

        /// <summary>
        /// The largest threshold the setting allows, in MB. A <see cref="MemoryStream"/> cannot hold more than
        /// about 2 GB, so an entry larger than that always goes to a temp file.
        /// </summary>
        public const int MaxThresholdMB = 2047;

        /// <summary>
        /// Replaces the threshold from the application settings. Used by tests; set to null to use the setting.
        /// </summary>
        public static long? MemoryThresholdOverride { get; set; }

        /// <summary>
        /// The largest archive entry, in bytes, that is held in memory. Read from the
        /// <see cref="GrepSettings.Key.ArchiveMemoryThresholdMB"/> setting. Zero extracts every entry to a temp file.
        /// </summary>
        public static long DefaultMemoryThreshold =>
            MemoryThresholdOverride ?? ThresholdBytes(GrepSettings.Instance.Get<int>(GrepSettings.Key.ArchiveMemoryThresholdMB));

        /// <summary>
        /// Converts a threshold setting in MB to bytes. A negative value uses the default.
        /// </summary>
        public static long ThresholdBytes(int megabytes)
        {
            if (megabytes < 0)
            {
                megabytes = DefaultThresholdMB;
            }

            return Math.Min(megabytes, MaxThresholdMB) * 1024L * 1024;
        }

        private readonly long memoryThreshold;
        private Stream inner;

        public ArchiveEntryStream(ulong expectedSize) : this(expectedSize, DefaultMemoryThreshold)
        {
        }

        public ArchiveEntryStream(ulong expectedSize, long memoryThreshold)
        {
            this.memoryThreshold = Math.Min(memoryThreshold, Array.MaxLength);

            if (expectedSize > (ulong)this.memoryThreshold)
            {
                inner = CreateTempFile();
            }
            else
            {
                // a known size avoids the repeated doubling and copying of a growing MemoryStream
                int capacity = expectedSize == 0 ? DefaultCapacity : (int)expectedSize;
                inner = new MemoryStream(capacity);
            }
        }

        public bool IsInMemory => inner is MemoryStream;

        private static FileStream CreateTempFile()
        {
            string path = Path.Combine(Utils.GetTempFolder(), Guid.NewGuid().ToString("N") + ".tmp");
            return new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                FileBufferSize, FileOptions.DeleteOnClose);
        }

        private void SpillToDisk()
        {
            if (inner is not MemoryStream memory)
                return;

            FileStream file = CreateTempFile();
            try
            {
                long position = memory.Position;
                memory.WriteTo(file);
                file.Position = position;
            }
            catch
            {
                file.Dispose();
                throw;
            }

            inner = file;
            memory.Dispose();
        }

        private void SpillIfNeeded(long requiredLength)
        {
            if (inner is MemoryStream && requiredLength > memoryThreshold)
            {
                SpillToDisk();
            }
        }

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => inner.CanSeek;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override int Read(Span<byte> buffer) => inner.Read(buffer);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value)
        {
            SpillIfNeeded(value);
            inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            SpillIfNeeded(inner.Position + count);
            inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            SpillIfNeeded(inner.Position + buffer.Length);
            inner.Write(buffer);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
