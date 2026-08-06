using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// TypeCollection for rate limiting algorithms.
/// </summary>
[TypeCollection(typeof(RateLimitAlgorithmBase), typeof(IRateLimitAlgorithm), typeof(RateLimitAlgorithms))]
[ExcludeFromCodeCoverage]
public abstract partial class RateLimitAlgorithms : TypeCollectionBase<RateLimitAlgorithmBase, IRateLimitAlgorithm> { }
