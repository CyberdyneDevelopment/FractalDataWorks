using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// TypeCollection for cache entry priority levels.
/// </summary>
[TypeCollection(typeof(CachePriorityBase), typeof(ICachePriority), typeof(CachePriorities))]
[ExcludeFromCodeCoverage]
public abstract partial class CachePriorities : TypeCollectionBase<CachePriorityBase, ICachePriority> { }
