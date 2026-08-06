using System;

namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Connection data transfer object for UI display.
/// </summary>
public sealed class ConnectionPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the connection name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the connection type.</summary>
    public string ConnectionType { get; set; } = string.Empty;
    // Why: tri-state, not a collapsed bool — mirrors the server ConnectionSummaryDto.LastTestSuccess.
    // Null means never tested (renders as "Unknown"), distinct from a known test failure (false).
    /// <summary>Gets or sets whether the last connection test succeeded, false if it failed, or null if never tested.</summary>
    public bool? LastTestSuccess { get; set; }
    /// <summary>Gets or sets the last tested time.</summary>
    public DateTimeOffset? LastTestedAt { get; set; }
    /// <summary>Gets or sets the last time the connection's schema was discovered.</summary>
    public DateTimeOffset? LastDiscoveredAt { get; set; }
    /// <summary>Gets or sets when the connection was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
