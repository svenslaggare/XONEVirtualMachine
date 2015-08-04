using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Compiler;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine
{
	class Program
	{
        /// <summary>
        /// Creates an add function with that takes the given amount of arguments
        /// </summary>
        private static Function CreateAddFunction(Win64Container container, int numArgs, bool optimize = false)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var def = new FunctionDefinition("add", Enumerable.Repeat(intType, numArgs).ToList(), intType);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));

            for (int i = 1; i < numArgs; i++)
            {
                instructions.Add(new Instruction(OpCodes.LoadArgument, i));
                instructions.Add(new Instruction(OpCodes.AddInt));
            }

            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(def, instructions, new List<VMType>())
            {
                Optimize = optimize
            };
        }

        /// <summary>
        /// Creates a loop call add function
        /// </summary>
        private static Function CreateLoopCallAdd(Win64Container container, int count, bool optimize = false)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var def = new FunctionDefinition("main", new List<VMType>(), intType);

            var instructions = new List<Instruction>();

            instructions.Add(new Instruction(OpCodes.LoadInt, count));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));

            instructions.Add(new Instruction(OpCodes.LoadInt, 1));
            instructions.Add(new Instruction(OpCodes.LoadLocal, 1));
            instructions.Add(new Instruction(OpCodes.Call, "add", Enumerable.Repeat(intType, 2).ToList()));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 1));

            instructions.Add(new Instruction(OpCodes.LoadLocal, 0));
            instructions.Add(new Instruction(OpCodes.LoadInt, 1));
            instructions.Add(new Instruction(OpCodes.SubInt));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));
            instructions.Add(new Instruction(OpCodes.LoadLocal, 0));

            instructions.Add(new Instruction(OpCodes.LoadInt, 0));
            instructions.Add(new Instruction(OpCodes.BranchGreaterThan, 2));

            instructions.Add(new Instruction(OpCodes.LoadLocal, 1));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(def, instructions, new List<VMType>() { intType, intType })
            {
                Optimize = optimize
            };
        }

        /// <summary>
        /// Creates the fibonacci function
        /// </summary>
        private static Function CreateFibFunction(Win64Container container, bool optimize = false)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var def = new FunctionDefinition("fib", Enumerable.Repeat(intType, 1).ToList(), intType);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));
            instructions.Add(new Instruction(OpCodes.LoadInt, 1));
            instructions.Add(new Instruction(OpCodes.BranchGreaterThan, 5));
            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));
            instructions.Add(new Instruction(OpCodes.Ret));

            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));
            instructions.Add(new Instruction(OpCodes.LoadInt, 2));
            instructions.Add(new Instruction(OpCodes.SubInt));
            instructions.Add(new Instruction(OpCodes.Call, "fib", Enumerable.Repeat(intType, 1).ToList()));

            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));
            instructions.Add(new Instruction(OpCodes.LoadInt, 1));
            instructions.Add(new Instruction(OpCodes.SubInt));
            instructions.Add(new Instruction(OpCodes.Call, "fib", Enumerable.Repeat(intType, 1).ToList()));

            instructions.Add(new Instruction(OpCodes.AddInt));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(def, instructions, new List<VMType>())
            {
                Optimize = optimize
            };
        }

        /// <summary>
        /// Creates the fibonacci function
        /// </summary>
        private static Function CreateSumFunction(Win64Container container, bool optimize = false)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var def = new FunctionDefinition("sum", Enumerable.Repeat(intType, 1).ToList(), intType);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));
            instructions.Add(new Instruction(OpCodes.LoadInt, 0));
            instructions.Add(new Instruction(OpCodes.BranchNotEqual, 5));
            instructions.Add(new Instruction(OpCodes.LoadInt, 0));
            instructions.Add(new Instruction(OpCodes.Ret));

            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));
            instructions.Add(new Instruction(OpCodes.LoadInt, 1));
            instructions.Add(new Instruction(OpCodes.SubInt));
            instructions.Add(new Instruction(OpCodes.Call, "sum", Enumerable.Repeat(intType, 1).ToList()));

            instructions.Add(new Instruction(OpCodes.LoadArgument, 0));
            instructions.Add(new Instruction(OpCodes.AddInt));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(def, instructions, new List<VMType>())
            {
                Optimize = optimize
            };
        }

        /// <summary>
        /// Creates the main function
        /// </summary>
        private static Function CreateMainFunction(Win64Container container, string toCall, int n, bool optimize = false)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var def = new FunctionDefinition("main", new List<VMType>(), intType);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.LoadInt, n));
            instructions.Add(new Instruction(OpCodes.Call, toCall, Enumerable.Repeat(intType, 1).ToList()));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(def, instructions, new List<VMType>())
            {
                Optimize = optimize
            };
        }

        static void Main(string[] args)
        {
            using (var container = new Win64Container())
            {
                //container.VirtualMachine.Settings["NumIntRegisters"] = 2;
                bool optimize = true;
                //var assembly = new Assembly(
                //    CreateAddFunction(container, 2, optimize),
                //    CreateLoopCallAdd(container, 30000000, optimize));

                var assembly = new Assembly(
                    CreateFibFunction(container, optimize),
                    CreateMainFunction(container, "fib", 35, optimize));

                //var assembly = new Assembly(
                //    CreateSumFunction(container, optimize),
                //    CreateMainFunction(container, "sum", 100, optimize));

                container.LoadAssembly(assembly);
                container.VirtualMachine.Compile();

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                int returnValue = container.VirtualMachine.GetEntryPoint()();
                var elapsed = stopwatch.Elapsed;

                //foreach (var function in assembly.Functions)
                //{
                //    var disassembler = new Disassembler(
                //        container.VirtualMachine.Compiler.GetCompilationData(function),
                //        x => new Compiler.Win64.Disassembler(x));
                //    Console.WriteLine(disassembler.Disassemble());
                //}

                Console.WriteLine(returnValue);
                Console.WriteLine(elapsed.TotalMilliseconds);
            }

            Console.ReadLine();
        }
    }
}
