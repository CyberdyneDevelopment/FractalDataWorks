using System;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Request for state transition operations (cancel, pause, resume).
/// </summary>
public class ExecutionStateRequest
{
    /// <summary>
    /// Gets or sets the execution ID (from route).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets an optional message for the state transition.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the actor performing the action.
    /// </summary>
    public string? Actor { get; set; }
}
