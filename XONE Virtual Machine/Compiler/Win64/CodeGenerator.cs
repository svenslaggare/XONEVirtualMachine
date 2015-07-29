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

            RegisterAllocation registerAllocation = null;
            IList<VirtualRegisterInstruction> virtualInstructions = null;

            if (function.Optimize)
            {
                virtualInstructions = VirtualRegisters.Create(function.Instructions);
                registerAllocation = LinearScanRegisterAllocation.Allocate(
                    LivenessAnalysis.ComputeLiveness(VirtualControlFlowGraph.FromBasicBlocks(
                        VirtualBasicBlock.CreateBasicBlocks(new ReadOnlyCollection<VirtualRegisterInstruction>(virtualInstructions)))));
            }

            for (int i = 0; i < function.Instructions.Count; i++)
            {
                if (function.Optimize)
                {
                    this.GenerateOptimizedInstruction(compilationData, registerAllocation, virtualInstructions[i], i);
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
                            Assembler.SubRegisterToRegister(generatedCode, Registers.AX, Registers.CX, true);
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

        private struct RegisterEither
        {
            public bool IsLeft { get; set; }
            public Registers LeftReg { get; set; }
            public Registers RightRef { get; set; }
        }

        private RegisterEither GetRegister(int register)
        {
            if (register == 0)
            {
                return new RegisterEither() { IsLeft = true, LeftReg = Registers.AX };
            }

            if (register == 1)
            {
                return new RegisterEither() { IsLeft = true, LeftReg = Registers.CX };
            }

            if (register == 2)
            {
                return new RegisterEither() { IsLeft = true, LeftReg = Registers.DX };
            }

            return new RegisterEither();
        }

        /// <summary>
        /// Generates optimized native code for the given instruction
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="registerAllocation">The register allocation data</param>
        /// <param name="instruction">The current instruction</param>
        /// <param name="index">The index of the instruction</param>
        private void GenerateOptimizedInstruction(CompilationData compilationData, RegisterAllocation registerAllocation,
            VirtualRegisterInstruction virtualRegister, int index)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            var operandStack = compilationData.OperandStack;
            var funcDef = compilationData.Function.Definition;
            int stackOffset = 1;

            compilationData.InstructionMapping.Add(generatedCode.Count);

            var instruction = virtualRegister.Instruction;

            switch (instruction.OpCode)
            {
                case OpCodes.LoadInt:
                    Assembler.MoveIntToRegister(
                        generatedCode,
                        GetRegister(registerAllocation.GetRegister(virtualRegister.AssignRegister.Value) ?? 0).LeftReg,
                        instruction.IntValue);
                    break;
                case OpCodes.AddInt:
                case OpCodes.SubInt:
                case OpCodes.MulInt:
                case OpCodes.DivInt:
                    {
                        var op2Reg = GetRegister(registerAllocation.GetRegister(virtualRegister.UsesRegisters[0]) ?? 0).LeftReg;
                        var op1Reg = GetRegister(registerAllocation.GetRegister(virtualRegister.UsesRegisters[1]) ?? 0).LeftReg;
                        var storeReg = GetRegister(registerAllocation.GetRegister(virtualRegister.AssignRegister.Value) ?? 0).LeftReg;

                        switch (instruction.OpCode)
                        {
                            case OpCodes.AddInt:
                                Assembler.AddRegisterToRegister(generatedCode, op1Reg, op2Reg, true);
                                break;
                            case OpCodes.SubInt:
                                Assembler.SubRegisterToRegister(generatedCode, op1Reg, op2Reg, true);
                                break;
                            case OpCodes.MulInt:
                                Assembler.MultRegisterToRegister(generatedCode, op1Reg, op2Reg, true);
                                break;
                            case OpCodes.DivInt:
                                //This sign extends the eax register
                                generatedCode.Add(0x99); //cdq
                                Assembler.DivRegisterFromRegister(generatedCode, op1Reg, op2Reg, true);
                                break;
                        }

                        if (op1Reg != storeReg)
                        {
                            Assembler.MoveRegisterToRegister(generatedCode, storeReg, op1Reg);
                        }
                    }
                    break;
                case OpCodes.Ret:
                    {
                        //Handle the return value
                        var opReg = GetRegister(registerAllocation.GetRegister(virtualRegister.UsesRegisters[0]) ?? 0).LeftReg;

                        if (opReg != Registers.AX)
                        {
                            Assembler.MoveRegisterToRegister(generatedCode, Registers.AX, opReg);
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
                        //Load rax with the locals offset
                        int localOffset =
                            (stackOffset + instruction.IntValue + funcDef.Parameters.Count)
                            * -Assembler.RegisterSize;

                        if (instruction.OpCode == OpCodes.LoadLocal)
                        {
                            var storeReg = GetRegister(registerAllocation.GetRegister(virtualRegister.AssignRegister.Value) ?? 0).LeftReg;

                            //Load register with the local
                            Assembler.MoveMemoryRegisterWithOffsetToRegister(
                                generatedCode,
                                storeReg,
                                Registers.BP,
                                localOffset); //mov <reg>, [rbp+<offset>]
                        }
                        else
                        {
                            var opReg = GetRegister(registerAllocation.GetRegister(virtualRegister.UsesRegisters[0]) ?? 0).LeftReg;

                            //Store the operand at the given local
                            Assembler.MoveRegisterToMemoryRegisterWithOffset(
                                generatedCode,
                                Registers.BP,
                                localOffset,
                                opReg); //mov [rbp+<local offset>], <reg>
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

                        var op2Reg = GetRegister(registerAllocation.GetRegister(virtualRegister.UsesRegisters[0]) ?? 0).LeftReg;
                        var op1Reg = GetRegister(registerAllocation.GetRegister(virtualRegister.UsesRegisters[1]) ?? 0).LeftReg;

                        //Compare
                        Assembler.CompareRegisterToRegister(generatedCode, op1Reg, op2Reg);

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
