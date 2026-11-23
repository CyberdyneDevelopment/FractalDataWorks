using Fdw.Collections;
using Microsoft.Extensions.Caching.Memory;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// Base class for cache entry priority levels.
/// </summary>
public abstract class CachePriorityBase : TypeOptionBase<CachePriorityBase>, ICachePriority
{
    /// <summary>
    /// Initializes a new instance of <see cref="CachePriorityBase"/>.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="memoryPriority">The memory cache's own priority this level means.</param>
    protected CachePriorityBase(string name, CacheItemPriority memoryPriority) : base(name)
    {
        MemoryPriority = memoryPriority;
    }

    /// <inheritdoc />
    public CacheItemPriority MemoryPriority { get; }
}
