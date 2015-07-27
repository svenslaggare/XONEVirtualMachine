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
            var compilationData = new CompilationData(function);
            this.compiledFunctions.Add(compilationData);
			this.codeGen.CompileFunction(compilationData);
    
            //Allocate native memory and copy the generated code
            var memory = this.memoryManager.Allocate(function.GeneratedCode.Count);
			NativeHelpers.CopyTo(memory, function.GeneratedCode);
            function.Definition.SetEntryPoint(memory);

            return memory;
        }

        /// <summary>
        /// Resolves the call target for the given function
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        private void ResolveCallTargets(CompilationData compilationData)
        {
            //Get a pointer to the functions native instructions
            var funcCodePtr = compilationData.Function.Definition.EntryPoint;
            var generatedCode = compilationData.Function.GeneratedCode;

            foreach (var unresolvedCall in compilationData.UnresolvedFunctionCalls)
            {
                long toCallAddress = unresolvedCall.Function.EntryPoint.ToInt64();

                //Update the call target
                if (unresolvedCall.AddressMode == FunctionCallAddressModes.Absolute)
                {
                    NativeHelpers.SetLong(generatedCode, unresolvedCall.CallSiteOffset + 2, toCallAddress);
                    NativeHelpers.SetLong(funcCodePtr, unresolvedCall.CallSiteOffset + 2, toCallAddress);
                }
                else
                {
                    int target = (int)(toCallAddress - (funcCodePtr.ToInt64() + unresolvedCall.CallSiteOffset + 5));
                    NativeHelpers.SetInt(generatedCode, unresolvedCall.CallSiteOffset + 1, target);
                    NativeHelpers.SetInt(funcCodePtr, unresolvedCall.CallSiteOffset + 1, target);
                }
            }

            var code = string.Join(", ", generatedCode);

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
