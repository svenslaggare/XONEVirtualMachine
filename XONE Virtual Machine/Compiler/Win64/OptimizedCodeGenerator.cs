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
    /// Represents an optimized code generator
    /// </summary>
    public class OptimizedCodeGenerator
    {
        private readonly VirtualMachine virtualMachine;
        private readonly CallingConvetions callingConvetions = new CallingConvetions();

        /// <summary>
        /// Creates a new code generator
        /// </summary>
        /// <param name="virtualMachine">The virtual machine</param>
        public OptimizedCodeGenerator(VirtualMachine virtualMachine)
        {
            this.virtualMachine = virtualMachine;
        }

        /// <summary>
        /// Generates a call to the given function
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="toCall">The address of the function to call</param>
        /// <param name="callRegister">The register where the address will be stored in</param>
        private void GenerateCall(IList<byte> generatedCode, IntPtr toCall, Registers callRegister = Registers.AX)
        {
            RawAssembler.MoveLongToRegister(generatedCode, callRegister, toCall.ToInt64());
            RawAssembler.CallInRegister(generatedCode, callRegister);
        }

        /// <summary>
        /// Compiles the given function
        /// </summary>
        /// <param name="function">The compilationData</param>
        public void CompileFunction(CompilationData compilationData)
        {
            var function = compilationData.Function;
            this.CreateProlog(compilationData);

            for (int i = 0; i < function.Instructions.Count; i++)
            {
                this.GenerateInstruction(compilationData, compilationData.VirtualInstructions[i], i);
            }
        }

        /// <summary>
        /// A none integer register
        /// </summary>
        private struct NoneIntRegister
        {
            public bool IsBase { get; set; }
            public Registers BaseRegister { get; set; }
            public ExtendedRegisters ExtendedRegister { get; set; }

            public static bool operator ==(NoneIntRegister lhs, NoneIntRegister rhs)
            {
                if (lhs.IsBase != rhs.IsBase)
                {
                    return false;
                }

                if (lhs.IsBase)
                {
                    return lhs.BaseRegister == rhs.BaseRegister;
                }
                else
                {
                    return lhs.ExtendedRegister == rhs.ExtendedRegister;
                }
            }

            public static bool operator !=(NoneIntRegister lhs, NoneIntRegister rhs)
            {
                return !(lhs == rhs);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is NoneIntRegister))
                {
                    return false;
                }

                var other = (NoneIntRegister)obj;
                return this == other;
            }

            public override int GetHashCode()
            {
                if (this.IsBase)
                {
                    return (this.IsBase.GetHashCode() + 1) + 31 * (int)this.BaseRegister;
                }
                else
                {
                    return (this.IsBase.GetHashCode() + 1) + 31 * (int)this.ExtendedRegister;
                }
            }

            public override string ToString()
            {
                if (this.IsBase)
                {
                    return "R" + this.BaseRegister.ToString();
                }
                else
                {
                    return this.ExtendedRegister.ToString();
                }
            }
        }

        /// <summary>
        /// Returns the spill register
        /// </summary>
        private NoneIntRegister GetSpillRegister()
        {
            return new NoneIntRegister() { IsBase = false, ExtendedRegister = ExtendedRegisters.R12 };
        }

        /// <summary>
        /// Returns the register for the given allocated register
        /// </summary>
        /// <param name="register">The register</param>
        private NoneIntRegister GetRegister(int register)
        {
            if (register == 0)
            {
                return new NoneIntRegister() { IsBase = true, BaseRegister = Registers.AX };
            }
            else if (register == 1)
            {
                return new NoneIntRegister() { IsBase = true, BaseRegister = Registers.CX };
            }
            else if (register == 2)
            {
                return new NoneIntRegister() { IsBase = true, BaseRegister = Registers.DX };
            }
            else if (register == 3)
            {
                return new NoneIntRegister() { IsBase = false, ExtendedRegister = ExtendedRegisters.R8 };
            }
            else if (register == 4)
            {
                return new NoneIntRegister() { IsBase = false, ExtendedRegister = ExtendedRegisters.R9 };
            }
            else if (register == 5)
            {
                return new NoneIntRegister() { IsBase = false, ExtendedRegister = ExtendedRegisters.R10 };
            }
            else if (register == 6)
            {
                return new NoneIntRegister() { IsBase = false, ExtendedRegister = ExtendedRegisters.R11 };
            }

            throw new InvalidOperationException("The given register is not valid.");
        }

        /// <summary>
        /// Generates code for a two register operand instruction
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="op1">The first operand</param>
        /// <param name="op2">The second operand</param>
        private void GenerateTwoRegistersInstruction(IList<byte> generatedCode, NoneIntRegister op1, NoneIntRegister op2,
            Action<IList<byte>, Registers, Registers> inst1, Action<IList<byte>, ExtendedRegisters, ExtendedRegisters> inst2,
            Action<IList<byte>, Registers, ExtendedRegisters> inst3, Action<IList<byte>, ExtendedRegisters, Registers> inst4)
        {
            if (op1.IsBase && op2.IsBase)
            {
                inst1(generatedCode, op1.BaseRegister, op2.BaseRegister);
            }
            else if (!op1.IsBase && !op2.IsBase)
            {
                inst2(generatedCode, op1.ExtendedRegister, op2.ExtendedRegister);
            }
            else if (op1.IsBase && !op2.IsBase)
            {
                inst3(generatedCode, op1.BaseRegister, op2.ExtendedRegister);
            }
            else
            {
                inst4(generatedCode, op1.ExtendedRegister, op2.BaseRegister);
            }
        }

        /// <summary>
        /// Handles how two memory operands are rewritten
        /// </summary>
        private enum MemoryRewrite
        {
            MemoryOnLeft,
            MemoryOnRight
        }

        /// <summary>
        /// Generates code for a two register operand instruction
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="op1Register">The first operand</param>
        /// <param name="op2Register">The second operand</param>
        private void GenerateTwoRegistersInstruction(CompilationData compilationData, int op1Register, int op2Register,
            Action<IList<byte>, Registers, Registers> inst1, Action<IList<byte>, ExtendedRegisters, ExtendedRegisters> inst2,
            Action<IList<byte>, Registers, ExtendedRegisters> inst3, Action<IList<byte>, ExtendedRegisters, Registers> inst4,
            Action<IList<byte>, Registers, Registers, int> inst5, Action<IList<byte>, ExtendedRegisters, Registers, int> inst6,
            Action<IList<byte>, Registers, int, Registers> inst7, Action<IList<byte>, Registers, int, ExtendedRegisters> inst8,
            MemoryRewrite memoryRewrite = MemoryRewrite.MemoryOnLeft)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;

            int? op1Stack = compilationData.RegisterAllocation.GetStackIndex(op1Register);
            int? op2Stack = compilationData.RegisterAllocation.GetStackIndex(op2Register);

            if (!op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1Reg = this.GetRegister(regAlloc.GetRegister(op1Register) ?? 0);
                var op2Reg = this.GetRegister(regAlloc.GetRegister(op2Register) ?? 0);

                if (op1Reg.IsBase && op2Reg.IsBase)
                {
                    inst1(generatedCode, op1Reg.BaseRegister, op2Reg.BaseRegister);
                }
                else if (!op1Reg.IsBase && !op2Reg.IsBase)
                {
                    inst2(generatedCode, op1Reg.ExtendedRegister, op2Reg.ExtendedRegister);
                }
                else if (op1Reg.IsBase && !op2Reg.IsBase)
                {
                    inst3(generatedCode, op1Reg.BaseRegister, op2Reg.ExtendedRegister);
                }
                else
                {
                    inst4(generatedCode, op1Reg.ExtendedRegister, op2Reg.BaseRegister);
                }
            }
            else if (!op1Stack.HasValue && op2Stack.HasValue)
            {
                var op1Reg = this.GetRegister(regAlloc.GetRegister(op1Register) ?? 0);
                var op2StackOffset = -RawAssembler.RegisterSize * (1 + op2Stack.Value);

                if (op1Reg.IsBase)
                {
                    inst5(generatedCode, op1Reg.BaseRegister, Registers.BP, op2StackOffset);
                }
                else
                {
                    inst6(generatedCode, op1Reg.ExtendedRegister, Registers.BP, op2StackOffset);
                }
            }
            else if (op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1StackOffset = -RawAssembler.RegisterSize * (1 + op1Stack.Value);
                var op2Reg = this.GetRegister(regAlloc.GetRegister(op2Register) ?? 0);

                if (op2Reg.IsBase)
                {
                    inst7(generatedCode, Registers.BP, op1StackOffset, op2Reg.BaseRegister);
                }
                else
                {
                    inst8(generatedCode, Registers.BP, op1StackOffset, op2Reg.ExtendedRegister);
                }
            }
            else
            {
                var op1StackOffset = -RawAssembler.RegisterSize * (1 + op1Stack.Value);
                var op2StackOffset = -RawAssembler.RegisterSize * (1 + op2Stack.Value);

                var spillReg = this.GetSpillRegister();

                if (spillReg.IsBase)
                {
                    if (memoryRewrite == MemoryRewrite.MemoryOnLeft)
                    {
                        RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister(
                            generatedCode,
                            spillReg.BaseRegister,
                            Registers.BP,
                            op2StackOffset);

                        inst7(generatedCode, Registers.BP, op1StackOffset, spillReg.BaseRegister);
                    }
                    else
                    {
                        RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister(
                            generatedCode,
                            spillReg.BaseRegister,
                            Registers.BP,
                            op1StackOffset);

                        inst5(generatedCode, spillReg.BaseRegister, Registers.BP, op2StackOffset);

                        RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(
                            generatedCode,
                            Registers.BP,
                            op1StackOffset,
                            spillReg.BaseRegister);
                    }
                }
                else
                {
                    if (memoryRewrite == MemoryRewrite.MemoryOnLeft)
                    {
                        RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister(
                            generatedCode,
                            spillReg.ExtendedRegister,
                            Registers.BP,
                            op2StackOffset);

                        inst8(generatedCode, Registers.BP, op1StackOffset, spillReg.ExtendedRegister);
                    }
                    else
                    {
                        RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister(
                            generatedCode,
                            spillReg.ExtendedRegister,
                            Registers.BP,
                            op1StackOffset);

                        inst6(generatedCode, spillReg.ExtendedRegister, Registers.BP, op2StackOffset);

                        RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(
                            generatedCode,
                            Registers.BP,
                            op1StackOffset,
                            spillReg.ExtendedRegister);
                    }
                }
            }
        }

        /// <summary>
        /// Generates code for an one register operand instruction
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="op">The operand</param>
        private void GenerateOneRegisterInstruction(IList<byte> generatedCode, NoneIntRegister op,
            Action<IList<byte>, Registers> inst1, Action<IList<byte>, ExtendedRegisters> inst2)
        {
            if (op.IsBase)
            {
                inst1(generatedCode, op.BaseRegister);
            }
            else
            {
                inst2(generatedCode, op.ExtendedRegister);
            }
        }

        /// <summary>
        /// Generates code for an one register operand instruction with an int value
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="op">The operand</param>
        /// <param name="value">The value</param>
        private void GenerateOneRegisterWithValueInstruction(IList<byte> generatedCode, NoneIntRegister op, int value,
            Action<IList<byte>, Registers, int> inst1, Action<IList<byte>, ExtendedRegisters, int> inst2)
        {
            if (op.IsBase)
            {
                inst1(generatedCode, op.BaseRegister, value);
            }
            else
            {
                inst2(generatedCode, op.ExtendedRegister, value);
            }
        }

        /// <summary>
        /// Generates code for an one register operand instruction with an int value
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="opRegister">The operand</param>
        /// <param name="value">The value</param>
        private void GenerateOneRegisterWithValueInstruction(CompilationData compilationData, int opRegister, int value,
            Action<IList<byte>, Registers, int> inst1, Action<IList<byte>, ExtendedRegisters, int> inst2,
            Action<IList<byte>, Registers, int, int> inst3)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;

            var opStack = regAlloc.GetStackIndex(opRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetRegister(regAlloc.GetRegister(opRegister) ?? 0);

                if (opReg.IsBase)
                {
                    inst1(generatedCode, opReg.BaseRegister, value);
                }
                else
                {
                    inst2(generatedCode, opReg.ExtendedRegister, value);
                }
            }
            else
            {
                var opStackOffset = -RawAssembler.RegisterSize * (1 + opStack.Value);
                inst3(generatedCode, Registers.BP, opStackOffset, value);
            }
        }

        /// <summary>
        /// Creates the function prolog
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        private void CreateProlog(CompilationData compilationData)
        {
            var function = compilationData.Function;

            //Calculate the size of the stack aligned to 16 bytes
            var def = function.Definition;
            int neededStackSize =
                RawAssembler.RegisterSize
                * (def.Parameters.Count + compilationData.RegisterAllocation.NumSpilledRegisters);

            int stackSize = ((neededStackSize + 15) / 16) * 16;

            //Save the base pointer
            RawAssembler.PushRegister(function.GeneratedCode, Registers.BP); //push rbp
            RawAssembler.MoveRegisterToRegister(function.GeneratedCode, Registers.BP, Registers.SP); //mov rbp, rsp

            //Make room for the variables on the stack
            RawAssembler.SubConstantFromRegister(function.GeneratedCode, Registers.SP, stackSize); //sub rsp, <size of stack>

            //Move the arguments to the stack
            this.callingConvetions.MoveArgumentsToStack(compilationData);

            if (compilationData.RegisterAllocation.NumSpilledRegisters > 0)
            {
                this.GenerateOneRegisterInstruction(
                    function.GeneratedCode,
                    this.GetSpillRegister(),
                    RawAssembler.PushRegister,
                    RawAssembler.PushRegister);
            }

            //Zero locals
            this.ZeroLocals(compilationData);
        }

        /// <summary>
        /// Zeroes the locals
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        private void ZeroLocals(CompilationData compilationData)
        {
            var func = compilationData.Function;

            if (compilationData.RegisterAllocation.NumSpilledRegisters > 0)
            {
                var spillReg = this.GetSpillRegister();
                GenerateTwoRegistersInstruction(
                    func.GeneratedCode,
                    spillReg,
                    spillReg,
                    (gen, x, y) => RawAssembler.XorRegisterToRegister(gen, x, y),
                    RawAssembler.XorRegisterToRegister,
                    null,
                    null);
            }

            foreach (var localRegister in compilationData.LocalVirtualRegisters)
            {
                var reg = compilationData.RegisterAllocation.GetRegister(localRegister);

                if (reg.HasValue)
                {
                    var localReg = GetRegister(reg.Value);

                    GenerateTwoRegistersInstruction(
                        func.GeneratedCode,
                        localReg,
                        localReg,
                        (gen, x, y) => RawAssembler.XorRegisterToRegister(gen, x, y),
                        RawAssembler.XorRegisterToRegister,
                        null,
                        null);
                }
                else
                {
                    var spillReg = this.GetSpillRegister();
                    int stackOffset = 
                        -RawAssembler.RegisterSize
                        * (1 + compilationData.RegisterAllocation.GetStackIndex(localRegister) ?? 0);

                    if (spillReg.IsBase)
                    {
                        RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(
                            func.GeneratedCode,
                            Registers.BP,
                            stackOffset,
                            spillReg.BaseRegister);
                    }
                    else
                    {
                        RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(
                            func.GeneratedCode,
                            Registers.BP,
                            stackOffset,
                            spillReg.ExtendedRegister);
                    }
                }
            }
        }

        /// <summary>
        /// Creates the function epilog
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        private void CreateEpilog(CompilationData compilationData)
        {
            var generatedCode = compilationData.Function.GeneratedCode;

            if (compilationData.RegisterAllocation.NumSpilledRegisters > 0)
            {
                this.GenerateOneRegisterInstruction(
                    generatedCode,
                    this.GetSpillRegister(),
                    RawAssembler.PopRegister,
                    RawAssembler.PopRegister);
            }

            //Restore the base pointer
            RawAssembler.MoveRegisterToRegister(generatedCode, Registers.SP, Registers.BP); //mov rsp, rbp
            RawAssembler.PopRegister(generatedCode, Registers.BP); //pop rbp
        }

        /// <summary>
        /// Generates native code for the given instruction
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="instruction">The current instruction</param>
        /// <param name="index">The index of the instruction</param>
        private void GenerateInstruction(CompilationData compilationData, VirtualInstruction virtualInstruction, int index)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var operandStack = compilationData.OperandStack;
            var funcDef = compilationData.Function.Definition;

            compilationData.InstructionMapping.Add(generatedCode.Count);

            var instruction = virtualInstruction.Instruction;
            var registerAllocation = compilationData.RegisterAllocation;

            Func<int> GetAssignRegister = () =>
            {
                return virtualInstruction.AssignRegister.Value;
            };

            Func<int, int> GetUseRegister = x =>
            {
                return virtualInstruction.UsesRegisters[x];
            };

            switch (instruction.OpCode)
            {
                case OpCodes.LoadInt:
                    {
                        var storeReg = GetAssignRegister();

                        GenerateOneRegisterWithValueInstruction(
                            compilationData,
                            storeReg,
                            instruction.IntValue,
                            RawAssembler.MoveIntToRegister,
                            RawAssembler.MoveIntToRegister,
                            RawAssembler.MoveIntToMemoryRegWithOffset);
                    }
                    break;
                case OpCodes.AddInt:
                case OpCodes.SubInt:
                case OpCodes.MulInt:
                case OpCodes.DivInt:
                    {
                        var op2Reg = GetUseRegister(0);
                        var op1Reg = GetUseRegister(1);
                        var storeReg = GetAssignRegister();

                        switch (instruction.OpCode)
                        {
                            case OpCodes.AddInt:
                                GenerateTwoRegistersInstruction(
                                    compilationData,
                                    op1Reg,
                                    op2Reg,
                                    (gen, x, y) => RawAssembler.AddRegisterToRegister(gen, x, y),
                                    RawAssembler.AddRegisterToRegister,
                                    RawAssembler.AddRegisterToRegister,
                                    RawAssembler.AddRegisterToRegister,
                                    RawAssembler.AddMemoryRegisterWithOffsetToRegister,
                                    RawAssembler.AddMemoryRegisterWithOffsetToRegister,
                                    RawAssembler.AddRegisterToMemoryRegisterWithOffset,
                                    RawAssembler.AddRegisterToMemoryRegisterWithOffset);
                                break;
                            case OpCodes.SubInt:
                                GenerateTwoRegistersInstruction(
                                    compilationData,
                                    op1Reg,
                                    op2Reg,
                                    (gen, x, y) => RawAssembler.SubRegisterFromRegister(gen, x, y),
                                    RawAssembler.SubRegisterFromRegister,
                                    RawAssembler.SubRegisterFromRegister,
                                    RawAssembler.SubRegisterFromRegister,
                                    RawAssembler.SubMemoryRegisterWithOffsetFromRegister,
                                    RawAssembler.SubMemoryRegisterWithOffsetFromRegister,
                                    RawAssembler.SubRegisterFromMemoryRegisterWithOffset,
                                    RawAssembler.SubRegisterFromMemoryRegisterWithOffset);
                                break;
                            case OpCodes.MulInt:
                                Action<IList<byte>, Registers, int, Registers> multRegisterToMemoryRegisterWithOffset = (gen, destMem, offset, src) =>
                                {
                                    var spillReg = GetSpillRegister();

                                    if (spillReg.IsBase)
                                    {
                                        RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister(
                                            gen,
                                            spillReg.BaseRegister,
                                            Registers.BP,
                                            offset);

                                        RawAssembler.MultRegisterToRegister(
                                            gen,
                                            spillReg.BaseRegister,
                                            src);

                                        RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(
                                            gen,
                                            Registers.BP,
                                            offset,
                                            spillReg.BaseRegister);
                                    }
                                    else
                                    {
                                        RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister(
                                            gen,
                                            spillReg.ExtendedRegister,
                                            Registers.BP,
                                            offset);

                                        RawAssembler.MultRegisterToRegister(
                                            gen,
                                            spillReg.ExtendedRegister,
                                            src);

                                        RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(
                                            gen,
                                            Registers.BP,
                                            offset,
                                            spillReg.ExtendedRegister);
                                    }
                                };

                                Action<IList<byte>, Registers, int, ExtendedRegisters> multRegisterToMemoryRegisterWithOffset2 = (gen, destMem, offset, src) =>
                                {
                                    var spillReg = GetSpillRegister();

                                    if (spillReg.IsBase)
                                    {
                                        RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister(
                                            gen,
                                            spillReg.BaseRegister,
                                            Registers.BP,
                                            offset);

                                        RawAssembler.MultRegisterToRegister(
                                            gen,
                                            spillReg.BaseRegister,
                                            src);

                                        RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(
                                            gen,
                                            Registers.BP,
                                            offset,
                                            spillReg.BaseRegister);
                                    }
                                    else
                                    {
                                        RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister(
                                            gen,
                                            spillReg.ExtendedRegister,
                                            Registers.BP,
                                            offset);

                                        RawAssembler.MultRegisterToRegister(
                                            gen,
                                            spillReg.ExtendedRegister,
                                            src);

                                        RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(
                                            gen,
                                            Registers.BP,
                                            offset,
                                            spillReg.ExtendedRegister);
                                    }
                                };

                                GenerateTwoRegistersInstruction(
                                    compilationData,
                                    op1Reg,
                                    op2Reg,
                                    (gen, x, y) => RawAssembler.MultRegisterToRegister(gen, x, y),
                                    RawAssembler.MultRegisterToRegister,
                                    RawAssembler.MultRegisterToRegister,
                                    RawAssembler.MultRegisterToRegister,
                                    RawAssembler.MultMemoryRegisterWithOffsetToRegister,
                                    RawAssembler.MultMemoryRegisterWithOffsetToRegister,
                                    multRegisterToMemoryRegisterWithOffset,
                                    multRegisterToMemoryRegisterWithOffset2,
                                    MemoryRewrite.MemoryOnRight);
                                break;
                                //case OpCodes.DivInt:
                                //    if (op1Reg.BaseRegister != Registers.AX)
                                //    {
                                //        throw new InvalidOperationException("Internal limitation: RAX only supported as destination of division.");
                                //    }

                                //    //This sign extends the rax register
                                //    generatedCode.Add(0x99); //cdq

                                //    GenerateTwoRegistersInstruction(
                                //        generatedCode,
                                //        op1Reg,
                                //        op2Reg,
                                //        (gen, x, y) => Assembler.DivRegisterFromRegister(gen, x, y),
                                //        null,
                                //        Assembler.DivRegisterFromRegister,
                                //        null);
                                //    break;
                        }

                        if (op1Reg != storeReg)
                        {
                            GenerateTwoRegistersInstruction(
                                compilationData,
                                storeReg,
                                op1Reg,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                                (gen, destMem, offset, source) => RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(gen, destMem, offset, source),
                                RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset);
                        }
                    }
                    break;
                case OpCodes.Ret:
                    {
                        //Handle the return value
                        var opVirtualReg = GetUseRegister(0);
                        var opStack = registerAllocation.GetStackIndex(opVirtualReg);

                        if (opStack.HasValue)
                        {
                            RawAssembler.MoveMemoryRegisterWithOffsetToRegister(
                                generatedCode,
                                Registers.AX,
                                Registers.BP,
                                -RawAssembler.RegisterSize * (1 + opStack.Value));
                        }
                        else
                        {
                            var opReg = this.GetRegister(registerAllocation.GetRegister(opVirtualReg) ?? 0);

                            if (!(opReg.IsBase && opReg.BaseRegister == Registers.AX))
                            {
                                GenerateTwoRegistersInstruction(
                                    generatedCode,
                                    new NoneIntRegister() { IsBase = true, BaseRegister = Registers.AX },
                                    opReg,
                                    RawAssembler.MoveRegisterToRegister,
                                    RawAssembler.MoveRegisterToRegister,
                                    RawAssembler.MoveRegisterToRegister,
                                    RawAssembler.MoveRegisterToRegister);
                            }
                        }

                        //Restore the base pointer
                        this.CreateEpilog(compilationData);

                        //Make the return
                        RawAssembler.Return(generatedCode);
                    }
                    break;
                case OpCodes.LoadLocal:
                case OpCodes.StoreLocal:
                    {
                        if (instruction.OpCode == OpCodes.LoadLocal)
                        {
                            var valueReg = GetAssignRegister();
                            var localReg = GetUseRegister(0);

                            GenerateTwoRegistersInstruction(
                                compilationData,
                                valueReg,
                                localReg,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                                (gen, destMem, offset, source) => RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(gen, destMem, offset, source),
                                RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset);
                        }
                        else
                        {
                            var valueReg = GetUseRegister(0);
                            var localReg = GetAssignRegister();

                            GenerateTwoRegistersInstruction(
                                compilationData,
                                localReg,
                                valueReg,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveRegisterToRegister,
                                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                                (gen, destMem, offset, source) => RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(gen, destMem, offset, source),
                                RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset);
                        }
                    }
                    break;
                case OpCodes.Branch:
                    RawAssembler.Jump(generatedCode, 0); //jmp <target>

                    compilationData.UnresolvedBranches.Add(
                        generatedCode.Count - 5,
                        new UnresolvedBranchTarget(instruction.IntValue, 5));
                    break;
                case OpCodes.BranchEqual:
                case OpCodes.BranchNotEqual:
                case OpCodes.BranchGreaterThan:
                case OpCodes.BranchGreaterOrEqual:
                case OpCodes.BranchLessThan:
                case OpCodes.BranchLessOrEqual:
                    {
                        var opType = compilationData.Function.OperandTypes[index].Last();
                        bool unsignedComparison = false;

                        var op2Reg = GetUseRegister(0);
                        var op1Reg = GetUseRegister(1);

                        //Compare
                        GenerateTwoRegistersInstruction(
                            compilationData,
                            op1Reg,
                            op2Reg,
                            RawAssembler.CompareRegisterToRegister,
                            RawAssembler.CompareRegisterToRegister,
                            RawAssembler.CompareRegisterToRegister,
                            RawAssembler.CompareRegisterToRegister,
                            RawAssembler.CompareRegisterToMemoryRegisterWithOffset,
                            RawAssembler.CompareRegisterToMemoryRegisterWithOffset,
                            RawAssembler.CompareMemoryRegisterWithOffsetToRegister,
                            RawAssembler.CompareMemoryRegisterWithOffsetToRegister);

                        switch (instruction.OpCode)
                        {
                            case OpCodes.BranchEqual:
                                RawAssembler.JumpEqual(generatedCode, 0); // je <target>
                                break;
                            case OpCodes.BranchNotEqual:
                                RawAssembler.JumpNotEqual(generatedCode, 0); // jne <target>
                                break;
                            case OpCodes.BranchGreaterThan:
                                if (unsignedComparison)
                                {
                                    RawAssembler.JumpGreaterThanUnsigned(generatedCode, 0); // jg <target>
                                }
                                else
                                {
                                    RawAssembler.JumpGreaterThan(generatedCode, 0); // jg <target>
                                }
                                break;
                            case OpCodes.BranchGreaterOrEqual:
                                if (unsignedComparison)
                                {
                                    RawAssembler.JumpGreaterThanOrEqualUnsigned(generatedCode, 0); // jge <target>
                                }
                                else
                                {
                                    RawAssembler.JumpGreaterThanOrEqual(generatedCode, 0); // jge <target>
                                }
                                break;
                            case OpCodes.BranchLessThan:
                                if (unsignedComparison)
                                {
                                    RawAssembler.JumpLessThanUnsigned(generatedCode, 0); // jl <target>
                                }
                                else
                                {
                                    RawAssembler.JumpLessThan(generatedCode, 0); // jl <target>
                                }
                                break;
                            case OpCodes.BranchLessOrEqual:
                                if (unsignedComparison)
                                {
                                    RawAssembler.JumpLessThanOrEqualUnsigned(generatedCode, 0); // jle <target>
                                }
                                else
                                {
                                    RawAssembler.JumpLessThanOrEqual(generatedCode, 0); // jle <target>
                                }
                                break;
                        }

                        compilationData.UnresolvedBranches.Add(
                            generatedCode.Count - 6,
                            new UnresolvedBranchTarget(instruction.IntValue, 6));
                    }
                    break;
            }
        }
    }
}
