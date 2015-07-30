using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Compiler.Analysis;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Analysis
{
    /// <summary>
    /// Tests the register allocation
    /// </summary>
    [TestClass]
    public class TestRegisterAllocation
    {
        /// <summary>
        /// Tests a simple function
        /// </summary>
        [TestMethod]
        public void TestSimple()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var instructions = new List<Instruction>();
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.LoadInt, 4));
                instructions.Add(new Instruction(OpCodes.AddInt));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(
                    new FunctionDefinition("test", new List<VMType>() { }, intType),
                    instructions,
                    new List<VMType>());

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisters.Create(func.Instructions);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);
                var registerAllocation = LinearScanRegisterAllocation.Allocate(livenessIntervals);

                Assert.AreEqual(2, registerAllocation.Allocated.Count);
                Assert.AreEqual(0, registerAllocation.Spilled.Count);
            }
        }

        /// <summary>
        /// Tests a simple function
        /// </summary>
        [TestMethod]
        public void TestSimple2()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var instructions = new List<Instruction>();
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.LoadInt, 4));
                instructions.Add(new Instruction(OpCodes.LoadInt, 6));
                instructions.Add(new Instruction(OpCodes.AddInt));
                instructions.Add(new Instruction(OpCodes.AddInt));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(
                    new FunctionDefinition("test", new List<VMType>() { }, intType),
                    instructions,
                    new List<VMType>());

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisters.Create(func.Instructions);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);
                var registerAllocation = LinearScanRegisterAllocation.Allocate(livenessIntervals, 2);

                Assert.AreEqual(2, registerAllocation.Allocated.Count);
                Assert.AreEqual(1, registerAllocation.Spilled.Count);
            }
        }

        /// <summary>
        /// Tests a simple function
        /// </summary>
        [TestMethod]
        public void TestSimple3()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var instructions = new List<Instruction>();
                instructions.Add(new Instruction(OpCodes.LoadInt, 1));
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.AddInt));
                instructions.Add(new Instruction(OpCodes.LoadInt, 3));
                instructions.Add(new Instruction(OpCodes.AddInt));
                instructions.Add(new Instruction(OpCodes.LoadInt, 4));
                instructions.Add(new Instruction(OpCodes.AddInt));
                instructions.Add(new Instruction(OpCodes.LoadInt, 5));
                instructions.Add(new Instruction(OpCodes.AddInt));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(
                    new FunctionDefinition("test", new List<VMType>() { }, intType),
                    instructions,
                    new List<VMType>());

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisters.Create(func.Instructions);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);
                var registerAllocation = LinearScanRegisterAllocation.Allocate(livenessIntervals);

                Assert.AreEqual(2, registerAllocation.Allocated.Count);
                Assert.AreEqual(0, registerAllocation.Spilled.Count);
            }
        }
    }
}
