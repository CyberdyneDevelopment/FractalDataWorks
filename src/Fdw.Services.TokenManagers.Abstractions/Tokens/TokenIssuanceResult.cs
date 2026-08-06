using System;
using System.Collections.Generic;

namespace Fdw.Services.TokenManagers.Abstractions.Tokens;

/// <summary>
/// The result of a successful token issuance or refresh operation.
/// Access and refresh tokens are returned as opaque strings; callers
/// must not attempt to parse them — use the token manager's <c>Validate</c>/
/// <c>ExtractClaims</c> to inspect claims.
/// </summary>
public sealed class TokenIssuanceResult
{
    /// <summary>Gets or sets the issued access token (bearer token value).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token, or <c>null</c> if this grant type does not
    /// issue refresh tokens (e.g., client-credentials).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>Gets or sets the access token type, typically <c>"Bearer"</c>.</summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>Gets or sets the UTC instant at which the access token expires.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the scopes granted by the authorization server (may be a subset of requested).
    /// </summary>
    public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the roles embedded in the token at issuance time.
    /// Implementations that authenticate via agent key MUST include the <c>agent</c>
    /// role in this list for agent principals.
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
