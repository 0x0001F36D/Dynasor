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
    using Dynasor.NetCore;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    public class TestClass
    {
        private readonly string _test;

        public TestClass(string test)
        {
            this._test = test ?? throw new ArgumentNullException(nameof(test));
        }

        public override string ToString() => this._test;
    }

    [TestClass]
    public class CrossRefs
    {

        public delegate T Creator<T>();

        [TestMethod]
        public void Reference()
        {
            var test = Dynasor.Invoke<Creator<TestClass>>(@"TestClass test() => new TestClass(""123"");");
            var d = test();
            Assert.IsInstanceOfType(d, typeof(TestClass));
            Assert.AreEqual(d.ToString(), "123");
        }
    }
}
