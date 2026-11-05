using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Who this platform says it is when it mints a token, and for how long one is good.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record JwtTokenIssuerConfiguration
{
    /// <summary>Gets the issuer this platform asserts.</summary>
    /// <remarks>
    /// Must match what a resource server is configured to expect, by ordinal comparison. A trailing
    /// slash present on one side and absent on the other is the usual cause of a token that verifies
    /// cryptographically and is then refused.
    /// </remarks>
    public required string Issuer { get; init; }

    /// <summary>Gets how long an access token remains valid.</summary>
    /// <remarks>
    /// Required: the issuer states it (the resolver reads it from the token manager's own configuration), there is no default (FDW-3823).
    /// A JWT cannot be recalled once minted, so the window during which a revoked
    /// principal still holds a working token is exactly this value — which is the argument for
    /// minutes rather than hours, with a refresh token carrying the longer session.
    /// </remarks>
    public required TimeSpan Lifetime { get; init; }
}
