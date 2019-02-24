
namespace Dynasor.NetCore
{
    using Microsoft.CodeAnalysis;
    using System;
    using System.Runtime.Serialization;
     
    internal class CompilationException : Exception
    {
        public CompilationException(Diagnostic[] failures)
        {
            this.Failures = failures;
        }

        public Diagnostic[] Failures { get; }
    }
}