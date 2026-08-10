using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Concurrency limiter that restricts the number of simultaneous active requests.
/// Unlike time-based algorithms, this limits how many requests can be processed at once.
/// </summary>
[TypeOption(typeof(RateLimitAlgorithms), "Concurrency")]
[ExcludeFromCodeCoverage]
public sealed class ConcurrencyRateLimitAlgorithm : RateLimitAlgorithmBase
{
    /// <summary>Initializes a new instance of <see cref="ConcurrencyRateLimitAlgorithm"/>.</summary>
    public ConcurrencyRateLimitAlgorithm() : base(4, "Concurrency") { }

    /// <inheritdoc />
    public override RateLimiter CreateLimiter(IRateLimitPolicy policy) =>
        new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = policy.RequestsPerWindow,
            QueueLimit = policy.QueueExceededRequests ? policy.QueueLimit : 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
}
