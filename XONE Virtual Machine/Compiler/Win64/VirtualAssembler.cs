using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Compiler.Analysis;

namespace XONEVirtualMachine.Compiler.Win64
{
    /// <summary>
    /// Handles how two memory operands are rewritten
    /// </summary>
    public enum MemoryRewrite
    {
        MemoryOnLeft,
        MemoryOnRight
    }

    /// <summary>
    /// Represents an assembler using virtual registers
    /// </summary>
    public class VirtualAssembler
    {
        private readonly CompilationData compilationData;
        private readonly bool needSpillRegister = false;

        private IntRegister[] intRegisters = new IntRegister[]
        {
            new IntRegister(Register.AX),
            new IntRegister(Register.CX),
            new IntRegister(Register.DX),
            new IntRegister(ExtendedRegister.R8),
            new IntRegister(ExtendedRegister.R9),
            new IntRegister(ExtendedRegister.R10),
            new IntRegister(ExtendedRegister.R11)
        };

        /// <summary>
        /// Creates a new virtual assembler
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        public VirtualAssembler(CompilationData compilationData)
        {
            this.compilationData = compilationData;
            this.needSpillRegister = 
                compilationData.RegisterAllocation.NumSpilledRegisters > 0 
                || compilationData.Function.Instructions.Any(x => x.OpCode == Core.OpCodes.DivInt);
        }

        /// <summary>
        /// Determines if the spill register is needed
        /// </summary>
        public bool NeedSpillRegister
        {
            get { return this.needSpillRegister; }
        }

        /// <summary>
        /// Returns the spill register
        /// </summary>
        public IntRegister GetSpillRegister()
        {
            return new IntRegister(ExtendedRegister.R12);
        }

        /// <summary>
        /// Returns the register for the given allocated register
        /// </summary>
        /// <param name="register">The register</param>
        public IntRegister GetRegister(int register)
        {
            if (register >= 0 && register < this.intRegisters.Length)
            {
                return this.intRegisters[register];
            }

            throw new InvalidOperationException("The given register is not valid.");
        }

        /// <summary>
        /// Returns the hardware register for the given virtual register
        /// </summary>
        /// <param name="virtualRegister">The virtual register</param>
        /// <returns>The register or null if the register is spilled</returns>
        public IntRegister? GetRegisterForVirtual(VirtualRegister virtualRegister)
        {
            var reg = this.compilationData.RegisterAllocation.GetRegister(virtualRegister);

            if (reg.HasValue)
            {
                return this.GetRegister(reg.Value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the instructions that are alive at the given instruction
        /// </summary>
        /// <param name="instructionIndex">The index of the instruction</param>
        public IEnumerable<IntRegister> GetAliveRegisters(int instructionIndex)
        {
            return this.compilationData.RegisterAllocation
                .GetAllocatedRegisters()
                .Where(interval =>
                {
                    return instructionIndex >= interval.Start && instructionIndex <= interval.End;
                })
                .Select(interval => this.GetRegisterForVirtual(interval.VirtualRegister).Value);
        }

        /// <summary>
        /// Rewrites two memory operand instructions
        /// </summary>
        /// <param name="memoryRewrite">The memory rewrite rule</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        private void RewriteMemory(MemoryRewrite memoryRewrite, MemoryOperand destination, MemoryOperand source,
            Action<IList<byte>, IntRegister, MemoryOperand> inst1,
            Action<IList<byte>, MemoryOperand, IntRegister> inst2)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var spillReg = this.GetSpillRegister();

            if (memoryRewrite == MemoryRewrite.MemoryOnLeft)
            {
                Assembler.Move(generatedCode, spillReg, source);
                inst2(generatedCode, destination, spillReg);
            }
            else
            {
                Assembler.Move(generatedCode, spillReg, destination);
                inst1(generatedCode, spillReg, source);
                Assembler.Move(generatedCode, destination, spillReg);
            }
        }

        /// <summary>
        /// Calculates the stack offset for the given virtual register
        /// </summary>
        /// <param name="stackIndex">The stack index</param>
        public int CalculateStackOffset(int stackIndex)
        {
            return -RawAssembler.RegisterSize * (1 + this.compilationData.Function.Definition.Parameters.Count + stackIndex);
        }


        /// <summary>
        /// Generates code for an instruction with a virtual register source
        /// </summary>
        /// <param name="sourceRegister">The source register</param>
        public void GenerateOneRegisterInstruction(VirtualRegister sourceRegister,
            Action<IList<byte>, IntRegister> inst1,
            Action<IList<byte>, MemoryOperand> inst2)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;
            int? opStack = compilationData.RegisterAllocation.GetStackIndex(sourceRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetRegister(regAlloc.GetRegister(sourceRegister) ?? 0);
                inst1(generatedCode, opReg);
            }
            else
            {
                var opStackOffset = CalculateStackOffset(opStack.Value);
                inst2(generatedCode, new MemoryOperand(Register.BP, opStackOffset));
            }
        }

        /// <summary>
        /// Generates code for an instruction with two virtual registers
        /// </summary>
        /// <param name="destinationRegister">The destination register</param>
        /// <param name="sourceRegister">The source register</param>
        /// <param name="memoryRewrite">Determines how an instruction with two memory operands will be rewritten into one memory operand.</param>
        public void GenerateTwoRegistersInstruction(VirtualRegister destinationRegister, VirtualRegister sourceRegister,
            Action<IList<byte>, IntRegister, IntRegister> inst1,
            Action<IList<byte>, IntRegister, MemoryOperand> inst2,
            Action<IList<byte>, MemoryOperand, IntRegister> inst3,
            MemoryRewrite memoryRewrite = MemoryRewrite.MemoryOnLeft)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;

            int? op1Stack = compilationData.RegisterAllocation.GetStackIndex(destinationRegister);
            int? op2Stack = compilationData.RegisterAllocation.GetStackIndex(sourceRegister);

            if (!op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1Reg = GetRegister(regAlloc.GetRegister(destinationRegister) ?? 0);
                var op2Reg = GetRegister(regAlloc.GetRegister(sourceRegister) ?? 0);
                inst1(generatedCode, op1Reg, op2Reg);
            }
            else if (!op1Stack.HasValue && op2Stack.HasValue)
            {
                var op1Reg = this.GetRegister(regAlloc.GetRegister(destinationRegister) ?? 0);
                var op2StackOffset = CalculateStackOffset(op2Stack.Value);
                inst2(generatedCode, op1Reg, new MemoryOperand(Register.BP, op2StackOffset));
            }
            else if (op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1StackOffset = CalculateStackOffset(op1Stack.Value);
                var op2Reg = this.GetRegister(regAlloc.GetRegister(sourceRegister) ?? 0);
                inst3(generatedCode, new MemoryOperand(Register.BP, op1StackOffset), op2Reg);
            }
            else
            {
                var op1StackOffset = CalculateStackOffset(op1Stack.Value);
                var op2StackOffset = CalculateStackOffset(op2Stack.Value);

                this.RewriteMemory(
                    memoryRewrite,
                    new MemoryOperand(Register.BP, op1StackOffset),
                    new MemoryOperand(Register.BP, op2StackOffset),
                    inst2,
                    inst3);
            }
        }

        /// <summary>
        /// Generates code for an instruction with a fixed register destination and virtual register source
        /// </summary>
        /// <param name="destination">The destination</param>
        /// <param name="sourceRegister">The source register</param>
        /// <param name="skipIfSame">Indicates if the instruction will be skipped of destination == source.</param>
        public void GenerateTwoRegisterFixedDestinationInstruction(IntRegister destination, VirtualRegister sourceRegister,
            Action<IList<byte>, IntRegister, IntRegister> inst1,
            Action<IList<byte>, IntRegister, MemoryOperand> inst2,
            bool skipIfSame = false)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;
            int? opStack = compilationData.RegisterAllocation.GetStackIndex(sourceRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetRegister(regAlloc.GetRegister(sourceRegister) ?? 0);

                if (skipIfSame)
                {
                    if (destination != opReg)
                    {
                        inst1(generatedCode, destination, opReg);
                    }
                }
                else
                {
                    inst1(generatedCode, destination, opReg);
                }
            }
            else
            {
                var opStackOffset = CalculateStackOffset(opStack.Value);
                inst2(generatedCode, destination, new MemoryOperand(Register.BP, opStackOffset));
            }
        }

        /// <summary>
        /// Generates code for an instruction with a virtual register destination and fixed register source
        /// </summary>
        /// <param name="destinationRegister">The destination register</param>
        /// <param name="source">The source</param>
        /// <param name="skipIfSame">Indicates if the instruction will be skipped of destination == source.</param>
        public void GenerateTwoRegisterFixedSourceInstruction(VirtualRegister destinationRegister, IntRegister source,
            Action<IList<byte>, IntRegister, IntRegister> inst1,
            Action<IList<byte>, MemoryOperand, IntRegister> inst2,
            bool skipIfSame = false)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;
            int? opStack = compilationData.RegisterAllocation.GetStackIndex(destinationRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetRegister(regAlloc.GetRegister(destinationRegister) ?? 0);
                
                if (skipIfSame)
                {
                    if (opReg != source)
                    {
                        inst1(generatedCode, opReg, source);
                    }
                }
                else
                {
                    inst1(generatedCode, opReg, source);
                }
            }
            else
            {
                var opStackOffset = CalculateStackOffset(opStack.Value);
                inst2(generatedCode, new MemoryOperand(Register.BP, opStackOffset), source);
            }
        }

        /// <summary>
        /// Generates code for an instruction with a memory destination and virtual register source
        /// </summary>
        /// <param name="destination">The destination</param>
        /// <param name="sourceRegister">The source register</param>
        /// <param name="memoryRewrite">Determines how an instruction with two memory operands will be rewritten into one memory operand.</param>
        public void GenerateOneRegisterMemoryDestinationInstruction(MemoryOperand destination, VirtualRegister sourceRegister,
            Action<IList<byte>, IntRegister, MemoryOperand> inst1,
            Action<IList<byte>, MemoryOperand, IntRegister> inst2,
            MemoryRewrite memoryRewrite = MemoryRewrite.MemoryOnLeft)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;
            int? opStack = compilationData.RegisterAllocation.GetStackIndex(sourceRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetRegister(regAlloc.GetRegister(sourceRegister) ?? 0);
                inst2(generatedCode, destination, opReg);
            }
            else
            {
                var opStackOffset = CalculateStackOffset(opStack.Value);
                RewriteMemory(memoryRewrite, destination, new MemoryOperand(Register.BP, opStackOffset), inst1, inst2);
            }
        }

        /// <summary>
        /// Generates code for an instruction with virtual register destination and memory source
        /// </summary>
        /// <param name="destinationRegister">The destination</param>
        /// <param name="source">The source</param>
        /// <param name="memoryRewrite">Determines how an instruction with two memory operands will be rewritten into one memory operand.</param>
        public void GenerateOneRegisterMemorySourceInstruction(VirtualRegister destinationRegister, MemoryOperand source,
            Action<IList<byte>, IntRegister, MemoryOperand> inst1,
            Action<IList<byte>, MemoryOperand, IntRegister> inst2,
            MemoryRewrite memoryRewrite = MemoryRewrite.MemoryOnLeft)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;
            int? opStack = compilationData.RegisterAllocation.GetStackIndex(destinationRegister);

            if (!opStack.HasValue)
            {
                var destinationReg = this.GetRegister(regAlloc.GetRegister(destinationRegister) ?? 0);
                inst1(generatedCode, destinationReg, source);
            }
            else
            {
                var opStackOffset = CalculateStackOffset(opStack.Value);
                RewriteMemory(memoryRewrite, new MemoryOperand(Register.BP, opStackOffset), source, inst1, inst2);
            }
        }

        /// <summary>
        /// Generates code for an one virtual register operand instruction with an int value
        /// </summary>
        /// <param name="destinationRegister">The destination register</param>
        /// <param name="value">The value</param>
        public void GenerateOneRegisterWithValueInstruction(VirtualRegister destinationRegister, int value,
            Action<IList<byte>, IntRegister, int> inst1,
            Action<IList<byte>, MemoryOperand, int> inst2)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;

            var opStack = regAlloc.GetStackIndex(destinationRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetRegister(regAlloc.GetRegister(destinationRegister) ?? 0);
                inst1(generatedCode, opReg, value);
            }
            else
            {
                var stackOp = new MemoryOperand(
                    Register.BP,
                    CalculateStackOffset(opStack.Value));
                inst2(generatedCode, stackOp, value);
            }
        }
    }
}
