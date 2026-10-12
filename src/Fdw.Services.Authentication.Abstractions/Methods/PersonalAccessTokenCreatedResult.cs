using System;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>
/// Returned once when a Personal Access Token is first created.
/// Contains the raw token value — this is the only time it is exposed.
/// </summary>
public sealed class PersonalAccessTokenCreatedResult
{
    /// <summary>Gets or sets the token ID (primary key).</summary>
    public Guid TokenId { get; set; }

    /// <summary>Gets or sets the full raw token value. Store this securely — it will not be retrievable again.</summary>
    public string RawToken { get; set; } = string.Empty;

    /// <summary>Gets or sets the display prefix (first 20 characters of the token).</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the user-assigned label for this token.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC expiration timestamp, or <c>null</c> for non-expiring tokens.</summary>
    public DateTime? ExpiresAt { get; set; }
}
