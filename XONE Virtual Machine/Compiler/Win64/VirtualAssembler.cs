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

        private FloatRegister[] floatRegisters = new FloatRegister[]
        {
            FloatRegister.XMM0,
            FloatRegister.XMM1,
            FloatRegister.XMM2,
            FloatRegister.XMM3,
            FloatRegister.XMM4,
            FloatRegister.XMM5,
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
        /// Returns the int spill register
        /// </summary>
        public IntRegister GetIntSpillRegister()
        {
            return new IntRegister(ExtendedRegister.R12);
        }

        /// <summary>
        /// Returns the float spill register
        /// </summary>
        public FloatRegister GetFloatSpillRegister()
        {
            return FloatRegister.XMM5;
        }

        /// <summary>
        /// Returns the int register for the given allocated register
        /// </summary>
        /// <param name="register">The register</param>
        public IntRegister GetIntRegister(int register)
        {
            if (register >= 0 && register < this.intRegisters.Length)
            {
                return this.intRegisters[register];
            }

            throw new InvalidOperationException("The given register is not valid.");
        }

        /// <summary>
        /// Returns the float register for the given allocated register
        /// </summary>
        /// <param name="register">The register</param>
        public FloatRegister GetFloatRegister(int register)
        {
            if (register >= 0 && register < this.floatRegisters.Length)
            {
                return this.floatRegisters[register];
            }

            throw new InvalidOperationException("The given register is not valid.");
        }

        /// <summary>
        /// Returns the int register for the given virtual register
        /// </summary>
        /// <param name="virtualRegister">The virtual register</param>
        /// <returns>The register or null if the register is spilled</returns>
        public IntRegister? GetIntRegisterForVirtual(VirtualRegister virtualRegister)
        {
            if (virtualRegister.Type != VirtualRegisterType.Integer)
            {
                return null;
            }

            var reg = this.compilationData.RegisterAllocation.GetRegister(virtualRegister);

            if (reg.HasValue)
            {
                return this.GetIntRegister(reg.Value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the float register for the given virtual register
        /// </summary>
        /// <param name="virtualRegister">The virtual register</param>
        /// <returns>The register or null if the register is spilled</returns>
        public FloatRegister? GetFloatRegisterForVirtual(VirtualRegister virtualRegister)
        {
            if (virtualRegister.Type != VirtualRegisterType.Float)
            {
                return null;
            }

            var reg = this.compilationData.RegisterAllocation.GetRegister(virtualRegister);

            if (reg.HasValue)
            {
                return this.GetFloatRegister(reg.Value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the register for the given virtual register
        /// </summary>
        /// <param name="virtualRegister">The virtual register</param>
        /// <returns>The register or null if the register is spilled</returns>
        public HardwareRegister? GetRegisterForVirtual(VirtualRegister virtualRegister)
        {
            if (virtualRegister.Type == VirtualRegisterType.Float)
            {
                return this.GetFloatRegisterForVirtual(virtualRegister);
            }
            else
            {
                return this.GetIntRegisterForVirtual(virtualRegister);
            }
        }

        /// <summary>
        /// Returns the instructions that are alive at the given instruction
        /// </summary>
        /// <param name="instructionIndex">The index of the instruction</param>
        public IEnumerable<HardwareRegister> GetAliveRegisters(int instructionIndex)
        {
            return this.compilationData.RegisterAllocation
                .GetAllocatedRegisters()
                .Where(interval =>
                {
                    return instructionIndex >= interval.Start && instructionIndex <= interval.End;
                })
                .Select<LiveInterval, HardwareRegister>(interval =>
                {
                    if (interval.VirtualRegister.Type == VirtualRegisterType.Integer)
                    {
                        return this.GetIntRegisterForVirtual(interval.VirtualRegister).Value;
                    }
                    else
                    {
                        return this.GetFloatRegisterForVirtual(interval.VirtualRegister).Value;
                    }
                });
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
            var spillReg = this.GetIntSpillRegister();

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
        /// Rewrites two memory operand instructions
        /// </summary>
        /// <param name="memoryRewrite">The memory rewrite rule</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        private void RewriteMemory(MemoryRewrite memoryRewrite, MemoryOperand destination, MemoryOperand source,
            Action<IList<byte>, FloatRegister, MemoryOperand> inst1,
            Action<IList<byte>, MemoryOperand, FloatRegister> inst2)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var spillReg = this.GetFloatSpillRegister();

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
        /// Calculates the stack offset for the given virtual register
        /// </summary>
        /// <param name="virtualRegister">The virtual register</param>
        public int? CalculateStackOffset(VirtualRegister virtualRegister)
        {
            int? stackIndex = this.compilationData.RegisterAllocation.GetStackIndex(virtualRegister);

            if (stackIndex.HasValue)
            {
                return this.CalculateStackOffset(stackIndex.Value);
            }
            else
            {
                return null;
            }
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
                var opReg = this.GetIntRegisterForVirtual(sourceRegister).Value;
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
                var op1Reg = this.GetIntRegisterForVirtual(destinationRegister).Value;
                var op2Reg = this.GetIntRegisterForVirtual(sourceRegister).Value;
                inst1(generatedCode, op1Reg, op2Reg);
            }
            else if (!op1Stack.HasValue && op2Stack.HasValue)
            {
                var op1Reg = this.GetIntRegisterForVirtual(destinationRegister).Value;
                var op2StackOffset = CalculateStackOffset(op2Stack.Value);
                inst2(generatedCode, op1Reg, new MemoryOperand(Register.BP, op2StackOffset));
            }
            else if (op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1StackOffset = CalculateStackOffset(op1Stack.Value);
                var op2Reg = this.GetIntRegisterForVirtual(sourceRegister).Value;
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
        /// Generates code for a float instruction with two virtual registers
        /// </summary>
        /// <param name="destinationRegister">The destination register</param>
        /// <param name="sourceRegister">The source register</param>
        public void GenerateTwoRegistersFloatInstruction(VirtualRegister destinationRegister, VirtualRegister sourceRegister,
            Action<IList<byte>, FloatRegister, FloatRegister> inst1,
            Action<IList<byte>, FloatRegister, MemoryOperand> inst2)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;

            int? op1Stack = compilationData.RegisterAllocation.GetStackIndex(destinationRegister);
            int? op2Stack = compilationData.RegisterAllocation.GetStackIndex(sourceRegister);

            if (!op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1Reg = this.GetFloatRegisterForVirtual(destinationRegister).Value;
                var op2Reg = this.GetFloatRegisterForVirtual(sourceRegister).Value;
                inst1(generatedCode, op1Reg, op2Reg);
            }
            else if (!op1Stack.HasValue && op2Stack.HasValue)
            {
                var op1Reg = this.GetFloatRegisterForVirtual(destinationRegister).Value;
                var op2StackOffset = CalculateStackOffset(op2Stack.Value);
                inst2(generatedCode, op1Reg, new MemoryOperand(Register.BP, op2StackOffset));
            }
            else if (op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1StackOffset = CalculateStackOffset(op1Stack.Value);
                var op2Reg = this.GetFloatRegisterForVirtual(sourceRegister).Value;
                var spillReg = this.GetFloatSpillRegister();

                Assembler.Move(generatedCode, spillReg, new MemoryOperand(Register.BP, op1StackOffset));
                inst1(generatedCode, spillReg, op2Reg);
                Assembler.Move(generatedCode, new MemoryOperand(Register.BP, op1StackOffset), spillReg);
            }
            else
            {
                var op1StackOffset = CalculateStackOffset(op1Stack.Value);
                var op2StackOffset = CalculateStackOffset(op2Stack.Value);

                this.RewriteMemory(
                    MemoryRewrite.MemoryOnRight,
                    new MemoryOperand(Register.BP, op1StackOffset),
                    new MemoryOperand(Register.BP, op2StackOffset),
                    inst2,
                    null);
            }
        }

        /// <summary>
        /// Generates code for a float instruction with two virtual registers
        /// </summary>
        /// <param name="destinationRegister">The destination register</param>
        /// <param name="sourceRegister">The source register</param>
        /// <param name="memoryRewrite">Determines how an instruction with two memory operands will be rewritten into one memory operand.</param>
        public void GenerateTwoRegistersFloatInstruction(VirtualRegister destinationRegister, VirtualRegister sourceRegister,
            Action<IList<byte>, FloatRegister, FloatRegister> inst1,
            Action<IList<byte>, FloatRegister, MemoryOperand> inst2,
            Action<IList<byte>, MemoryOperand, FloatRegister> inst3,
            MemoryRewrite memoryRewrite = MemoryRewrite.MemoryOnLeft)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;

            int? op1Stack = compilationData.RegisterAllocation.GetStackIndex(destinationRegister);
            int? op2Stack = compilationData.RegisterAllocation.GetStackIndex(sourceRegister);

            if (!op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1Reg = this.GetFloatRegisterForVirtual(destinationRegister).Value;
                var op2Reg = this.GetFloatRegisterForVirtual(sourceRegister).Value;
                inst1(generatedCode, op1Reg, op2Reg);
            }
            else if (!op1Stack.HasValue && op2Stack.HasValue)
            {
                var op1Reg = this.GetFloatRegisterForVirtual(destinationRegister).Value;
                var op2StackOffset = CalculateStackOffset(op2Stack.Value);
                inst2(generatedCode, op1Reg, new MemoryOperand(Register.BP, op2StackOffset));
            }
            else if (op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1StackOffset = CalculateStackOffset(op1Stack.Value);
                var op2Reg = this.GetFloatRegisterForVirtual(sourceRegister).Value;
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
                var opReg = this.GetIntRegisterForVirtual(sourceRegister).Value;

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
        /// Generates code for an instruction with a fixed register destination and virtual register source
        /// </summary>
        /// <param name="destination">The destination</param>
        /// <param name="sourceRegister">The source register</param>
        /// <param name="skipIfSame">Indicates if the instruction will be skipped of destination == source.</param>
        public void GenerateTwoRegisterFixedDestinationInstruction(FloatRegister destination, VirtualRegister sourceRegister,
            Action<IList<byte>, FloatRegister, FloatRegister> inst1,
            Action<IList<byte>, FloatRegister, MemoryOperand> inst2,
            bool skipIfSame = false)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;
            int? opStack = compilationData.RegisterAllocation.GetStackIndex(sourceRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetFloatRegisterForVirtual(sourceRegister).Value;

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
                var opReg = this.GetIntRegisterForVirtual(destinationRegister).Value;

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
        /// Generates code for an instruction with a virtual register destination and fixed register source
        /// </summary>
        /// <param name="destinationRegister">The destination register</param>
        /// <param name="source">The source</param>
        /// <param name="skipIfSame">Indicates if the instruction will be skipped of destination == source.</param>
        public void GenerateTwoRegisterFixedSourceInstruction(VirtualRegister destinationRegister, FloatRegister source,
            Action<IList<byte>, FloatRegister, FloatRegister> inst1,
            Action<IList<byte>, MemoryOperand, FloatRegister> inst2,
            bool skipIfSame = false)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;
            int? opStack = compilationData.RegisterAllocation.GetStackIndex(destinationRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetFloatRegisterForVirtual(destinationRegister).Value;

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
                var opReg = this.GetIntRegisterForVirtual(sourceRegister).Value;
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
                var destinationReg = this.GetIntRegisterForVirtual(destinationRegister).Value;
                inst1(generatedCode, destinationReg, source);
            }
            else
            {
                var opStackOffset = CalculateStackOffset(opStack.Value);
                RewriteMemory(memoryRewrite, new MemoryOperand(Register.BP, opStackOffset), source, inst1, inst2);
            }
        }

        /// <summary>
        /// Generates code for a float instruction with virtual register destination and memory source
        /// </summary>
        /// <param name="destinationRegister">The destination</param>
        /// <param name="source">The source</param>
        /// <param name="memoryRewrite">Determines how an instruction with two memory operands will be rewritten into one memory operand.</param>
        public void GenerateOneRegisterMemorySourceFloatInstruction(VirtualRegister destinationRegister, MemoryOperand source,
            Action<IList<byte>, FloatRegister, MemoryOperand> inst1,
            Action<IList<byte>, MemoryOperand, FloatRegister> inst2,
            MemoryRewrite memoryRewrite = MemoryRewrite.MemoryOnLeft)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;
            int? opStack = compilationData.RegisterAllocation.GetStackIndex(destinationRegister);

            if (!opStack.HasValue)
            {
                var destinationReg = this.GetFloatRegisterForVirtual(destinationRegister).Value;
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
                var opReg = this.GetIntRegisterForVirtual(destinationRegister).Value;
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
