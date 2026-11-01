using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Record type for updating <c>etl.PipelineExecution</c> when a queued run moves to Running.
/// Contains only the columns the start of a run sets.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecutionStartRecord
{
    /// <summary>
    /// Gets or sets the execution status at start (Running).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the pipeline execution started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }
}
