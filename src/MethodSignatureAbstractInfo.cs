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
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    internal class MethodSignatureAbstractInfo : IMethodSignature
    {
        internal MethodSignatureAbstractInfo(MethodDeclarationSyntax m)
        {
            this.AttributeSyntaxes = m.AttributeLists.SelectMany(a => a.Attributes).ToList();
            this.Modifiers = m.Modifiers.ToList();
            this.ReturnType = m.ReturnType;
            this.Name = m.Identifier;
            this.TypeParameters = m.TypeParameterList?.Parameters.ToList() ?? default;
            this.Parameters = m.ParameterList.Parameters.ToList() ?? default;
            this.ConstraintClauses = m.ConstraintClauses.ToList();
        }

        public IReadOnlyList<AttributeSyntax> AttributeSyntaxes { get; }
        public IReadOnlyList<SyntaxToken> Modifiers { get; }
        public TypeSyntax ReturnType { get; }
        public SyntaxToken Name { get; }
        public IReadOnlyList<TypeParameterSyntax> TypeParameters { get; }
        public IReadOnlyList<ParameterSyntax> Parameters { get; }
        public IReadOnlyList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }
    }
}