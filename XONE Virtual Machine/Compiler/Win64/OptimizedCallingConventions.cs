using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine.Compiler.Win64
{
    /// <summary>
    /// Defines the calling conventions for Windows x64 for the optimized code gen.
    /// </summary>
    /// <remarks>
    /// See <see href="https://en.wikipedia.org/wiki/X86_calling_conventions#Microsoft_x64_calling_convention">link<see/> for more details.
    /// </remarks>
    public class OptimizedCallingConventions
    {
        private static readonly int numRegisterArguments = 4;

        private readonly IntRegister[] intArgumentRegisters = new IntRegister[]
        {
            RegisterCallArguments.Argument0,
            RegisterCallArguments.Argument1,
            RegisterCallArguments.Argument2,
            RegisterCallArguments.Argument3
        };

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

                RawAssembler.MoveMemoryRegisterWithOffsetToRegister(
                    generatedCode,
                    Register.AX,
                    Register.BP,
                    RawAssembler.RegisterSize * (6 + stackArgumentIndex)); //mov rax, [rbp+REG_SIZE*<arg offset>]

                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
                    argStackOffset,
                    Register.AX); //mov [rbp+<arg offset>], rax
            }

            if (argumentIndex == 3)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
                    argStackOffset,
                    RegisterCallArguments.Argument3); //mov [rbp+<arg offset>], <reg arg 3>
            }

            if (argumentIndex == 2)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
                    argStackOffset,
                    RegisterCallArguments.Argument2); //mov [rbp+<arg offset>], <reg arg 2>
            }

            if (argumentIndex == 1)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
                    argStackOffset,
                    RegisterCallArguments.Argument1); //mov [rbp+<arg offset>], <reg arg 1>
            }

            if (argumentIndex == 0)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
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

                RawAssembler.MoveMemoryRegisterWithOffsetToRegister(
                    generatedCode,
                    Register.AX,
                    Register.BP,
                    RawAssembler.RegisterSize * (6 + stackArgumentIndex)); //mov rax, [rbp+REG_SIZE*<arg offset>]

                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
                    argStackOffset,
                    Register.AX); //mov [rbp+<arg offset>], rax
            }

            if (argumentIndex == 3)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
                    argStackOffset,
                    FloatRegisterCallArguments.Argument3); //movss [rbp+<arg offset>], <reg arg 3>
            }

            if (argumentIndex == 2)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
                    argStackOffset,
                    FloatRegisterCallArguments.Argument2); //movss [rbp+<arg offset>], <reg arg 2>
            }

            if (argumentIndex == 1)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
                    argStackOffset,
                    FloatRegisterCallArguments.Argument1); //movss [rbp+<arg offset>], <reg arg 1>
            }

            if (argumentIndex == 0)
            {
                RawAssembler.MoveRegisterToMemoryRegisterWithOffset(
                    generatedCode,
                    Register.BP,
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
        /// <param name="argumentRegisters">The virtual registers for the arguments</param>
        /// <param name="aliveRegistersStack">The alive registers stack</param>
        /// <param name="toCall">The function to call</param>
        public void CallFunctionArgument(
            CompilationData compilationData,
            int argumentIndex, VMType argumentType,
            IReadOnlyList<int> argumentRegisters,
            IDictionary<IntRegister, int> aliveRegistersStack,
            FunctionDefinition toCall)
        {
            var virtualAssembler = compilationData.VirtualAssembler;
            var regAlloc = compilationData.RegisterAllocation;
            var generatedCode = compilationData.Function.GeneratedCode;
            int thisNumArgs = compilationData.Function.Definition.Parameters.Count;
            int numArgs = toCall.Parameters.Count;

            int argsStart = 1 + thisNumArgs + regAlloc.NumSpilledRegisters;

            if (regAlloc.NumSpilledRegisters > 0)
            {
                //argsStart += 1 + (1 + CalculateStackArguments(toCall.Parameters)) % 2;
                argsStart += 1 + (aliveRegistersStack.Count + CalculateStackArguments(toCall.Parameters)) % 2;
            }

            int alignment = this.CalculateStackAlignment(compilationData, toCall.Parameters, aliveRegistersStack.Count);

            var virtualReg = argumentRegisters[numArgs - 1 - argumentIndex];
            var virtualRegStack = regAlloc.GetStackIndex(virtualReg);

            //Check if to pass argument by via stack
            if (argumentIndex >= numRegisterArguments)
            {
                //Move arguments to the stack
                var spillReg = virtualAssembler.GetSpillRegister();
                var argMemory = new MemoryOperand();

                if (virtualRegStack.HasValue)
                {
                    argMemory = new MemoryOperand(
                        Register.BP,
                        virtualAssembler.CalculateStackOffset(virtualRegStack.Value));
                }
                else
                {
                    argMemory = new MemoryOperand(
                        Register.BP,
                        -(argsStart + aliveRegistersStack[virtualAssembler.GetRegisterForVirtual(virtualReg).Value])
                        * RawAssembler.RegisterSize);
                }

                Assembler.Move(generatedCode, spillReg, argMemory);
                Assembler.Push(generatedCode, spillReg);
            }
            else
            {
                if (argumentType.IsPrimitiveType(PrimitiveTypes.Float))
                {

                }
                else
                {
                    var argReg = intArgumentRegisters[argumentIndex];
                    var argMemory = new MemoryOperand();

                    if (virtualRegStack.HasValue)
                    {
                        argMemory = new MemoryOperand(
                            Register.BP,
                            virtualAssembler.CalculateStackOffset(virtualRegStack.Value));
                    }
                    else
                    {
                        argMemory = new MemoryOperand(
                            Register.BP,
                            -(argsStart + aliveRegistersStack[virtualAssembler.GetRegisterForVirtual(virtualReg).Value])
                            * RawAssembler.RegisterSize);
                    }

                    Assembler.Move(generatedCode, argReg, argMemory);
                }
            }
        }

        /// <summary>
        /// Handles the given function call arguments
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="argumentRegisters">The virtual registers for the arguments</param>
        /// <param name="aliveRegistersStack">The alive registers stack</param>
        /// <param name="toCall">The function to call</param>
        public void CallFunctionArguments(
            CompilationData compilationData,
            IReadOnlyList<int> argumentRegisters,
            IDictionary<IntRegister, int> aliveRegistersStack, FunctionDefinition toCall)
        {
            for (int arg = toCall.Parameters.Count - 1; arg >= 0; arg--)
            {
                this.CallFunctionArgument(compilationData, arg, toCall.Parameters[arg], argumentRegisters, aliveRegistersStack, toCall);
            }
        }

        /// <summary>
        /// Calculates the stack alignment
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="parameters">The parameters of the function to call</param>
        /// <param name="numSavedRegisters">The number of saved registers</param>
        public int CalculateStackAlignment(CompilationData compilationData, IReadOnlyList<VMType> parameterTypes, int numSavedRegisters)
        {
            int numStackArgs = this.CalculateStackArguments(parameterTypes);
            return ((numStackArgs + numSavedRegisters) % 2) * RawAssembler.RegisterSize;
        }

        /// <summary>
        /// Makes the return value for a function
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="returnValueRegister">The virtual register where the return value is stored</param>
        public void MakeReturnValue(CompilationData compilationData, int returnValueRegister)
        {
            var def = compilationData.Function.Definition;
            var virtualAssembler = compilationData.VirtualAssembler;

            if (!def.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
            {
                if (def.ReturnType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    //compilationData.OperandStack.PopRegister(FloatRegister.XMM0);
                }
                else
                {
                    virtualAssembler.GenerateTwoRegisterFixedDestinationInstruction(
                       new IntRegister(Register.AX),
                       returnValueRegister,
                       Assembler.Move,
                       Assembler.Move,
                       true);
                }
            }
        }

        /// <summary>
        /// Handles the return value from a function
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="toCall">The function to call</param>
        /// <param name="returnValueRegister">The register to store the return value</param>
        public void HandleReturnValue(CompilationData compilationData, FunctionDefinition toCall, int returnValueRegister)
        {
            //If we have passed arguments via the stack, adjust the stack pointer.
            int numStackArgs = this.CalculateStackArguments(toCall.Parameters);
            var virtualAssembler = compilationData.VirtualAssembler;

            if (numStackArgs > 0)
            {
                RawAssembler.AddConstantToRegister(
                    compilationData.Function.GeneratedCode,
                    Register.SP,
                    numStackArgs * RawAssembler.RegisterSize);
            }

            if (!toCall.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
            {
                if (toCall.ReturnType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    //compilationData.OperandStack.PushRegister(FloatRegister.XMM0);
                }
                else
                {
                    virtualAssembler.GenerateTwoRegisterFixedSourceInstruction(
                        returnValueRegister,
                        Register.AX,
                        Assembler.Move,
                        Assembler.Move,
                        true);
                }
            }
        }
    }
}
