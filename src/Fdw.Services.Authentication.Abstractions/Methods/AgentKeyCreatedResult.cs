using System;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>
/// Returned once when an agent key is first created.
/// Contains the raw key value — this is the only time it is exposed.
/// </summary>
public sealed class AgentKeyCreatedResult
{
    /// <summary>Gets or sets the agent key identifier.</summary>
    public Guid KeyId { get; set; }

    /// <summary>Gets or sets the full raw key value. Store this securely — it will not be retrievable again.</summary>
    public string RawKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the display prefix of the key.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable label for this key.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC expiration timestamp, or <c>null</c> for non-expiring keys.</summary>
    public DateTime? ExpiresAt { get; set; }
}
