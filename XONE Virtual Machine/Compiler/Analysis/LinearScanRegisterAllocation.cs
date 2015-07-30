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
    /// Represents a register allocation
    /// </summary>
    public class RegisterAllocation
    {
        private readonly IReadOnlyDictionary<LiveInterval, int> allocated;

        /// <summary>
        /// The spilled registers
        /// </summary>
        public IReadOnlyList<LiveInterval> Spilled { get; }

        private readonly IDictionary<int, AllocatedRegister> registers = new Dictionary<int, AllocatedRegister>();

        /// <summary>
        /// Creates a new register allocation
        /// </summary>
        /// <param name="allocated">The allocated registers</param>
        /// <param name="spilled">The spilled registers</param>
        public RegisterAllocation(IDictionary<LiveInterval, int> allocated, IList<LiveInterval> spilled)
        {
            this.allocated = new ReadOnlyDictionary<LiveInterval, int>(allocated);
            this.Spilled = new ReadOnlyCollection<LiveInterval>(spilled);

            foreach (var interval in this.allocated)
            {
                this.registers.Add(interval.Key.VirtualRegister, new AllocatedRegister(interval.Value, interval.Key));
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
            get { return this.Spilled.Count; }
        }

        /// <summary>
        /// Returns the register for the given virtual register
        /// </summary>
        /// <param name="virtualRegister">The virtual register</param>
        public int? GetRegister(int virtualRegister)
        {
            AllocatedRegister allocatedRegister;
            if (this.registers.TryGetValue(virtualRegister, out allocatedRegister))
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
            if (this.registers.TryGetValue(virtualRegister, out allocatedRegister))
            {
                return allocatedRegister;
            }

            return null;
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
        private static int CompareByEndPoint(LiveInterval x, LiveInterval y)
        {
            return x.End.CompareTo(y.End);
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

            var freeRegisters = new SortedSet<int>(Enumerable.Range(0, numRegisters));
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
