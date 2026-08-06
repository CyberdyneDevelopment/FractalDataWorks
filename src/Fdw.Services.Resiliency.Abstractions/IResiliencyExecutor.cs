using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Resolves the effective resiliency policy for a stage, loads its configuration,
/// dispatches to the registered <see cref="IResiliencyType"/>, and wraps the stage delegate.
/// </summary>
/// <remarks>
/// Injected into OrchestrationNodeOrchestrator. The orchestrator calls Execute once per stage with
/// the effective policy id resolved by <see cref="IEffectiveResiliencyResolver"/>.
/// </remarks>
public interface IResiliencyExecutor
{
    /// <summary>
    /// Executes the stage delegate under the resiliency strategy bound to the given policy.
    /// </summary>
    /// <param name="policyId">
    /// The resolved effective policy identifier, or <c>null</c> to run with no resiliency (pass-through).
    /// </param>
    /// <param name="runStage">The delegate that re-runs the entire stage from scratch on each attempt.</param>
    /// <param name="ctx">Execution context with ExecutionId, StageId, and attempt metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The aggregate result from the strategy execution.</returns>
    Task<IGenericResult> Execute(
        Guid? policyId,
        Func<CancellationToken, Task<IGenericResult>> runStage,
        IResiliencyExecutionContext ctx,
        CancellationToken cancellationToken);
}
