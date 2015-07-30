using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Compiler.Analysis
{
    /// <summary>
    /// Represents a live interval
    /// </summary>
    public class LiveInterval
    {
        /// <summary>
        /// The start of the interval
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// The end of the interval
        /// </summary>
        public int End { get; }

        /// <summary>
        /// The virtual register number
        /// </summary>
        public int VirtualRegister { get; }

        /// <summary>
        /// Creates a new interval
        /// </summary>
        /// <param name="start">The start of the interval</param>
        /// <param name="end">he end of the interval</param>
        /// <param name="virtualRegister">The virtual register number</param>
        public LiveInterval(int start, int end, int virtualRegister)
        {
            this.Start = start;
            this.End = end;
            this.VirtualRegister = virtualRegister;
        }

        public override string ToString()
        {
            return $"Reg: {this.VirtualRegister}, Start: {this.Start}, End: {this.End}";
        }
    }

    /// <summary>
    /// Performs liveness analysis
    /// </summary>
    public static class LivenessAnalysis
    {
        /// <summary>
        /// Computes the number of virtual registers in the given control flow graph
        /// </summary>
        /// <param name="controlFlowGraph">The control flow graph</param>
        private static int NumVirtualRegister(VirtualControlFlowGraph controlFlowGraph)
        {
            var virtualRegisters = new HashSet<int>();
            foreach (var block in controlFlowGraph.Vertices)
            {
                foreach (var instruction in block.Instructions)
                {
                    if (instruction.AssignRegister.HasValue)
                    {
                        virtualRegisters.Add(instruction.AssignRegister.Value);
                    }

                    foreach (var register in instruction.UsesRegisters)
                    {
                        virtualRegisters.Add(register);
                    }
                }
            }

            return virtualRegisters.Count;
        }

        /// <summary>
        /// Represents a usage site
        /// </summary>
        private class UsageSite
        {
            /// <summary>
            /// The block
            /// </summary>
            public VirtualBasicBlock Block { get; set; }

            /// <summary>
            /// The offset within the block
            /// </summary>
            public int Offset { get; set; }
        }

        /// <summary>
        /// Returns the virtual register usage from the registers in the given control flow graph
        /// </summary>
        /// <param name="controlFlowGraph">The control flow graph</param>
        /// <param name="useSites">The use sites</param>
        /// <param name="assignSites">The assign sites</param>
        private static void GetRegisterUsage(VirtualControlFlowGraph controlFlowGraph,
            out IDictionary<int, IList<UsageSite>> useSites, out IDictionary<int, IList<UsageSite>> assignSites)
        {
            useSites = new Dictionary<int, IList<UsageSite>>();
            assignSites = new Dictionary<int, IList<UsageSite>>();

            foreach (var block in controlFlowGraph.Vertices)
            {
                int offset = 0;
                foreach (var instruction in block.Instructions)
                {
                    if (instruction.AssignRegister != null)
                    {
                        int register = instruction.AssignRegister.Value;
                        IList<UsageSite> assigns;
                        if (!useSites.TryGetValue(register, out assigns))
                        {
                            assigns = new List<UsageSite>();
                            useSites.Add(register, assigns);
                        }

                        assigns.Add(new UsageSite()
                        {
                            Block = block,
                            Offset = offset
                        });
                    }

                    foreach (var register in instruction.UsesRegisters)
                    {
                        IList<UsageSite> uses;
                        if (!useSites.TryGetValue(register, out uses))
                        {
                            uses = new List<UsageSite>();
                            useSites.Add(register, uses);
                        }

                        uses.Add(new UsageSite()
                        {
                            Block = block,
                            Offset = offset
                        });
                    }

                    offset++;
                }
            }
        }

        /// <summary>
        /// Represents a backflow graph
        /// </summary>
        private class BackflowGraph
        {
            public IDictionary<VirtualBasicBlock, ISet<VirtualBasicBlock>> Edges { get; set; }
        }

        /// <summary>
        /// Returns the backflow for the given control flow graph
        /// </summary>
        /// <param name="controlFlowGraph">The control flow graph</param>
        private static BackflowGraph GetBackflow(VirtualControlFlowGraph controlFlowGraph)
        {
            var backflowEdges = new Dictionary<VirtualBasicBlock, ISet<VirtualBasicBlock>>();

            foreach (var edgeList in controlFlowGraph.Edges.Values)
            {
                foreach (var edge in edgeList)
                {
                    ISet<VirtualBasicBlock> edges;
                    if (!backflowEdges.TryGetValue(edge.To, out edges))
                    {
                        edges = new HashSet<VirtualBasicBlock>();
                        backflowEdges.Add(edge.To, edges);
                    }

                    edges.Add(edge.From);
                }
            }

            return new BackflowGraph()
            {
                Edges = backflowEdges
            };
        }

        /// <summary>
        /// Computes the liveness for the given register
        /// </summary>
        /// <param name="backflowGraph">The backflow graph</param>
        /// <param name="basicBlock">The current block</param>
        /// <param name="startOffset">The offset in the current block</param>
        /// <param name="visited">The visited blocks</param>
        /// <param name="register">The current register</param>
        /// <param name="aliveAt">The instructions which the register is alive</param>
        private static void ComputeLiveness(
            BackflowGraph backflowGraph,
            VirtualBasicBlock basicBlock, int startOffset,
            ISet<VirtualBasicBlock> visited,
            int register, ISet<int> aliveAt)
        {
            if (visited.Contains(basicBlock))
            {
                return;
            }

            visited.Add(basicBlock);

            bool terminated = false;

            for (int i = startOffset; i >= 0; i--)
            {
                var instruction = basicBlock.Instructions[i];

                if (instruction.AssignRegister == register)
                {
                    if (!instruction.UsesRegisters.Contains(register))
                    {
                        aliveAt.Add(i + basicBlock.StartOffset);
                        terminated = true;
                        break;
                    }
                }

                aliveAt.Add(i + basicBlock.StartOffset);
            }

            //If we have not terminated the search, search edges flowing backwards from the current block
            if (!terminated)
            {
                ISet<VirtualBasicBlock> edges;
                if (backflowGraph.Edges.TryGetValue(basicBlock, out edges))
                {
                    foreach (var backEdge in edges)
                    {
                        ComputeLiveness(
                            backflowGraph,
                            backEdge,
                            backEdge.Instructions.Count - 1,
                            visited,
                            register,
                            aliveAt);
                    }
                }
            }
        }

        /// <summary>
        /// Computes the liveness for the given register
        /// </summary>
        /// <param name="backflowGraph">The backflow graph</param>
        /// <param name="register">The register</param>
        /// <param name="useSites">The use sites</param>
        /// <param name="aliveAt">The instructions which the register is alive</param>
        private static void ComputeLiveness(BackflowGraph backflowGraph, int register, IList<UsageSite> useSites, ISet<int> aliveAt)
        {
            foreach (var useSite in useSites)
            {
                ComputeLiveness(
                    backflowGraph,
                    useSite.Block,
                    useSite.Offset,
                    new HashSet<VirtualBasicBlock>(),
                    register,
                    aliveAt);
            }
        }

        /// <summary>
        /// Returns the live interval for the given register
        /// </summary>
        /// <param name="register">The register</param>
        /// <param name="aliveAt">The instructions which the register is alive</param>
        private static LiveInterval GetLiveInterval(int register, ISet<int> aliveAt)
        {
            int start = int.MaxValue;
            int end = int.MinValue;

            foreach (var instruction in aliveAt)
            {
                start = Math.Min(start, instruction);
                end = Math.Max(end, instruction);
            }

            return new LiveInterval(start, end, register);
        }

        /// <summary>
        /// Compues the liveness for the given control flow graph
        /// </summary>
        /// <param name="controlFlowGraph">The control flow graph</param>
        public static IList<LiveInterval> ComputeLiveness(VirtualControlFlowGraph controlFlowGraph)
        {
            var liveIntervals = new List<LiveInterval>();
            int numRegisters = NumVirtualRegister(controlFlowGraph);

            //Get the register usage
            IDictionary<int, IList<UsageSite>> useSites;
            IDictionary<int, IList<UsageSite>> assignSites;
            GetRegisterUsage(controlFlowGraph, out useSites, out assignSites);

            //Get the backflow fraph
            var backflowGraph = GetBackflow(controlFlowGraph);

            for (int reg = 0; reg < numRegisters; reg++)
            {
                IList<UsageSite> registerUseSites;

                if (useSites.TryGetValue(reg, out registerUseSites))
                {
                    var aliveAt = new HashSet<int>();
                    ComputeLiveness(backflowGraph, reg, registerUseSites, aliveAt);
                    liveIntervals.Add(GetLiveInterval(reg, aliveAt));
                }
                else
                {
                    //This mean that the register is not used. Atm, we do not remove write-only virtual registers.
                    //So we need to compute the liveness information, else we get an exception in code gen.
                    var aliveAt = new HashSet<int>();
                    foreach (var assignSite in assignSites[reg])
                    {
                        aliveAt.ElementAt(assignSite.Block.StartOffset + assignSite.Offset);
                    }

                    liveIntervals.Add(GetLiveInterval(reg, aliveAt));
                }
            }

            return liveIntervals;
        }
    }
}
