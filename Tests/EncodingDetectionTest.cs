using System;
using System.IO;
using System.Text;
using dnGREP.Common;
using Xunit;

namespace Tests
{
    public sealed class EncodingDetectionTest : IDisposable
    {
        // larger than the 1 MB the detector inspects
        private const int LargeSize = 1536 * 1024;

        private readonly string tempFolder = Path.Combine(Path.GetTempPath(), "dnGREP-enc-" + Guid.NewGuid().ToString("N"));

        public EncodingDetectionTest()
        {
            Directory.CreateDirectory(tempFolder);
        }

        public void Dispose()
        {
            Directory.Delete(tempFolder, true);
        }

        private string WriteFile(byte[] bytes)
        {
            string path = Path.Combine(tempFolder, Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static byte[] AsciiBytes(int size)
        {
            const string line = "The quick brown fox jumps over the lazy dog 0123456789\r\n";
            StringBuilder sb = new(size + line.Length);
            while (sb.Length < size)
                sb.Append(line);
            return Encoding.ASCII.GetBytes(sb.ToString(0, size));
        }

        private static byte[] Utf8Text(int repeat) =>
            Encoding.UTF8.GetBytes(string.Concat(System.Linq.Enumerable.Repeat("caf\u00E9 na\u00EFve \u4E2D\u6587 text\r\n", repeat)));

        private static byte[] Concat(byte[] first, byte[] second)
        {
            byte[] all = new byte[first.Length + second.Length];
            first.CopyTo(all, 0);
            second.CopyTo(all, first.Length);
            return all;
        }

        [Fact]
        public void SmallAsciiFileIsAscii()
        {
            Encoding encoding = Utils.GetFileEncoding(WriteFile(AsciiBytes(10_000)));

            Assert.Equal(Encoding.ASCII.CodePage, encoding.CodePage);
        }

        [Fact]
        public void LargeAsciiFileDoesNotUseAscii()
        {
            Encoding encoding = Utils.GetFileEncoding(WriteFile(AsciiBytes(LargeSize)));

            Assert.Equal(Encoding.UTF8, encoding);
        }

        [Fact]
        public void LargeFileWithUtf8AfterTheInspectedBytesIsUtf8()
        {
            byte[] bytes = Concat(AsciiBytes(LargeSize), Utf8Text(500));

            Assert.Equal(Encoding.UTF8, Utils.GetFileEncoding(WriteFile(bytes)));
        }

        [Fact]
        public void LargeFileWithUtf8AtTheStartIsUtf8()
        {
            byte[] bytes = Concat(Utf8Text(500), AsciiBytes(LargeSize));

            Assert.Equal(Encoding.UTF8, Utils.GetFileEncoding(WriteFile(bytes)));
        }

        [Fact]
        public void LargeFileWithUtf8ByteOrderMarkIsUtf8()
        {
            byte[] bytes = Concat([0xEF, 0xBB, 0xBF], AsciiBytes(LargeSize));

            Assert.Equal(Encoding.UTF8, Utils.GetFileEncoding(WriteFile(bytes)));
        }

        [Fact]
        public void LargeUtf16WithoutByteOrderMarkIsUtf16()
        {
            byte[] bytes = Encoding.Unicode.GetBytes(Encoding.ASCII.GetString(AsciiBytes(LargeSize / 2)));

            Assert.Equal(Encoding.Unicode, Utils.GetFileEncoding(WriteFile(bytes)));
        }

        [Fact]
        public void LargeUtf16WithByteOrderMarkIsUtf16()
        {
            byte[] bytes = Concat(Encoding.Unicode.GetPreamble(),
                Encoding.Unicode.GetBytes(Encoding.ASCII.GetString(AsciiBytes(LargeSize / 2))));

            Assert.Equal(Encoding.Unicode.CodePage, Utils.GetFileEncoding(WriteFile(bytes)).CodePage);
        }

        [Fact]
        public void StreamOverloadResetsThePosition()
        {
            using MemoryStream stream = new(AsciiBytes(LargeSize));

            Encoding encoding = Utils.GetFileEncoding(stream);

            Assert.Equal(Encoding.UTF8, encoding);
            Assert.Equal(0, stream.Position);
        }

        [Fact]
        public void LargeFileIsDetectedWithoutReadingEverything()
        {
            using CountingStream stream = new(AsciiBytes(8 * 1024 * 1024));

            Utils.GetFileEncoding(stream);

            // the detector plus the 1 KB unicode check; far less than the 8 MB of the file
            Assert.True(stream.BytesRead < 2 * 1024 * 1024, $"read {stream.BytesRead} bytes");
        }

        private sealed class CountingStream(byte[] data) : MemoryStream(data)
        {
            public long BytesRead { get; private set; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int n = base.Read(buffer, offset, count);
                BytesRead += n;
                return n;
            }

            public override int Read(Span<byte> buffer)
            {
                int n = base.Read(buffer);
                BytesRead += n;
                return n;
            }
        }
    }
}
