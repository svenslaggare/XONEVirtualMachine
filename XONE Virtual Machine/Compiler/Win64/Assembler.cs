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
    public enum Registers : byte
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
    /// The numbered registers
    /// </summary>
    public enum NumberedRegisters : byte
    {
        R8 = 0,
        R9 = 1,
        R10 = 2,
        R11 = 3,
    }

    /// <summary>
    /// The float registers
    /// </summary>
    public enum FloatRegisters: byte
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
    /// Represents an assembler
    /// </summary>
    public static class Assembler
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
        public static void PushRegister(IList<byte> codeGenerator, Registers register)
        {
            codeGenerator.Add((byte)(0x50 | (byte)register));
        }

        /// <summary>
        /// Pushes the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register">The register</param>
        public static void PushRegister(IList<byte> codeGenerator, FloatRegisters register)
        {
            SubByteFromRegister(codeGenerator, Registers.SP, RegisterSize);   //sub rsp, <reg size>
            MoveRegisterToMemoryRegisterWithByteOffset(codeGenerator, Registers.SP, 0, register);     //movss [rsp+0], <float reg>
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
        public static void PopRegister(IList<byte> codeGenerator, Registers register)
        {
            codeGenerator.Add((byte)(0x58 | (byte)register));
        }

        /// <summary>
        /// Pushes the given generator
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register">The register</param>
        public static void PopRegister(IList<byte> codeGenerator, NumberedRegisters register)
        {
            codeGenerator.Add(0x41);
            codeGenerator.Add((byte)(0x58 | (byte)register));
        }

        /// <summary>
        /// Pops the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="register">The register</param>
        public static void PopRegister(IList<byte> codeGenerator, FloatRegisters register)
        {
            MoveMemoryByRegisterToRegister(codeGenerator, register, Registers.SP);               //movss <reg>, [rsp]
            AddByteToReg(codeGenerator, Registers.SP, RegisterSize);    //add rsp, <reg size>
        }

        /// <summary>
        /// Moves content of the second register to the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToRegister(IList<byte> codeGenerator, Registers destination, Registers source)
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
        public static void MoveRegisterToRegister(IList<byte> codeGenerator, NumberedRegisters destination, NumberedRegisters source)
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
        public static void MoveRegisterToRegister(IList<byte> codeGenerator, NumberedRegisters destination, Registers source)
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
        public static void MoveRegisterToRegister(IList<byte> codeGenerator, Registers destination, NumberedRegisters source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x89);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Moves the content from the register to the memory address
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationAddress">The destination address</param>
        /// <param name="sourceRegister">The source register</param>
        public static void MoveRegisterToMemory(IList<byte> codeGenerator, long destinationAddress, Registers sourceRegister)
        {
            if (sourceRegister != Registers.AX)
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
        public static void MoveMemoryToRegister(IList<byte> codeGenerator, Registers destinationRegister, long sourceAddress)
        {
            if (destinationRegister != Registers.AX)
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
        public static void MoveMemoryByRegisterToRegister(IList<byte> codeGenerator, Registers destination, Registers sourceMemoryRegister,
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
        public static void MoveRegisterToMemoryRegisterWithOffset(IList<byte> codeGenerator, Registers destinationMemoryRegister,
            int offset, Registers source, bool is32bits = false)
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
        public static void MoveRegisterToMemoryRegisterWithOffset(IList<byte> codeGenerator, Registers destinationMemoryRegister,
            int offset, NumberedRegisters source)
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
        /// <param name="destinationMemoryRegister"></param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void MoveRegisterToMemoryRegisterWithByteOffset(IList<byte> codeGenerator, Registers destinationMemoryRegister, 
            byte offset, Registers source, bool is32bits = false)
        {
            if (destinationMemoryRegister != Registers.SP)
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
        /// <param name="destinationMemoryRegister"></param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithByteOffset(IList<byte> codeGenerator, Registers destinationMemoryRegister,
            byte offset, NumberedRegisters source)
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
        /// <param name="destinationMemoryRegister"></param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void MoveRegisterToMemoryRegisterWithIntOffset(IList<byte> codeGenerator, Registers destinationMemoryRegister,
            int offset, Registers source, bool is32bits)
        {
            if (destinationMemoryRegister != Registers.SP)
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
        /// <param name="destinationMemoryRegister"></param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithIntOffset(IList<byte> codeGenerator, Registers destinationMemoryRegister,
            int offset, NumberedRegisters source)
        {
            if (destinationMemoryRegister != Registers.SP)
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
        /// Moves the content from a memory where the address is a register + offset to a register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="sourceMemoryRegister">The source memory register</param>
        /// <param name="offset">The offset</param>
        public static void MoveMemoryRegisterWithOffsetToRegister(IList<byte> codeGenerator, Registers destination,
            Registers sourceMemoryRegister, int offset)
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
        public static void MoveMemoryRegisterWithByteOffsetToRegister(IList<byte> codeGenerator, Registers destination,
            Registers sourceMemoryRegister, byte offset)
        {
            if (sourceMemoryRegister != Registers.SP)
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
        public static void MoveMemoryRegisterWithIntOffsetToRegister(IList<byte> codeGenerator, Registers destination,
            Registers sourceMemoryRegister, int offset)
        {
            if (sourceMemoryRegister != Registers.SP)
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
        /// Moves the given integer to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="value">The value</param>
        public static void MoveIntToRegister(IList<byte> codeGenerator, Registers destination, int value)
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
        public static void MoveIntToRegister(IList<byte> codeGenerator, NumberedRegisters destination, int value)
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
        /// Moves the given long (64-bits) to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="value">The value</param>
        public static void MoveLongToRegister(IList<byte> codeGenerator, Registers destination, long value)
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
        public static void MoveMemoryByRegisterToRegister(IList<byte> codeGenerator, FloatRegisters destination, Registers sourceMemoryRegister)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x10);

            switch (sourceMemoryRegister)
            {
                case Registers.SP:
                    codeGenerator.Add((byte)(0x04 | (byte)((byte)destination << 3)));
                    codeGenerator.Add(0x24);
                    break;
                case Registers.BP:
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
        /// <param name="destinationMemoryRegister"></param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithOffset(IList<byte> codeGenerator, Registers destinationMemoryRegister, int offset,
            FloatRegisters source)
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
        /// <param name="destinationnMemoryRegister"></param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithByteOffset(IList<byte> codeGenerator, Registers destinationnMemoryRegister, byte offset,
            FloatRegisters source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x11);

            if (destinationnMemoryRegister != Registers.SP)
            {
                codeGenerator.Add((byte)(0x40 | (byte)destinationnMemoryRegister | (byte)((byte)source << 3)));
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
        /// <param name="destinationMemoryRegister"></param>
        /// <param name="offset">The offset</param>
        /// <param name="source">The source register</param>
        public static void MoveRegisterToMemoryRegisterWithIntOffset(IList<byte> codeGenerator, Registers destinationMemoryRegister, int offset,
            FloatRegisters source)
        {
            codeGenerator.Add(0xf3);
            codeGenerator.Add(0x0f);
            codeGenerator.Add(0x11);

            if (destinationMemoryRegister != Registers.SP)
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
        public static void CallInRegister(IList<byte> codeGenerator, Registers functionRegister)
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
        public static void AddRegisterToRegister(IList<byte> codeGenerator, Registers destination, Registers source, bool is32bits = false)
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
        public static void AddRegisterToRegister(IList<byte> codeGenerator, NumberedRegisters destination, NumberedRegisters source)
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
        public static void AddRegisterToRegister(IList<byte> codeGenerator, NumberedRegisters destination, Registers source)
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
        public static void AddRegisterToRegister(IList<byte> codeGenerator, Registers destination, NumberedRegisters source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x01);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Adds the given integer constant to the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationRegister"></param>
        /// <param name="sourceValue">The source value</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void AddConstantToRegister(IList<byte> codeGenerator, Registers destinationRegister, int sourceValue, bool is32bits = false)
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
        public static void AddByteToReg(IList<byte> codeGenerator, Registers destinationRegister, byte sourceValue, bool is32bits = false)
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
        public static void AddIntToRegister(IList<byte> codeGenerator, Registers destReg, int sourceValue, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            if (destReg == Registers.AX)
            {
                codeGenerator.Add(0x05);
            }
            else
            {
                codeGenerator.Add(0x81);
                codeGenerator.Add((byte)(0xc1 | (byte)destReg));
            }

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
        public static void AddRegisterToRegister(IList<byte> codeGenerator, FloatRegisters destination, FloatRegisters source)
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
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, Registers destination, Registers source, bool is32bits = false)
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
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, NumberedRegisters destination, NumberedRegisters source)
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
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, NumberedRegisters destination, Registers source)
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
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, Registers destination, NumberedRegisters source)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x29);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// Subtracts the given constant from the given register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destinationRegister"></param>
        /// <param name="value">The value</param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void SubConstantFromRegister(IList<byte> codeGenerator, Registers destinationRegister, int value, bool is32bits = false)
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
        public static void SubByteFromRegister(IList<byte> codeGenerator, Registers destinationRegister, byte value, bool is32bits = false)
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
        public static void SubIntFromRegister(IList<byte> codeGenerator, Registers destinationRegister, int value, bool is32bits = false)
        {
            if (!is32bits)
            {
                codeGenerator.Add(0x48);
            }

            if (destinationRegister == Registers.AX)
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
        /// Subtracts the second register from the first register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void SubRegisterFromRegister(IList<byte> codeGenerator, FloatRegisters destination, FloatRegisters source)
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
        public static void MultRegisterToRegister(IList<byte> codeGenerator, Registers destination, Registers source, bool is32bits = false)
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
        public static void MultRegisterToRegister(IList<byte> codeGenerator, NumberedRegisters destination, NumberedRegisters source)
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
        public static void MultRegisterToRegister(IList<byte> codeGenerator, NumberedRegisters destination, Registers source)
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
        public static void MultRegisterToRegister(IList<byte> codeGenerator, Registers destination, NumberedRegisters source)
        {
            codeGenerator.Add(0x49);
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
        public static void MultRegisterToRegister(IList<byte> codeGenerator, FloatRegisters destination, FloatRegisters source)
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
        public static void DivRegisterFromRegister(IList<byte> codeGenerator, Registers destination, Registers source, bool is32bits = false)
        {
            if (destination != Registers.AX)
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
        public static void DivRegisterFromRegister(IList<byte> codeGenerator, Registers destination, NumberedRegisters source)
        {
            if (destination != Registers.AX)
            {
                throw new ArgumentException("Only the AX register is supported as destination.");
            }

            codeGenerator.Add(0x49);
            codeGenerator.Add(0xf7);
            codeGenerator.Add((byte)(0xf8 | (byte)source | (byte)((byte)destination << 3)));
        }

        /// <summary>
        /// Divides the second register from the first
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="destination">The destination register</param>
        /// <param name="source">The source register</param>
        public static void DivRegisterFromRegister(IList<byte> codeGenerator, FloatRegisters destination, FloatRegisters source)
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
        public static void AndRegisterToRegister(IList<byte> codeGenerator, Registers destination, Registers source, bool is32bits = false)
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
        public static void OrRegisterToRegister(IList<byte> codeGenerator, Registers dest, Registers src, bool is32bits = false)
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
        public static void XorRegisterToRegister(IList<byte> codeGenerator, Registers destination, Registers source, bool is32bits = false)
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
        public static void XorRegisterToRegister(IList<byte> codeGenerator, NumberedRegisters destination, NumberedRegisters source)
        {
            codeGenerator.Add(0x4d);
            codeGenerator.Add(0x31);
            codeGenerator.Add((byte)(0xc0 | (byte)destination | (byte)((byte)source << 3)));
        }

        /// <summary>
        /// NOT's the register
        /// </summary>
        /// <param name="codeGenerator">The coder generator</param>
        /// <param name="reg"></param>
        /// <param name="is32bits">Indicates if a 32-bits register</param>
        public static void NotRegister(IList<byte> codeGenerator, Registers reg, bool is32bits = false)
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
        public static void CompareRegisterToRegister(IList<byte> codeGenerator, Registers register1, Registers register2)
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
        public static void CompareRegisterToRegister(IList<byte> codeGenerator, NumberedRegisters register1, NumberedRegisters register2)
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
        public static void CompareRegisterToRegister(IList<byte> codeGenerator, NumberedRegisters register1, Registers register2)
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
        public static void CompareRegisterToRegister(IList<byte> codeGenerator, Registers register1, NumberedRegisters register2)
        {
            codeGenerator.Add(0x4c);
            codeGenerator.Add(0x39);
            codeGenerator.Add((byte)(0xc0 | (byte)register1 | (byte)((byte)register2 << 3)));
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
