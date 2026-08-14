using System;
using System.Collections.Generic;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// What happened when an identity was asked to prove itself.
/// </summary>
/// <remarks>
/// <b>The token itself is deliberately absent.</b> This endpoint answers "can this identity
/// authenticate", which an operator needs, without handing back a bearer credential that would let
/// whoever called it impersonate the service. Everything here — issuer, audience, granted scopes,
/// expiry — is what diagnoses a failure, and none of it is usable as a credential.
/// </remarks>
public sealed class VerifyIdentityResponse
{
    /// <summary>Gets or sets a value indicating whether a token was obtained.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Gets or sets the identity configuration that was verified.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the mechanism that was used.</summary>
    public string? Mechanism { get; set; }

    /// <summary>Gets or sets the issuer that answered, when one did.</summary>
    public string? Issuer { get; set; }

    /// <summary>Gets or sets the audience the token was issued for.</summary>
    public string? Audience { get; set; }

    /// <summary>Gets or sets the scopes actually granted, which may be narrower than requested.</summary>
    public IReadOnlyList<string> GrantedScopes { get; set; } = [];

    /// <summary>Gets or sets when the issued token stops being valid.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Gets or sets the reason acquisition failed, when it did.</summary>
    /// <remarks>
    /// This is the structured failure message from the domain — "no configuration named X", "provider
    /// rejected this service's credential", "could not reach provider" are distinct and stay distinct,
    /// because collapsing them is what makes an auth failure take a day to diagnose.
    /// </remarks>
    public string? Failure { get; set; }
}
