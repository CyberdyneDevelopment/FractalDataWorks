using System;
using System.Linq;
using System.Security.Claims;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Names the authentication schemes that validate opaque credentials (agent keys and personal access tokens) and tells
/// whether a principal was authenticated through one.
/// </summary>
/// <remarks>
/// Why the check reads the scheme and nothing else: the ApiKey handler names its identity after the scheme that
/// authenticated it, so the scheme is the one fact that holds for both credential kinds. The <c>agent</c> claim marks
/// agent keys only, and a role lookup would read the owner's roles, which an interactive sign-in of the same person holds
/// too. Defined here, beside the principal-to-context mapping every consumer already references, so a layer that does not
/// reference the authentication implementation can still ask.
/// </remarks>
public static class ApiKeyAuthenticationSchemes
{
    /// <summary>The prefix every ApiKey scheme name carries; the declared service's name follows it.</summary>
    public const string Prefix = "Fdw.ApiKey.";

    /// <summary>Tells whether an ApiKey scheme authenticated the principal.</summary>
    /// <param name="principal">The request's principal.</param>
    /// <returns><see langword="true"/> when an authenticated identity of the principal was issued by an ApiKey scheme.</returns>
    public static bool Authenticated(ClaimsPrincipal principal)
    {
        // Explicit, not ArgumentNullException.ThrowIfNull: this project targets netstandard2.0, which does not have it.
        if (principal is null)
            throw new ArgumentNullException(nameof(principal));

        return principal.Identities.Any(identity =>
            identity.IsAuthenticated
            && identity.AuthenticationType is { } scheme
            && scheme.StartsWith(Prefix, StringComparison.Ordinal));
    }
}
