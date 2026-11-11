using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using Fdw.Collections.Attributes;
using Fdw.Claims.Abstractions;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// One bucket for each authenticated caller: the principal's id, so a user and an agent key each have their own
/// allowance and one caller's traffic never refuses another's.
/// </summary>
[TypeOption(typeof(RateLimitPartitionings), "PerCaller")]
[ExcludeFromCodeCoverage]
public sealed class PerCallerRateLimitPartitioning : RateLimitPartitioningBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerCallerRateLimitPartitioning"/> class.
    /// </summary>
    public PerCallerRateLimitPartitioning() : base("PerCaller") { }

    /// <inheritdoc />
    public override string KeyOf(IRateLimitPolicy policy, ClaimsPrincipal? user, IPAddress? remoteAddress)
    {
        if (user is null || user.Identity is null || !user.Identity.IsAuthenticated)
        {
            throw new InvalidOperationException(
                $"The {policy.Name} rate limit policy gives each caller its own allowance, and this request has no authenticated caller.");
        }

        // The caller's id is its sub claim, which every principal FDW signs in carries (FDW-3580).
        var id = CallerClaims.UserId(user);
        if (id is null)
        {
            throw new InvalidOperationException(
                $"The {policy.Name} rate limit policy gives each caller its own allowance, and this request's caller carries no sub claim.");
        }

        return id;
    }
}
