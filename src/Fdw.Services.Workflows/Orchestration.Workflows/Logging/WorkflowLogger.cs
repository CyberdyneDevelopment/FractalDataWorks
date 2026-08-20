using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Orchestration.Workflows.Logging;

/// <summary>
/// Message logging for workflow operations.
/// EventId range: 9001-9099
/// </summary>
[MessageLoggingTypeCode("WORKFLOW")]
public static partial class WorkflowLogger
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Validation Errors (9001-9010)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs validation error for missing WorkflowId.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error, Message = "WorkflowId is required")]
    public static partial IGenericMessage WorkflowIdRequired(ILogger logger);

    /// <summary>Logs validation error for missing workflow name.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error, Message = "Workflow name is required")]
    public static partial IGenericMessage WorkflowNameRequired(ILogger logger);

    /// <summary>Logs validation error for workflow with no steps.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error, Message = "Workflow must have at least one step")]
    public static partial IGenericMessage WorkflowMustHaveSteps(ILogger logger);

    /// <summary>Logs error when workflow execution is not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error, Message = "Workflow execution '{workflowExecutionId}' not found")]
    public static partial IGenericMessage WorkflowExecutionNotFound(ILogger logger, string workflowExecutionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Workflow Execution Events (9011-9030)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a workflow execution starts.</summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Starting workflow {workflowId} execution {executionId} with {stepCount} steps")]
    public static partial IGenericMessage WorkflowExecutionStarting(
        ILogger logger,
        string workflowId,
        string executionId,
        int stepCount);

    /// <summary>Logs when a workflow step is being executed.</summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Executing step {stepId} ({stepName}) in workflow {workflowId}")]
    public static partial IGenericMessage ExecutingWorkflowStep(
        ILogger logger,
        string stepId,
        string stepName,
        string workflowId);

    /// <summary>Logs when a step fails and workflow stops due to ContinueOnFailure=false.</summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Step {stepId} failed, stopping workflow (ContinueOnFailure=false)")]
    public static partial IGenericMessage StepFailedStoppingWorkflow(
        ILogger logger,
        string stepId);

    /// <summary>Logs when a workflow execution completes.</summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Workflow {workflowId} execution {executionId} completed with status {status}. Successful: {successCount}, Failed: {failedCount}, Skipped: {skippedCount}")]
    public static partial IGenericMessage WorkflowExecutionCompleted(
        ILogger logger,
        string workflowId,
        string executionId,
        string status,
        int successCount,
        int failedCount,
        int skippedCount);

    /// <summary>Logs when an exception occurs during workflow execution.</summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Error executing workflow {workflowId}")]
    public static partial IGenericMessage WorkflowExecutionException(
        ILogger logger,
        Exception exception,
        string workflowId);

    /// <summary>Logs when a step's distributor result is not successful (infrastructure-level failure, distinct from a business-status "Failed" step result).</summary>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "Step {stepId} execution did not succeed: {error}")]
    public static partial IGenericMessage StepExecutionResultFailure(
        ILogger logger,
        string stepId,
        string error);

    // ═══════════════════════════════════════════════════════════════════════════
    // Workflow Control Events (9031-9040)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a workflow execution is paused.</summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Paused workflow execution {executionId}")]
    public static partial IGenericMessage WorkflowExecutionPaused(
        ILogger logger,
        string executionId);

    /// <summary>Logs when a workflow execution is resumed.</summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Resumed workflow execution {executionId}")]
    public static partial IGenericMessage WorkflowExecutionResumed(
        ILogger logger,
        string executionId);

    /// <summary>Logs when a workflow execution is cancelled.</summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Cancelled workflow execution {executionId}")]
    public static partial IGenericMessage WorkflowExecutionCancelled(
        ILogger logger,
        string executionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Step Execution Events (9041-9060)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a step execution is cancelled.</summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Step {stepId} execution was cancelled")]
    public static partial IGenericMessage StepExecutionCancelled(
        ILogger logger,
        string stepId);

    /// <summary>Logs when a step execution fails with an exception.</summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Error executing step {stepId}")]
    public static partial IGenericMessage StepExecutionException(
        ILogger logger,
        Exception exception,
        string stepId);

    /// <summary>Logs when a pipeline step is being executed.</summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Executing pipeline {pipelineId} for step {stepId}")]
    public static partial IGenericMessage ExecutingPipelineStep(
        ILogger logger,
        string pipelineId,
        string stepId);

    /// <summary>Logs when a notification step is being executed.</summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Trace,
        Message = "Notification step {stepId}: Would send notification")]
    public static partial IGenericMessage ExecutingNotificationStep(
        ILogger logger,
        string stepId);

    /// <summary>Logs when a custom step is being executed.</summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Trace,
        Message = "Custom step {stepId}: Would execute custom handler")]
    public static partial IGenericMessage ExecutingCustomStep(
        ILogger logger,
        string stepId);

    /// <summary>Logs when a step execution is cancelled.</summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Warning,
        Message = "Step {stepId} execution was cancelled")]
    public static partial IGenericMessage StepCancelled(
        ILogger logger,
        Exception exception,
        string stepId);

    /// <summary>Logs that workflow→pipeline execution is not yet wired (fail-loud, no simulated success).</summary>
    // Why: the local distributor must not pretend a pipeline step succeeded. Until the workflow→pipeline
    // execution seam is built, a Pipeline step fails loud with this message instead of a Task.Delay stub.
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "Pipeline step {stepId} (pipeline {pipelineId}): workflow→pipeline execution not yet wired")]
    public static partial IGenericMessage PipelineExecutionNotWired(
        ILogger logger,
        string stepId,
        string pipelineId);
}
