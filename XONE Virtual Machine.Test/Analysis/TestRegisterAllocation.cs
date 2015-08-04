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
                var func = TestProgramGenerator.Simple(container);

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisters.Create(container.VirtualMachine, func);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);
                var registerAllocation = LinearScanRegisterAllocation.Allocate(livenessIntervals);

                Assert.AreEqual(2, registerAllocation.NumAllocatedRegisters);
                Assert.AreEqual(0, registerAllocation.NumSpilledRegisters);
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
                var func = TestProgramGenerator.Simple2(container);

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisters.Create(container.VirtualMachine, func);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);
                var registerAllocation = LinearScanRegisterAllocation.Allocate(livenessIntervals, 2);

                Assert.AreEqual(2, registerAllocation.NumAllocatedRegisters);
                Assert.AreEqual(1, registerAllocation.NumSpilledRegisters);
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
                var func = TestProgramGenerator.Simple3(container);

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisters.Create(container.VirtualMachine, func);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);
                var registerAllocation = LinearScanRegisterAllocation.Allocate(livenessIntervals);

                Assert.AreEqual(2, registerAllocation.NumAllocatedRegisters);
                Assert.AreEqual(0, registerAllocation.NumSpilledRegisters);
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

                var virtualInstructions = VirtualRegisters.Create(container.VirtualMachine, func);
                var virtualControlFlowGraph = VirtualControlFlowGraph.FromBasicBlocks(
                    VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualInstruction>(virtualInstructions)));

                var livenessIntervals = LivenessAnalysis.ComputeLiveness(virtualControlFlowGraph);
                var registerAllocation = LinearScanRegisterAllocation.Allocate(livenessIntervals, 2);

                Assert.AreEqual(3, registerAllocation.NumAllocatedRegisters);
                Assert.AreEqual(0, registerAllocation.NumSpilledRegisters);

                Assert.AreEqual(0, registerAllocation.GetRegister(0));
                Assert.AreEqual(1, registerAllocation.GetRegister(1));
                Assert.AreEqual(1, registerAllocation.GetRegister(1));
            }
        }
    }
}
