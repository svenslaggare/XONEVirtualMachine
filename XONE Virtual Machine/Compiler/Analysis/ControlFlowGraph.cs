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
    public class ControlFlowEdge : ControlFlowEdge<Instruction, BasicBlock>
    {
        /// <summary>
        /// Creates a new edge
        /// </summary>
        /// <param name="from">The from vertex</param>
        /// <param name="to">The to vertex</param>
        public ControlFlowEdge(BasicBlock from, BasicBlock to)
            : base(from, to)
        {

        }
    }

    /// <summary>
    /// Represents a control flow graph
    /// </summary>
    public class ControlFlowGraph : ControlFlowGraph<Instruction, BasicBlock, ControlFlowEdge>
    {
        /// <summary>
        /// Creates a new control flow graph
        /// </summary>
        /// <param name="vertices">The vertices</param>
        /// <param name="edges">The edges</param>
        private ControlFlowGraph(IList<BasicBlock> vertices, IDictionary<BasicBlock, ISet<ControlFlowEdge>> edges)
            : base(vertices, edges)
        {

        }

        /// <summary>
        /// Creates a control flow graph from the given basic blocks
        /// </summary>
        /// <param name="basicBlocks">The basic blocks</param>
        public static ControlFlowGraph FromBasicBlocks(IList<BasicBlock> basicBlocks)
        {
            return FromBasicBlocks(
                basicBlocks,
                x => x,
                (from, to) => new ControlFlowEdge(from, to),
                (vertices, edges) => new ControlFlowGraph(vertices, edges));
        }
    }

    /// <summary>
    /// Represents an edge in a control graph
    /// </summary>
    /// <typeparam name="TInst">The type of the instruction</typeparam>
    /// <typeparam name="TBlock">The type of the blocks</typeparam>
    public class ControlFlowEdge<TInst, TBlock> where TBlock : BasicBlock<TInst>
    {
        /// <summary>
        /// The from vertex
        /// </summary>
        public TBlock From { get; }

        /// <summary>
        /// The to vertex
        /// </summary>
        public TBlock To { get; }

        /// <summary>
        /// Creates a new edge
        /// </summary>
        /// <param name="from">The from vertex</param>
        /// <param name="to">The to vertex</param>
        public ControlFlowEdge(TBlock from, TBlock to)
        {
            this.From = from;
            this.To = to;
        }

        public override bool Equals(object obj)
        {
            var edgeObj = obj as ControlFlowEdge<TInst, TBlock>;

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
    /// <typeparam name="TInst">The type of the instruction</typeparam>
    /// <typeparam name="TBlock">The type of the block</typeparam>
    /// <typeparam name="TEdge">The type of the edge</typeparam>
    public class ControlFlowGraph<TInst, TBlock, TEdge>
        where TBlock : BasicBlock<TInst>
        where TEdge : ControlFlowEdge<TInst, TBlock>
    {
        /// <summary>
        /// The vertices
        /// </summary>
        public IReadOnlyList<TBlock> Vertices { get; }

        /// <summary>
        /// The neighbor lists
        /// </summary>
        public IReadOnlyDictionary<TBlock, ISet<TEdge>> NeighborLists { get; }

        /// <summary>
        /// Creates a new control flow graph
        /// </summary>
        /// <param name="vertices">The vertices</param>
        /// <param name="neighborLists">The neighbor lists</param>
        protected ControlFlowGraph(IList<TBlock> vertices, IDictionary<TBlock, ISet<TEdge>> neighborLists)
        {
            this.Vertices = new ReadOnlyCollection<TBlock>(vertices);
            this.NeighborLists = new ReadOnlyDictionary<TBlock, ISet<TEdge>>(neighborLists);
        }

        /// <summary>
        /// Creates a control flow graph from the given basic blocks
        /// </summary>
        /// <typeparam name="TGraph">The type of the graph</typeparam>
        /// <param name="basicBlocks">The basic blocks</param>
        /// <param name="getInstruction">Function to return a instruction for the given T element</param>
        /// <param name="createEdge">Function to create an edge</param>
        /// <param name="createGraph">Function to create a graph</param>
        public static TGraph FromBasicBlocks<TGraph>(
            IList<TBlock> basicBlocks,
            GetInstruction<TInst> getInstruction,
            Func<TBlock, TBlock, TEdge> createEdge,
            Func<IList<TBlock>, IDictionary<TBlock, ISet<TEdge>>, TGraph> createGraph)
            where TGraph : ControlFlowGraph<TInst, TBlock, TEdge>
        {
            var edges = new Dictionary<TBlock, ISet<TEdge>>();

            //Create a mapping from start offset -> basic block
            var offsetMapping = new Dictionary<int, TBlock>();
            foreach (var block in basicBlocks)
            {
                offsetMapping.Add(block.StartOffset, block);
            }

            Action<TBlock, TBlock> AddEdge = (from, to) =>
            {
                ISet<TEdge> fromEdges;
                if (!edges.TryGetValue(from, out fromEdges))
                {
                    fromEdges = new HashSet<TEdge>();
                    edges.Add(from, fromEdges);
                }

                fromEdges.Add(createEdge(from, to));
            };

            foreach (var block in basicBlocks)
            {
                var lastInst = getInstruction(block.Last);

                if (lastInst.OpCode != OpCodes.Ret)
                {
                    var targetBlock = offsetMapping[lastInst.IntValue];

                    if (InstructionHelpers.IsConditionalBranch(lastInst))
                    {
                        AddEdge(block, targetBlock);
                        AddEdge(block, offsetMapping[block.StartOffset + block.Instructions.Count]);
                    }
                    else if (lastInst.OpCode == OpCodes.Branch)
                    {
                        AddEdge(block, targetBlock);
                    }
                    else
                    {
                        AddEdge(block, offsetMapping[block.StartOffset + block.Instructions.Count]);
                    }
                }
            }

            return createGraph(basicBlocks, edges);
        }
    }
}
