using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Base interface for all orchestration types (pipelines, workflows, sagas, etc.).
/// </summary>
/// <remarks>
/// An orchestration coordinates the execution of multiple steps in a defined order
/// with support for error handling, retry logic, and state management.
/// </remarks>
public interface IOrchestration
{
    /// <summary>
    /// Gets the unique identifier for this orchestration definition.
    /// </summary>
    string OrchestrationId { get; }

    /// <summary>
    /// Gets the name of this orchestration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this orchestration does.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the version of this orchestration definition.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the execution phases (steps/stages/tasks) that make up this orchestration.
    /// </summary>
    /// <remarks>
    /// Implementations should explicitly implement this property and return their typed phases.
    /// For example, pipelines return Stages, workflows return Steps.
    /// </remarks>
    IReadOnlyList<IOrchestrationStep> Phases { get; }

    /// <summary>
    /// Gets a value indicating whether this orchestration is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the tags associated with this orchestration for categorization.
    /// </summary>
    IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets metadata associated with this orchestration.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Validates the orchestration definition.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the orchestration is valid.</returns>
    Task<IGenericResult> Validate(CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic orchestration interface with typed phase.
/// </summary>
/// <typeparam name="TPhase">The phase type (step, stage, task, etc.).</typeparam>
/// <remarks>
/// Different orchestration types use different terminology for their execution units:
/// - Pipelines use "stages"
/// - Workflows use "steps"
/// - Jobs use "tasks"
/// This generic interface unifies them as "phases" while preserving domain-specific terminology
/// through explicit interface implementations.
/// </remarks>
public interface IOrchestration<TPhase> : IOrchestration
    where TPhase : IOrchestrationStep
{
    /// <summary>
    /// Gets the typed phases (steps/stages/tasks) that make up this orchestration.
    /// </summary>
    new IReadOnlyList<TPhase> Phases { get; }
}

