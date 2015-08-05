using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Compiler.Analysis;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Analysis
{
    /// <summary>
    /// Tests virtual registers
    /// </summary>
    [TestClass]
    public class TestVirtualRegisters
    {
        /// <summary>
        /// Tests a simple function
        /// </summary>
        [TestMethod]
        public void TestSimple()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

                var instructions = new List<Instruction>();
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.LoadInt, 4));
                instructions.Add(new Instruction(OpCodes.AddInt));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(
                    new FunctionDefinition("test", new List<VMType>() { }, intType),
                    instructions,
                    new List<VMType>());

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisterIR.Create(container.VirtualMachine, func);
                Assert.AreEqual(4, virtualInstructions.Count);

                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 0), virtualInstructions[0].AssignRegister);

                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 1), virtualInstructions[1].AssignRegister);

                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 0), virtualInstructions[2].AssignRegister);
                Assert.AreEqual(2, virtualInstructions[2].UsesRegisters.Count);
                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 1), virtualInstructions[2].UsesRegisters[0]);
                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 0), virtualInstructions[2].UsesRegisters[1]);

                Assert.IsNull(virtualInstructions[3].AssignRegister);
                Assert.AreEqual(1, virtualInstructions[3].UsesRegisters.Count);
                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 0), virtualInstructions[3].UsesRegisters[0]);
            }
        }

        /// <summary>
        /// Tests a function with mixed types
        /// </summary>
        [TestMethod]
        public void TestMixedTypes()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var floatType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
                var paramTypes = new List<VMType>() { intType, floatType };

                var addFunc = new Function(
                    new FunctionDefinition("add", paramTypes, intType),
                    new List<Instruction>()
                    {
                        new Instruction(OpCodes.LoadInt, 0),
                        new Instruction(OpCodes.Ret)
                    },
                    new List<VMType>());

                var instructions = new List<Instruction>();
                instructions.Add(new Instruction(OpCodes.LoadInt, 2));
                instructions.Add(new Instruction(OpCodes.LoadFloat, 4));
                instructions.Add(new Instruction(OpCodes.Call, "add", paramTypes));
                instructions.Add(new Instruction(OpCodes.Ret));

                container.VirtualMachine.Binder.Define(addFunc.Definition);

                var func = new Function(
                    new FunctionDefinition("test", paramTypes, intType),
                    instructions,
                    new List<VMType>());

                container.VirtualMachine.Verifier.VerifiyFunction(func);

                var virtualInstructions = VirtualRegisterIR.Create(container.VirtualMachine, func);
                Assert.AreEqual(4, virtualInstructions.Count);

                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 0), virtualInstructions[0].AssignRegister);

                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Float, 1), virtualInstructions[1].AssignRegister);

                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 0), virtualInstructions[2].AssignRegister);
                Assert.AreEqual(2, virtualInstructions[2].UsesRegisters.Count);
                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Float, 1), virtualInstructions[2].UsesRegisters[0]);
                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 0), virtualInstructions[2].UsesRegisters[1]);

                Assert.IsNull(virtualInstructions[3].AssignRegister);
                Assert.AreEqual(1, virtualInstructions[3].UsesRegisters.Count);
                Assert.AreEqual(new VirtualRegister(VirtualRegisterType.Integer, 0), virtualInstructions[3].UsesRegisters[0]);
            }
        }
    }
}
