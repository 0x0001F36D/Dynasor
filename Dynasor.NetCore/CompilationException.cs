
namespace Dynasor.NetCore
{
    using Microsoft.CodeAnalysis;
    using System;
    using System.Runtime.Serialization;
     
    public sealed class CompilationException : Exception
    {
        internal CompilationException(Diagnostic[] failures)
        {
            this.Failures = failures;
        }

        public Diagnostic[] Failures { get; }
    }
}