using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Orchestration.Workflows.Abstractions;

/// <summary>
/// Resolves dependencies between workflow steps and pipelines.
/// </summary>
public interface IDependencyResolver
{
    /// <summary>
    /// Resolves the execution order for workflow steps.
    /// </summary>
    /// <param name="steps">The workflow steps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the ordered execution groups.</returns>
    Task<IGenericResult<IReadOnlyList<IExecutionGroup>>> ResolveExecutionOrder(
        IReadOnlyList<IWorkflowStep> steps,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that step dependencies are valid.
    /// </summary>
    /// <param name="steps">The workflow steps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with validation errors if any.</returns>
    Task<IGenericResult> ValidateDependencies(
        IReadOnlyList<IWorkflowStep> steps,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for circular dependencies.
    /// </summary>
    /// <param name="steps">The workflow steps.</param>
    /// <returns>Result containing circular dependency paths if found.</returns>
    IGenericResult<IReadOnlyList<string>> CheckForCircularDependencies(
        IReadOnlyList<IWorkflowStep> steps);

    /// <summary>
    /// Gets steps that can execute in parallel.
    /// </summary>
    /// <param name="steps">The workflow steps.</param>
    /// <param name="completedStepIds">IDs of steps that have completed.</param>
    /// <returns>Result containing steps that can execute in parallel.</returns>
    IGenericResult<IReadOnlyList<IWorkflowStep>> GetParallelExecutableSteps(
        IReadOnlyList<IWorkflowStep> steps,
        IReadOnlyCollection<string> completedStepIds);
}