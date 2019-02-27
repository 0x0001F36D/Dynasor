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
    using System.Runtime.CompilerServices;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;

    internal static class CodeSnippet
    {
        internal static string Using(string ns)
        {
            return string.IsNullOrWhiteSpace(ns)
                ? string.Empty
                : $"using {ns};";
        }

        internal static string RandomString()
        {
            return "_" + Guid.NewGuid().ToString().Replace("-", null);
        }

        internal static void AppendRefs(StringBuilder sb, out HashSet<MetadataReference> references)
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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="className"></param>
        /// <param name="method">The code of the method.</param>
        /// <param name="references"></param>
        /// <returns></returns>
        internal static string GeneratePage(string className, IEnumerable<string> method, out HashSet<MetadataReference> references)
        {
            var sb = new StringBuilder();

            AppendRefs(sb, out references);

            sb.Append($"internal class {className}{{");
            foreach (var m in method)
            {
                sb.AppendLine(m);
            }
            sb.Append("}");

            return sb.ToString();
        }
        
    }

    public class DelegateCollection
    {
        private readonly IDictionary<string, Delegate> _collection;

        internal DelegateCollection(IDictionary<string, Delegate> collection)
        {
            this._collection = collection ?? throw new ArgumentNullException(nameof(collection));
            foreach (var d in this._collection)
            {
                Console.WriteLine((d.Key, d.Value.ToString(), d.Value.Method));
            }
        }

        public dynamic Invoke(string name)
        {
            if (this._collection.TryGetValue(name, out var d))
            {
                return d;
            }
            throw new InvalidOperationException();
        }
    }



    public static class Dynasor
    {
        private static readonly CSharpCompilationOptions s_options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        private const MethodAttributes PUBLIC_HIDEBYSIG = MethodAttributes.Public | MethodAttributes.HideBySig;
        private const BindingFlags PUBLIC_NONPUBLIC_STATIC = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

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

            var tree = CSharpSyntaxTree.ParseText(sb, cancellationToken: token);
            var root = tree.GetCompilationUnitRoot(cancellationToken: token);
            var junk = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(junk, new[] { tree }, references, s_options);

            using (var binaryStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(binaryStream, cancellationToken: token);
                if (emitResult.Success)
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
                    var failures = from d in emitResult.Diagnostics
                                   where d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error
                                   select d;
                    throw new CompilationException(failures.ToArray());
                }

            }
        }
        

        public static T Invoke<T>(string code, CancellationToken token = default) where T : Delegate
            => Compile(new[] { code }, (a, t, n) => typeof(T), token).First().Value as T;

        public static dynamic Invoke(string code, CancellationToken token = default)
            => Compile(new[] { code }, BuildDynamicDelegateType, token).First().Value;


        public static DelegateCollection Invoke(IEnumerable<string> code, CancellationToken token = default)
        {
            return new DelegateCollection(Compile(code, BuildDynamicDelegateType, token));
        }
    }
}
