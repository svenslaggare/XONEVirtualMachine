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
    public class BasicBlock
    {
        /// <summary>
        /// The offset from the functions instructions
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// The instructions in the block
        /// </summary>
        public IReadOnlyList<Instruction> Instructions { get; }

        /// <summary>
        /// Creates a new basic block
        /// </summary>
        /// <param name="startOffset">The start offset</param>
        /// <param name="instructions">The instructions</param>
        private BasicBlock(int startOffset, IList<Instruction> instructions)
        {
            this.StartOffset = startOffset;
            this.Instructions = new ReadOnlyCollection<Instruction>(instructions);
        }

        /// <summary>
        /// Returns the first instruction
        /// </summary>
        public Instruction First
        {
            get { return this.Instructions[0]; }
        }

        /// <summary>
        /// Returns the last instruction
        /// </summary>
        public Instruction Last
        {
            get { return this.Instructions[this.Instructions.Count - 1]; }
        }

        /// <summary>
        /// Finds the leaders in the given function
        /// </summary>
        /// <param name="function">The functions</param>
        private static IList<int> FindLeaders(Function function)
        {
            var leaders = new SortedSet<int>();

            bool prevIsBranch = false;
            for (int i = 0; i < function.Instructions.Count; i++)
            {
                var instruction = function.Instructions[i];

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
        /// <param name="function">The functions</param>
        public static IList<BasicBlock> CreateBasicBlocks(Function function)
        {
            var instructions = function.Instructions;
            var blocks = new List<BasicBlock>();

            //Find the leaders
            var leaders = FindLeaders(function);

            //Now construct the blocks
            for (int leaderIndex = 0; leaderIndex < leaders.Count; leaderIndex++)
            {
                var blockInstructions = new List<Instruction>();
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

                blocks.Add(new BasicBlock(current, blockInstructions));
            }

            return blocks;
        }

        public override string ToString()
        {
            return $"Start offset: {this.StartOffset}, Count: {this.Instructions.Count}, First: {{{this.First}}}, Last: {{{this.Last}}}";
        }
    }
}
