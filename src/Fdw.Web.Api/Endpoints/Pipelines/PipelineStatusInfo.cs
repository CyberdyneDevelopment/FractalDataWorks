using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Pipeline status information.
/// </summary>
public class PipelineStatusInfo
{
    /// <summary>
    /// Gets or sets the pipeline ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the pipeline type.
    /// </summary>
    public required string PipelineType { get; set; }

    /// <summary>
    /// Gets or sets whether the pipeline is currently executing.
    /// </summary>
    public bool IsExecuting { get; set; }
}
