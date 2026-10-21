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
    public TokenBucketRateLimitAlgorithm() : base("TokenBucket") { }

    /// <inheritdoc />
    public override RateLimiter CreateLimiter(RateLimitAllowance allowance) =>
        new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = allowance.AllowBurst ? allowance.BurstLimit : allowance.RequestsPerWindow,
            ReplenishmentPeriod = allowance.Window,
            TokensPerPeriod = allowance.RequestsPerWindow,
            QueueLimit = allowance.QueueExceededRequests ? allowance.QueueLimit : 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
}
