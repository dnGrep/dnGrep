using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace dnGREP.Common
{
    public class EolReader(TextReader reader) : IDisposable
    {
        // Chunks stay small so they are always pooled and never reach the large object heap
        private const int ChunkSize = 16 * 1024;

        private readonly char[] charBuffer = new char[1024];
        private int charPos;
        private int charLen;

        private readonly List<char[]> chunks = [];
        private int lineLength;

        public void Dispose()
        {
            ReturnChunks();
            GC.SuppressFinalize(this);
        }

        private int ReadBuffer()
        {
            charLen = 0;
            charPos = 0;

            charLen = reader.ReadBlock(charBuffer, 0, 1024);

            return charLen;
        }

        public bool EndOfStream
        {
            get
            {
                if (reader == null)
                    return true;

                if (charPos < charLen)
                    return false;

                int numRead = ReadBuffer();
                return numRead == 0;
            }
        }

        public string? ReadLine()
        {
            if (reader == null)
                return null;

            if (charPos == charLen)
            {
                if (ReadBuffer() == 0) return null;
            }

            // Most lines fit in the read buffer and are created with a single allocation.
            // A line that spans several reads is collected in a pooled buffer, and the string
            // is still created once, at the end.
            lineLength = 0;
            do
            {
                int i = charPos;
                do
                {
                    char ch = charBuffer[i];
                    i++;
                    // Note the following common line feed chars:
                    // \n - UNIX   \r\n - DOS   \r - Mac

                    if (ch == '\r' || ch == '\n')
                    {
                        // Include the \n of a \r\n pair in the line.
                        // If the \r is the last char in the buffer, the next char is only
                        // available after the buffer is refilled; that case is rare.
                        int end = i;
                        bool crAtBufferEnd = false;
                        if (ch == '\r')
                        {
                            if (i < charLen)
                            {
                                if (charBuffer[i] == '\n')
                                    end++;
                            }
                            else
                            {
                                crAtBufferEnd = true;
                            }
                        }

                        if (lineLength == 0 && !crAtBufferEnd)
                        {
                            string s = new(charBuffer, charPos, end - charPos);
                            charPos = end;
                            return s;
                        }

                        AppendToLine(charPos, end - charPos);
                        charPos = end;
                        if (crAtBufferEnd && ReadBuffer() > 0 && charBuffer[charPos] == '\n')
                        {
                            charPos++;
                            AppendToLine(charPos - 1, 1);
                        }
                        return TakeLine();
                    }

                } while (i < charLen);

                AppendToLine(charPos, charLen - charPos);

            } while (ReadBuffer() > 0);

            return TakeLine();
        }

        private void AppendToLine(int start, int count)
        {
            while (count > 0)
            {
                int offset = lineLength % ChunkSize;
                if (offset == 0)
                {
                    chunks.Add(ArrayPool<char>.Shared.Rent(ChunkSize));
                }

                int n = Math.Min(count, ChunkSize - offset);
                Array.Copy(charBuffer, start, chunks[^1], offset, n);
                start += n;
                count -= n;
                lineLength += n;
            }
        }

        private string TakeLine()
        {
            string s = string.Create(lineLength, chunks, static (span, list) =>
            {
                int pos = 0;
                foreach (char[] chunk in list)
                {
                    int n = Math.Min(ChunkSize, span.Length - pos);
                    chunk.AsSpan(0, n).CopyTo(span[pos..]);
                    pos += n;
                }
            });

            ReturnChunks();
            return s;
        }

        private void ReturnChunks()
        {
            foreach (char[] chunk in chunks)
            {
                ArrayPool<char>.Shared.Return(chunk);
            }
            chunks.Clear();
            lineLength = 0;
        }
    }
}