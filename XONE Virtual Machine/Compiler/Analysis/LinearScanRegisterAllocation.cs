using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Compiler.Analysis
{
    /// <summary>
    /// Represents an allocated register
    /// </summary>
    public struct AllocatedRegister
    {
        /// <summary>
        /// The hardware register
        /// </summary>
        public int HardwareRegister { get; }

        /// <summary>
        /// The liveness information
        /// </summary>
        public LiveInterval LiveInterval { get; }

        /// <summary>
        /// Creates a new allocated register
        /// </summary>
        /// <param name="hardwareRegister">The hardware register</param>
        /// <param name="liveInterval">The liveness information</param>
        public AllocatedRegister(int hardwareRegister, LiveInterval liveInterval)
        {
            this.HardwareRegister = hardwareRegister;
            this.LiveInterval = liveInterval;
        }
    }

    /// <summary>
    /// Represents a spilled register
    /// </summary>
    public struct SpilledRegister
    {
        /// <summary>
        /// The stack index
        /// </summary>
        public int StackIndex { get; }

        /// <summary>
        /// The liveness information
        /// </summary>
        public LiveInterval LiveInterval { get; }

        /// <summary>
        /// Creates a new spilled register
        /// </summary>
        /// <param name="stackIndex">The stack index</param>
        /// <param name="liveInterval">The liveness information</param>
        public SpilledRegister(int stackIndex, LiveInterval liveInterval)
        {
            this.StackIndex = stackIndex;
            this.LiveInterval = liveInterval;
        }
    }

    /// <summary>
    /// Represents a register allocation
    /// </summary>
    public class RegisterAllocation
    {
        private readonly IDictionary<int, AllocatedRegister> allocated = new Dictionary<int, AllocatedRegister>();
        private readonly IDictionary<int, SpilledRegister> spilled = new Dictionary<int, SpilledRegister>();

        /// <summary>
        /// Creates a new register allocation
        /// </summary>
        /// <param name="allocated">The allocated registers</param>
        /// <param name="spilled">The spilled registers</param>
        public RegisterAllocation(IDictionary<LiveInterval, int> allocated, IList<LiveInterval> spilled)
        {
            int stackIndex = 0;
            foreach (var interval in spilled)
            {
                this.spilled.Add(interval.VirtualRegister, new SpilledRegister(stackIndex, interval));
                stackIndex++;
            }

            foreach (var interval in allocated)
            {
                this.allocated.Add(interval.Key.VirtualRegister, new AllocatedRegister(interval.Value, interval.Key));
            }
        }

        /// <summary>
        /// Returns the number of allocated registers
        /// </summary>
        public int NumAllocatedRegisters
        {
            get { return this.allocated.Count; }
        }

        /// <summary>
        /// Returns the number of spilled registers
        /// </summary>
        public int NumSpilledRegisters
        {
            get { return this.spilled.Count; }
        }

        /// <summary>
        /// Returns the register for the given virtual register
        /// </summary>
        /// <param name="virtualRegister">The virtual register</param>
        public int? GetRegister(int virtualRegister)
        {
            AllocatedRegister allocatedRegister;
            if (this.allocated.TryGetValue(virtualRegister, out allocatedRegister))
            {
                return allocatedRegister.HardwareRegister;
            }

            return null;
        }

        /// <summary>
        /// Returns the register allocation information for the given virtual register
        /// </summary>
        /// <param name="virtualRegister">The virtual register</param>
        public AllocatedRegister? GetRegisterAllocation(int virtualRegister)
        {
            AllocatedRegister allocatedRegister;
            if (this.allocated.TryGetValue(virtualRegister, out allocatedRegister))
            {
                return allocatedRegister;
            }

            return null;
        }

        /// <summary>
        /// Returns the stack index for the given virtual register
        /// </summary>
        /// <param name="virtualRegister">The virtual register</param>
        public int? GetStackIndex(int virtualRegister)
        {
            SpilledRegister spilledRegister;
            if (this.spilled.TryGetValue(virtualRegister, out spilledRegister))
            {
                return spilledRegister.StackIndex;
            }

            return null;
        }

        /// <summary>
        /// Returns the liveness information for the allocated registers
        /// </summary>
        public IEnumerable<LiveInterval> GetAllocatedRegisters()
        {
            return this.allocated.Values.Select(x => x.LiveInterval);
        }
    }

    /// <summary>
    /// Register allocation using the linear-scan algorithm.
    /// </summary>
    /// <remarks>See: <see cref="http://web.cs.ucla.edu/~palsberg/course/cs132/linearscan.pdf"/></remarks>
    public static class LinearScanRegisterAllocation
    {
        /// <summary>
        /// Compares by end point
        /// </summary>

        /// <summary>
        /// Compares by end point
        /// </summary>
        private class CompareByEndPoint : IComparer<LiveInterval>
        {
            public int Compare(LiveInterval x, LiveInterval y)
            {
                int compare = x.End.CompareTo(y.End);

                if (compare == 0)
                {
                    compare = x.Start.CompareTo(y.Start);

                    if (compare == 0)
                    {
                        return x.VirtualRegister.CompareTo(y.VirtualRegister);
                    }
                }

                return compare;
            }
        }

        /// <summary>
        /// Allocates registers
        /// </summary>
        /// <param name="liveIntervals">The live intervals</param>
        /// <param name="numRegisters">The number of registers</param>
        public static RegisterAllocation Allocate(IList<LiveInterval> liveIntervals, int numRegisters = 7)
        {
            var allocatedRegisteres = new Dictionary<LiveInterval, int>();
            var spilledRegisters = new List<LiveInterval>();

            //If we do not got any registers, spill all.
            if (numRegisters == 0)
            {
                foreach (var interval in liveIntervals)
                {
                    spilledRegisters.Add(interval);
                }

                return new RegisterAllocation(allocatedRegisteres, spilledRegisters);
            }

            var freeRegisters = new SortedSet<int>(Enumerable.Range(0, numRegisters));
            var active = new SortedSet<LiveInterval>(new CompareByEndPoint());
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
            ISet<LiveInterval> active,
            ISet<int> freeRegisters,
            LiveInterval currentInterval)
        {
            var toRemove = new List<LiveInterval>();

            foreach (var interval in active)
            {
                if (interval.End >= currentInterval.Start)
                {
                    break;
                }

                toRemove.Add(interval);
                freeRegisters.Add(allocatedRegisteres[interval]);
            }

            foreach (var interval in toRemove)
            {
                active.Remove(interval);
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
            ISet<LiveInterval> active,
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
            }
            else
            {
                spilledRegisters.Add(currentInterval);
                allocatedRegisteres.Remove(currentInterval);
            }
        }
    }
}
