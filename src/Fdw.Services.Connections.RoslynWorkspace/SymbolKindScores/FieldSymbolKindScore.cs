using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>Score for <c>SymbolKind.Field</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SymbolKindScores), "Field")]
public sealed class FieldSymbolKindScore : SymbolKindScoreBase
{
    /// <summary>Initializes the Field scoring option.</summary>
    public FieldSymbolKindScore() : base(id: 4, name: "Field", weight: 2) { }
}
