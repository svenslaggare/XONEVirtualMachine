using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeaEngine.Net;

namespace XONEVirtualMachine.Compiler.Win64
{
    /// <summary>
    /// Represents a disassembler
    /// </summary>
    public static class Disassembler
    {
        /// <summary>
        /// Disassembles the given code
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        public static string Disassemble(IList<byte> generatedCode)
        {
            var strBuffer = new StringBuilder();
            var buffer = new UnmanagedBuffer(generatedCode.ToArray());

            var disasm = new Disasm();
            disasm.Archi = 64;
            disasm.EIP = new IntPtr(buffer.Ptr.ToInt64());

            int offset = 0;
            while (offset < generatedCode.Count)
            {
                disasm.EIP = new IntPtr(buffer.Ptr.ToInt64() + offset);
                int result = BeaEngine64.Disasm(disasm);

                if (result == (int)BeaConstants.SpecialInfo.UNKNOWN_OPCODE)
                {
                    break;
                }

                //Console.WriteLine("0x" + offset.ToString("X") + " " + disasm.CompleteInstr);
                strBuffer.AppendLine(disasm.CompleteInstr);
                offset += result;
            }

            return strBuffer.ToString();
        }
    }
}
