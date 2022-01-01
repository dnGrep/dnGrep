using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using dnGREP.Common;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.Engines
{
    public class GrepEngineHex : GrepEngineBase, IGrepEngine
    {
        public IList<string> DefaultFileExtensions => new string[0];

        public bool IsSearchOnly => true;

        public Version FrameworkVersion => Assembly.GetAssembly(typeof(IGrepEngine)).GetName().Version;

        public bool Replace(string sourceFile, string destinationFile, string searchPattern, string replacePattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding, IEnumerable<GrepMatch> replaceItems)
        {
            // should not get here, replace is not allowed from a Hex search
            throw new NotImplementedException();
        }

        public List<GrepSearchResult> Search(string file, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            using (FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan))
            {
                return Search(fileStream, file, searchPattern, searchType, searchOptions, encoding);
            }
        }

        public List<GrepSearchResult> Search(Stream input, string fileName, string searchPattern, SearchType searchType, GrepSearchOption searchOptions, Encoding encoding)
        {
            List<GrepSearchResult> searchResults = new List<GrepSearchResult>();

            byte?[] searchArray = ToByteArray(searchPattern);

            const int bufferSize = 4096;
            byte[] buffer1, buffer2;

            long length = input.Length;
            List<GrepMatch> matches = new List<GrepMatch>();
            using (BinaryReader readStream = new BinaryReader(input))
            {
                int startIndex = 0;
                buffer1 = readStream.ReadBytes(bufferSize);

                while (readStream.BaseStream.Position < length)
                {
                    buffer2 = readStream.ReadBytes(bufferSize);

                    matches.AddRange(DoByteArraySearch(buffer1, buffer2, searchArray, startIndex, searchPattern));

                    startIndex += buffer1.Length;
                    buffer1 = buffer2;
                }
                matches.AddRange(DoByteArraySearch(buffer1, null, searchArray, startIndex, searchPattern));

            }

            if (matches.Count > 0)
            {
                searchResults.Add(new GrepSearchResult(fileName, searchPattern, matches, encoding) { IsHexFile = true });
            }

            return searchResults;
        }

        private byte?[] ToByteArray(string searchPattern)
        {
            // the expected search pattern is a space separated list of byte values in hexadecimal: 20 68 74
            List<byte?> list = new List<byte?>();
            string[] parts = searchPattern.TrimEnd().Split(' ');
            foreach (string num in parts)
            {
                if (num == "?" || num == "??")
                {
                    list.Add(null);
                }
                else if (byte.TryParse(num, NumberStyles.HexNumber, null, out byte result))
                {
                    list.Add(result);
                }
            }
            return list.ToArray();
        }

        private List<GrepMatch> DoByteArraySearch(byte[] buffer1, byte[] buffer2, byte?[] searchArray, int index, string searchPattern)
        {
            List<GrepMatch> globalMatches = new List<GrepMatch>();
            foreach (var match in ByteArraySearchIterator(buffer1, buffer2, searchArray, index, searchPattern))
            {
                globalMatches.Add(match);

                if (Utils.CancelSearch)
                {
                    break;
                }
            }

            return globalMatches;
        }

        private IEnumerable<GrepMatch> ByteArraySearchIterator(byte[] buffer1, byte[] buffer2, byte?[] searchArray, int startIndex, string searchPattern)
        {
            int combinedLength = buffer1.Length + (buffer2 == null ? 0 : buffer2.Length);

            for (int idx = 0; idx < buffer1.Length; idx++)
            {
                if (buffer1[idx] == searchArray[0] || !searchArray[0].HasValue)
                {
                    bool hasMatch = true;
                    bool compareComplete = searchArray.Length == 1;
                    for (int jdx = 1; jdx < searchArray.Length && idx + jdx < combinedLength && hasMatch; jdx++)
                    {
                        compareComplete = jdx == searchArray.Length - 1;
                        if (!searchArray[jdx].HasValue)
                        {
                            continue;
                        }
                        hasMatch = GetByte(buffer1, buffer2, idx + jdx) == searchArray[jdx];
                    }

                    if (hasMatch && compareComplete)
                    {
                        yield return new GrepMatch(searchPattern, 0, startIndex + idx, searchArray.Length);

                        // move to the end of this match to begin the next search (no overlapping matches)
                        idx += searchArray.Length - 1;
                    }
                }
            }
        }

        private byte GetByte(byte[] buffer1, byte[] buffer2, int index)
        {
            if (index < buffer1.Length)
                return buffer1[index];

            index -= buffer1.Length;
            if (index < buffer2.Length)
                return buffer2[index];

            // error
            return 0;
        }

        public void Unload()
        {
        }
    }
}
