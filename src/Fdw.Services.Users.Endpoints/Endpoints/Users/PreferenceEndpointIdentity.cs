using System;
using System.Security.Claims;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Resolves the current user's GUID from the FDW access token for the self-scoped
/// (<c>/users/me/preferences</c>) endpoints.
/// </summary>
internal static class PreferenceEndpointIdentity
{
    /// <summary>
    /// Attempts to read the durable user GUID from the access-token <c>sub</c> claim.
    /// </summary>
    /// <param name="user">The authenticated principal.</param>
    /// <param name="userId">The resolved user GUID when successful.</param>
    /// <returns><see langword="true"/> when a GUID was resolved; otherwise <see langword="false"/>.</returns>
    public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out userId);
    }
}
