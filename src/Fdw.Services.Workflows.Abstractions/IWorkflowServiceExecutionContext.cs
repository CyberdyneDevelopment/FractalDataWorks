using System;
using System.Collections.Generic;
using System.Threading;

namespace Fdw.Services.Workflows.Abstractions;

/// <summary>
/// Service-layer execution context for workflow operations.
/// </summary>
/// <remarks>
/// Carries correlation, metadata, and step state for a running workflow at the service
/// boundary. Distinguished from <c>Fdw.Orchestration.Workflows.Abstractions.IWorkflowExecutionContext</c>,
/// which is the orchestration-layer context carrying full <see cref="Fdw.Orchestration.Abstractions.IExecutionContext"/> state.
/// </remarks>
// Why: Two types named IWorkflowExecutionContext existed in different namespaces (Services.Workflows vs
// Orchestration.Workflows). Services-layer variant is renamed to IWorkflowServiceExecutionContext to
// eliminate ambiguity for consumers that reference both namespaces.
public interface IWorkflowServiceExecutionContext
{
    /// <summary>
    /// Gets the unique execution identifier.
    /// </summary>
    string ExecutionId { get; }

    /// <summary>
    /// Gets the correlation identifier for distributed tracing.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the execution start time.
    /// </summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the execution metadata.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Gets the step context for the current step.
    /// </summary>
    IWorkflowStepContext? CurrentStep { get; }
}
