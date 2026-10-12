using Fdw.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;
using Fdw.Results;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Base interface for all orchestration steps.
/// </summary>
/// <remarks>
/// A step is a single unit of work within an orchestration. Steps can have
/// dependencies on other steps, error handling policies, and timeout settings.
/// </remarks>
public interface IOrchestrationStep
{
    /// <summary>
    /// Gets the unique identifier for this step within the orchestration.
    /// </summary>
    string StepId { get; }

    /// <summary>
    /// Gets the name of this step.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this step does.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the sequence number for ordering steps.
    /// </summary>
    int SequenceNumber { get; }

    /// <summary>
    /// Gets the list of step IDs that must complete before this step can execute.
    /// </summary>
    IReadOnlyList<string> DependsOn { get; }

    /// <summary>
    /// Gets a value indicating whether this step can run in parallel with other steps.
    /// </summary>
    bool AllowParallel { get; }

    /// <summary>
    /// Gets the error handling mode for this step.
    /// </summary>
    IErrorHandlingMode? ErrorHandling { get; }

    /// <summary>
    /// Gets the maximum execution time for this step.
    /// </summary>
    TimeSpan? Timeout { get; }

    /// <summary>
    /// Gets a value indicating whether this step's result can be cached.
    /// </summary>
    /// <remarks>
    /// Cacheable steps are idempotent - running them multiple times with the
    /// same input produces the same output.
    /// </remarks>
    bool IsCacheable { get; }

    /// <summary>
    /// Gets a value indicating whether this step is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the step-specific configuration.
    /// </summary>
    IGenericConfiguration? Configuration { get; }

    /// <summary>
    /// Gets metadata associated with this step.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Validates the step configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the step is valid.</returns>
    Task<IGenericResult> Validate(CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic step interface with typed configuration.
/// </summary>
/// <typeparam name="TConfiguration">The configuration type.</typeparam>
public interface IOrchestrationStep<TConfiguration> : IOrchestrationStep
    where TConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets the typed configuration for this step.
    /// </summary>
    new TConfiguration? Configuration { get; }
}
