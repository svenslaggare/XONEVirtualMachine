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
        public Registers BaseRegister { get; }

        /// <summary>
        /// Returns the extended register
        /// </summary>
        public ExtendedRegisters ExtendedRegister { get; }

        /// <summary>
        /// Creates a new base register
        /// </summary>
        /// <param name="baseRegister">The base register</param>
        public IntRegister(Registers baseRegister)
        {
            this.IsBase = true;
            this.BaseRegister = baseRegister;
            this.ExtendedRegister = ExtendedRegisters.R8;
        }

        /// <summary>
        /// Creates a new extended register
        /// </summary>
        /// <param name="extendedRegister">The extended register</param>
        public IntRegister(ExtendedRegisters extendedRegister)
        {
            this.IsBase = false;
            this.BaseRegister = Registers.AX;
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
        public static implicit operator IntRegister(Registers baseRegister)
        {
            return new IntRegister(baseRegister);
        }

        /// <summary>
        /// Implicits converts the given extended register into an int register
        /// </summary>
        /// <param name="extendedRegister">The register</param>
        public static implicit operator IntRegister(ExtendedRegisters extendedRegister)
        {
            return new IntRegister(extendedRegister);
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
            Action<IList<byte>, Registers, Registers> inst1, Action<IList<byte>, ExtendedRegisters, ExtendedRegisters> inst2,
            Action<IList<byte>, Registers, ExtendedRegisters> inst3, Action<IList<byte>, ExtendedRegisters, Registers> inst4)
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
            Action<IList<byte>, Registers> inst1, Action<IList<byte>, ExtendedRegisters> inst2)
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
            Action<IList<byte>, Registers, int> inst1, Action<IList<byte>, ExtendedRegisters, int> inst2)
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
        /// Generates code for an instruction with a register destination and memory source
        /// </summary>
        /// <param name="op1">The first operand</param>
        /// <param name="op2">The second operand</param>
        private static void GenerateSourceMemoryInstruction(IList<byte> generatedCode, IntRegister op1, MemoryOperand op2,
            Action<IList<byte>, Registers, Registers, int> inst1, Action<IList<byte>, ExtendedRegisters, ExtendedRegisters, int> inst2,
            Action<IList<byte>, Registers, ExtendedRegisters, int> inst3, Action<IList<byte>, ExtendedRegisters, Registers, int> inst4)
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
        /// Generates code for an instruction with a memory destination and register source
        /// </summary>
        /// <param name="op1">The first operand</param>
        /// <param name="op2">The second operand</param>
        private static void GenerateDestinationMemoryInstruction(IList<byte> generatedCode, MemoryOperand op1, IntRegister op2,
            Action<IList<byte>, Registers, int, Registers> inst1, Action<IList<byte>, ExtendedRegisters, int, ExtendedRegisters> inst2,
            Action<IList<byte>, ExtendedRegisters, int, Registers> inst3, Action<IList<byte>, Registers, int, ExtendedRegisters> inst4)
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
                inst3(generatedCode, op1.Register.ExtendedRegister, op1.Offset, op2.BaseRegister);
            }
            else
            {
                inst4(generatedCode, op1.Register.BaseRegister, op1.Offset, op2.ExtendedRegister);
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
        public static void AddInt(IList<byte> generatedCode, IntRegister destination, int value)
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
    }
}
