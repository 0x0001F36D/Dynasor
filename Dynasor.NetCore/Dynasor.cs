
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
    using System.Runtime.Loader;
    using System.Text;

    public static class Dynasor
    {

        private static string Using(string ns)
        {
            return $"using {ns};";
        }

        private static string RandomString()
        {
            var str = "_" + Guid.NewGuid().ToString().Replace("-", null);
            return str;
        }
        private static readonly CSharpCompilationOptions s_options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        
        private static void AppendRefs(StringBuilder sb, out HashSet<MetadataReference> references)
        {
            var namespaces = new HashSet<string>();
            references = new HashSet<MetadataReference>();

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.IsNullOrWhiteSpace(a.Location) && references.Add(MetadataReference.CreateFromFile(a.Location)))
                {
                    var nss = from type in a.GetTypes()
                              let ns = type.Namespace
                              where
                                  !string.IsNullOrWhiteSpace(ns)&& 
                                  //type.GetCustomAttribute<CompilerGeneratedAttribute>() == null&&
                                  type.IsPublic && 
                                  !ns.Contains("Internal", StringComparison.CurrentCultureIgnoreCase)
                              select ns;

                    foreach (var ns in nss)
                    {
                        if (namespaces.Add(ns))
                            sb.Append(Using(ns));
                    }
                }
            }
            namespaces.Clear();
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
