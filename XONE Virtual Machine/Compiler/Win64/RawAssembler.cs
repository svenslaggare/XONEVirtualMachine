using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Compiler.Win64
{
    /// <summary>
    /// The registers
    /// </summary>
    public enum Register : byte
    {
        AX = 0,
        CX = 1,
        DX = 2,
        BX = 3,
        SP = 4,
        BP = 5,
        SI = 6,
        DI = 7,
    }

    /// <summary>
    /// The extended registers
    /// </summary>
    public enum ExtendedRegister : byte
    {
        R8 = 0,
        R9 = 1,
        R10 = 2,
        R11 = 3,
        R12 = 4,
        R13 = 5,
        R14 = 6,
        R15 = 7
    }

    /// <summary>
    /// The float registers
    /// </summary>
    public enum FloatRegister: byte
    {
        XMM0 = 0,
        XMM1 = 1,
        XMM2 = 2,
        XMM3 = 3,
        XMM4 = 4,
        XMM5 = 5,
        XMM6 = 6,
        XMM7 = 7,
    }

    /// <summary>
    /// Represents a raw assembler
    /// </summary>
    public static class RawAssembler
    {
        /// <summary>
        /// The size of a register
        /// </summary>
        public const int RegisterSize = 8;

        /// <summary>
        /// Indicates if the given value fits in a byte
        /// </summary>
        /// <param name="value">The value</param>
        public static bool IsValidByteValue(int value)
        {
            return value >= -128 && value < 128;
        }

        /// <summary>
        /// Pushes the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register">The register</param>
        public static void PushRegister(IList<byte> codeGenerator, Register register)
        {
            codeGenerator.Add((byte)(0x50 | (byte)register));
        }

        /// <summary>
        /// Pushes the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register">The register</param>
        public static void PushRegister(IList<byte> codeGenerator, ExtendedRegister register)
        {
            codeGenerator.Add(0x41);
            codeGenerator.Add((byte)(0x50 | (byte)register));
        }

        /// <summary>
        /// Pushes the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register">The register</param>
        public static void PushRegister(IList<byte> codeGenerator, FloatRegister register)
        {
            SubByteFromRegister(codeGenerator, Register.SP, RegisterSize);   //sub rsp, <reg size>
            MoveRegisterToMemoryRegisterWithByteOffset(codeGenerator, Register.SP, 0, register);     //movss [rsp+0], <float reg>
        }

        /// <summary>
        /// Pushes the given integer
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="value">The value</param>
        public static void PushInt(IList<byte> codeGenerator, int value)
        {
            codeGenerator.Add(0x68);

            foreach (var component in BitConverter.GetBytes(value))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Pops the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register">The register</param>
        public static void PopRegister(IList<byte> codeGenerator, Register register)
        {
            codeGenerator.Add((byte)(0x58 | (byte)register));
        }

        /// <summary>
        /// Pushes the given generator
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register">The register</param>
        public static void PopRegister(IList<byte> codeGenerator, ExtendedRegister register)
        {
            codeGenerator.Add(0x41);
            codeGenerator.Add((byte)(0x58 | (byte)register));
        }

        /// <summary>
        /// Pops the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register">The register</param>
        public static void PopRegister(IList<byte> codeGenerator, FloatRegister register)
        {
            MoveMemoryByRegisterToRegister(codeGenerator, register, Register.SP);               //movss <reg>, [rsp]
            AddByteToReg(codeGenerator, Register.SP, RegisterSize);    //add rsp, <reg size>
        }

        /// <summary>
        /// Moves content of the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToRegister(IList<byte> codeGenerator, Register destination, Register source)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0x89);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Moves content of the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x89);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Moves content of the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister destination, Register source)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x89);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Moves content of the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToRegister(IList<byte> codeGenerator, Register destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x89);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Moves content of the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToRegister(IList<byte> codeGenerator, FloatRegister destination, FloatRegister source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x10);
            codeGenerator.Add((byte)(0xc0 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Moves the content from the register to the memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationAddress">The destination address</param>
        /// <param name="sourceRegister">The source register</param>
        public static void MoveRegisterToMemory(IList<byte> codeGenerator, long destinationAddress, Register sourceRegister)
        {
            if (sourceRegister != Register.AX)
            {
                throw new ArgumentException("Only the AX register is supported.");
            }

            codeGenerator.Add(0x48);
            codeGenerator.Add(0xa3);

            foreach (var component in BitConverter.GetBytes(destinationAddress))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from given memory address to the register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationRegister">The destination register</param>
        /// <param name="sourceAddress">The source address</param>
        public static void MoveMemoryToRegister(IList<byte> codeGenerator, Register destinationRegister, long sourceAddress)
        {
            if (destinationRegister != Register.AX)
            {
                throw new ArgumentException("Only the AX register is supported.");
            }

            codeGenerator.Add(0x48);
            codeGenerator.Add(0xa1);

            foreach (var component in BitConverter.GetBytes(sourceAddress))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from memory where the address is in the second register to the first register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void MoveMemoryByRegisterToRegister(IList<byte> codeGenerator, Register destination, Register sourceMemoryRegister,
            bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0x8b);
            codeGenerator.Add((byte)((byte)sourceMemoryRegister | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void MoveRegisterToMemoryRegisterWithOffset(IList<byte> codeGenerator, Register destinationMemoryRegister,
            int offset, Register source, bool is32bits = false)
        {
            if (IsValidByteValue(offset))
            {
                MoveRegisterToMemoryRegisterWithByteOffset(codeGenerator, destinationMemoryRegister, (byte)offset, source, is32bits);
            }
            else
            {
                MoveRegisterToMemoryRegisterWithIntOffset(codeGenerator, destinationMemoryRegister, offset, source, is32bits);
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithOffset(IList<byte> codeGenerator, Register destinationMemoryRegister,
            int offset, ExtendedRegister source)
        {
            if (IsValidByteValue(offset))
            {
                MoveRegisterToMemoryRegisterWithByteOffset(codeGenerator, destinationMemoryRegister, (byte)offset, source);
            }
            else
            {
                MoveRegisterToMemoryRegisterWithIntOffset(codeGenerator, destinationMemoryRegister, offset, source);
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + byte offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void MoveRegisterToMemoryRegisterWithByteOffset(IList<byte> codeGenerator, Register destinationMemoryRegister, 
            byte offset, Register source, bool is32bits = false)
        {
            if (destinationMemoryRegister != Register.SP)
            {
                if (!is32bits)
                {
                    codeGenerator.Add(0x48);
                }

                codeGenerator.Add(0x89);
                codeGenerator.Add((byte)(0x40 | (byte)destinationMemoryRegister | (byte)((byte)source << 3)));
                codeGenerator.Add(offset);
            }
            else
            {
                if (!is32bits)
                {
                    codeGenerator.Add(0x48);
                }

                codeGenerator.Add(0x89);
                codeGenerator.Add((byte)(0x44 | (byte)((byte)source << 3)));
                codeGenerator.Add(0x24);
                codeGenerator.Add(offset);
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + byte offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithByteOffset(IList<byte> codeGenerator, Register destinationMemoryRegister,
            byte offset, ExtendedRegister source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x89);
            codeGenerator.Add((byte)(0x40 | (byte)destinationMemoryRegister | (byte)((byte)source << 3)));
            codeGenerator.Add(offset);
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + int offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void MoveRegisterToMemoryRegisterWithIntOffset(
            IList<byte> codeGenerator,
            Register destinationMemoryRegister,
            int offset, Register source,
            bool is32bits = false)
        {
            if (destinationMemoryRegister != Register.SP)
            {
                if (!is32bits)
                {
                    codeGenerator.Add(0x48);
                }

                codeGenerator.Add(0x89);
                codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)((byte)source << 3)));
            }
            else
            {
                if (!is32bits)
                {
                    codeGenerator.Add(0x48);
                }

                codeGenerator.Add(0x89);
                codeGenerator.Add((byte)(0x84 | (byte)((byte)source << 3)));
                codeGenerator.Add(0x24);
            }

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + int offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithIntOffset(
            IList<byte> codeGenerator,
            Register destinationMemoryRegister,
            int offset, ExtendedRegister source)
        {
            if (destinationMemoryRegister != Register.SP)
            {
                codeGenerator.Add(0x4c);
                codeGenerator.Add(0x89);
                codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)((byte)source << 3)));
            }
            else
            {
                codeGenerator.Add(0x4c);
                codeGenerator.Add(0x89);
                codeGenerator.Add((byte)(0x84 | (byte)((byte)source << 3)));
                codeGenerator.Add(0x24);
            }

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + int offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithIntOffset(
            IList<byte> codeGenerator,
            ExtendedRegister destinationMemoryRegister,
            int offset, ExtendedRegister source)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x89);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)((byte)source << 3)));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + int offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithIntOffset(
            IList<byte> codeGenerator,
            ExtendedRegister destinationMemoryRegister,
            int offset, Register source)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x89);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)((byte)source << 3)));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + int offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithIntOffset(
            IList<byte> codeGenerator,
            ExtendedRegister destinationMemoryRegister,
            int offset, FloatRegister source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x41);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x11);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)((byte)source << 3)));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from a memory where the address is a register + offset to a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="offset">The offset</param>
        public static void MoveMemoryRegisterWithOffsetToRegister(IList<byte> codeGenerator, Register destination,
            Register sourceMemoryRegister, int offset)
        {
            if (IsValidByteValue(offset))
            {
                MoveMemoryRegisterWithByteOffsetToRegister(codeGenerator, destination, sourceMemoryRegister, (byte)offset);
            }
            else
            {
                MoveMemoryRegisterWithIntOffsetToRegister(codeGenerator, destination, sourceMemoryRegister, offset);
            }
        }

        /// <summary>
        /// Moves the content from a memory where the address is a register + char offset to a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="offset">The offset</param>
        public static void MoveMemoryRegisterWithByteOffsetToRegister(IList<byte> codeGenerator, Register destination,
            Register sourceMemoryRegister, byte offset)
        {
            if (sourceMemoryRegister != Register.SP)
            {
                codeGenerator.Add(0x48);
                codeGenerator.Add(0x8b);
                codeGenerator.Add((byte)(0x40 | (byte)sourceMemoryRegister | (byte)((byte)destination << 3)));
                codeGenerator.Add(offset);
            }
            else
            {
                codeGenerator.Add(0x48);
                codeGenerator.Add(0x8B);
                codeGenerator.Add((byte)(0x44 | (byte)((byte)destination << 3)));
                codeGenerator.Add(0x24);
                codeGenerator.Add(offset);
            }
        }

        /// <summary>
        /// Moves the content from a memory where the address is a register + int offset to a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="offset">The offset</param>
        public static void MoveMemoryRegisterWithIntOffsetToRegister(
            IList<byte> codeGenerator,
            Register destination,
            Register sourceMemoryRegister, int offset)
        {
            if (sourceMemoryRegister != Register.SP)
            {
                codeGenerator.Add(0x48);
                codeGenerator.Add(0x8b);
                codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)((byte)destination << 3)));
            }
            else
            {
                codeGenerator.Add(0x48);
                codeGenerator.Add(0x8b);
                codeGenerator.Add((byte)(0x84 | (byte)((byte)destination << 3)));
                codeGenerator.Add(0x24);
            }

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from a memory where the address is a register + int offset to a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="offset">The offset</param>
        public static void MoveMemoryRegisterWithIntOffsetToRegister(
            IList<byte> codeGenerator,
            ExtendedRegister destination,
            Register sourceMemoryRegister, int offset)
        {
            if (sourceMemoryRegister != Register.SP)
            {
                codeGenerator.Add(0x4c);
                codeGenerator.Add(0x8b);
                codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)((byte)destination << 3)));
            }
            else
            {
                codeGenerator.Add(0x4c);
                codeGenerator.Add(0x8b);
                codeGenerator.Add((byte)(0x84 | (byte)((byte)destination << 3)));
                codeGenerator.Add(0x24);
            }

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from a memory where the address is a register + int offset to a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="offset">The offset</param>
        public static void MoveMemoryRegisterWithIntOffsetToRegister(
            IList<byte> codeGenerator,
            ExtendedRegister destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x8b);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)((byte)destination << 3)));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from a memory where the address is a register + int offset to a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="offset">The offset</param>
        public static void MoveMemoryRegisterWithIntOffsetToRegister(
            IList<byte> codeGenerator,
            Register destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x8b);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)((byte)destination << 3)));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }


        /// <summary>
        /// Moves the content from a memory where the address is a register + int offset to a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="offset">The offset</param>
        public static void MoveMemoryRegisterWithIntOffsetToRegister(
            IList<byte> codeGenerator,
            FloatRegister destination,
            Register sourceMemoryRegister, int offset)
        {
            if (sourceMemoryRegister != Register.SP)
            {
                codeGenerator.Add(0xf3);
                codeGenerator.Add(0x0f);
                codeGenerator.Add(0x10);
                codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)((byte)destination << 3)));
            }
            else
            {
                codeGenerator.Add(0xf3);
                codeGenerator.Add(0x0f);
                codeGenerator.Add(0x10);
                codeGenerator.Add((byte)(0x84 | (byte)((byte)destination << 3)));
                codeGenerator.Add(0x24);
            }

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from a memory where the address is a register + int offset to a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="offset">The offset</param>
        public static void MoveMemoryRegisterWithIntOffsetToRegister(
            IList<byte> codeGenerator,
            FloatRegister destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x41);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x10);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)((byte)destination << 3)));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the given integer to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="value">The value</param>
        public static void MoveIntToRegister(IList<byte> codeGenerator, Register destination, int value)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0xc7);
            codeGenerator.Add((byte)(0xc0 | (byte)destination));

            foreach (var component in BitConverter.GetBytes(value))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the given integer to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="value">The value</param>
        public static void MoveIntToRegister(IList<byte> codeGenerator, ExtendedRegister destination, int value)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0xc7);
            codeGenerator.Add((byte)(0xc0 | (byte)destination));

            foreach (var component in BitConverter.GetBytes(value))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the given integer to memory where the address is in a register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="memoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="value">The value</param>
        public static void MoveIntToMemoryRegWithOffset(IList<byte> codeGenerator, Register memoryRegister, int offset, int value)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0xc7);
            codeGenerator.Add((byte)(0x80 | (byte)memoryRegister));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }

            foreach (var component in BitConverter.GetBytes(value))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the given integer to memory where the address is in a register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="memoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="value">The value</param>
        public static void MoveIntToMemoryRegWithOffset(IList<byte> codeGenerator, ExtendedRegister memoryRegister, int offset, int value)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0xc7);
            codeGenerator.Add((byte)(0x80 | (byte)memoryRegister));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }

            foreach (var component in BitConverter.GetBytes(value))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the given long (64-bits) to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="value">The value</param>
        public static void MoveLongToRegister(IList<byte> codeGenerator, Register destination, long value)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add((byte)(0xb8 | (byte)destination));

            foreach (var component in BitConverter.GetBytes(value))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Moves the content from memory where the address is in the second register to the first register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        public static void MoveMemoryByRegisterToRegister(IList<byte> codeGenerator, FloatRegister destination, Register sourceMemoryRegister)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x10);

            switch (sourceMemoryRegister)
            {
                case Register.SP:
                    codeGenerator.Add((byte)(0x04 | (byte)((byte)destination << 3)));
                    codeGenerator.Add(0x24);
                    break;
                case Register.BP:
                    codeGenerator.Add((byte)(0x45 | (byte)((byte)destination << 3)));
                    codeGenerator.Add(0x00);
                    break;
                default:
                    codeGenerator.Add((byte)((byte)sourceMemoryRegister | (byte)((byte)destination << 3)));
                    break;
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            Register destinationMemoryRegister,
            int offset,
            FloatRegister source)
        {
            if (IsValidByteValue(offset))
            {
                MoveRegisterToMemoryRegisterWithByteOffset(codeGenerator, destinationMemoryRegister, (byte)offset, source);
            }
            else
            {
                MoveRegisterToMemoryRegisterWithIntOffset(codeGenerator, destinationMemoryRegister, offset, source);
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + byte offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithByteOffset(
            IList<byte> codeGenerator,
            Register destinationMemoryRegister,
            byte offset,
            FloatRegister source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x11);

            if (destinationMemoryRegister != Register.SP)
            {
                codeGenerator.Add((byte)(0x40 | (byte)destinationMemoryRegister | (byte)((byte)source << 3)));
                codeGenerator.Add(offset);
            }
            else
            {
                codeGenerator.Add((byte)(0x44 | (byte)((byte)source << 3)));
                codeGenerator.Add(0x24);
                codeGenerator.Add(offset);
            }
        }

        /// <summary>
        /// Moves the content from a register to memory where the address is in a register + int offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithIntOffset(
            IList<byte> codeGenerator, 
            Register destinationMemoryRegister,
            int offset,
            FloatRegister source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x11);

            if (destinationMemoryRegister != Register.SP)
            {
                codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)((byte)source << 3)));
            }
            else
            {
                codeGenerator.Add((byte)(0x84 | (byte)((byte)source << 3)));
                codeGenerator.Add(0x24);
            }

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Calls the given function where the entry points is in a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="functionRegister">The register where the address is</param>
        public static void CallInRegister(IList<byte> codeGenerator, Register functionRegister)
        {
            codeGenerator.Add(0xff);
            codeGenerator.Add((byte)(0xd0 | (byte)functionRegister));
        }

        /// <summary>
        /// Calls the given function
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="relativeAddress">The relative address</param>
        public static void Call(IList<byte> codeGenerator, int relativeAddress)
        {
            codeGenerator.Add(0xe8);

            foreach (var component in BitConverter.GetBytes(relativeAddress))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Makes a return from the current function
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        public static void Return(IList<byte> codeGenerator)
        {
            codeGenerator.Add(0xc3);
        }

        /// <summary>
        /// Adds the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void AddRegisterToRegister(IList<byte> codeGenerator, Register destination, Register source, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0x01);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Adds the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void AddRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x01);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Adds the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void AddRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister destination, Register source)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x01);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Adds the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void AddRegisterToRegister(IList<byte> codeGenerator, Register destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x01);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Adds the second register to the memory address which is in the first register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="source">The source register</param>
        public static void AddRegisterToMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            Register destinationMemoryRegister, int offset,
            Register source)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0x01);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)source << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the second register to the memory address which is in the first register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="source">The source register</param>
        public static void AddRegisterToMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            Register destinationMemoryRegister, int offset,
            ExtendedRegister source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x01);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)source << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the second register to the memory address which is in the first register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="source">The source register</param>
        public static void AddRegisterToMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            ExtendedRegister destinationMemoryRegister, int offset,
            ExtendedRegister source)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x01);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)source << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the second register to the memory address which is in the first register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="source">The source register</param>
        public static void AddRegisterToMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            ExtendedRegister destinationMemoryRegister, int offset,
            Register source)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x01);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)source << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void AddMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            Register destination,
            Register sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0x03);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)destination << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void AddMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            ExtendedRegister destination,
            Register sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x03);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)destination << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void AddMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            ExtendedRegister destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x03);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)destination << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void AddMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            Register destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x03);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)destination << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void AddMemoryRegisterWithIntOffsetToRegister(
            IList<byte> codeGenerator,
            FloatRegister destination,
            Register sourceMemoryRegister, int offset)
        {
            if (sourceMemoryRegister != Register.SP)
            {
                codeGenerator.Add(0xf3);
                codeGenerator.Add(0x0f);
                codeGenerator.Add(0x58);
                codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)destination << 3));
            }
            else
            {
                codeGenerator.Add(0xf3);
                codeGenerator.Add(0x0f);
                codeGenerator.Add(0x58);
                codeGenerator.Add((byte)(0x84 | (byte)destination << 3));
                codeGenerator.Add(0x24);
            }

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void AddMemoryRegisterWithIntOffsetToRegister(
            IList<byte> codeGenerator,
            FloatRegister destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x41);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x58);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)destination << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the given integer constant to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationRegister"></param>
        /// <param name="sourceValue">The source value</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void AddConstantToRegister(IList<byte> codeGenerator, Register destinationRegister, int sourceValue, bool is32bits = false)
        {
            if (IsValidByteValue(sourceValue))
            {
                AddByteToReg(codeGenerator, destinationRegister, (byte)sourceValue, is32bits);
            }
            else
            {
                AddIntToRegister(codeGenerator, destinationRegister, sourceValue, is32bits);
            }
        }

        /// <summary>
        /// Adds the given byte to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationRegister"></param>
        /// <param name="sourceValue">The source value</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void AddByteToReg(IList<byte> codeGenerator, Register destinationRegister, byte sourceValue, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0x83);
            codeGenerator.Add((byte)(0xc0 | (byte)destinationRegister));
            codeGenerator.Add(sourceValue);
        }

        /// <summary>
        /// Adds the given int to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destReg"></param>
        /// <param name="sourceValue">The source value</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void AddIntToRegister(IList<byte> codeGenerator, Register destReg, int sourceValue, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            if (destReg == Register.AX)
            {
                codeGenerator.Add(0x05);
            }
            else
            {
                codeGenerator.Add(0x81);
                codeGenerator.Add((byte)(0xc0 | (byte)destReg));
            }

            foreach (var component in BitConverter.GetBytes(sourceValue))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the given int to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destReg"></param>
        /// <param name="sourceValue">The source value</param>
        public static void AddIntToRegister(IList<byte> codeGenerator, ExtendedRegister destReg, int sourceValue)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x81);
            codeGenerator.Add((byte)(0xc0 | (byte)destReg));

            foreach (var component in BitConverter.GetBytes(sourceValue))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Adds the second register to the first register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void AddRegisterToRegister(IList<byte> codeGenerator, FloatRegister destination, FloatRegister source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x58);
            codeGenerator.Add((byte)(0xc0 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Subtracts the second register from the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, Register destination, Register source, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0x29);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Subtracts the second register from the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, ExtendedRegister destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x29);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Subtracts the second register from the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, ExtendedRegister destination, Register source)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x29);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Subtracts the second register from the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, Register destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x29);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Subtracts the second register to the memory address which is in the first register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="source">The source register</param>
        public static void SubRegisterFromMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            Register destinationMemoryRegister, int offset,
            Register source)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0x29);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)source << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Subtracts the second register to the memory address which is in the first register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="source">The source register</param>
        public static void SubRegisterFromMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            Register destinationMemoryRegister, int offset,
            ExtendedRegister source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x29);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)source << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Subtracts the second register to the memory address which is in the first register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="source">The source register</param>
        public static void SubRegisterFromMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            ExtendedRegister destinationMemoryRegister, int offset,
            ExtendedRegister source)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x29);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)source << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Subtracts the second register to the memory address which is in the first register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationMemoryRegister">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="source">The source register</param>
        public static void SubRegisterFromMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            ExtendedRegister destinationMemoryRegister, int offset,
            Register source)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x29);
            codeGenerator.Add((byte)(0x80 | (byte)destinationMemoryRegister | (byte)source << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Subtracts the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void SubMemoryRegisterWithOffsetFromRegister(
            IList<byte> codeGenerator,
            Register destination,
            Register sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0x2b);
            codeGenerator.Add((byte)(0x80 | (byte)destination | (byte)sourceMemoryRegister << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Subtracts the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void SubMemoryRegisterWithOffsetFromRegister(
            IList<byte> codeGenerator,
            ExtendedRegister destination,
            Register sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x2b);
            codeGenerator.Add((byte)(0x80 | (byte)destination | (byte)sourceMemoryRegister << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Subtracts the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void SubMemoryRegisterWithOffsetFromRegister(
            IList<byte> codeGenerator,
            ExtendedRegister destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x2b);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)destination << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }


        /// <summary>
        /// Subtracts the memory to the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void SubMemoryRegisterWithOffsetFromRegister(
            IList<byte> codeGenerator,
            Register destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x2b);
            codeGenerator.Add((byte)(0x80 | (byte)sourceMemoryRegister | (byte)destination << 3));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Subtracts the given constant from the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationRegister"></param>
        /// <param name="value">The value</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void SubConstantFromRegister(IList<byte> codeGenerator, Register destinationRegister, int value, bool is32bits = false)
        {
            if (IsValidByteValue(value))
            {
                SubByteFromRegister(codeGenerator, destinationRegister, (byte)value, is32bits);
            }
            else
            {
                SubIntFromRegister(codeGenerator, destinationRegister, value, is32bits);
            }
        }

        /// <summary>
        /// Subtracts the given byte from the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationRegister"></param>
        /// <param name="value">The value</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void SubByteFromRegister(IList<byte> codeGenerator, Register destinationRegister, byte value, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0x83);
            codeGenerator.Add((byte)(0xe8 | (byte)destinationRegister));
            codeGenerator.Add(value);
        }

        /// <summary>
        /// Subtracts the given int from the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationRegister"></param>
        /// <param name="value">The value</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void SubIntFromRegister(IList<byte> codeGenerator, Register destinationRegister, int value, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            if (destinationRegister == Register.AX)
            {
                codeGenerator.Add(0x2d);
            }
            else
            {
                codeGenerator.Add(0x81);
                codeGenerator.Add((byte)(0xe8 | (byte)destinationRegister));
            }

            foreach (var component in BitConverter.GetBytes(value))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Subtracts the given int from the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationRegister"></param>
        /// <param name="value">The value</param>
        public static void SubIntFromRegister(IList<byte> codeGenerator, ExtendedRegister destinationRegister, int value)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x81);
            codeGenerator.Add((byte)(0xe8 | (byte)destinationRegister));

            foreach (var component in BitConverter.GetBytes(value))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Subtracts the second register from the first register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, FloatRegister destination, FloatRegister source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x5c);
            codeGenerator.Add((byte)(0xc0 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Multiplies the first register by the second
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void MultRegisterToRegister(IList<byte> codeGenerator, Register destination, Register source, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0x0f);
            codeGenerator.Add(0xaf);
            codeGenerator.Add((byte)(0xc0 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Multiplies the first register by the second
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MultRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0xaf);
            codeGenerator.Add((byte)(0xc0 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Multiplies the first register by the second
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MultRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister destination, Register source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0xaf);
            codeGenerator.Add((byte)(0xc0 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Multiplies the first register by the second
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MultRegisterToRegister(IList<byte> codeGenerator, Register destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0xaf);
            codeGenerator.Add((byte)(0xc0 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Multiplies the memory by the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void MultMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            Register destination,
            Register sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0xaf);
            codeGenerator.Add((byte)(0x80 | (byte)destination << 3 | (byte)sourceMemoryRegister));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Multiplies the memory by the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void MultMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            ExtendedRegister destination,
            Register sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0xaf);
            codeGenerator.Add((byte)(0x80 | (byte)destination << 3 | (byte)sourceMemoryRegister));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Multiplies the memory by the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void MultMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            ExtendedRegister destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0xaf);
            codeGenerator.Add((byte)(0x80 | (byte)destination << 3 | (byte)sourceMemoryRegister));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Multiplies the memory by the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination memory register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void MultMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            Register destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0xaf);
            codeGenerator.Add((byte)(0x80 | (byte)destination << 3 | (byte)sourceMemoryRegister));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Multiplies the first register by the second
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MultRegisterToRegister(IList<byte> codeGenerator, FloatRegister destination, FloatRegister source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x59);
            codeGenerator.Add((byte)(0xc0 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Divides the second register from the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void DivRegisterFromRegister(IList<byte> codeGenerator, Register destination, Register source, bool is32bits = false)
        {
            if (destination != Register.AX)
            {
                throw new ArgumentException("Only the AX register is supported as destination.");
            }

            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0xf7);
            codeGenerator.Add((byte)(0xf8 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Divides the second register from the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void DivRegisterFromRegister(IList<byte> codeGenerator, Register destination, ExtendedRegister source)
        {
            if (destination != Register.AX)
            {
                throw new ArgumentException("Only the AX register is supported as destination.");
            }

            codeGenerator.Add(0x49);
            codeGenerator.Add(0xf7);
            codeGenerator.Add((byte)(0xf8 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Divides the memory by the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void DivMemoryRegisterWithOffsetFromRegister(
            IList<byte> codeGenerator,
            Register destination,
            Register sourceMemoryRegister, int offset)
        {
            if (destination != Register.AX)
            {
                throw new ArgumentException("Only the AX register is supported as destination.");
            }

            codeGenerator.Add(0x48);
            codeGenerator.Add(0xf7);
            codeGenerator.Add((byte)(0xb8 | (byte)sourceMemoryRegister | (byte)((byte)destination << 3)));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Divides the memory by the first register where the memory address is in the second register + offset
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="offset">The memory offset</param>
        /// <param name="sourceMemoryRegister">The source register</param>
        public static void DivMemoryRegisterWithOffsetFromRegister(
            IList<byte> codeGenerator,
            Register destination,
            ExtendedRegister sourceMemoryRegister, int offset)
        {
            if (destination != Register.AX)
            {
                throw new ArgumentException("Only the AX register is supported as destination.");
            }

            codeGenerator.Add(0x49);
            codeGenerator.Add(0xf7);
            codeGenerator.Add((byte)(0xb8 | (byte)sourceMemoryRegister | (byte)((byte)destination << 3)));

            foreach (var component in BitConverter.GetBytes(offset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Divides the second register from the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void DivRegisterFromRegister(IList<byte> codeGenerator, FloatRegister destination, FloatRegister source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x5e);
            codeGenerator.Add((byte)(0xc0 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// AND's the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void AndRegisterToRegister(IList<byte> codeGenerator, Register destination, Register source, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0x21);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// OR's the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="dest"></param>
        /// <param name="src"></param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void OrRegisterToRegister(IList<byte> codeGenerator, Register dest, Register src, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0x09);
            codeGenerator.Add((byte)(0xc0 | (byte)dest | (byte)((byte)src << 3)));
        }

        /// <summary>
        /// XOR's the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void XorRegisterToRegister(IList<byte> codeGenerator, Register destination, Register source, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0x31);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// XOR's the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void XorRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x31);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// XOR's the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void XorRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister destination, Register source)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x31);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// XOR's the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void XorRegisterToRegister(IList<byte> codeGenerator, Register destination, ExtendedRegister source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x31);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// NOT's the register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="reg"></param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void NotRegister(IList<byte> codeGenerator, Register reg, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            codeGenerator.Add(0xf7);
            codeGenerator.Add((byte)(0xd0 | (byte)reg));
        }

        /// <summary>
        /// Compares the two registers
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1">The first register</param>
        /// <param name="register2">The second register</param>
        public static void CompareRegisterToRegister(IList<byte> codeGenerator, Register register1, Register register2)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0x39);
            codeGenerator.Add((byte)(0xc0 | (byte)register1 | (byte)((byte)register2 << 3)));
        }

        /// <summary>
        /// Compares the two registers
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1">The first register</param>
        /// <param name="register2">The second register</param>
        public static void CompareRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister register1, ExtendedRegister register2)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x39);
            codeGenerator.Add((byte)(0xc0 | (byte)register1 | (byte)((byte)register2 << 3)));
        }

        /// <summary>
        /// Compares the two registers
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1">The first register</param>
        /// <param name="register2">The second register</param>
        public static void CompareRegisterToRegister(IList<byte> codeGenerator, ExtendedRegister register1, Register register2)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x39);
            codeGenerator.Add((byte)(0xc0 | (byte)register1 | (byte)((byte)register2 << 3)));
        }

        /// <summary>
        /// Compares the two registers
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1">The first register</param>
        /// <param name="register2">The second register</param>
        public static void CompareRegisterToRegister(IList<byte> codeGenerator, Register register1, ExtendedRegister register2)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x39);
            codeGenerator.Add((byte)(0xc0 | (byte)register1 | (byte)((byte)register2 << 3)));
        }

        /// <summary>
        /// Compares the a register and a memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1">The first register</param>
        /// <param name="register2Memory">The register with the address of the second operand</param>
        /// <param name="register2MemoryOffset">The memory offset</param>
        public static void CompareRegisterToMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            Register register1,
            Register register2Memory,
            int register2MemoryOffset)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0x3b);
            codeGenerator.Add((byte)(0x80 | (byte)register2Memory | (byte)register1 << 3));

            foreach (var component in BitConverter.GetBytes(register2MemoryOffset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Compares the a register and a memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1">The first register</param>
        /// <param name="register2Memory">The register with the address of the second operand</param>
        /// <param name="register2MemoryOffset">The memory offset</param>
        public static void CompareRegisterToMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            ExtendedRegister register1,
            ExtendedRegister register2Memory,
            int register2MemoryOffset)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x3b);
            codeGenerator.Add((byte)(0x80 | (byte)register2Memory | (byte)register1 << 3));

            foreach (var component in BitConverter.GetBytes(register2MemoryOffset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Compares the a register and a memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1">The first register</param>
        /// <param name="register2Memory">The register with the address of the second operand</param>
        /// <param name="register2MemoryOffset">The memory offset</param>
        public static void CompareRegisterToMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            Register register1,
            ExtendedRegister register2Memory,
            int register2MemoryOffset)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x3b);
            codeGenerator.Add((byte)(0x80 | (byte)register2Memory | (byte)register1 << 3));

            foreach (var component in BitConverter.GetBytes(register2MemoryOffset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Compares the a register and a memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1Memory">The register with the address of the first operand</param>
        /// <param name="register1MemoryOffset">The memory offset</param>
        /// <param name="register2">The second register</param>
        public static void CompareMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            Register register1Memory,
            int register1MemoryOffset,
            Register register2)
        {
            codeGenerator.Add(0x48);
            codeGenerator.Add(0x39);
            codeGenerator.Add((byte)(0x80 | (byte)register1Memory | (byte)register2 << 3));

            foreach (var component in BitConverter.GetBytes(register1MemoryOffset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Compares the a register and a memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1Memory">The register with the address of the first operand</param>
        /// <param name="register1MemoryOffset">The memory offset</param>
        /// <param name="register2">The second register</param>
        public static void CompareMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            ExtendedRegister register1Memory,
            int register1MemoryOffset,
            ExtendedRegister register2)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x39);
            codeGenerator.Add((byte)(0x80 | (byte)register1Memory | (byte)register2 << 3));

            foreach (var component in BitConverter.GetBytes(register1MemoryOffset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Compares the a register and a memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1Memory">The register with the address of the first operand</param>
        /// <param name="register1MemoryOffset">The memory offset</param>
        /// <param name="register2">The second register</param>
        public static void CompareMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            ExtendedRegister register1Memory,
            int register1MemoryOffset,
            Register register2)
        {
            codeGenerator.Add(0x49);
            codeGenerator.Add(0x39);
            codeGenerator.Add((byte)(0x80 | (byte)register1Memory | (byte)register2 << 3));

            foreach (var component in BitConverter.GetBytes(register1MemoryOffset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Compares the a register and a memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1">The first register</param>
        /// <param name="register2Memory">The register with the address of the second operand</param>
        /// <param name="register2MemoryOffset">The memory offset</param>
        public static void CompareRegisterToMemoryRegisterWithOffset(
            IList<byte> codeGenerator,
            ExtendedRegister register1,
            Register register2Memory,
            int register2MemoryOffset)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x3b);
            codeGenerator.Add((byte)(0x80 | (byte)register2Memory | (byte)register1 << 3));

            foreach (var component in BitConverter.GetBytes(register2MemoryOffset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Compares the a register and a memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register1Memory">The register with the address of the first operand</param>
        /// <param name="register1MemoryOffset">The memory offset</param>
        /// <param name="register2">The second register</param>
        public static void CompareMemoryRegisterWithOffsetToRegister(
            IList<byte> codeGenerator,
            Register register1Memory,
            int register1MemoryOffset,
            ExtendedRegister register2)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x39);
            codeGenerator.Add((byte)(0x80 | (byte)register1Memory | (byte)register2 << 3));

            foreach (var component in BitConverter.GetBytes(register1MemoryOffset))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps to the target relative the current instruction
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void Jump(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0xE9);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if equal to the target
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpEqual(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x84);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if not equal to the target
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpNotEqual(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x85);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if > to the target relative the current instruction. Uses unsigned comparison.
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpGreaterThan(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x8F);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if >= to the target relative the current instruction
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpGreaterThanUnsigned(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x87);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if >= to the target relative the current instruction. Uses unsigned comparison.
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpGreaterThanOrEqual(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x8D);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if >= to the target
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpGreaterThanOrEqualUnsigned(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x83);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if less to the target relative the current instruction
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpLessThan(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x8C);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if less to the target relative the current instruction. Uses unsigned comparison.
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpLessThanUnsigned(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x82);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if less or equal to the target relative the current instruction
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpLessThanOrEqual(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x8E);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }

        /// <summary>
        /// Jumps if less or equal to the target relative the current instruction. Uses unsigned comparison.
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="target">The target</param>
        public static void JumpLessThanOrEqualUnsigned(IList<byte> codeGenerator, int target)
        {
            codeGenerator.Add(0x0F);
            codeGenerator.Add(0x86);

            foreach (var component in BitConverter.GetBytes(target))
            {
                codeGenerator.Add(component);
            }
        }
    }
}
