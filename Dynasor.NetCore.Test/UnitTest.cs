
namespace Dynasor.NetCore.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Diagnostics;

    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            try
            {
                var add = Dynasor.CompileWithoutCache("int add(int a, int b)=>a+b;");
                Assert.AreEqual(add(1, 2), 3);
            }
            catch (CompilationException)
            {
                Assert.Fail();
            }
        }
    }
}
