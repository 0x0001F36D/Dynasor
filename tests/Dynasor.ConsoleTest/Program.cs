namespace Dynasor.ConsoleTest
{
    using Microsoft.CodeAnalysis;
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    class Program
    {
        static void Main(string[] args)
        {


            var dele = RuntimeDelegateFactory.InstanceMethod(new Program(), "Main");
            Console.WriteLine(dele?.Method);

           // var type = RuntimeDelegateFactory.MockRuntimeDelegateType(dele.Method);
           // Console.WriteLine(type);

            Console.ReadKey();
        }

    }
}