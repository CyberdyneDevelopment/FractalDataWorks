using System.Threading.RateLimiting;
using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Base class for rate limiting algorithms.
/// </summary>
/// <remarks>
/// Why <see cref="CreateLimiter"/> is declared here: the caller holds the algorithm as a resolved
/// TypeOption, so it can ask the option to build its limiter rather than reading
/// <see cref="Fdw.Collections.TypeOptionBase{TKey,TSelf}.Name"/> and switching on the string. The
/// switch that did this had four cases and no default, so an algorithm it did not recognise
/// registered no limiter at all and the policy silently ceased to exist. A fifth algorithm is now
/// a new option and nothing else.
///
/// It returns a <see cref="RateLimiter"/> from System.Threading.RateLimiting rather than taking
/// ASP.NET's RateLimiterOptions, which keeps this package on netstandard2.0 — the host-side
/// registrar wraps what this returns into a named allowance.
/// </remarks>
public abstract class RateLimitAlgorithmBase : TypeOptionBase<RateLimitAlgorithmBase>, IRateLimitAlgorithm
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitAlgorithmBase"/> class.
    /// </summary>
    protected RateLimitAlgorithmBase(string name) : base(name) { }

    /// <summary>
    /// Builds the limiter this algorithm describes, configured from <paramref name="allowance"/>.
    /// </summary>
    /// <param name="allowance">The numbers supplying window, limits and queueing behaviour.</param>
    /// <returns>The configured limiter.</returns>
    public abstract RateLimiter CreateLimiter(RateLimitAllowance allowance);
}
