using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dnGREP.Common;
using Xunit;

namespace Tests
{
    public class EolReaderTest
    {
        // The reader uses an internal 1024 char buffer
        private const int BufferSize = 1024;

        private static List<string> ReadAll(string text)
        {
            using StringReader baseReader = new(text);
            using EolReader reader = new(baseReader);
            List<string> lines = [];
            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (line != null)
                    lines.Add(line);
            }
            return lines;
        }

        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("abc\n")]
        [InlineData("abc\r")]
        [InlineData("abc\r\n")]
        [InlineData("abc\r\ndef\nghi\rjkl")]
        [InlineData("\r\n\r\n\n\r\r\n")]
        [InlineData("\n\r")]
        [InlineData("a\r\r\nb")]
        public void LinesKeepTheirLineEndings(string text)
        {
            List<string> lines = ReadAll(text);

            Assert.Equal(text, string.Concat(lines));

            foreach (string line in lines)
            {
                // a line ends at the first line ending char, apart from the \n of a \r\n pair
                int idx = line.IndexOfAny(['\r', '\n']);
                if (idx >= 0)
                {
                    bool crlf = line[idx] == '\r' && idx + 1 < line.Length && line[idx + 1] == '\n';
                    Assert.Equal(crlf ? line.Length - 2 : line.Length - 1, idx);
                }
            }
        }

        [Fact]
        public void CrLfIsOneLineEnding()
        {
            Assert.Equal(["a\r\n", "b\r\n", "c"], ReadAll("a\r\nb\r\nc"));
            Assert.Equal(["a\r", "\r\n", "b\n", "\n"], ReadAll("a\r\r\nb\n\n"));
        }

        [Theory]
        [InlineData(BufferSize - 2)]
        [InlineData(BufferSize - 1)]
        [InlineData(BufferSize)]
        [InlineData(BufferSize + 1)]
        [InlineData(2 * BufferSize - 1)]
        public void CrLfAtBufferBoundary(int contentLength)
        {
            // the \r lands on or around the last char of the buffer, the \n on or around the first of the next
            string content = new('x', contentLength);
            string text = content + "\r\n" + "second\r\n" + content + "\rthird\nlast";

            List<string> lines = ReadAll(text);

            Assert.Equal([content + "\r\n", "second\r\n", content + "\r", "third\n", "last"], lines);
        }

        [Fact]
        public void CarriageReturnAsLastCharOfBufferWithoutLineFeed()
        {
            string content = new('x', BufferSize - 1);

            List<string> lines = ReadAll(content + "\rnext");

            Assert.Equal([content + "\r", "next"], lines);
        }

        [Fact]
        public void CarriageReturnAsLastCharOfFile()
        {
            string content = new('x', BufferSize - 1);

            Assert.Equal([content + "\r"], ReadAll(content + "\r"));
        }

        [Fact]
        public void LineLongerThanBufferWithCrLf()
        {
            string content = new('y', 3 * BufferSize + 17);
            string text = content + "\r\n" + content + "\n";

            Assert.Equal([content + "\r\n", content + "\n"], ReadAll(text));
        }

        [Fact]
        public void LineLongerThanBufferEndingWithCrAtBufferEnd()
        {
            // the pending text is in the StringBuilder when the \r is the last char of a later buffer
            string content = new('z', 2 * BufferSize - 1);

            Assert.Equal([content + "\r\n", "end"], ReadAll(content + "\r\nend"));
        }

        [Fact]
        public void RoundTripsRandomLongLines()
        {
            Random rng = new(5678);
            string[] pieces = [new string('a', 700), new string('b', 1500), new string('c', 70000), "x", "\r", "\n", "\r\n"];
            StringBuilder sb = new();
            for (int i = 0; i < 400; i++)
                sb.Append(pieces[rng.Next(pieces.Length)]);
            string text = sb.ToString();

            List<string> lines = ReadAll(text);

            Assert.Equal(text, string.Concat(lines));
            // same lines as the framework reader, which does not keep the line endings
            using StringReader reference = new(text);
            foreach (string line in lines)
            {
                Assert.Equal(reference.ReadLine(), line.TrimEnd('\r', '\n'));
            }
            Assert.Null(reference.ReadLine());
        }

        [Fact]
        public void RoundTripsRandomText()
        {
            Random rng = new(1234);
            string[] pieces = ["a", "bb", "\r", "\n", "\r\n", "ccc"];
            StringBuilder sb = new();
            for (int i = 0; i < 20000; i++)
                sb.Append(pieces[rng.Next(pieces.Length)]);
            string text = sb.ToString();

            Assert.Equal(text, string.Concat(ReadAll(text)));
        }
    }
}
