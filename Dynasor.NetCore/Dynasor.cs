
namespace Dynasor.NetCore
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
    using System.Reflection.Emit;
    using System.Runtime.Loader;
    using System.Text;

    public static class Dynasor
    {

        private static string Using(string ns)
        {
            return string.IsNullOrWhiteSpace(ns) ? string.Empty : $"using {ns};";
        }

        private static string RandomString()
        {
            return "_" + Guid.NewGuid().ToString().Replace("-", null);            
        }

        private static readonly CSharpCompilationOptions s_options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        private static void AppendRefs(StringBuilder sb, out HashSet<MetadataReference> references)
        {
            var namespaces = new HashSet<string>();
            references = new HashSet<MetadataReference>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;

                if (!string.IsNullOrWhiteSpace(assembly.Location) && references.Add(MetadataReference.CreateFromFile(assembly.Location)))
                {
                    var nss = from type in assembly.GetTypes()
                              let
                                ns = type.Namespace
                              where
                                  type.IsPublic &&
                                  !ns.Contains("Internal", StringComparison.CurrentCultureIgnoreCase) &&
                                  namespaces.Add(ns)
                              select ns;

                    foreach (var ns in nss)
                        sb.Append(Using(ns));
                }
            }
            namespaces.Clear();
        }

        private static Type BuildDelegateType(Assembly assembly, MethodInfo mi)
        {
            const MethodAttributes Public_HideBySig = MethodAttributes.Public | MethodAttributes.HideBySig;
            var asmb = AssemblyBuilder.DefineDynamicAssembly(assembly.GetName(), AssemblyBuilderAccess.Run);
            var modb = asmb.DefineDynamicModule("#");
            var typeTemplate = modb.DefineType($"#{Guid.NewGuid()}", TypeAttributes.Sealed | TypeAttributes.Public, typeof(MulticastDelegate));
            var ctor = typeTemplate.DefineConstructor(MethodAttributes.RTSpecialName | Public_HideBySig, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) });
            ctor.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            var delegateParameters = Array.ConvertAll(mi.GetParameters(), x => x.ParameterType);
            var invoke = typeTemplate.DefineMethod("Invoke", MethodAttributes.Virtual | Public_HideBySig, mi.ReturnType, delegateParameters);
            invoke.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            for (int i = delegateParameters.Length; i > 0;)
                invoke.DefineParameter(i--, ParameterAttributes.None, delegateParameters[i].Name);
            var delegateType = typeTemplate.CreateType();
            return delegateType;
        }
         
        public static dynamic CompileWithoutCache(string code)
        {
            var sb = new StringBuilder();

            AppendRefs(sb, out var references);
            var className = RandomString();
            sb.Append($"public static class {className}").Append("{").AppendLine("public static " + code).Append("}");


            var tree = CSharpSyntaxTree.ParseText(sb.ToString());
            var root = tree.GetCompilationUnitRoot();
            var junk = Path.GetRandomFileName();
            var compilation = CSharpCompilation.Create(junk, new[] { tree }, references, s_options);

            using (var binaryStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(binaryStream);
                if (emitResult.Success)
                {
                    try
                    {
                        binaryStream.Seek(0, SeekOrigin.Begin);
                        var assembly = AssemblyLoadContext.Default.LoadFromStream(binaryStream);
                        var type = assembly.GetType(className, true);
                        var mds = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
                        var methodName = mds.Identifier.ToString();
                        
                        var delegateType = BuildDelegateType(assembly, type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static));

                        var dele = Delegate.CreateDelegate(delegateType, type, methodName, false, true);

                        return dele;
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                else
                {
                    var failures = emitResult.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

                    Debug.WriteLine("Error:");
                    foreach (var f in failures)
                    {
                        Debug.WriteLine(f);
                    }
                    throw new CompilationException(failures);
                }
            }
        }


        public static T CompileWithoutCache<T>(string code)
            where T : Delegate
        {
            var sb = new StringBuilder();

            AppendRefs(sb, out var references);
            var className = RandomString();
            sb.Append($"public static class {className}")
                .Append("{")
                .AppendLine("public static " + code)
                .Append("}");


            var tree = CSharpSyntaxTree.ParseText(sb.ToString());
            var root = tree.GetCompilationUnitRoot();
            var junk = Path.GetRandomFileName();
            var compilation = CSharpCompilation.Create(junk, new[] { tree }, references, s_options);

            using (var binaryStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(binaryStream);
                if (emitResult.Success)
                {
                    try
                    {

                        binaryStream.Seek(0, SeekOrigin.Begin);
                        var assembly = AssemblyLoadContext.Default.LoadFromStream(binaryStream);
                        var type = assembly.GetType(className, true);
                        var mds = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
                        var methodName = mds.Identifier.ToString();
                        var dele = Delegate.CreateDelegate(typeof(T), type, methodName, false, true);

                        return dele as T;
                    }
                    catch (Exception e)
                    {

                        throw e;
                    }
                }
                else
                {
                    var failures = emitResult.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

                    Debug.WriteLine("Error:");
                    foreach (var f in failures)
                    {
                        Debug.WriteLine(f);
                    }
                    throw new CompilationException(failures);
                }
            }
        }
    }
}
