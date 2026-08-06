using Polly;

namespace Fdw.Orchestration.Abstractions.Resilience;

/// <summary>
/// Factory for creating Polly resilience pipelines from orchestration configuration.
/// </summary>
/// <remarks>
/// Bridges the gap between our TypeCollection-based configuration and Polly's
/// resilience pipeline execution. Our TypeCollections define WHAT strategy to use,
/// Polly executes HOW to apply it.
/// </remarks>
public interface IResiliencePipelineFactory
{
    /// <summary>
    /// Creates a resilience pipeline for a step execution.
    /// </summary>
    /// <param name="options">The resilience options.</param>
    /// <returns>A configured Polly resilience pipeline.</returns>
    ResiliencePipeline Create(ResilienceOptions options);

    /// <summary>
    /// Creates a resilience pipeline for a step execution with typed result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="options">The resilience options.</param>
    /// <returns>A configured Polly resilience pipeline.</returns>
    ResiliencePipeline<TResult> Create<TResult>(ResilienceOptions options);

    /// <summary>
    /// Creates a resilience pipeline from step configuration.
    /// </summary>
    /// <param name="step">The orchestration step.</param>
    /// <returns>A configured Polly resilience pipeline.</returns>
    ResiliencePipeline Create(IOrchestrationStep step);

    /// <summary>
    /// Creates a resilience pipeline from step configuration with typed result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="step">The orchestration step.</param>
    /// <returns>A configured Polly resilience pipeline.</returns>
    ResiliencePipeline<TResult> CreateForStep<TResult>(IOrchestrationStep step);
}