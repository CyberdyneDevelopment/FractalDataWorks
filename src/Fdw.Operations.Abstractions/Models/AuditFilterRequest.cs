using System;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Filter criteria for audit record queries.
/// </summary>
public sealed class AuditFilterRequest
{
    /// <summary>Gets or sets the optional entity type filter.</summary>
    public string? EntityType { get; set; }
    /// <summary>Gets or sets the optional user name filter.</summary>
    public string? UserName { get; set; }
    /// <summary>Gets or sets the optional action filter.</summary>
    public string? Action { get; set; }
    /// <summary>Gets or sets the start date filter.</summary>
    public DateTime? From { get; set; }
    /// <summary>Gets or sets the end date filter.</summary>
    public DateTime? To { get; set; }
    /// <summary>Gets or sets the number of records to skip.</summary>
    public int Skip { get; set; }
    /// <summary>Gets or sets the number of records to take.</summary>
    public int Take { get; set; } = 50;
}
