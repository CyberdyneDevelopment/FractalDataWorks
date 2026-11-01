using System.Collections.Generic;

namespace Fdw.Services.Authentication.Abstractions.Tokens.Outbound;

/// <summary>
/// Carries the client-identity parameters for a non-interactive outbound
/// credential acquisition request.
/// </summary>
public sealed class OutboundCredentialRequest
{
    /// <summary>Gets or sets the OAuth 2.0 client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the client secret. Never logged.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Gets or sets the scopes to request on the access token.</summary>
    public IReadOnlyList<string> Scopes { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Gets or sets the token audience, or <c>null</c> to use the implementation default.
    /// </summary>
    public string? Audience { get; set; }
}
