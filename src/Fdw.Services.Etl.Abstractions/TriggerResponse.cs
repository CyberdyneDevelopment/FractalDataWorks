using System;

namespace Fdw.Services.Etl.Projects.Clients;

/// <summary>
/// Response body from the unified trigger endpoint.
/// Returns the execution identifier and initial state.
/// </summary>
public sealed class TriggerResponse
{
    /// <summary>
    /// Gets or sets the execution item identifier assigned to this execution.
    /// Use with <c>GET /etl/executions/{executionId}</c> or the status reader.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the initial state of the execution.
    /// Will be "Triggered" for normal dispatch or "AwaitingApproval" when RequireApprovalToRun is effective.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
