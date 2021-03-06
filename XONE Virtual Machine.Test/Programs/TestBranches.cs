﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Programs
{
    /// <summary>
    /// Test branch instructions
    /// </summary>
    [TestClass]
    public class TestBranches
    {
        /// <summary>
        /// Creates a new branch program
        /// </summary>
        private Function CreateBranchProgram(Win64Container container, OpCodes branchInstruction, int value1, int value2)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadInt, value1));
            instructions.Add(new Instruction(OpCodes.LoadInt, value2));
            instructions.Add(new Instruction(branchInstruction, 6));
            instructions.Add(new Instruction(OpCodes.LoadInt, 0));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));
            instructions.Add(new Instruction(OpCodes.Branch, 8));
            instructions.Add(new Instruction(OpCodes.LoadInt, 1));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));
            instructions.Add(new Instruction(OpCodes.LoadLocal, 0));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(
                new FunctionDefinition("main", new List<VMType>(), intType),
                instructions,
                new List<VMType>() { intType });
        }

        /// <summary>
        /// Creates a new float branch program
        /// </summary>
        private Function CreateBranchFloatProgram(Win64Container container, OpCodes branchInstruction, float value1, float value2)
        {
            var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadFloat, value1));
            instructions.Add(new Instruction(OpCodes.LoadFloat, value2));
            instructions.Add(new Instruction(branchInstruction, 6));
            instructions.Add(new Instruction(OpCodes.LoadInt, 0));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));
            instructions.Add(new Instruction(OpCodes.Branch, 8));
            instructions.Add(new Instruction(OpCodes.LoadInt, 1));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));
            instructions.Add(new Instruction(OpCodes.LoadLocal, 0));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(
                new FunctionDefinition("main", new List<VMType>(), intType),
                instructions,
                new List<VMType>() { intType });
        }

        /// <summary>
        /// Tests the branch equal instruction
        /// </summary>
        [TestMethod]
        public void TestBranchEqual()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchEqual, 1, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchEqual, 2, 1)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch not equal instruction
        /// </summary>
        [TestMethod]
        public void TestBranchNotEqual()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchNotEqual, 2, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchNotEqual, 1, 1)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch less than instruction
        /// </summary>
        [TestMethod]
        public void TestLessThan()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchLessThan, 1, 2)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchLessThan, 1, 1)));

                Assert.AreEqual(0, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchLessThan, 2, 1)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch less than or equal instruction
        /// </summary>
        [TestMethod]
        public void TestLessThanOrEqual()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchLessOrEqual, 1, 2)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchLessOrEqual, 1, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchLessOrEqual, 2, 1)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch grater than instruction
        /// </summary>
        [TestMethod]
        public void TestGreaterThan()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchGreaterThan, 2, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchGreaterThan, 1, 1)));

                Assert.AreEqual(0, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchGreaterThan, 1, 2)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch grater than or equal instruction
        /// </summary>
        [TestMethod]
        public void TestGreaterThanOrEqual()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchGreaterOrEqual, 2, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchGreaterOrEqual, 1, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchProgram(container, OpCodes.BranchGreaterOrEqual, 1, 2)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch equal instruction float
        /// </summary>
        [TestMethod]
        public void TestBranchEqualFloat()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchEqual, 1.0f, 1.0f)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchEqual, 2.0f, 1.0f)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch not equal instruction float
        /// </summary>
        [TestMethod]
        public void TestBranchNotEqualFloat()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchNotEqual, 2, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchNotEqual, 1, 1)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch less than instruction float
        /// </summary>
        [TestMethod]
        public void TestLessThanFloat()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchLessThan, 1, 2)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchLessThan, 1, 1)));

                Assert.AreEqual(0, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchLessThan, 2, 1)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch less than or equal instruction float
        /// </summary>
        [TestMethod]
        public void TestLessThanOrEqualFloat()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchLessOrEqual, 1, 2)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchLessOrEqual, 1, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchLessOrEqual, 2, 1)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch grater than instruction float
        /// </summary>
        [TestMethod]
        public void TestGreaterThanFloat()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchGreaterThan, 2, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchGreaterThan, 1, 1)));

                Assert.AreEqual(0, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchGreaterThan, 1, 2)));

                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests the branch grater than or equal instruction float
        /// </summary>
        [TestMethod]
        public void TestGreaterThanOrEqualFloat()
        {
            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchGreaterOrEqual, 2, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchGreaterOrEqual, 1, 1)));

                Assert.AreEqual(1, container.Execute());
            }

            using (var container = new Win64Container())
            {
                container.LoadAssembly(Assembly.SingleFunction(
                    this.CreateBranchFloatProgram(container, OpCodes.BranchGreaterOrEqual, 1, 2)));

                Assert.AreEqual(0, container.Execute());
            }
        }
    }
}
