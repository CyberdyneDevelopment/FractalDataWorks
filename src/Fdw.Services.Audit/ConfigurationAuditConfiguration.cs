using System;
using Fdw.Data;

namespace Fdw.Services.Audit;

/// <summary>
/// Maps to <c>audit.ConfigurationAudit</c> — audit trail for configuration changes.
/// </summary>
[GenerateMapper]
public sealed partial class ConfigurationAuditConfiguration
{

    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type being audited.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity identifier.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the action performed (Create, Update, Delete).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON representation before the change.</summary>
    public string? BeforeJson { get; set; }

    /// <summary>Gets or sets the JSON representation after the change.</summary>
    public string? AfterJson { get; set; }

    /// <summary>Gets or sets the comma-separated list of changed field names.</summary>
    public string? ChangedFields { get; set; }

    /// <summary>Gets or sets the user who made the change.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the user.</summary>
    public string? UserName { get; set; }

    /// <summary>Gets or sets the IP address of the client.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the user agent of the client.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Gets or sets the timestamp of the change.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Gets or sets the correlation identifier linking related changes.</summary>
    public Guid? CorrelationId { get; set; }

    /// <summary>Gets or sets whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
