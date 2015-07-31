using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        /// <summary>
        /// Tests a function with a loop
        /// </summary>
        [TestMethod]
        public void TestLoop()
        {
            using (var container = new Win64Container())
            {
                int count = 100;
                var func = TestProgramGenerator.LoopCount(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), count);
            }
        }

        /// <summary>
        /// Tests the sum function
        /// </summary>
        [TestMethod]
        public void TestSum()
        {
            using (var container = new Win64Container())
            {
                int count = 100;
                var func = TestProgramGenerator.SumNoneLoop(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), (count * (count + 1)) / 2);
            }
        }

        /// <summary>
        /// Tests the sum function with a locals
        /// </summary>
        [TestMethod]
        public void TestSumLocal()
        {
            using (var container = new Win64Container())
            {
                int count = 100;
                var func = TestProgramGenerator.SumNoneLoopLocal(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), (count * (count + 1)) / 2);
            }
        }


        /// <summary>
        /// Tests the product function
        /// </summary>
        [TestMethod]
        public void TestProduct()
        {
            using (var container = new Win64Container())
            {
                int count = 10;
                int product = Enumerable.Aggregate(Enumerable.Range(1, count), 1, (total, current) => total * current);
                var func = TestProgramGenerator.ProductNoneLoop(container, count);
                func.Optimize = true;
                container.LoadAssembly(Assembly.SingleFunction(func));
                Assert.AreEqual(container.Execute(), product);
            }
        }
    }
}
