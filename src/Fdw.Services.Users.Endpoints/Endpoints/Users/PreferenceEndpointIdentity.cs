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

    /// <summary>
    /// Gets the name a person's preferences row is written under: the person's id. The row's own Id is minted when it is first
    /// saved, so the row is found by this name and never by the person's id as an Id.
    /// </summary>
    /// <param name="userId">The person's id.</param>
    /// <returns>The row's name.</returns>
    public static string RowName(Guid userId) => userId.ToString();
}
