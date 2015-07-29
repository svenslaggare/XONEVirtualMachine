﻿using System;
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
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var instructions = new List<Instruction>();
                instructions.Add(new Instruction(OpCodes.LoadInt, 4));
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.BranchEqual, 6));

                instructions.Add(new Instruction(OpCodes.LoadInt, 5));
                instructions.Add(new Instruction(OpCodes.StoreLocal, 0));
                instructions.Add(new Instruction(OpCodes.Branch, 8));

                instructions.Add(new Instruction(OpCodes.LoadInt, 15));
                instructions.Add(new Instruction(OpCodes.StoreLocal, 0));

                instructions.Add(new Instruction(OpCodes.LoadLocal, 0));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(
                    new FunctionDefinition("test", new List<VMType>() { }, intType),
                    instructions,
                    new List<VMType>() { intType });

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var basicBlocks = BasicBlock.CreateBasicBlocks(func);
                var controlGraph = ControlFlowGraph.FromBasicBlocks(basicBlocks);

                Assert.AreEqual(2, controlGraph.Edges.Count);

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
            }
        }
    }
}
