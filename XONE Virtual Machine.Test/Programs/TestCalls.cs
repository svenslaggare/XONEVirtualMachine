using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Creates a main function that calls the add function with the given number of arguments
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="numArgs">The number of arguments</param>
        private Function CreateMainFunction(Win64Container container, int numArgs)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
            var def = new FunctionDefinition("main", new List<VMType>(), intType);

            var parameters = new List<VMType>();
            for (int i = 0; i < numArgs; i++)
            {
                parameters.Add(intType);
            }

            var instructions = new List<Instruction>();

            for (int i = 1; i <= numArgs; i++)
            {
                instructions.Add(new Instruction(OpCodes.LoadInt, i));
            }

            instructions.Add(new Instruction(OpCodes.Call, "add", parameters));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(def, instructions, new List<VMType>());
        }

        /// <summary>
        /// Creates a add function with that takes the given amount of arguments
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="numArgs">The number of arguments</param>
        private Function CreateAddFunction(Win64Container container, int numArgs)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var parameters = new List<VMType>();
            for (int i = 0; i < numArgs; i++)
            {
                parameters.Add(intType);
            }

            var def = new FunctionDefinition("add", parameters, intType);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));

            for (int i = 1; i < numArgs; i++)
            {
                instructions.Add(new Instruction(OpCodes.LoadArgument, i));
                instructions.Add(new Instruction(OpCodes.AddInt));
            }

            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(def, instructions, new List<VMType>());
        }

        /// <summary>
        /// Tests arguments
        /// </summary>
        [TestMethod]
        public void TestArguments()
        {
            for (int i = 1; i <= 10; i++)
            {
                using (var container = new Win64Container())
                {
                    var assembly = new Assembly(
                        this.CreateAddFunction(container, i),
                        this.CreateMainFunction(container, i));

                    container.VirtualMachine.LoadAssembly(assembly);
                    Assert.AreEqual(i * (1 + i) / 2, container.Execute());
                }
            }
        }

        private delegate int FuncIntArgIntIntIntIntIntInt(int x1, int x2, int x3, int x4, int x5, int x6);

        private int StackAdd(int x1, int x2, int x3, int x4, int x5, int x6)
        {
            return x1 + x2 + x3 + x4 + x5 + x6;
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
                var parameters = Enumerable.Repeat(intType, 6).ToList();

                container.VirtualMachine.Binder.Define(FunctionDefinition.NewExternal<FuncIntArgIntIntIntIntIntInt>(
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
    }
}
