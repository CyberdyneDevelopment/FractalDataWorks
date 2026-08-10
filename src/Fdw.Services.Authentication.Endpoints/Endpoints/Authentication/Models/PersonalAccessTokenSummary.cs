using System;

namespace Fdw.Services.Authentication.Endpoints.Models;

/// <summary>
/// Summary view of a personal access token (safe for display — no raw token value).
/// Field shapes match the client's <c>PersonalAccessTokenSummaryPayload</c>.
/// </summary>
public class PersonalAccessTokenSummary
{
    /// <summary>
    /// Gets or sets the token ID.
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// Gets or sets the display prefix.
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user-assigned label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiration timestamp.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the last used timestamp.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}
