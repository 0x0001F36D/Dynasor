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
namespace Dynasor.ConsoleTest
{
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Editing;
    using System;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    class Program
    {
        static Program()
        {
            Console.BufferHeight = short.MaxValue - 1;
        }

        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            var k = "int s(int vb) => vb + {0} ;";
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < 10; i++)
            {
                var s = string.Format(k, rnd.Next());
                sw.Start();
                using (var r = Dynasor.Compile<Func<int, int>>(s))
                {
                    sw.Stop();
                    Console.WriteLine(sw.Elapsed);
                    Console.WriteLine(r.Method(20));
                }
                sw.Reset();
            }


            Console.ReadKey();
            return ;
        }
        
    }
}