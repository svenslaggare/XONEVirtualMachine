using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test
{
    /// <summary>
    /// Generates test programs
    /// </summary>
    public static class TestProgramGenerator
    {
        /// <summary>
        /// A simple function without any control flow
        /// </summary>
        public static Function Simple(Win64Container container)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadInt, 2));
            instructions.Add(new Instruction(OpCodes.LoadInt, 4));
            instructions.Add(new Instruction(OpCodes.AddInt));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(
                new FunctionDefinition("main", new List<VMType>() { }, intType),
                instructions,
                new List<VMType>());
        }

        /// <summary>
        /// A simple function without any control flow
        /// </summary>
        public static Function Simple2(Win64Container container)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadInt, 2));
            instructions.Add(new Instruction(OpCodes.LoadInt, 4));
            instructions.Add(new Instruction(OpCodes.LoadInt, 6));
            instructions.Add(new Instruction(OpCodes.AddInt));
            instructions.Add(new Instruction(OpCodes.AddInt));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(
                new FunctionDefinition("main", new List<VMType>() { }, intType),
                instructions,
                new List<VMType>());
        }

        /// <summary>
        /// A simple function without any control flow
        /// </summary>
        public static Function Simple3(Win64Container container)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadInt, 1));
            instructions.Add(new Instruction(OpCodes.LoadInt, 2));
            instructions.Add(new Instruction(OpCodes.AddInt));
            instructions.Add(new Instruction(OpCodes.LoadInt, 3));
            instructions.Add(new Instruction(OpCodes.AddInt));
            instructions.Add(new Instruction(OpCodes.LoadInt, 4));
            instructions.Add(new Instruction(OpCodes.AddInt));
            instructions.Add(new Instruction(OpCodes.LoadInt, 5));
            instructions.Add(new Instruction(OpCodes.AddInt));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(
                new FunctionDefinition("main", new List<VMType>() { }, intType),
                instructions,
                new List<VMType>());
        }

        /// <summary>
        /// A function with branches
        /// </summary>
        public static Function Branch(Win64Container container)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadInt, 4));
            instructions.Add(new Instruction(OpCodes.LoadInt, 2));
            instructions.Add(new Instruction(OpCodes.BranchEqual, 6));

            instructions.Add(new Instruction(OpCodes.LoadInt, 5));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));
            instructions.Add(new Instruction(OpCodes.Branch, 8));

            instructions.Add(new Instruction(OpCodes.LoadInt, 15));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));

            instructions.Add(new Instruction(OpCodes.LoadLocal, 0));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(
                new FunctionDefinition("main", new List<VMType>() { }, intType),
                instructions,
                new List<VMType>() { intType });
        }

        /// <summary>
        /// A function with multiple returns
        /// </summary>
        public static Function MultipleReturns(Win64Container container)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadInt, 4));
            instructions.Add(new Instruction(OpCodes.Ret));

            instructions.Add(new Instruction(OpCodes.LoadInt, 5));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(
                new FunctionDefinition("main", new List<VMType>() { }, intType),
                instructions,
                new List<VMType>() { intType });
        }

        /// <summary>
        /// The max function
        /// </summary>
        public static Function Max(Win64Container container)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));
            instructions.Add(new Instruction(OpCodes.LoadArgument, 1));
            instructions.Add(new Instruction(OpCodes.BranchGreaterThan, 6));

            instructions.Add(new Instruction(OpCodes.LoadArgument, 1));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));
            instructions.Add(new Instruction(OpCodes.Branch, 9));

            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));
            instructions.Add(new Instruction(OpCodes.Branch, 9));

            instructions.Add(new Instruction(OpCodes.LoadLocal, 0));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(
                new FunctionDefinition("max", new List<VMType>() { intType, intType }, intType),
                instructions,
                new List<VMType>() { intType });
        }

        /// <summary>
        /// Function with locals with none overlapping life time
        /// </summary>
        public static Function Locals(Win64Container container)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadInt, 2));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));

            instructions.Add(new Instruction(OpCodes.LoadInt, 4));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 1));

            instructions.Add(new Instruction(OpCodes.LoadLocal, 1));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(
                new FunctionDefinition("main", new List<VMType>(), intType),
                instructions,
                new List<VMType>() { intType, intType });
        }
    }
}
