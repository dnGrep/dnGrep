using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using dnGREP.Common;
using dnGREP.Engines;

namespace dnGREP.Benchmarks
{
    /// <summary>
    /// Reproduces the real search path (GrepCore, encoding detection, parallel search) over several
    /// large files in one process and reports peak private bytes and what is retained afterwards.
    /// BenchmarkDotNet does not report peak process memory, so this runs on its own.
    /// </summary>
    public static class PeakMemoryRunner
    {
        private const double MB = 1024.0 * 1024.0;

        // fully optimized so the JIT does not keep the results alive in a stack slot after the last use
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static int Run(string[] args)
        {
            Dictionary<string, string> options = ParseArgs(args);
            if (options.ContainsKey("help"))
            {
                PrintUsage();
                return 0;
            }

            int length = GetInt(options, "length", 1_000_000);
            LineEnding eol = GetEnum(options, "eol", LineEnding.LF);
            MatchDensity density = GetEnum(options, "density", MatchDensity.Sparse);
            string type = options.GetValueOrDefault("type", "plain").ToLowerInvariant();
            int sizeMB = GetInt(options, "mb", TestData.DefaultSizeMB);
            int fileCount = GetInt(options, "files", 8);
            int parallel = GetInt(options, "parallel", -1);
            bool serial = options.ContainsKey("serial");
            bool multiline = options.ContainsKey("multiline");
            int budgetPercent = GetInt(options, "budget", SearchMemoryGate.DefaultBudgetPercent);
            bool noGate = options.ContainsKey("no-gate");
            string label = options.GetValueOrDefault("label", string.Empty);
            string? csv = options.GetValueOrDefault("csv");

            (SearchType searchType, GrepSearchOption searchOptions) = type switch
            {
                "plain" => (SearchType.PlainText, GrepSearchOption.Global | GrepSearchOption.CaseSensitive),
                "wholeword" => (SearchType.PlainText, GrepSearchOption.Global | GrepSearchOption.CaseSensitive | GrepSearchOption.WholeWord),
                "regex" => (SearchType.Regex, GrepSearchOption.Global | GrepSearchOption.CaseSensitive),
                _ => throw new ArgumentException($"Unknown search type '{type}'; use plain, wholeword or regex")
            };

            if (multiline)
            {
                searchOptions |= GrepSearchOption.Multiline;
            }

            List<string> files = PrepareFiles(length, eol, density, sizeMB, fileCount, out long expectedMatches);

            GrepSettings.Instance.Set(GrepSettings.Key.MaxDegreeOfParallelism, parallel);
            GrepSettings.Instance.Set(GrepSettings.Key.LimitSearchMemory, !noGate);
            GrepSettings.Instance.Set(GrepSettings.Key.SearchMemoryBudgetPercent, budgetPercent);
            GrepCore.Initialize();

            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            using Process process = Process.GetCurrentProcess();
            process.Refresh();
            double startPrivate = process.PrivateMemorySize64 / MB;
            long startAllocated = GC.GetTotalAllocatedBytes(true);
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            TimeSpan startPause = GC.GetTotalPauseDuration();

            using PeakSampler sampler = new(process);
            Stopwatch sw = Stopwatch.StartNew();
            List<GrepSearchResult>? results = RunSearch(files, searchType, searchOptions, !serial);
            sw.Stop();
            sampler.Stop();

            long matches = results.Sum(r => r.Matches.Count);
            long allocated = GC.GetTotalAllocatedBytes(true) - startAllocated;
            GCMemoryInfo lastGc = GC.GetGCMemoryInfo(GCKind.Any);
            double lohAfterLastGc = lastGc.GenerationInfo.Length > 3 ? lastGc.GenerationInfo[3].SizeAfterBytes / MB : 0;
            double heapAfterSearch = GC.GetTotalMemory(false) / MB;
            process.Refresh();
            double privateAfterSearch = process.PrivateMemorySize64 / MB;

            // what the application still holds once the search is done: the results are alive
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            double retainedHeap = GC.GetTotalMemory(true) / MB;
            process.Refresh();
            double retainedPrivate = process.PrivateMemorySize64 / MB;

            GC.KeepAlive(results);
            results = null;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            double releasedHeap = GC.GetTotalMemory(true) / MB;
            process.Refresh();
            double releasedPrivate = process.PrivateMemorySize64 / MB;

            double pauseMs = (GC.GetTotalPauseDuration() - startPause).TotalMilliseconds;

            Console.WriteLine();
            Console.WriteLine($"Files: {files.Count} x {sizeMB} MB, line length {(length <= 0 ? "single line" : length.ToString(CultureInfo.InvariantCulture))}, {eol}, density {density}, search {type}, " +
                $"{(serial ? "serial" : $"parallel (max {parallel})")}, {(multiline ? "multiline" : "line by line")}, budget {(noGate ? "off" : $"{budgetPercent}% of memory ({SearchMemoryGate.BudgetBytes(budgetPercent, SearchMemoryGate.TotalMemoryBytes()) / MB:N0} MB)")}");
            Console.WriteLine($"Elapsed:                       {sw.Elapsed.TotalSeconds,10:N2} s");
            Console.WriteLine($"Matches found:                 {matches,10:N0} (expected {expectedMatches * files.Count:N0})");
            Console.WriteLine($"Private bytes at start:        {startPrivate,10:N0} MB");
            Console.WriteLine($"PEAK private bytes:            {sampler.PeakPrivate / MB,10:N0} MB");
            Console.WriteLine($"PEAK working set:              {sampler.PeakWorkingSet / MB,10:N0} MB");
            Console.WriteLine($"PEAK managed heap (sampled):   {sampler.PeakHeap / MB,10:N0} MB");
            Console.WriteLine($"Total allocated:               {allocated / MB,10:N0} MB");
            Console.WriteLine($"GCs gen0/gen1/gen2:            {GC.CollectionCount(0) - gen0} / {GC.CollectionCount(1) - gen1} / {GC.CollectionCount(2) - gen2}");
            Console.WriteLine($"GC pause total:                {pauseMs,10:N0} ms");
            Console.WriteLine($"LOH size after last GC:        {lohAfterLastGc,10:N0} MB");
            Console.WriteLine($"After search (no collection):  heap {heapAfterSearch,7:N0} MB, private {privateAfterSearch,7:N0} MB");
            Console.WriteLine($"Results alive, after full GC:  heap {retainedHeap,7:N0} MB, private {retainedPrivate,7:N0} MB");
            Console.WriteLine($"Results released, full GC:     heap {releasedHeap,7:N0} MB, private {releasedPrivate,7:N0} MB");

            if (!string.IsNullOrEmpty(csv))
            {
                AppendCsv(csv, label, length, eol, density, type, sizeMB, files.Count, serial ? 0 : parallel,
                    sw.Elapsed.TotalSeconds, matches, sampler.PeakPrivate / MB, sampler.PeakWorkingSet / MB,
                    sampler.PeakHeap / MB, allocated / MB, GC.CollectionCount(2) - gen2, pauseMs,
                    retainedHeap, retainedPrivate, releasedHeap);
            }

            return matches == expectedMatches * files.Count ? 0 : 2;
        }

        // GrepCore keeps its own list of the results, so it must not outlive the search
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static List<GrepSearchResult> RunSearch(List<string> files, SearchType searchType,
            GrepSearchOption searchOptions, bool searchParallel)
        {
            GrepCore core = new()
            {
                SearchParams = new GrepEngineInitParams(0, 0, 0.7, false, searchParallel)
            };
            return core.Search(files, searchType, TestData.SearchText, searchOptions, -1);
        }

        private static List<string> PrepareFiles(int length, LineEnding eol, MatchDensity density, int sizeMB,
            int fileCount, out long expectedMatches)
        {
            TestFile source = TestData.GetOrCreate(length, eol, density, sizeMB);
            expectedMatches = source.Matches;

            string folder = Path.Combine(TestData.RootFolder, $"peak_L{length}_{eol}_{density}_{sizeMB}MB");
            Directory.CreateDirectory(folder);

            List<string> files = [];
            for (int i = 0; i < fileCount; i++)
            {
                string copy = Path.Combine(folder, $"file{i:D3}.txt");
                if (!File.Exists(copy) || new FileInfo(copy).Length != source.Length)
                {
                    File.Copy(source.Path, copy, true);
                }
                files.Add(copy);
            }
            return files;
        }

        private static void AppendCsv(string path, string label, int length, LineEnding eol, MatchDensity density,
            string type, int sizeMB, int files, int parallel, double seconds, long matches, double peakPrivate,
            double peakWorkingSet, double peakHeap, double allocated, int gen2, double pauseMs,
            double retainedHeap, double retainedPrivate, double releasedHeap)
        {
            bool writeHeader = !File.Exists(path);
            using StreamWriter writer = new(path, true);
            if (writeHeader)
            {
                writer.WriteLine("label,lineLength,eol,density,type,sizeMB,files,parallel,seconds,matches,peakPrivateMB," +
                    "peakWorkingSetMB,peakHeapMB,allocatedMB,gen2Count,pauseMs,retainedHeapMB,retainedPrivateMB,releasedHeapMB");
            }

            string[] fields =
            [
                label, length.ToString(CultureInfo.InvariantCulture), eol.ToString(), density.ToString(), type,
                sizeMB.ToString(CultureInfo.InvariantCulture), files.ToString(CultureInfo.InvariantCulture),
                parallel.ToString(CultureInfo.InvariantCulture), F(seconds), matches.ToString(CultureInfo.InvariantCulture),
                F(peakPrivate), F(peakWorkingSet), F(peakHeap), F(allocated), gen2.ToString(CultureInfo.InvariantCulture),
                F(pauseMs), F(retainedHeap), F(retainedPrivate), F(releasedHeap)
            ];
            writer.WriteLine(string.Join(',', fields));

            static string F(double d) => d.ToString("F1", CultureInfo.InvariantCulture);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("""
                Usage: dnGREP.Benchmarks peak [options]

                  --length N      line length in chars; 0 = whole file is one line (default 1000000)
                  --eol lf|crlf   line ending (default lf)
                  --density D     None | Last | Sparse | Dense (default Sparse)
                  --type T        plain | wholeword | regex (default plain)
                  --mb N          size of each file in MB (default 64, or DNGREP_BENCH_MB)
                  --files N       number of files to search (default 8)
                  --parallel N    max degree of parallelism, -1 = default (default -1)
                  --serial        turn off the "search in parallel" option
                  --multiline     search with the Multiline option (reads each file as a whole)
                  --budget N      search memory budget for parallel files, as a percent of total memory (default 25)
                                    --no-gate       turn the search memory limit off
                  --label TEXT    label for the csv row, e.g. "baseline"
                  --csv PATH      append a result row to this csv file

                Exit code is 2 if the match count is not what the generated files contain.
                """);
        }

        internal static Dictionary<string, string> ParseArgs(string[] args)
        {
            Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                    continue;

                string key = args[i][2..];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    result[key] = args[++i];
                else
                    result[key] = string.Empty;
            }
            return result;
        }

        internal static int GetInt(Dictionary<string, string> options, string key, int defaultValue) =>
            options.TryGetValue(key, out string? value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)
                ? i : defaultValue;

        internal static T GetEnum<T>(Dictionary<string, string> options, string key, T defaultValue) where T : struct, Enum =>
            options.TryGetValue(key, out string? value) && Enum.TryParse(value, true, out T e) ? e : defaultValue;

        /// <summary>
        /// Samples the process memory while the search runs to find the peaks.
        /// </summary>
        private sealed class PeakSampler : IDisposable
        {
            private readonly Process process;
            private readonly Thread thread;
            private volatile bool stop;

            public long PeakPrivate { get; private set; }
            public long PeakWorkingSet { get; private set; }
            public long PeakHeap { get; private set; }

            public PeakSampler(Process process)
            {
                this.process = process;
                thread = new Thread(Sample) { IsBackground = true, Name = "PeakSampler" };
                thread.Start();
            }

            private void Sample()
            {
                while (!stop)
                {
                    process.Refresh();
                    PeakPrivate = Math.Max(PeakPrivate, process.PrivateMemorySize64);
                    PeakWorkingSet = Math.Max(PeakWorkingSet, process.WorkingSet64);
                    PeakHeap = Math.Max(PeakHeap, GC.GetTotalMemory(false));
                    Thread.Sleep(20);
                }
            }

            public void Stop()
            {
                stop = true;
                thread.Join();
            }

            public void Dispose() => Stop();
        }
    }
}
