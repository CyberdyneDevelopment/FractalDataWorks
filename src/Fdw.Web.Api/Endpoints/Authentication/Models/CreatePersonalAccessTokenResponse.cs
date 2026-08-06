using System;

namespace Fdw.Services.Authentication.Endpoints.Models;

/// <summary>
/// Response model for a newly created personal access token.
/// </summary>
public class CreatePersonalAccessTokenResponse
{
    /// <summary>
    /// Gets or sets the token ID.
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// Gets or sets the raw token value (only returned once).
    /// </summary>
    public string RawToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display prefix.
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
