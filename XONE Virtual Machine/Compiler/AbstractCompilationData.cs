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
    public abstract class AbstractCompilationData
    {
        /// <summary>
        /// The function
        /// </summary>
        public Function Function { get; }

        /// <summary>
        /// Mapping from instruction number to native instruction offset
        /// </summary>
        public IList<int> InstructionMapping { get; } = new List<int>();

        /// <summary>
        /// The unresolved function calls
        /// </summary>
        public IList<UnresolvedFunctionCall> UnresolvedFunctionCalls { get; } = new List<UnresolvedFunctionCall>();

        /// <summary>
        /// The unresolved branches
        /// </summary>
        public IDictionary<int, UnresolvedBranchTarget> UnresolvedBranches { get; } = new Dictionary<int, UnresolvedBranchTarget>();

        /// <summary>
        /// Creates new compilation data
        /// </summary>
        /// <param name="function">The function</param>
        public AbstractCompilationData(Function function)
        {
            this.Function = function;
        }
    }
}
