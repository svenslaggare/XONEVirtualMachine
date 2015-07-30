using System;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XONEVirtualMachine;
using XONEVirtualMachine.Compiler.Analysis;
using XONEVirtualMachine.Core;

namespace XONE_Virtual_Machine.Test.Programs
{
    /// <summary>
    /// Tests optimized functions
    /// </summary>
    [TestClass]
    public class TestOptimized
    {
        /// <summary>
        /// Tests a simple function
        /// </summary>
        [TestMethod]
        public void TestSimple()
        {
            using (var container = new Win64Container())
            {
                var func = TestProgramGenerator.Simple(container);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), 6);
            }
        }

        /// <summary>
        /// Tests a simple function
        /// </summary>
        [TestMethod]
        public void TestSimple2()
        {
            using (var container = new Win64Container())
            {
                var func = TestProgramGenerator.Simple2(container);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), 12);
            }
        }

        /// <summary>
        /// Tests a simple function
        /// </summary>
        [TestMethod]
        public void TestSimple3()
        {
            using (var container = new Win64Container())
            {
                var func = TestProgramGenerator.Simple3(container);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), 15);
            }
        }

        /// <summary>
        /// Tests a locals function
        /// </summary>
        [TestMethod]
        public void TestLocals()
        {
            using (var container = new Win64Container())
            {
                var func = TestProgramGenerator.Locals(container);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), 4);
            }
        }
    }
}
