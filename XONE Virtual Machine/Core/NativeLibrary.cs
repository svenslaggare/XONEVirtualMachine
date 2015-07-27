using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Core
{
    /// <summary>
    /// Defines the functions that are not defined in managed code
    /// </summary>
    public static class NativeLibrary
    {
        /// <summary>
        /// Delegate for a '(Int) Void' function.
        /// </summary>
        delegate void FuncVoidArgInt(int x);

        /// <summary>
        /// Prints the given value
        /// </summary>
        /// <param name="value">The value</param>
        private static void Println(int value)
        {
            Console.WriteLine(value);
        }

        delegate int FuncIntArgIntIntIntInt(int x1, int x2, int x3, int x4);
            
        private static int AddInt(int x1, int x2, int x3, int x4)
        {
            return x1 + x2 + x3 + x4;
        }

        /// <summary>
        /// Adds the native library to the given VM
        /// </summary>
        /// <param name="virtualMachine">The virtual machine</param>
        public static void Add(VirtualMachine virtualMachine)
        {
            var intType = virtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
            var voidType = virtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Void);

            virtualMachine.Binder.Define(FunctionDefinition.NewExternal<FuncVoidArgInt>(
              "std.println",
              new List<VMType>() { intType },
              voidType,
              Println));

            virtualMachine.Binder.Define(FunctionDefinition.NewExternal<FuncIntArgIntIntIntInt>(
              "std.add",
              Enumerable.Repeat(intType, 4).ToList(),
              intType,
              AddInt));
        }
    }
}
