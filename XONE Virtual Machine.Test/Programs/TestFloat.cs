﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Programs
{
    /// <summary>
    /// Tests floating point instructions
    /// </summary>
    [TestClass]
    public class TestFloat
    {
        delegate float FloatEntryPoint();

        /// <summary>
        /// Executes a program that has an entry point that returns a float
        /// </summary>
        private static float ExecuteFloatProgram(Win64Container container, string entryPointName = "main")
        {
            container.VirtualMachine.Compile();
            var entryPoint = container.VirtualMachine.Binder.GetFunction(entryPointName + "()");
            var programPtr = (FloatEntryPoint)Marshal.GetDelegateForFunctionPointer(
                entryPoint.EntryPoint,
                typeof(FloatEntryPoint));

            return programPtr();
        }

        /// <summary>
        /// Tests the add instruction
        /// </summary>
        [TestMethod]
        public void TestAdd()
        {
            using (var container = new Win64Container())
            {
                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var funcDef = new FunctionDefinition("main", new List<VMType>(), floatType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadFloat, 2.5f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 1.35f));
                instructions.Add(new Instruction(OpCodes.AddFloat));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.OperandStackSize = 2;

                container.VirtualMachine.LoadFunction(func);
                Assert.AreEqual(2.5f + 1.35f, ExecuteFloatProgram(container), 1E-4);
            }
        }

        /// <summary>
        /// Tests the sub instruction
        /// </summary>
        [TestMethod]
        public void TestSub()
        {
            using (var container = new Win64Container())
            {
                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var funcDef = new FunctionDefinition("main", new List<VMType>(), floatType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadFloat, 2.5f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 1.35f));
                instructions.Add(new Instruction(OpCodes.SubFloat));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.OperandStackSize = 2;

                container.VirtualMachine.LoadFunction(func);
                Assert.AreEqual(2.5f - 1.35f, ExecuteFloatProgram(container), 1E-4);
            }
        }

        /// <summary>
        /// Tests the mul instruction
        /// </summary>
        [TestMethod]
        public void TestMul()
        {
            using (var container = new Win64Container())
            {
                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var funcDef = new FunctionDefinition("main", new List<VMType>(), floatType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadFloat, 2.5f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 1.35f));
                instructions.Add(new Instruction(OpCodes.MulFloat));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.OperandStackSize = 2;

                container.VirtualMachine.LoadFunction(func);
                Assert.AreEqual(2.5f * 1.35f, ExecuteFloatProgram(container), 1E-4);
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
                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var funcDef = new FunctionDefinition("main", new List<VMType>(), floatType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadFloat, 2.5f));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 1.35f));
                instructions.Add(new Instruction(OpCodes.DivFloat));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, new List<VMType>());
                func.OperandStackSize = 2;

                container.VirtualMachine.LoadFunction(func);
                Assert.AreEqual(2.5f / 1.35f, ExecuteFloatProgram(container), 1E-4);
            }
        }
    }
}
