﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine
{
	class Program
	{
        /// <summary>
        /// Creates a main function that calls the add function with the given number of arguments
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="numArgs">The number of arguments</param>
        private static Function CreateMainFunction(Win64Container container, int numArgs)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
            var def = new FunctionDefinition("main", new List<VMType>(), intType);

            var instructions = new List<Instruction>();

            for (int i = 1; i <= numArgs; i++)
            {
                instructions.Add(new Instruction(OpCodes.LoadInt, i));
            }

            instructions.Add(new Instruction(OpCodes.Call, "add", Enumerable.Repeat(intType, numArgs).ToList()));
            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(def, instructions, new List<VMType>());
        }

        /// <summary>
        /// Creates an add function with that takes the given amount of arguments
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="numArgs">The number of arguments</param>
        private static Function CreateAddFunction(Win64Container container, int numArgs)
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

            return new Function(def, instructions, new List<VMType>());
        }

        /// <summary>
        /// Creates a sum function
        /// </summary>
        private static Function CreateSumFunction(Win64Container container, int count, int loopCount, bool optimize = false)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var def = new FunctionDefinition("main", new List<VMType>() { }, intType);

            var instructions = new List<Instruction>();

            instructions.Add(new Instruction(OpCodes.LoadInt, loopCount));
            instructions.Add(new Instruction(OpCodes.StoreLocal, 0));

            instructions.Add(new Instruction(OpCodes.LoadInt, 1));

            for (int i = 1; i < count; i++)
            {
                instructions.Add(new Instruction(OpCodes.LoadInt, i + 1));
                instructions.Add(new Instruction(OpCodes.AddInt));
            }

            instructions.Add(new Instruction(OpCodes.LoadLocal, 1));
            instructions.Add(new Instruction(OpCodes.AddInt));
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
        /// Creates a sum function
        /// </summary>
        private static Function CreateSumFunction2(Win64Container container, int count, bool optimize = false)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var def = new FunctionDefinition("main", new List<VMType>() { }, intType);

            var instructions = new List<Instruction>();

            for (int i = 1; i <= count; i++)
            {
                instructions.Add(new Instruction(OpCodes.LoadInt, i));
            }

            for (int i = 0; i < count - 1; i++)
            {
                instructions.Add(new Instruction(OpCodes.AddInt));
            }

            instructions.Add(new Instruction(OpCodes.Ret));

            return new Function(def, instructions, new List<VMType>() { intType, intType })
            {
                Optimize = optimize
            };
        }

        /// <summary>
        /// Creates a product function
        /// </summary>
        private static Function CreateProductFunction(Win64Container container, int count, bool optimize = false)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);

            var def = new FunctionDefinition("main", new List<VMType>(), intType);

            var instructions = new List<Instruction>();

            for (int i = 1; i <= count; i++)
            {
                instructions.Add(new Instruction(OpCodes.LoadInt, i));
            }

            for (int i = 0; i < count - 1; i++)
            {
                instructions.Add(new Instruction(OpCodes.MulInt));
            }

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
                bool optimize = true;
                //var assembly = Assembly.SingleFunction(CreateSumFunction(container, 100, 1000000, optimize));
                //var assembly = Assembly.SingleFunction(CreateSumFunction2(container, 10, optimize));
                var assembly = Assembly.SingleFunction(CreateProductFunction(container, 10, optimize));

                container.LoadAssembly(assembly);
                container.VirtualMachine.Compile();

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                int returnValue = container.VirtualMachine.GetEntryPoint()();
                var elapsed = stopwatch.Elapsed;

                Console.WriteLine(returnValue);
                Console.WriteLine(elapsed.TotalMilliseconds);
            }

            Console.ReadLine();
        }
    }
}
