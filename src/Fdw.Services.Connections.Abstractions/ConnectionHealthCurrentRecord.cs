using System;
using Fdw.Data;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// One row of conn.ConnectionHealthCurrent — the latest health check result for one connection,
/// or an unprobed row (Status="Unknown", IsHealthy=null) when no check has ever run for it.
/// </summary>
[GenerateMapper]
public sealed class ConnectionHealthCurrentRecord
{
    /// <summary>Gets or sets the logical Id of the connection.</summary>
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
