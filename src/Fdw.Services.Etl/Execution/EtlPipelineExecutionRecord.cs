using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// POCO for the <c>etl.PipelineExecution</c> table.
/// Tracks pipeline execution metrics and status.
/// </summary>
/// <remarks>
/// <para>
/// Property names MUST match the SQL column names in <c>etl.PipelineExecution</c> exactly.
/// The <c>[GenerateMapper]</c> attribute generates a strongly-typed mapper for
/// <c>DbDataReader</c> → POCO mapping via DataGateway.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineExecutionRecord
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the pipeline that was executed.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the schedule that triggered this execution.
    /// Null for manual/ad-hoc executions.
    /// </summary>
    public string? ScheduleName { get; set; }

    /// <summary>
    /// Gets or sets the execution status (Running, Succeeded, Failed, Cancelled).
    /// </summary>
    public string Status { get; set; } = "Running";

    /// <summary>
    /// Gets or sets when the pipeline execution started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the pipeline execution completed. Null if still running.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of records extracted from the source.
    /// </summary>
    public int RecordsExtracted { get; set; }

    /// <summary>
    /// Gets or sets the number of records transformed.
    /// </summary>
    public int RecordsTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of records successfully loaded to the destination.
    /// </summary>
    public int RecordsLoaded { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed during processing.
    /// </summary>
    public int RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the pipeline execution in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the error message if the pipeline execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the user or service that executed the pipeline.
    /// </summary>
    public string? ExecutedBy { get; set; }
}
