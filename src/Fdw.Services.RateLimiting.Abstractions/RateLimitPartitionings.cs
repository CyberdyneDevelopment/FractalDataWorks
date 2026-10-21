using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// The ways a rate limit policy can divide requests into buckets: one bucket for everyone, one for each caller, or one
/// for each address.
/// </summary>
[TypeCollection(typeof(RateLimitPartitioningBase), typeof(IRateLimitPartitioning), typeof(RateLimitPartitionings))]
[ExcludeFromCodeCoverage]
public abstract partial class RateLimitPartitionings : TypeCollectionBase<RateLimitPartitioningBase, IRateLimitPartitioning> { }
