using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Token bucket algorithm that replenishes tokens at a steady rate.
/// Allows controlled bursts while maintaining an average rate limit.
/// </summary>
[TypeOption(typeof(RateLimitAlgorithms), "TokenBucket")]
[ExcludeFromCodeCoverage]
public sealed class TokenBucketRateLimitAlgorithm : RateLimitAlgorithmBase
{
    /// <summary>Initializes a new instance of <see cref="TokenBucketRateLimitAlgorithm"/>.</summary>
    public TokenBucketRateLimitAlgorithm() : base(3, "TokenBucket") { }
}
