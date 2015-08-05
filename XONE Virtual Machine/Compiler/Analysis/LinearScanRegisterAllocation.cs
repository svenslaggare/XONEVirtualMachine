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
        private readonly IDictionary<VirtualRegister, AllocatedRegister> allocated = new Dictionary<VirtualRegister, AllocatedRegister>();
        private readonly IDictionary<VirtualRegister, SpilledRegister> spilled = new Dictionary<VirtualRegister, SpilledRegister>();

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
        public int? GetRegister(VirtualRegister virtualRegister)
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
        public AllocatedRegister? GetRegisterAllocation(VirtualRegister virtualRegister)
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
        public int? GetStackIndex(VirtualRegister virtualRegister)
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
        /// <param name="numIntRegisters">The number of int registers</param>
        /// <param name="numFloatRegisters">The number of float registers</param>
        public static RegisterAllocation Allocate(IList<LiveInterval> liveIntervals, int? numIntRegisters = null, int? numFloatRegisters = null)
        {
            var allocatedRegisteres = new Dictionary<LiveInterval, int>();
            var spilledRegisters = new List<LiveInterval>();

            numIntRegisters = numIntRegisters ?? 7;
            numFloatRegisters = numFloatRegisters ?? 5;

            //If we do not got any registers, spill all.
            if (numIntRegisters + numIntRegisters == 0)
            {
                foreach (var interval in liveIntervals)
                {
                    spilledRegisters.Add(interval);
                }

                return new RegisterAllocation(allocatedRegisteres, spilledRegisters);
            }

            var freeIntRegisters = new SortedSet<int>(Enumerable.Range(0, numIntRegisters.Value));
            var freeFloatRegisters = new SortedSet<int>(Enumerable.Range(0, numFloatRegisters.Value));

            var active = new SortedSet<LiveInterval>(new CompareByEndPoint());
            liveIntervals = liveIntervals.OrderBy(x => x.Start).ToList();

            foreach (var interval in liveIntervals)
            {
                ExplireOldIntervals(
                    allocatedRegisteres,
                    active,
                    freeIntRegisters,
                    freeFloatRegisters,
                    interval);

                var regType = interval.VirtualRegister.Type;

                var activeOfType = active.Where(x => x.VirtualRegister.Type == regType);
                int maxRegs = 0;
                SortedSet<int> freeRegs;

                if (regType == VirtualRegisterType.Float)
                {
                    maxRegs = numFloatRegisters.Value;
                    freeRegs = freeFloatRegisters;
                }
                else
                {
                    maxRegs = numIntRegisters.Value;
                    freeRegs = freeIntRegisters;
                }

                if (activeOfType.Count() == maxRegs)
                {
                    SplitAtInterval(
                        allocatedRegisteres,
                        spilledRegisters,
                        active,
                        interval,
                        regType);
                }
                else
                {
                    var freeReg = freeRegs.First();
                    freeRegs.Remove(freeReg);
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
        /// <param name="freeIntRegisters">The free int registers</param>
        /// <param name="freeFloatRegisters">The free float registers</param>
        /// <param name="currentInterval">The current intervals</param>
        private static void ExplireOldIntervals(
            IDictionary<LiveInterval, int> allocatedRegisteres,
            ISet<LiveInterval> active,
            ISet<int> freeIntRegisters,
            ISet<int> freeFloatRegisters,
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
                
                if (interval.VirtualRegister.Type == VirtualRegisterType.Float)
                {
                    freeFloatRegisters.Add(allocatedRegisteres[interval]);
                }
                else
                {
                    freeIntRegisters.Add(allocatedRegisteres[interval]);
                }
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
        /// <param name="registerType">The register type</param>
        private static void SplitAtInterval(
            IDictionary<LiveInterval, int> allocatedRegisteres,
            IList<LiveInterval> spilledRegisters,
            ISet<LiveInterval> active,
            LiveInterval currentInterval,
            VirtualRegisterType registerType)
        {
            var spill = active.Where(x => x.VirtualRegister.Type == registerType).Last();

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
