using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Base class for rate limiting algorithms.
/// </summary>
public abstract class RateLimitAlgorithmBase : TypeOptionBase<int, RateLimitAlgorithmBase>, IRateLimitAlgorithm
{
    /// <summary>
    /// Initializes a new instance of <see cref="RateLimitAlgorithmBase"/>.
    /// </summary>
    protected RateLimitAlgorithmBase(int id, string name) : base(id, name) { }
}
