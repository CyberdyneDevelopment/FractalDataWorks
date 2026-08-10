using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Sliding window algorithm that smoothly distributes the request limit across time.
/// Uses weighted averages between current and previous window counts to prevent boundary bursts.
/// </summary>
[TypeOption(typeof(RateLimitAlgorithms), "SlidingWindow")]
[ExcludeFromCodeCoverage]
public sealed class SlidingWindowRateLimitAlgorithm : RateLimitAlgorithmBase
{
    /// <summary>Initializes a new instance of <see cref="SlidingWindowRateLimitAlgorithm"/>.</summary>
    public SlidingWindowRateLimitAlgorithm() : base(2, "SlidingWindow") { }

    /// <inheritdoc />
    public override RateLimiter CreateLimiter(IRateLimitPolicy policy) =>
        new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = policy.RequestsPerWindow,
            Window = policy.Window,
            SegmentsPerWindow = policy.SegmentsPerWindow,
            QueueLimit = policy.QueueExceededRequests ? policy.QueueLimit : 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
}
