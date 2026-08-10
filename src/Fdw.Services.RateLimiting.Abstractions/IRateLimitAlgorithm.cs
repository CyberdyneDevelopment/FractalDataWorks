using System.Threading.RateLimiting;
using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// A rate limiting algorithm: the member of <see cref="RateLimitAlgorithms"/> that knows how to
/// build the limiter it names.
/// </summary>
/// <remarks>
/// Why the contract carries the behaviour and not just identity: callers hold a policy whose
/// <c>Algorithm</c> is typed as this interface, so this is where the ability to build a limiter has
/// to be visible. Declaring it only on the base left every caller reading <c>Name</c> and switching
/// on it, which is how a four-case switch with no default came to decide whether a policy existed.
/// </remarks>
public interface IRateLimitAlgorithm : ITypeOption<int, RateLimitAlgorithmBase>
{
    /// <summary>
    /// Builds the limiter this algorithm describes, configured from <paramref name="policy"/>.
    /// </summary>
    /// <param name="policy">The policy supplying window, limits and queueing behaviour.</param>
    /// <returns>The configured limiter.</returns>
    RateLimiter CreateLimiter(IRateLimitPolicy policy);
}
