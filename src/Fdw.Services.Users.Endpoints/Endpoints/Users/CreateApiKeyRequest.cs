using System;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request to mint an API key for an account.
/// </summary>
public class CreateApiKeyRequest : UserScopedRequest
{
    /// <summary>Gets or sets the label shown in key listings.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind: <c>user</c> or <c>agent</c>. Required — the two authenticate as
    /// different principals, so neither is assumed.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets when the key stops working. Null is a key that does not expire.</summary>
    public DateTime? ExpiresAt { get; set; }
}
