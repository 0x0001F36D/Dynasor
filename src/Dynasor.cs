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
namespace Dynasor
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Runtime.Loader;
    using System.Threading;
    
    public static class Dynasor
    {
        private static readonly CSharpCompilationOptions s_options = 
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        private const MethodAttributes PUBLIC_HIDEBYSIG = 
            MethodAttributes.Public | 
            MethodAttributes.HideBySig;

        private const BindingFlags PUBLIC_NONPUBLIC_STATIC = 
            BindingFlags.NonPublic | 
            BindingFlags.Static | 
            BindingFlags.Public;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Type BuildDynamicDelegateType(Assembly assembly, Type type, string methodName)
        {
            var mi = type.GetMethod(methodName, PUBLIC_NONPUBLIC_STATIC);

            var asmb = AssemblyBuilder.DefineDynamicAssembly(assembly.GetName(), AssemblyBuilderAccess.Run);
            var modb = asmb.DefineDynamicModule("#");
            var typeTemplate = modb.DefineType($"#{Guid.NewGuid()}", TypeAttributes.Sealed | TypeAttributes.Public, typeof(MulticastDelegate));
            var ctor = typeTemplate.DefineConstructor(MethodAttributes.RTSpecialName | PUBLIC_HIDEBYSIG, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) });
            ctor.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);

            var delegateParameters = Array.ConvertAll(mi.GetParameters(), x => x.ParameterType);

            var invoke = typeTemplate.DefineMethod("Invoke", MethodAttributes.Virtual | PUBLIC_HIDEBYSIG, mi.ReturnType, delegateParameters);
            invoke.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            for (int i = delegateParameters.Length; i > 0;)
                invoke.DefineParameter(i--, ParameterAttributes.None, delegateParameters[i].Name);

            var delegateType = typeTemplate.CreateType();
            return delegateType;
        }
        
        private delegate Type DynamicDelegateBuilder(Assembly assembly,Type type, string methodName);
        
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IDictionary<string, Delegate> Compile(
            IEnumerable<string> codes, 
            DynamicDelegateBuilder delegateTypeCreator,
            CancellationToken token)
        {
            var className = CodeSnippet.RandomString();
            var sb = CodeSnippet.GeneratePage(className,codes, out var references);

            Debug.WriteLine(sb);

            var tree = CSharpSyntaxTree.ParseText(sb, cancellationToken: token);
            var root = tree.GetCompilationUnitRoot(cancellationToken: token);
            var junk = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(junk, new[] { tree }, references, s_options);

            using (var binaryStream = new MemoryStream())
            {
                var compilationResult = compilation.Emit(binaryStream, cancellationToken: token);
                if (compilationResult.Success)
                {
                    binaryStream.Seek(0, SeekOrigin.Begin);
                    var assembly = AssemblyLoadContext.Default.LoadFromStream(binaryStream);
                    var type = assembly.GetType(className, true);
                    var list = new Dictionary<string, Delegate>();
                    foreach (var md in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        var methodName = md.Identifier.ToString();
                        var delegateType = delegateTypeCreator(assembly, type, methodName);
                        var dele = Delegate.CreateDelegate(delegateType, type, methodName, false, true);
                        list.Add(methodName,dele);
                    }

                    return list;
                }
                else
                {
                    var failures = from d in compilationResult.Diagnostics
                                   where d.IsWarningAsError || d.Severity.Equals(DiagnosticSeverity.Error)
                                   select d;

                    throw new CompilationException(failures.ToArray());
                }
            }
        }


        public static T Invoke<T>(string code, CancellationToken token = default) where T : Delegate
            => Compile(new[] { code }, (a, t, n) => typeof(T), token).First().Value as T;

        public static dynamic Invoke(string code, CancellationToken token = default)
            => Compile(new[] { code }, BuildDynamicDelegateType, token).First().Value;

        public static dynamic Invoke(IEnumerable<string> code, CancellationToken token = default)
            => new DelegateCollection(Compile(code, BuildDynamicDelegateType, token));
        
    }
}
