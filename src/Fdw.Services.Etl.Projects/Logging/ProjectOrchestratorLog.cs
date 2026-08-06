using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Logging;

/// <summary>
/// MessageLogging for Project orchestrator operations.
/// EventId range: 8180-8199
/// </summary>
[MessageLoggingTypeCode("PROJECTS")]
public static partial class ProjectOrchestratorLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Background Service Lifecycle (8180-8183)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the project orchestrator background service starts.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information,
        Message = "Project orchestrator background service started")]
    public static partial IGenericMessage OrchestratorStarted(ILogger logger);

    /// <summary>Logs when the project orchestrator background service is stopping.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Information,
        Message = "Project orchestrator background service stopping, draining queue")]
    public static partial IGenericMessage OrchestratorStopping(ILogger logger);

    /// <summary>Logs when the project execution queue is full and a request is dropped.</summary>
    [MessageLogging(EventId = 81002, Level = LogLevel.Warning,
        Message = "Project execution queue full, dropping request for project '{projectName}'")]
    public static partial IGenericMessage ProjectOrchestratorQueueFull(ILogger logger, string projectName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Project Execution (8184-8187)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a project execution starts.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information,
        Message = "Project execution started: '{projectName}' (ExecutionId: {executionId}, Trigger: {triggerSource})")]
    public static partial IGenericMessage ProjectExecutionStarted(
        ILogger logger, string projectName, Guid executionId, string triggerSource);

    /// <summary>Logs when a project execution completes (success or failure).</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Information,
        Message = "Project execution completed: '{projectName}' (ExecutionId: {executionId}, Status: {status})")]
    public static partial IGenericMessage ProjectExecutionCompleted(
        ILogger logger, string projectName, Guid executionId, string status);

    /// <summary>Logs when a project is not found in configuration.</summary>
    [MessageLogging(EventId = 31011, Level = LogLevel.Error,
        Message = "Project not found: '{projectName}' — cannot execute")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Logs when an unhandled exception occurs in the project orchestrator.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "Unhandled exception in project orchestrator for '{projectName}' (ExecutionId: {executionId})")]
    public static partial IGenericMessage ProjectOrchestratorException(
        ILogger logger, Exception exception, string projectName, Guid executionId);

    /// <summary>Logs when a project execution item is not found in the tracker.</summary>
    [MessageLogging(EventId = 31012, Level = LogLevel.Error,
        Message = "Project execution item not found: executionItemId={executionItemId}")]
    public static partial IGenericMessage ProjectExecutionItemNotFound(ILogger logger, Guid executionItemId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Stage Execution (8188-8189)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a stage execution starts.</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Information,
        Message = "Stage execution started: '{stageName}' (Ordinal: {ordinal}, ExecutionId: {executionId})")]
    public static partial IGenericMessage StageExecutionStarted(
        ILogger logger, string stageName, int ordinal, Guid executionId);

    /// <summary>Logs when a stage execution fails.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error,
        Message = "Stage execution failed: '{stageName}' (Ordinal: {ordinal}, ExecutionId: {executionId})")]
    public static partial IGenericMessage StageExecutionFailed(
        ILogger logger, string stageName, int ordinal, Guid executionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Step Execution (8190-8191)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a step execution starts.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Information,
        Message = "Step execution started: '{stepName}' (Ordinal: {ordinal}, ExecutionId: {executionId})")]
    public static partial IGenericMessage StepExecutionStarted(
        ILogger logger, string stepName, int ordinal, Guid executionId);

    /// <summary>Logs when a step execution fails.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error,
        Message = "Step execution failed: '{stepName}' (Ordinal: {ordinal}, ExecutionId: {executionId})")]
    public static partial IGenericMessage StepExecutionFailed(
        ILogger logger, string stepName, int ordinal, Guid executionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Pipeline Dispatch (8192-8195)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a pipeline is dispatched to the execution queue from a step.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Information,
        Message = "Pipeline dispatched from step '{stepName}': pipelineId={pipelineId}, childExecutionId={childExecutionId}")]
    public static partial IGenericMessage PipelineDispatched(
        ILogger logger, string stepName, Guid pipelineId, Guid childExecutionId);

    /// <summary>Logs when a pipeline completion signal is received by the orchestrator.</summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Information,
        Message = "Pipeline completion signal received: executionId={executionId}, succeeded={succeeded}")]
    public static partial IGenericMessage PipelineCompletionReceived(
        ILogger logger, Guid executionId, bool succeeded);

    /// <summary>Logs when a completion signal was not received within the expected window.</summary>
    [MessageLogging(EventId = 81003, Level = LogLevel.Warning,
        Message = "Completion signal timeout or cancellation for executionId={executionId}")]
    public static partial IGenericMessage CompletionSignalTimeout(ILogger logger, Guid executionId);

    /// <summary>Logs when a completion signal is received and resolved by the signaler.</summary>
    [MessageLogging(EventId = 11026, Level = LogLevel.Trace,
        Message = "Completion signal resolved: executionId={executionId}")]
    public static partial IGenericMessage CompletionSignalReceived(ILogger logger, Guid executionId);

    /// <summary>Logs when Await finds no completion source registered for an execution item — the pipeline was never dispatched properly or was already cleaned up; treated as a failure.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error,
        Message = "No completion signal registered for executionItemId={executionItemId}; treating as failure")]
    public static partial IGenericMessage CompletionSignalNotRegistered(ILogger logger, Guid executionItemId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Policy (8196-8199)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when execution is halted due to a failure policy.</summary>
    [MessageLogging(EventId = 41009, Level = LogLevel.Warning,
        Message = "Halting execution due to policy '{policy}' at '{level}' (ExecutionId: {executionId})")]
    public static partial IGenericMessage HaltingDueToPolicy(
        ILogger logger, string policy, string level, Guid executionId);

    /// <summary>Logs when a stage fails after all steps have been attempted.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error,
        Message = "Stage failed: stageExecutionItemId={stageExecutionItemId}")]
    public static partial IGenericMessage StageFailed(ILogger logger, Guid stageExecutionItemId);
}
