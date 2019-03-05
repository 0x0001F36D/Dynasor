
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