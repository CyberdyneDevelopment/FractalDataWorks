using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Represents the result of a pipeline execution.
/// </summary>
public interface IEtlPipelineExecutionResult
{
    /// <summary>
    /// Gets the unique execution ID.
    /// </summary>
    Guid ExecutionId { get; }

    /// <summary>
    /// Gets whether the execution was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the number of records extracted.
    /// </summary>
    int RecordsExtracted { get; }

    /// <summary>
    /// Gets the number of records transformed.
    /// </summary>
    int RecordsTransformed { get; }

    /// <summary>
    /// Gets the number of records loaded.
    /// </summary>
    int RecordsLoaded { get; }

    /// <summary>
    /// Gets the number of records that failed.
    /// </summary>
    int RecordsFailed { get; }

    /// <summary>
    /// Gets the duration of the extract phase.
    /// </summary>
    TimeSpan ExtractDuration { get; }

    /// <summary>
    /// Gets the duration of the transform phase.
    /// </summary>
    TimeSpan TransformDuration { get; }

    /// <summary>
    /// Gets the duration of the load phase.
    /// </summary>
    TimeSpan LoadDuration { get; }

    /// <summary>
    /// Gets the total execution duration.
    /// </summary>
    TimeSpan TotalDuration { get; }

    /// <summary>
    /// Gets the timestamp when execution started.
    /// </summary>
    DateTime StartedAt { get; }

    /// <summary>
    /// Gets the timestamp when execution completed.
    /// </summary>
    DateTime? CompletedAt { get; }

    /// <summary>
    /// Gets any error messages from failed records.
    /// </summary>
    IReadOnlyList<string> Errors { get; }
}
