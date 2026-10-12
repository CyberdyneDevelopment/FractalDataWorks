using System;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// Pipeline status update model.
/// </summary>
public sealed class PipelineStatusUpdate
{
    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public required string PipelineName { get; set; }

    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the status (Running, Succeeded, Failed, Cancelled).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets an optional message.
    /// </summary>
    public string? Message { get; set; }
}