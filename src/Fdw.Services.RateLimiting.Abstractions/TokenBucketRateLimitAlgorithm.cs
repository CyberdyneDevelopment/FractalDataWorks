using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
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

    /// <inheritdoc />
    public override RateLimiter CreateLimiter(IRateLimitPolicy policy) =>
        new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = policy.AllowBurst ? policy.BurstLimit : policy.RequestsPerWindow,
            ReplenishmentPeriod = policy.Window,
            TokensPerPeriod = policy.RequestsPerWindow,
            QueueLimit = policy.QueueExceededRequests ? policy.QueueLimit : 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
}
