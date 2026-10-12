using System.Collections.Generic;

namespace Fdw.Web.Endpoints.Contracts;

/// <summary>
/// Base request for triggering an operation execution.
/// Provides common properties for all trigger endpoints including name resolution,
/// parameter passing, correlation tracking, and dry-run support.
/// </summary>
public class TriggerOperationRequest
{
    /// <summary>
    /// Gets or sets the name of the resource to trigger (from route or body).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional parameters for the operation.
    /// </summary>
    public IDictionary<string, object?>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets an optional correlation ID for distributed tracing.
    /// If not provided, a new one will be generated.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the trigger source identifier (e.g., "API:Manual", "Scheduler:Cron").
    /// </summary>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets whether this is a dry run (validation only, no execution).
    /// </summary>
    public bool DryRun { get; set; }
}
