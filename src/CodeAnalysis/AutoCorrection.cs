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
namespace Dynasor.CodeAnalysis
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using global::Dynasor.Utilities;
    using System.Reflection;
    using System;

    internal static class MethodAutoCorrection
    { 
        

        private readonly static SyntaxToken[] s_tokens =
        {
            SyntaxFactory.Token(SyntaxKind.StaticKeyword),
            SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
        };

        private readonly static StringBuilder s_sb = new StringBuilder();

        public static bool Validate(string code, out MethodDeclarationSyntax m)
        {
            var root = CSharpSyntaxTree.ParseText(code).GetCompilationUnitRoot();
            m = root.ChildNodes().SingleOrDefault() as MethodDeclarationSyntax;
            return m != null;
        }

        public static bool FixMethodModifiers(string code, out string result, out MethodSignatureAbstractInfo info)
        {
            if (!Validate(code, out var k))
            {
                result = default;
                info = default;
                return false;
            }

            var t = new List<SyntaxToken>(s_tokens);
            lock (s_sb)
            {
                s_sb.Append(code);
                info = new MethodSignatureAbstractInfo(k);
                var stack = info.Modifiers.ToStack();
                while (stack.TryPop(out var c))
                {
                    if (!t.Contains(c) || t.Count == 0)
                    {
                        s_sb.Remove(c.Span.Start, c.Span.Length + SyntaxFactory.Space.Span.Length);
                    }
                    else
                    {
                        t.Remove(c);
                        ((IList<SyntaxToken>)info.Modifiers).Remove(c);
                    }
                }

                foreach (var mdf in t)
                {
                    s_sb.Insert(0, SyntaxFactory.Space)
                        .Insert(0, mdf);
                    ((IList<SyntaxToken>)info.Modifiers).Add(mdf);
                }

                result = s_sb.ToString();
                s_sb.Clear();

            }
            return true;
        }
    }
}