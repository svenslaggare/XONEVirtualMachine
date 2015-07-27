using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine.Compiler.Win64
{
	/// <summary>
	/// Represents a JIT compiler
	/// </summary>
	public class JITCompiler : IJITCompiler
	{
        private readonly VirtualMachine virtualMachine;
        private readonly CodeGenerator codeGen;
        private readonly MemoryManager memoryManager = new MemoryManager();
        private readonly IList<CompilationData> compiledFunctions = new List<CompilationData>();

		/// <summary>
		/// Creates a new compiler
		/// </summary>
        /// <param name="virtualMachine">The virtual machine</param>
		public JITCompiler(VirtualMachine virtualMachine)
		{
            this.virtualMachine = virtualMachine;
            this.codeGen = new CodeGenerator(virtualMachine);
        }

		/// <summary>
		/// Compiles the given function
		/// </summary>
		/// <param name="function">The function to compile</param>
		/// <returns>A pointer to the start of the compiled function</returns>
		public IntPtr Compile(Function function)
		{
            //Compile the function
            var compilationData = new CompilationData(function, new OperandStack(function));
            this.compiledFunctions.Add(compilationData);
			this.codeGen.CompileFunction(compilationData);
    
            //Allocate native memory. The instructions will be copied later when all symbols has been resolved.
            var memory = this.memoryManager.Allocate(function.GeneratedCode.Count);
            function.Definition.SetEntryPoint(memory);

            return memory;
        }

        /// <summary>
        /// Resolves the call target for the given function
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        private void ResolveCallTargets(CompilationData compilationData)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            long entryPoint = compilationData.Function.Definition.EntryPoint.ToInt64();

            foreach (var unresolvedCall in compilationData.UnresolvedFunctionCalls)
            {
                long toCallAddress = unresolvedCall.Function.EntryPoint.ToInt64();

                //Update the call target
                if (unresolvedCall.AddressMode == FunctionCallAddressModes.Absolute)
                {
                    NativeHelpers.SetLong(generatedCode, unresolvedCall.CallSiteOffset + 2, toCallAddress);
                }
                else
                {
                    int target = (int)(toCallAddress - (entryPoint + unresolvedCall.CallSiteOffset + 5));
                    NativeHelpers.SetInt(generatedCode, unresolvedCall.CallSiteOffset + 1, target);
                }
            }            

            compilationData.UnresolvedFunctionCalls.Clear();
        }

        /// <summary>
        /// Resolve the symbols for functions
        /// </summary>
        private void ResolveSymbols()
        {
            foreach (var function in this.compiledFunctions)
            {
                this.ResolveCallTargets(function);
                NativeHelpers.CopyTo(
                    function.Function.Definition.EntryPoint,
                    function.Function.GeneratedCode);
            }
        }

        /// <summary>
        /// Makes the compiled functions executable
        /// </summary>
        public void MakeExecutable()
        {
            this.ResolveSymbols();
            this.memoryManager.MakeExecutable();
        }

		/// <summary>
		/// Disposes resources
		/// </summary>
		public void Dispose()
		{
            this.memoryManager.Dispose();
		}
	}
}
