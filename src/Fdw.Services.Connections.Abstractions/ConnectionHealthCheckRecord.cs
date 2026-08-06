using System;
using Fdw.Data;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Represents a single health check result persisted in ops.ConnectionHealthCheck.
/// </summary>
// Why: [GenerateMapper] is required (not just an optimization) — the MsSql connection's reader
// mapping throws InvalidOperationException for any complex query-result type that has no generated
// PocoMapper registered (see MsSqlConnection.MapReaderResult). This type is the query-result shape
// for IConnectionHealthService.GetHistory.
[GenerateMapper]
public sealed class ConnectionHealthCheckRecord
{
    /// <summary>Gets or sets the logical Id of the connection that was checked.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>Gets or sets the display name of the connection at the time of the check.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the connection was healthy at the time of the check.</summary>
    public bool IsHealthy { get; set; }

    /// <summary>Gets or sets the response time in milliseconds, if measured.</summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>Gets or sets the error message when the check failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets when the health check was performed.</summary>
    public DateTimeOffset CheckedAt { get; set; }

    /// <summary>Gets or sets the identity of the user or system that performed the check.</summary>
    public string CheckedBy { get; set; } = string.Empty;
}
