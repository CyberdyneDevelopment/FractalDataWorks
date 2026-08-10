using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Fixed window algorithm that counts requests within discrete time windows.
/// Simple and memory-efficient, but can allow bursts at window boundaries.
/// </summary>
[TypeOption(typeof(RateLimitAlgorithms), "FixedWindow")]
[ExcludeFromCodeCoverage]
public sealed class FixedWindowRateLimitAlgorithm : RateLimitAlgorithmBase
{
    /// <summary>Initializes a new instance of <see cref="FixedWindowRateLimitAlgorithm"/>.</summary>
    public FixedWindowRateLimitAlgorithm() : base(1, "FixedWindow") { }

    /// <inheritdoc />
    public override RateLimiter CreateLimiter(IRateLimitPolicy policy) =>
        new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = policy.RequestsPerWindow,
            Window = policy.Window,
            QueueLimit = policy.QueueExceededRequests ? policy.QueueLimit : 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
}
