﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Programs
{
    /// <summary>
    /// Test function calls
    /// </summary>
    [TestClass]
    public class TestCalls
    {
        /// <summary>
        /// Tests arguments
        /// </summary>
        [TestMethod]
        public void TestArguments()
        {
            for (int i = 16; i <= 16; i++)
            {
                using (var container = new Win64Container())
                {
                    var assembly = new Assembly(
                        TestProgramGenerator.AddMainFunction(container, i),
                        TestProgramGenerator.AddFunction(container, i));

                    container.VirtualMachine.LoadAssembly(assembly);
                    Assert.AreEqual(i * (1 + i) / 2, container.Execute());
                }
            }
        }

        private delegate float FloatMain();

        /// <summary>
        /// Tests float arguments
        /// </summary>
        [TestMethod]
        public void TestFloatArguments()
        {
            for (int i = 1; i <= 16; i++)
            {
                using (var container = new Win64Container())
                {
                    var assembly = new Assembly(
                        TestProgramGenerator.FloatAddMainFunction(container, i),
                        TestProgramGenerator.FloatAddFunction(container, i));

                    container.VirtualMachine.LoadAssembly(assembly);
                    container.VirtualMachine.Compile();
                    var funcPtr = Marshal.GetDelegateForFunctionPointer<FloatMain>(
                        container.VirtualMachine.Binder.GetFunction("floatMain()").EntryPoint);
                    Assert.AreEqual(i * (1 + i) / 2, funcPtr());
                }
            }
        }

        private delegate int FuncIntArgIntIntIntIntIntIntIntIntInt(int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9);

        private int StackAdd(int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8, int x9)
        {
            return x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9;
        }

        /// <summary>
        /// Tests the stack arguments
        /// </summary>
        [TestMethod]
        public void TestStackArguments()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var parameters = Enumerable.Repeat(intType, 9).ToList();

                container.VirtualMachine.Binder.Define(FunctionDefinition.NewExternal<FuncIntArgIntIntIntIntIntIntIntIntInt>(
                    "add",
                    parameters,
                    intType,
                    StackAdd));

                var def = new FunctionDefinition("main", new List<VMType>(), intType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadInt, 1));
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.LoadInt, 3));
                instructions.Add(new Instruction(OpCodes.LoadInt, 4));
                instructions.Add(new Instruction(OpCodes.LoadInt, 5));
                instructions.Add(new Instruction(OpCodes.LoadInt, 6));
                instructions.Add(new Instruction(OpCodes.LoadInt, 7));
                instructions.Add(new Instruction(OpCodes.LoadInt, 8));
                instructions.Add(new Instruction(OpCodes.LoadInt, 9));

                instructions.Add(new Instruction(
                    OpCodes.Call,
                    "add",
                    parameters));

                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(def, instructions, new List<VMType>());
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9, container.Execute());
            }
        }

        private delegate int FuncIntArgIntIntIntIntIntIntIntInt(int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8);

        private int StackAdd(int x1, int x2, int x3, int x4, int x5, int x6, int x7, int x8)
        {
            return x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8;
        }

        /// <summary>
        /// Tests the stack arguments
        /// </summary>
        [TestMethod]
        public void TestStackArguments2()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var parameters = Enumerable.Repeat(intType, 8).ToList();

                container.VirtualMachine.Binder.Define(FunctionDefinition.NewExternal<FuncIntArgIntIntIntIntIntIntIntInt>(
                    "add",
                    parameters,
                    intType,
                    StackAdd));

                var def = new FunctionDefinition("main", new List<VMType>(), intType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadInt, 1));
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.LoadInt, 3));
                instructions.Add(new Instruction(OpCodes.LoadInt, 4));
                instructions.Add(new Instruction(OpCodes.LoadInt, 5));
                instructions.Add(new Instruction(OpCodes.LoadInt, 6));
                instructions.Add(new Instruction(OpCodes.LoadInt, 7));
                instructions.Add(new Instruction(OpCodes.LoadInt, 8));

                instructions.Add(new Instruction(
                    OpCodes.Call,
                    "add",
                    parameters));

                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(def, instructions, new List<VMType>());
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(1 + 2 + 3 + 4 + 5 + 6 + 7 + 8, container.Execute());
            }
        }

        private delegate int FuncIntArgIntFloatIntFloatIntFloat(int x1, float x2, int x3, float x4, int x5, float x6);

        private int MixedAdd(int x1, float x2, int x3, float x4, int x5, float x6)
        {
            return x1 + (int)x2 + x3 + (int)x4 + x5 + (int)x6;
        }

        /// <summary>
        /// Tests mixed arguments
        /// </summary>
        [TestMethod]
        public void TestMixedArguments()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var parameters = new List<VMType>() { intType, floatType, intType, floatType, intType, floatType };

                container.VirtualMachine.Binder.Define(FunctionDefinition.NewExternal<FuncIntArgIntFloatIntFloatIntFloat>(
                    "add",
                    parameters,
                    intType,
                    MixedAdd));

                var def = new FunctionDefinition("main", new List<VMType>(), intType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadInt, 1));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 2.0f));
                instructions.Add(new Instruction(OpCodes.LoadInt, 3));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 4.0f));
                instructions.Add(new Instruction(OpCodes.LoadInt, 5));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 6.0f));

                instructions.Add(new Instruction(
                    OpCodes.Call,
                    "add",
                    parameters));

                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(def, instructions, new List<VMType>());
                container.LoadAssembly(Assembly.SingleFunction(func));

                Assert.AreEqual(1 + 2 + 3 + 4 + 5 + 6, container.Execute());
            }
        }

        /// <summary>
        /// Tests calling function defined before
        /// </summary>
        [TestMethod]
        public void DefinitionOrder()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var assemblyFunctions = new List<Function>();

                Action testFn = () =>
                {
                    var def = new FunctionDefinition("test", new List<VMType>(), intType);

                    var instructions = new List<Instruction>();

                    instructions.Add(new Instruction(OpCodes.LoadInt, 1));
                    instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                    instructions.Add(new Instruction(OpCodes.AddInt));
                    instructions.Add(new Instruction(OpCodes.Ret));

                    var func = new Function(def, instructions, new List<VMType>());
                    assemblyFunctions.Add(func);
                };

                Action mainFn = () =>
                {
                    var def = new FunctionDefinition("main", new List<VMType>(), intType);

                    var instructions = new List<Instruction>();
                    instructions.Add(new Instruction(OpCodes.Call, "test", new List<VMType>()));
                    instructions.Add(new Instruction(OpCodes.Ret));

                    var func = new Function(def, instructions, new List<VMType>());
                    assemblyFunctions.Add(func);
                };

                mainFn();
                testFn();
                container.LoadAssembly(new Assembly(assemblyFunctions));
                Assert.AreEqual(3, container.Execute());
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
                var assembly = new Assembly(
                    TestProgramGenerator.MainWithIntCall(container, "sum", 10),
                    TestProgramGenerator.ResursiveSum(container));

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
                var assembly = new Assembly(
                    TestProgramGenerator.MainWithIntCall(container, "fib", 11),
                    TestProgramGenerator.RecursiveFib(container));

                container.LoadAssembly(assembly);
                Assert.AreEqual(89, container.Execute());
            }
        }
    }
}
