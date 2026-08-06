using System.Diagnostics.CodeAnalysis;
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
}
