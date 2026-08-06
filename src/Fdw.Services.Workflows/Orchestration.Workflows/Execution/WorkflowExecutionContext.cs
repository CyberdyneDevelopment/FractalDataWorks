using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Orchestration.Abstractions;
using Fdw.Orchestration.Workflows.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Orchestration.Workflows.Execution;

/// <summary>
/// Concrete implementation of workflow execution context.
/// </summary>
public sealed class WorkflowExecutionContext : IWorkflowExecutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowExecutionContext"/> class.
    /// </summary>
    public WorkflowExecutionContext(
        string workflowId,
        string triggeredBy,
        IServiceProvider? services = null,
        ILogger<WorkflowExecutionContext>? logger = null,
        IReadOnlyDictionary<string, object?>? parameters = null,
        string? correlationId = null,
        bool isDryRun = false,
        IExecutionPolicyContext? policy = null,
        CancellationToken cancellationToken = default)
    {
        ExecutionId = Guid.NewGuid();
        WorkflowId = workflowId;
        TriggeredBy = triggeredBy;
        Services = services ?? EmptyServiceProvider.Instance;
        Logger = logger ?? NullLogger<WorkflowExecutionContext>.Instance;
        StartTime = DateTimeOffset.UtcNow;
        Parameters = parameters ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        SharedState = new Dictionary<string, object?>(StringComparer.Ordinal);
        CorrelationId = correlationId ?? ExecutionId.ToString();
        IsDryRun = isDryRun;
        Policy = policy ?? ExecutionPolicyContext.Default;
        CancellationToken = cancellationToken;
    }

    /// <inheritdoc/>
    public Guid ExecutionId { get; }

    /// <inheritdoc/>
    public string WorkflowId { get; }

    /// <inheritdoc/>
    public string TriggeredBy { get; }

    /// <inheritdoc/>
    public IServiceProvider Services { get; }

    /// <inheritdoc/>
    public ILogger Logger { get; }

    /// <inheritdoc/>
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <inheritdoc/>
    public IDictionary<string, object?> SharedState { get; }

    /// <inheritdoc/>
    public string? CorrelationId { get; }

    /// <inheritdoc/>
    public bool IsDryRun { get; }

    /// <inheritdoc/>
    public IExecutionPolicyContext Policy { get; }

    /// <inheritdoc/>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Minimal service provider returned when no DI container is available.
    /// </summary>
    // Why: WorkflowExecutionContext may be constructed without a DI container in
    // lightweight scenarios (e.g. unit tests). NullServiceProvider prevents null
    // reference exceptions while making the absence of services explicit via
    // GetService returning null.
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        /// <summary>Gets the singleton instance.</summary>
        public static readonly EmptyServiceProvider Instance = new();

        /// <inheritdoc/>
        public object? GetService(Type serviceType) => null;
    }
}
