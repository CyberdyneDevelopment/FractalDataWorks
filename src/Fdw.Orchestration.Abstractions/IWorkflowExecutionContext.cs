using Fdw.Orchestration.Abstractions;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Workflow execution context.
/// </summary>
/// <remarks>
/// Extends <see cref="IExecutionContext"/> with workflow-specific state:
/// the workflow definition ID, trigger source, dry-run flag, correlation tracing,
/// and the policy context governing error handling and caching.
/// Universal per-run fields (ExecutionId, StartTime, CancellationToken, Logger,
/// Services, Parameters, SharedState) are inherited from <see cref="IExecutionContext"/>.
/// </remarks>
// Why: Removes duplication of StartTime/CancellationToken/Parameters/SharedState
// that previously existed independently on this interface alongside IOrchestrationContext.
public interface IWorkflowExecutionContext : IExecutionContext
{
    /// <summary>
    /// Gets the workflow definition ID.
    /// </summary>
    string WorkflowId { get; }

    /// <summary>
    /// Gets who triggered the workflow.
    /// </summary>
    string TriggeredBy { get; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets whether this is a dry run.
    /// </summary>
    bool IsDryRun { get; }

    /// <summary>
    /// Gets the policy context governing this execution (error handling, caching, resiliency).
    /// </summary>
    IExecutionPolicyContext Policy { get; }
}
