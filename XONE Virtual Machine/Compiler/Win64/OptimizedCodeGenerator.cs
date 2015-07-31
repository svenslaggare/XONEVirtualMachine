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
        /// Returns the spill register
        /// </summary>
        private IntRegister GetSpillRegister()
        {
            return new IntRegister(ExtendedRegisters.R12);
        }

        /// <summary>
        /// Returns the register for the given allocated register
        /// </summary>
        /// <param name="register">The register</param>
        private IntRegister GetRegister(int register)
        {
            if (register == 0)
            {
                return new IntRegister(Registers.AX);
            }
            else if (register == 1)
            {
                return new IntRegister(Registers.CX);
            }
            else if (register == 2)
            {
                return new IntRegister(Registers.DX);
            }
            else if (register == 3)
            {
                return new IntRegister(ExtendedRegisters.R8);
            }
            else if (register == 4)
            {
                return new IntRegister(ExtendedRegisters.R9);
            }
            else if (register == 5)
            {
                return new IntRegister(ExtendedRegisters.R10);
            }
            else if (register == 6)
            {
                return new IntRegister(ExtendedRegisters.R11);
            }

            throw new InvalidOperationException("The given register is not valid.");
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
            Action<IList<byte>, IntRegister, IntRegister> inst1,
            Action<IList<byte>, IntRegister, MemoryOperand> inst2,
            Action<IList<byte>, MemoryOperand, IntRegister> inst3,
            MemoryRewrite memoryRewrite = MemoryRewrite.MemoryOnLeft)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;

            int? op1Stack = compilationData.RegisterAllocation.GetStackIndex(op1Register);
            int? op2Stack = compilationData.RegisterAllocation.GetStackIndex(op2Register);

            if (!op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1Reg = GetRegister(regAlloc.GetRegister(op1Register) ?? 0);
                var op2Reg = GetRegister(regAlloc.GetRegister(op2Register) ?? 0);
                inst1(generatedCode, op1Reg, op2Reg);
            }
            else if (!op1Stack.HasValue && op2Stack.HasValue)
            {
                var op1Reg = this.GetRegister(regAlloc.GetRegister(op1Register) ?? 0);
                var op2StackOffset = -RawAssembler.RegisterSize * (1 + op2Stack.Value);
                inst2(generatedCode, op1Reg, new MemoryOperand(Registers.BP, op2StackOffset));
            }
            else if (op1Stack.HasValue && !op2Stack.HasValue)
            {
                var op1StackOffset = -RawAssembler.RegisterSize * (1 + op1Stack.Value);
                var op2Reg = this.GetRegister(regAlloc.GetRegister(op2Register) ?? 0);
                inst3(generatedCode, new MemoryOperand(Registers.BP, op1StackOffset), op2Reg);
            }
            else
            {
                var op1StackOffset = -RawAssembler.RegisterSize * (1 + op1Stack.Value);
                var op2StackOffset = -RawAssembler.RegisterSize * (1 + op2Stack.Value);
                var spillReg = this.GetSpillRegister();

                if (memoryRewrite == MemoryRewrite.MemoryOnLeft)
                {
                    Assembler.Move(generatedCode, spillReg, new MemoryOperand(Registers.BP, op2StackOffset));
                    inst3(generatedCode, new MemoryOperand(Registers.BP, op1StackOffset), spillReg);
                }
                else
                {
                    Assembler.Move(generatedCode, spillReg, new MemoryOperand(Registers.BP, op1StackOffset));
                    inst2(generatedCode, spillReg, new MemoryOperand(Registers.BP, op2StackOffset));
                    Assembler.Move(generatedCode, new MemoryOperand(Registers.BP, op1StackOffset), spillReg);
                }
            }
        }

        /// <summary>
        /// Generates code for an instruction with a register destination and memory source
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="destination">The destination</param>
        /// <param name="op2">The operand</param>
        private void GenerateSourceMemoryInstruction(CompilationData compilationData, IntRegister destination, int opRegister,
            Action<IList<byte>, IntRegister, IntRegister> inst1, Action<IList<byte>, IntRegister, MemoryOperand> inst2)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;
            int? opStack = compilationData.RegisterAllocation.GetStackIndex(opRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetRegister(regAlloc.GetRegister(opRegister) ?? 0);
                inst1(generatedCode, destination, opReg);
            }
            else
            {
                var opStackOffset = -RawAssembler.RegisterSize * (1 + opStack.Value);
                inst2(generatedCode, destination, new MemoryOperand(Registers.BP, opStackOffset));
            }
        }

        /// <summary>
        /// Generates code for an one virtual register operand instruction with an int value
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="opRegister">The operand</param>
        /// <param name="value">The value</param>
        private void GenerateOneRegisterWithValueInstruction(CompilationData compilationData, int opRegister, int value,
            Action<IList<byte>, IntRegister, int> inst1, Action<IList<byte>, MemoryOperand, int> inst2)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var regAlloc = compilationData.RegisterAllocation;

            var opStack = regAlloc.GetStackIndex(opRegister);

            if (!opStack.HasValue)
            {
                var opReg = this.GetRegister(regAlloc.GetRegister(opRegister) ?? 0);
                inst1(generatedCode, opReg, value);
            }
            else
            {
                var stackOp = new MemoryOperand(
                    Registers.BP,
                    -RawAssembler.RegisterSize * (1 + opStack.Value));
                inst2(generatedCode, stackOp, value);
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
            Assembler.Push(function.GeneratedCode, Registers.BP); //push rbp
            Assembler.Move(function.GeneratedCode, Registers.BP, Registers.SP); //mov rbp, rsp

            //Make room for the variables on the stack
            RawAssembler.SubConstantFromRegister(function.GeneratedCode, Registers.SP, stackSize); //sub rsp, <size of stack>

            //Move the arguments to the stack
            this.callingConvetions.MoveArgumentsToStack(compilationData);

            if (compilationData.RegisterAllocation.NumSpilledRegisters > 0)
            {
                Assembler.Push(function.GeneratedCode, this.GetSpillRegister());
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
                Assembler.Xor(func.GeneratedCode, spillReg, spillReg);
            }

            foreach (var localRegister in compilationData.LocalVirtualRegisters)
            {
                var reg = compilationData.RegisterAllocation.GetRegister(localRegister);

                if (reg.HasValue)
                {
                    var localReg = GetRegister(reg.Value);
                    Assembler.Xor(func.GeneratedCode, localReg, localReg);
                }
                else
                {
                    var spillReg = this.GetSpillRegister();
                    int stackOffset = 
                        -RawAssembler.RegisterSize
                        * (1 + compilationData.RegisterAllocation.GetStackIndex(localRegister) ?? 0);

                    Assembler.Move(func.GeneratedCode, new MemoryOperand(Registers.BP, stackOffset), spillReg);
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
                Assembler.Pop(generatedCode, this.GetSpillRegister());
            }

            //Restore the base pointer
            Assembler.Move(generatedCode, Registers.SP, Registers.BP);  //mov rsp, rbp
            Assembler.Pop(generatedCode, Registers.BP); //pop rbp
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
                            Assembler.Move,
                            Assembler.Move);
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
                                    Assembler.Add,
                                    Assembler.Add,
                                    Assembler.Add);
                                break;
                            case OpCodes.SubInt:
                                GenerateTwoRegistersInstruction(
                                    compilationData,
                                    op1Reg,
                                    op2Reg,
                                    Assembler.Sub,
                                    Assembler.Sub,
                                    Assembler.Sub);
                                break;
                            case OpCodes.MulInt:
                                Action<IList<byte>, MemoryOperand, IntRegister> multRegisterToMemoryRegisterWithOffset = (gen, destMem, src) =>
                                {
                                    var spillReg = GetSpillRegister();
                                    Assembler.Move(gen, spillReg, destMem);
                                    Assembler.Mult(gen, spillReg, src);
                                    Assembler.Move(gen, destMem, spillReg);
                                };

                                GenerateTwoRegistersInstruction(
                                    compilationData,
                                    op1Reg,
                                    op2Reg,
                                    Assembler.Mult,
                                    Assembler.Mult,
                                    multRegisterToMemoryRegisterWithOffset,
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
                                Assembler.Move,
                                Assembler.Move,
                                Assembler.Move);
                        }
                    }
                    break;
                case OpCodes.Ret:
                    {
                        //Handle the return value
                        var opReg = GetUseRegister(0);

                        GenerateSourceMemoryInstruction(
                            compilationData,
                            new IntRegister(Registers.AX),
                            opReg,
                            Assembler.Move,
                            Assembler.Move);

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
                                Assembler.Move,
                                Assembler.Move,
                                Assembler.Move);
                        }
                        else
                        {
                            var valueReg = GetUseRegister(0);
                            var localReg = GetAssignRegister();

                            GenerateTwoRegistersInstruction(
                                compilationData,
                                localReg,
                                valueReg,
                                Assembler.Move,
                                Assembler.Move,
                                Assembler.Move);
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
                            Assembler.Compare,
                            Assembler.Compare,
                            Assembler.Compare);

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
