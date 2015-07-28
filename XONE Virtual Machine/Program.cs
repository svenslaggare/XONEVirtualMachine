using System;
using System.Collections.Generic;
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
        /// Creates the test function
        /// </summary>
        private static Function CreateTestFunction(Win64Container container)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
            var def = new FunctionDefinition("test", new List<VMType>(), intType);

            var instructions = new List<Instruction>();

            instructions.Add(new Instruction(OpCodes.LoadInt, 1));
            instructions.Add(new Instruction(OpCodes.LoadInt, 2));
            instructions.Add(new Instruction(OpCodes.Pop));
            instructions.Add(new Instruction(OpCodes.Ret));

            var func = new Function(def, instructions, new List<VMType>());
            container.VirtualMachine.LoadAssembly(Assembly.SingleFunction(func));
            return func;
        }

        /// <summary>
        /// Creates the main function
        /// </summary>
        private static Function CreateMainFunction(Win64Container container)
        {
            var intType = container.VirtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
            var def = new FunctionDefinition("main", new List<VMType>(), intType);

            var instructions = new List<Instruction>();
            instructions.Add(new Instruction(OpCodes.Call, "test", new List<VMType>()));
            instructions.Add(new Instruction(OpCodes.Ret));

            var func = new Function(def, instructions, new List<VMType>());
            container.VirtualMachine.LoadAssembly(Assembly.SingleFunction(func));
            return func;
        }

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

        static void Main(string[] args)
        {
            using (var container = new Win64Container())
            {
                var assembly = new Assembly(new List<Function>()
                {
                    CreateAddFunction(container, 4),
                    CreateMainFunction(container, 4)
                });
                //CreateTestFunction(container);
                //CreateMainFunction(container);

                container.LoadAssembly(assembly);
                Console.WriteLine(container.Execute());
            }

            Console.ReadLine();
        }
    }
}
