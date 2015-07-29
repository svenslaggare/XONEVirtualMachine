using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Compiler.Analysis
{
    /// <summary>
    /// Represents a register allocation
    /// </summary>
    public class RegisterAllocation
    {
        /// <summary>
        /// The allocated registers
        /// </summary>
        public IReadOnlyDictionary<LiveInterval, int> Allocated { get; }

        /// <summary>
        /// The spilled registers
        /// </summary>
        public IList<LiveInterval> Spilled { get; }

        /// <summary>
        /// Creates a new register allocation
        /// </summary>
        /// <param name="allocated">The allocated registers</param>
        /// <param name="spilled">The spilled registers</param>
        public RegisterAllocation(IDictionary<LiveInterval, int> allocated, IList<LiveInterval> spilled)
        {
            this.Allocated = new ReadOnlyDictionary<LiveInterval, int>(allocated);
            this.Spilled = new ReadOnlyCollection<LiveInterval>(spilled);
        }
    }

    /// <summary>
    /// Represents register allocation using the linear-scan algorithm.
    /// </summary>
    public static class LinearScanRegisterAllocation
    {
        /// <summary>
        /// Compares by end point
        /// </summary>
        private static int CompareByEndPoint(LiveInterval x, LiveInterval y)
        {
            return x.End.CompareTo(y.End);
        }

        /// <summary>
        /// Allocates the registers
        /// </summary>
        /// <param name="liveIntervals">The live intervals</param>
        /// <param name="numRegisters">The number of registers</param>
        public static RegisterAllocation Allocate(IList<LiveInterval> liveIntervals, int numRegisters = 7)
        {
            var allocatedRegisteres = new Dictionary<LiveInterval, int>();
            var spilledRegisters = new List<LiveInterval>();

            var freeRegisters = new HashSet<int>(Enumerable.Range(0, numRegisters));
            var active = new List<LiveInterval>();
            liveIntervals = liveIntervals.OrderBy(x => x.Start).ToList();

            foreach (var interval in liveIntervals)
            {
                ExplireOldIntervals(allocatedRegisteres, active, freeRegisters, interval);

                if (active.Count == numRegisters)
                {
                    SplitAtInterval(allocatedRegisteres, spilledRegisters, active, interval);
                }
                else
                {
                    var freeReg = freeRegisters.First();
                    freeRegisters.Remove(freeReg);
                    allocatedRegisteres.Add(interval, freeReg);
                    active.Add(interval);
                    active.Sort(CompareByEndPoint);
                }
            }

            return new RegisterAllocation(allocatedRegisteres, spilledRegisters);
        }

        /// <summary>
        /// Expires old intervals
        /// </summary>
        /// <param name="allocatedRegisteres">The allocated registers</param>
        /// <param name="active">The active registers</param>
        /// <param name="freeRegisters">The free registers</param>
        /// <param name="currentInterval">The current intervals</param>
        private static void ExplireOldIntervals(
            IDictionary<LiveInterval, int> allocatedRegisteres,
            List<LiveInterval> active,
            ISet<int> freeRegisters,
            LiveInterval currentInterval)
        {
            foreach (var interval in active)
            {
                if (interval.End >= currentInterval.Start)
                {
                    return;
                }

                active.Remove(interval);
                freeRegisters.Add(allocatedRegisteres[interval]);
            }
        }

        /// <summary>
        /// Splits at the given interval
        /// </summary>
        /// <param name="allocatedRegisteres">The allocated registers</param>
        /// <param name="spilledRegisters">The spilled registers</param>
        /// <param name="active">The active registers</param>
        /// <param name="currentInterval">The current intervals</param>
        private static void SplitAtInterval(
            IDictionary<LiveInterval, int> allocatedRegisteres,
            IList<LiveInterval> spilledRegisters,
            List<LiveInterval> active,
            LiveInterval currentInterval)
        {
            var spill = active.Last();

            if (spill.End > currentInterval.End)
            {
                allocatedRegisteres.Add(currentInterval, allocatedRegisteres[spill]);

                spilledRegisters.Add(spill);
                allocatedRegisteres.Remove(spill);

                active.Remove(spill);
                active.Add(currentInterval);
                active.Sort(CompareByEndPoint);
            }
            else
            {
                spilledRegisters.Add(currentInterval);
                allocatedRegisteres.Remove(currentInterval);
            }
        }
    }
}
