using System;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Audit trail record.
/// </summary>
public sealed class AuditRecordPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the timestamp of the audit event.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Gets or sets the user who performed the action.</summary>
    public string UserName { get; set; } = string.Empty;
    /// <summary>Gets or sets the action performed.</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>Gets or sets the entity type affected.</summary>
    public string EntityType { get; set; } = string.Empty;
    /// <summary>Gets or sets the entity identifier.</summary>
    public string EntityId { get; set; } = string.Empty;
    /// <summary>Gets or sets additional details about the change.</summary>
    public string? Details { get; set; }
}
