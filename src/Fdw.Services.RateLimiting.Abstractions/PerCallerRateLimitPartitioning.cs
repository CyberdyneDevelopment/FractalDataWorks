using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using Fdw.Collections.Attributes;

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
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (user is null || user.Identity is null || !user.Identity.IsAuthenticated)
        {
            throw new InvalidOperationException(
                $"The {policy.Name} rate limit policy gives each caller its own allowance, and this request has no authenticated caller.");
        }

        // The caller's id is one claim under two spellings, as the session endpoints read it: the handler that signed the caller
        // in names it NameIdentifier, or sub when the token's claims were not mapped. This is not a choice between callers.
        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        if (id is null || id.Trim().Length == 0)
        {
            throw new InvalidOperationException(
                $"The {policy.Name} rate limit policy gives each caller its own allowance, and this request's caller carries no {nameof(ClaimTypes.NameIdentifier)} or sub claim.");
        }

        return id;
    }
}
