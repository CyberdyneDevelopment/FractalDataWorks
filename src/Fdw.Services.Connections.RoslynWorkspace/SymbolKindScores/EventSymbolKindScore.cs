using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>Score for <c>SymbolKind.Event</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SymbolKindScores), "Event")]
public sealed class EventSymbolKindScore : SymbolKindScoreBase
{
    /// <summary>Initializes the Event scoring option.</summary>
    public EventSymbolKindScore() : base(id: 5, name: "Event", weight: 3) { }
}
