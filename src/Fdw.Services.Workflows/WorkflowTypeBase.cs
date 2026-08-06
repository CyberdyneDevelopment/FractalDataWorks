using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fdw.ServiceTypes;
using Fdw.Services.Workflows.Abstractions;

namespace Fdw.Services.Workflows;  // NOT .Abstractions!

/// <summary>
/// Base class for workflow service type definitions that inherit from ServiceTypeBase.
/// </summary>
/// <typeparam name="TService">The workflow service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating workflow service instances.</typeparam>
/// <typeparam name="TConfiguration">The workflow service configuration type.</typeparam>
/// <remarks>
/// <para>
/// Workflow types define the metadata and capabilities of workflow services.
/// Different workflow engines (Saga, StateMachine, Simple) have different implementations
/// with type-specific coordination, compensation, and persistence features.
/// </para>
/// </remarks>
public abstract class WorkflowTypeBase<TService, TFactory, TConfiguration> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IWorkflowType<TService, TConfiguration, TFactory>
    where TService : IGenericWorkflow
    where TFactory : IWorkflowFactory<TService, TConfiguration>
    where TConfiguration : WorkflowConfiguration
{
    /// <summary>
    /// Gets the name of the workflow engine (e.g., "Saga", "StateMachine", "Simple").
    /// </summary>
    public string WorkflowEngine { get; }

    /// <summary>
    /// Gets the executor type used by this workflow service.
    /// </summary>
    public Type ExecutorType { get; }

    /// <summary>
    /// Gets whether this workflow type supports compensation (saga pattern).
    /// </summary>
    public bool SupportsCompensation { get; }

    /// <summary>
    /// Gets whether this workflow type supports parallel step execution.
    /// </summary>
    public bool SupportsParallelExecution { get; }

    /// <summary>
    /// Gets whether this workflow type supports persistence for long-running workflows.
    /// </summary>
    public virtual bool SupportsPersistence => false;

    /// <summary>
    /// Gets whether this workflow type supports conditional branching.
    /// </summary>
    public virtual bool SupportsConditionalBranching => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTypeBase{TService, TFactory, TConfiguration}"/> class.
    /// </summary>
    /// <param name="name">The name of the workflow type.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="workflowEngine">The workflow engine name.</param>
    /// <param name="executorType">The executor type for this workflow.</param>
    /// <param name="supportsCompensation">Whether this type supports compensation.</param>
    /// <param name="supportsParallelExecution">Whether this type supports parallel execution.</param>
    /// <param name="category">The category for this workflow type (defaults to "Workflow").</param>
    protected WorkflowTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string workflowEngine,
        Type executorType,
        bool supportsCompensation,
        bool supportsParallelExecution,
        string? category = null)
        : base(name, sectionName, displayName, description,
               category ?? "Workflow")
    {
        WorkflowEngine = workflowEngine ?? throw new ArgumentNullException(nameof(workflowEngine));
        ExecutorType = executorType ?? throw new ArgumentNullException(nameof(executorType));
        SupportsCompensation = supportsCompensation;
        SupportsParallelExecution = supportsParallelExecution;
    }

}
