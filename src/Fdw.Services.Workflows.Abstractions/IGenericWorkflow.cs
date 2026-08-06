using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Orchestration.Workflows.Abstractions;
using Fdw.Results;
using Fdw.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Workflows.Abstractions;

/// <summary>
/// Service interface for workflow execution and management.
/// </summary>
/// <remarks>
/// Workflow services provide execution capabilities for business workflows.
/// Each workflow type (saga, state machine, etc.) may have its own service implementation
/// with type-specific coordination and compensation features.
/// </remarks>
public interface IGenericWorkflow : IDisposable, IServiceOption
{
    /// <summary>
    /// Executes a workflow.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the execution result.</returns>
    Task<IGenericResult<IWorkflowExecutionResult>> Execute(
        IWorkflow workflow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a workflow with custom context.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the execution result.</returns>
    Task<IGenericResult<IWorkflowExecutionResult>> Execute(
        IWorkflow workflow,
        IWorkflowServiceExecutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow without executing it.
    /// </summary>
    /// <param name="workflow">The workflow to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating validation success or failure with details.</returns>
    Task<IGenericResult> Validate(
        IWorkflow workflow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates a failed workflow execution.
    /// </summary>
    /// <param name="executionId">The execution ID to compensate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating compensation success or failure.</returns>
    Task<IGenericResult> Compensate(
        string executionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed workflow service interface with configuration.
/// </summary>
/// <typeparam name="TConfiguration">The workflow service configuration type.</typeparam>
public interface IGenericWorkflow<TConfiguration> : IGenericWorkflow
    where TConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    TConfiguration Configuration { get; }
}