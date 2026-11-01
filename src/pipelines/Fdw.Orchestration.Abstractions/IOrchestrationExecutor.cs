using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Executes orchestrations.
/// </summary>
public interface IOrchestrationExecutor
{
    /// <summary>
    /// Executes an orchestration.
    /// </summary>
    /// <param name="orchestration">The orchestration to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<IGenericResult<IOrchestrationResult>> Execute(
        IOrchestration orchestration,
        IOrchestrationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic executor interface for typed orchestrations.
/// </summary>
/// <typeparam name="TOrchestration">The orchestration type.</typeparam>
public interface IOrchestrationExecutor<TOrchestration> : IOrchestrationExecutor
    where TOrchestration : class, IOrchestration
{
    /// <summary>
    /// Executes a typed orchestration.
    /// </summary>
    /// <param name="orchestration">The orchestration to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<IGenericResult<IOrchestrationResult>> Execute(
        TOrchestration orchestration,
        IOrchestrationContext<TOrchestration> context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic executor interface with typed orchestration and output.
/// </summary>
/// <typeparam name="TOrchestration">The orchestration type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public interface IOrchestrationExecutor<TOrchestration, TOutput> : IOrchestrationExecutor<TOrchestration>
    where TOrchestration : class, IOrchestration
{
    /// <summary>
    /// Executes a typed orchestration and returns typed output.
    /// </summary>
    /// <param name="orchestration">The orchestration to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The typed execution result.</returns>
    new Task<IGenericResult<IOrchestrationResult<TOutput>>> Execute(
        TOrchestration orchestration,
        IOrchestrationContext<TOrchestration> context,
        CancellationToken cancellationToken = default);
}

