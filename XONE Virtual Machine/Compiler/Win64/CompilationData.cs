using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Compiler.Analysis;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine.Compiler.Win64
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
        public OperandStack OperandStack { get; }

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
        /// The virtual instructions
        /// </summary>
        /// <remarks>Only has value if the function is optimized.</remarks>
        public IReadOnlyList<VirtualInstruction> VirtualInstructions { get; }

        /// <summary>
        /// The register allocation
        /// </summary>
        /// <remarks>Only has value if the function is optimized.</remarks>
        public RegisterAllocation RegisterAllocation { get; }

        /// <summary>
        /// Creates new compilation data
        /// </summary>
        /// <param name="function">The function</param>
        public CompilationData(Function function)
        {
            this.Function = function;
            this.OperandStack = new OperandStack(function);

            if (function.Optimize)
            {
                this.VirtualInstructions = new ReadOnlyCollection<VirtualInstruction>(VirtualRegisters.Create(function.Instructions));
                this.RegisterAllocation = LinearScanRegisterAllocation.Allocate(
                    LivenessAnalysis.ComputeLiveness(VirtualControlFlowGraph.FromBasicBlocks(
                        VirtualBasicBlock.CreateBasicBlocks(this.VirtualInstructions))));
            }
        }
    }
}
