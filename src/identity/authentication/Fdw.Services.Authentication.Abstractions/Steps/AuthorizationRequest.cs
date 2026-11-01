using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// What this platform must remember across a redirect it started.
/// </summary>
/// <remarks>
/// Never sent to the caller. The verifier proves the exchange comes from whoever made the request,
/// and the nonce proves the token answers that request — both are worthless if the party being
/// checked is holding them.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record AuthorizationRequest
{
    /// <summary>Gets the PKCE code verifier.</summary>
    public required string CodeVerifier { get; init; }

    /// <summary>Gets the nonce the returned token must echo.</summary>
    public required string Nonce { get; init; }

    /// <summary>Gets the provider this request was made to.</summary>
    /// <remarks>
    /// Checked when the caller returns, so a code obtained from one configured provider cannot be
    /// exchanged at another's endpoint — the mix-up attack RFC 9207 addresses.
    /// </remarks>
    public required string Issuer { get; init; }
}
