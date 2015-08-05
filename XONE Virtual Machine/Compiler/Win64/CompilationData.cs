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
    public class CompilationData : AbstractCompilationData
    {
        /// <summary>
        /// The size of the stack
        /// </summary>
        public int StackSize { get; set; }

        /// <summary>
        /// The operand stack
        /// </summary>
        /// <remarks>Only has value if the function is not optimized.</remarks>
        public OperandStack OperandStack { get; }

        /// <summary>
        /// The virtual instructions
        /// </summary>
        /// <remarks>Only has value if the function is optimized.</remarks>
        public IReadOnlyList<VirtualInstruction> VirtualInstructions { get; }

        /// <summary>
        /// The virtual registers for locals
        /// </summary>
        /// <remarks>Only has value if the function is optimized.</remarks>
        public IReadOnlyList<VirtualRegister> LocalVirtualRegisters { get; }

        /// <summary>
        /// The register allocation
        /// </summary>
        /// <remarks>Only has value if the function is optimized.</remarks>
        public RegisterAllocation RegisterAllocation { get; }

        /// <summary>
        /// The virtual assembler
        /// </summary>
        /// <remarks>Only has value if the function is optimized.</remarks>
        public VirtualAssembler VirtualAssembler { get; }

        /// <summary>
        /// Creates new compilation data
        /// </summary>
        /// <param name="virtualMachine">The virtual macine</param>
        /// <param name="function">The function</param>
        public CompilationData(VirtualMachine virtualMachine, Function function)
            : base(function)
        {
            this.OperandStack = new OperandStack(function);

            if (function.Optimize)
            {
                IList<VirtualRegister> localRegs;
                this.VirtualInstructions = new ReadOnlyCollection<VirtualInstruction>(
                    VirtualRegisterIR.Create(virtualMachine, function, out localRegs));

                this.LocalVirtualRegisters = new ReadOnlyCollection<VirtualRegister>(localRegs);

                var numIntRegs = virtualMachine.Settings.GetSetting<int>("NumIntRegisters");
                var numFloatRegs = virtualMachine.Settings.GetSetting<int>("NumFloatRegisters");

                this.RegisterAllocation = LinearScanRegisterAllocation.Allocate(
                    LivenessAnalysis.ComputeLiveness(VirtualControlFlowGraph.FromBasicBlocks(
                        VirtualBasicBlock.CreateBasicBlocks(this.VirtualInstructions))), numIntRegs, numFloatRegs);

                this.VirtualAssembler = new VirtualAssembler(this);
            }
        }
    }
}
