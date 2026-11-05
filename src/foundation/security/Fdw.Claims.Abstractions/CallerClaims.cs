using System;
using System.Security.Claims;

namespace Fdw.Claims.Abstractions;

/// <summary>
/// Reads who the caller is from a principal FDW's authentication built.
/// </summary>
/// <remarks>
/// <para>
/// Why only <c>sub</c>: every principal FDW builds (a cookie session, a token FDW issued, a personal access token, an
/// agent key, a foreign bearer token) carries the caller's id as <see cref="ClaimDefinitions"/> <c>sub</c>.
/// ClaimTypes.NameIdentifier is at most an extra copy and never the only one, and no FDW handler writes a username
/// claim. Readers used to try NameIdentifier, then sub, then "name"; that read a display name as an id, and the ones
/// reading NameIdentifier alone refused personal access token and agent-key callers (FDW-3580).
/// </para>
/// <para>
/// A foreign issuer's <c>sub</c> is its own id for the person and need not be a GUID, so the id is read as text first.
/// What a missing id means is the caller's to decide (an unauthenticated refusal, a not-found), so nothing here
/// supplies one.
/// </para>
/// </remarks>
public static class CallerClaims
{
    /// <summary>
    /// Gets the caller's id: the <c>sub</c> claim's value.
    /// </summary>
    /// <param name="principal">The calling principal.</param>
    /// <returns>The id, or <see langword="null"/> when the principal carries no non-blank <c>sub</c>.</returns>
    public static string? UserId(ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            throw new ArgumentNullException(nameof(principal));
        }

        return principal.FindFirst(ClaimDefinitions.sub.Name)?.Value is { } value && value.Trim().Length > 0
            ? value
            : null;
    }

    /// <summary>
    /// Gets the caller's id as a GUID, the form a principal FDW issued carries.
    /// </summary>
    /// <param name="principal">The calling principal.</param>
    /// <param name="userId">The id when the result is <see langword="true"/>.</param>
    /// <returns>Whether the principal carries a <c>sub</c> that is a GUID.</returns>
    public static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        userId = Guid.Empty;
        return UserId(principal) is { } text && Guid.TryParse(text, out userId);
    }
}
