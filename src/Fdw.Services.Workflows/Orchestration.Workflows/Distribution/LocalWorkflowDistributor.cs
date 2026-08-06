using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using Fdw.Orchestration.Workflows.Abstractions;
using Fdw.Orchestration.Workflows.Abstractions.Distribution;
using Fdw.Orchestration.Workflows.Execution;
using Fdw.Orchestration.Workflows.Logging;
using Fdw.Orchestration.Workflows.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Orchestration.Workflows.Distribution;

/// <summary>
/// Local workflow distributor that executes steps in-process.
/// This is the default implementation; for distributed execution,
/// implement IWorkflowDistributor with a message queue or task scheduler.
/// </summary>
public sealed class LocalWorkflowDistributor : IWorkflowDistributor
{
    private readonly ILogger<LocalWorkflowDistributor> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalWorkflowDistributor"/> class.
    /// </summary>
    public LocalWorkflowDistributor(
        ILogger<LocalWorkflowDistributor> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public bool IsDistributed => false;

    /// <inheritdoc/>
    public string DistributorName => "Local";

    /// <inheritdoc/>
    public bool CanDistribute(IWorkflow workflow) => true;

    /// <inheritdoc/>
    public async Task<IGenericResult<IWorkflowStepResult>> ExecuteStep(
        IWorkflow workflow,
        IWorkflowStep step,
        IWorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // Check condition if present
            if (step.Condition != null && !EvaluateCondition(step.Condition, context))
            {
                return GenericResult<IWorkflowStepResult>.Success(
                    WorkflowStepResult.Skipped(step.StepId, "Condition not met"));
            }

            // Execute based on step type
            var stepTypeName = step.Type.Name;

            return stepTypeName switch
            {
                "Pipeline" => await ExecutePipelineStep(step, context, startTime, cancellationToken).ConfigureAwait(false),
                "Wait" => await ExecuteWaitStep(step, startTime, cancellationToken).ConfigureAwait(false),
                "Decision" => await ExecuteDecisionStep(step, context, startTime).ConfigureAwait(false),
                "Notify" => await ExecuteNotifyStep(step, context, startTime).ConfigureAwait(false),
                "Custom" => await ExecuteCustomStep(step, context, startTime, cancellationToken).ConfigureAwait(false),
                _ => GenericResult<IWorkflowStepResult>.Success(
                    WorkflowStepResult.Failure(step.StepId, startTime, $"Unknown step type: {stepTypeName}"))
            };
        }
        catch (OperationCanceledException ex)
        {
            return GenericResult<IWorkflowStepResult>.Failure(
                WorkflowLogger.StepCancelled(_logger, ex, step.StepId));
        }
        catch (Exception ex)
        {
            return GenericResult<IWorkflowStepResult>.Failure(
                WorkflowLogger.StepExecutionException(_logger, ex, step.StepId));
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IWorkflowStepResult>> GetStepStatus(
        string stepExecutionId,
        CancellationToken cancellationToken = default)
    {
        // Local execution is synchronous, so status is immediately available
        // In distributed implementation, this would query the task queue
        return Task.FromResult(
            GenericResult<IWorkflowStepResult>.Failure(OrchestratedWorkflowResultCodes.ByName("StepStatusNotAvailable")));
    }

    private static bool EvaluateCondition(IWorkflowCondition condition, IWorkflowExecutionContext context)
    {
        var conditionTypeName = condition.Type.Name;

        return conditionTypeName switch
        {
            "Always" => true,
            "OnSuccess" => !context.SharedState.ContainsKey("__LastStepFailed"),
            "OnFailure" => context.SharedState.ContainsKey("__LastStepFailed"),
            "Expression" => EvaluateExpression(condition.Expression, context),
            _ => true
        };
    }

    private static bool EvaluateExpression(string? expression, IWorkflowExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return true;
        }

        // Simple expression evaluation - check if a shared state key exists and has a truthy value
        if (context.SharedState.TryGetValue(expression, out var value))
        {
            return value switch
            {
                bool b => b,
                string s => !string.IsNullOrEmpty(s) && !string.Equals(s, "false", StringComparison.OrdinalIgnoreCase),
                int i => i != 0,
                _ => value != null
            };
        }

        return false;
    }

    private async Task<IGenericResult<IWorkflowStepResult>> ExecutePipelineStep(
        IWorkflowStep step,
        IWorkflowExecutionContext context,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(step.PipelineId))
        {
            return GenericResult<IWorkflowStepResult>.Success(
                WorkflowStepResult.Failure(step.StepId, startTime, "Pipeline ID is required"));
        }

        // Why: NO simulated success. The workflow→pipeline execution seam is not yet built; until it is, a
        // Pipeline step fails loud with FDW MessageLogging rather than a Task.Delay stub that fakes success.
        // Tracked as a future feature.
        return await Task.FromResult(GenericResult<IWorkflowStepResult>.Failure(
            WorkflowLogger.PipelineExecutionNotWired(_logger, step.StepId, step.PipelineId))).ConfigureAwait(false);
    }

    private static async Task<IGenericResult<IWorkflowStepResult>> ExecuteWaitStep(
        IWorkflowStep step,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        if (step.Parameters.TryGetValue("DurationSeconds", out var durationObj) &&
            durationObj is int durationSeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cancellationToken).ConfigureAwait(false);
        }

        return GenericResult<IWorkflowStepResult>.Success(
            WorkflowStepResult.Success(step.StepId, startTime));
    }

    private static Task<IGenericResult<IWorkflowStepResult>> ExecuteDecisionStep(
        IWorkflowStep step,
        IWorkflowExecutionContext context,
        DateTimeOffset startTime)
    {
        // Decision steps evaluate a condition and set shared state
        if (step.Parameters.TryGetValue("Condition", out var conditionObj) &&
            conditionObj is string conditionExpr)
        {
            var result = EvaluateExpression(conditionExpr, context);
            context.SharedState[$"Decision_{step.StepId}"] = result;
        }

        return Task.FromResult(
            GenericResult<IWorkflowStepResult>.Success(
                WorkflowStepResult.Success(step.StepId, startTime)));
    }

    private Task<IGenericResult<IWorkflowStepResult>> ExecuteNotifyStep(
        IWorkflowStep step,
        IWorkflowExecutionContext context,
        DateTimeOffset startTime)
    {
        // Notification steps - would integrate with INotificationDispatcher
        WorkflowLogger.ExecutingNotificationStep(_logger, step.StepId);

        return Task.FromResult(
            GenericResult<IWorkflowStepResult>.Success(
                WorkflowStepResult.Success(step.StepId, startTime)));
    }

    private Task<IGenericResult<IWorkflowStepResult>> ExecuteCustomStep(
        IWorkflowStep step,
        IWorkflowExecutionContext context,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        // Custom steps would look up and execute a custom handler
        WorkflowLogger.ExecutingCustomStep(_logger, step.StepId);

        return Task.FromResult(
            GenericResult<IWorkflowStepResult>.Success(
                WorkflowStepResult.Success(step.StepId, startTime)));
    }
}
