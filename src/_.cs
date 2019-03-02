
namespace Dynasor
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

    public static class RuntimeDelegateFactory
    {
        private sealed class Signature : IEquatable< Signature>
        {

            internal Signature(Type returnType, params ParameterInfo[] parameters) : 
                this(returnType, Array.ConvertAll(parameters, x => x.ParameterType))
            {

            }

            internal Signature(Type returnType, params Type[] parameterTypes)
            {
                this.ReturnType = returnType;
                this.ParameterTypes = parameterTypes;
            }

            internal Type ReturnType { get; }
            internal Type[] ParameterTypes { get; }

            public override int GetHashCode()
            {
                var r = this.ReturnType.GetHashCode();
                for (int i = 0; i < this.ParameterTypes.Length; i++)
                {
                    if ((i % 3) == 0)
                        r += this.ParameterTypes[i].GetHashCode() << i;
                    else
                        r += this.ParameterTypes[i].GetHashCode() >> i;
                }
                return r;
            }

            public override bool Equals(object obj)
            {
                return obj is Signature s ? this.Equals(s) : false;
            }

            public bool Equals(Signature other)
            {
                if (this.ReturnType == other.ReturnType &&
                    this.ParameterTypes.Length == other.ParameterTypes.Length)
                {
                    for (int i = 0; i < this.ParameterTypes.Length; i++)
                    {
                        if (this.ParameterTypes[i] != other.ParameterTypes[i])
                            return false;
                    }
                    return true;
                }
                return false;
            }
        }


        private const MethodAttributes PUBLIC_HIDEBYSIG =
            MethodAttributes.Public |
            MethodAttributes.HideBySig;
        

        private static readonly ModuleBuilder s_module;
        private static readonly Dictionary<Signature, Type> s_caches;

        static RuntimeDelegateFactory()
        {
            const string SHARP = "#";
            var asmb = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(SHARP), AssemblyBuilderAccess.Run);
            s_module = asmb.DefineDynamicModule(SHARP);
            s_caches = new Dictionary<Signature, Type>();
        }

        private static readonly Type[] s_delegateConstructSignature = { typeof(object), typeof(IntPtr) };
        
        public static Delegate StaticMethod(Type target, string methodName)
        {
            return BindingMethond(target, methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        }
        private static Delegate BindingMethond(object target, string methodName, BindingFlags flags)
        {
            var m = target.GetType().GetMethod(methodName, flags);
            var dt = MockRuntimeDelegateType(m);
            var dele = Delegate.CreateDelegate(dt, target, methodName);
            return dele;
        }
        public static Delegate InstanceMethod(object target, string methodName)
        {
            return BindingMethond(target, methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static Type MockRuntimeDelegateType(MethodInfo mi)
        {
            if (mi == null)
                throw new ArgumentNullException(nameof(mi));

            if (mi.IsGenericMethod)
                throw new ArgumentException("Not supported generic method.");

            var prs = mi.GetParameters();
            var sign = new Signature(mi.ReturnType, prs);

            if(s_caches.TryGetValue(sign, out var t))
            {
                return t;
            }            
            
            var typeTemplate = s_module.DefineType($"#{Guid.NewGuid()}",
                TypeAttributes.Sealed | TypeAttributes.Public, typeof(MulticastDelegate));

            // 
            // [MethodImpl(MethodImplOptions.InternalCall)]
            // private extern void DelegateConstruct(object target, IntPtr slot);
            var ctor = typeTemplate.DefineConstructor(MethodAttributes.RTSpecialName | PUBLIC_HIDEBYSIG, CallingConventions.Standard, s_delegateConstructSignature);
            ctor.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);

          
            var invoke = typeTemplate.DefineMethod("Invoke", MethodAttributes.Virtual | PUBLIC_HIDEBYSIG, sign.ReturnType, sign.ParameterTypes);
            invoke.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            for (var stackIndex = prs.Length; stackIndex > 0;)
            {                
                invoke.DefineParameter(stackIndex--, prs[stackIndex].Attributes, prs[stackIndex].Name);
            }

            var delegateType = typeTemplate.CreateType();            
            s_caches.Add(sign, delegateType);
            return delegateType;
        }


    }

    public static class SnippetCaches
    {
        internal static readonly HashSet<MetadataReference> References;
        public static StringBuilder CreateNewSnippet()
        {
            return new StringBuilder(s_result);
        }
        
        private static readonly string s_result;
        static SnippetCaches()
        {
            AppendRefs(out var result, out var refs);
            s_result = result;
            References = refs;
        }


        private static string Using(string ns)
        {
            return string.IsNullOrWhiteSpace(ns)
                ? string.Empty
                : $"using {ns};\r\n";
        }
        
        private static void AppendRefs(out string result, out HashSet<MetadataReference> references)
        {
            var sb = new StringBuilder();
            var namespaces = new HashSet<string>();
            references = new HashSet<MetadataReference>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;

                if (!string.IsNullOrWhiteSpace(assembly.Location) && 
                    references.Add(MetadataReference.CreateFromFile(assembly.Location)))
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
            result = sb.ToString();
        }

    }

    internal static class Roslyn
    {
        private static readonly CSharpCompilationOptions s_options = new CSharpCompilationOptions((OutputKind)2);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Diagnostic[] TryCompile(
            string pageOfTheCSharpCode,
            IEnumerable<MetadataReference> references,
            out Assembly assembly,
            out CompilationUnitSyntax root,
            CancellationToken token = default)
        {
            var tree = CSharpSyntaxTree.ParseText(pageOfTheCSharpCode, cancellationToken: token);
            root = tree.GetCompilationUnitRoot(cancellationToken: token);
            var junk = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(junk, new[] { tree }, references, s_options);

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
