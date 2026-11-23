using System;
using BenchmarkDotNet.Running;
using dnGREP.Benchmarks;

// "peak" runs the whole-process peak memory scenario; anything else goes to BenchmarkDotNet
if (args.Length > 0 && args[0].Equals("peak", StringComparison.OrdinalIgnoreCase))
{
    return PeakMemoryRunner.Run(args[1..]);
}

// "generate" writes test files to a folder, for use outside the benchmarks
if (args.Length > 0 && args[0].Equals("generate", StringComparison.OrdinalIgnoreCase))
{
    return FileGenerator.Run(args[1..]);
}

BenchmarkSwitcher.FromAssembly(typeof(PlainTextEngineBenchmarks).Assembly).Run(args);
return 0;
