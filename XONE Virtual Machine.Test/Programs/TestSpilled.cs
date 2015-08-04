using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Programs
{
    /// <summary>
    /// Tests spilled memory
    /// </summary>
    [TestClass]
    public class TestSpilled
    {
        /// <summary>
        /// Tests a simple function
        /// </summary>
        [TestMethod]
        public void TestSimple()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;
                var func = TestProgramGenerator.Simple(container);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), 6);
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
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;
                var func = TestProgramGenerator.Simple2(container);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), 12);
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
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;
                var func = TestProgramGenerator.Simple3(container);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), 15);
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
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;
                var func = TestProgramGenerator.Locals(container);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), 4);
            }
        }

        /// <summary>
        /// Tests a function with a loop
        /// </summary>
        [TestMethod]
        public void TestLoop()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;
                int count = 100;
                var func = TestProgramGenerator.LoopCount(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), count);
            }
        }

        /// <summary>
        /// Tests the sum function
        /// </summary>
        [TestMethod]
        public void TestSum()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;
                int count = 100;
                var func = TestProgramGenerator.SumNoneLoop(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), (count * (count + 1)) / 2);
            }
        }

        /// <summary>
        /// Tests the sum function with a locals
        /// </summary>
        [TestMethod]
        public void TestSumLocal()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;
                int count = 100;
                var func = TestProgramGenerator.SumNoneLoopLocal(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), (count * (count + 1)) / 2);
            }
        }

        /// <summary>
        /// Tests the negative sum function
        /// </summary>
        [TestMethod]
        public void TestNegativeSum()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;
                int count = 10;
                var func = TestProgramGenerator.NegativeSumNoneLoop(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), TestProgramGenerator.NegativeSumResult(count));
            }
        }

        /// <summary>
        /// Tests the product function
        /// </summary>
        [TestMethod]
        public void TestProduct()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;
                int count = 10;
                int product = Enumerable.Aggregate(Enumerable.Range(1, count), 1, (total, current) => total * current);
                var func = TestProgramGenerator.ProductNoneLoop(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), product);
            }
        }

        /// <summary>
        /// Tests function calls
        /// </summary>
        [TestMethod]
        public void TestCall()
        {
            for (int i = 1; i <= 16; i++)
            {
                using (var container = new Win64Container())
                {
                    container.VirtualMachine.Settings["NumIntRegisters"] = 0;

                    var mainFunc = TestProgramGenerator.AddMainFunction(container, i);
                    mainFunc.Optimize = true;

                    var addFunc = TestProgramGenerator.AddFunction(container, i);
                    addFunc.Optimize = true;

                    var assembly = new Assembly(mainFunc, addFunc);

                    container.VirtualMachine.LoadAssembly(assembly);
                    Assert.AreEqual(i * (1 + i) / 2, container.Execute());
                }
            }
        }

        /// <summary>
        /// Tests function call
        /// </summary>
        [TestMethod]
        public void TestCallVoid()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;

                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var voidType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Void);

                var nopFunc = new Function(
                    new FunctionDefinition("nop", new List<VMType>(), voidType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>())
                {
                    Optimize = true
                };

                var mainFunc = new Function(
                    new FunctionDefinition("main", new List<VMType>(), intType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.Call, "nop", new List<VMType>()),
                        new Instruction(OpCodes.LoadInt, 0),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>())
                {
                    Optimize = true
                };

                container.LoadAssembly(new Assembly(nopFunc, mainFunc));
                Assert.AreEqual(0, container.Execute());
            }
        }

        /// <summary>
        /// Tests a recursive function
        /// </summary>
        [TestMethod]
        public void TestRecursive1()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;

                var mainFunc = TestProgramGenerator.MainWithIntCall(container, "sum", 10);
                mainFunc.Optimize = true;

                var sumFunc = TestProgramGenerator.ResursiveSum(container);
                sumFunc.Optimize = true;

                var assembly = new Assembly(mainFunc, sumFunc);

                container.LoadAssembly(assembly);
                Assert.AreEqual(55, container.Execute());
            }
        }

        /// <summary>
        /// Tests a recursive function
        /// </summary>
        [TestMethod]
        public void TestRecursive2()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;

                var mainFunc = TestProgramGenerator.MainWithIntCall(container, "fib", 11);
                mainFunc.Optimize = true;

                var sumFunc = TestProgramGenerator.RecursiveFib(container);
                sumFunc.Optimize = true;

                var assembly = new Assembly(mainFunc, sumFunc);

                container.LoadAssembly(assembly);
                Assert.AreEqual(89, container.Execute());
            }
        }

        /// <summary>
        /// Tests the div instruction
        /// </summary>
        [TestMethod]
        public void TestDiv()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;

                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var funcDef = new FunctionDefinition("main", new List<VMType>(), intType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadInt, 4));
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.DivInt));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(4 / 2, container.Execute());
            }
        }

        /// <summary>
        /// Tests the div instruction
        /// </summary>
        [TestMethod]
        public void TestDiv2()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 0;

                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var funcDef = new FunctionDefinition("main", new List<VMType>(), intType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadInt, 3));
                instructions.Add(new Instruction(OpCodes.LoadInt, 8));
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.DivInt));
                instructions.Add(new Instruction(OpCodes.MulInt));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(12, container.Execute());
            }
        }
    }
}
