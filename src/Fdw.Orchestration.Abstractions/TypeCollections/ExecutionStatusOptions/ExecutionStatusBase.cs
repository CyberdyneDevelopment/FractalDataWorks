using Fdw.Collections;

namespace Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;

/// <summary>
/// Base class for execution status TypeOptions.
/// </summary>
/// <remarks>
/// Provides common functionality for execution status values that track
/// the state of orchestration or step execution.
/// </remarks>
public abstract class ExecutionStatusBase : TypeOptionBase<int, ExecutionStatusBase>, IExecutionStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStatusBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="isTerminal">Whether this is a terminal status (execution complete).</param>
    /// <param name="isSuccess">Whether this status represents success.</param>
    /// <param name="isFailure">Whether this status represents failure.</param>
    /// <param name="allowsRetry">Whether execution can be retried from this status.</param>
    /// <param name="allowsResume">Whether execution can be resumed from this status.</param>
    /// <param name="isInProgress">Whether execution is currently in progress.</param>
    /// <param name="hasWarnings">Whether this status indicates warnings occurred.</param>
    protected ExecutionStatusBase(
        int id,
        string name,
        bool isTerminal,
        bool isSuccess,
        bool isFailure = false,
        bool allowsRetry = false,
        bool allowsResume = false,
        bool isInProgress = false,
        bool hasWarnings = false)
        : base(id, name)
    {
        IsTerminal = isTerminal;
        IsSuccess = isSuccess;
        IsFailure = isFailure;
        AllowsRetry = allowsRetry;
        AllowsResume = allowsResume;
        IsInProgress = isInProgress;
        HasWarnings = hasWarnings;
    }

    /// <inheritdoc/>
    public bool IsTerminal { get; }

    /// <inheritdoc/>
    public bool IsSuccess { get; }

    /// <inheritdoc/>
    public bool IsFailure { get; }

    /// <inheritdoc/>
    public bool AllowsRetry { get; }

    /// <inheritdoc/>
    public bool AllowsResume { get; }

    /// <inheritdoc/>
    public bool IsInProgress { get; }

    /// <inheritdoc/>
    public bool HasWarnings { get; }
}
