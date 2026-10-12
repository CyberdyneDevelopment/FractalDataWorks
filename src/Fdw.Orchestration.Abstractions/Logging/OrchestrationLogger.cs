using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Orchestration.Abstractions.Logging;

/// <summary>
/// Static logger class for orchestration execution, step execution, and state management operations.
/// </summary>
/// <remarks>
/// Event ID ranges:
/// - 5000-5019: Orchestration execution events
/// - 5020-5039: Step execution events
/// - 5040-5059: Caching events
/// - 5060-5079: Resilience events
/// - 5080-5099: State management events
/// </remarks>
[MessageLoggingTypeCode("ORCH")]
public static partial class OrchestrationLogger
{
    // Orchestration Execution Events: 5000-5019

    /// <summary>
    /// Logs when orchestration execution starts.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="orchestrationId">The orchestration identifier.</param>
    /// <param name="orchestrationName">The orchestration name.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Orchestration started: ExecutionId={executionId}, OrchestrationId={orchestrationId}, Name={orchestrationName}")]
    public static partial IGenericMessage OrchestrationStarted(
        ILogger logger,
        string executionId,
        string orchestrationId,
        string orchestrationName);

    /// <summary>
    /// Logs when orchestration execution completes successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="status">The completion status.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="stepsCompleted">The number of steps completed.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Orchestration completed: ExecutionId={executionId}, Status={status}, Duration={duration}, StepsCompleted={stepsCompleted}")]
    public static partial IGenericMessage OrchestrationCompleted(
        ILogger logger,
        string executionId,
        string status,
        TimeSpan duration,
        int stepsCompleted);

    /// <summary>
    /// Logs when orchestration execution fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="orchestrationId">The orchestration identifier.</param>
    /// <param name="stepId">The step identifier where failure occurred (if any).</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Orchestration failed: ExecutionId={executionId}, OrchestrationId={orchestrationId}, StepId={stepId}, Error={errorMessage}")]
    public static partial IGenericMessage OrchestrationFailed(
        ILogger logger,
        Exception? exception,
        string executionId,
        string orchestrationId,
        string? stepId,
        string errorMessage);

    /// <summary>
    /// Logs when orchestration execution is cancelled.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="orchestrationId">The orchestration identifier.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Warning,
        Message = "Orchestration cancelled: ExecutionId={executionId}, OrchestrationId={orchestrationId}")]
    public static partial IGenericMessage OrchestrationCancelled(
        ILogger logger,
        string executionId,
        string orchestrationId);

    /// <summary>
    /// Logs when orchestration execution is paused.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="orchestrationId">The orchestration identifier.</param>
    /// <param name="currentStepId">The current step identifier.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Orchestration paused: ExecutionId={executionId}, OrchestrationId={orchestrationId}, CurrentStep={currentStepId}")]
    public static partial IGenericMessage OrchestrationPaused(
        ILogger logger,
        string executionId,
        string orchestrationId,
        string? currentStepId);

    /// <summary>
    /// Logs when orchestration execution is resumed.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="orchestrationId">The orchestration identifier.</param>
    /// <param name="resumeStepId">The step identifier to resume from.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Orchestration resumed: ExecutionId={executionId}, OrchestrationId={orchestrationId}, ResumeStep={resumeStepId}")]
    public static partial IGenericMessage OrchestrationResumed(
        ILogger logger,
        string executionId,
        string orchestrationId,
        string? resumeStepId);

    /// <summary>
    /// Logs orchestration execution progress.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="currentStepId">The current step identifier.</param>
    /// <param name="stepsCompleted">The number of steps completed.</param>
    /// <param name="totalSteps">The total number of steps.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Orchestration progress: ExecutionId={executionId}, CurrentStep={currentStepId}, Progress={stepsCompleted}/{totalSteps}")]
    public static partial IGenericMessage OrchestrationProgress(
        ILogger logger,
        string executionId,
        string currentStepId,
        int stepsCompleted,
        int totalSteps);

    // Step Execution Events: 5020-5039

    /// <summary>
    /// Logs when step execution starts.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="attemptNumber">The attempt number.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Step started: ExecutionId={executionId}, StepId={stepId}, StepName={stepName}, Attempt={attemptNumber}")]
    public static partial IGenericMessage StepStarted(
        ILogger logger,
        string executionId,
        string stepId,
        string stepName,
        int attemptNumber);

    /// <summary>
    /// Logs when step execution completes successfully.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="duration">The execution duration.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Step completed: ExecutionId={executionId}, StepId={stepId}, Duration={duration}")]
    public static partial IGenericMessage StepCompleted(
        ILogger logger,
        string executionId,
        string stepId,
        TimeSpan duration);

    /// <summary>
    /// Logs when step execution fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="attemptNumber">The attempt number.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Step failed: ExecutionId={executionId}, StepId={stepId}, Attempt={attemptNumber}, Error={errorMessage}")]
    public static partial IGenericMessage StepFailed(
        ILogger logger,
        Exception? exception,
        string executionId,
        string stepId,
        int attemptNumber,
        string errorMessage);

    /// <summary>
    /// Logs when a step is skipped.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="reason">The reason the step was skipped.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Warning,
        Message = "Step skipped: ExecutionId={executionId}, StepId={stepId}, Reason={reason}")]
    public static partial IGenericMessage StepSkipped(
        ILogger logger,
        string executionId,
        string stepId,
        string reason);

    /// <summary>
    /// Logs when a step is retrying.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="attemptNumber">The next attempt number.</param>
    /// <param name="delay">The delay before retry.</param>
    /// <param name="reason">The reason for retry.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 81000,
        Level = LogLevel.Warning,
        Message = "Step retrying: ExecutionId={executionId}, StepId={stepId}, NextAttempt={attemptNumber}, Delay={delay}, Reason={reason}")]
    public static partial IGenericMessage StepRetrying(
        ILogger logger,
        string executionId,
        string stepId,
        int attemptNumber,
        TimeSpan delay,
        string reason);

    // Caching Events: 5040-5059

    /// <summary>
    /// Logs when a cache hit occurs.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="cacheType">The type of cache (definition, step result, state).</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Debug,
        Message = "Cache hit: Key={cacheKey}, CacheType={cacheType}")]
    public static partial IGenericMessage CacheHit(
        ILogger logger,
        string cacheKey,
        string cacheType);

    /// <summary>
    /// Logs when a cache miss occurs.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="cacheType">The type of cache.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Cache miss: Key={cacheKey}, CacheType={cacheType}")]
    public static partial IGenericMessage CacheMiss(
        ILogger logger,
        string cacheKey,
        string cacheType);

    /// <summary>
    /// Logs when a cache entry is set.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="cacheType">The type of cache.</param>
    /// <param name="expiration">The expiration time.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Cache set: Key={cacheKey}, CacheType={cacheType}, Expiration={expiration}")]
    public static partial IGenericMessage CacheSet(
        ILogger logger,
        string cacheKey,
        string cacheType,
        TimeSpan? expiration);

    /// <summary>
    /// Logs when a cache entry is evicted.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="cacheType">The type of cache.</param>
    /// <param name="reason">The eviction reason.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Debug,
        Message = "Cache evicted: Key={cacheKey}, CacheType={cacheType}, Reason={reason}")]
    public static partial IGenericMessage CacheEvicted(
        ILogger logger,
        string cacheKey,
        string cacheType,
        string reason);

    // Resilience Events: 5060-5079

    /// <summary>
    /// Logs when a resilience pipeline is activated.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="pipelineType">The type of resilience pipeline.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Debug,
        Message = "Resilience pipeline activated: ExecutionId={executionId}, StepId={stepId}, PipelineType={pipelineType}")]
    public static partial IGenericMessage ResiliencePipelineActivated(
        ILogger logger,
        string executionId,
        string stepId,
        string pipelineType);

    /// <summary>
    /// Logs when a circuit breaker opens.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="duration">The duration the circuit will remain open.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 81001,
        Level = LogLevel.Warning,
        Message = "Circuit breaker opened: ExecutionId={executionId}, StepId={stepId}, Duration={duration}")]
    public static partial IGenericMessage CircuitBreakerOpened(
        ILogger logger,
        string executionId,
        string stepId,
        TimeSpan duration);

    /// <summary>
    /// Logs when a circuit breaker closes.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Information,
        Message = "Circuit breaker closed: ExecutionId={executionId}, StepId={stepId}")]
    public static partial IGenericMessage CircuitBreakerClosed(
        ILogger logger,
        string executionId,
        string stepId);

    /// <summary>
    /// Logs when no step executor is found for a step.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="stepId">The step identifier.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "No step executor found: ExecutionId={executionId}, StepId={stepId}, returning null output")]
    public static partial IGenericMessage NoStepExecutorFound(
        ILogger logger,
        string executionId,
        string stepId);

    /// <summary>
    /// Logs when cancellation is requested for an execution.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="orchestrationId">The orchestration identifier.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Cancellation requested: ExecutionId={executionId}, OrchestrationId={orchestrationId}")]
    public static partial IGenericMessage CancellationRequested(
        ILogger logger,
        string executionId,
        string orchestrationId);

    /// <summary>
    /// Logs when an execution is not found.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "Execution {executionId} not found")]
    public static partial IGenericMessage ExecutionNotFound(
        ILogger logger,
        string executionId);

    // State Management Events: 5080-5099

    /// <summary>
    /// Logs when execution state is checkpointed.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="checkpointId">The checkpoint identifier.</param>
    /// <param name="stepId">The step identifier at checkpoint.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Debug,
        Message = "State checkpointed: ExecutionId={executionId}, CheckpointId={checkpointId}, StepId={stepId}")]
    public static partial IGenericMessage StateCheckpointed(
        ILogger logger,
        string executionId,
        string checkpointId,
        string stepId);

    /// <summary>
    /// Logs when execution state is restored.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="checkpointId">The checkpoint identifier.</param>
    /// <param name="resumeStepId">The step to resume from.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Information,
        Message = "State restored: ExecutionId={executionId}, CheckpointId={checkpointId}, ResumeStep={resumeStepId}")]
    public static partial IGenericMessage StateRestored(
        ILogger logger,
        string executionId,
        string checkpointId,
        string resumeStepId);

    /// <summary>
    /// Logs when state restoration fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="executionId">The execution identifier.</param>
    /// <param name="checkpointId">The checkpoint identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Error,
        Message = "State restoration failed: ExecutionId={executionId}, CheckpointId={checkpointId}, Error={errorMessage}")]
    public static partial IGenericMessage StateRestorationFailed(
        ILogger logger,
        Exception? exception,
        string executionId,
        string checkpointId,
        string errorMessage);
}
