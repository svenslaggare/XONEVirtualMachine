using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Core
{
    /// <summary>
    /// Represents a verification exception
    /// </summary>
    public class VerificationException : Exception
    {
        /// <summary>
        /// The function being verified
        /// </summary>
        public Function Function { get; }

        /// <summary>
        /// The instruction being verified
        /// </summary>
        public Instruction Instruction { get; }

        /// <summary>
        /// The index of the instruction
        /// </summary>
        public int InstructionIndex { get; }

        /// <summary>
        /// Creates a new verification exception
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="function">The function being verified</param>
        /// <param name="instruction">The instruction being verified</param>
        /// <param name="index">The index of the instruction</param>
        public VerificationException(string message, Function function, Instruction instruction, int index)
            : base($"{index}: {message}")
        {
            this.Function = function;
            this.Instruction = instruction;
            this.InstructionIndex = index;
        }
    }

    /// <summary>
    /// Represents a verifier
    /// </summary>
    public class Verifier
    {
        private readonly VirtualMachine virtualMachine;

        private readonly VMType intType;
        private readonly VMType floatType;
        private readonly VMType voidType;

        /// <summary>
        /// Creates a new verifier
        /// </summary>
        /// <param name="virtualMachine">The virtual machine</param>
        public Verifier(VirtualMachine virtualMachine)
        {
            this.virtualMachine = virtualMachine;
            this.intType = this.virtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Int);
            this.floatType = this.virtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Float);
            this.voidType = this.virtualMachine.TypeProvider.GetPrimitiveType(PrimitiveTypes.Void);
        }

        /// <summary>
        /// Asserts that the given amount of operand exists on the stack.
        /// </summary>
        private void AssertOperandCount(Function function, Instruction instruction, int index, Stack<VMType> operandStack, int count)
        {
            if (operandStack.Count < count)
            {
                throw new VerificationException(
                    $"Expected {count} operands on the stack, but got: {operandStack.Count}.",
                    function, instruction, index);
            }
        }

        /// <summary>
        /// Asserts that the given types are equal
        /// </summary>
        private void AssertSameType(Function function, Instruction instruction, int index, VMType expectedType, VMType actualType)
        {
            if (expectedType != actualType)
            {
                throw new VerificationException(
                    $"Expected type '{expectedType}' but got type '{actualType}'.",
                    function, instruction, index);
            }
        }

        /// <summary>
        /// Verifies the given instruction
        /// </summary>
        /// <param name="function">The function being verified</param>
        /// <param name="instruction">The instruction</param>
        /// <param name="index">The index of the instruction</param>
        /// <param name="operandStack">The operand stack</param>
        private void VerifiyInstruction(Function function, Instruction instruction, int index, Stack<VMType> operandStack)
        {
            switch (instruction.OpCode)
            {
                case OpCodes.Pop:
                    this.AssertOperandCount(function, instruction, index, operandStack, 1);
                    operandStack.Pop();
                    break;
                case OpCodes.LoadInt:
                    operandStack.Push(this.intType);
                    break;
                case OpCodes.LoadFloat:
                    operandStack.Push(this.floatType);
                    break;
                case OpCodes.AddInt:
                case OpCodes.SubInt:
                case OpCodes.MulInt:
                case OpCodes.DivInt:
                    {
                        this.AssertOperandCount(function, instruction, index, operandStack, 2);
                        var op1 = operandStack.Pop();
                        var op2 = operandStack.Pop();

                        this.AssertSameType(function, instruction, index, this.intType, op1);
                        this.AssertSameType(function, instruction, index, this.intType, op2);

                        operandStack.Push(this.intType);
                    }
                    break;
                case OpCodes.AddFloat:
                case OpCodes.SubFloat:
                case OpCodes.MulFloat:
                case OpCodes.DivFloat:
                    {
                        this.AssertOperandCount(function, instruction, index, operandStack, 2);
                        var op1 = operandStack.Pop();
                        var op2 = operandStack.Pop();

                        this.AssertSameType(function, instruction, index, this.floatType, op1);
                        this.AssertSameType(function, instruction, index, this.floatType, op2);

                        operandStack.Push(this.floatType);
                    }
                    break;
                case OpCodes.Call:
                    {
                        var signature = this.virtualMachine.Binder.FunctionSignature(
                            instruction.StringValue,
                            instruction.Parameters);

                        var funcToCall = this.virtualMachine.Binder.GetFunction(signature);

                        //Check that the function exists
                        if (funcToCall == null)
                        {
                            throw new VerificationException(
                                $"There exists no function with the signature '{signature}'.",
                                function, instruction, index);
                        }

                        //Check argument types
                        int numParams = funcToCall.Parameters.Count;
                        this.AssertOperandCount(function, instruction, index, operandStack, numParams);

                        for (int argIndex = numParams - 1; argIndex >= 0; argIndex--)
                        {
                            var op = operandStack.Pop();
                            var arg = funcToCall.Parameters[argIndex];
                            this.AssertSameType(function, instruction, index, arg, op);
                        }

                        if (funcToCall.ReturnType != this.voidType)
                        {
                            operandStack.Push(funcToCall.ReturnType);
                        }
                    }
                    break;
                case OpCodes.Ret:
                    int returnCount = 0;

                    if (function.Definition.ReturnType != this.voidType)
                    {
                        returnCount = 1;
                    }

                    if (operandStack.Count == returnCount)
                    {
                        if (returnCount > 0)
                        {
                            var returnType = operandStack.Pop();
                            
                            if (returnType != function.Definition.ReturnType)
                            {
                                throw new VerificationException(
                                    $"Expected return type of '{function.Definition.ReturnType}' but got type '{returnType}'.",
                                    function, instruction, index);
                            }
                        }
                    }
                    else
                    {
                        throw new VerificationException(
                            $"Expected {returnCount} operand(s) on the stack when returning but got {operandStack.Count} operands.",
                            function, instruction, index);
                    }
                    break;
                case OpCodes.LoadArgument:
                    if (instruction.IntValue >= 0 && instruction.IntValue < function.Definition.Parameters.Count)
                    {
                        operandStack.Push(function.Definition.Parameters[instruction.IntValue]);
                    }
                    else
                    {
                        throw new VerificationException(
                            $"Argument index {instruction.IntValue} is not valid.",
                            function, instruction, index);
                    }
                    break;
                case OpCodes.LoadLocal:
                    if (instruction.IntValue >= 0 && instruction.IntValue < function.Locals.Count)
                    {
                        operandStack.Push(function.Locals[instruction.IntValue]);
                    }
                    else
                    {
                        throw new VerificationException(
                            $"Local index {instruction.IntValue} is not valid.",
                            function, instruction, index);
                    }
                    break;
                case OpCodes.StoreLocal:
                    this.AssertOperandCount(function, instruction, index, operandStack, 1);

                    if (instruction.IntValue >= 0 && instruction.IntValue < function.Locals.Count)
                    {
                        var op = operandStack.Pop();
                        var local = function.Locals[instruction.IntValue];
                        this.AssertSameType(function, instruction, index, local, op);
                    }
                    else
                    {
                        throw new VerificationException(
                            $"Local index {instruction.IntValue} is not valid.",
                            function, instruction, index);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Verifies that given function has the correct semantics
        /// </summary>
        /// <param name="function">The function</param>
        public void VerifiyFunction(Function function)
        {
            var operandStack = new Stack<VMType>();

            for (int i = 0; i < function.Instructions.Count; i++)
            {
                var instruction = function.Instructions[i];

                //Calculate the maximum size of the operand stack
                function.OperandStackSize = Math.Max(function.OperandStackSize, operandStack.Count);

                this.VerifiyInstruction(function, instruction, i, operandStack);

                if (i == function.Instructions.Count - 1)
                {
                    if (instruction.OpCode != OpCodes.Ret)
                    {
                        throw new VerificationException(
                            "Functions must end with a return instruction.",
                            function, instruction, i);
                    }
                }
            }
        }
    }
}
