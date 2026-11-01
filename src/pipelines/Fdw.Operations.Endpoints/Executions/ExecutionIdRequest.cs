using System;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Request for getting an execution by ID.
/// </summary>
public class ExecutionIdRequest
{
    /// <summary>
    /// Gets or sets the execution ID (from route).
    /// </summary>
    public Guid Id { get; set; }
}
