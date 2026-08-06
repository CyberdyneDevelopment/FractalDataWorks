using System;

namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side response model for a newly created personal access token.
/// The raw token value is only available at creation time.
/// </summary>
public sealed class CreateTokenResponse
{
    /// <summary>
    /// Gets or sets the token ID.
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// Gets or sets the raw token value (only returned once).
    /// </summary>
    public string RawToken { get; set; } = "";

    /// <summary>
    /// Gets or sets the display prefix.
    /// </summary>
    public string Prefix { get; set; } = "";

    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
