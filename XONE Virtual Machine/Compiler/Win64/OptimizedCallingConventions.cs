﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpAssembler.x64;
using XONEVirtualMachine.Compiler.Analysis;
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

        private readonly FloatRegister[] floatArgumentRegisters = new FloatRegister[]
        {
            FloatRegisterCallArguments.Argument0,
            FloatRegisterCallArguments.Argument1,
            FloatRegisterCallArguments.Argument2,
            FloatRegisterCallArguments.Argument3
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
        /// Moves an argument to the stack
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="argumentIndex">The argument index</param>
        /// <param name="argumentType">The type of the argument</param>
        private void MoveArgumentToStack(CompilationData compilationData, int argumentIndex, VMType argumentType)
        {
            var generatedCode = compilationData.Function.GeneratedCode;
            int argStackOffset = -(1 + argumentIndex) * Assembler.RegisterSize;

            if (argumentIndex >= numRegisterArguments)
            {
                int stackArgumentIndex = this.GetStackArgumentIndex(compilationData, argumentIndex);

                var argStackSource = new MemoryOperand(
                    Register.BP,
                    Assembler.RegisterSize * (6 + stackArgumentIndex));

                HardwareRegister tmpReg;

                if (argumentType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    tmpReg = compilationData.VirtualAssembler.GetFloatSpillRegister();
                }
                else
                {
                    tmpReg = new IntRegister(Register.AX);
                }

                Assembler.Move(generatedCode, tmpReg, argStackSource);
                Assembler.Move(generatedCode, new MemoryOperand(Register.BP, argStackOffset), tmpReg);
            }
            else
            {
                //var argReg = floatArgumentRegisters[argumentIndex];
                HardwareRegister argReg;

                if (argumentType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    argReg = floatArgumentRegisters[argumentIndex];
                }
                else
                {
                    argReg = intArgumentRegisters[argumentIndex];
                }

                Assembler.Move(generatedCode, new MemoryOperand(Register.BP, argStackOffset), argReg);
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
                this.MoveArgumentToStack(compilationData, argumentIndex, parameterTypes[argumentIndex]);
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
            IReadOnlyList<VirtualRegister> argumentRegisters,
            IDictionary<HardwareRegister, int> aliveRegistersStack,
            FunctionDefinition toCall)
        {
            var virtualAssembler = compilationData.VirtualAssembler;
            var regAlloc = compilationData.RegisterAllocation;
            var generatedCode = compilationData.Function.GeneratedCode;
            int thisNumArgs = compilationData.Function.Definition.Parameters.Count;
            int numArgs = toCall.Parameters.Count;

            int argsStart = 1 + compilationData.StackSize / Assembler.RegisterSize;

            if (virtualAssembler.NeedSpillRegister)
            {
                argsStart += 1;
            }

            int alignment = this.CalculateStackAlignment(compilationData, toCall.Parameters, aliveRegistersStack.Count);

            var virtualReg = argumentRegisters[numArgs - 1 - argumentIndex];
            var virtualRegStack = regAlloc.GetStackIndex(virtualReg);

            //Check if to pass argument by via the stack
            if (argumentIndex >= numRegisterArguments)
            {
                //Move arguments to the stack
                HardwareRegister spillReg;
                int stackOffset = 0;

                if (argumentType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    spillReg = virtualAssembler.GetFloatSpillRegister();

                    if (!virtualRegStack.HasValue)
                    {
                        stackOffset = aliveRegistersStack[virtualAssembler.GetFloatRegisterForVirtual(virtualReg).Value];
                    }
                }
                else
                {
                    spillReg = virtualAssembler.GetIntSpillRegister();

                    if (!virtualRegStack.HasValue)
                    {
                        stackOffset = aliveRegistersStack[virtualAssembler.GetIntRegisterForVirtual(virtualReg).Value];
                    }
                }

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
                        -(argsStart + stackOffset)
                        * Assembler.RegisterSize);
                }

                Assembler.Move(generatedCode, spillReg, argMemory);
                Assembler.Push(generatedCode, spillReg);
            }
            else
            {
                HardwareRegister argReg;
                int stackOffset = 0;

                if (argumentType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    argReg = floatArgumentRegisters[argumentIndex];

                    if (!virtualRegStack.HasValue)
                    {
                        stackOffset = aliveRegistersStack[virtualAssembler.GetFloatRegisterForVirtual(virtualReg).Value];
                    }
                }
                else
                {
                    argReg = intArgumentRegisters[argumentIndex];

                    if (!virtualRegStack.HasValue)
                    {
                        stackOffset = aliveRegistersStack[virtualAssembler.GetIntRegisterForVirtual(virtualReg).Value];
                    }
                }

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
                        -(argsStart + stackOffset)
                        * Assembler.RegisterSize);
                }

                Assembler.Move(generatedCode, argReg, argMemory);
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
            IReadOnlyList<VirtualRegister> argumentRegisters,
            IDictionary<HardwareRegister, int> aliveRegistersStack, FunctionDefinition toCall)
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
            return ((numStackArgs + numSavedRegisters) % 2) * Assembler.RegisterSize;
        }

        /// <summary>
        /// Makes the return value for a function
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="returnValueRegister">The virtual register where the return value is stored</param>
        public void MakeReturnValue(CompilationData compilationData, VirtualRegister returnValueRegister)
        {
            var def = compilationData.Function.Definition;
            var virtualAssembler = compilationData.VirtualAssembler;

            if (!def.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
            {
                if (def.ReturnType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    virtualAssembler.GenerateTwoRegisterFixedDestinationInstruction(
                       FloatRegister.XMM0,
                       returnValueRegister,
                       Assembler.Move,
                       Assembler.Move,
                       true);
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
        public void HandleReturnValue(CompilationData compilationData, FunctionDefinition toCall, VirtualRegister returnValueRegister)
        {
            //If we have passed arguments via the stack, adjust the stack pointer.
            int numStackArgs = this.CalculateStackArguments(toCall.Parameters);
            var virtualAssembler = compilationData.VirtualAssembler;

            if (numStackArgs > 0)
            {
                Assembler.Add(
                    compilationData.Function.GeneratedCode,
                    Register.SP,
                    numStackArgs * Assembler.RegisterSize);
            }

            if (!toCall.ReturnType.IsPrimitiveType(PrimitiveTypes.Void))
            {
                if (toCall.ReturnType.IsPrimitiveType(PrimitiveTypes.Float))
                {
                    virtualAssembler.GenerateTwoRegisterFixedSourceInstruction(
                        returnValueRegister,
                        FloatRegister.XMM0,
                        Assembler.Move,
                        Assembler.Move,
                        true);
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
