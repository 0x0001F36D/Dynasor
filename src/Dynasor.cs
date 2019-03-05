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
    using global::Dynasor.CodeAnalysis;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Dynasor.Compiler;
    using System.Collections.Generic;
    using global::Dynasor.Reflection;

    public static class Dynasor
    { 


        [DebuggerStepThrough]
        public static dynamic Compile(CotMethod cotMethod, CancellationToken token = default)
        {
            var rndClass = CodeSnippet.RandomString();
            var pageOfCode = CodeSnippet.GeneratePage(rndClass, new[] { cotMethod.ToString() }, out var refs);

            var errors = Roslyn.Compile(pageOfCode, refs, out var asm, token);

            if (errors.Length == 0)
            {
                var t = asm.GetType(rndClass);
                try
                {
                    var mn = cotMethod.Info.Name.ValueText;
                    var d = RuntimeDelegateFactory.StaticMethod(t, mn);
                    return d;
                }
                catch (ArgumentException)
                {
                    throw;
                }
            }
            throw new CompilationException(errors);

        }


        /// <summary>
        /// 執行期編譯
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cotMethod"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static DelegateWrapper<T> Compile<T>(CotMethod cotMethod, CancellationToken token = default) 
            where T : Delegate
        {
            var rndClass = CodeSnippet.RandomString();
            var pageOfCode = CodeSnippet.GeneratePage(rndClass, new[] { cotMethod.ToString() }, out var refs);

            var errors = Roslyn.Compile(pageOfCode, refs, out var asm, token);

            if (errors.Length == 0)
            {
                var t = asm.GetType(rndClass);
                try
                {
                    var mn = cotMethod.Info.Name.ValueText;
                    var d = Delegate.CreateDelegate(typeof(T), t, mn, false, true) as T;
                    return new DelegateWrapper<T>(d, mn);
                }
                catch (ArgumentException)
                {
                    throw;
                }
            }
            throw new CompilationException(errors);

        }

        private class Box
        {
        }
    }

    public sealed class DelegateWrapper<T> : IDisposable
        where T : Delegate 
    {
        private volatile T _dele;

        internal DelegateWrapper(T dele, string name)
        {
            this._dele = dele;
            this.Name = name;
        }

        public string Name { get; }


        public T Method => this._dele;


        public void Dispose()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    /// <summary>
    /// Code of the method.
    /// </summary>
    [DebuggerDisplay("{DebugString}")]
    public sealed class CotMethod
    {
        private string DebugString => this._code;

        internal MethodSignatureAbstractInfo Info { get; }

        private readonly int _hash;
        private readonly string _code;

        private CotMethod(string code, MethodSignatureAbstractInfo info)
        {
            this._hash = code.GetHashCode();
            this._code = code.ToString();
            this.Info = info;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => this._code;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => this._hash;

        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public static implicit operator CotMethod(string code)
        { 
            if (!MethodAutoCorrection.FixMethodModifiers(code, out code, out var info))
            {
                throw new FormatException("Input code was not in a correct C# method declaration syntax.");
            }
            return new CotMethod(code, info);
        }

        public static implicit operator CotMethod(ReadOnlySpan<char> code)
            => code.ToString();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            if (obj is string s)
            {
                return s.GetHashCode() == this.GetHashCode();
            }
            else if( obj is CotMethod c)
            {
                return c._hash == this.GetHashCode();
            }
            return false;
        }
    }
    

    
}