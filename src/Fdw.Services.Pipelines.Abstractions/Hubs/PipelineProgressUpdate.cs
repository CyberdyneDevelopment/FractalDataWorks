using System;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// Pipeline progress update model.
/// </summary>
public sealed class PipelineProgressUpdate
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
    /// Gets or sets records extracted so far.
    /// </summary>
    public int RecordsExtracted { get; set; }

    /// <summary>
    /// Gets or sets records transformed so far.
    /// </summary>
    public int RecordsTransformed { get; set; }

    /// <summary>
    /// Gets or sets records loaded so far.
    /// </summary>
    public int RecordsLoaded { get; set; }

    /// <summary>
    /// Gets or sets records failed so far.
    /// </summary>
    public int RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}