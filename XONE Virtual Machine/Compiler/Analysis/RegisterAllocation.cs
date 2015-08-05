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

        /// <summary>
        /// The spilled registers
        /// </summary>
        public IReadOnlyDictionary<VirtualRegister, SpilledRegister> Spilled { get; }

        /// <summary>
        /// Creates a new register allocation
        /// </summary>
        /// <param name="allocated">The allocated registers</param>
        /// <param name="spilled">The spilled registers</param>
        public RegisterAllocation(IDictionary<LiveInterval, int> allocated, IList<LiveInterval> spilled)
        {
            int stackIndex = 0;
            var newSpilled = new Dictionary<VirtualRegister, SpilledRegister>();
            foreach (var interval in spilled)
            {
                newSpilled.Add(interval.VirtualRegister, new SpilledRegister(stackIndex, interval));
                stackIndex++;
            }

            this.Spilled = new ReadOnlyDictionary<VirtualRegister, SpilledRegister>(newSpilled);

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
            get { return this.Spilled.Count; }
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
            if (this.Spilled.TryGetValue(virtualRegister, out spilledRegister))
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
}
