using System;
using System.Collections.Generic;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// A token an identity provider issued to this process, with everything a caller needs to decide
/// whether it is still usable.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ExpiresAt"/> is absolute rather than a relative lifetime: a relative value is only
/// meaningful at the instant it was received, and this object outlives that instant in the cache.
/// </para>
/// <para>
/// <see cref="Value"/> is a bearer credential. It is never written to disk and never logged — the
/// logging in this domain reports audience, scopes, issuer and expiry, which is what an operator
/// needs to diagnose an authorization failure, and none of which lets them impersonate the service.
/// </para>
/// </remarks>
public sealed class IssuedIdentityToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssuedIdentityToken"/> class.
    /// </summary>
    /// <param name="value">The raw token as issued.</param>
    /// <param name="tokenType">The token type, as returned by the provider (e.g. <c>Bearer</c>).</param>
    /// <param name="issuer">The identity provider that issued this token.</param>
    /// <param name="audience">The audience this token was issued for.</param>
    /// <param name="expiresAt">The absolute instant at which this token stops being valid.</param>
    /// <param name="scopes">The scopes actually granted, which may be narrower than those requested.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/>, <paramref name="tokenType"/>, <paramref name="issuer"/>, or <paramref name="audience"/> is null, empty, or whitespace.</exception>
    public IssuedIdentityToken(
        string value,
        string tokenType,
        string issuer,
        string audience,
        DateTimeOffset expiresAt,
        IReadOnlyList<string>? scopes = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("An issued token must carry a value.", nameof(value));
        if (string.IsNullOrWhiteSpace(tokenType))
            throw new ArgumentException("An issued token must state its type.", nameof(tokenType));
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("An issued token must name its issuer.", nameof(issuer));
        if (string.IsNullOrWhiteSpace(audience))
            throw new ArgumentException("An issued token must name the audience it is for.", nameof(audience));

        Value = value;
        TokenType = tokenType;
        Issuer = issuer;
        Audience = audience;
        ExpiresAt = expiresAt;
        Scopes = scopes ?? [];
    }

    /// <summary>Gets the raw token as issued. A bearer credential — never log or persist this.</summary>
    public string Value { get; }

    /// <summary>Gets the token type as returned by the provider (e.g. <c>Bearer</c>).</summary>
    public string TokenType { get; }

    /// <summary>Gets the identity provider that issued this token.</summary>
    public string Issuer { get; }

    /// <summary>Gets the audience this token was issued for.</summary>
    public string Audience { get; }

    /// <summary>Gets the absolute instant at which this token stops being valid.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Gets the scopes actually granted, which may be narrower than those requested.</summary>
    public IReadOnlyList<string> Scopes { get; }

    /// <summary>
    /// Indicates whether this token is still usable at <paramref name="asOf"/>, allowing
    /// <paramref name="refreshSkew"/> of headroom so it does not expire mid-flight.
    /// </summary>
    /// <param name="asOf">The instant to evaluate against.</param>
    /// <param name="refreshSkew">How long before actual expiry the token should stop being treated as usable.</param>
    /// <returns><see langword="true"/> when the token can still be used.</returns>
    public bool IsUsableAt(DateTimeOffset asOf, TimeSpan refreshSkew) => asOf + refreshSkew < ExpiresAt;

    /// <summary>Gets the value to place in an HTTP <c>Authorization</c> header.</summary>
    public string AuthorizationHeaderValue => $"{TokenType} {Value}";
}
