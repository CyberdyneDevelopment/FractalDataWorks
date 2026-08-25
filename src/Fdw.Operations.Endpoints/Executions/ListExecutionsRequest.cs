using System;
using Fdw.Web.Endpoints;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Request for listing executions with pagination and filters.
/// </summary>
public class ListExecutionsRequest : PaginatedRequest
{
    /// <summary>
    /// Gets or sets an optional correlation ID filter.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets an optional item type filter (Workflow, Job, Stage, Step, Task).
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    /// Gets or sets an optional state filter.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets an optional root execution ID filter.
    /// </summary>
    public Guid? RootId { get; set; }
}
