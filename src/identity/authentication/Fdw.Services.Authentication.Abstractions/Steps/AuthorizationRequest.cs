using System;
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

    /// <summary>Gets the callback address the authorization request sent as its redirect_uri (FDW-5730).</summary>
    /// <remarks>
    /// The code exchange sends this one, never one worked out again: RFC 6749 section 4.1.3 requires the
    /// exchange's redirect_uri to be identical to the authorization request's, and an address taken from the
    /// request's host can only be known from the request that started the sign-in.
    /// </remarks>
    public required Uri RedirectUri { get; init; }
}
