using System;
using System.IO;
using System.Text;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Common
{
    public class EolReader : IDisposable
    {
        private TextReader baseReader;
        private char[] charBuffer;
        private int charPos;
        private int charLen;

        public EolReader(TextReader reader)
        {
            baseReader = reader;

            charBuffer = new char[1024];
        }

        public void Dispose()
        {
        }

        private int ReadBuffer()
        {
            charLen = 0;
            charPos = 0;

            charLen = baseReader.ReadBlock(charBuffer, 0, 1024);

            return charLen;
        }

        public bool EndOfStream
        {
            get
            {
                if (baseReader == null)
                    return true;

                if (charPos < charLen)
                    return false;

                int numRead = ReadBuffer();
                return numRead == 0;
            }
        }

        public string ReadLine()
        {
            if (baseReader == null)
                return null;

            if (charPos == charLen)
            {
                if (ReadBuffer() == 0) return null;
            }

            StringBuilder sb = null;
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
                        String s;
                        if (sb != null)
                        {
                            sb.Append(charBuffer, charPos, i - charPos);
                            s = sb.ToString();
                        }
                        else
                        {
                            s = new String(charBuffer, charPos, i - charPos);
                        }

                        charPos = i;
                        if (ch == '\r' && (charPos < charLen || ReadBuffer() > 0))
                        {
                            if (charBuffer[charPos] == '\n')
                            {
                                charPos++;
                                s += '\n';
                            }
                        }
                        return s;
                    }

                } while (i < charLen);

                i = charLen - charPos;

                if (sb == null) sb = new StringBuilder(i + 80);
                sb.Append(charBuffer, charPos, i);

            } while (ReadBuffer() > 0);

            return sb.ToString();
        }
    }
}