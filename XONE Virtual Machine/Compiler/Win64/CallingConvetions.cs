using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine.Compiler.Win64
{
    /// <summary>
    /// The float register call arguments
    /// </summary>
    public static class FloatRegisterCallArguments
    {
        /// <summary>
        /// The first argument
        /// </summary>
        public const FloatRegisters Argument0 = FloatRegisters.XMM0;

        /// <summary>
        /// The second argument
        /// </summary>
        public const FloatRegisters Argument1 = FloatRegisters.XMM1;

        /// <summary>
        /// The third argument
        /// </summary>
        public const FloatRegisters Argument2 = FloatRegisters.XMM2;

        /// <summary>
        /// The fourth argument
        /// </summary>
        public const FloatRegisters Argument3 = FloatRegisters.XMM3;
    }

    /// <summary>
    /// The register call arguments
    /// </summary>
    public static class RegisterCallArguments
    {
        /// <summary>
        /// The first argument
        /// </summary>
        public const Registers Argument0 = Registers.CX;

        /// <summary>
        /// The second argument
        /// </summary>
        public const Registers Argument1 = Registers.DX;

        /// <summary>
        /// The third argument
        /// </summary>
        public const ExtendedRegisters Argument2 = ExtendedRegisters.R8;

        /// <summary>
        /// The fourth argument
        /// </summary>
        public const ExtendedRegisters Argument3 = ExtendedRegisters.R9;
    }

    /// <summary>
    /// Defines the calling conventions for Windows x64.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://en.wikipedia.org/wiki/X86_calling_conventions#Microsoft_x64_calling_convention">link<see/> for more details.
    /// </remarks>
    public class CallingConvetions
    {
        private static readonly int numRegisterArguments = 4;

        /// <summary>
        /// Returns the stack argument index for the argument
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="argumentIndex">The argument index</param>
        private int GetStackArgumentIndex(CompilationData compilationData, int argumentIndex)
        {
            int stackArgIndex = 0;
            var parameterTypes = compilationData.Function.Definition.Parameters;

            int index = 0;
            foreach (var parameterType in parameterTypes)
            {
                if (index == argumentIndex)
                {
                    break;
                }

                if (index >= numRegisterArguments)
                {
                    stackArgIndex++;
                }

                index++;
            }

            return stackArgIndex;
        }

        /// <summary>
        /// Moves a none float argument to the stack
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="argumentIndex">The argument index</param>
        private void MoveNoneFloatArgumentToStack(CompilationData compilationData, int argumentIndex)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            int argStackOffset = -(1 + argumentIndex) * RawAssembler.RegisterSize;

            if (argumentIndex >= numRegisterArguments)
            {
                int stackArgumentIndex = this.GetStackArgumentIndex(compilationData, argumentIndex);
                int stackAlignment = this.CalculateStackAlignment(
                    compilationData,
                    compilationData.Function.Definition.Parameters);

                RawAssembler.MoveMemoryRegisterWithOffsetToRegister(
                    generatedCode,
                    Registers.AX,
                    Registers.BP,
                    RawAssembler.RegisterSize * (2 + stackArgumentIndex) + stackAlignment); //mov rax, [rbp+REG_SIZE*<arg offset>]

                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    Registers.AX); //mov [rbp+<arg offset>], rax
            }

            if (argumentIndex == 3)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    RegisterCallArguments.Argument3); //mov [rbp+<arg offset>], <reg arg 3>
            }

            if (argumentIndex == 2)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    RegisterCallArguments.Argument2); //mov [rbp+<arg offset>], <reg arg 2>
            }

            if (argumentIndex == 1)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    RegisterCallArguments.Argument1); //mov [rbp+<arg offset>], <reg arg 1>
            }

            if (argumentIndex == 0)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    RegisterCallArguments.Argument0); //mov [rbp+<arg offset>], <reg arg 0>
            }
        }

        /// <summary>
        /// Moves a float argument to the stack
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="argumentIndex">The argument index</param>
        private void MoveFloatArgumentToStack(CompilationData compilationData, int argumentIndex)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            int argStackOffset = -(1 + argumentIndex) * RawAssembler.RegisterSize;

            if (argumentIndex >= 4)
            {
                int stackArgumentIndex = this.GetStackArgumentIndex(compilationData, argumentIndex);
                int stackAlignment = this.CalculateStackAlignment(
                    compilationData,
                    compilationData.Function.Definition.Parameters);

                RawAssembler.MoveMemoryRegisterWithOffsetToRegister(
                    generatedCode,
                    Registers.AX,
                    Registers.BP,
                    RawAssembler.RegisterSize * (2 + stackArgumentIndex) + stackAlignment); //mov rax, [rbp+REG_SIZE*<arg offset>]

                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    Registers.AX); //mov [rbp+<arg offset>], rax
            }

            if (argumentIndex == 3)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    FloatRegisterCallArguments.Argument3); //movss [rbp+<arg offset>], <reg arg 3>
            }

            if (argumentIndex == 2)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    FloatRegisterCallArguments.Argument2); //movss [rbp+<arg offset>], <reg arg 2>
            }

            if (argumentIndex == 1)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    FloatRegisterCallArguments.Argument1); //movss [rbp+<arg offset>], <reg arg 1>
            }

            if (argumentIndex == 0)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Registers.BP,
                    argStackOffset,
                    FloatRegisterCallArguments.Argument0); //movss [rbp+<arg offset>], <reg arg 0>
            }
        }

        /// <summary>
        /// Moves the argument to the stack
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        public void MoveArgumentsToStack(CompilationData compilationData)
        {
            var function = compilationData.Function;
            var parameterTypes = function.Definition.Parameters;

            for (int argumentIndex = parameterTypes.Count - 1; argumentIndex >= 0; argumentIndex--)
            {
                if (parameterTypes[argumentIndex].IsPrimitiveType(PrimitiveTypes.Float))
                {
                    this.MoveFloatArgumentToStack(
                        compilationData,
                        argumentIndex);
                }
                else
                {
                    this.MoveNoneFloatArgumentToStack(
                        compilationData,
                        argumentIndex);
                }
            }
        }

        /// <summary>
        /// Calculates the number of argument that are passed via the stack
        /// </summary>
        /// <param name="parameterTypes">The parameter types</param>
        private int CalculateStackArguments(IReadOnlyList<VMType> parameterTypes)
        {
            int stackArgs = 0;

            int argIndex = 0;
            foreach (var parameterType in parameterTypes)
            {
                if (argIndex >= numRegisterArguments)
                {
                    stackArgs++;
                }

                argIndex++;
            }

            return stackArgs;
        }

        /// <summary>
        /// Handles the given function call argument
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="argumentIndex">The index of the argument</param>
        /// <param name="argumentType">The type of the argument</param>
        /// <param name="toCall">The function to call</param>
        public void CallFunctionArgument(CompilationData compilationData, int argumentIndex, VMType argumentType, FunctionDefinition toCall)
        {
            var operandStack = compilationData.OperandStack;

            //Check if to pass argument by via stack
            if (argumentIndex >= numRegisterArguments)
            {
                //Move from the operand stack to the normal stack
                operandStack.PopRegister(Registers.AX);
                RawAssembler.PushRegister(compilationData.Function.GeneratedCode, Registers.AX);
            }
            else
            {
                if (argumentType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    if (argumentIndex == 3)
                    {
                        operandStack.PopRegister(FloatRegisterCallArguments.Argument3);
                    }

                    if (argumentIndex == 2)
                    {
                        operandStack.PopRegister(FloatRegisterCallArguments.Argument2);
                    }

                    if (argumentIndex == 1)
                    {
                        operandStack.PopRegister(FloatRegisterCallArguments.Argument1);
                    }

                    if (argumentIndex == 0)
                    {
                        operandStack.PopRegister(FloatRegisterCallArguments.Argument0);
                    }
                }
                else
                {
                    if (argumentIndex == 3)
                    {
                        operandStack.PopRegister(RegisterCallArguments.Argument3);
                    }

                    if (argumentIndex == 2)
                    {
                        operandStack.PopRegister(RegisterCallArguments.Argument2);
                    }

                    if (argumentIndex == 1)
                    {
                        operandStack.PopRegister(RegisterCallArguments.Argument1);
                    }

                    if (argumentIndex == 0)
                    {
                        operandStack.PopRegister(RegisterCallArguments.Argument0);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the given function call arguments
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="toCall">The function to call</param>
        public void CallFunctionArguments(CompilationData compilationData, FunctionDefinition toCall)
        {
            for (int arg = toCall.Parameters.Count - 1; arg >= 0; arg--)
            {
                this.CallFunctionArgument(compilationData, arg, toCall.Parameters[arg], toCall);
            }
        }

        /// <summary>
        /// Calculates the stack alignment
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="parameters">The parameters of the function to call</param>
        public int CalculateStackAlignment(CompilationData compilationData, IReadOnlyList<VMType> parameterTypes)
        {
            int numStackArgs = this.CalculateStackArguments(parameterTypes);
            return ((numStackArgs % 2) + 4) * RawAssembler.RegisterSize;
        }

        /// <summary>
        /// Makes the return value for a function
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        public void MakeReturnValue(CompilationData compilationData)
        {
            var def = compilationData.Function.Definition;

            if (!def.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
            {
                if (def.ReturnType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    compilationData.OperandStack.PopRegister(FloatRegisters.XMM0);
                }
                else
                {
                    compilationData.OperandStack.PopRegister(Registers.AX);
                }
            }
        }

        /// <summary>
        /// Handles the return value from a function
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="toCall">The function to call</param>
        public void HandleReturnValue(CompilationData compilationData, FunctionDefinition toCall)
        {
            //If we have passed arguments via the stack, adjust the stack pointer.
            int numStackArgs = this.CalculateStackArguments(toCall.Parameters);

            if (numStackArgs > 0)
            {
                RawAssembler.AddConstantToRegister(
                    compilationData.Function.GeneratedCode,
                    Registers.SP,
                    numStackArgs * RawAssembler.RegisterSize);
            }

            if (!toCall.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
            {
                if (toCall.ReturnType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    compilationData.OperandStack.PushRegister(FloatRegisters.XMM0);
                }
                else
                {
                    compilationData.OperandStack.PushRegister(Registers.AX);
                }
            }
        }
    }
}
