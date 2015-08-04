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
        private readonly OptimizedCallingConventions callingConvetions = new OptimizedCallingConventions();

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
        private void GenerateCall(IList<byte> generatedCode, IntPtr toCall, Register callRegister = Register.AX)
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
            return new IntRegister(ExtendedRegister.R12);
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
            Assembler.Push(function.GeneratedCode, Register.BP);
            Assembler.Move(function.GeneratedCode, Register.BP, Register.SP);

            //Make room for the variables on the stack
            Assembler.Sub(function.GeneratedCode, Register.SP, stackSize);

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

            if (func.Locals.Count > 0)
            {
                if (compilationData.RegisterAllocation.NumSpilledRegisters > 0)
                {
                    //Zero the spill register
                    var spillReg = this.GetSpillRegister();
                    Assembler.Xor(func.GeneratedCode, spillReg, spillReg);
                }

                foreach (var localRegister in compilationData.LocalVirtualRegisters)
                {
                    var reg = compilationData.RegisterAllocation.GetRegister(localRegister);

                    if (reg.HasValue)
                    {
                        //Zero the local register
                        var localReg = compilationData.VirtualAssembler.GetRegister(reg.Value);
                        Assembler.Xor(func.GeneratedCode, localReg, localReg);
                    }
                    else
                    {
                        var spillReg = this.GetSpillRegister();
                        int stackOffset =
                            -RawAssembler.RegisterSize
                            * (1 + compilationData.RegisterAllocation.GetStackIndex(localRegister) ?? 0);

                        Assembler.Move(func.GeneratedCode, new MemoryOperand(Register.BP, stackOffset), spillReg);
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
                Assembler.Pop(generatedCode, this.GetSpillRegister());
            }

            //Restore the base pointer
            Assembler.Move(generatedCode, Register.SP, Register.BP);
            Assembler.Pop(generatedCode, Register.BP);
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
            var virtualAssembler = compilationData.VirtualAssembler;
            int stackOffset = 1;

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

                        virtualAssembler.GenerateOneRegisterWithValueInstruction(
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
                                virtualAssembler.GenerateTwoRegistersInstruction(
                                    op1Reg,
                                    op2Reg,
                                    Assembler.Add,
                                    Assembler.Add,
                                    Assembler.Add);
                                break;
                            case OpCodes.SubInt:
                                virtualAssembler.GenerateTwoRegistersInstruction(
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

                                virtualAssembler.GenerateTwoRegistersInstruction(
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
                            virtualAssembler.GenerateTwoRegistersInstruction(
                                storeReg,
                                op1Reg,
                                Assembler.Move,
                                Assembler.Move,
                                Assembler.Move);
                        }
                    }
                    break;
                case OpCodes.Call:
                    {
                        var signature = this.virtualMachine.Binder.FunctionSignature(
                            instruction.StringValue,
                            instruction.Parameters);

                        var funcToCall = this.virtualMachine.Binder.GetFunction(signature);

                        //Save registers
                        var aliveRegisters = virtualAssembler.GetAliveRegisters(index).ToList();
                        var aliveRegistersStack = new Dictionary<IntRegister, int>();
                        int stackIndex = 0;

                        foreach (var register in aliveRegisters)
                        {
                            Assembler.Push(generatedCode, register);
                            aliveRegistersStack.Add(register, stackIndex++);
                        }

                        //Align the stack
                        int stackAlignment = this.callingConvetions.CalculateStackAlignment(
                            compilationData,
                            funcToCall.Parameters,
                            aliveRegisters.Count);

                        if (stackAlignment > 0)
                        {
                            RawAssembler.SubConstantFromRegister(
                                generatedCode,
                                Register.SP,
                                stackAlignment);
                        }

                        //Set the function arguments
                        this.callingConvetions.CallFunctionArguments(
                            compilationData,
                            virtualInstruction.UsesRegisters,
                            aliveRegistersStack,
                            funcToCall);

                        //Reserve 32 bytes for the called function to spill registers
                        RawAssembler.SubByteFromRegister(generatedCode, Register.SP, 32);

                        //Generate the call
                        if (funcToCall.IsManaged)
                        {
                            //Mark that the function call needs to be patched with the entry point later
                            compilationData.UnresolvedFunctionCalls.Add(new UnresolvedFunctionCall(
                                FunctionCallAddressModes.Relative,
                                funcToCall,
                                generatedCode.Count));

                            RawAssembler.Call(generatedCode, 0);
                        }
                        else
                        {
                            this.GenerateCall(generatedCode, funcToCall.EntryPoint);
                        }

                        //Unalign the stack
                        RawAssembler.AddConstantToRegister(
                            generatedCode,
                            Register.SP,
                            stackAlignment + 32);

                        //Hande the return value
                        this.callingConvetions.HandleReturnValue(compilationData, funcToCall, GetAssignRegister());

                        var assignRegister = virtualAssembler.GetRegisterForVirtual(GetAssignRegister());

                        //Restore registers
                        foreach (var register in aliveRegisters.Reverse<IntRegister>())
                        {
                            //If the assign register is allocated, check if used.
                            if (assignRegister.HasValue)
                            {
                                if (register != assignRegister.Value)
                                {
                                    Assembler.Pop(generatedCode, register);
                                }
                                else
                                {
                                    //The assign register will have the return value as value, so don't pop to a register.
                                    Assembler.Pop(generatedCode);
                                }
                            }
                            else
                            {
                                Assembler.Pop(generatedCode, register);
                            }
                        }
                    }
                    break;
                case OpCodes.Ret:
                    {
                        //Handle the return value
                        this.callingConvetions.MakeReturnValue(compilationData, GetUseRegister(0));

                        //Restore the base pointer
                        this.CreateEpilog(compilationData);

                        //Make the return
                        RawAssembler.Return(generatedCode);
                    }
                    break;
                case OpCodes.LoadArgument:
                    {
                        //Load the virtual register with the argument valuie
                        int argOffset = (instruction.IntValue + stackOffset) * -RawAssembler.RegisterSize;
                        var storeReg = GetAssignRegister();

                        virtualAssembler.GenerateOneInstructionMemorySourceInstruction(
                            storeReg,
                            new MemoryOperand(Register.BP, argOffset),
                            Assembler.Move,
                            Assembler.Move);
                    }
                    break;
                case OpCodes.LoadLocal:
                case OpCodes.StoreLocal:
                    {
                        if (instruction.OpCode == OpCodes.LoadLocal)
                        {
                            var valueReg = GetAssignRegister();
                            var localReg = GetUseRegister(0);

                            virtualAssembler.GenerateTwoRegistersInstruction(
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

                            virtualAssembler.GenerateTwoRegistersInstruction(
                                localReg,
                                valueReg,
                                Assembler.Move,
                                Assembler.Move,
                                Assembler.Move);
                        }
                    }
                    break;
                case OpCodes.Branch:
                    Assembler.Jump(generatedCode, JumpCondition.Always, 0);

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
                        virtualAssembler.GenerateTwoRegistersInstruction(
                            op1Reg,
                            op2Reg,
                            Assembler.Compare,
                            Assembler.Compare,
                            Assembler.Compare);

                        JumpCondition condition = JumpCondition.Always;
                        switch (instruction.OpCode)
                        {
                            case OpCodes.BranchEqual:
                                condition = JumpCondition.Equal;
                                break;
                            case OpCodes.BranchNotEqual:
                                condition = JumpCondition.NotEqual;
                                break;
                            case OpCodes.BranchGreaterThan:
                                condition = JumpCondition.GreaterThan;
                                break;
                            case OpCodes.BranchGreaterOrEqual:
                                condition = JumpCondition.GreaterThanOrEqual;
                                break;
                            case OpCodes.BranchLessThan:
                                condition = JumpCondition.LessThan;
                                break;
                            case OpCodes.BranchLessOrEqual:
                                condition = JumpCondition.LessThanOrEqual;
                                break;
                        }

                        Assembler.Jump(generatedCode, condition, 0, unsignedComparison);

                        compilationData.UnresolvedBranches.Add(
                            generatedCode.Count - 6,
                            new UnresolvedBranchTarget(instruction.IntValue, 6));
                    }
                    break;
            }
        }
    }
}
