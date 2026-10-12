using System;
using Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Policy context — the rules that govern a single execution run.
/// Composed under <see cref="IOrchestrationContext.Policy"/> so the executor reads
/// policy values from a single dedicated context instead of duplicating them on
/// every execution-scope interface.
/// </summary>
/// <remarks>
/// Resiliency (retry / circuit-breaker / backoff) lives on its own
/// <c>IResiliencyPolicy</c> TypeOption referenced from this context. Per-execution
/// error-handling and caching policies that don't fit the TypeOption shape live
/// directly here.
/// </remarks>
public interface IExecutionPolicyContext
{
    /// <summary>
    /// Gets the default error-handling mode for steps that don't specify their own.
    /// </summary>
    IErrorHandlingMode? DefaultErrorHandling { get; }

    /// <summary>
    /// Gets a value indicating whether execution continues past a step failure.
    /// </summary>
    bool ContinueOnFailure { get; }

    /// <summary>
    /// Gets the duration step results are cached when caching is enabled.
    /// </summary>
    TimeSpan? ResultCacheDuration { get; }
}
