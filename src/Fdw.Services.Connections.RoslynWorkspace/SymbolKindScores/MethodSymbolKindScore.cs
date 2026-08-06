using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>Score for <c>SymbolKind.Method</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SymbolKindScores), "Method")]
public sealed class MethodSymbolKindScore : SymbolKindScoreBase
{
    /// <summary>Initializes the Method scoring option.</summary>
    public MethodSymbolKindScore() : base(id: 1, name: "Method", weight: 5) { }
}
