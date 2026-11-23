using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace dnGREP.Benchmarks
{
    /// <summary>
    /// Shared job: a short run, since each iteration reads a large file. The solution projects only
    /// build for a specific platform, so the generated benchmark project must be built for x64.
    /// For a more precise comparison, run with: --iterationCount 15 --warmupCount 3
    /// </summary>
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.Default
                .WithPlatform(Platform.X64)
                .WithStrategy(RunStrategy.Throughput)
                .WithLaunchCount(1)
                .WithWarmupCount(1)
                .WithIterationCount(5)
                .WithArguments([new MsBuildArgument("/p:Platform=x64")]));
        }
    }
}
