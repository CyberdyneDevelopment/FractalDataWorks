using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// One bucket for each remote address: for a policy whose callers have no identity yet.
/// </summary>
[TypeOption(typeof(RateLimitPartitionings), "PerAddress")]
[ExcludeFromCodeCoverage]
public sealed class PerAddressRateLimitPartitioning : RateLimitPartitioningBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerAddressRateLimitPartitioning"/> class.
    /// </summary>
    public PerAddressRateLimitPartitioning() : base("PerAddress") { }

    /// <inheritdoc />
    public override string KeyOf(IRateLimitPolicy policy, ClaimsPrincipal? user, IPAddress? remoteAddress)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (remoteAddress is null)
        {
            throw new InvalidOperationException(
                $"The {policy.Name} rate limit policy gives each address its own allowance, and this request has no remote address.");
        }

        return remoteAddress.ToString();
    }
}
