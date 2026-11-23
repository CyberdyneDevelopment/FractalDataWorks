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
    /// Gets or sets when execution started. Null while the run is queued.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when execution completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the status the run is stored with: the queued state, Running, Succeeded or Failed.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Gets or sets whether execution was successful. False for every run that did not succeed, including one that has not finished;
    /// <see cref="Status"/> says which.
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
    /// Gets or sets the total duration in milliseconds, or <see langword="null"/> while the run has not reported one; never 0 for unknown.
    /// </summary>
    public double? TotalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the durable id of the person the run acted as (FDW-6519), or <see langword="null"/> for a run that acts as a tenant (a schedule) or one
    /// written before 1.6.0.
    /// </summary>
    public Guid? ActingPersonId { get; set; }

    /// <summary>
    /// Gets or sets the name of the run mode that ran it (FDW-6519), or <see langword="null"/> for a run written before 1.6.0.
    /// </summary>
    public string? RunMode { get; set; }

    /// <summary>
    /// Gets or sets the application name of the host process that ran it (FDW-6519), or <see langword="null"/> for a run written before 1.6.0.
    /// </summary>
    public string? ExecutingHost { get; set; }

    /// <summary>
    /// Gets or sets the slot's own name as the slot file states it, <c>a</c> or <c>b</c> (FDW-6519), or <see langword="null"/> when the host does not run on a slot.
    /// </summary>
    public string? ExecutingSlot { get; set; }
}
