using Fdw.Collections;
using Microsoft.Extensions.Caching.Memory;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// Interface for cache entry priority levels.
/// </summary>
public interface ICachePriority : ITypeOption<int, CachePriorityBase>
{
    /// <summary>Gets the memory cache's own priority this level means.</summary>
    CacheItemPriority MemoryPriority { get; }
}
