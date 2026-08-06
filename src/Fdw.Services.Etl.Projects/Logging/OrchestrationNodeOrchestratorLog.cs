using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Logging;

/// <summary>
/// MessageLogging for OrchestrationNode orchestrator operations.
/// EventId range: 8180-8199.
/// </summary>
/// <remarks>
/// Replaces <see cref="ProjectOrchestratorLog"/> (v1). ProjectOrchestratorLog is preserved as an
/// [Obsolete] alias during the transition release.
/// </remarks>
[MessageLoggingTypeCode("PROJECTS")]
public static partial class OrchestrationNodeOrchestratorLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Background Service Lifecycle (8180-8183)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the orchestrator background service starts.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "OrchestrationNode orchestrator background service started")]
    public static partial IGenericMessage OrchestratorStarted(ILogger logger);

    /// <summary>Logs when the orchestrator background service is stopping.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "OrchestrationNode orchestrator background service stopping, draining queue")]
    public static partial IGenericMessage OrchestratorStopping(ILogger logger);

    /// <summary>Logs when the execution queue is full and a request is dropped.</summary>
    [MessageLogging(EventId = 81000, Level = LogLevel.Warning,
        Message = "OrchestrationNode execution queue full, dropping request for node '{nodeName}'")]
    public static partial IGenericMessage OrchestratorQueueFull(ILogger logger, string nodeName);

    /// <summary>Logs when an execution item is not found in the tracker.</summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Error,
        Message = "OrchestrationNode execution item not found: executionItemId={executionItemId}")]
    public static partial IGenericMessage ExecutionItemNotFound(ILogger logger, Guid executionItemId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Node Execution (8184-8187)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a node execution starts.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Node execution started: '{nodeName}' (NodeType: {nodeType}, ExecutionId: {executionId}, Trigger: {triggerSource})")]
    public static partial IGenericMessage NodeExecutionStarted(
        ILogger logger, string nodeName, string nodeType, Guid executionId, string triggerSource);

    /// <summary>Logs when a node execution completes (success or failure).</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Node execution completed: '{nodeName}' (ExecutionId: {executionId}, Status: {status})")]
    public static partial IGenericMessage NodeExecutionCompleted(
        ILogger logger, string nodeName, Guid executionId, string status);

    /// <summary>Logs when a node is not found in configuration.</summary>
    [MessageLogging(EventId = 31005, Level = LogLevel.Error,
        Message = "OrchestrationNode not found: Id='{nodeId}' — cannot execute")]
    public static partial IGenericMessage NodeNotFound(ILogger logger, Guid nodeId);

    /// <summary>Logs when an unhandled exception occurs in the orchestrator.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Unhandled exception in orchestrator for '{nodeName}' (ExecutionId: {executionId})")]
    public static partial IGenericMessage OrchestratorException(
        ILogger logger, Exception exception, string nodeName, Guid executionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Child Node Execution (8188-8191)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a branch child node execution starts.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "Child node execution started: '{nodeName}' (Ordinal: {ordinal}, ExecutionId: {executionId})")]
    public static partial IGenericMessage ChildNodeExecutionStarted(
        ILogger logger, string nodeName, int ordinal, Guid executionId);

    /// <summary>Logs when a branch child node execution fails.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "Child node execution failed: '{nodeName}' (Ordinal: {ordinal}, ExecutionId: {executionId})")]
    public static partial IGenericMessage ChildNodeExecutionFailed(
        ILogger logger, string nodeName, int ordinal, Guid executionId);

    /// <summary>Logs when a leaf node (CanHostPipelines) step execution starts.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "Leaf node pipeline execution started: '{nodeName}' (Ordinal: {ordinal}, ExecutionId: {executionId})")]
    public static partial IGenericMessage LeafNodeExecutionStarted(
        ILogger logger, string nodeName, int ordinal, Guid executionId);

    /// <summary>Logs when a leaf node execution fails.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "Leaf node pipeline execution failed: '{nodeName}' (Ordinal: {ordinal}, ExecutionId: {executionId})")]
    public static partial IGenericMessage LeafNodeExecutionFailed(
        ILogger logger, string nodeName, int ordinal, Guid executionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Pipeline Dispatch (8192-8195)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a pipeline is dispatched to the execution queue from a leaf node.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information,
        Message = "Pipeline dispatched from node '{nodeName}': pipelineId={pipelineId}, childExecutionId={childExecutionId}")]
    public static partial IGenericMessage PipelineDispatched(
        ILogger logger, string nodeName, Guid pipelineId, Guid childExecutionId);

    /// <summary>Logs when a pipeline completion signal is received by the orchestrator.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information,
        Message = "Pipeline completion signal received: executionId={executionId}, succeeded={succeeded}")]
    public static partial IGenericMessage PipelineCompletionReceived(
        ILogger logger, Guid executionId, bool succeeded);

    /// <summary>Logs when a completion signal was not received within the expected window.</summary>
    [MessageLogging(EventId = 81001, Level = LogLevel.Warning,
        Message = "Completion signal timeout or cancellation for executionId={executionId}")]
    public static partial IGenericMessage CompletionSignalTimeout(ILogger logger, Guid executionId);

    /// <summary>Logs when a completion signal is resolved by the signaler.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "Completion signal resolved: executionId={executionId}")]
    public static partial IGenericMessage CompletionSignalReceived(ILogger logger, Guid executionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Policy (8196-8199)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when execution is halted due to a failure policy.</summary>
    [MessageLogging(EventId = 41005, Level = LogLevel.Warning,
        Message = "Halting execution due to policy '{policy}' at '{level}' (ExecutionId: {executionId})")]
    public static partial IGenericMessage HaltingDueToPolicy(
        ILogger logger, string policy, string level, Guid executionId);

    /// <summary>Logs when a node fails after all children have been attempted.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "Node failed: executionItemId={executionItemId}")]
    public static partial IGenericMessage NodeFailed(ILogger logger, Guid executionItemId);

    /// <summary>Logs when a queued orchestration run is cancelled by host shutdown (a clean, expected exit).</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Debug,
        Message = "Orchestration run cancelled during host shutdown: rootNodeId={rootNodeId}, executionId={executionId}")]
    public static partial IGenericMessage OrchestratorCancelledDuringShutdown(
        ILogger logger,
        Exception ex,
        string rootNodeId,
        Guid executionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Non-Fatal Lifecycle Warnings (91004-91007)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the root TransitionState call fails. Non-fatal — orchestration continues.
    /// </summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Warning,
        Message = "Root TransitionState failed for execution {executionId}: {message}")]
    public static partial IGenericMessage RootTransitionStateFailed(ILogger logger, Guid executionId, string? message);

    /// <summary>
    /// Logs when a child TransitionState call fails. Non-fatal — child execution continues.
    /// </summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Warning,
        Message = "Child TransitionState failed for execution item {childItemId}: {message}")]
    public static partial IGenericMessage ChildTransitionStateFailed(ILogger logger, Guid childItemId, string? message);

    /// <summary>
    /// Logs when a child Complete call fails. Non-fatal — orchestration continues.
    /// </summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Warning,
        Message = "Child Complete failed for execution item {childItemId}: {message}")]
    public static partial IGenericMessage ChildCompleteFailed(ILogger logger, Guid childItemId, string? message);

    /// <summary>
    /// Logs when the root Complete call fails. Non-fatal — broadcast still occurs.
    /// </summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Warning,
        Message = "Root Complete failed for execution {executionId}: {message}")]
    public static partial IGenericMessage RootCompleteFailed(ILogger logger, Guid executionId, string? message);

    /// <summary>
    /// Logs when a per-execution WorkAuthenticationContext is established on the background node
    /// execution's DI scope, carrying the execution's TenantId for RLS SESSION_CONTEXT.
    /// </summary>
    [MessageLogging(EventId = 11027, Level = LogLevel.Information,
        Message = "WorkAuthenticationContext established for node execution {executionId} with TenantId {tenantId}")]
    public static partial IGenericMessage WorkAuthenticationContextEstablished(ILogger logger, Guid executionId, Guid tenantId);
}
