using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Compiler.Analysis;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Programs
{
    /// <summary>
    /// Tests optimized functions
    /// </summary>
    [TestClass]
    public class TestOptimized
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
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(6, container.Execute());
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
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(12, container.Execute());
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
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(15, container.Execute());
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
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(4, container.Execute());
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
                int count = 100;
                var func = TestProgramGenerator.LoopCount(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(count, container.Execute());
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
                int count = 100;
                var func = TestProgramGenerator.SumNoneLoop(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual((count * (count + 1)) / 2, container.Execute());
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
                int count = 100;
                var func = TestProgramGenerator.SumNoneLoopLocal(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual((count * (count + 1)) / 2, container.Execute());
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
                int count = 10;
                var func = TestProgramGenerator.NegativeSumNoneLoop(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(TestProgramGenerator.NegativeSumResult(count), container.Execute());
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
                int count = 10;
                int product = Enumerable.Aggregate(Enumerable.Range(1, count), 1, (total, current) => total * current);
                var func = TestProgramGenerator.ProductNoneLoop(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(product, container.Execute());
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
        /// Tests function calls
        /// </summary>
        [TestMethod]
        public void TestCallOrder()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var paramsType = Enumerable.Repeat(intType, 2).ToList();

                var addFunc = new Function(
                    new FunctionDefinition("sub", paramsType, intType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadArgument, 0),
                        new Instruction(OpCodes.LoadArgument, 1),
                        new Instruction(OpCodes.SubInt),
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
                        new Instruction(OpCodes.LoadInt, 6),
                        new Instruction(OpCodes.LoadInt, 2),
                        new Instruction(OpCodes.Call, "sub", paramsType.ToList()),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>())
                {
                    Optimize = true
                };

                container.LoadAssembly(new Assembly(addFunc, mainFunc));
                Assert.AreEqual(4, container.Execute());
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

        /// <summary>
        /// Tests a simple float program
        /// </summary>
        [TestMethod]
        public void TestFloat()
        {
            using (var container = new Win64Container())
            {
                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var funcDef = new FunctionDefinition("floatMain", new List<VMType>(), floatType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadFloat, 2.5f));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(2.5f, TestProgramGenerator.ExecuteFloatProgram(container), 1E-4);
            }
        }

        /// <summary>
        /// Tests the float add instruction
        /// </summary>
        [TestMethod]
        public void TestFloatAdd()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 5;

                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var funcDef = new FunctionDefinition("floatMain", new List<VMType>(), floatType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadFloat, 2.5f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 1.35f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 4.5f));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(2.5f + 1.35f + 4.5f, TestProgramGenerator.ExecuteFloatProgram(container), 1E-4);
            }
        }

        /// <summary>
        /// Tests the float add instruction
        /// </summary>
        [TestMethod]
        public void TestFloatAdd2()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 5;

                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var funcDef = new FunctionDefinition("floatMain", new List<VMType>(), floatType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadFloat, 1f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 2f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 3f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 4f));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(1 + 2 + 3 + 4, TestProgramGenerator.ExecuteFloatProgram(container), 1E-4);
            }
        }

        /// <summary>
        /// Tests the float add instruction
        /// </summary>
        [TestMethod]
        public void TestFloatAdd3()
        {
            using (var container = new Win64Container())
            {
                container.VirtualMachine.Settings["NumIntRegisters"] = 5;

                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var funcDef = new FunctionDefinition("floatMain", new List<VMType>(), floatType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadFloat, 1f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 2f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 3f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 4f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 5f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 6f));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(1 + 2 + 3 + 4 + 5 + 6, TestProgramGenerator.ExecuteFloatProgram(container), 1E-4);
            }
        }

        /// <summary>
        /// Tests float function calls
        /// </summary>
        [TestMethod]
        public void TestFloatCall()
        {
            for (int i = 1; i <= 16; i++)
            {
                using (var container = new Win64Container())
                {
                    container.VirtualMachine.Settings["NumIntRegisters"] = 5;

                    var mainFunc = TestProgramGenerator.FloatAddMainFunction(container, i);
                    mainFunc.Optimize = true;

                    var addFunc = TestProgramGenerator.FloatAddFunction(container, i);
                    addFunc.Optimize = true;

                    var assembly = new Assembly(mainFunc, addFunc);

                    container.VirtualMachine.LoadAssembly(assembly);
                    Assert.AreEqual(i * (1 + i) / 2, TestProgramGenerator.ExecuteFloatProgram(container));
                }
            }
        }
    }
}
