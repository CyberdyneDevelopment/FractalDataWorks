using System;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// DTO representing one connection's latest health check result.
/// </summary>
public class ConnectionHealthSummaryDto
{
    /// <summary>Gets or sets the connection's logical Id.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>Gets or sets the connection's display name.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the latest check's status ("Healthy", "Unhealthy", or "Unknown" when never probed).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the latest check succeeded, or null when never probed.</summary>
    public bool? IsHealthy { get; set; }

    /// <summary>Gets or sets the latest check's response time in milliseconds, if measured.</summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>Gets or sets the latest check's error message, when it failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets when the latest check ran, or null when never probed.</summary>
    public DateTimeOffset? LastCheckedAt { get; set; }
}
