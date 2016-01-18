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
        /// Creates the function prolog
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        private void CreateProlog(CompilationData compilationData)
        {
            var function = compilationData.Function;
            var virtualAssembler = compilationData.VirtualAssembler;

            //Calculate the size of the stack aligned to 16 bytes
            var def = function.Definition;
            int neededStackSize =
                RawAssembler.RegisterSize
                * (def.Parameters.Count + compilationData.RegisterAllocation.NumSpilledRegisters);

            int stackSize = ((neededStackSize + 15) / 16) * 16;
            compilationData.StackSize = stackSize;

            //Save the base pointer
            Assembler.Push(function.GeneratedCode, Register.BP);
            Assembler.Move(function.GeneratedCode, Register.BP, Register.SP);

            //Make room for the variables on the stack
            if (stackSize > 0)
            {
                Assembler.Sub(function.GeneratedCode, Register.SP, stackSize);
            }

            //Move the arguments to the stack
            this.callingConvetions.MoveArgumentsToStack(compilationData);

            if (compilationData.VirtualAssembler.NeedSpillRegister)
            {
                Assembler.Push(function.GeneratedCode, virtualAssembler.GetIntSpillRegister());
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
            var virtualAssembler = compilationData.VirtualAssembler;
            var func = compilationData.Function;

            if (func.Locals.Count > 0)
            {
                if (virtualAssembler.NeedSpillRegister)
                {
                    //Zero the spill register
                    var spillReg = virtualAssembler.GetIntSpillRegister();
                    Assembler.Xor(func.GeneratedCode, spillReg, spillReg);
                }

                foreach (var localRegister in compilationData.LocalVirtualRegisters)
                {
                    //Zero the local register
                    var localReg = virtualAssembler.GetRegisterForVirtual(localRegister);

                    if (localReg.HasValue)
                    {
                        if (localReg.Value.IsInt)
                        {
                            Assembler.Xor(func.GeneratedCode, localReg.Value.IntRegister, localReg.Value.IntRegister);
                        }
                        else
                        {
                            //IntPtr valuePtr = virtualMachine.Compiler.MemoryManager.AllocateReadonly(0);
                            //RawAssembler.MoveMemoryToRegister(func.GeneratedCode, localReg.Value.FloatRegister, valuePtr.ToInt32());
                            Assembler.Push(func.GeneratedCode, 0);
                            Assembler.Pop(func.GeneratedCode, localReg.Value.FloatRegister);
                        }
                    }
                    else
                    {
                        var spillReg = virtualAssembler.GetIntSpillRegister();
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
            var virtualAssembler = compilationData.VirtualAssembler;

            if (compilationData.VirtualAssembler.NeedSpillRegister)
            {
                Assembler.Pop(generatedCode, virtualAssembler.GetIntSpillRegister());
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

            Func<VirtualRegister> GetAssignRegister = () =>
            {
                return virtualInstruction.AssignRegister.Value;
            };

            Func<int, VirtualRegister> GetUseRegister = x =>
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
                case OpCodes.LoadFloat:
                    {
                        var storeReg = GetAssignRegister();
                        var storeRegister = virtualAssembler.GetFloatRegisterForVirtual(storeReg);
                        int floatPattern = BitConverter.ToInt32(BitConverter.GetBytes(instruction.FloatValue), 0);

                        if (storeRegister.HasValue)
                        {
                            //IntPtr valuePtr = virtualMachine.Compiler.MemoryManager.AllocateReadonly(instruction.FloatValue);
                            //RawAssembler.MoveMemoryToRegister(generatedCode, storeRegister.Value, valuePtr.ToInt32());
                            Assembler.Push(generatedCode, floatPattern);
                            Assembler.Pop(generatedCode, storeRegister.Value);
                        }
                        else
                        {
                            //int floatPattern = BitConverter.ToInt32(BitConverter.GetBytes(instruction.FloatValue), 0);
                            var storeStackOffset = virtualAssembler.CalculateStackOffset(storeReg).Value;
                            Assembler.Move(generatedCode, new MemoryOperand(Register.BP, storeStackOffset), floatPattern);
                        }
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
                        bool moveOp1ToStore = true;

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
                                    var spillReg = virtualAssembler.GetIntSpillRegister();
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
                            case OpCodes.DivInt:
                                {
                                    moveOp1ToStore = false;

                                    //The idiv instruction modifies the rdx and rax instruction, so we save them.
                                    var op1Register = virtualAssembler.GetIntRegisterForVirtual(op1Reg);

                                    IList<IntRegister> saveRegisters = null;

                                    if (op1Register.HasValue && op1Register == Register.AX)
                                    {
                                        saveRegisters = virtualAssembler.GetAliveRegisters(index)
                                           .Where(x => x.IsInt && x.IntRegister == new IntRegister(Register.DX))
                                           .Select(x => x.IntRegister)
                                           .ToList();
                                    }
                                    else
                                    {
                                        saveRegisters = virtualAssembler.GetAliveRegisters(index)
                                           .Where(x => x.IsInt && (x == new IntRegister(Register.AX) || x == new IntRegister(Register.DX)))
                                           .Select(x => x.IntRegister)
                                           .ToList();
                                    }

                                    foreach (var register in saveRegisters)
                                    {
                                        Assembler.Push(generatedCode, register);
                                    }

                                    //Move the first operand to rax
                                    virtualAssembler.GenerateTwoRegisterFixedDestinationInstruction(
                                        Register.AX,
                                        op1Reg,
                                        Assembler.Move,
                                        Assembler.Move,
                                        true);

                                    //Move the second operand to the spill register.
                                    //Not moving to a spill register will cause div by zero if the second operand is in the rdx register.
                                    var spillReg = virtualAssembler.GetIntSpillRegister();
                                    var op2Register = virtualAssembler.GetIntRegisterForVirtual(op2Reg);
                                    bool needSpill = false;

                                    if (op2Register.HasValue && op2Register.Value == Register.DX)
                                    {
                                        needSpill = true;
                                        virtualAssembler.GenerateTwoRegisterFixedDestinationInstruction(
                                            spillReg,
                                            op2Reg,
                                            Assembler.Move,
                                            Assembler.Move);
                                    }

                                    //This sign extends the rax register
                                    generatedCode.Add(0x48);
                                    generatedCode.Add(0x99);

                                    //Divide the register
                                    if (needSpill)
                                    {
                                        Assembler.Div(generatedCode, spillReg);
                                    }
                                    else
                                    {
                                        virtualAssembler.GenerateOneRegisterInstruction(
                                            op2Reg,
                                            Assembler.Div,
                                            Assembler.Div);
                                    }

                                    //Move the result
                                    virtualAssembler.GenerateTwoRegisterFixedSourceInstruction(
                                        storeReg,
                                        Register.AX,
                                        Assembler.Move,
                                        Assembler.Move,
                                        true);

                                    //Restore saved registers
                                    var storeRegister = virtualAssembler.GetIntRegisterForVirtual(storeReg);

                                    foreach (var register in saveRegisters.Reverse())
                                    {
                                        if (storeRegister.HasValue)
                                        {
                                            if (storeRegister != Register.AX)
                                            {
                                                Assembler.Pop(generatedCode, register);
                                            }
                                            else
                                            {
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
                        }

                        if (moveOp1ToStore && op1Reg != storeReg)
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
                case OpCodes.AddFloat:
                case OpCodes.SubFloat:
                case OpCodes.MulFloat:
                case OpCodes.DivFloat:
                    {
                        var op2Reg = GetUseRegister(0);
                        var op1Reg = GetUseRegister(1);
                        var storeReg = GetAssignRegister();

                        switch (instruction.OpCode)
                        {
                            case OpCodes.AddFloat:
                                virtualAssembler.GenerateTwoRegistersFloatInstruction(
                                    op1Reg,
                                    op2Reg,
                                    Assembler.Add,
                                    Assembler.Add);
                                break;
                            case OpCodes.SubFloat:
                                virtualAssembler.GenerateTwoRegistersFloatInstruction(
                                    op1Reg,
                                    op2Reg,
                                    Assembler.Sub,
                                    Assembler.Sub);
                                break;
                            case OpCodes.MulFloat:
                                virtualAssembler.GenerateTwoRegistersFloatInstruction(
                                    op1Reg,
                                    op2Reg,
                                    Assembler.Mult,
                                    Assembler.Mult);
                                break;
                            case OpCodes.DivFloat:
                                virtualAssembler.GenerateTwoRegistersFloatInstruction(
                                    op1Reg,
                                    op2Reg,
                                    Assembler.Div,
                                    Assembler.Div);
                                break;
                        }

                        if (op1Reg != storeReg)
                        {
                            virtualAssembler.GenerateTwoRegistersFloatInstruction(
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
                        var aliveRegistersStack = new Dictionary<HardwareRegister, int>();
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
                            Assembler.Sub(generatedCode, Register.SP, stackAlignment);
                        }

                        //Set the function arguments
                        this.callingConvetions.CallFunctionArguments(
                            compilationData,
                            virtualInstruction.UsesRegisters,
                            aliveRegistersStack,
                            funcToCall);

                        //Reserve 32 bytes for the called function to spill registers
                        Assembler.Sub(generatedCode, Register.SP, 32);

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
                        Assembler.Add(generatedCode, Register.SP, stackAlignment + 32);

                        //Hande the return value
                        var returnValueReg = VirtualRegister.Invalid;

                        if (!funcToCall.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
                        {
                            returnValueReg = GetAssignRegister();
                        }

                        this.callingConvetions.HandleReturnValue(compilationData, funcToCall, returnValueReg);
                        var assignRegister = virtualAssembler.GetRegisterForVirtual(returnValueReg);

                        //Restore registers
                        foreach (var register in aliveRegisters.Reverse<HardwareRegister>())
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
                        var returnValueReg = VirtualRegister.Invalid;

                        if (!funcDef.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
                        {
                            returnValueReg = GetUseRegister(0);
                        }

                        this.callingConvetions.MakeReturnValue(compilationData, returnValueReg);

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

                        if (storeReg.Type == VirtualRegisterType.Integer)
                        {
                            virtualAssembler.GenerateOneRegisterMemorySourceInstruction(
                                storeReg,
                                new MemoryOperand(Register.BP, argOffset),
                                Assembler.Move,
                                Assembler.Move);
                        }
                        else
                        {
                            virtualAssembler.GenerateOneRegisterMemorySourceFloatInstruction(
                              storeReg,
                              new MemoryOperand(Register.BP, argOffset),
                              Assembler.Move,
                              Assembler.Move);
                        }
                    }
                    break;
                case OpCodes.LoadLocal:
                    {
                        var valueReg = GetAssignRegister();
                        var localReg = GetUseRegister(0);

                        if (localReg.Type == VirtualRegisterType.Float)
                        {
                            virtualAssembler.GenerateTwoRegistersFloatInstruction(
                                valueReg,
                                localReg,
                                Assembler.Move,
                                Assembler.Move,
                                Assembler.Move);
                        }
                        else
                        {
                            virtualAssembler.GenerateTwoRegistersInstruction(
                                valueReg,
                                localReg,
                                Assembler.Move,
                                Assembler.Move,
                                Assembler.Move);
                        }
                    }
                    break;
                case OpCodes.StoreLocal:
                    {
                        var valueReg = GetUseRegister(0);
                        var localReg = GetAssignRegister();

                        if (localReg.Type == VirtualRegisterType.Float)
                        {
                            virtualAssembler.GenerateTwoRegistersFloatInstruction(
                                localReg,
                                valueReg,
                                Assembler.Move,
                                Assembler.Move,
                                Assembler.Move);
                        }
                        else
                        {
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
                        if (opType.IsPrimitiveType(PrimitiveTypes.Float))
                        {
                            unsignedComparison = true;
                            virtualAssembler.GenerateTwoRegistersFloatInstruction(
                                op1Reg,
                                op2Reg,
                                Assembler.Compare,
                                Assembler.Compare);
                        }
                        else
                        {
                            virtualAssembler.GenerateTwoRegistersInstruction(
                                op1Reg,
                                op2Reg,
                                Assembler.Compare,
                                Assembler.Compare,
                                Assembler.Compare);
                        }

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
