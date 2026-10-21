using System.Net;
using System.Security.Claims;
using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Base class for the ways a rate limit policy divides requests into buckets.
/// </summary>
public abstract class RateLimitPartitioningBase : TypeOptionBase<RateLimitPartitioningBase>, IRateLimitPartitioning
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitPartitioningBase"/> class.
    /// </summary>
    /// <param name="name">The partitioning's name.</param>
    protected RateLimitPartitioningBase(string name) : base(name) { }

    /// <inheritdoc />
    public abstract string KeyOf(IRateLimitPolicy policy, ClaimsPrincipal? user, IPAddress? remoteAddress);
}
