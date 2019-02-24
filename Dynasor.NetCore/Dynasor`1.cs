
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
    using System.Runtime.CompilerServices;
    using System.Runtime.Loader;
    using System.Text;


    public class Dynasor<T> where T : Delegate
    {
        private readonly string _code;

        private string Using(string ns)
        {
            return $"using {ns};";
        }

        private string RandomString()
        {
            var str = "_"+Guid.NewGuid().ToString().Replace("-", null);
            return str;
        }
        private static readonly CSharpCompilationOptions s_options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        public Dynasor(string code)
        {
            this._code = code;
        }

        private void AppendRefs(StringBuilder sb, out HashSet<MetadataReference> references)
        {
            var namespaces = new HashSet<string>();
            references = new HashSet<MetadataReference>();

            var location = default(string);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    location = a.Location;
                }
                catch (NotSupportedException)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(location) && references.Add(MetadataReference.CreateFromFile(location)))
                {
                    var nss = from type in a.GetTypes()
                              let ns = type.Namespace
                              where
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

        private T _cache;
        
        public T Compile()
        {
            if (this._cache is T t)
                return t;
             
            var sb = new StringBuilder();

            this.AppendRefs(sb, out var references);
            var className = this.RandomString();
            sb.Append($"public static class {className}")
                .Append("{")
                .AppendLine("public static " + this._code)
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
                        this._cache = Delegate.CreateDelegate(typeof(T), type, methodName, false, true) as T;

                        return this._cache;
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
