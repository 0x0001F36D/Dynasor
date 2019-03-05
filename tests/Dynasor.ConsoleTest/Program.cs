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