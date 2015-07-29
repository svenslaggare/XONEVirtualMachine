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
    /// Represents an edge in a control graph
    /// </summary>
    public class ControlFlowEdge
    {
        /// <summary>
        /// The from vertex
        /// </summary>
        public BasicBlock From { get; }

        /// <summary>
        /// The to vertex
        /// </summary>
        public BasicBlock To { get; }

        /// <summary>
        /// Creates a new edge
        /// </summary>
        /// <param name="from">The from vertex</param>
        /// <param name="to">The to vertex</param>
        public ControlFlowEdge(BasicBlock from, BasicBlock to)
        {
            this.From = from;
            this.To = to;
        }

        public override bool Equals(object obj)
        {
            var edgeObj = obj as ControlFlowEdge;

            if (edgeObj == null)
            {
                return false;
            }

            return this.From == edgeObj.From && this.To == edgeObj.To;
        }

        public override int GetHashCode()
        {
            return this.From.GetHashCode() + (31 * this.To.GetHashCode());
        }

        public override string ToString()
        {
            return $"To: {{{this.To}}}";
        }
    }

    /// <summary>
    /// Represents a control flow graph
    /// </summary>
    public class ControlFlowGraph
    {
        /// <summary>
        /// The edges
        /// </summary>
        public IReadOnlyDictionary<BasicBlock, ISet<ControlFlowEdge>> Edges { get; }

        /// <summary>
        /// Creates a new control flow graph
        /// </summary>
        /// <param name="edges">The edges</param>
        private ControlFlowGraph(IDictionary<BasicBlock, ISet<ControlFlowEdge>> edges)
        {
            this.Edges = new ReadOnlyDictionary<BasicBlock, ISet<ControlFlowEdge>>(edges);
        }

        /// <summary>
        /// Creates a control flow graph from the given basic blocks
        /// </summary>
        /// <param name="basicBlocks">The basic blocks</param>
        public static ControlFlowGraph FromBasicBlocks(IList<BasicBlock> basicBlocks)
        {
            var edges = new Dictionary<BasicBlock, ISet<ControlFlowEdge>>();

            //Create a mapping from start offset -> basic block
            var offsetMapping = new Dictionary<int, BasicBlock>();
            foreach (var block in basicBlocks)
            {
                offsetMapping.Add(block.StartOffset, block);
            }

            Action<BasicBlock, BasicBlock> AddEdge = (from, to) =>
            {
                ISet<ControlFlowEdge> fromEdges;
                if (!edges.TryGetValue(from, out fromEdges))
                {
                    fromEdges = new HashSet<ControlFlowEdge>();
                    edges.Add(from, fromEdges);
                }

                fromEdges.Add(new ControlFlowEdge(from, to));
            };

            foreach (var block in basicBlocks)
            {
                if (block.Last.OpCode != OpCodes.Ret)
                {
                    var targetBlock = offsetMapping[block.Last.IntValue];

                    if (InstructionHelpers.IsConditionalBranch(block.Last))
                    {
                        AddEdge(block, targetBlock);
                        AddEdge(block, offsetMapping[block.StartOffset + block.Instructions.Count]);
                    }
                    else if (block.Last.OpCode == OpCodes.Branch)
                    {
                        AddEdge(block, targetBlock);
                    }
                }
            }

            return new ControlFlowGraph(edges);
        }
    }
}
