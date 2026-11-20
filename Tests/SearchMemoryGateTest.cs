using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dnGREP.Common;
using Xunit;

namespace Tests
{
    public class SearchMemoryGateTest
    {
        private const long MB = 1024 * 1024;

        [Fact]
        public void FilesWithinTheBudgetRunTogether()
        {
            SearchMemoryGate gate = new(100 * MB);

            long a = gate.Enter(30 * MB, default);
            long b = gate.Enter(30 * MB, default);
            long c = gate.Enter(30 * MB, default);

            Assert.Equal(3, gate.ActiveCount);
            Assert.Equal(90 * MB, gate.InUse);

            gate.Exit(a);
            gate.Exit(b);
            gate.Exit(c);
            Assert.Equal(0, gate.ActiveCount);
            Assert.Equal(0, gate.InUse);
        }

        [Fact]
        public void FileLargerThanTheBudgetStillRunsAlone()
        {
            SearchMemoryGate gate = new(10 * MB);

            long reserved = gate.Enter(500 * MB, default);

            Assert.Equal(10 * MB, reserved);
            Assert.Equal(1, gate.ActiveCount);
            gate.Exit(reserved);
        }

        [Fact]
        public async Task FileOverTheBudgetWaitsUntilOthersFinish()
        {
            SearchMemoryGate gate = new(100 * MB);
            long first = gate.Enter(80 * MB, default);

            using ManualResetEventSlim entered = new();
            long second = 0;
            Task waiter = Task.Run(() =>
            {
                second = gate.Enter(40 * MB, default);
                entered.Set();
            }, TestContext.Current.CancellationToken);

            Assert.False(entered.Wait(500, TestContext.Current.CancellationToken));
            Assert.Equal(1, gate.ActiveCount);

            gate.Exit(first);

            Assert.True(entered.Wait(5000, TestContext.Current.CancellationToken));
            await waiter;
            Assert.Equal(40 * MB, second);
            gate.Exit(second);
        }

        [Fact]
        public async Task MemoryPressureHoldsNewFilesWhileAnotherIsActive()
        {
            long workingSet = 900 * MB;
            SearchMemoryGate gate = new(10_000 * MB, 800 * MB, () => Interlocked.Read(ref workingSet));

            long first = gate.Enter(MB, default); // always admitted when nothing is active

            using ManualResetEventSlim entered = new();
            long second = 0;
            Task waiter = Task.Run(() =>
            {
                second = gate.Enter(MB, default);
                entered.Set();
            }, TestContext.Current.CancellationToken);

            Assert.False(entered.Wait(500, TestContext.Current.CancellationToken));

            Interlocked.Exchange(ref workingSet, 100 * MB);

            Assert.True(entered.Wait(5000, TestContext.Current.CancellationToken));
            await waiter;
            gate.Exit(first);
            gate.Exit(second);
        }

        [Fact]
        public async Task CancelWakesAWaitingFile()
        {
            SearchMemoryGate gate = new(10 * MB);
            long first = gate.Enter(10 * MB, default);

            PauseCancelTokenSource source = new();
            PauseCancelToken token = source.Token;
            Task waiter = Task.Run(() => gate.Enter(5 * MB, token), TestContext.Current.CancellationToken);

            await Task.Delay(300, TestContext.Current.CancellationToken);
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
            Assert.Equal(1, gate.ActiveCount);
            gate.Exit(first);
        }

        [Fact]
        public void ConcurrentFilesNeverExceedTheBudget()
        {
            SearchMemoryGate gate = new(100 * MB);
            int running = 0, maxRunning = 0;

            Parallel.For(0, 40, new ParallelOptions { MaxDegreeOfParallelism = 16, CancellationToken = TestContext.Current.CancellationToken }, _ =>
            {
                long reserved = gate.Enter(30 * MB, default);
                try
                {
                    int now = Interlocked.Increment(ref running);
                    int seen;
                    while (now > (seen = Volatile.Read(ref maxRunning)) &&
                        Interlocked.CompareExchange(ref maxRunning, now, seen) != seen) { }
                    Thread.Sleep(20);
                }
                finally
                {
                    Interlocked.Decrement(ref running);
                    gate.Exit(reserved);
                }
            });

            Assert.InRange(maxRunning, 1, 3); // 3 x 30 MB fits in 100 MB, 4 does not
            Assert.Equal(0, gate.ActiveCount);
        }

        [Fact]
        public void EstimateGrowsWithFileSizeForMultilineButNotForShortLines()
        {
            string path = Path.Combine(Path.GetTempPath(), "dnGREP-cost-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                byte[] line = System.Text.Encoding.ASCII.GetBytes(new string('x', 78) + "\r\n");
                using (FileStream fs = File.Create(path))
                {
                    for (int i = 0; i < 100 * MB / line.Length; i++)
                    {
                        fs.Write(line);
                    }
                }
                long size = new FileInfo(path).Length;

                long lineCost = GrepCore.EstimateSearchCost(path, GrepSearchOption.None);
                long multiline = GrepCore.EstimateSearchCost(path, GrepSearchOption.Multiline);

                Assert.True(multiline >= 6 * size);
                Assert.True(lineCost < 20 * MB, $"line cost {lineCost}");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void EstimateTreatsAFileWithoutLineBreaksAsOneLongLine()
        {
            string path = Path.Combine(Path.GetTempPath(), "dnGREP-cost-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                using (FileStream fs = File.Create(path))
                {
                    fs.Write(System.Text.Encoding.ASCII.GetBytes(new string('x', 64 * 1024 * 1024)));
                }
                long size = new FileInfo(path).Length;

                long cost = GrepCore.EstimateSearchCost(path, GrepSearchOption.None);

                Assert.True(cost >= 4 * size, $"cost {cost}");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void EstimateUsesTheLongestLineInTheSample()
        {
            string path = Path.Combine(Path.GetTempPath(), "dnGREP-cost-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                byte[] shortLine = System.Text.Encoding.ASCII.GetBytes(new string('x', 78) + "\n");
                byte[] longLine = System.Text.Encoding.ASCII.GetBytes(new string('y', 512 * 1024) + "\n");
                using (FileStream fs = File.Create(path))
                {
                    fs.Write(longLine);
                    for (int i = 0; i < 20 * MB / shortLine.Length; i++)
                    {
                        fs.Write(shortLine);
                    }
                }

                long cost = GrepCore.EstimateSearchCost(path, GrepSearchOption.None);

                Assert.InRange(cost, 4 * 512 * 1024, 20 * MB);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void EstimateForMissingFileFallsBackToBaseCost()
        {
            long cost = GrepCore.EstimateSearchCost(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt"),
                GrepSearchOption.Multiline);

            Assert.True(cost > 0);
        }

        [Fact]
        public async Task DisabledGateNeverBlocksAndTracksNothing()
        {
            SearchMemoryGate gate = SearchMemoryGate.Disabled;
            PauseCancelToken token = default;

            long first = await Task.Run(() => gate.Enter(long.MaxValue, token), TestContext.Current.CancellationToken);
            long second = await Task.Run(() => gate.Enter(long.MaxValue, token), TestContext.Current.CancellationToken);

            Assert.False(gate.IsEnabled);
            Assert.Equal(0, first);
            Assert.Equal(0, second);
            Assert.Equal(0, gate.ActiveCount);
            Assert.Equal(0, gate.InUse);

            gate.Exit(first);
            gate.Exit(second);
            Assert.Equal(0, gate.ActiveCount);
        }

        [Fact]
        public void CreatedGateIsEnabled()
        {
            Assert.True(SearchMemoryGate.Create(25, 8L * 1024 * MB).IsEnabled);
        }

        [Fact]
        public void CreateSizesTheBudgetAsAPercentOfTotalMemory()
        {
            long total = 16L * 1024 * MB;

            Assert.Equal(4L * 1024 * MB, SearchMemoryGate.Create(25, total).Budget);
            Assert.Equal(8L * 1024 * MB, SearchMemoryGate.Create(50, total).Budget);
            Assert.Equal(total / 100, SearchMemoryGate.Create(1, total).Budget);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void CreateUsesTheDefaultPercentWhenInvalid(int percent)
        {
            long total = 8L * 1024 * MB;

            Assert.Equal(SearchMemoryGate.Create(SearchMemoryGate.DefaultBudgetPercent, total).Budget,
                SearchMemoryGate.Create(percent, total).Budget);
        }

        [Fact]
        public void CreateWithAVeryLargePercentDoesNotOverflow()
        {
            SearchMemoryGate gate = SearchMemoryGate.Create(int.MaxValue, 16L * 1024 * MB);

            Assert.True(gate.Budget > 16L * 1024 * MB);
        }

        [Fact]
        public void CreateUsesTheMemoryOfTheMachine()
        {
            Assert.True(SearchMemoryGate.TotalMemoryBytes() > 0);
            Assert.True(SearchMemoryGate.Create(SearchMemoryGate.DefaultBudgetPercent).Budget > 0);
        }
    }
}
