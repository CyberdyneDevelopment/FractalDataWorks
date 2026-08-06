using System;

namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side response model for a newly created agent key.
/// The raw key value is only available at creation time.
/// </summary>
public sealed class CreateAgentKeyResponse
{
    /// <summary>
    /// Gets or sets the key ID.
    /// </summary>
    public Guid KeyId { get; set; }

    /// <summary>
    /// Gets or sets the raw key value (only returned once).
    /// </summary>
    public string RawKey { get; set; } = "";

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
