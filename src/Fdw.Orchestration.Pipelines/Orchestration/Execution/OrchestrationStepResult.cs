using System;
using Fdw.Orchestration.Abstractions;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;

namespace Fdw.Orchestration.Execution;

/// <summary>
/// Default implementation of <see cref="IOrchestrationStepResult"/>.
/// </summary>
/// <remarks>
/// Captures the result of executing a single orchestration step, including
/// timing information, error details, and output data.
/// </remarks>
public class OrchestrationStepResult : IOrchestrationStepResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationStepResult"/> class.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="status">The execution status.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="endTime">The end time.</param>
    public OrchestrationStepResult(
        string stepId,
        string stepName,
        IExecutionStatus status,
        DateTimeOffset startTime,
        DateTimeOffset? endTime = null)
    {
        StepId = stepId ?? throw new ArgumentNullException(nameof(stepId));
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <inheritdoc/>
    public string StepId { get; }

    /// <inheritdoc/>
    public string StepName { get; }

    /// <inheritdoc/>
    public IExecutionStatus Status { get; internal set; }

    /// <inheritdoc/>
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc/>
    public DateTimeOffset? EndTime { get; internal set; }

    /// <inheritdoc/>
    public TimeSpan Duration => EndTime.HasValue
        ? EndTime.Value - StartTime
        : DateTimeOffset.UtcNow - StartTime;

    /// <inheritdoc/>
    public string? ErrorMessage { get; internal set; }

    /// <inheritdoc/>
    public Exception? Exception { get; internal set; }

    /// <inheritdoc/>
    public int RetryAttempts { get; internal set; }

    /// <inheritdoc/>
    public object? Output { get; internal set; }

    /// <inheritdoc/>
    public long RecordsProcessed { get; internal set; }

    /// <inheritdoc/>
    public bool WasCached { get; internal set; }

    /// <summary>
    /// Creates a successful step result.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="output">The step output.</param>
    /// <param name="recordsProcessed">Number of records processed.</param>
    /// <param name="wasCached">Whether the result was from cache.</param>
    /// <param name="retryAttempts">Number of retry attempts made.</param>
    /// <returns>A successful step result.</returns>
    public static OrchestrationStepResult Success(
        string stepId,
        string stepName,
        DateTimeOffset startTime,
        object? output = null,
        long recordsProcessed = 0,
        bool wasCached = false,
        int retryAttempts = 0)
    {
        return new OrchestrationStepResult(
            stepId,
            stepName,
            ExecutionStatuses.ByName("Succeeded"),
            startTime,
            DateTimeOffset.UtcNow)
        {
            Output = output,
            RecordsProcessed = recordsProcessed,
            WasCached = wasCached,
            RetryAttempts = retryAttempts
        };
    }

    /// <summary>
    /// Creates a successful step result with warnings.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="warningMessage">The warning message.</param>
    /// <param name="output">The step output.</param>
    /// <param name="recordsProcessed">Number of records processed.</param>
    /// <param name="retryAttempts">Number of retry attempts made.</param>
    /// <returns>A step result with warnings.</returns>
    public static OrchestrationStepResult SuccessWithWarnings(
        string stepId,
        string stepName,
        DateTimeOffset startTime,
        string warningMessage,
        object? output = null,
        long recordsProcessed = 0,
        int retryAttempts = 0)
    {
        return new OrchestrationStepResult(
            stepId,
            stepName,
            ExecutionStatuses.ByName("SucceededWithWarnings"),
            startTime,
            DateTimeOffset.UtcNow)
        {
            ErrorMessage = warningMessage,
            Output = output,
            RecordsProcessed = recordsProcessed,
            RetryAttempts = retryAttempts
        };
    }

    /// <summary>
    /// Creates a failed step result.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="retryAttempts">Number of retry attempts made.</param>
    /// <returns>A failed step result.</returns>
    public static OrchestrationStepResult Failure(
        string stepId,
        string stepName,
        DateTimeOffset startTime,
        string errorMessage,
        Exception? exception = null,
        int retryAttempts = 0)
    {
        return new OrchestrationStepResult(
            stepId,
            stepName,
            ExecutionStatuses.ByName("Failed"),
            startTime,
            DateTimeOffset.UtcNow)
        {
            ErrorMessage = errorMessage,
            Exception = exception,
            RetryAttempts = retryAttempts
        };
    }

    /// <summary>
    /// Creates a skipped step result.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="reason">The reason for skipping.</param>
    /// <returns>A skipped step result.</returns>
    public static OrchestrationStepResult Skipped(
        string stepId,
        string stepName,
        string? reason = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new OrchestrationStepResult(
            stepId,
            stepName,
            ExecutionStatuses.ByName("Cancelled"),
            now,
            now)
        {
            ErrorMessage = reason ?? "Step was skipped"
        };
    }

    /// <summary>
    /// Creates a timed out step result.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="retryAttempts">Number of retry attempts made.</param>
    /// <returns>A timed out step result.</returns>
    public static OrchestrationStepResult TimedOut(
        string stepId,
        string stepName,
        DateTimeOffset startTime,
        TimeSpan timeout,
        int retryAttempts = 0)
    {
        return new OrchestrationStepResult(
            stepId,
            stepName,
            ExecutionStatuses.ByName("TimedOut"),
            startTime,
            DateTimeOffset.UtcNow)
        {
            ErrorMessage = $"Step timed out after {timeout.TotalSeconds:F1} seconds",
            RetryAttempts = retryAttempts
        };
    }
}

/// <summary>
/// Generic implementation of <see cref="IOrchestrationStepResult{TOutput}"/>.
/// </summary>
/// <typeparam name="TOutput">The output type.</typeparam>
public sealed class OrchestrationStepResult<TOutput> : OrchestrationStepResult, IOrchestrationStepResult<TOutput>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationStepResult{TOutput}"/> class.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="status">The execution status.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="endTime">The end time.</param>
    public OrchestrationStepResult(
        string stepId,
        string stepName,
        IExecutionStatus status,
        DateTimeOffset startTime,
        DateTimeOffset? endTime = null)
        : base(stepId, stepName, status, startTime, endTime)
    {
    }

    /// <inheritdoc/>
    public new TOutput? Output
    {
        get => (TOutput?)base.Output;
        internal set => base.Output = value;
    }

    /// <summary>
    /// Creates a successful step result with typed output.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="output">The typed step output.</param>
    /// <param name="recordsProcessed">Number of records processed.</param>
    /// <param name="wasCached">Whether the result was from cache.</param>
    /// <param name="retryAttempts">Number of retry attempts made.</param>
    /// <returns>A successful step result.</returns>
    public static OrchestrationStepResult<TOutput> Success(
        string stepId,
        string stepName,
        DateTimeOffset startTime,
        TOutput? output,
        long recordsProcessed = 0,
        bool wasCached = false,
        int retryAttempts = 0)
    {
        return new OrchestrationStepResult<TOutput>(
            stepId,
            stepName,
            ExecutionStatuses.ByName("Succeeded"),
            startTime,
            DateTimeOffset.UtcNow)
        {
            Output = output,
            RecordsProcessed = recordsProcessed,
            WasCached = wasCached,
            RetryAttempts = retryAttempts
        };
    }

    /// <summary>
    /// Creates a failed step result.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="retryAttempts">Number of retry attempts made.</param>
    /// <returns>A failed step result.</returns>
    public new static OrchestrationStepResult<TOutput> Failure(
        string stepId,
        string stepName,
        DateTimeOffset startTime,
        string errorMessage,
        Exception? exception = null,
        int retryAttempts = 0)
    {
        return new OrchestrationStepResult<TOutput>(
            stepId,
            stepName,
            ExecutionStatuses.ByName("Failed"),
            startTime,
            DateTimeOffset.UtcNow)
        {
            ErrorMessage = errorMessage,
            Exception = exception,
            RetryAttempts = retryAttempts
        };
    }
}
