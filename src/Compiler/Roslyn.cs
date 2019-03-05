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
