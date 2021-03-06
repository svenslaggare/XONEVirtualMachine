﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XONEVirtualMachine;
using XONEVirtualMachine.Compiler;

namespace XONE_Virtual_Machine.Test
{
    /// <summary>
    /// Contains test related helper methods
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Saves the disassembled functions to a file for the given container
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="fileName">The name of the file</param>
        public static void SaveDisassembledFunctions(Win64Container container, string fileName)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create))
            using (var writer = new StreamWriter(fileStream))
            {
                foreach (var assembly in container.VirtualMachine.LoadedAssemblies)
                {
                    foreach (var function in assembly.Functions)
                    {
                        var disassembler = new Disassembler(
                            container.VirtualMachine.Compiler.GetCompilationData(function),
                            x => new XONEVirtualMachine.Compiler.Win64.Disassembler(x),
                            DisassemblerOptions.NewLineAfterInstruction);

                        writer.WriteLine(disassembler.Disassemble());
                    }
                }
            }
        }
    }
}
