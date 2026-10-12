using System;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>Summary view of an agent key (safe for display — no raw key value).</summary>
public sealed class AgentKeySummary
{
    /// <summary>Gets or sets the agent key identifier.</summary>
    public Guid KeyId { get; set; }

    /// <summary>Gets or sets the user-facing display prefix of the key.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable label for this key.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC expiration timestamp, or <c>null</c> for non-expiring keys.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the last successful use, or <c>null</c> if never used.</summary>
    public DateTime? LastUsedAt { get; set; }
}
