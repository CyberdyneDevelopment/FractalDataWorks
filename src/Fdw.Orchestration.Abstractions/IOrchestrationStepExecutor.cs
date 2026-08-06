using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Executes individual orchestration steps.
/// </summary>
public interface IOrchestrationStepExecutor
{
    /// <summary>
    /// Executes a single step.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="input">Input from the previous step, if any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The step execution result.</returns>
    Task<IGenericResult<IOrchestrationStepResult>> Execute(
        IOrchestrationStep step,
        IOrchestrationContext context,
        object? input,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic step executor for typed steps.
/// </summary>
/// <typeparam name="TStep">The step type.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public interface IOrchestrationStepExecutor<TStep, TInput, TOutput> : IOrchestrationStepExecutor
    where TStep : class, IOrchestrationStep
{
    /// <summary>
    /// Executes a typed step with typed input and output.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="input">Typed input from the previous step.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The typed step execution result.</returns>
    Task<IGenericResult<IOrchestrationStepResult<TOutput>>> Execute(
        TStep step,
        IOrchestrationContext context,
        TInput? input,
        CancellationToken cancellationToken = default);
}
