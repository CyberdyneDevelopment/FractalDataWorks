using System;
using System.Security.Claims;
using Fdw.Claims.Abstractions;

namespace Fdw.Services.RateLimiting;

/// <summary>
/// Finds the company a request belongs to: the tenant the caller is acting in (FDW-1946).
/// </summary>
/// <remarks>
/// It is the same claim every other tenant-aware read in the platform uses (<c>ClaimDefinitions.tenantId</c>). A caller with
/// no such claim belongs to no company, which is what an unidentified caller is, and is held to the server's allowance. A claim
/// that is there but is not a company id is a fault: the request is refused, not quietly held to the server's allowance as if
/// it had no company.
/// </remarks>
internal static class RateLimitCompany
{
    /// <summary>
    /// Finds the company a request belongs to.
    /// </summary>
    /// <param name="user">The principal the request was authenticated as.</param>
    /// <returns>The company, or null when the caller has none.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the caller has a company claim that is not a company id.</exception>
    internal static Guid? Of(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (string.IsNullOrEmpty(claim))
        {
            return null;
        }

        if (Guid.TryParse(claim, out var tenantId))
        {
            return tenantId;
        }

        throw new InvalidOperationException(
            $"The request's '{ClaimDefinitions.tenantId.Name}' claim is not a company id, so the company's rate limit allowance cannot be found.");
    }
}
