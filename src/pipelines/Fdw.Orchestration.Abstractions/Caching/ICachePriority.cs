using Fdw.Collections;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// Interface for cache entry priority levels.
/// </summary>
public interface ICachePriority : ITypeOption<int, CachePriorityBase> { }
