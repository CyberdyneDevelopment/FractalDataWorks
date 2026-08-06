using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>High priority - less likely to be evicted.</summary>
[TypeOption(typeof(CachePriorities), "High")]
[ExcludeFromCodeCoverage]
public sealed class HighCachePriority : CachePriorityBase
{
    /// <summary>Initializes a new instance of <see cref="HighCachePriority"/>.</summary>
    public HighCachePriority() : base(2, "High") { }
}
