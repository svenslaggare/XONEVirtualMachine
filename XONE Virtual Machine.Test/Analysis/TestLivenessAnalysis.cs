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
                var func = TestProgramGenerator.Simple(container);

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisterIR.Create(container.VirtualMachine, func);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);

                Assert.AreEqual(0, livenessIntervals[0].VirtualRegister.Number);
                Assert.AreEqual(0, livenessIntervals[0].Start);
                Assert.AreEqual(3, livenessIntervals[0].End);

                Assert.AreEqual(1, livenessIntervals[1].VirtualRegister.Number);
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
                var func = TestProgramGenerator.Branch(container);

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisterIR.Create(container.VirtualMachine, func);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);

                Assert.AreEqual(3, livenessIntervals.Count);

                Assert.AreEqual(0, livenessIntervals[0].VirtualRegister.Number);
                Assert.AreEqual(0, livenessIntervals[0].Start);
                Assert.AreEqual(9, livenessIntervals[0].End);

                Assert.AreEqual(1, livenessIntervals[1].VirtualRegister.Number);
                Assert.AreEqual(1, livenessIntervals[1].Start);
                Assert.AreEqual(2, livenessIntervals[1].End);

                Assert.AreEqual(2, livenessIntervals[2].VirtualRegister.Number);
                Assert.AreEqual(4, livenessIntervals[2].Start);
                Assert.AreEqual(8, livenessIntervals[2].End);
            }
        }

        /// <summary>
        /// Tests a locals function
        /// </summary>
        [TestMethod]
        public void TestLocals()
        {
            using (var container = new Win64Container())
            {
                var func = TestProgramGenerator.Locals(container);

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisterIR.Create(container.VirtualMachine, func);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);

                Assert.AreEqual(3, livenessIntervals.Count);

                Assert.AreEqual(0, livenessIntervals[0].VirtualRegister.Number);
                Assert.AreEqual(0, livenessIntervals[0].Start);
                Assert.AreEqual(5, livenessIntervals[0].End);

                Assert.AreEqual(1, livenessIntervals[1].VirtualRegister.Number);
                Assert.AreEqual(1, livenessIntervals[1].Start);
                Assert.AreEqual(1, livenessIntervals[1].End);

                Assert.AreEqual(2, livenessIntervals[2].VirtualRegister.Number);
                Assert.AreEqual(3, livenessIntervals[2].Start);
                Assert.AreEqual(4, livenessIntervals[2].End);
            }
        }
    }
}
