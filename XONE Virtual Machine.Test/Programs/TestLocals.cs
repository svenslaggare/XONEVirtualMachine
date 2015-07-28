using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Programs
{
    /// <summary>
    /// Tests locals
    /// </summary>
    [TestClass]
    public class TestLocals
    {
        /// <summary>
        /// Tests locals
        /// </summary>
        [TestMethod]
        public void TestLocals1()
        {
            using (var container = new Win64Container())
            {
                var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
                var funcDef = new FunctionDefinition("main", new List<VMType>(), intType);

                var instructions = new List<Instruction>();

                instructions.Add(new Instruction(OpCodes.LoadInt, 100));
                instructions.Add(new Instruction(OpCodes.StoreLocal, 0));

                instructions.Add(new Instruction(OpCodes.LoadInt, 200));
                instructions.Add(new Instruction(OpCodes.StoreLocal, 1));

                instructions.Add(new Instruction(OpCodes.LoadInt, 300));
                instructions.Add(new Instruction(OpCodes.StoreLocal, 2));

                instructions.Add(new Instruction(OpCodes.LoadInt, 400));
                instructions.Add(new Instruction(OpCodes.StoreLocal, 3));

                instructions.Add(new Instruction(OpCodes.LoadLocal, 3));
                instructions.Add(new Instruction(OpCodes.Ret));

                var func = new Function(funcDef, instructions, Enumerable.Repeat(intType, 4).ToList());
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(400, container.Execute());
            }
        }
    }
}
