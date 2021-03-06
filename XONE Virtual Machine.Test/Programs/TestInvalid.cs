﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Programs
{
    /// <summary>
    /// Tests programs that are invalid (e.g don't get pass verifier)
    /// </summary>
    [TestClass]
    public class TestInvalid
    {
        /// <summary>
        /// Tests an empty function
        /// </summary>
        [TestMethod]
        public void TestEmpty()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var func = new Function(
                    new FunctionDefinition("main", new List<VMType>(), intType),
                    new List<Instruction>(),
                    new List<VMType>());

                container.LoadAssembly(Assembly.SingleFunction(func));

                try
                {
                    container.Execute();
                    Assert.Fail("Expected empty functions to not pass.");
                }
                catch (VerificationException e)
                {
                    Assert.AreEqual("0: Empty functions are not allowed.", e.Message);
                }
            }
        }

        /// <summary>
        /// Tests with a void parameter
        /// </summary>
        [TestMethod]
        public void TestVoidParameter()
        {
            using (var container = new Win64Container())
            {
                var voidType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Void);

                var instructions = new List<Instruction>();
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(
                    new FunctionDefinition("test", new List<VMType>() { voidType }, voidType),
                    instructions,
                    new List<VMType>());

                container.LoadAssembly(Assembly.SingleFunction(func));

                try
                {
                    container.Execute();
                    Assert.Fail("Expected void parameter to not pass.");
                }
                catch (VerificationException e)
                {
                    Assert.AreEqual("0: 'Void' is not a valid parameter type.", e.Message);
                }
            }
        }

        /// <summary>
        /// Tests ending a function without a return
        /// </summary>
        [TestMethod]
        public void TestNotEndInReturn()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var instructions = new List<Instruction>();
                instructions.Add(new Instruction(OpCodes.LoadInt, 0));

                var func = new Function(
                    new FunctionDefinition("main", new List<VMType>(), intType),
                    instructions,
                    new List<VMType>());

                container.LoadAssembly(Assembly.SingleFunction(func));

                try
                {
                    container.Execute();
                    Assert.Fail("Expected without return to not pass.");
                }
                catch (VerificationException e)
                {
                    Assert.AreEqual("0: Functions must end with a return instruction.", e.Message);
                }
            }
        }
    }
}
