using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fdw.Claims.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// What a token from another issuer may put on the person who called with it (FDW-2363).
/// </summary>
/// <remarks>
/// A token from another issuer proves who called and nothing about what they may do here: only what this platform states or derives reaches the
/// principal. The platform's own claims (permissions, roles, tenant, organization, the acting role, the actor, an agent's identity, the session) are
/// its statements about a person or a session it created, so a foreign token carrying one of them is carrying a statement it has no standing to
/// make. They are removed. The issuer's own identifiers for the person (<c>sub</c>) and for the token (<c>jti</c>) stay, as do the claims that are not
/// the platform's (name, email, issuer, audience, expiry). The set is read from <see cref="ClaimDefinitions"/>, so a claim a downstream assembly adds is
/// covered without a list to update here.
/// </remarks>
public static class ForeignClaims
{
    /// <summary>Removes the platform's authority claims from the identity a foreign token produced.</summary>
    /// <param name="identity">The identity the validated foreign token produced.</param>
    /// <returns>The types of the claims that were removed, in the order they were found; none when the token carried none.</returns>
    public static IReadOnlyList<string> WithoutPlatformClaims(ClaimsIdentity identity)
    {
        if (identity is null)
        {
            throw new ArgumentNullException(nameof(identity));
        }

        var reserved = ClaimDefinitions.All()
            .Select(definition => definition.Name)
            .Where(name => !string.Equals(name, ClaimDefinitions.sub.Name, StringComparison.Ordinal)
                           && !string.Equals(name, ClaimDefinitions.jti.Name, StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        var carried = identity.Claims.Where(claim => reserved.Contains(claim.Type)).ToList();
        foreach (var claim in carried)
        {
            identity.RemoveClaim(claim);
        }

        return [.. carried.Select(claim => claim.Type)];
    }
}
