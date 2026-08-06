using System;

namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side summary of an agent key (safe for display — no raw key value).
/// </summary>
public sealed class AgentKeySummaryPayload
{
    /// <summary>
    /// Gets or sets the key ID.
    /// </summary>
    public Guid KeyId { get; set; }

    /// <summary>
    /// Gets or sets the display prefix.
    /// </summary>
    public string Prefix { get; set; } = "";

    /// <summary>
    /// Gets or sets the label (without the agent: prefix).
    /// </summary>
    public string Label { get; set; } = "";

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
