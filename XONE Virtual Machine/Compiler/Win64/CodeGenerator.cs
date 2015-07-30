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
    /// Represents a code generator
    /// </summary>
    public class CodeGenerator
    {
        private readonly VirtualMachine virtualMachine;
        private readonly CallingConvetions callingConvetions = new CallingConvetions();

        /// <summary>
        /// Creates a new code generator
        /// </summary>
        /// <param name="virtualMachine">The virtual machine</param>
        public CodeGenerator(VirtualMachine virtualMachine)
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
            Assembler.MoveLongToRegister(generatedCode, callRegister, toCall.ToInt64());
            Assembler.CallInRegister(generatedCode, callRegister);
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
                if (function.Optimize)
                {
                    this.GenerateOptimizedInstruction(compilationData, compilationData.VirtualInstructions[i], i);
                }
                else
                {
                    this.GenerateInstruction(compilationData, function.Instructions[i], i);
                }
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
                (def.Parameters.Count + function.Locals.Count + compilationData.Function.OperandStackSize)
                * Assembler.RegisterSize;

            int stackSize = ((neededStackSize + 15) / 16) * 16;

            //Save the base pointer
            Assembler.PushRegister(function.GeneratedCode, Registers.BP); //push rbp
            Assembler.MoveRegisterToRegister(function.GeneratedCode, Registers.BP, Registers.SP); //mov rbp, rsp

            //Make room for the variables on the stack
            Assembler.SubConstantFromRegister(function.GeneratedCode, Registers.SP, stackSize); //sub rsp, <size of stack>

            //Move the arguments to the stack
            this.callingConvetions.MoveArgumentsToStack(compilationData);

            //Zero locals
            if (function.Optimize)
            {
                this.ZeroLocalsOptimized(compilationData);
            }
            else
            {
                this.ZeroLocals(compilationData);
            }
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
                //Zero rax
                Assembler.XorRegisterToRegister(
                    func.GeneratedCode,
                    Registers.AX,
                    Registers.AX); //xor rax, rax

                for (int i = 0; i < func.Locals.Count; i++)
                {
                    int localOffset = (i + func.Definition.Parameters.Count + 1) * -Assembler.RegisterSize;
                    Assembler.MoveRegisterToMemoryRegisterWithOffset(
                        func.GeneratedCode,
                        Registers.BP,
                        localOffset,
                        Registers.AX); //mov [rbp-local], rax
                }
            }
        }

        /// <summary>
        /// Zeroes the locals
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        private void ZeroLocalsOptimized(CompilationData compilationData)
        {
            var func = compilationData.Function;

            //TODO: Better local initialization
            var initializedLocals = new HashSet<int>();

            foreach (var instruction in compilationData.VirtualInstructions)
            {
                if (instruction.Instruction.OpCode == OpCodes.LoadLocal
                    && !initializedLocals.Contains(instruction.Instruction.IntValue))
                {
                    var localReg = GetRegister(compilationData.RegisterAllocation.GetRegister(instruction.UsesRegisters[0]));

                    if (localReg.IsBase)
                    {
                        Assembler.XorRegisterToRegister(func.GeneratedCode, localReg.BaseRegister, localReg.BaseRegister);
                    }
                    else
                    {
                        Assembler.XorRegisterToRegister(func.GeneratedCode, localReg.ExtendedRegister, localReg.ExtendedRegister);
                    }

                    initializedLocals.Add(instruction.Instruction.IntValue);
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

            //Restore the base pointer
            Assembler.MoveRegisterToRegister(generatedCode, Registers.SP, Registers.BP); //mov rsp, rbp
            Assembler.PopRegister(generatedCode, Registers.BP); //pop rbp
        }

        /// <summary>
        /// Generates native code for the given instruction
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="instruction">The current instruction</param>
        /// <param name="index">The index of the instruction</param>
        private void GenerateInstruction(CompilationData compilationData, Instruction instruction, int index)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var operandStack = compilationData.OperandStack;
            var funcDef = compilationData.Function.Definition;
            int stackOffset = 1;

            compilationData.InstructionMapping.Add(generatedCode.Count);

            switch (instruction.OpCode)
            {
                case OpCodes.Pop:
                    operandStack.PopRegister(Registers.AX);
                    break;
                case OpCodes.LoadInt:
                    operandStack.PushInt(instruction.IntValue);
                    break;
                case OpCodes.LoadFloat:
                    int floatPattern = BitConverter.ToInt32(BitConverter.GetBytes(instruction.FloatValue), 0);
                    operandStack.PushInt(floatPattern);
                    break;
                case OpCodes.AddInt:
                case OpCodes.SubInt:
                case OpCodes.MulInt:
                case OpCodes.DivInt:
                    operandStack.PopRegister(Registers.CX);
                    operandStack.PopRegister(Registers.AX);

                    switch (instruction.OpCode)
                    {
                        case OpCodes.AddInt:
                            Assembler.AddRegisterToRegister(generatedCode, Registers.AX, Registers.CX, true);
                            break;
                        case OpCodes.SubInt:
                            Assembler.SubRegisterFromRegister(generatedCode, Registers.AX, Registers.CX, true);
                            break;
                        case OpCodes.MulInt:
                            Assembler.MultRegisterToRegister(generatedCode, Registers.AX, Registers.CX, true);
                            break;
                        case OpCodes.DivInt:
                            //This sign extends the eax register
                            generatedCode.Add(0x99); //cdq
                            Assembler.DivRegisterFromRegister(generatedCode, Registers.AX, Registers.CX, true);
                            break;
                    }

                    operandStack.PushRegister(Registers.AX);
                    break;
                case OpCodes.AddFloat:
                case OpCodes.SubFloat:
                case OpCodes.MulFloat:
                case OpCodes.DivFloat:
                    operandStack.PopRegister(FloatRegisters.XMM1);
                    operandStack.PopRegister(FloatRegisters.XMM0);

                    switch (instruction.OpCode)
                    {
                        case OpCodes.AddFloat:
                            Assembler.AddRegisterToRegister(generatedCode, FloatRegisters.XMM0, FloatRegisters.XMM1);
                            break;
                        case OpCodes.SubFloat:
                            Assembler.SubRegisterFromRegister(generatedCode, FloatRegisters.XMM0, FloatRegisters.XMM1);
                            break;
                        case OpCodes.MulFloat:
                            Assembler.MultRegisterToRegister(generatedCode, FloatRegisters.XMM0, FloatRegisters.XMM1);
                            break;
                        case OpCodes.DivFloat:
                            Assembler.DivRegisterFromRegister(generatedCode, FloatRegisters.XMM0, FloatRegisters.XMM1);
                            break;
                    }

                    operandStack.PushRegister(FloatRegisters.XMM0);
                    break;
                case OpCodes.Call:
                    {
                        var signature = this.virtualMachine.Binder.FunctionSignature(
                            instruction.StringValue,
                            instruction.Parameters);

                        var funcToCall = this.virtualMachine.Binder.GetFunction(signature);

                        //Set the function arguments
                        this.callingConvetions.CallFunctionArguments(compilationData, funcToCall);

                        //Align the stack
                        int stackAlignment = this.callingConvetions.CalculateStackAlignment(
                            compilationData,
                            funcToCall.Parameters);

                        Assembler.SubConstantFromRegister(
                            generatedCode,
                            Registers.SP,
                            stackAlignment);

                        //Generate the call
                        if (funcToCall.IsManaged)
                        {
                            //Mark that the function call needs to be patched with the entry point later
                            compilationData.UnresolvedFunctionCalls.Add(new UnresolvedFunctionCall(
                                FunctionCallAddressModes.Relative,
                                funcToCall,
                                generatedCode.Count));

                            Assembler.Call(generatedCode, 0);
                        }
                        else
                        {
                            this.GenerateCall(generatedCode, funcToCall.EntryPoint);
                        }

                        //Unalign the stack
                        Assembler.AddConstantToRegister(
                            generatedCode,
                            Registers.SP,
                            stackAlignment);

                        //Hande the return value
                        this.callingConvetions.HandleReturnValue(compilationData, funcToCall);
                    }
                    break;
                case OpCodes.Ret:
                    //Handle the return value
                    this.callingConvetions.MakeReturnValue(compilationData);

                    //Restore the base pointer
                    this.CreateEpilog(compilationData);

                    //Make the return
                    Assembler.Return(generatedCode);
                    break;
                case OpCodes.LoadArgument:
                    {
                        //Load rax with the argument
                        int argOffset = (instruction.IntValue + stackOffset) * -Assembler.RegisterSize;

                        Assembler.MoveMemoryRegisterWithOffsetToRegister(
                            generatedCode,
                            Registers.AX,
                            Registers.BP,
                            argOffset); //mov rax, [rbp+<arg offset>]

                        //Push the loaded value
                        operandStack.PushRegister(Registers.AX);
                    }
                    break;
                case OpCodes.LoadLocal:
                case OpCodes.StoreLocal:
                    {
                        //Load rax with the locals offset
                        int localOffset =
                            (stackOffset + instruction.IntValue + funcDef.Parameters.Count)
                            * -Assembler.RegisterSize;

                        if (instruction.OpCode == OpCodes.LoadLocal)
                        {
                            //Load rax with the local
                            Assembler.MoveMemoryRegisterWithOffsetToRegister(
                                generatedCode,
                                Registers.AX,
                                Registers.BP,
                                localOffset); //mov rax, [rbp+<offset>]

                            //Push the loaded value
                            operandStack.PushRegister(Registers.AX);
                        }
                        else
                        {
                            //Pop the top operand
                            operandStack.PopRegister(Registers.AX);

                            //Store the operand at the given local
                            Assembler.MoveRegisterToMemoryRegisterWithOffset(
                                generatedCode,
                                Registers.BP,
                                localOffset,
                                Registers.AX); //mov [rbp+<local offset>], rax
                        }
                    }
                    break;
                case OpCodes.Branch:
                    Assembler.Jump(generatedCode, 0); //jmp <target>

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

                        if (opType.IsPrimitiveType(PrimitiveTypes.Int))
                        {
                            //Pop 2 operands
                            operandStack.PopRegister(Registers.CX);
                            operandStack.PopRegister(Registers.AX);

                            //Compare
                            Assembler.CompareRegisterToRegister(generatedCode, Registers.AX, Registers.CX); //cmp rax, rcx
                        }
                        else if (opType.IsPrimitiveType(PrimitiveTypes.Float))
                        {
                            //Pop 2 operands
                            operandStack.PopRegister(FloatRegisters.XMM1);
                            operandStack.PopRegister(FloatRegisters.XMM0);

                            //Compare
                            generatedCode.AddRange(new byte[] { 0x0f, 0x2e, 0xc1 }); //ucomiss xmm0, xmm1
                            unsignedComparison = true;
                        }

                        switch (instruction.OpCode)
                        {
                            case OpCodes.BranchEqual:
                                Assembler.JumpEqual(generatedCode, 0); // je <target>
                                break;
                            case OpCodes.BranchNotEqual:
                                Assembler.JumpNotEqual(generatedCode, 0); // jne <target>
                                break;
                            case OpCodes.BranchGreaterThan:
                                if (unsignedComparison)
                                {
                                    Assembler.JumpGreaterThanUnsigned(generatedCode, 0); // jg <target>
                                }
                                else
                                {
                                    Assembler.JumpGreaterThan(generatedCode, 0); // jg <target>
                                }
                                break;
                            case OpCodes.BranchGreaterOrEqual:
                                if (unsignedComparison)
                                {
                                    Assembler.JumpGreaterThanOrEqualUnsigned(generatedCode, 0); // jge <target>
                                }
                                else
                                {
                                    Assembler.JumpGreaterThanOrEqual(generatedCode, 0); // jge <target>
                                }
                                break;
                            case OpCodes.BranchLessThan:
                                if (unsignedComparison)
                                {
                                    Assembler.JumpLessThanUnsigned(generatedCode, 0); // jl <target>
                                }
                                else
                                {
                                    Assembler.JumpLessThan(generatedCode, 0); // jl <target>
                                }
                                break;
                            case OpCodes.BranchLessOrEqual:
                                if (unsignedComparison)
                                {
                                    Assembler.JumpLessThanOrEqualUnsigned(generatedCode, 0); // jle <target>
                                }
                                else
                                {
                                    Assembler.JumpLessThanOrEqual(generatedCode, 0); // jle <target>
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

        /// <summary>
        /// A none integer register
        /// </summary>
        private struct NoneIntRegister
        {
            public bool IsBase { get; set; }
            public Registers BaseRegister { get; set; }
            public NumberedRegisters ExtendedRegister { get; set; }

            public static bool operator==(NoneIntRegister lhs, NoneIntRegister rhs)
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
                return new NoneIntRegister() { IsBase = false, ExtendedRegister = NumberedRegisters.R8 };
            }
            else if (register == 4)
            {
                return new NoneIntRegister() { IsBase = false, ExtendedRegister = NumberedRegisters.R9 };
            }
            else if (register == 5)
            {
                return new NoneIntRegister() { IsBase = false, ExtendedRegister = NumberedRegisters.R10 };
            }
            else if (register == 6)
            {
                return new NoneIntRegister() { IsBase = false, ExtendedRegister = NumberedRegisters.R11 };
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
            Action<IList<byte>, Registers, Registers> inst1, Action<IList<byte>, NumberedRegisters, NumberedRegisters> inst2,
            Action<IList<byte>, Registers, NumberedRegisters> inst3, Action<IList<byte>, NumberedRegisters, Registers> inst4)
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
        /// Generates optimized native code for the given instruction
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="instruction">The current instruction</param>
        /// <param name="index">The index of the instruction</param>
        private void GenerateOptimizedInstruction(CompilationData compilationData, VirtualInstruction virtualInstruction, int index)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var operandStack = compilationData.OperandStack;
            var funcDef = compilationData.Function.Definition;
            //int stackOffset = 1;

            compilationData.InstructionMapping.Add(generatedCode.Count);

            var instruction = virtualInstruction.Instruction;
            var registerAllocation = compilationData.RegisterAllocation;

            Func<NoneIntRegister> GetAssignRegister = () =>
            {
                return GetRegister(registerAllocation.GetRegister(virtualInstruction.AssignRegister.Value));
            };

            Func<int, NoneIntRegister> GetUseRegister = x =>
            {
                return GetRegister(registerAllocation.GetRegister(virtualInstruction.UsesRegisters[x]));
            };

            switch (instruction.OpCode)
            {
                case OpCodes.LoadInt:
                    {
                        var storeReg = GetAssignRegister();
                       
                        if (storeReg.IsBase)
                        {
                            Assembler.MoveIntToRegister(
                                generatedCode,
                                GetAssignRegister().BaseRegister,
                                instruction.IntValue);
                        }
                        else
                        {
                            Assembler.MoveIntToRegister(
                                generatedCode,
                                GetAssignRegister().ExtendedRegister,
                                instruction.IntValue);
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

                        switch (instruction.OpCode)
                        {
                            case OpCodes.AddInt:
                                GenerateTwoRegistersInstruction(
                                    generatedCode,
                                    op1Reg,
                                    op2Reg,
                                    (gen, x, y) => Assembler.AddRegisterToRegister(gen, x, y),
                                    Assembler.AddRegisterToRegister,
                                    Assembler.AddRegisterToRegister,
                                    Assembler.AddRegisterToRegister);
                                break;
                            case OpCodes.SubInt:
                                GenerateTwoRegistersInstruction(
                                    generatedCode,
                                    op1Reg,
                                    op2Reg,
                                    (gen, x, y) => Assembler.SubRegisterFromRegister(gen, x, y),
                                    Assembler.SubRegisterFromRegister,
                                    Assembler.SubRegisterFromRegister,
                                    Assembler.SubRegisterFromRegister);
                                break;
                            case OpCodes.MulInt:
                                GenerateTwoRegistersInstruction(
                                    generatedCode,
                                    op1Reg,
                                    op2Reg,
                                    (gen, x, y) => Assembler.MultRegisterToRegister(gen, x, y),
                                    Assembler.MultRegisterToRegister,
                                    Assembler.MultRegisterToRegister,
                                    Assembler.MultRegisterToRegister);
                                break;
                            case OpCodes.DivInt:
                                if (op1Reg.BaseRegister != Registers.AX)
                                {
                                    throw new InvalidOperationException("Internal limitation: RAX only supported as destination of division.");
                                }

                                //This sign extends the rax register
                                generatedCode.Add(0x99); //cdq

                                GenerateTwoRegistersInstruction(
                                    generatedCode,
                                    op1Reg,
                                    op2Reg,
                                    (gen, x, y) => Assembler.DivRegisterFromRegister(gen, x, y),
                                    null,
                                    Assembler.DivRegisterFromRegister,
                                    null);
                                break;
                        }

                        if (op1Reg != storeReg)
                        {
                            GenerateTwoRegistersInstruction(
                                generatedCode,
                                storeReg,
                                op1Reg,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister);
                        }
                    }
                    break;
                case OpCodes.Ret:
                    {
                        //Handle the return value
                        var opReg = GetUseRegister(0);

                        if (!(opReg.IsBase && opReg.BaseRegister == Registers.AX))
                        {
                            GenerateTwoRegistersInstruction(
                                generatedCode,
                                new NoneIntRegister() { IsBase = true, BaseRegister = Registers.AX },
                                opReg,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister);
                        }

                        //Restore the base pointer
                        this.CreateEpilog(compilationData);

                        //Make the return
                        Assembler.Return(generatedCode);
                    }
                    break;
                case OpCodes.LoadLocal:
                case OpCodes.StoreLocal:
                    {
                        ////Load rax with the locals offset
                        //int localOffset =
                        //    (stackOffset + instruction.IntValue + funcDef.Parameters.Count)
                        //    * -Assembler.RegisterSize;

                        //if (instruction.OpCode == OpCodes.LoadLocal)
                        //{
                        //    var storeReg = GetAssignRegister().BaseRegister;

                        //    //Load register with the local
                        //    Assembler.MoveMemoryRegisterWithOffsetToRegister(
                        //        generatedCode,
                        //        storeReg,
                        //        Registers.BP,
                        //        localOffset); //mov <reg>, [rbp+<offset>]
                        //}
                        //else
                        //{
                        //    var opReg = GetUseRegister(0).BaseRegister;

                        //    //Store the operand at the given local
                        //    Assembler.MoveRegisterToMemoryRegisterWithOffset(
                        //        generatedCode,
                        //        Registers.BP,
                        //        localOffset,
                        //        opReg); //mov [rbp+<local offset>], <reg>
                        //}

                        if (instruction.OpCode == OpCodes.LoadLocal)
                        {
                            var valueReg = GetAssignRegister();
                            var localReg = GetUseRegister(0);

                            GenerateTwoRegistersInstruction(
                                generatedCode,
                                valueReg,
                                localReg,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister);
                        }
                        else
                        {
                            var valueReg = GetUseRegister(0);
                            var localReg = GetAssignRegister();

                            GenerateTwoRegistersInstruction(
                                generatedCode,
                                localReg,
                                valueReg,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister,
                                Assembler.MoveRegisterToRegister);
                        }
                    }
                    break;
                case OpCodes.Branch:
                    Assembler.Jump(generatedCode, 0); //jmp <target>

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
                                generatedCode,
                                op1Reg,
                                op2Reg,
                                Assembler.CompareRegisterToRegister,
                                Assembler.CompareRegisterToRegister,
                                Assembler.CompareRegisterToRegister,
                                Assembler.CompareRegisterToRegister);

                        switch (instruction.OpCode)
                        {
                            case OpCodes.BranchEqual:
                                Assembler.JumpEqual(generatedCode, 0); // je <target>
                                break;
                            case OpCodes.BranchNotEqual:
                                Assembler.JumpNotEqual(generatedCode, 0); // jne <target>
                                break;
                            case OpCodes.BranchGreaterThan:
                                if (unsignedComparison)
                                {
                                    Assembler.JumpGreaterThanUnsigned(generatedCode, 0); // jg <target>
                                }
                                else
                                {
                                    Assembler.JumpGreaterThan(generatedCode, 0); // jg <target>
                                }
                                break;
                            case OpCodes.BranchGreaterOrEqual:
                                if (unsignedComparison)
                                {
                                    Assembler.JumpGreaterThanOrEqualUnsigned(generatedCode, 0); // jge <target>
                                }
                                else
                                {
                                    Assembler.JumpGreaterThanOrEqual(generatedCode, 0); // jge <target>
                                }
                                break;
                            case OpCodes.BranchLessThan:
                                if (unsignedComparison)
                                {
                                    Assembler.JumpLessThanUnsigned(generatedCode, 0); // jl <target>
                                }
                                else
                                {
                                    Assembler.JumpLessThan(generatedCode, 0); // jl <target>
                                }
                                break;
                            case OpCodes.BranchLessOrEqual:
                                if (unsignedComparison)
                                {
                                    Assembler.JumpLessThanOrEqualUnsigned(generatedCode, 0); // jle <target>
                                }
                                else
                                {
                                    Assembler.JumpLessThanOrEqual(generatedCode, 0); // jle <target>
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
