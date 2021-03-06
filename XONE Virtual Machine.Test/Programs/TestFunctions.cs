﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Programs
{
    /// <summary>
    /// Tests function definitions
    /// </summary>
    [TestClass]
    public class TestFunctions
    {
        /// <summary>
        /// Tests when the entry point is not defined
        /// </summary>
        [TestMethod]
        public void TestNoMain()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var testFunc = new Function(
                    new FunctionDefinition("test", new List<VMType>() { intType }, intType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadInt, 0),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                container.LoadAssembly(Assembly.SingleFunction(testFunc));

                try
                {
                    container.Execute();
                    Assert.Fail("Expected no entry point to not pass.");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("There is no entry point defined.", e.Message);
                }
            }
        }

        /// <summary>
        /// Tests an invalid main function
        /// </summary>
        [TestMethod]
        public void TestInvalidMain()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var mainFunc = new Function(
                    new FunctionDefinition("main", new List<VMType>() { intType }, intType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadInt, 0),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                try
                {
                    container.LoadAssembly(Assembly.SingleFunction(mainFunc));
                    Assert.Fail("Expected invalid main to not pass.");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Expected the main function to have the signature: 'main() Int'.", e.Message);
                }
            }
        }

        /// <summary>
        /// Tests an invalid main function
        /// </summary>
        [TestMethod]
        public void TestInvalidMain2()
        {
            using (var container = new Win64Container())
            {
                var voidType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Void);

                var mainFunc = new Function(
                    new FunctionDefinition("main", new List<VMType>() { }, voidType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                try
                {
                    container.LoadAssembly(Assembly.SingleFunction(mainFunc));
                    Assert.Fail("Expected invalid main to not pass.");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("Expected the main function to have the signature: 'main() Int'.", e.Message);
                }
            }
        }

        /// <summary>
        /// Tests function overload
        /// </summary>
        [TestMethod]
        public void TestOverload()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);

                var func1 = new Function(
                    new FunctionDefinition("test", new List<VMType>() { intType }, intType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadInt, 0),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                var func2 = new Function(
                    new FunctionDefinition("test", new List<VMType>() { floatType }, floatType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadFloat, 0.0f),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                var assembly = new Assembly(func1, func2);
                container.LoadAssembly(assembly);
            }
        }

        /// <summary>
        /// Tests invalid function overload
        /// </summary>
        [TestMethod]
        public void TestInvalidOverload()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);

                var func1 = new Function(
                    new FunctionDefinition("test", new List<VMType>() { intType }, intType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadInt, 0),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                var func2 = new Function(
                    new FunctionDefinition("test", new List<VMType>() { intType }, floatType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadFloat, 0.0f),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                var assembly = new Assembly(func1, func2);

                try
                {
                    container.LoadAssembly(assembly);
                    Assert.Fail("Expected invalid overload to not pass.");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("The function 'test(Int) Float' is already defined.", e.Message);
                }
            }
        }

        /// <summary>
        /// Tests defining a function that already exists
        /// </summary>
        [TestMethod]
        public void TestAlreadyDefined()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var func1 = new Function(
                    new FunctionDefinition("test", new List<VMType>(), intType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadInt, 0),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                var func2 = new Function(
                    new FunctionDefinition("test", new List<VMType>(), intType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadInt, 0),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                var assembly = new Assembly(func1, func2);

                try
                {
                    container.LoadAssembly(assembly);
                    Assert.Fail("Expected already defined to not pass.");
                }
                catch (Exception e)
                {
                    Assert.AreEqual("The function 'test() Int' is already defined.", e.Message);
                }
            }
        }
    }
}
