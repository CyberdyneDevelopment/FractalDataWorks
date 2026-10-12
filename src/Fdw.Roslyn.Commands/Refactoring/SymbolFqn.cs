using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring;

/// <summary>
/// Builds migration-guide fully-qualified names for symbols touched by refactoring translators.
/// </summary>
internal static class SymbolFqn
{
    private static readonly SymbolDisplayFormat Format = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    /// <summary>
    /// Gets the fully-qualified display name of <paramref name="symbol"/>.
    /// </summary>
    public static string Of(ISymbol symbol) => symbol.ToDisplayString(Format);

    /// <summary>
    /// Gets the fully-qualified name a sibling of <paramref name="symbol"/> named
    /// <paramref name="newName"/> would have — i.e. the symbol's container qualified name
    /// plus the new name.
    /// </summary>
    public static string OfRenamed(ISymbol symbol, string newName) =>
        symbol.ContainingSymbol is null
        || symbol.ContainingSymbol is INamespaceSymbol { IsGlobalNamespace: true }
            ? newName
            : symbol.ContainingSymbol.ToDisplayString(Format) + "." + newName;
}
