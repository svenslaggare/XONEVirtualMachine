using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine.Compiler
{
    /// <summary>
    /// Holds compilation data
    /// </summary>
    public class CompilationData
    {
        /// <summary>
        /// The function
        /// </summary>
        public Function Function { get; }

        /// <summary>
        /// The operand stack
        /// </summary>
        public IOperandStack OperandStack { get; }

        /// <summary>
        /// The unresolved function calls
        /// </summary>
        public IList<UnresolvedFunctionCall> UnresolvedFunctionCalls { get; } = new List<UnresolvedFunctionCall>();

        /// <summary>
        /// Creates new compilation data
        /// </summary>
        /// <param name="function">The function</param>
        /// <param name="operandStack">The operand stack</param>
        public CompilationData(Function function, IOperandStack operandStack)
        {
            this.Function = function;
            this.OperandStack = operandStack;
        }
    }
}
