using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Record of a pipeline execution.
/// </summary>
public class PipelineExecutionRecord
{
    /// <summary>
    /// Gets or sets the unique execution ID.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public required string PipelineName { get; set; }

    /// <summary>
    /// Gets or sets when execution started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when execution completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets whether execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of records extracted.
    /// </summary>
    public int RecordsExtracted { get; set; }

    /// <summary>
    /// Gets or sets the number of records transformed.
    /// </summary>
    public int RecordsTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of records loaded.
    /// </summary>
    public int RecordsLoaded { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed.
    /// </summary>
    public int RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the total duration in milliseconds.
    /// </summary>
    public double TotalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets who executed the pipeline.
    /// </summary>
    public string? ExecutedBy { get; set; }
}
