using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Compiler.Analysis;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Analysis
{
    /// <summary>
    /// Tests creating control flow graphs
    /// </summary>
    [TestClass]
    public class TestControlFlowGraphs
    {
        /// <summary>
        /// Tests a function with branches
        /// </summary>
        [TestMethod]
        public void TestBranchFunction()
        {
            using (var container = new Win64Container())
            {
                var func = TestProgramGenerator.Branch(container);

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var basicBlocks = BasicBlock.CreateBasicBlocks(func);
                var controlGraph = ControlFlowGraph.FromBasicBlocks(basicBlocks);

                Assert.AreEqual(3, controlGraph.Edges.Count);

                Assert.AreEqual(2, controlGraph.Edges[basicBlocks[0]].Count);
                Assert.AreEqual(true, controlGraph.Edges[basicBlocks[0]].SetEquals(new HashSet<ControlFlowEdge>()
                {
                    new ControlFlowEdge(basicBlocks[0], basicBlocks[1]),
                    new ControlFlowEdge(basicBlocks[0], basicBlocks[2]),
                }));

                Assert.AreEqual(1, controlGraph.Edges[basicBlocks[1]].Count);
                Assert.AreEqual(true, controlGraph.Edges[basicBlocks[1]].SetEquals(new HashSet<ControlFlowEdge>()
                {
                    new ControlFlowEdge(basicBlocks[1], basicBlocks[3])
                }));

                Assert.AreEqual(1, controlGraph.Edges[basicBlocks[2]].Count);
                Assert.AreEqual(true, controlGraph.Edges[basicBlocks[2]].SetEquals(new HashSet<ControlFlowEdge>()
                {
                    new ControlFlowEdge(basicBlocks[2], basicBlocks[3])
                }));
            }
        }

        /// <summary>
        /// Tests the max function
        /// </summary>
        [TestMethod]
        public void TestMax()
        {
            using (var container = new Win64Container())
            {
                var func = TestProgramGenerator.Max(container);

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var basicBlocks = BasicBlock.CreateBasicBlocks(func);
                var controlGraph = ControlFlowGraph.FromBasicBlocks(basicBlocks);

                Assert.AreEqual(3, controlGraph.Edges.Count);

                Assert.AreEqual(2, controlGraph.Edges[basicBlocks[0]].Count);
                Assert.AreEqual(true, controlGraph.Edges[basicBlocks[0]].SetEquals(new HashSet<ControlFlowEdge>()
                {
                    new ControlFlowEdge(basicBlocks[0], basicBlocks[1]),
                    new ControlFlowEdge(basicBlocks[0], basicBlocks[2]),
                }));

                Assert.AreEqual(1, controlGraph.Edges[basicBlocks[1]].Count);
                Assert.AreEqual(true, controlGraph.Edges[basicBlocks[1]].SetEquals(new HashSet<ControlFlowEdge>()
                    {
                        new ControlFlowEdge(basicBlocks[1], basicBlocks[3])
                    }));

                Assert.AreEqual(1, controlGraph.Edges[basicBlocks[2]].Count);
                Assert.AreEqual(true, controlGraph.Edges[basicBlocks[2]].SetEquals(new HashSet<ControlFlowEdge>()
                    {
                        new ControlFlowEdge(basicBlocks[2], basicBlocks[3])
                    }));
            }
        }
    }
}
