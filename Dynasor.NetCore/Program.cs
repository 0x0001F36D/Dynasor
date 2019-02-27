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
            var sym = new Symbol("A").Return();


            Console.ReadKey(    );
            return;

            var add = Dynasor.CompileWithoutCache("int add(int a, int b)=>a+b;");
            Console.WriteLine(add(1, 2));

            var code = @"
void o(object o)
    => Console.WriteLine(o);";
            var o = Dynasor.CompileWithoutCache<Action<object>>(code);
            o("Hello");


            var pipeline = new Dynasor<Pipeline<int>>(@"
int pipeline(int a)
{
    return a+20;
}");
            var re2 = pipeline.Compile();
            Console.WriteLine(re2(10));

            var cross = Dynasor.CompileWithoutCache<Cross>(@"
void cross(ref SimpleTest t)
    =>t.Append(""Okay"");");
            var test = new SimpleTest("Not");
            cross(ref test);
            Console.WriteLine(test);


            Console.ReadKey();
        }
    }
}
