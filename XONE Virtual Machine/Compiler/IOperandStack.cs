using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine.Compiler.Win64;

namespace XONEVirtualMachine.Compiler
{
    /// <summary>
    /// Represents an operand stack
    /// </summary>
    public interface IOperandStack
    {
        /// <summary>
        /// Returns the number of operands on the stack
        /// </summary>
        int NumStackOperands { get; }

        /// <summary>
        /// Pops an operand from the operand stack to the given register
        /// </summary>
        /// <param name="register">The register to pop to</param>
        void PopRegister(Registers register);

        /// <summary>
        /// Pops an operand from the operand stack to the given register
        /// </summary>
        /// <param name="register">The register to pop to</param>
        void PopRegister(NumberedRegisters register);

        /// <summary>
        /// Pops an operand from the operand stack to the given register
        /// </summary>
        /// <param name="register">The register to pop to</param>
        void PopRegister(FloatRegisters register);

        /// <summary>
        /// Pushes the given value to the operand stack
        /// </summary>
        /// <param name="value">The value</param>
        void PushInt(int value);

        /// <summary>
        /// Pushes the given register to the operand stack
        /// </summary>
        /// <param name="register">The register</param>
        void PushRegister(Registers register);

        /// <summary>
        /// Pushes the given register to the operand stack
        /// </summary>
        /// <param name="register">The register</param>
        void PushRegister(FloatRegisters register);
    }
}
