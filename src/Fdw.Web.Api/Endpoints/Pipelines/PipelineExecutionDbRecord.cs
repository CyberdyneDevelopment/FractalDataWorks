using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Internal record type for pipeline execution database table (etl.PipelineExecution).
/// </summary>
public class PipelineExecutionDbRecord
{
    /// <summary>Gets or sets the unique execution identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>Gets or sets the schedule name that triggered execution.</summary>
    public string? ScheduleName { get; set; }

    /// <summary>Gets or sets the execution status (Running, Succeeded, Failed).</summary>
    public string Status { get; set; } = "Running";

    /// <summary>Gets or sets when execution started.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Gets or sets when execution completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets the number of records extracted.</summary>
    public int RecordsExtracted { get; set; }

    /// <summary>Gets or sets the number of records transformed.</summary>
    public int RecordsTransformed { get; set; }

    /// <summary>Gets or sets the number of records loaded.</summary>
    public int RecordsLoaded { get; set; }

    /// <summary>Gets or sets the number of records that failed.</summary>
    public int RecordsFailed { get; set; }

    /// <summary>Gets or sets the total duration in milliseconds.</summary>
    public long? DurationMs { get; set; }

    /// <summary>Gets or sets the error message if execution failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets who executed the pipeline.</summary>
    public string? ExecutedBy { get; set; }
}
