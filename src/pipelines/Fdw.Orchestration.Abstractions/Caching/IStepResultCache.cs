using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// Cache for orchestration step results.
/// </summary>
/// <remarks>
/// Caches results of idempotent steps to avoid re-execution.
/// Useful for steps that are expensive to run but produce the same output
/// given the same input.
/// </remarks>
public interface IStepResultCache
{
    /// <summary>
    /// Gets a cached step result.
    /// </summary>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="stepId">The step ID.</param>
    /// <param name="inputHash">Hash of the step input (for cache key differentiation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached result, or null if not found.</returns>
    Task<IOrchestrationStepResult?> GetResult(
        string orchestrationId,
        string stepId,
        string? inputHash = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed cached step result.
    /// </summary>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="stepId">The step ID.</param>
    /// <param name="inputHash">Hash of the step input (for cache key differentiation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached result, or null if not found.</returns>
    Task<IOrchestrationStepResult<TOutput>?> GetResult<TOutput>(
        string orchestrationId,
        string stepId,
        string? inputHash = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches a step result.
    /// </summary>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="result">The step result to cache.</param>
    /// <param name="inputHash">Hash of the step input (for cache key differentiation).</param>
    /// <param name="options">Cache options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CacheResult(
        string orchestrationId,
        IOrchestrationStepResult result,
        string? inputHash = null,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached results for a step.
    /// </summary>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="stepId">The step ID. If null, invalidates all steps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateResults(
        string orchestrationId,
        string? stepId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a step is cacheable based on its configuration.
    /// </summary>
    /// <param name="step">The step to check.</param>
    /// <returns>True if the step's results can be cached.</returns>
    bool IsStepCacheable(IOrchestrationStep step);

    /// <summary>
    /// Computes a hash of the step input for cache key differentiation.
    /// </summary>
    /// <param name="input">The step input.</param>
    /// <returns>A hash string representing the input.</returns>
    string ComputeInputHash(object? input);
}
