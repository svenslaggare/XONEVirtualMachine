using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Compiler
{
    /// <summary>
    /// Represents a managed disassembler
    /// </summary>
    public class Disassembler
    {
        private readonly INativeDisassembler nativeDisassembler;
        private readonly AbstractCompilationData compilationData;
        private string disassembledCode = "";

        /// <summary>
        /// Creates a new managed disassembler
        /// </summary>
        /// <param name="compilationData">The compilation data</param>
        /// <param name="createDisassembler">Function to create the disassembler</param>
        public Disassembler(AbstractCompilationData compilationData, Func<AbstractCompilationData, INativeDisassembler> createDisassembler)
        {
            this.compilationData = compilationData;
            this.nativeDisassembler = createDisassembler(compilationData);
        }

        /// <summary>
        /// Disassembles the loaded function
        /// </summary>
        private void DisassembleFunction()
        {
            var output = new StringBuilder();
            var instructions = compilationData.Function.Instructions;

            output.AppendLine(compilationData.Function.ToString());

            //Disassemble the prolog
            if (compilationData.InstructionMapping[0] != 0)
            {
                output.AppendLine("<prolog>");
                this.nativeDisassembler.DisassembleBlock(0, compilationData.InstructionMapping[0], output);
            }

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];
                int start = compilationData.InstructionMapping[i];
                int nextStart = 0;

                if (i + 1 < instructions.Count)
                {
                    nextStart = compilationData.InstructionMapping[i + 1];
                }
                else
                {
                    nextStart = compilationData.Function.GeneratedCode.Count;
                }

                int size = nextStart - start;
                output.AppendLine(instruction.Disassemble());
                this.nativeDisassembler.DisassembleBlock(start, size, output);
            }

            this.disassembledCode = output.ToString();
        }

        /// <summary>
        /// Disassembles the given function
        /// </summary>
        public string Disassemble()
        {
            if (string.IsNullOrEmpty(this.disassembledCode))
            {
                this.DisassembleFunction();
            }

            return this.disassembledCode;
        }
    }
}
