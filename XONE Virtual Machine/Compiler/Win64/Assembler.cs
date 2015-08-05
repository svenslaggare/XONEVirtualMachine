using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Compiler.Win64
{
    /// <summary>
    /// Represents an integer register
    /// </summary>
    public struct IntRegister
    {
        /// <summary>
        /// Indicates if the register is a base register
        /// </summary>
        public bool IsBase { get; }

        /// <summary>
        /// Returns the base register
        /// </summary>
        public Register BaseRegister { get; }

        /// <summary>
        /// Returns the extended register
        /// </summary>
        public ExtendedRegister ExtendedRegister { get; }

        /// <summary>
        /// Creates a new base register
        /// </summary>
        /// <param name="baseRegister">The base register</param>
        public IntRegister(Register baseRegister)
        {
            this.IsBase = true;
            this.BaseRegister = baseRegister;
            this.ExtendedRegister = ExtendedRegister.R8;
        }

        /// <summary>
        /// Creates a new extended register
        /// </summary>
        /// <param name="extendedRegister">The extended register</param>
        public IntRegister(ExtendedRegister extendedRegister)
        {
            this.IsBase = false;
            this.BaseRegister = Register.AX;
            this.ExtendedRegister = extendedRegister;
        }

        public override string ToString()
        {
            if (this.IsBase)
            {
                return "R" + this.BaseRegister;
            }
            else
            {
                return this.ExtendedRegister.ToString();
            }
        }

        /// <summary>
        /// Implicits converts the given base register into an int register
        /// </summary>
        /// <param name="baseRegister">The register</param>
        public static implicit operator IntRegister(Register baseRegister)
        {
            return new IntRegister(baseRegister);
        }

        /// <summary>
        /// Implicits converts the given extended register into an int register
        /// </summary>
        /// <param name="extendedRegister">The register</param>
        public static implicit operator IntRegister(ExtendedRegister extendedRegister)
        {
            return new IntRegister(extendedRegister);
        }

        /// <summary>
        /// Checks if lhs == rhs
        /// </summary>
        /// <param name="lhs">The left hand side</param>
        /// <param name="rhs">The right hand side</param>
        public static bool operator ==(IntRegister lhs, IntRegister rhs)
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

        /// <summary>
        /// Checks if lhs != rhs
        /// </summary>
        /// <param name="lhs">The left hand side</param>
        /// <param name="rhs">The right hand side</param>
        public static bool operator !=(IntRegister lhs, IntRegister rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Checks if the current object is equal to the given
        /// </summary>
        /// <param name="obj">The object</param>
        public override bool Equals(object obj)
        {
            if (!(obj is IntRegister))
            {
                return false;
            }

            var other = (IntRegister)obj;
            return this == other;
        }

        /// <summary>
        /// Computes the hash code
        /// </summary>
        public override int GetHashCode()
        {
            if (this.IsBase)
            {
                return this.IsBase.GetHashCode() + 31 * (int)this.BaseRegister;
            }
            else
            {
                return this.IsBase.GetHashCode() + 31 * (int)this.ExtendedRegister;
            }
        }
    }

    /// <summary>
    /// Represents a memory operand
    /// </summary>
    public struct MemoryOperand
    {
        /// <summary>
        /// The register where the address is stored
        /// </summary>
        public IntRegister Register { get; }

        /// <summary>
        /// Indicates if the operand has an offset
        /// </summary>
        public bool HasOffset { get; }

        /// <summary>
        /// The offset
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Creates a new memory operand
        /// </summary>
        /// <param name="register">The register where the address is stored</param>
        public MemoryOperand(IntRegister register)
        {
            this.Register = register;
            this.HasOffset = false;
            this.Offset = 0;
        }

        /// <summary>
        /// Creates a new memory operand with an offset
        /// </summary>
        /// <param name="register">The register where the address is stored</param>
        /// <param name="offset">The offset</param>
        public MemoryOperand(IntRegister register, int offset)
        {
            this.Register = register;
            this.HasOffset = true;
            this.Offset = offset;
        }

        public override string ToString()
        {
            if (this.HasOffset)
            {
                if (this.Offset > 0)
                {
                    return $"[{this.Register}+{this.Offset}]";
                }
                else
                {
                    return $"[{this.Register}{this.Offset}]";
                }
            }
            else
            {
                return $"[{this.Register}]";
            }
        }
    }

    /// <summary>
    /// The jump condition
    /// </summary>
    public enum JumpCondition
    {
        Always,
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

    /// <summary>
    /// Represents an assembler
    /// </summary>
    public static class Assembler
    {
        /// <summary>
        /// Generates code for a two register operand instruction
        /// </summary>
        /// <param name="op1">The first operand</param>
        /// <param name="op2">The second operand</param>
        private static void GenerateTwoRegistersInstruction(IList<byte> generatedCode, IntRegister op1, IntRegister op2,
            Action<IList<byte>, Register, Register> inst1, Action<IList<byte>, ExtendedRegister, ExtendedRegister> inst2,
            Action<IList<byte>, Register, ExtendedRegister> inst3, Action<IList<byte>, ExtendedRegister, Register> inst4)
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
        /// Generates code for an one register operand instruction
        /// </summary>
        /// <param name="op">The operand</param>
        private static void GenerateOneRegisterInstruction(IList<byte> generatedCode, IntRegister op,
            Action<IList<byte>, Register> inst1, Action<IList<byte>, ExtendedRegister> inst2)
        {
            if (op.IsBase)
            {
                inst1(generatedCode, op.BaseRegister);
            }
            else
            {
                inst2(generatedCode, op.ExtendedRegister);
            }
        }

        /// <summary>
        /// Generates code for an one register operand instruction with an int value
        /// </summary>
        /// <param name="op">The operand</param>
        /// <param name="value">The value</param>
        private static void GenerateOneRegisterWithValueInstruction(IList<byte> generatedCode, IntRegister op, int value,
            Action<IList<byte>, Register, int> inst1, Action<IList<byte>, ExtendedRegister, int> inst2)
        {
            if (op.IsBase)
            {
                inst1(generatedCode, op.BaseRegister, value);
            }
            else
            {
                inst2(generatedCode, op.ExtendedRegister, value);
            }
        }

        /// <summary>
        /// Generates code for an one memory operand instruction with an int value
        /// </summary>
        /// <param name="op">The operand</param>
        /// <param name="value">The value</param>
        private static void GenerateOneMemoryOperandWithValueInstruction(IList<byte> generatedCode, MemoryOperand op, int value,
            Action<IList<byte>, Register, int, int> inst1, Action<IList<byte>, ExtendedRegister, int, int> inst2)
        {
            if (op.Register.IsBase)
            {
                inst1(generatedCode, op.Register.BaseRegister, op.Offset, value);
            }
            else
            {
                inst2(generatedCode, op.Register.ExtendedRegister, op.Offset, value);
            }
        }

        /// <summary>
        /// Generates code for an instruction with a register destination and memory source
        /// </summary>
        /// <param name="op1">The first operand</param>
        /// <param name="op2">The second operand</param>
        private static void GenerateSourceMemoryInstruction(IList<byte> generatedCode, IntRegister op1, MemoryOperand op2,
            Action<IList<byte>, Register, Register, int> inst1, Action<IList<byte>, ExtendedRegister, ExtendedRegister, int> inst2,
            Action<IList<byte>, Register, ExtendedRegister, int> inst3, Action<IList<byte>, ExtendedRegister, Register, int> inst4)
        {
            if (op1.IsBase && op2.Register.IsBase)
            {
                inst1(generatedCode, op1.BaseRegister, op2.Register.BaseRegister, op2.Offset);
            }
            else if (!op1.IsBase && !op2.Register.IsBase)
            {
                inst2(generatedCode, op1.ExtendedRegister, op2.Register.ExtendedRegister, op2.Offset);
            }
            else if(op1.IsBase && !op2.Register.IsBase)
            {
                inst3(generatedCode, op1.BaseRegister, op2.Register.ExtendedRegister, op2.Offset);
            }
            else
            {
                inst4(generatedCode, op1.ExtendedRegister, op2.Register.BaseRegister, op2.Offset);
            }
        }

        /// <summary>
        /// Generates code for an instruction with a register destination and memory source
        /// </summary>
        /// <param name="op1">The first operand</param>
        /// <param name="op2">The second operand</param>
        private static void GenerateSourceMemoryInstruction(IList<byte> generatedCode, FloatRegister op1, MemoryOperand op2,
            Action<IList<byte>, FloatRegister, Register, int> inst1, Action<IList<byte>, FloatRegister, ExtendedRegister, int> inst2)
        {
            if (op2.Register.IsBase)
            {
                inst1(generatedCode, op1, op2.Register.BaseRegister, op2.Offset);
            }
            else
            {
                inst2(generatedCode, op1, op2.Register.ExtendedRegister, op2.Offset);
            }
        }

        /// <summary>
        /// Generates code for an instruction with a memory destination and register source
        /// </summary>
        /// <param name="op1">The first operand</param>
        /// <param name="op2">The second operand</param>
        private static void GenerateDestinationMemoryInstruction(IList<byte> generatedCode, MemoryOperand op1, IntRegister op2,
            Action<IList<byte>, Register, int, Register> inst1, Action<IList<byte>, ExtendedRegister, int, ExtendedRegister> inst2,
            Action<IList<byte>, Register, int, ExtendedRegister> inst3, Action<IList<byte>, ExtendedRegister, int, Register> inst4)
        {
            if (op1.Register.IsBase && op2.IsBase)
            {
                inst1(generatedCode, op1.Register.BaseRegister, op1.Offset, op2.BaseRegister);
            }
            else if (!op1.Register.IsBase && !op2.IsBase)
            {
                inst2(generatedCode, op1.Register.ExtendedRegister, op1.Offset, op2.ExtendedRegister);
            }
            else if (op1.Register.IsBase && !op2.IsBase)
            {
                inst3(generatedCode, op1.Register.BaseRegister, op1.Offset, op2.ExtendedRegister);
            }
            else
            {
                inst4(generatedCode, op1.Register.ExtendedRegister, op1.Offset, op2.BaseRegister);
            }
        }

        /// <summary>
        /// Generates code for an instruction with a memory destination and register source
        /// </summary>
        /// <param name="op1">The first operand</param>
        /// <param name="op2">The second operand</param>
        private static void GenerateDestinationMemoryInstruction(IList<byte> generatedCode, MemoryOperand op1, FloatRegister op2,
            Action<IList<byte>, Register, int, FloatRegister> inst1, Action<IList<byte>, ExtendedRegister, int, FloatRegister> inst2)
        {
            if (op1.Register.IsBase)
            {
                inst1(generatedCode, op1.Register.BaseRegister, op1.Offset, op2);
            }
            else
            {
                inst2(generatedCode, op1.Register.ExtendedRegister, op1.Offset, op2);
            }
        }


        /// <summary>
        /// Adds the second register to the first
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        public static void Add(IList<byte> generatedCode, IntRegister destination, IntRegister source)
        {
            GenerateTwoRegistersInstruction(
                generatedCode,
                destination,
                source,
                (gen, x, y) => RawAssembler.AddRegisterToRegister(gen, x, y),
                RawAssembler.AddRegisterToRegister,
                RawAssembler.AddRegisterToRegister,
                RawAssembler.AddRegisterToRegister);
        }

        /// <summary>
        /// Adds the given int to the register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="value">The value</param>
        public static void Add(IList<byte> generatedCode, IntRegister destination, int value)
        {
            GenerateOneRegisterWithValueInstruction(
                generatedCode,
                destination,
                value,
                (gen, x, y) => RawAssembler.AddIntToRegister(gen, x, y),
                RawAssembler.AddIntToRegister);
        }

        /// <summary>
        /// Adds the memory operand to the register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source memory</param>
        public static void Add(IList<byte> generatedCode, IntRegister destination, MemoryOperand source)
        {
            GenerateSourceMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.AddMemoryRegisterWithOffsetToRegister,
                RawAssembler.AddMemoryRegisterWithOffsetToRegister,
                RawAssembler.AddMemoryRegisterWithOffsetToRegister,
                RawAssembler.AddMemoryRegisterWithOffsetToRegister);
        }

        /// <summary>
        /// Adds register to the memory operand
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination memory</param>
        /// <param name="source">The source register</param>
        public static void Add(IList<byte> generatedCode, MemoryOperand destination, IntRegister source)
        {
            GenerateDestinationMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.AddRegisterToMemoryRegisterWithOffset,
                RawAssembler.AddRegisterToMemoryRegisterWithOffset,
                RawAssembler.AddRegisterToMemoryRegisterWithOffset,
                RawAssembler.AddRegisterToMemoryRegisterWithOffset);
        }

        /// <summary>
        /// Adds the second register to the first
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        public static void Add(IList<byte> generatedCode, FloatRegister destination, FloatRegister source)
        {
            RawAssembler.AddRegisterToRegister(generatedCode, destination, source);
        }

        /// <summary>
        /// Adds the memory operand to the register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source memory</param>
        public static void Add(IList<byte> generatedCode, FloatRegister destination, MemoryOperand source)
        {
            GenerateSourceMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.AddMemoryRegisterWithIntOffsetToRegister,
                RawAssembler.AddMemoryRegisterWithIntOffsetToRegister);
        }

        /// <summary>
        /// Subtracts the second register to the first
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        public static void Sub(IList<byte> generatedCode, IntRegister destination, IntRegister source)
        {
            GenerateTwoRegistersInstruction(
                generatedCode,
                destination,
                source,
                (gen, x, y) => RawAssembler.SubRegisterFromRegister(gen, x, y),
                RawAssembler.SubRegisterFromRegister,
                RawAssembler.SubRegisterFromRegister,
                RawAssembler.SubRegisterFromRegister);
        }

        /// <summary>
        /// Subtracts the memory operand from the register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source memory</param>
        public static void Sub(IList<byte> generatedCode, IntRegister destination, MemoryOperand source)
        {
            GenerateSourceMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.SubMemoryRegisterWithOffsetFromRegister,
                RawAssembler.SubMemoryRegisterWithOffsetFromRegister,
                RawAssembler.SubMemoryRegisterWithOffsetFromRegister,
                RawAssembler.SubMemoryRegisterWithOffsetFromRegister);
        }

        /// <summary>
        /// Subtrats register from the memory operand
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination memory</param>
        /// <param name="source">The source register</param>
        public static void Sub(IList<byte> generatedCode, MemoryOperand destination, IntRegister source)
        {
            GenerateDestinationMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.SubRegisterFromMemoryRegisterWithOffset,
                RawAssembler.SubRegisterFromMemoryRegisterWithOffset,
                RawAssembler.SubRegisterFromMemoryRegisterWithOffset,
                RawAssembler.SubRegisterFromMemoryRegisterWithOffset);
        }

        /// <summary>
        /// Subtracts the given value from the register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="value">The value to subtract</param>
        public static void Sub(IList<byte> generatedCode, IntRegister destination, int value)
        {
            GenerateOneRegisterWithValueInstruction(
                generatedCode,
                destination,
                value,
                (gen, x, y) => RawAssembler.SubIntFromRegister(gen, x, y),
                RawAssembler.SubIntFromRegister);
        }

        /// <summary>
        /// Multiplies the second register by the first
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        public static void Mult(IList<byte> generatedCode, IntRegister destination, IntRegister source)
        {
            GenerateTwoRegistersInstruction(
                generatedCode,
                destination,
                source,
                (gen, x, y) => RawAssembler.MultRegisterToRegister(gen, x, y),
                RawAssembler.MultRegisterToRegister,
                RawAssembler.MultRegisterToRegister,
                RawAssembler.MultRegisterToRegister);
        }

        /// <summary>
        /// Multiplies the memory operand by the register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source memory</param>
        public static void Mult(IList<byte> generatedCode, IntRegister destination, MemoryOperand source)
        {
            GenerateSourceMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.MultMemoryRegisterWithOffsetToRegister,
                RawAssembler.MultMemoryRegisterWithOffsetToRegister,
                RawAssembler.MultMemoryRegisterWithOffsetToRegister,
                RawAssembler.MultMemoryRegisterWithOffsetToRegister);
        }

        /// <summary>
        /// Divides the rax register with the given register. This instruction also modifies the rdx register.
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="source">The source register</param>
        public static void Div(IList<byte> generatedCode, IntRegister source)
        {
            if (source.IsBase)
            {
                RawAssembler.DivRegisterFromRegister(generatedCode, Register.AX, source.BaseRegister);
            }
            else
            {
                RawAssembler.DivRegisterFromRegister(generatedCode, Register.AX, source.ExtendedRegister);
            }
        }

        /// <summary>
        /// Divides the rax register with the given memory operand. This instruction also modifies the rdx register.
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="source">The source operand</param>
        public static void Div(IList<byte> generatedCode, MemoryOperand source)
        {
            if (source.Register.IsBase)
            {
                RawAssembler.DivMemoryRegisterWithOffsetFromRegister(generatedCode, Register.AX, source.Register.BaseRegister, source.Offset);
            }
            else
            {
                RawAssembler.DivMemoryRegisterWithOffsetFromRegister(generatedCode, Register.AX, source.Register.ExtendedRegister, source.Offset);
            }
        }

        /// <summary>
        /// Moves the second register to the first register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        public static void Move(IList<byte> generatedCode, IntRegister destination, IntRegister source)
        {
            GenerateTwoRegistersInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.MoveRegisterToRegister,
                RawAssembler.MoveRegisterToRegister,
                RawAssembler.MoveRegisterToRegister,
                RawAssembler.MoveRegisterToRegister);
        }

        /// <summary>
        /// Moves the memory operand to the register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source memory</param>
        public static void Move(IList<byte> generatedCode, IntRegister destination, MemoryOperand source)
        {
            GenerateSourceMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister);
        }

        /// <summary>
        /// Moves the register to the memory operand
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination memory</param>
        /// <param name="source">The source</param>
        public static void Move(IList<byte> generatedCode, MemoryOperand destination, IntRegister source)
        {
            GenerateDestinationMemoryInstruction(
                generatedCode,
                destination,
                source,
                (gen, dest, offset, src) => RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset(gen, dest, offset, src),
                RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset,
                RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset,
                RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset);
        }

        /// <summary>
        /// Moves the given int value to the given register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="value">The value</param>
        public static void Move(IList<byte> generatedCode, IntRegister destination, int value)
        {
            GenerateOneRegisterWithValueInstruction(
                generatedCode,
                destination,
                value,
                RawAssembler.MoveIntToRegister,
                RawAssembler.MoveIntToRegister);
        }

        /// <summary>
        /// Moves the given int value to the given memory
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination memory</param>
        /// <param name="value">The value</param>
        public static void Move(IList<byte> generatedCode, MemoryOperand destination, int value)
        {
            GenerateOneMemoryOperandWithValueInstruction(
                generatedCode,
                destination,
                value,
                RawAssembler.MoveIntToMemoryRegWithOffset,
                RawAssembler.MoveIntToMemoryRegWithOffset);
        }

        /// <summary>
        /// Moves the second register to the first register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        public static void Move(IList<byte> generatedCode, FloatRegister destination, FloatRegister source)
        {
            RawAssembler.MoveRegisterToRegister(generatedCode, destination, source);
        }

        /// <summary>
        /// Moves the memory operand to the register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source memory</param>
        public static void Move(IList<byte> generatedCode, FloatRegister destination, MemoryOperand source)
        {
            GenerateSourceMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister,
                RawAssembler.MoveMemoryRegisterWithIntOffsetToRegister);
        }

        /// <summary>
        /// Moves the register to the memory operand
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination memory</param>
        /// <param name="source">The source</param>
        public static void Move(IList<byte> generatedCode, MemoryOperand destination, FloatRegister source)
        {
            GenerateDestinationMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset,
                RawAssembler.MoveRegisterToMemoryRegisterWithIntOffset);
        }

        /// <summary>
        /// Compares the second register to the first register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        public static void Compare(IList<byte> generatedCode, IntRegister destination, IntRegister source)
        {
            GenerateTwoRegistersInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.CompareRegisterToRegister,
                RawAssembler.CompareRegisterToRegister,
                RawAssembler.CompareRegisterToRegister,
                RawAssembler.CompareRegisterToRegister);
        }

        /// <summary>
        /// Compares the memory operand to the register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source memory</param>
        public static void Compare(IList<byte> generatedCode, IntRegister destination, MemoryOperand source)
        {
            GenerateSourceMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.CompareRegisterToMemoryRegisterWithOffset,
                RawAssembler.CompareRegisterToMemoryRegisterWithOffset,
                RawAssembler.CompareRegisterToMemoryRegisterWithOffset,
                RawAssembler.CompareRegisterToMemoryRegisterWithOffset);
        }

        /// <summary>
        /// Compares the register to the memory operand
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination memory</param>
        /// <param name="source">The source</param>
        public static void Compare(IList<byte> generatedCode, MemoryOperand destination, IntRegister source)
        {
            GenerateDestinationMemoryInstruction(
                generatedCode,
                destination,
                source,
                RawAssembler.CompareMemoryRegisterWithOffsetToRegister,
                RawAssembler.CompareMemoryRegisterWithOffsetToRegister,
                RawAssembler.CompareMemoryRegisterWithOffsetToRegister,
                RawAssembler.CompareMemoryRegisterWithOffsetToRegister);
        }

        /// <summary>
        /// XOR's the second register to the first
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="destination">The destination</param>
        /// <param name="source">The source</param>
        public static void Xor(IList<byte> generatedCode, IntRegister destination, IntRegister source)
        {
            GenerateTwoRegistersInstruction(
                generatedCode,
                destination,
                source,
                (gen, x, y) => RawAssembler.XorRegisterToRegister(gen, x, y),
                RawAssembler.XorRegisterToRegister,
                RawAssembler.XorRegisterToRegister,
                RawAssembler.XorRegisterToRegister);
        }

        /// <summary>
        /// Pushes the given register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="register">The register</param>
        public static void Push(IList<byte> generatedCode, IntRegister register)
        {
            GenerateOneRegisterInstruction(
                generatedCode,
                register,
                RawAssembler.PushRegister,
                RawAssembler.PushRegister);
        }

        /// <summary>
        /// Pushes the given register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="register">The register</param>
        public static void Push(IList<byte> generatedCode, FloatRegister register)
        {
            RawAssembler.PushRegister(generatedCode, register);
        }

        /// <summary>
        /// Pushes the given integer
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="value">The value to push</param>
        public static void Push(IList<byte> generatedCode, int value)
        {
            RawAssembler.PushInt(generatedCode, value);
        }

        /// <summary>
        /// Pops the given register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="register">The register</param>
        public static void Pop(IList<byte> generatedCode, IntRegister register)
        {
            GenerateOneRegisterInstruction(
                generatedCode,
                register,
                RawAssembler.PopRegister,
                RawAssembler.PopRegister);
        }


        /// <summary>
        /// Pops the given register
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="register">The register</param>
        public static void Pop(IList<byte> generatedCode, FloatRegister register)
        {
            RawAssembler.PopRegister(generatedCode, register);
        }

        /// <summary>
        /// Pops the top operand
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        public static void Pop(IList<byte> generatedCode)
        {
            RawAssembler.AddByteToReg(generatedCode, Register.SP, RawAssembler.RegisterSize);
        }

        /// <summary>
        /// Jumps to the given target
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="condition">The jump condition</param>
        /// <param name="target">The target relative to the end of the generated instruction.</param>
        /// <param name="unsignedComparison">Indicates if to use an unsigned comparison</param>
        public static void Jump(IList<byte> generatedCode, JumpCondition condition, int target, bool unsignedComparison = false)
        {
            switch (condition)
            {
                case JumpCondition.Always:
                    RawAssembler.Jump(generatedCode, target);
                    break;
                case JumpCondition.Equal:
                    RawAssembler.JumpEqual(generatedCode, target);
                    break;
                case JumpCondition.NotEqual:
                    RawAssembler.JumpNotEqual(generatedCode, target);
                    break;
                case JumpCondition.LessThan:
                    if (unsignedComparison)
                    {
                        RawAssembler.JumpLessThanUnsigned(generatedCode, target);
                    }
                    else
                    {
                        RawAssembler.JumpLessThan(generatedCode, target);
                    }
                    break;
                case JumpCondition.LessThanOrEqual:
                    if (unsignedComparison)
                    {
                        RawAssembler.JumpLessThanOrEqualUnsigned(generatedCode, target);
                    }
                    else
                    {
                        RawAssembler.JumpLessThanOrEqual(generatedCode, target);
                    }
                    break;
                case JumpCondition.GreaterThan:
                    if (unsignedComparison)
                    {
                        RawAssembler.JumpGreaterThanUnsigned(generatedCode, target);
                    }
                    else
                    {
                        RawAssembler.JumpGreaterThan(generatedCode, target);
                    }
                    break;
                case JumpCondition.GreaterThanOrEqual:
                    if (unsignedComparison)
                    {
                        RawAssembler.JumpGreaterThanOrEqualUnsigned(generatedCode, target);
                    }
                    else
                    {
                        RawAssembler.JumpGreaterThanOrEqual(generatedCode, target);
                    }
                    break;
            }
        }
    }
}
