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
                this.GenerateInstruction(compilationData, function.Instructions[i], i);
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
                * RawAssembler.RegisterSize;

            int stackSize = ((neededStackSize + 15) / 16) * 16;
            compilationData.StackSize = stackSize;

            //Save the base pointer
            RawAssembler.PushRegister(function.GeneratedCode, Register.BP); //push rbp
            RawAssembler.MoveRegisterToRegister(function.GeneratedCode, Register.BP, Register.SP); //mov rbp, rsp

            //Make room for the variables on the stack
            RawAssembler.SubConstantFromRegister(function.GeneratedCode, Register.SP, stackSize); //sub rsp, <size of stack>

            //Move the arguments to the stack
            this.callingConvetions.MoveArgumentsToStack(compilationData);

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
                //Zero rax
                RawAssembler.XorRegisterToRegister(
                    func.GeneratedCode,
                    Register.AX,
                    Register.AX); //xor rax, rax

                for (int i = 0; i < func.Locals.Count; i++)
                {
                    int localOffset = (i + func.Definition.Parameters.Count + 1) * -RawAssembler.RegisterSize;
                    RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                        func.GeneratedCode,
                        Register.BP,
                        localOffset,
                        Register.AX); //mov [rbp-local], rax
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
            RawAssembler.MoveRegisterToRegister(generatedCode, Register.SP, Register.BP); //mov rsp, rbp
            RawAssembler.PopRegister(generatedCode, Register.BP); //pop rbp
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
                    operandStack.PopRegister(Register.AX);
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
                    operandStack.PopRegister(Register.CX);
                    operandStack.PopRegister(Register.AX);

                    switch (instruction.OpCode)
                    {
                        case OpCodes.AddInt:
                            RawAssembler.AddRegisterToRegister(generatedCode, Register.AX, Register.CX, true);
                            break;
                        case OpCodes.SubInt:
                            RawAssembler.SubRegisterFromRegister(generatedCode, Register.AX, Register.CX, true);
                            break;
                        case OpCodes.MulInt:
                            RawAssembler.MultRegisterByRegister(generatedCode, Register.AX, Register.CX, true);
                            break;
                        case OpCodes.DivInt:
                            //This sign extends the eax register
                            generatedCode.Add(0x99); //cdq
                            RawAssembler.DivRegisterFromRegister(generatedCode, Register.AX, Register.CX, true);
                            break;
                    }

                    operandStack.PushRegister(Register.AX);
                    break;
                case OpCodes.AddFloat:
                case OpCodes.SubFloat:
                case OpCodes.MulFloat:
                case OpCodes.DivFloat:
                    operandStack.PopRegister(FloatRegister.XMM1);
                    operandStack.PopRegister(FloatRegister.XMM0);

                    switch (instruction.OpCode)
                    {
                        case OpCodes.AddFloat:
                            RawAssembler.AddRegisterToRegister(generatedCode, FloatRegister.XMM0, FloatRegister.XMM1);
                            break;
                        case OpCodes.SubFloat:
                            RawAssembler.SubRegisterFromRegister(generatedCode, FloatRegister.XMM0, FloatRegister.XMM1);
                            break;
                        case OpCodes.MulFloat:
                            RawAssembler.MultRegisterByRegister(generatedCode, FloatRegister.XMM0, FloatRegister.XMM1);
                            break;
                        case OpCodes.DivFloat:
                            RawAssembler.DivRegisterFromRegister(generatedCode, FloatRegister.XMM0, FloatRegister.XMM1);
                            break;
                    }

                    operandStack.PushRegister(FloatRegister.XMM0);
                    break;
                case OpCodes.Call:
                    {
                        var signature = this.virtualMachine.Binder.FunctionSignature(
                            instruction.StringValue,
                            instruction.Parameters);

                        var funcToCall = this.virtualMachine.Binder.GetFunction(signature);

                        //Align the stack
                        int stackAlignment = this.callingConvetions.CalculateStackAlignment(
                            compilationData,
                            funcToCall.Parameters);

                        if (stackAlignment > 0)
                        {
                            RawAssembler.SubConstantFromRegister(
                                generatedCode,
                                Register.SP,
                                stackAlignment);
                        }

                        //Set the function arguments
                        this.callingConvetions.CallFunctionArguments(compilationData, funcToCall);

                        //Reserve 32 bytes for called function to spill registers
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
                        this.callingConvetions.HandleReturnValue(compilationData, funcToCall);
                    }
                    break;
                case OpCodes.Ret:
                    //Handle the return value
                    this.callingConvetions.MakeReturnValue(compilationData);

                    //Restore the base pointer
                    this.CreateEpilog(compilationData);

                    //Make the return
                    RawAssembler.Return(generatedCode);
                    break;
                case OpCodes.LoadArgument:
                    {
                        //Load rax with the argument
                        int argOffset = (instruction.IntValue + stackOffset) * -RawAssembler.RegisterSize;

                        RawAssembler.MoveMemoryRegisterWithOffsetToRegister(
                            generatedCode,
                            Register.AX,
                            Register.BP,
                            argOffset); //mov rax, [rbp+<arg offset>]

                        //Push the loaded value
                        operandStack.PushRegister(Register.AX);
                    }
                    break;
                case OpCodes.LoadLocal:
                case OpCodes.StoreLocal:
                    {
                        //Load rax with the locals offset
                        int localOffset =
                            (stackOffset + instruction.IntValue + funcDef.Parameters.Count)
                            * -RawAssembler.RegisterSize;

                        if (instruction.OpCode == OpCodes.LoadLocal)
                        {
                            //Load rax with the local
                            RawAssembler.MoveMemoryRegisterWithOffsetToRegister(
                                generatedCode,
                                Register.AX,
                                Register.BP,
                                localOffset); //mov rax, [rbp+<offset>]

                            //Push the loaded value
                            operandStack.PushRegister(Register.AX);
                        }
                        else
                        {
                            //Pop the top operand
                            operandStack.PopRegister(Register.AX);

                            //Store the operand at the given local
                            RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                                generatedCode,
                                Register.BP,
                                localOffset,
                                Register.AX); //mov [rbp+<local offset>], rax
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

                        if (opType.IsPrimitiveType(PrimitiveTypes.Int))
                        {
                            //Pop 2 operands
                            operandStack.PopRegister(Register.CX);
                            operandStack.PopRegister(Register.AX);

                            //Compare
                            RawAssembler.CompareRegisterToRegister(generatedCode, Register.AX, Register.CX); //cmp rax, rcx
                        }
                        else if (opType.IsPrimitiveType(PrimitiveTypes.Float))
                        {
                            //Pop 2 operands
                            operandStack.PopRegister(FloatRegister.XMM1);
                            operandStack.PopRegister(FloatRegister.XMM0);

                            //Compare
                            generatedCode.AddRange(new byte[] { 0x0f, 0x2e, 0xc1 }); //ucomiss xmm0, xmm1
                            unsignedComparison = true;
                        }

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
