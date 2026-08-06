using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>Score for <c>SymbolKind.Property</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SymbolKindScores), "Property")]
public sealed class PropertySymbolKindScore : SymbolKindScoreBase
{
    /// <summary>Initializes the Property scoring option.</summary>
    public PropertySymbolKindScore() : base(id: 2, name: "Property", weight: 4) { }
}
