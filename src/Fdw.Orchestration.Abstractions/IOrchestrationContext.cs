namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Execution context for an orchestration run.
/// </summary>
/// <remarks>
/// Extends <see cref="IExecutionContext"/> with orchestration-specific state:
/// the orchestration definition, step tracking, and the policy context that
/// governs error handling and caching for this run.
/// Universal per-run fields (ExecutionId, StartTime, CancellationToken, Logger,
/// Services, Parameters, SharedState) are inherited from <see cref="IExecutionContext"/>.
/// </remarks>
// Why: IOrchestrationContext previously redeclared all IExecutionContext fields.
// Now it composes them via inheritance, keeping the surface area focused on
// orchestration-specific concerns.
public interface IOrchestrationContext : IExecutionContext
{
    /// <summary>
    /// Gets the orchestration being executed.
    /// </summary>
    IOrchestration Orchestration { get; }

    /// <summary>
    /// Gets the current step being executed, if any.
    /// </summary>
    IOrchestrationStep? CurrentStep { get; }

    /// <summary>
    /// Gets the results of completed steps, keyed by step ID.
    /// </summary>
    System.Collections.Generic.IReadOnlyDictionary<string, IOrchestrationStepResult> CompletedSteps { get; }

    /// <summary>
    /// Gets the policy context governing this execution (error handling, caching, resiliency).
    /// </summary>
    IExecutionPolicyContext Policy { get; }
}

/// <summary>
/// Generic context interface with typed orchestration.
/// </summary>
/// <typeparam name="TOrchestration">The orchestration type.</typeparam>
public interface IOrchestrationContext<TOrchestration> : IOrchestrationContext
    where TOrchestration : class, IOrchestration
{
    /// <summary>
    /// Gets the typed orchestration being executed.
    /// </summary>
    new TOrchestration Orchestration { get; }
}
