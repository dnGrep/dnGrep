using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using dnGREP.Common;
using dnGREP.Engines;
using Xunit;

namespace Tests
{
    public sealed class ArchiveEntryStreamTest : IDisposable
    {
        private readonly string tempFolder = Path.Combine(Path.GetTempPath(), "dnGREP-aes-" + Guid.NewGuid().ToString("N"));

        public ArchiveEntryStreamTest()
        {
            Directory.CreateDirectory(tempFolder);
        }

        public void Dispose()
        {
            Directory.Delete(tempFolder, true);
        }

        private static byte[] Data(int size)
        {
            byte[] data = new byte[size];
            new Random(42).NextBytes(data);
            return data;
        }

        [Theory]
        [InlineData(64, 64L * 1024 * 1024)]
        [InlineData(1, 1024 * 1024)]
        [InlineData(0, 0)]
        [InlineData(-1, 64L * 1024 * 1024)]
        [InlineData(2047, 2047L * 1024 * 1024)]
        [InlineData(1_000_000, 2047L * 1024 * 1024)]
        public void ThresholdBytesConvertsAndLimitsTheSetting(int megabytes, long expected)
        {
            Assert.Equal(expected, ArchiveEntryStream.ThresholdBytes(megabytes));
        }

        [Fact]
        public void HighestThresholdFitsInAMemoryStream()
        {
            Assert.True(ArchiveEntryStream.ThresholdBytes(int.MaxValue) <= Array.MaxLength);
        }

        [Fact]
        public void ZeroThresholdWritesEverythingToDisk()
        {
            using ArchiveEntryStream stream = new(0, 0);

            stream.Write(Data(10));

            Assert.False(stream.IsInMemory);
            Assert.Equal(10, stream.Length);
        }

        [Fact]
        public void OverrideReplacesTheThresholdFromTheSetting()
        {
            // the shared settings are not touched here: other tests load and convert them in parallel
            try
            {
                ArchiveEntryStream.MemoryThresholdOverride = 123;
                Assert.Equal(123, ArchiveEntryStream.DefaultMemoryThreshold);
            }
            finally
            {
                ArchiveEntryStream.MemoryThresholdOverride = null;
            }
        }

        [Fact]
        public void KnownSizeBelowThresholdStaysInMemory()
        {
            using ArchiveEntryStream stream = new(1000, 4096);

            Assert.True(stream.IsInMemory);
            stream.Write(Data(1000));
            Assert.True(stream.IsInMemory);
            Assert.Equal(1000, stream.Length);
        }

        [Fact]
        public void KnownSizeAboveThresholdStartsOnDisk()
        {
            using ArchiveEntryStream stream = new(10_000, 4096);

            Assert.False(stream.IsInMemory);
        }

        [Fact]
        public void UnknownSizeGrowingPastThresholdSpillsAndKeepsData()
        {
            byte[] data = Data(10_000);
            using ArchiveEntryStream stream = new(0, 4096);
            Assert.True(stream.IsInMemory);

            for (int i = 0; i < data.Length; i += 1000)
            {
                stream.Write(data, i, 1000);
            }

            Assert.False(stream.IsInMemory);
            Assert.Equal(data.Length, stream.Length);
            Assert.Equal(data.Length, stream.Position);

            stream.Seek(0, SeekOrigin.Begin);
            byte[] read = new byte[data.Length];
            stream.ReadExactly(read);
            Assert.Equal(data, read);
        }

        [Fact]
        public void SpillKeepsThePositionOfAnEarlierSeek()
        {
            byte[] data = Data(3000);
            using ArchiveEntryStream stream = new(0, 4096);
            stream.Write(data);
            stream.Seek(1000, SeekOrigin.Begin);

            stream.Write(Data(5000)); // exceeds the threshold from position 1000

            Assert.False(stream.IsInMemory);
            Assert.Equal(6000, stream.Position);
            Assert.Equal(6000, stream.Length);

            stream.Seek(0, SeekOrigin.Begin);
            byte[] head = new byte[1000];
            stream.ReadExactly(head);
            Assert.Equal(data.AsSpan(0, 1000).ToArray(), head);
        }

        [Fact]
        public void SetLengthBeyondThresholdSpills()
        {
            using ArchiveEntryStream stream = new(0, 4096);

            stream.SetLength(10_000);

            Assert.False(stream.IsInMemory);
            Assert.Equal(10_000, stream.Length);
        }

        [Fact]
        public void DisposingTheStreamDeletesTheTempFile()
        {
            string folder = Utils.GetTempFolder();
            int before = Directory.GetFiles(folder, "*.tmp").Length;

            ArchiveEntryStream stream = new(10_000, 4096);
            stream.Write(Data(10_000));
            int during = Directory.GetFiles(folder, "*.tmp").Length;
            stream.Dispose();
            int after = Directory.GetFiles(folder, "*.tmp").Length;

            Assert.True(during > before);
            Assert.Equal(before, after);
        }

        [Theory]
        [InlineData(1024 * 1024)] // entries held in memory
        [InlineData(1)] // every entry goes through a temp file
        public void ArchiveSearchIsTheSameInMemoryAndOnDisk(long threshold)
        {
            string zipPath = Path.Combine(tempFolder, "test.zip");
            using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                for (int i = 0; i < 3; i++)
                {
                    var entry = zip.CreateEntry($"file{i}.txt");
                    using StreamWriter writer = new(entry.Open(), new UTF8Encoding(false));
                    for (int line = 0; line < 200; line++)
                    {
                        writer.WriteLine(line == 50 + i ? $"line {line} has the needle caf\u00E9" : $"line {line} is just filler text");
                    }
                }
            }

            try
            {
                ArchiveEntryStream.MemoryThresholdOverride = threshold;

                GrepCore core = new()
                {
                    FileFilter = new(tempFolder, "*.txt", string.Empty, false, false,
                        false, true, -1, true,
                        true, true, false, 0, 0,
                        FileDateFilter.None, null, null)
                };
                var results = core.Search([zipPath], SearchType.PlainText, "needle", GrepSearchOption.None, -1);

                Assert.Equal(3, results.Count);
                Assert.All(results, r => Assert.Single(r.Matches));
                Assert.Equal([51, 52, 53], results.Select(r => r.Matches[0].LineNumber).Order().ToArray());
            }
            finally
            {
                ArchiveEntryStream.MemoryThresholdOverride = null;
            }
        }
    }
}
