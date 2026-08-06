using System;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Execution context passed into each strategy's Execute call.
/// Provides correlation data for logging, lineage, and retry bookkeeping.
/// </summary>
public interface IResiliencyExecutionContext
{
    /// <summary>
    /// Gets the top-level project execution identifier.
    /// Used for correlation across the execution tree.
    /// </summary>
    Guid ExecutionId { get; }

    /// <summary>
    /// Gets the stage execution item identifier.
    /// The retry boundary is the stage — all retries re-run this stage.
    /// </summary>
    Guid StageId { get; }

    /// <summary>
    /// Gets the source data set identifier, if applicable.
    /// Used by PrimaryBackup strategy to identify which data set to substitute.
    /// </summary>
    Guid? SourceDataSetId { get; }

    /// <summary>
    /// Gets the zero-based attempt number for the current invocation.
    /// 0 = first attempt, 1 = first retry, etc.
    /// </summary>
    int AttemptNumber { get; }
}
