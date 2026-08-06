using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fdw.Conventions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Orchestration.Workflows.Abstractions;
using Fdw.Orchestration.Workflows.Abstractions.Distribution;
using Fdw.Orchestration.Workflows.Abstractions.WorkflowExecutionStatusOptions;
using Fdw.Orchestration.Workflows.Execution;
using Fdw.Orchestration.Workflows.Logging;
using Fdw.Orchestration.Workflows.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Orchestration.Workflows;

/// <summary>
/// Orchestrates workflow execution across multiple steps.
/// </summary>
public sealed class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IWorkflowDistributor _distributor;
    private readonly ILogger<WorkflowOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, WorkflowExecutionState> _executions = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowOrchestrator"/> class.
    /// </summary>
    public WorkflowOrchestrator(
        IWorkflowDistributor distributor,
        ILogger<WorkflowOrchestrator> logger)
    {
        _distributor = distributor;
        _logger = logger;
    }

    /// <inheritdoc/>
    [ConventionOverride(MaxMethodLines = 85)]
    public async Task<IGenericResult<IWorkflowExecutionResult>> ExecuteWorkflow(
        IWorkflow workflow,
        IWorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workflow.WorkflowId))
        {
            return GenericResult<IWorkflowExecutionResult>.Failure(
                WorkflowLogger.WorkflowIdRequired(_logger));
        }

        if (string.IsNullOrEmpty(workflow.Name))
        {
            return GenericResult<IWorkflowExecutionResult>.Failure(
                WorkflowLogger.WorkflowNameRequired(_logger));
        }

        if (workflow.Steps.Count == 0)
        {
            return GenericResult<IWorkflowExecutionResult>.Failure(
                WorkflowLogger.WorkflowMustHaveSteps(_logger));
        }

        // Get execution state
        var state = new WorkflowExecutionState(
            context.ExecutionId.ToString(),
            context.WorkflowId,
            context.StartTime,
            workflow.Steps.Count);
        _executions[context.ExecutionId.ToString()] = state;

        WorkflowLogger.WorkflowExecutionStarting(
            _logger,
            workflow.WorkflowId,
            context.ExecutionId.ToString(),
            workflow.Steps.Count);

        var stepResults = new List<IWorkflowStepResult>();

        try
        {
            foreach (var step in workflow.Steps)
            {
                if (cancellationToken.IsCancellationRequested || state.IsCancelled)
                {
                    state.SetStatus(WorkflowExecutionStatuses.ByName("Cancelled"));
                    break;
                }

                while (state.IsPaused)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                state.SetCurrentStep(step.StepId);

                WorkflowLogger.ExecutingWorkflowStep(
                    _logger,
                    step.StepId,
                    step.Name,
                    workflow.WorkflowId);

                var stepResult = await _distributor.ExecuteStep(
                    workflow,
                    step,
                    context,
                    cancellationToken).ConfigureAwait(false);

                if (stepResult.IsSuccess && stepResult.Value != null)
                {
                    stepResults.Add(stepResult.Value);

                    if (string.Equals(stepResult.Value.Status.Name, "Failed", StringComparison.Ordinal))
                    {
                        context.SharedState["__LastStepFailed"] = true;

                        // Check if we should stop on failure (stop when ContinueOnFailure is false)
                        if (!context.Policy.ContinueOnFailure)
                        {
                            WorkflowLogger.StepFailedStoppingWorkflow(_logger, step.StepId);
                            break;
                        }
                    }
                    else
                    {
                        context.SharedState.Remove("__LastStepFailed");
                    }
                }
                else
                {
                    var stepErrorMessage = stepResult.CurrentMessage ?? "Unknown error";
                    WorkflowLogger.StepExecutionResultFailure(_logger, step.StepId, stepErrorMessage);
                    stepResults.Add(WorkflowStepResult.Failure(
                        step.StepId,
                        DateTimeOffset.UtcNow,
                        stepErrorMessage));
                }

                state.IncrementCompletedSteps();
            }

            var result = WorkflowExecutionResult.FromStepResults(
                context.ExecutionId.ToString(),
                workflow.WorkflowId,
                context.StartTime,
                stepResults);

            state.SetStatus(result.Status);

            WorkflowLogger.WorkflowExecutionCompleted(
                _logger,
                workflow.WorkflowId,
                context.ExecutionId.ToString(),
                result.Status.Name,
                result.SuccessfulSteps,
                result.FailedSteps,
                result.SkippedSteps);

            return GenericResult<IWorkflowExecutionResult>.Success(result);
        }
        catch (Exception ex)
        {
            var result = WorkflowExecutionResult.Failure(
                context.ExecutionId.ToString(),
                workflow.WorkflowId,
                context.StartTime,
                stepResults,
                ex.Message);

            state.SetStatus(result.Status);

            return GenericResult<IWorkflowExecutionResult>.Failure(
                WorkflowLogger.WorkflowExecutionException(_logger, ex, workflow.WorkflowId));
        }
        finally
        {
            // Keep execution state for a while for status queries
            // In production, this would be persisted to a database
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ICurrentWorkflowStatus>> GetStatus(
        string workflowExecutionId,
        CancellationToken cancellationToken = default)
    {
        if (!_executions.TryGetValue(workflowExecutionId, out var state))
        {
            return Task.FromResult(
                GenericResult<ICurrentWorkflowStatus>.Failure(
                    WorkflowLogger.WorkflowExecutionNotFound(_logger, workflowExecutionId)));
        }

        var status = new CurrentWorkflowStatus(
            state.WorkflowExecutionId,
            state.Status,
            state.CurrentStepId,
            state.CompletedSteps,
            state.TotalSteps,
            state.StartTime);

        return Task.FromResult(GenericResult<ICurrentWorkflowStatus>.Success(status));
    }

    /// <inheritdoc/>
    public Task<IGenericResult> Pause(
        string workflowExecutionId,
        CancellationToken cancellationToken = default)
    {
        if (!_executions.TryGetValue(workflowExecutionId, out var state))
        {
            return Task.FromResult(
                GenericResult.Failure(
                    WorkflowLogger.WorkflowExecutionNotFound(_logger, workflowExecutionId)));
        }

        state.Pause();
        WorkflowLogger.WorkflowExecutionPaused(_logger, workflowExecutionId);

        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc/>
    public Task<IGenericResult> Resume(
        string workflowExecutionId,
        CancellationToken cancellationToken = default)
    {
        if (!_executions.TryGetValue(workflowExecutionId, out var state))
        {
            return Task.FromResult(
                GenericResult.Failure(
                    WorkflowLogger.WorkflowExecutionNotFound(_logger, workflowExecutionId)));
        }

        state.Resume();
        WorkflowLogger.WorkflowExecutionResumed(_logger, workflowExecutionId);

        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc/>
    public Task<IGenericResult> Cancel(
        string workflowExecutionId,
        CancellationToken cancellationToken = default)
    {
        if (!_executions.TryGetValue(workflowExecutionId, out var state))
        {
            return Task.FromResult(
                GenericResult.Failure(
                    WorkflowLogger.WorkflowExecutionNotFound(_logger, workflowExecutionId)));
        }

        state.Cancel();
        WorkflowLogger.WorkflowExecutionCancelled(_logger, workflowExecutionId);

        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc/>
    public Task<IGenericResult> RetryStep(
        string workflowExecutionId,
        string stepId,
        CancellationToken cancellationToken = default)
    {
        // Retry would require persisting workflow state and re-executing from a specific step
        // This is a placeholder for the full implementation
        return Task.FromResult(
            GenericResult.Failure(OrchestratedWorkflowResultCodes.ByName("StepRetryNotImplemented")));
    }

    private sealed class WorkflowExecutionState
    {
        private volatile bool _isPaused;
        private volatile bool _isCancelled;

        public WorkflowExecutionState(
            string workflowExecutionId,
            string workflowId,
            DateTimeOffset startTime,
            int totalSteps)
        {
            WorkflowExecutionId = workflowExecutionId;
            WorkflowId = workflowId;
            StartTime = startTime;
            TotalSteps = totalSteps;
            Status = WorkflowExecutionStatuses.ByName("Running");
        }

        public string WorkflowExecutionId { get; }
        public string WorkflowId { get; }
        public DateTimeOffset StartTime { get; }
        public int TotalSteps { get; }
        public int CompletedSteps { get; private set; }
        public string? CurrentStepId { get; private set; }
        public IWorkflowExecutionStatus Status { get; private set; }
        public bool IsPaused => _isPaused;
        public bool IsCancelled => _isCancelled;

        public void IncrementCompletedSteps() => CompletedSteps++;
        public void SetCurrentStep(string stepId) => CurrentStepId = stepId;
        public void SetStatus(IWorkflowExecutionStatus status) => Status = status;
        public void Pause() => _isPaused = true;
        public void Resume() => _isPaused = false;
        public void Cancel() => _isCancelled = true;
    }
}
