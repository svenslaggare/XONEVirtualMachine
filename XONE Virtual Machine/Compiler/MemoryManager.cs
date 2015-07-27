using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XONEVirtualMachine.Compiler
{
    /// <summary>
    /// Represents a memory manager
    /// </summary>
    public class MemoryManager : IDisposable
    {
        private readonly IList<CodePage> pages = new List<CodePage>();
        private CodePage activePage = null; 

        private readonly int pageSize = 4096;

        /// <summary>
        /// Creates a new page
        /// </summary>
        /// <param name="minSize">The minimum size required by the page</param>
        private CodePage CreatePage(int minSize)
        {
            int size = (minSize + (this.pageSize - 1) / this.pageSize) * this.pageSize;

            //Allocate writable & readable memory
            var memory = WinAPI.VirtualAlloc(
                IntPtr.Zero,
                (uint)size,
                WinAPI.AllocationType.Commit,
                WinAPI.MemoryProtection.ReadWrite);

            var page = new CodePage(memory, size);
            this.pages.Add(page);
            return page;
        }

        /// <summary>
        /// Allocates memory of the given size
        /// </summary>
        /// <param name="size">The amount to allocate</param>
        /// <returns>Pointer to the allocated memory</returns>
        public IntPtr Allocate(int size)
        {
            if (this.activePage == null)
            {
                this.activePage = this.CreatePage(size);
                return this.activePage.Allocate(size).Value;
            }
            else
            {
                var memory = this.activePage.Allocate(size);

                //Check if active page has any room
                if (memory != null)
                {
                    return memory.Value;
                }
                else
                {
                    this.activePage = this.CreatePage(size);
                    return this.activePage.Allocate(size).Value;
                }
            }
        }
        
        /// <summary>
        /// Makes the allocated memory executable (and not writable)
        /// </summary>
        public void MakeExecutable()
        {
            foreach (var page in this.pages)
            {
                page.SetProtectionMode(WinAPI.MemoryProtection.ExecuteRead);
            }
        }

        /// <summary>
        /// Disposes the pages
        /// </summary>
        public void Dispose()
        {
            foreach (var page in this.pages)
            {
                page.Dispose();
            }
        }
    }
}
