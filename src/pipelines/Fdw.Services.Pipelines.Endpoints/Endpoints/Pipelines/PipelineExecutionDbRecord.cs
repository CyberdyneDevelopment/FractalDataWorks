using System;
using Fdw.Data;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Internal record type for pipeline execution database table (etl.PipelineExecution).
/// </summary>
[GenerateMapper]
public partial class PipelineExecutionDbRecord
{
    /// <summary>Gets or sets the unique execution identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>Gets or sets the schedule name that triggered execution.</summary>
    public string? ScheduleName { get; set; }

    /// <summary>Gets or sets the execution status, as the writer stated it (the queued state, Running, Succeeded, Failed).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets when execution started. Null while the run is queued.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>Gets or sets when execution completed.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

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

    /// <summary>Gets or sets the person the run acted as (FDW-6519), or null for a run that acts as a tenant or a row written before 1.6.0.</summary>
    public Guid? ActingPersonId { get; set; }

    /// <summary>Gets or sets the name of the run mode that ran it (FDW-6519), or null on a row written before 1.6.0.</summary>
    public string? RunMode { get; set; }

    /// <summary>Gets or sets the application name of the host process that ran it (FDW-6519), or null on a row written before 1.6.0.</summary>
    public string? ExecutingHost { get; set; }

    /// <summary>Gets or sets the slot's own name as the slot file states it (FDW-6519), or null when the host does not run on a slot.</summary>
    public string? ExecutingSlot { get; set; }
}
