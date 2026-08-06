using System;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// DTO representing a single connection health check record.
/// </summary>
public class ConnectionHealthCheckDto
{
    /// <summary>Gets or sets whether the connection was healthy.</summary>
    public bool IsHealthy { get; set; }

    /// <summary>Gets or sets the response time in milliseconds.</summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>Gets or sets the error message when the check failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets when the health check was performed.</summary>
    public DateTimeOffset CheckedAt { get; set; }

    /// <summary>Gets or sets who performed the health check.</summary>
    public string CheckedBy { get; set; } = string.Empty;
}
