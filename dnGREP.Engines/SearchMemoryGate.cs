using System;
using System.Threading;

namespace dnGREP.Common
{
    /// <summary>
    /// Limits the estimated memory used by files that are searched at the same time.
    /// A worker asks for the estimated cost of a file before starting it and gives the cost
    /// back when done. The gate never blocks the only active file, so a single file that
    /// costs more than the whole budget still runs, alone.
    /// </summary>
    public sealed class SearchMemoryGate
    {
        /// <summary>The budget percent used when the setting is missing or invalid.</summary>
        public const int DefaultBudgetPercent = 25;

        private const int PollMilliseconds = 250;
        private const double PressureFraction = 0.75;

        private readonly object sync = new();
        private readonly bool enabled = true;
        private readonly Func<long> workingSet;
        private readonly long workingSetLimit;
        private long inUse;
        private int active;

        /// <summary>
        /// A gate that never waits, used when the memory limit is turned off.
        /// </summary>
        public static SearchMemoryGate Disabled { get; } = new();

        private SearchMemoryGate()
        {
            enabled = false;
            Budget = long.MaxValue;
            workingSet = () => 0;
        }

        /// <param name="budgetBytes">Total estimated bytes that concurrent files may use.</param>
        /// <param name="workingSetLimitBytes">While the process working set is above this, a new file
        /// only starts when no other file is being searched. Zero disables the check.</param>
        /// <param name="workingSetProvider">Overrides the process working set, for tests.</param>
        public SearchMemoryGate(long budgetBytes, long workingSetLimitBytes = 0, Func<long>? workingSetProvider = null)
        {
            Budget = Math.Max(1, budgetBytes);
            workingSetLimit = workingSetLimitBytes;
            workingSet = workingSetProvider ?? (() => Environment.WorkingSet);
        }

        /// <summary>
        /// The memory available to the process: physical memory, or the container or GC limit when one is set.
        /// This is the total, not the memory that is currently free.
        /// </summary>
        public static long TotalMemoryBytes()
        {
            long total = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            return total > 0 ? total : 8L * 1024 * 1024 * 1024;
        }

        /// <summary>
        /// Converts a percent of the total memory to bytes. A value below 1 uses <see cref="DefaultBudgetPercent"/>.
        /// </summary>
        public static long BudgetBytes(int budgetPercent, long totalMemoryBytes)
        {
            if (budgetPercent < 1)
            {
                budgetPercent = DefaultBudgetPercent;
            }

            double bytes = totalMemoryBytes / 100.0 * budgetPercent;
            return (long)Math.Min(bytes, long.MaxValue / 2);
        }

        /// <summary>
        /// Creates a gate sized from the memory available to the process.
        /// </summary>
        /// <param name="budgetPercent">The percent of total memory that concurrent files may use.</param>
        public static SearchMemoryGate Create(int budgetPercent) => Create(budgetPercent, TotalMemoryBytes());

        public static SearchMemoryGate Create(int budgetPercent, long totalMemoryBytes)
        {
            return new SearchMemoryGate(BudgetBytes(budgetPercent, totalMemoryBytes),
                (long)(totalMemoryBytes * PressureFraction));
        }

        public long Budget { get; }

        /// <summary>False for <see cref="Disabled"/>, which does not limit anything.</summary>
        public bool IsEnabled => enabled;

        public int ActiveCount
        {
            get { lock (sync) { return active; } }
        }

        public long InUse
        {
            get { lock (sync) { return inUse; } }
        }

        /// <summary>
        /// Waits until a file with the estimated cost may start. Returns the amount reserved,
        /// which must be passed to <see cref="Exit"/>.
        /// </summary>
        /// <exception cref="OperationCanceledException">The search was canceled while waiting.</exception>
        public long Enter(long cost, PauseCancelToken pauseCancelToken)
        {
            if (!enabled)
            {
                return 0;
            }

            cost = Math.Clamp(cost, 1, Budget);

            while (true)
            {
                lock (sync)
                {
                    if (active == 0 || (inUse + cost <= Budget && !UnderMemoryPressure()))
                    {
                        inUse += cost;
                        active++;
                        return cost;
                    }

                    Monitor.Wait(sync, PollMilliseconds);
                }

                // outside the lock: this blocks while the search is paused
                pauseCancelToken.WaitWhilePausedOrThrowIfCancellationRequested();
            }
        }

        public void Exit(long reserved)
        {
            if (!enabled)
            {
                return;
            }

            lock (sync)
            {
                inUse -= reserved;
                active--;
                Monitor.PulseAll(sync);
            }
        }

        private bool UnderMemoryPressure() => workingSetLimit > 0 && workingSet() > workingSetLimit;
    }
}
