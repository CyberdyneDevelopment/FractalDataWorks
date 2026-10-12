using System;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Response from triggering a pipeline job.
/// </summary>
public class TriggerPipelineResponse
{
    /// <summary>
    /// Gets or sets the execution identifier.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the initial execution status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
