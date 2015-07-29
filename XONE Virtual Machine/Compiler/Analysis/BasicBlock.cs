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
    /// Represents a basic block
    /// </summary>
    public class BasicBlock : BasicBlock<Instruction>
    {
        /// <summary>
        /// Creates a new basic block
        /// </summary>
        /// <param name="startOffset">The start offset</param>
        /// <param name="instructions">The instructions</param>
        public BasicBlock(int startOffset, IList<Instruction> instructions)
            : base(startOffset, instructions)
        {

        }

        /// <summary>
        /// Creates the basic blocks for the given function
        /// </summary>
        /// <param name="function">The functions</param>
        public static IList<BasicBlock> CreateBasicBlocks(Function function)
        {
            return CreateBasicBlocks(
                function.Instructions,
                x => x,
                (offset, instructions) => new BasicBlock(offset, instructions));
        }
    }

    /// <summary>
    /// Returns the instruction for the given element
    /// </summary>
    /// <typeparam name="T">The type of the instruction</typeparam>
    /// <param name="element">The instruction</param>
    public delegate Instruction GetInstruction<T>(T element);

    /// <summary>
    /// Represents a basic block
    /// </summary>
    /// <typeparam name="T">The type of the instruction</typeparam>
    public class BasicBlock<T>
    {
        /// <summary>
        /// The offset from the functions instructions
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// The instructions in the block
        /// </summary>
        public IReadOnlyList<T> Instructions { get; }

        /// <summary>
        /// Creates a new basic block
        /// </summary>
        /// <param name="startOffset">The start offset</param>
        /// <param name="instructions">The instructions</param>
        protected BasicBlock(int startOffset, IList<T> instructions)
        {
            this.StartOffset = startOffset;
            this.Instructions = new ReadOnlyCollection<T>(instructions);
        }

        /// <summary>
        /// Returns the first instruction
        /// </summary>
        public T First
        {
            get { return this.Instructions[0]; }
        }

        /// <summary>
        /// Returns the last instruction
        /// </summary>
        public T Last
        {
            get { return this.Instructions[this.Instructions.Count - 1]; }
        }

        /// <summary>
        /// Finds the leaders in the given function
        /// </summary>
        /// <param name="instructions">The instructions</param>
        /// <param name="getInstruction">Returns the instruction for the given elemenet</param>
        private static IList<int> FindLeaders(IReadOnlyList<T> instructions, GetInstruction<T> getInstruction)
        {
            var leaders = new SortedSet<int>();

            bool prevIsBranch = false;
            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = getInstruction(instructions[i]);

                //The first instruction is a leader
                if (i == 0)
                {
                    leaders.Add(0);
                    continue;
                }

                //The target of a branch instruction is a leader
                if (instruction.OpCode == OpCodes.Branch || InstructionHelpers.IsConditionalBranch(instruction))
                {
                    leaders.Add(instruction.IntValue);
                    prevIsBranch = true;
                    continue;
                }

                //A return instruction can be seen as a branch
                if (instruction.OpCode == OpCodes.Ret)
                {
                    prevIsBranch = true;
                    continue;
                }

                //Instructions following a branch is a leader
                if (prevIsBranch)
                {
                    leaders.Add(i);
                    prevIsBranch = false;
                }
            }

            return leaders.ToList();
        }

        /// <summary>
        /// Creates the basic blocks for the given function
        /// </summary>
        /// <param name="instructions">The instructions</param>
        /// <param name="getInstruction">Function to return a instruction for the given T element</param>
        /// <param name="createBlock">Creates a new basic block</param>
        public static IList<TBlock> CreateBasicBlocks<TBlock>(IReadOnlyList<T> instructions,
            GetInstruction<T> getInstruction, Func<int, IList<T>, TBlock> createBlock)
            where TBlock : BasicBlock<T>
        {
            var blocks = new List<TBlock>();

            //Find the leaders
            var leaders = FindLeaders(instructions, getInstruction);

            //Now construct the blocks
            for (int leaderIndex = 0; leaderIndex < leaders.Count; leaderIndex++)
            {
                var blockInstructions = new List<T>();
                var current = leaders[leaderIndex];

                if (leaderIndex + 1 < leaders.Count)
                {
                    for (int i = current; i < leaders[leaderIndex + 1]; i++)
                    {
                        blockInstructions.Add(instructions[i]);
                    }
                }
                else
                {
                    for (int i = current; i < instructions.Count; i++)
                    {
                        blockInstructions.Add(instructions[i]);
                    }
                }

                blocks.Add(createBlock(current, blockInstructions));
            }

            return blocks;
        }

        public override string ToString()
        {
            return $"Start offset: {this.StartOffset}, Count: {this.Instructions.Count}, First: {{{this.First}}}, Last: {{{this.Last}}}";
        }
    }
}
