using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>Score for <c>SymbolKind.NamedType</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SymbolKindScores), "NamedType")]
public sealed class NamedTypeSymbolKindScore : SymbolKindScoreBase
{
    /// <summary>Initializes the NamedType scoring option.</summary>
    public NamedTypeSymbolKindScore() : base(name: "NamedType", weight: 6) { }
}
