using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// Finds a type by its fully-qualified name without its generic arity.
/// </summary>
/// <remarks>
/// A guide names <c>SecretManagerTypeBase</c>, not <c>SecretManagerTypeBase`3</c>, so metadata-name lookup
/// cannot be used: it needs the arity, and a guide that carried it would break the moment a type gained a
/// parameter.
/// </remarks>
internal static class GuideTypeResolver
{
    /// <summary>
    /// Resolves <paramref name="fullName"/> across the compilation and everything it references.
    /// </summary>
    /// <param name="compilation">The compilation to search.</param>
    /// <param name="fullName">The namespace-qualified type name.</param>
    /// <returns>The first type with that name, or null when none is declared or referenced.</returns>
    public static INamedTypeSymbol? Resolve(Microsoft.CodeAnalysis.Compilation compilation, string fullName)
    {
        var split = fullName.LastIndexOf('.');
        if (split <= 0) return null;

        INamespaceSymbol? current = compilation.GlobalNamespace;
        foreach (var part in fullName[..split].Split('.'))
        {
            current = current.GetNamespaceMembers()
                .FirstOrDefault(n => string.Equals(n.Name, part, StringComparison.Ordinal));
            if (current is null) return null;
        }

        return current.GetTypeMembers(fullName[(split + 1)..]).FirstOrDefault();
    }

    /// <summary>
    /// Gets the namespace-qualified name of a type, without generic arity.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The name a guide would use for it.</returns>
    public static string FullName(INamedTypeSymbol type) =>
        type.ContainingNamespace.ToDisplayString() + "." + type.Name;
}
