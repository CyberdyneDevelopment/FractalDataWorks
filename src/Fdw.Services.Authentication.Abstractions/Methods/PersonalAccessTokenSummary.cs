using System;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>Summary view of a Personal Access Token (safe for display — no raw token value).</summary>
public sealed class PersonalAccessTokenSummary
{
    /// <summary>Gets or sets the token ID (primary key).</summary>
    public Guid TokenId { get; set; }

    /// <summary>Gets or sets the user-facing display prefix (first 20 characters of the token).</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the user-assigned label for this token.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC expiration timestamp, or <c>null</c> for non-expiring tokens.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the last successful use, or <c>null</c> if never used.</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>Gets or sets whether the token has been revoked.</summary>
    public bool IsRevoked { get; set; }
}
