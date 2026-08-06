using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request to get a specific execution record.
/// </summary>
public class GetPipelineExecutionRequest
{
    /// <summary>
    /// Gets or sets the execution ID (bound from route).
    /// </summary>
    public Guid ExecutionId { get; set; }
}
