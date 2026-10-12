using System;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// Pipeline execution complete model.
/// </summary>
public sealed class PipelineExecutionComplete
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
    /// Gets or sets whether execution succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the final status.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets records extracted.
    /// </summary>
    public int RecordsExtracted { get; set; }

    /// <summary>
    /// Gets or sets records transformed.
    /// </summary>
    public int RecordsTransformed { get; set; }

    /// <summary>
    /// Gets or sets records loaded.
    /// </summary>
    public int RecordsLoaded { get; set; }

    /// <summary>
    /// Gets or sets records failed.
    /// </summary>
    public int RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the total duration in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the completion timestamp.
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets an optional error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
}