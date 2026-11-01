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
/// A token from another issuer proves who called. Whether it may also state what they may do here is the deployer's choice, made per issuer on its
/// entry (<see cref="AcceptedClaims"/>): a platform claim the entry names is kept, every other platform claim is removed. The issuer's own identifiers
/// for the person (<c>sub</c>) and for the token (<c>jti</c>) stay, as do the claims that are not the platform's (name, email, issuer, audience,
/// expiry). The set is read from <see cref="ClaimDefinitions"/>, so a claim a downstream assembly adds is covered without a list to update here. A
/// claim the definition marks session-bound can never be named by an entry, so it is always removed (FDW-2933).
/// </remarks>
public static class ForeignClaims
{
    /// <summary>Removes the platform's claims from the identity a foreign token produced, except those the issuer's entry accepts.</summary>
    /// <param name="identity">The identity the validated foreign token produced.</param>
    /// <param name="accepted">The platform claim types the issuer's entry accepts, as <see cref="AcceptedClaims"/> read them.</param>
    /// <returns>The types of the claims that were removed, in the order they were found; none when the token carried none.</returns>
    public static IReadOnlyList<string> WithoutPlatformClaims(ClaimsIdentity identity, IReadOnlyCollection<string> accepted)
    {
        if (identity is null)
        {
            throw new ArgumentNullException(nameof(identity));
        }

        if (accepted is null)
        {
            throw new ArgumentNullException(nameof(accepted));
        }

        var reserved = ClaimDefinitions.All()
            .Select(definition => definition.Name)
            .Where(name => !string.Equals(name, ClaimDefinitions.sub.Name, StringComparison.Ordinal)
                           && !string.Equals(name, ClaimDefinitions.jti.Name, StringComparison.Ordinal)
                           && !accepted.Contains(name, StringComparer.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        var carried = identity.Claims.Where(claim => reserved.Contains(claim.Type)).ToList();
        foreach (var claim in carried)
        {
            identity.RemoveClaim(claim);
        }

        return [.. carried.Select(claim => claim.Type)];
    }
}
