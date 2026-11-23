using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using dnGREP.Common;
using dnGREP.Engines;

namespace dnGREP.Benchmarks
{
    /// <summary>
    /// Single file, single thread search through the plain text engine, as used for non-multiline
    /// searches: reads one line at a time through the EolReader.
    /// Look at Allocated and the Gen2 column: LOH allocations are counted as Gen2.
    /// </summary>
    [MemoryDiagnoser]
    [Config(typeof(BenchmarkConfig))]
    public class PlainTextEngineBenchmarks
    {
        private static readonly UTF8Encoding utf8 = new(false);

        private const GrepSearchOption Options = GrepSearchOption.Global | GrepSearchOption.CaseSensitive;

        private GrepEnginePlainText engine = null!;
        private TestFile file = null!;

        /// <summary>0 means the whole file is a single line</summary>
        [Params(80, 10_000, 1_000_000, 0)]
        public int LineLength { get; set; }

        [Params(LineEnding.LF, LineEnding.CRLF)]
        public LineEnding Eol { get; set; }

        [Params(MatchDensity.None, MatchDensity.Sparse, MatchDensity.Dense)]
        public MatchDensity Density { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            file = TestData.GetOrCreate(LineLength, Eol, Density);
            engine = new GrepEnginePlainText();
            engine.Initialize(GrepEngineInitParams.Default, FileFilter.Default, null);
        }

        [Benchmark]
        public int PlainText() => CountMatches(SearchType.PlainText, Options);

        [Benchmark]
        public int PlainTextWholeWord() => CountMatches(SearchType.PlainText, Options | GrepSearchOption.WholeWord);

        [Benchmark]
        public int Regex() => CountMatches(SearchType.Regex, Options);

        private int CountMatches(SearchType searchType, GrepSearchOption options)
        {
            var results = engine.Search(file.Path, TestData.SearchText, searchType, options, utf8);
            return results.Sum(r => r.Matches.Count);
        }
    }
}
