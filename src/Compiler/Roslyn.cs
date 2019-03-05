
namespace Dynasor.Compiler
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Loader;
    using System.Threading;

    internal static class Roslyn
    {
        private static readonly CSharpCompilationOptions s_options = new CSharpCompilationOptions((OutputKind)2);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Diagnostic[] Compile(
            string pageOfTheCSharpCode,
            IEnumerable<MetadataReference> references,
            out Assembly assembly, 
            CancellationToken token = default)
        {
            var tree = CSharpSyntaxTree.ParseText(pageOfTheCSharpCode, cancellationToken: token);
           
            var virtualFileName = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(virtualFileName, new[] { tree }, references, s_options);

            using (var binaryStream = new MemoryStream())
            {
                var compilationResult = compilation.Emit(binaryStream, cancellationToken: token);
                if (compilationResult.Success)
                {
                    binaryStream.Seek(0, SeekOrigin.Begin);
                    assembly = AssemblyLoadContext.Default.LoadFromStream(binaryStream);
                    return Array.Empty<Diagnostic>();
                }
                else
                {
                    var failures = from d in compilationResult.Diagnostics
                                   where d.IsWarningAsError || d.Severity.Equals(DiagnosticSeverity.Error)
                                   select d;
                    assembly = default;
                    return failures.ToArray();
                }
            }
        }


    }
    
    
}
