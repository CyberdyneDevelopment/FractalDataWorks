using Fdw.Collections;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Scoring weight for a Roslyn <c>SymbolKind</c>. Used by the name-resolution scoring pass to
/// prefer some kinds over others when multiple symbols share a name (e.g., prefer NamedType over
/// Field). The collection is keyed by the Roslyn enum's <c>ToString()</c> form so callers can
/// dispatch with <c>SymbolKindScores.ByName(symbol.Kind.ToString())</c>.
/// </summary>
public interface ISymbolKindScore : ITypeOption<int, ISymbolKindScore>
{
    /// <summary>Scoring weight added to a symbol's match score when its kind matches this option.</summary>
    int Weight { get; }
}
