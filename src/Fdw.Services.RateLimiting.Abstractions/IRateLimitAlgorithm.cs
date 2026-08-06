using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Interface for rate limiting algorithms.
/// </summary>
public interface IRateLimitAlgorithm : ITypeOption<int, RateLimitAlgorithmBase> { }
