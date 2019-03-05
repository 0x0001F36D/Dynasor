
namespace Dynasor.Reflection
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Reflection.Emit;

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

            var m = target.GetMethod(methodName, (BindingFlags)48 | BindingFlags.Static);
            var dt = MockRuntimeDelegateType(m);
            var dele = Delegate.CreateDelegate(dt, target, methodName);
            return dele;
        }

        public static Delegate InstanceMethod(object target, string methodName)
        {
            var m = target.GetType().GetMethod(methodName, (BindingFlags)48 | BindingFlags.Instance);
            var dt = MockRuntimeDelegateType(m);
            var dele = Delegate.CreateDelegate(dt, target, methodName);
            return dele;
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
    
    
}
