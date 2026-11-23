using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace dnGREP.Benchmarks
{
    public enum LineEnding
    {
        LF,
        CRLF
    }

    public enum MatchDensity
    {
        /// <summary>No matches: measures the read and scan cost only.</summary>
        None,
        /// <summary>A single match on the last line.</summary>
        Last,
        /// <summary>About one match every 16K chars (at least one per line for short lines).</summary>
        Sparse,
        /// <summary>About one match every 128 chars (at least one per line for short lines).</summary>
        Dense
    }

    public sealed record TestFile(string Path, long Length, long Matches);

    /// <summary>
    /// Generates deterministic ASCII test files and caches them in the temp folder.
    /// A line length of 0 means the whole file is one line.
    /// </summary>
    public static class TestData
    {
        public const string SearchText = "needle";

        // The match is surrounded by spaces so whole word searches also find it
        private const string Token = " needle ";
        private const int TokenMatchOffset = 1;
        private const int SegmentLength = 64 * 1024;

        private static readonly string root = Path.Combine(Path.GetTempPath(), "dnGREP.Benchmarks");

        /// <summary>
        /// File size in MB, from the DNGREP_BENCH_MB environment variable (default 64).
        /// </summary>
        public static int DefaultSizeMB
        {
            get
            {
                string? value = Environment.GetEnvironmentVariable("DNGREP_BENCH_MB");
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mb) && mb > 0 ? mb : 64;
            }
        }

        public static string RootFolder => root;

        public static TestFile GetOrCreate(int lineLength, LineEnding eol, MatchDensity density, int sizeMB = 0)
        {
            if (sizeMB <= 0)
                sizeMB = DefaultSizeMB;

            Directory.CreateDirectory(root);
            string path = Path.Combine(root, $"L{lineLength}_{eol}_{density}_{sizeMB}MB.txt");
            string metaPath = path + ".matches";

            if (File.Exists(path) && File.Exists(metaPath) &&
                long.TryParse(File.ReadAllText(metaPath), NumberStyles.Integer, CultureInfo.InvariantCulture, out long cached))
            {
                return new TestFile(path, new FileInfo(path).Length, cached);
            }

            long matches = Generate(path, (long)sizeMB * 1024 * 1024, lineLength, eol, density);
            File.WriteAllText(metaPath, matches.ToString(CultureInfo.InvariantCulture));
            return new TestFile(path, new FileInfo(path).Length, matches);
        }

        /// <summary>
        /// Writes a new test file to the given path, replacing any existing file, and returns the number of matches in it.
        /// </summary>
        public static long GenerateFile(string path, int sizeMB, int lineLength, LineEnding eol, MatchDensity density) =>
            Generate(path, (long)sizeMB * 1024 * 1024, lineLength, eol, density);

        private static long Generate(string path, long totalChars, int lineLength, LineEnding eol, MatchDensity density)
        {
            string eolText = eol == LineEnding.CRLF ? "\r\n" : "\n";
            long lineContent = lineLength <= 0 ? totalChars - eolText.Length : lineLength;
            long lineCount = Math.Max(1, totalChars / (lineContent + eolText.Length));

            if (lineContent > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(lineLength), "Line is too long");

            int spacing = density switch
            {
                MatchDensity.Sparse => 16 * 1024,
                MatchDensity.Dense => 128,
                _ => 0
            };
            // short lines: only some lines get a match
            long linesPerMatch = spacing > 0 && lineContent < spacing ? spacing / lineContent : 1;
            int step = (int)Math.Min(spacing, lineContent);

            char[] filler = CreateFiller();
            char[] segment = new char[SegmentLength];
            long matches = 0;
            long fillerPos = 0;

            using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, FileOptions.SequentialScan);
            using StreamWriter writer = new(stream, new UTF8Encoding(false), 1 << 16);

            for (long line = 0; line < lineCount; line++)
            {
                bool matchLine = density switch
                {
                    MatchDensity.None => false,
                    MatchDensity.Last => line == lineCount - 1,
                    _ => line % linesPerMatch == 0
                };

                for (long pos = 0; pos < lineContent; pos += SegmentLength)
                {
                    int len = (int)Math.Min(SegmentLength, lineContent - pos);
                    fillerPos = CopyFiller(filler, fillerPos, segment, len);

                    if (matchLine)
                    {
                        matches += Overlay(segment, len, pos, (int)lineContent, density, step);
                    }

                    writer.Write(segment, 0, len);
                }

                writer.Write(eolText);
            }

            return matches;
        }

        /// <summary>
        /// Writes the match tokens that start in this segment. Tokens that would cross a segment
        /// boundary are skipped, so the returned count is exact.
        /// </summary>
        private static int Overlay(char[] segment, int len, long segmentStart, int lineContent, MatchDensity density, int step)
        {
            int count = 0;
            if (density == MatchDensity.Last)
            {
                long p = lineContent - Token.Length;
                if (p >= segmentStart && p + Token.Length <= segmentStart + len)
                {
                    Token.CopyTo(0, segment, (int)(p - segmentStart), Token.Length);
                    count++;
                }
                return count;
            }

            long first = step / 2;
            if (first < segmentStart)
            {
                first += (segmentStart - first + step - 1) / step * step;
            }

            for (long p = first; p + Token.Length <= lineContent; p += step)
            {
                if (p >= segmentStart + len)
                    break;
                if (p + Token.Length > segmentStart + len)
                    continue;

                Token.CopyTo(0, segment, (int)(p - segmentStart), Token.Length);
                count++;
            }
            return count;
        }

        private static long CopyFiller(char[] filler, long position, char[] destination, int length)
        {
            int copied = 0;
            int idx = (int)(position % filler.Length);
            while (copied < length)
            {
                int n = Math.Min(length - copied, filler.Length - idx);
                Array.Copy(filler, idx, destination, copied, n);
                copied += n;
                idx = 0;
            }
            return position + length;
        }

        /// <summary>
        /// A block of random lowercase words that never contains the search text.
        /// </summary>
        private static char[] CreateFiller()
        {
            Random rng = new(42);
            char[] block = new char[1_048_573];
            int wordLength = 0;
            int nextSpace = rng.Next(3, 10);
            for (int i = 0; i < block.Length; i++)
            {
                if (wordLength == nextSpace)
                {
                    block[i] = ' ';
                    wordLength = 0;
                    nextSpace = rng.Next(3, 10);
                }
                else
                {
                    block[i] = (char)('a' + rng.Next(26));
                    wordLength++;
                }
            }

            // wrap-around safe check
            string text = new(block);
            if (text.Contains(SearchText, StringComparison.Ordinal) ||
                (text + text[..SearchText.Length]).Contains(SearchText, StringComparison.Ordinal))
            {
                block[0] = 'x';
                return CreateFillerWithout(block);
            }
            return block;
        }

        private static char[] CreateFillerWithout(char[] block)
        {
            string text = new(block);
            int idx;
            while ((idx = text.IndexOf(SearchText, StringComparison.Ordinal)) >= 0)
            {
                block[idx] = 'z';
                text = new string(block);
            }
            return block;
        }
    }
}
