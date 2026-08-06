using System;
using FastEndpoints;

namespace Fdw.Operations.Endpoints.Audit;

/// <summary>
/// Request for listing audit records with optional filters.
/// </summary>
public class ListAuditRecordsRequest
{
    /// <summary>Optional entity type filter.</summary>
    [QueryParam]
    public string? EntityType { get; set; }

    /// <summary>Optional entity ID filter.</summary>
    [QueryParam]
    public string? EntityId { get; set; }

    /// <summary>Optional action filter (Create, Update, Delete).</summary>
    [QueryParam]
    public string? Action { get; set; }

    /// <summary>Optional user ID filter.</summary>
    [QueryParam]
    public string? UserId { get; set; }

    /// <summary>Optional start date filter (inclusive).</summary>
    [QueryParam]
    public DateTimeOffset? From { get; set; }

    /// <summary>Optional end date filter (inclusive).</summary>
    [QueryParam]
    public DateTimeOffset? To { get; set; }

    /// <summary>Maximum number of records to return.</summary>
    [QueryParam]
    public int Limit { get; set; } = 100;
}
