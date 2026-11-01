using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// One bucket for everyone under the policy: a cap on the whole host rather than on any caller.
/// </summary>
[TypeOption(typeof(RateLimitPartitionings), "Shared")]
[ExcludeFromCodeCoverage]
public sealed class SharedRateLimitPartitioning : RateLimitPartitioningBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SharedRateLimitPartitioning"/> class.
    /// </summary>
    public SharedRateLimitPartitioning() : base("Shared") { }

    /// <inheritdoc />
    public override string KeyOf(IRateLimitPolicy policy, ClaimsPrincipal? user, IPAddress? remoteAddress)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        return policy.Name;
    }
}
