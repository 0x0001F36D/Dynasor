using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace Dynasor.NetCore
{
    public class SimpleTest
    {
        private string _name;

        public SimpleTest(string name)
        {
            this._name = name;
        }

        public void Append(string txt)
        {
            this._name += " " + txt;
        }
        public override string ToString() => this._name.ToString();
    }


    class Program
    {
        delegate void Cross(ref SimpleTest test);
        delegate T Pipeline<T>(T obj);


        static void Main(string[] args)
        {

            var dc = Dynasor.Invoke(new[]
            {
                @"static void a()=>Console.WriteLine(19);",
                "static void b(ref int i)=>Console.WriteLine(i+=2);",
                "static void c(out int i)=>i = 100;"
            });

            var aa = dc.Invoke("a");
            aa();
            var i = 555;
            var bb = dc.Invoke("b");
            bb(ref i);

            var cc = dc.Invoke("c");
            var k = 0;
            cc(ref k);
            Console.WriteLine(k);

            Console.ReadKey();

        }
    }
}
