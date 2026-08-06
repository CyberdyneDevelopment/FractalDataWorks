using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>Low priority - may be evicted first under memory pressure.</summary>
[TypeOption(typeof(CachePriorities), "Low")]
[ExcludeFromCodeCoverage]
public sealed class LowCachePriority : CachePriorityBase
{
    /// <summary>Initializes a new instance of <see cref="LowCachePriority"/>.</summary>
    public LowCachePriority() : base(0, "Low") { }
}
