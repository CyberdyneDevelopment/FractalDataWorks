using System.Net;
using System.Security.Claims;
using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Interface defining how a rate limit policy divides requests into buckets. Requests that have the same key share one
/// allowance; requests with different keys each have their own.
/// </summary>
public interface IRateLimitPartitioning : ITypeOption<int, RateLimitPartitioningBase>
{
    /// <summary>
    /// Gets the key of the bucket a request belongs to under a policy.
    /// </summary>
    /// <param name="policy">The policy the request is counted under.</param>
    /// <param name="user">The principal the request was authenticated as, as the pipeline has it when the limiter runs.</param>
    /// <param name="remoteAddress">The address the request came from.</param>
    /// <returns>The bucket's key.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the request has no identity this partitioning needs. Such a request is never given a shared bucket: it
    /// reached a policy that needs the identity without having one, which is a fault to find rather than a case to absorb.
    /// </exception>
    string KeyOf(IRateLimitPolicy policy, ClaimsPrincipal? user, IPAddress? remoteAddress);
}
