using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Orchestration.Abstractions;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;

namespace Fdw.Orchestration.Execution;

/// <summary>
/// Default implementation of <see cref="IOrchestrationResult"/>.
/// </summary>
/// <remarks>
/// Captures the result of executing an orchestration, including all step results,
/// timing information, error details, and aggregated metrics.
/// </remarks>
public class OrchestrationResult : IOrchestrationResult
{
    private readonly List<IOrchestrationStepResult> _stepResults;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationResult"/> class.
    /// </summary>
    /// <param name="executionId">The unique execution identifier.</param>
    /// <param name="orchestrationId">The orchestration ID that was executed.</param>
    /// <param name="status">The execution status.</param>
    /// <param name="startTime">The execution start time.</param>
    public OrchestrationResult(
        string executionId,
        string orchestrationId,
        IExecutionStatus status,
        DateTimeOffset startTime)
    {
        ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
        OrchestrationId = orchestrationId ?? throw new ArgumentNullException(nameof(orchestrationId));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        StartTime = startTime;
        _stepResults = [];
    }

    /// <inheritdoc/>
    public string ExecutionId { get; }

    /// <inheritdoc/>
    public string OrchestrationId { get; }

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
    public IReadOnlyList<IOrchestrationStepResult> StepResults => _stepResults.AsReadOnly();

    /// <inheritdoc/>
    public object? Output { get; internal set; }

    /// <inheritdoc/>
    public IOrchestrationMetrics? Metrics { get; internal set; }

    /// <summary>
    /// Adds a step result to this orchestration result.
    /// </summary>
    /// <param name="stepResult">The step result to add.</param>
    internal void AddStepResult(IOrchestrationStepResult stepResult)
    {
        _stepResults.Add(stepResult);
    }

    /// <summary>
    /// Marks the execution as complete and computes final metrics.
    /// </summary>
    /// <param name="status">The final status.</param>
    /// <param name="errorMessage">Optional error message.</param>
    /// <param name="exception">Optional exception.</param>
    internal void Complete(IExecutionStatus status, string? errorMessage = null, Exception? exception = null)
    {
        Status = status;
        EndTime = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
        Exception = exception;
        Metrics = ComputeMetrics();
    }

    /// <summary>
    /// Computes metrics from the step results.
    /// </summary>
    /// <returns>Computed metrics.</returns>
    private OrchestrationMetrics ComputeMetrics()
    {
        var succeededStatus = ExecutionStatuses.ByName("Succeeded");
        var succeededWithWarningsStatus = ExecutionStatuses.ByName("SucceededWithWarnings");
        var failedStatus = ExecutionStatuses.ByName("Failed");
        var cancelledStatus = ExecutionStatuses.ByName("Cancelled");

        return new OrchestrationMetrics
        {
            TotalSteps = _stepResults.Count,
            SucceededSteps = _stepResults.Count(r =>
                r.Status.Id == succeededStatus.Id || r.Status.Id == succeededWithWarningsStatus.Id),
            FailedSteps = _stepResults.Count(r => r.Status.Id == failedStatus.Id),
            SkippedSteps = _stepResults.Count(r => r.Status.Id == cancelledStatus.Id),
            TotalRetryAttempts = _stepResults.Sum(r => r.RetryAttempts),
            TotalRecordsProcessed = _stepResults.Sum(r => r.RecordsProcessed),
            CacheHits = _stepResults.Count(r => r.WasCached),
            CacheMisses = _stepResults.Count(r => !r.WasCached)
        };
    }

    /// <summary>
    /// Creates a successful orchestration result.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="stepResults">The step results.</param>
    /// <param name="output">The final output.</param>
    /// <returns>A successful result.</returns>
    public static OrchestrationResult Success(
        string executionId,
        string orchestrationId,
        DateTimeOffset startTime,
        IEnumerable<IOrchestrationStepResult> stepResults,
        object? output = null)
    {
        var result = new OrchestrationResult(
            executionId,
            orchestrationId,
            ExecutionStatuses.ByName("Succeeded"),
            startTime);

        foreach (var stepResult in stepResults)
        {
            result.AddStepResult(stepResult);
        }

        result.Output = output;
        result.EndTime = DateTimeOffset.UtcNow;
        result.Metrics = result.ComputeMetrics();

        return result;
    }

    /// <summary>
    /// Creates a successful orchestration result with warnings.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="stepResults">The step results.</param>
    /// <param name="warningMessage">The warning message.</param>
    /// <param name="output">The final output.</param>
    /// <returns>A result with warnings.</returns>
    public static OrchestrationResult SuccessWithWarnings(
        string executionId,
        string orchestrationId,
        DateTimeOffset startTime,
        IEnumerable<IOrchestrationStepResult> stepResults,
        string warningMessage,
        object? output = null)
    {
        var result = new OrchestrationResult(
            executionId,
            orchestrationId,
            ExecutionStatuses.ByName("SucceededWithWarnings"),
            startTime);

        foreach (var stepResult in stepResults)
        {
            result.AddStepResult(stepResult);
        }

        result.Output = output;
        result.ErrorMessage = warningMessage;
        result.EndTime = DateTimeOffset.UtcNow;
        result.Metrics = result.ComputeMetrics();

        return result;
    }

    /// <summary>
    /// Creates a failed orchestration result.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="stepResults">The step results so far.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result.</returns>
    public static OrchestrationResult Failure(
        string executionId,
        string orchestrationId,
        DateTimeOffset startTime,
        IEnumerable<IOrchestrationStepResult> stepResults,
        string errorMessage,
        Exception? exception = null)
    {
        var result = new OrchestrationResult(
            executionId,
            orchestrationId,
            ExecutionStatuses.ByName("Failed"),
            startTime);

        foreach (var stepResult in stepResults)
        {
            result.AddStepResult(stepResult);
        }

        result.ErrorMessage = errorMessage;
        result.Exception = exception;
        result.EndTime = DateTimeOffset.UtcNow;
        result.Metrics = result.ComputeMetrics();

        return result;
    }

    /// <summary>
    /// Creates a cancelled orchestration result.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="stepResults">The step results so far.</param>
    /// <param name="reason">The cancellation reason.</param>
    /// <returns>A cancelled result.</returns>
    public static OrchestrationResult Cancelled(
        string executionId,
        string orchestrationId,
        DateTimeOffset startTime,
        IEnumerable<IOrchestrationStepResult> stepResults,
        string? reason = null)
    {
        var result = new OrchestrationResult(
            executionId,
            orchestrationId,
            ExecutionStatuses.ByName("Cancelled"),
            startTime);

        foreach (var stepResult in stepResults)
        {
            result.AddStepResult(stepResult);
        }

        result.ErrorMessage = reason ?? "Execution was cancelled";
        result.EndTime = DateTimeOffset.UtcNow;
        result.Metrics = result.ComputeMetrics();

        return result;
    }

    /// <summary>
    /// Creates a timed out orchestration result.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="stepResults">The step results so far.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>A timed out result.</returns>
    public static OrchestrationResult TimedOut(
        string executionId,
        string orchestrationId,
        DateTimeOffset startTime,
        IEnumerable<IOrchestrationStepResult> stepResults,
        TimeSpan timeout)
    {
        var result = new OrchestrationResult(
            executionId,
            orchestrationId,
            ExecutionStatuses.ByName("TimedOut"),
            startTime);

        foreach (var stepResult in stepResults)
        {
            result.AddStepResult(stepResult);
        }

        result.ErrorMessage = $"Orchestration timed out after {timeout.TotalSeconds:F1} seconds";
        result.EndTime = DateTimeOffset.UtcNow;
        result.Metrics = result.ComputeMetrics();

        return result;
    }
}

/// <summary>
/// Generic implementation of <see cref="IOrchestrationResult{TOutput}"/>.
/// </summary>
/// <typeparam name="TOutput">The output type.</typeparam>
public sealed class OrchestrationResult<TOutput> : OrchestrationResult, IOrchestrationResult<TOutput>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationResult{TOutput}"/> class.
    /// </summary>
    /// <param name="executionId">The unique execution identifier.</param>
    /// <param name="orchestrationId">The orchestration ID that was executed.</param>
    /// <param name="status">The execution status.</param>
    /// <param name="startTime">The execution start time.</param>
    public OrchestrationResult(
        string executionId,
        string orchestrationId,
        IExecutionStatus status,
        DateTimeOffset startTime)
        : base(executionId, orchestrationId, status, startTime)
    {
    }

    /// <inheritdoc/>
    public new TOutput? Output
    {
        get => (TOutput?)base.Output;
        internal set => base.Output = value;
    }

    /// <summary>
    /// Creates a successful orchestration result with typed output.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="stepResults">The step results.</param>
    /// <param name="output">The typed final output.</param>
    /// <returns>A successful result.</returns>
    public static OrchestrationResult<TOutput> Success(
        string executionId,
        string orchestrationId,
        DateTimeOffset startTime,
        IEnumerable<IOrchestrationStepResult> stepResults,
        TOutput? output)
    {
        var result = new OrchestrationResult<TOutput>(
            executionId,
            orchestrationId,
            ExecutionStatuses.ByName("Succeeded"),
            startTime);

        foreach (var stepResult in stepResults)
        {
            result.AddStepResult(stepResult);
        }

        result.Output = output;
        result.EndTime = DateTimeOffset.UtcNow;

        return result;
    }

    /// <summary>
    /// Creates a failed orchestration result.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="stepResults">The step results so far.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result.</returns>
    public new static OrchestrationResult<TOutput> Failure(
        string executionId,
        string orchestrationId,
        DateTimeOffset startTime,
        IEnumerable<IOrchestrationStepResult> stepResults,
        string errorMessage,
        Exception? exception = null)
    {
        var result = new OrchestrationResult<TOutput>(
            executionId,
            orchestrationId,
            ExecutionStatuses.ByName("Failed"),
            startTime);

        foreach (var stepResult in stepResults)
        {
            result.AddStepResult(stepResult);
        }

        result.ErrorMessage = errorMessage;
        result.Exception = exception;
        result.EndTime = DateTimeOffset.UtcNow;

        return result;
    }
}
