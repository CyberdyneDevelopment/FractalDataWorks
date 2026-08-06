using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Fdw.Orchestration.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Orchestration.Execution;

/// <summary>
/// Default implementation of <see cref="IOrchestrationContext"/>.
/// </summary>
/// <remarks>
/// Provides execution context for orchestration runs, including state management,
/// logging, and access to services.
/// </remarks>
public sealed class OrchestrationExecutionContext : IOrchestrationContext
{
    private readonly ConcurrentDictionary<string, object?> _sharedState;
    private readonly ConcurrentDictionary<string, IOrchestrationStepResult> _completedSteps;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationExecutionContext"/> class.
    /// </summary>
    /// <param name="executionId">The unique execution identifier.</param>
    /// <param name="orchestration">The orchestration being executed.</param>
    /// <param name="services">The service provider for resolving dependencies.</param>
    /// <param name="logger">The logger for this execution.</param>
    /// <param name="cancellationToken">The cancellation token for this execution.</param>
    /// <param name="parameters">Optional input parameters for the execution.</param>
    public OrchestrationExecutionContext(
        Guid executionId,
        IOrchestration orchestration,
        IServiceProvider services,
        ILogger<OrchestrationExecutionContext>? logger,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        ExecutionId = executionId;
        Orchestration = orchestration ?? throw new ArgumentNullException(nameof(orchestration));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Logger = logger ?? NullLogger<OrchestrationExecutionContext>.Instance;
        CancellationToken = cancellationToken;
        Parameters = parameters ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        StartTime = DateTimeOffset.UtcNow;
        _sharedState = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);
        _completedSteps = new ConcurrentDictionary<string, IOrchestrationStepResult>(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public Guid ExecutionId { get; }

    /// <inheritdoc/>
    public IOrchestration Orchestration { get; }

    /// <inheritdoc/>
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc/>
    public ILogger Logger { get; }

    /// <inheritdoc/>
    public IServiceProvider Services { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <inheritdoc/>
    public IDictionary<string, object?> SharedState => _sharedState;

    /// <inheritdoc/>
    public IOrchestrationStep? CurrentStep { get; internal set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, IOrchestrationStepResult> CompletedSteps => _completedSteps;

    /// <inheritdoc/>
    public IExecutionPolicyContext Policy { get; init; } = ExecutionPolicyContext.Default;

    /// <summary>
    /// Records a completed step result.
    /// </summary>
    /// <param name="stepId">The step ID.</param>
    /// <param name="result">The step result.</param>
    internal void RecordStepResult(string stepId, IOrchestrationStepResult result)
    {
        _completedSteps[stepId] = result;
    }

    /// <summary>
    /// Sets the current step being executed.
    /// </summary>
    /// <param name="step">The step to set as current, or null to clear.</param>
    internal void SetCurrentStep(IOrchestrationStep? step)
    {
        CurrentStep = step;
    }
}

/// <summary>
/// Generic implementation of <see cref="IOrchestrationContext{TOrchestration}"/>.
/// </summary>
/// <typeparam name="TOrchestration">The orchestration type.</typeparam>
public sealed class OrchestrationExecutionContext<TOrchestration> : IOrchestrationContext<TOrchestration>
    where TOrchestration : class, IOrchestration
{
    private readonly OrchestrationExecutionContext _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationExecutionContext{TOrchestration}"/> class.
    /// </summary>
    /// <param name="executionId">The unique execution identifier.</param>
    /// <param name="orchestration">The typed orchestration being executed.</param>
    /// <param name="services">The service provider for resolving dependencies.</param>
    /// <param name="logger">The logger for this execution.</param>
    /// <param name="cancellationToken">The cancellation token for this execution.</param>
    /// <param name="parameters">Optional input parameters for the execution.</param>
    public OrchestrationExecutionContext(
        Guid executionId,
        TOrchestration orchestration,
        IServiceProvider services,
        ILogger<OrchestrationExecutionContext>? logger,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        Orchestration = orchestration ?? throw new ArgumentNullException(nameof(orchestration));
        _inner = new OrchestrationExecutionContext(
            executionId,
            orchestration,
            services,
            logger,
            cancellationToken,
            parameters);
    }

    /// <inheritdoc/>
    public TOrchestration Orchestration { get; }

    /// <inheritdoc/>
    IOrchestration IOrchestrationContext.Orchestration => Orchestration;

    /// <inheritdoc/>
    public Guid ExecutionId => _inner.ExecutionId;

    /// <inheritdoc/>
    public DateTimeOffset StartTime => _inner.StartTime;

    /// <inheritdoc/>
    public CancellationToken CancellationToken => _inner.CancellationToken;

    /// <inheritdoc/>
    public ILogger Logger => _inner.Logger;

    /// <inheritdoc/>
    public IServiceProvider Services => _inner.Services;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Parameters => _inner.Parameters;

    /// <inheritdoc/>
    public IDictionary<string, object?> SharedState => _inner.SharedState;

    /// <inheritdoc/>
    public IOrchestrationStep? CurrentStep => _inner.CurrentStep;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, IOrchestrationStepResult> CompletedSteps => _inner.CompletedSteps;

    /// <inheritdoc/>
    public IExecutionPolicyContext Policy { get; init; } = ExecutionPolicyContext.Default;

    /// <summary>
    /// Gets the inner context for internal operations.
    /// </summary>
    internal OrchestrationExecutionContext Inner => _inner;
}
