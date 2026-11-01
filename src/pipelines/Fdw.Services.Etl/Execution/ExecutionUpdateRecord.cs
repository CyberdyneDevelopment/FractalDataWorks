using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Record type for updating <c>etl.PipelineExecution</c> after pipeline completion.
/// Contains only the columns that can be updated post-creation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecutionUpdateRecord
{
    /// <summary>
    /// Gets or sets the final execution status (Succeeded, Failed, Cancelled).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the pipeline execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the total execution duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the number of records extracted from the source.
    /// </summary>
    public long RecordsExtracted { get; set; }

    /// <summary>
    /// Gets or sets the number of records loaded to the destination.
    /// </summary>
    public long RecordsLoaded { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed during processing.
    /// </summary>
    public long RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the number of records skipped during processing.
    /// </summary>
    public long RecordsSkipped { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
