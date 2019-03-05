﻿/*
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
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public interface IMethodSignature
    {
        IReadOnlyList<AttributeSyntax> AttributeSyntaxes { get; }
        IReadOnlyList<TypeParameterConstraintClauseSyntax> ConstraintClauses { get; }
        IReadOnlyList<SyntaxToken> Modifiers { get; }
        SyntaxToken Name { get; }
        IReadOnlyList<ParameterSyntax> Parameters { get; }
        TypeSyntax ReturnType { get; }
        IReadOnlyList<TypeParameterSyntax> TypeParameters { get; }
    }
}