using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// Finds the members of an abstraction that a concrete type leaves unimplemented.
/// </summary>
internal static class GuideRequiredMembers
{
    /// <summary>
    /// Lists every abstract or interface member of the abstractions that <paramref name="target"/> does not supply.
    /// </summary>
    /// <param name="target">The type under inspection.</param>
    /// <param name="abstractions">The interfaces and base classes a guide names.</param>
    /// <returns>The display name of each missing member; empty for an abstract target, which is not yet complete.</returns>
    public static IReadOnlyList<string> Unimplemented(
        INamedTypeSymbol target,
        IEnumerable<INamedTypeSymbol> abstractions)
    {
        if (target.IsAbstract) return Array.Empty<string>();

        var missing = new List<string>();
        foreach (var abstraction in abstractions)
        {
            foreach (var member in abstraction.GetMembers().Where(IsRequired))
            {
                if (!IsSupplied(target, abstraction, member))
                    missing.Add(member.ToDisplayString());
            }
        }

        return missing;
    }

    private static bool IsRequired(ISymbol member) =>
        member.IsAbstract
        && !member.IsStatic
        && member is not IMethodSymbol { AssociatedSymbol: not null };

    private static bool IsSupplied(INamedTypeSymbol target, INamedTypeSymbol abstraction, ISymbol member)
    {
        if (abstraction.TypeKind == TypeKind.Interface)
            return target.FindImplementationForInterfaceMember(member) is not null;

        for (var current = target; current is not null; current = current.BaseType)
        {
            foreach (var candidate in current.GetMembers(member.Name))
            {
                if (Overrides(candidate, member)) return true;
            }
        }

        return false;
    }

    private static bool Overrides(ISymbol candidate, ISymbol abstractMember)
    {
        var overridden = candidate switch
        {
            IMethodSymbol method => method.OverriddenMethod as ISymbol,
            IPropertySymbol property => property.OverriddenProperty,
            IEventSymbol evt => evt.OverriddenEvent,
            _ => null
        };

        while (overridden is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(overridden.OriginalDefinition, abstractMember.OriginalDefinition))
                return true;

            overridden = overridden switch
            {
                IMethodSymbol method => method.OverriddenMethod,
                IPropertySymbol property => property.OverriddenProperty,
                IEventSymbol evt => evt.OverriddenEvent,
                _ => null
            };
        }

        return false;
    }
}
