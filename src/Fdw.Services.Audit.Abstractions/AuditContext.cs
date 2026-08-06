using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Audit.Abstractions;

/// <summary>
/// Caller context for audit operations, extracted at the transport layer.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AuditContext
{
    /// <summary>
    /// Gets or sets the authenticated user identifier.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the caller.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string of the caller.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier for the request.
    /// </summary>
    public Guid? CorrelationId { get; set; }
}
