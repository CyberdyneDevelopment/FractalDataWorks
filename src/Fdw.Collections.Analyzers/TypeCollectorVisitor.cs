using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Collections.Analyzers;

internal sealed class TypeCollectorVisitor : SymbolVisitor
{
    public List<INamedTypeSymbol> Types { get; } = [];

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            member.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        Types.Add(symbol);

        // Visit nested types
        foreach (var nestedType in symbol.GetTypeMembers())
        {
            nestedType.Accept(this);
        }
    }
}
