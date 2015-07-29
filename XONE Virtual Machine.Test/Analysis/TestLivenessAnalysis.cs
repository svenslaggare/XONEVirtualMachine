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
    /// Tests liveness analysis
    /// </summary>
    [TestClass]
    public class TestLivenessAnalysis
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
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualRegisterInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);

                Assert.AreEqual(0, livenessIntervals[0].VirtualRegister);
                Assert.AreEqual(0, livenessIntervals[0].Start);
                Assert.AreEqual(3, livenessIntervals[0].End);

                Assert.AreEqual(1, livenessIntervals[1].VirtualRegister);
                Assert.AreEqual(1, livenessIntervals[1].Start);
                Assert.AreEqual(2, livenessIntervals[1].End);
            }
        }

        /// <summary>
        /// A function with branches
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

                var virtualInstructions = VirtualRegisters.Create(func.Instructions);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualRegisterInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);

                Assert.AreEqual(0, livenessIntervals[0].VirtualRegister);
                Assert.AreEqual(0, livenessIntervals[0].Start);
                Assert.AreEqual(9, livenessIntervals[0].End);

                Assert.AreEqual(1, livenessIntervals[1].VirtualRegister);
                Assert.AreEqual(1, livenessIntervals[1].Start);
                Assert.AreEqual(2, livenessIntervals[1].End);
            }
        }
    }
}
