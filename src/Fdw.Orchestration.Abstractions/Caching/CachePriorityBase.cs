using Fdw.Collections;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// Base class for cache entry priority levels.
/// </summary>
public abstract class CachePriorityBase : TypeOptionBase<int, CachePriorityBase>, ICachePriority
{
    /// <summary>
    /// Initializes a new instance of <see cref="CachePriorityBase"/>.
    /// </summary>
    protected CachePriorityBase(int id, string name) : base(id, name) { }
}
