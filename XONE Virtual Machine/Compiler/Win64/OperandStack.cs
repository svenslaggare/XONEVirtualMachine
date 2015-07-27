using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Core;

namespace XONEVirtualMachine.Compiler.Win64
{
    /// <summary>
    /// Represents an operand stack
    /// </summary>
    public class OperandStack : IOperandStack
    {
        private readonly Function function;
        private int operandTopIndex;

        /// <summary>
        /// Creates a new operand stack
        /// </summary>
        /// <param name="function">The function</param>
        public OperandStack(Function function)
        {
            this.function = function;
            this.operandTopIndex = -1;
        }

        /// <summary>
        /// Returns the number of operands on the stack
        /// </summary>
        public int NumStackOperands
        {
            get { return this.operandTopIndex + 1; }
        }

        /// <summary>
        /// Asserts that the operand stack is not empty
        /// </summary>
        private void AssertNotEmpty()
        {
            if (this.operandTopIndex <= -1)
            {
                throw new InvalidOperationException("The operand stack is empty.");
            }
        }

        /// <summary>
        /// Calculates the offset in the stack frame for the given stack operand
        /// </summary>
        /// <param name="operandStackIndex">The index of the stack operand</param>
        private int GetStackOperandOffset(int operandStackIndex)
        {
            return 
                -Assembler.RegisterSize
                * (1 + this.function.Locals.Count + this.function.Definition.Parameters.Count + operandStackIndex);
        }

        /// <summary>
        /// Pops an operand from the operand stack to the given register
        /// </summary>
        /// <param name="register">The register to pop to</param>
        public void PopRegister(Registers register)
        {
            this.AssertNotEmpty();

            int stackOffset = GetStackOperandOffset(this.operandTopIndex);
            Assembler.MoveMemoryRegisterWithOffsetToRegister(
                this.function.GeneratedCode,
                register,
                Registers.BP,
                stackOffset); //mov <reg>, [rbp+<operand offset>]
            this.operandTopIndex--;
        }

        /// <summary>
        /// Pops an operand from the operand stack to the given register
        /// </summary>
        /// <param name="register">The register</param>
        public void PopRegister(NumberedRegisters register)
        {
            this.AssertNotEmpty();

            int stackOffset = GetStackOperandOffset(this.operandTopIndex);

            if (Assembler.IsValidByteValue(stackOffset))
            {
                //mov <reg>, [rbp+<operand offset>]
                this.function.GeneratedCode.AddRange(new byte[]
                {
                    0x4c, 0x8b, (byte)(0x45 | ((byte)register << 3)), (byte)stackOffset
                });
            }
            else
            {
                //mov <reg>, [rbp+<operand offset>]
                this.function.GeneratedCode.AddRange(new byte[]
                {
                    0x4c, 0x8b, (byte)(0x85 | ((byte)register << 3))
                });

                foreach (var component in BitConverter.GetBytes(stackOffset))
                {
                    this.function.GeneratedCode.Add(component);
                }
            }

            this.operandTopIndex--;
        }

        /// <summary>
        /// Pops an operand from the operand stack to the given register
        /// </summary>
        /// <param name="register">The register</param>
        public void PopRegister(FloatRegisters register)
        {
            this.AssertNotEmpty();

            int stackOffset = GetStackOperandOffset(this.operandTopIndex);

            if (Assembler.IsValidByteValue(stackOffset))
            {
                //movss <reg>, [rbp+<operand offset>]
                this.function.GeneratedCode.AddRange(new byte[]
                {
                    0xf3, 0x0f, 0x10, (byte)(0x45 | ((byte)register << 3)), (byte)stackOffset
                });
            }
            else
            {
                //mov <reg>, [rbp+<operand offset>]
                this.function.GeneratedCode.AddRange(new byte[]
                {
                   0xf3, 0x0f, 0x10, (byte)(0x85 | ((byte)register << 3))
                });

                foreach (var component in BitConverter.GetBytes(stackOffset))
                {
                    this.function.GeneratedCode.Add(component);
                }
            }

            this.operandTopIndex--;
        }

        /// <summary>
        /// Pushes the given register to the operand stack
        /// </summary>
        /// <param name="register">The register</param>
        public void PushRegister(Registers register)
        {
            this.operandTopIndex++;
            int stackOffset = GetStackOperandOffset(this.operandTopIndex);

            //mov [rbp+<operand offset>], <reg>
            Assembler.MoveRegisterToMemoryRegisterWithOffset(
                this.function.GeneratedCode,
                Registers.BP,
                stackOffset,
                register);
        }

        /// <summary>
        /// Pushes the given register to the operand stack
        /// </summary>
        /// <param name="register">The register</param>
        public void PushRegister(FloatRegisters register)
        {
            this.operandTopIndex++;
            int stackOffset = GetStackOperandOffset(this.operandTopIndex);

            //movss [rbp+<operand offset>], <reg>
            Assembler.MoveRegisterToMemoryRegisterWithOffset(
                this.function.GeneratedCode,
                Registers.BP,
                stackOffset,
                register);
        }

        /// <summary>
        /// Pushes the given value to the operand stack
        /// </summary>
        /// <param name="value">The value</param>
        public void PushInt(int value)
        {
            this.operandTopIndex++;
            int stackOffset = GetStackOperandOffset(this.operandTopIndex);

            //mov [rbp+<operand offset>], value
            if (Assembler.IsValidByteValue(stackOffset))
            {
                this.function.GeneratedCode.AddRange(new byte[]
                {
                    0x48, 0xc7, 0x45, (byte)stackOffset
                });
            }
            else
            {
                this.function.GeneratedCode.AddRange(new byte[]
                {
                    0x48, 0xc7, 0x85
                });

                foreach (var component in BitConverter.GetBytes(stackOffset))
                {
                    function.GeneratedCode.Add(component);
                }
            }

            foreach (var component in BitConverter.GetBytes(value))
            {
                function.GeneratedCode.Add(component);
            }
        }
    }
}
