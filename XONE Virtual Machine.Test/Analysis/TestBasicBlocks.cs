using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Compiler.Analysis;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Analysis
{
    /// <summary>
    /// Tests creating basic blocks
    /// </summary>
    [TestClass]
    public class TestBasicBlocks
    {
        /// <summary>
        /// A function without branches
        /// </summary>
        [TestMethod]
        public void TestSimpleFunction()
        {
            using (var container = new Win64Container())
            {
                var func = TestProgramGenerator.Simple(container);
                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var basicBlocks = BasicBlock.CreateBasicBlocks(func);
                Assert.AreEqual(1, basicBlocks.Count);
                Assert.AreEqual(4, basicBlocks[0].Instructions.Count);
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
                var instructions = func.Instructions;

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var basicBlocks = BasicBlock.CreateBasicBlocks(func);

                //Check that all the instrutions are in exactly one block
                Assert.AreEqual(instructions.Count, basicBlocks.Aggregate(0, (total, current) => total + current.Instructions.Count));
                Assert.AreEqual(4, basicBlocks.Count);

                Assert.AreEqual(0, basicBlocks[0].StartOffset);
                Assert.AreEqual(3, basicBlocks[0].Instructions.Count);

                Assert.AreEqual(3, basicBlocks[1].StartOffset);
                Assert.AreEqual(3, basicBlocks[1].Instructions.Count);

                Assert.AreEqual(6, basicBlocks[2].StartOffset);
                Assert.AreEqual(2, basicBlocks[2].Instructions.Count);

                Assert.AreEqual(8, basicBlocks[3].StartOffset);
                Assert.AreEqual(2, basicBlocks[3].Instructions.Count);
            }
        }

        /// <summary>
        /// A function with Multiple returns
        /// </summary>
        [TestMethod]
        public void TestMultipleReturns()
        {
            using (var container = new Win64Container())
            {
                var func = TestProgramGenerator.MultipleReturns(container);
                var instructions = func.Instructions;

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var basicBlocks = BasicBlock.CreateBasicBlocks(func);

                //Check that all the instrutions are in exactly one block
                Assert.AreEqual(instructions.Count, basicBlocks.Aggregate(0, (total, current) => total + current.Instructions.Count));
                Assert.AreEqual(2, basicBlocks.Count);

                Assert.AreEqual(0, basicBlocks[0].StartOffset);
                Assert.AreEqual(2, basicBlocks[0].Instructions.Count);

                Assert.AreEqual(2, basicBlocks[1].StartOffset);
                Assert.AreEqual(2, basicBlocks[1].Instructions.Count);
            }
        }
    }
}
