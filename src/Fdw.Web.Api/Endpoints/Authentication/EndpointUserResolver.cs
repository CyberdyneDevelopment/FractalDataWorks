using System;
using System.Security.Claims;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Resolves the authenticated subject's user identifier from a claims principal.
/// </summary>
internal static class EndpointUserResolver
{
    /// <summary>
    /// Attempts to resolve the subject GUID from the standard identifier claim.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <param name="userId">The resolved user identifier, when successful.</param>
    /// <returns><c>true</c> if a GUID subject was resolved; otherwise <c>false</c>.</returns>
    // Why: OpenIddict bakes the user's GUID Id into the JWT 'sub' claim and emits no name claim,
    // so User.Identity.Name is null. ClaimTypes.NameIdentifier maps from 'sub'; the raw 'sub' is
    // read as an alternate claim name (not a fallback value) when inbound claim mapping is disabled.
    public static bool TryResolveUserId(ClaimsPrincipal user, out Guid userId)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(subject, out userId);
    }
}
