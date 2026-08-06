using System;

namespace Fdw.Web.Endpoints.Contracts;

/// <summary>
/// Base response for a triggered operation execution.
/// Provides common fields returned from all trigger endpoints including
/// execution tracking identifiers, state, and dry-run information.
/// </summary>
public class TriggerOperationResponse
{
    /// <summary>
    /// Gets or sets the execution ID for tracking the triggered operation.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current execution state.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this was a dry run.
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// Gets or sets an optional message (e.g., dry-run result description).
    /// </summary>
    public string? Message { get; set; }
}
