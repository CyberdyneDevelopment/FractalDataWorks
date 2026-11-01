using Polly;
using Fdw.Results;
using Fdw.Services.Resiliency.Abstractions;

namespace Fdw.Services.Resiliency.Factories;

/// <summary>
/// Factory interface for creating Polly ResiliencePipeline instances from policy configurations.
/// </summary>
/// <remarks>
/// The factory is a singleton that caches pipelines by policy name for efficient reuse.
/// Pipelines are created lazily on first request and stored in a thread-safe cache.
/// </remarks>
public interface IResiliencyPipelineFactory
{
    /// <summary>
    /// Gets or creates a ResiliencePipeline for the specified policy.
    /// </summary>
    /// <param name="policy">The resiliency policy configuration.</param>
    /// <param name="operationName">Optional operation name for logging context.</param>
    /// <returns>
    /// A result containing the ResiliencePipeline if successful,
    /// or an error message if pipeline creation fails.
    /// </returns>
    IGenericResult<ResiliencePipeline> GetOrCreate(
        IResiliencyPolicy policy,
        string? operationName = null);

    /// <summary>
    /// Gets or creates a ResiliencePipeline for the specified policy name.
    /// </summary>
    /// <param name="policyName">The name of the resiliency policy (e.g., "Database", "HttpClient").</param>
    /// <param name="operationName">Optional operation name for logging context.</param>
    /// <returns>
    /// A result containing the ResiliencePipeline if successful,
    /// or an error message if the policy is not found or pipeline creation fails.
    /// </returns>
    IGenericResult<ResiliencePipeline> GetOrCreate(
        string policyName,
        string? operationName = null);

    /// <summary>
    /// Gets or creates a generic ResiliencePipeline{TResult} for the specified policy.
    /// </summary>
    /// <typeparam name="TResult">The result type for the pipeline.</typeparam>
    /// <param name="policy">The resiliency policy configuration.</param>
    /// <param name="operationName">Optional operation name for logging context.</param>
    /// <returns>
    /// A result containing the ResiliencePipeline if successful,
    /// or an error message if pipeline creation fails.
    /// </returns>
    IGenericResult<ResiliencePipeline<TResult>> GetOrCreatePipeline<TResult>(
        IResiliencyPolicy policy,
        string? operationName = null);

    /// <summary>
    /// Gets or creates a generic ResiliencePipeline{TResult} for the specified policy name.
    /// </summary>
    /// <typeparam name="TResult">The result type for the pipeline.</typeparam>
    /// <param name="policyName">The name of the resiliency policy (e.g., "Database", "HttpClient").</param>
    /// <param name="operationName">Optional operation name for logging context.</param>
    /// <returns>
    /// A result containing the ResiliencePipeline if successful,
    /// or an error message if the policy is not found or pipeline creation fails.
    /// </returns>
    IGenericResult<ResiliencePipeline<TResult>> GetOrCreatePipeline<TResult>(
        string policyName,
        string? operationName = null);

    /// <summary>
    /// Clears all cached pipelines.
    /// </summary>
    /// <remarks>
    /// Use this method when configuration changes and pipelines need to be recreated.
    /// </remarks>
    void ClearCache();
}
