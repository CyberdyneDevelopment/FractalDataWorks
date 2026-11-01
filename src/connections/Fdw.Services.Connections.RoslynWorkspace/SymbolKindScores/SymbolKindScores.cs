using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// TypeCollection of scoring weights per Roslyn <c>SymbolKind</c>, keyed by the enum's name.
/// Look up with <c>SymbolKindScores.ByName(symbol.Kind.ToString())</c>; compare against
/// <c>SymbolKindScores.NotFound</c> for kinds we don't score.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SymbolKindScoreBase), typeof(ISymbolKindScore), typeof(SymbolKindScores))]
public abstract partial class SymbolKindScores : TypeCollectionBase<SymbolKindScoreBase, ISymbolKindScore>
{
}
