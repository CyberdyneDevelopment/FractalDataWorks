using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>Will not be evicted due to memory pressure.</summary>
[TypeOption(typeof(CachePriorities), "NeverRemove")]
[ExcludeFromCodeCoverage]
public sealed class NeverRemoveCachePriority : CachePriorityBase
{
    /// <summary>Initializes a new instance of <see cref="NeverRemoveCachePriority"/>.</summary>
    public NeverRemoveCachePriority() : base(3, "NeverRemove") { }
}
