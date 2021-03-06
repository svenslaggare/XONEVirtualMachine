﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine.Compiler
{
    /// <summary>
    /// Represents an entry point
    /// </summary>
    public delegate int EntryPoint();

    /// <summary>
    /// Represents an interface for a JIT compiler
    /// </summary>
    public interface IJITCompiler : IDisposable
    {
        /// <summary>
        /// Returns the memory manager
        /// </summary>
        MemoryManager MemoryManager { get; }

        /// <summary>
        /// Returns the compilation data for the given function
        /// </summary>
        /// <param name="function">The function</param>
        /// <returns>The data or null if not compiled</returns>
        AbstractCompilationData GetCompilationData(Function function);

        /// <summary>
        /// Compiles the given function
        /// </summary>
        /// <param name="function">The function to compile</param>
        /// <returns>A pointer to the start of the compiled function</returns>
        IntPtr Compile(Function function);

        /// <summary>
        /// Makes the compiled functions executable
        /// </summary>
        void MakeExecutable();
    }
}
