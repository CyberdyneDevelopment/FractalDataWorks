using System;
using System.Collections.Generic;

namespace Fdw.Services.Authentication.Abstractions.Tokens.Outbound;

/// <summary>
/// A non-interactive outbound credential (access token) acquired via
/// <see cref="IOutboundCredentialService.Acquire"/>.
/// </summary>
public sealed class OutboundCredential
{
    /// <summary>Gets or sets the bearer token value to include in outbound requests.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Gets or sets the token type, typically <c>"Bearer"</c>.</summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>Gets or sets the UTC instant at which this credential expires.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets the scopes granted on this credential.</summary>
    public IReadOnlyList<string> Scopes { get; set; } = System.Array.Empty<string>();
}
