using System;
using System.IO;
using System.Text;

namespace dnGREP.Common
{
    public sealed class ReverseLineReader
    {
        private const int BufferSize = 4096;
        private Stream baseStream;
        private readonly Encoding encoding;
        private Func<long, byte, bool> characterStartDetector;

        private byte[] buffer;
        private char[] charBuffer;
        private int bufferTail;
        private int charBufferTail;

        public bool EndOfStream
        {
            get
            {
                if (baseStream == null)
                    return true;

                return baseStream.Position == 0 && bufferTail == 0 && charBufferTail == 0;
            }
        }

        public void Reset()
        {
            bufferTail = 0;
            charBufferTail = 0;
        }

        public static bool IsEncodingSupported(Encoding encoding)
        {
            return encoding.IsSingleByte || encoding is UTF8Encoding || encoding is UnicodeEncoding || encoding is UTF32Encoding;
        }

        public ReverseLineReader(Stream baseStream, Encoding encoding)
        {
            this.baseStream = baseStream;
            this.encoding = encoding;
            this.buffer = new byte[BufferSize];
            this.charBuffer = new char[encoding.GetMaxCharCount(BufferSize)];
            this.bufferTail = 0;
            this.charBufferTail = 0;

            if (encoding.IsSingleByte)
            {
                // For a single byte encoding, every byte is the start (and end) of a character
                characterStartDetector = (pos, data) => true;
            }
            else if (encoding is UTF8Encoding)
            {
                // For UTF-8, bytes with the top bit clear or the second bit set are the start of a character
                // See http://www.cl.cam.ac.uk/~mgk25/unicode.html
                characterStartDetector = (pos, data) => (data & 0x80) == 0 || (data & 0x40) != 0;
            }
            else if (encoding is UnicodeEncoding)
            {
                // For UTF-16, even-numbered positions are the start of a character.
                // This assumes no surrogate pairs. More work required to handle that.
                characterStartDetector = (pos, data) => (pos & 1) == 0;
            }
            else if (encoding is UTF32Encoding)
            {
                // For UTF-32, every fourth byte is the start of the character
                characterStartDetector = (pos, data) => (data & 0x03) == 0;
            }
            else
            {
                throw new ArgumentException("Only single byte, UTF-8, UTF-16, UTF-32 encodings are permitted");
            }
        }
        
        // false - stream ended
        // true - continue reading
        private bool Fetch()
        {
            // first thing first: copy leftover bytes to the end
            int leftoverBytes = bufferTail;
            if (leftoverBytes > 0)
            {
                Array.Copy(buffer, 0, buffer, BufferSize - leftoverBytes, leftoverBytes);
            }
            if(baseStream.Position <= 0) // stream ended
            {
                if(leftoverBytes > 0) // stream ended unexpectedly
                {
                    // there should not be any leftover bytes when source stream end
                    throw new EndOfStreamException("Unexpected end of file. Check the text encoding, it should be wrong");
                }
                return false;
            }

            // calculate bytes to read
            int bytesToRead = (int)Math.Min(baseStream.Position, BufferSize - leftoverBytes);

            baseStream.Seek(-bytesToRead, SeekOrigin.Current);
            baseStream.Read(buffer, 0, bytesToRead);
            baseStream.Seek(-bytesToRead, SeekOrigin.Current);

            // set new position to decode characters from buffer
            bufferTail = bytesToRead + leftoverBytes;
            return true;
        }

        // false - need fetch more
        // true - line finished
        private bool EatCharBuffer(ref string str)
        {
            // find new line sequence
            // Note the following common line feed chars:
            // \n - UNIX   \r\n - DOS   \r - Mac
            if(charBufferTail <= 0)
            {
                return false; // line not finished, need fetch more
            }

            StringBuilder builder = new StringBuilder(charBufferTail+str.Length);
            builder.Append(str);
            for (; charBufferTail > 0; --charBufferTail)
            {
                if (charBuffer[charBufferTail - 1] == '\n')
                {
                    if (builder.Length == 0)
                    {
                        builder.Insert(0, charBuffer[charBufferTail - 1]);
                        continue;
                    }
                    else
                    {
                        str = builder.ToString();
                        return true; // line finished
                    }
                }
                if (charBuffer[charBufferTail - 1] == '\r')
                {
                    if (builder.Length == 0)
                    {
                        builder.Insert(0, charBuffer[charBufferTail - 1]);
                        continue;
                    }
                    else if (builder.Length == 1 && builder[0] == '\n')
                    {
                        builder.Insert(0, charBuffer[charBufferTail - 1]);
                        continue;
                    }
                    else
                    {
                        str = builder.ToString();
                        return true; // line finished
                    }
                }
                builder.Insert(0, charBuffer[charBufferTail - 1]);
            }
            str = builder.ToString();
            return true; // line not finished
        }

        public string ReadLine()
        {
            if (baseStream == null)
                return null;

            string resultStr = "";
            while(!EatCharBuffer(ref resultStr))
            {
                if(!Fetch())
                {
                    return resultStr;
                }

                int firstCharPosition = 0;
                while (!characterStartDetector(firstCharPosition, buffer[firstCharPosition]))
                {
                    firstCharPosition++;
                    if (firstCharPosition == 4 || firstCharPosition == bufferTail)
                    {
                        throw new InvalidDataException("Failed to find character start byte");
                    }
                }
                
                // consume byte buffer and fill char buffer
                charBufferTail = encoding.GetChars(buffer, firstCharPosition, bufferTail - firstCharPosition, charBuffer, 0);
                // byte buffer consumed, offset the tail
                bufferTail = firstCharPosition;
            }
            return resultStr;
        }
    }
}