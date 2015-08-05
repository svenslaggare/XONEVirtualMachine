using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine.Compiler.Analysis
{
    /// <summary>
    /// Represents an instruction using virtual registers
    /// </summary>
    public struct VirtualInstruction
    {
        /// <summary>
        /// The instruction
        /// </summary>
        public Instruction Instruction { get; }

        /// <summary>
        /// The virtual registers the instruction uses
        /// </summary>
        public IReadOnlyList<int> UsesRegisters { get; }

        /// <summary>
        /// The register that the instruction assigns
        /// </summary>
        public int? AssignRegister { get; }

        /// <summary>
        /// Creates a new virtual register instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="usesRegisters">The registers that is being used</param>
        /// <param name="assignRegister">The register that the instruction assigns to</param>
        public VirtualInstruction(Instruction instruction, IList<int> usesRegisters, int? assignRegister = null)
        {
            this.Instruction = instruction;
            this.UsesRegisters = new ReadOnlyCollection<int>(usesRegisters);
            this.AssignRegister = assignRegister;
        }

        public override string ToString()
        {
            if (this.AssignRegister != null)
            {
                string useValue = string.Join(", ", this.UsesRegisters);

                if (useValue == "")
                {
                    useValue = "{constant}";
                }

                return $"{{{this.Instruction}}}: {this.AssignRegister} <- {useValue}";
            }
            else
            {
                return $"{{{this.Instruction}}}: {string.Join(", ", this.UsesRegisters)}";
            }
        }
    }

    /// <summary>
    /// The types for virtual registers
    /// </summary>
    public enum VirtualRegisterType : byte
    {
        Integer,
        Float
    }

    /// <summary>
    /// Represents a virtual register
    /// </summary>
    public struct VirtualRegister
    {
        /// <summary>
        /// The type of the register
        /// </summary>
        public VirtualRegisterType Type { get; }

        /// <summary>
        /// The register number
        /// </summary>
        public int Number { get;}

        /// <summary>
        /// Creates a new virtual register
        /// </summary>
        /// <param name="type">The type of the register</param>
        /// <param name="number">The register number</param>
        public VirtualRegister(VirtualRegisterType type, int number)
        {
            this.Type = type;
            this.Number = number;
        }

        /// <summary>
        /// Checks if lhs == rhs
        /// </summary>
        /// <param name="lhs">The left hand side</param>
        /// <param name="rhs">The right hand side</param>
        public static bool operator ==(VirtualRegister lhs, VirtualRegister rhs)
        {
            return lhs.Type == rhs.Type && lhs.Number == rhs.Number;
        }

        /// <summary>
        /// Checks if lhs != rhs
        /// </summary>
        /// <param name="lhs">The left hand side</param>
        /// <param name="rhs">The right hand side</param>
        public static bool operator !=(VirtualRegister lhs, VirtualRegister rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Checks if the current object is equal to the given
        /// </summary>
        /// <param name="obj">The object</param>
        public override bool Equals(object obj)
        {
            if (!(obj is VirtualRegister))
            {
                return false;
            }

            var other = (VirtualRegister)obj;
            return this == other;
        }

        /// <summary>
        /// Computes the hash code
        /// </summary>
        public override int GetHashCode()
        {
            return this.Type.GetHashCode() + 31 * this.Number.GetHashCode();
        }
    }

    /// <summary>
    /// Represents an IR using virtual registers
    /// </summary>
    public static class VirtualRegisterIR
    {
        /// <summary>
        /// Creates the virtual registers IR for the given instructions
        /// </summary>
        /// <param name="virtualMachine">The virtual machine</param>
        /// <param name="function">The function</param>
        public static IList<VirtualInstruction> Create(VirtualMachine virtualMachine, Function function)
        {
            IList<int> localRegisters;
            return Create(virtualMachine, function, out localRegisters);
        }

        /// <summary>
        /// Creates the virtual registers IR for the given function
        /// </summary>
        /// <param name="virtualMachine">The virtual machine</param>
        /// <param name="function">The function</param>
        /// <param name="localRegisters">The local registers</param>
        public static IList<VirtualInstruction> Create(
            VirtualMachine virtualMachine,
            Function function,
            out IList<int> localRegisters)
        {
            var instructions = function.Instructions;
            var virtualInstructions = new List<VirtualInstruction>();
            var localRegs = new HashSet<int>();

            int virtualRegister = 0;
            int numStackRegisters = 0;
            Func<int> UseRegister = () => --virtualRegister;
            Func<int> AssignRegister = () =>
            {
                int reg = virtualRegister++;
                numStackRegisters = Math.Max(virtualRegister, numStackRegisters);
                return reg;
            };

            var localInstructions = new List<int>();

            var i = 0;
            foreach (var instruction in instructions)
            {
                var usesRegisters = new List<int>();
                int? assignRegister = null;

                switch (instruction.OpCode)
                {
                    case OpCodes.Pop:
                        usesRegisters.Add(UseRegister());
                        break;
                    case OpCodes.Ret:
                        if (!function.Definition.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
                        {
                            usesRegisters.Add(UseRegister());
                        }
                        break;
                    case OpCodes.AddInt:
                    case OpCodes.SubInt:
                    case OpCodes.MulInt:
                    case OpCodes.DivInt:
                    case OpCodes.AddFloat:
                    case OpCodes.SubFloat:
                    case OpCodes.MulFloat:
                    case OpCodes.DivFloat:
                        usesRegisters.Add(UseRegister());
                        usesRegisters.Add(UseRegister());
                        assignRegister = AssignRegister();
                        break;
                    case OpCodes.Call:
                        for (int arg = 0; arg < instruction.Parameters.Count; arg++)
                        {
                            usesRegisters.Add(UseRegister());
                        }

                        var toCall = virtualMachine.Binder.GetFunction(
                            virtualMachine.Binder.FunctionSignature(instruction.StringValue, instruction.Parameters));

                        if (!toCall.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
                        {
                            assignRegister = AssignRegister();
                        }
                        break;
                    case OpCodes.LoadArgument:
                        assignRegister = AssignRegister();
                        break;
                    case OpCodes.LoadInt:
                    case OpCodes.LoadFloat:
                        assignRegister = AssignRegister();
                        break;
                    case OpCodes.BranchEqual:
                    case OpCodes.BranchNotEqual:
                    case OpCodes.BranchGreaterThan:
                    case OpCodes.BranchGreaterOrEqual:
                    case OpCodes.BranchLessThan:
                    case OpCodes.BranchLessOrEqual:
                        usesRegisters.Add(UseRegister());
                        usesRegisters.Add(UseRegister());
                        break;
                    case OpCodes.LoadLocal:
                        assignRegister = AssignRegister();
                        localInstructions.Add(i);
                        break;
                    case OpCodes.StoreLocal:
                        usesRegisters.Add(UseRegister());
                        localInstructions.Add(i);
                        break;
                }

                virtualInstructions.Add(new VirtualInstruction(instruction, usesRegisters, assignRegister));
                i++;
            }

            //Assign the locals to virtual registers.
            foreach (var local in localInstructions)
            {
                var instruction = virtualInstructions[local];
                int localRegister = numStackRegisters + instruction.Instruction.IntValue;

                if (instruction.Instruction.OpCode == OpCodes.LoadLocal)
                {
                    instruction = new VirtualInstruction(
                        instruction.Instruction,
                        new List<int>() { localRegister },
                        instruction.AssignRegister);
                }
                else
                {
                    instruction = new VirtualInstruction(
                        instruction.Instruction,
                        instruction.UsesRegisters.ToList(),
                        localRegister);
                }

                virtualInstructions[local] = instruction;
                localRegs.Add(localRegister);
            }

            localRegisters = localRegs.ToList();

            return virtualInstructions;
        }
    }

    /// <summary>
    /// Represents a basic block for a virtual instruction
    /// </summary>
    public class VirtualBasicBlock : BasicBlock<VirtualInstruction>
    {
        /// <summary>
        /// Creates a new basic block
        /// </summary>
        /// <param name="startOffset">The start offset</param>
        /// <param name="instructions">The instructions</param>
        public VirtualBasicBlock(int startOffset, IList<VirtualInstruction> instructions)
            : base(startOffset, instructions)
        {

        }

        /// <summary>
        /// Creates the basic blocks for the given function
        /// </summary>
        /// <param name="virtualInstructions">The virtual instructions</param>
        public static IList<VirtualBasicBlock> CreateBasicBlocks(IReadOnlyList<VirtualInstruction> virtualInstructions)
        {
            return CreateBasicBlocks(
                virtualInstructions,
                x => x.Instruction,
                (offset, instructions) => new VirtualBasicBlock(offset, instructions));
        }
    }

    /// <summary>
    /// Represents an edge in a control graph for virtual instructions
    /// </summary>
    public class VirtualControlFlowEdge : ControlFlowEdge<VirtualInstruction, VirtualBasicBlock>
    {
        /// <summary>
        /// Creates a new edge
        /// </summary>
        /// <param name="from">The from vertex</param>
        /// <param name="to">The to vertex</param>
        public VirtualControlFlowEdge(VirtualBasicBlock from, VirtualBasicBlock to)
            : base(from, to)
        {

        }
    }

    /// <summary>
    /// Represents a control flow graph for virtual instructions
    /// </summary>
    public class VirtualControlFlowGraph : ControlFlowGraph<VirtualInstruction, VirtualBasicBlock, VirtualControlFlowEdge>
    {
        /// <summary>
        /// Creates a new control flow graph
        /// </summary>
        /// <param name="vertices">The vertices</param>
        /// <param name="neighborLists">The neighbor lists</param>
        private VirtualControlFlowGraph(IList<VirtualBasicBlock> vertices, IDictionary<VirtualBasicBlock, ISet<VirtualControlFlowEdge>> neighborLists)
            : base(vertices, neighborLists)
        {

        }

        /// <summary>
        /// Creates a control flow graph from the given basic blocks
        /// </summary>
        /// <param name="basicBlocks">The basic blocks</param>
        public static VirtualControlFlowGraph FromBasicBlocks(IList<VirtualBasicBlock> basicBlocks)
        {
            return FromBasicBlocks(
                basicBlocks,
                x => x.Instruction,
                (from, to) => new VirtualControlFlowEdge(from, to),
                (vertices, edges) => new VirtualControlFlowGraph(vertices, edges));
        }
    }
}
