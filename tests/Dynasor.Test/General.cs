/*
    Copyright 2019 Viyrex

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/
namespace Dynasor.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using System;

    [TestClass]
    public class General
    {
        [TestMethod]
        public void TestMethod1()
        {
            try
            {
                var add = Dynasor.Compile("int add(int a, int b)=>a+b;");
                Assert.AreEqual(add(1, 2), 3);
            }
            catch (CompilationException)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            try
            {
                using (var add = Dynasor.Compile<Func<int, int, int>>("int add(int a, int b)=>a+b;"))
                    Assert.AreEqual(add.Method(1, 2), 3);
            }
            catch (CompilationException)
            {
                Assert.Fail();
            }
        }

        /*
        [TestMethod]
        public void TestMethod3()
        {
            try
            {
                var o = Dynasor.Compile(new string[]
                {
                    "int add(int a, int b)=>a+b;",
                    "int sub(int a, int b)=>a-b;"
                });
                Assert.AreEqual(o.add(1, 2), 3);
                Assert.AreEqual(o.sub(1, 2), -1);
            }
            catch (CompilationException)
            {
                Assert.Fail();
            }
        }
        */
    }
}
