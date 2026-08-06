using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Resiliency.Logging;


/// <summary>
/// MessageLogging methods for resiliency operations.
/// Every log message is returned in the result AND logged.
/// </summary>
[MessageLoggingTypeCode("RESILIENCY")]
public static partial class ResiliencyLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Retry Events (6101-6110)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a retry attempt is being made.
    /// </summary>
    [MessageLogging(
        EventId = 81000,
        Level = LogLevel.Warning,
        Message = "Retry attempt {attempt}/{maxRetries} for operation '{operation}'")]
    public static partial IGenericMessage RetryAttempt(
        ILogger logger,
        int attempt,
        int maxRetries,
        string operation);

    /// <summary>
    /// Logs when all retry attempts have been exhausted.
    /// </summary>
    [MessageLogging(
        EventId = 81001,
        Level = LogLevel.Error,
        Message = "All retry attempts exhausted for '{operation}': {error}")]
    public static partial IGenericMessage RetriesExhausted(
        ILogger logger,
        string operation,
        string error);

    /// <summary>
    /// Logs when a retry delay is being applied.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Applying retry delay of {delayMs}ms for operation '{operation}'")]
    public static partial IGenericMessage RetryDelayApplied(
        ILogger logger,
        double delayMs,
        string operation);

    /// <summary>
    /// Logs when an operation succeeds after retries.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Operation '{operation}' succeeded after {attempts} attempt(s)")]
    public static partial IGenericMessage OperationSucceededAfterRetry(
        ILogger logger,
        string operation,
        int attempts);

    // ═══════════════════════════════════════════════════════════════════════════
    // Circuit Breaker Events (6111-6120)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a circuit breaker opens (starts rejecting requests).
    /// </summary>
    // Why Warning, not Error (FDW-583): a circuit opening is the resiliency mechanism doing its job —
    // abnormal but handled, matching OrchestrationLogger's existing Warning for the same condition.
    [MessageLogging(
        EventId = 81002,
        Level = LogLevel.Warning,
        Message = "Circuit breaker opened for '{operation}', duration: {durationSeconds}s")]
    public static partial IGenericMessage CircuitBreakerOpened(
        ILogger logger,
        string operation,
        int durationSeconds);

    /// <summary>
    /// Logs when a circuit breaker closes (resumes normal operation).
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Circuit breaker closed for '{operation}', resuming normal operation")]
    public static partial IGenericMessage CircuitBreakerClosed(
        ILogger logger,
        string operation);

    /// <summary>
    /// Logs when a circuit breaker transitions to half-open state.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Circuit breaker half-open for '{operation}', testing recovery")]
    public static partial IGenericMessage CircuitBreakerHalfOpen(
        ILogger logger,
        string operation);

    /// <summary>
    /// Logs when a request is rejected due to an open circuit breaker.
    /// </summary>
    [MessageLogging(
        EventId = 81003,
        Level = LogLevel.Warning,
        Message = "Request rejected by circuit breaker for '{operation}'")]
    public static partial IGenericMessage CircuitBreakerRejected(
        ILogger logger,
        string operation);

    // ═══════════════════════════════════════════════════════════════════════════
    // Pipeline Events (6121-6130)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when creating a resiliency pipeline.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Creating resiliency pipeline for policy '{policyName}'")]
    public static partial IGenericMessage CreatingPipeline(
        ILogger logger,
        string policyName);

    /// <summary>
    /// Logs when a resiliency policy is registered.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Resiliency policy '{policyName}' registered with {maxRetries} retries")]
    public static partial IGenericMessage PolicyRegistered(
        ILogger logger,
        string policyName,
        int maxRetries);

    /// <summary>
    /// Logs when a pipeline is retrieved from cache.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Retrieved cached pipeline for policy '{policyName}'")]
    public static partial IGenericMessage PipelineRetrievedFromCache(
        ILogger logger,
        string policyName);

    /// <summary>
    /// Logs when a pipeline is created and cached.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Pipeline created and cached for policy '{policyName}'")]
    public static partial IGenericMessage PipelineCreatedAndCached(
        ILogger logger,
        string policyName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Timeout Events (6131-6140)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when an operation times out.
    /// </summary>
    [MessageLogging(
        EventId = 81004,
        Level = LogLevel.Error,
        Message = "Operation '{operation}' timed out after {timeoutSeconds}s")]
    public static partial IGenericMessage OperationTimedOut(
        ILogger logger,
        string operation,
        int timeoutSeconds);

    /// <summary>
    /// Logs when a timeout is configured for an operation.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Debug,
        Message = "Timeout of {timeoutSeconds}s configured for operation '{operation}'")]
    public static partial IGenericMessage TimeoutConfigured(
        ILogger logger,
        int timeoutSeconds,
        string operation);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error Events (6141-6150)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when an exception occurs during pipeline execution.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Exception during pipeline execution for '{operation}'")]
    public static partial IGenericMessage PipelineExecutionException(
        ILogger logger,
        Exception exception,
        string operation);

    /// <summary>
    /// Logs when a policy is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "Resiliency policy '{policyName}' not found")]
    public static partial IGenericMessage PolicyNotFound(
        ILogger logger,
        string policyName);

    /// <summary>
    /// Logs when pipeline creation fails.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to create pipeline for policy '{policyName}': {error}")]
    public static partial IGenericMessage PipelineCreationFailed(
        ILogger logger,
        string policyName,
        string error);

    /// <summary>
    /// Logs when an invalid policy configuration is detected.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "Invalid resiliency policy configuration for '{policyName}': {error}")]
    public static partial IGenericMessage InvalidPolicyConfiguration(
        ILogger logger,
        string policyName,
        string error);

    // ═══════════════════════════════════════════════════════════════════════════
    // Diagnostic Events (6151-6160)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs policy configuration details for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Policy '{policyName}' configuration - MaxRetries: {maxRetries}, InitialDelay: {initialDelayMs}ms, MaxDelay: {maxDelayMs}ms, CircuitBreakerThreshold: {cbThreshold}")]
    public static partial IGenericMessage PolicyConfiguration(
        ILogger logger,
        string policyName,
        int maxRetries,
        double initialDelayMs,
        double maxDelayMs,
        int cbThreshold);

    /// <summary>
    /// Logs when the factory is initialized.
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Information,
        Message = "ResiliencyPipelineFactory initialized with {policyCount} available policies")]
    public static partial IGenericMessage FactoryInitialized(
        ILogger logger,
        int policyCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // ResiliencyExecutor / Strategy Dispatch (6153-6165)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the executor starts for a stage execution.
    /// </summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Trace,
        Message = "ResiliencyExecutor started: executionId={executionId}, stageId={stageId}, policyId={policyId}")]
    public static partial IGenericMessage ResiliencyExecutorStarted(
        ILogger logger,
        Guid executionId,
        Guid stageId,
        Guid? policyId);

    /// <summary>
    /// Logs when a resiliency policy is successfully resolved for a stage.
    /// </summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Resiliency policy resolved: executionId={executionId}, policyId={policyId}, strategyType={strategyType}")]
    public static partial IGenericMessage PolicyResolved(
        ILogger logger,
        Guid executionId,
        Guid policyId,
        string strategyType);

    /// <summary>
    /// Logs when a resiliency policy is not found (by id or name).
    /// </summary>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Error,
        Message = "Resiliency policy not found: executionId={executionId}, policyRef='{policyRef}'")]
    public static partial IGenericMessage PolicyNotFound(
        ILogger logger,
        Guid executionId,
        string policyRef);

    /// <summary>
    /// Logs when a strategy is dispatched for execution.
    /// </summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Trace,
        Message = "Resiliency strategy dispatched: executionId={executionId}, strategyType={strategyType}")]
    public static partial IGenericMessage StrategyDispatched(
        ILogger logger,
        Guid executionId,
        string strategyType);

    /// <summary>
    /// Logs when an attempt succeeds.
    /// </summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Resiliency attempt succeeded: executionId={executionId}, attempt={attemptNumber}")]
    public static partial IGenericMessage AttemptSucceeded(
        ILogger logger,
        Guid executionId,
        int attemptNumber);

    /// <summary>
    /// Logs when an attempt fails.
    /// </summary>
    [MessageLogging(
        EventId = 81005,
        Level = LogLevel.Warning,
        Message = "Resiliency attempt failed: executionId={executionId}, attempt={attemptNumber}, reason='{reason}'")]
    public static partial IGenericMessage AttemptFailed(
        ILogger logger,
        Guid executionId,
        int attemptNumber,
        string reason);

    /// <summary>
    /// Logs when all retry attempts are exhausted (terminal failure).
    /// </summary>
    [MessageLogging(
        EventId = 81006,
        Level = LogLevel.Error,
        Message = "Max retries exceeded: executionId={executionId}, maxRetries={maxRetries}")]
    public static partial IGenericMessage MaxRetriesExceeded(
        ILogger logger,
        Guid executionId,
        int maxRetries);

    /// <summary>
    /// Logs when PrimaryBackup strategy activates the backup source.
    /// </summary>
    [MessageLogging(
        EventId = 81007,
        Level = LogLevel.Warning,
        Message = "Backup source activated: executionId={executionId}, backupDataSetId={backupDataSetId}")]
    public static partial IGenericMessage BackupSourceActivated(
        ILogger logger,
        Guid executionId,
        Guid backupDataSetId);

    /// <summary>
    /// Logs when RetryNotify strategy sends a terminal-failure notification.
    /// </summary>
    // Why Information, not Warning (FDW-583): the notification itself succeeded — the thing being
    // reported ABOUT was a failure, but this record is the deliberate notify action completing.
    [MessageLogging(
        EventId = 81008,
        Level = LogLevel.Information,
        Message = "Resiliency failure notification sent: executionId={executionId}, targetId={notificationTargetId}")]
    public static partial IGenericMessage NotificationSent(
        ILogger logger,
        Guid executionId,
        Guid notificationTargetId);

    /// <summary>
    /// Logs when a strategy type is not registered in ResiliencyTypes.
    /// </summary>
    [MessageLogging(
        EventId = 31002,
        Level = LogLevel.Error,
        Message = "Resiliency strategy type not found: executionId={executionId}, strategyType='{strategyType}'")]
    public static partial IGenericMessage StrategyNotFound(
        ILogger logger,
        Guid executionId,
        string strategyType);

    /// <summary>
    /// Logs when an unhandled exception occurs inside the ResiliencyExecutor.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Unhandled exception in ResiliencyExecutor: executionId={executionId}, stageId={stageId}")]
    public static partial IGenericMessage ResiliencyExecutorException(
        ILogger logger,
        Exception exception,
        Guid executionId,
        Guid stageId);
}
