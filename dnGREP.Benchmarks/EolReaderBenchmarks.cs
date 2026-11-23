using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using dnGREP.Common;

namespace dnGREP.Benchmarks
{
    /// <summary>
    /// The line reader on its own, compared with StreamReader.ReadLine as a reference.
    /// </summary>
    [MemoryDiagnoser]
    [Config(typeof(BenchmarkConfig))]
    public class EolReaderBenchmarks
    {
        private static readonly UTF8Encoding utf8 = new(false);

        private TestFile file = null!;

        /// <summary>0 means the whole file is a single line</summary>
        [Params(80, 10_000, 1_000_000, 0)]
        public int LineLength { get; set; }

        [Params(LineEnding.LF, LineEnding.CRLF)]
        public LineEnding Eol { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            file = TestData.GetOrCreate(LineLength, Eol, MatchDensity.None);
        }

        [Benchmark]
        public long EolReaderReadLine()
        {
            using FileStream stream = new(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            using StreamReader baseReader = new(stream, utf8, false, 4096, true);
            using EolReader reader = new(baseReader);

            long chars = 0;
            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (line != null)
                    chars += line.Length;
            }
            return chars;
        }

        [Benchmark]
        public long StreamReaderReadLine()
        {
            using FileStream stream = new(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            using StreamReader reader = new(stream, utf8, false, 4096, true);

            long chars = 0;
            while (reader.ReadLine() is string line)
            {
                chars += line.Length;
            }
            return chars;
        }
    }
}
