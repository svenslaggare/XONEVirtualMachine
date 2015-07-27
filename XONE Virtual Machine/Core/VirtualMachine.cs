﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Compiler;

namespace XONEVirtualMachine.Core
{
    /// <summary>
    /// Defines the virtual machine
    /// </summary>
    public class VirtualMachine : IDisposable
    {
        /// <summary>
        /// Returns the binder
        /// </summary>
        public Binder Binder { get; } = new Binder();

        /// <summary>
        /// Creates a new type provider
        /// </summary>
        public TypeProvider TypeProvider { get; } = new TypeProvider();

        /// <summary>
        /// Returns the compiler
        /// </summary>
        public IJITCompiler Compiler { get; }

        /// <summary>
        /// Creates a new virtual machine
        /// </summary>
        /// <param name="createCompiler">A function to create the compiler</param>
        public VirtualMachine(Func<VirtualMachine, IJITCompiler> createCompiler)
        {
            this.Compiler = createCompiler(this);
        }

        /// <summary>
        /// Returns the entry point
        /// </summary>
        public EntryPoint GetEntryPoint()
        {
            var entryPoint = this.Binder.GetFunction("main()");

            if (entryPoint == null)
            {
                throw new InvalidOperationException("There is no entry point defined.");
            }

            return (EntryPoint)Marshal.GetDelegateForFunctionPointer(
                entryPoint.EntryPoint,
                typeof(EntryPoint));
        }

        /// <summary>
        /// Loads the given function
        /// </summary>
        /// <param name="function">The function to load</param>
        public void LoadFunction(Function function)
        {
            this.Compiler.Compile(function);
            this.Binder.Define(function.Definition);
        }

        /// <summary>
        /// Compiles loaded functions
        /// </summary>
        public void Compile()
        {
            this.Compiler.MakeExecutable();
        }

        /// <summary>
        /// Disposes the resources
        /// </summary>
        public void Dispose()
        {
            this.Compiler.Dispose();
        }
    }
}
