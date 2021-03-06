﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Compiler.Win64;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine
{
    /// <summary>
    /// Represents container for a Windows x64 VM
    /// </summary>
    public class Win64Container : IDisposable
    {
        /// <summary>
        /// Returns the virtual machine
        /// </summary>
        public VirtualMachine VirtualMachine { get; }

        /// <summary>
        /// Creates a new Windows x64 container
        /// </summary>
        public Win64Container()
        {
            this.VirtualMachine = new VirtualMachine(vm => new JITCompiler(vm));
            NativeLibrary.Add(this.VirtualMachine);
        }

        /// <summary>
        /// Loads the given assembly
        /// </summary>
        /// <param name="assembly">The assembly</param>
        public void LoadAssembly(Assembly assembly)
        {
            this.VirtualMachine.LoadAssembly(assembly);
        }

        /// <summary>
        /// Executes the loaded program
        /// </summary>
        /// <returns>The return value from the program</returns>
        public int Execute()
        {
            this.VirtualMachine.Compile();
            var funcPtr = this.VirtualMachine.GetEntryPoint();
            return funcPtr();
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            this.VirtualMachine.Dispose();
        }
    }
}
