﻿using System;
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
    public class VirtualRegisterInstruction
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
        public VirtualRegisterInstruction(Instruction instruction, IList<int> usesRegisters, int? assignRegister = null)
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
    /// Represents an IR using virtual registers
    /// </summary>
    public static class VirtualRegisters
    {
        /// <summary>
        /// Creates the virtual registers IR for the given instructions
        /// </summary>
        /// <param name="instructions">The instructions</param>
        public static IList<VirtualRegisterInstruction> Create(IReadOnlyList<Instruction> instructions)
        {
            var virtualInstructions = new List<VirtualRegisterInstruction>();

            int virtualRegister = 0;

            Func<int> UseRegister = () => --virtualRegister;
            Func<int> AssignRegister = () => virtualRegister++;

            foreach (var instruction in instructions)
            {
                var usesRegisters = new List<int>();
                int? assignRegister = null;

                switch (instruction.OpCode)
                {
                    case OpCodes.Pop:
                    case OpCodes.Ret:
                    case OpCodes.StoreLocal:
                        usesRegisters.Add(UseRegister());
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
                        for (int i = 0; i < instruction.Parameters.Count; i++)
                        {
                            usesRegisters.Add(UseRegister());
                        }
                        assignRegister = AssignRegister();
                        break;
                    case OpCodes.LoadArgument:
                    case OpCodes.LoadLocal:
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
                }

                virtualInstructions.Add(new VirtualRegisterInstruction(instruction, usesRegisters, assignRegister));
            }

            return virtualInstructions;
        }
    }

    /// <summary>
    /// Represents a basic block for a virtual instruction
    /// </summary>
    public class VirtualBasicBlock : BasicBlock<VirtualRegisterInstruction>
    {
        /// <summary>
        /// Creates a new basic block
        /// </summary>
        /// <param name="startOffset">The start offset</param>
        /// <param name="instructions">The instructions</param>
        public VirtualBasicBlock(int startOffset, IList<VirtualRegisterInstruction> instructions)
            : base(startOffset, instructions)
        {

        }

        /// <summary>
        /// Creates the basic blocks for the given function
        /// </summary>
        /// <param name="virtualInstructions">The virtual instructions</param>
        public static IList<VirtualBasicBlock> CreateBasicBlocks(IReadOnlyList<VirtualRegisterInstruction> virtualInstructions)
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
    public class VirtualControlFlowEdge : ControlFlowEdge<VirtualRegisterInstruction, VirtualBasicBlock>
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
    public class VirtualControlFlowGraph : ControlFlowGraph<VirtualRegisterInstruction, VirtualBasicBlock, VirtualControlFlowEdge>
    {
        /// <summary>
        /// Creates a new control flow graph
        /// </summary>
        /// <param name="vertices">The vertices</param>
        /// <param name="edges">The edges</param>
        private VirtualControlFlowGraph(IList<VirtualBasicBlock> vertices, IDictionary<VirtualBasicBlock, ISet<VirtualControlFlowEdge>> edges)
            : base(vertices, edges)
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
