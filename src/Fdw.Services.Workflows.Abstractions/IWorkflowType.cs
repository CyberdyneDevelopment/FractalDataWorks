using Fdw.Configuration;
using System;
using Fdw.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Workflows.Abstractions;

/// <summary>
/// Interface for workflow service types.
/// </summary>
/// <typeparam name="TService">The workflow service type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the workflow service.</typeparam>
/// <typeparam name="TFactory">The factory type for creating workflow service instances.</typeparam>
public interface IWorkflowType<TService, TConfiguration, TFactory> : IServiceType<Guid, TService, TFactory, TConfiguration>, IWorkflowType
    where TService : IGenericWorkflow
    where TConfiguration : IGenericConfiguration
    where TFactory : IWorkflowFactory<TService, TConfiguration>
{
}

/// <summary>
/// Non-generic interface for workflow service types.
/// </summary>
public interface IWorkflowType : IServiceType
{
    /// <summary>
    /// Gets the name of the workflow engine (e.g., "Saga", "StateMachine", "Simple").
    /// </summary>
    string WorkflowEngine { get; }

    /// <summary>
    /// Gets the executor type used by this workflow service.
    /// </summary>
    Type ExecutorType { get; }

    /// <summary>
    /// Gets whether this workflow type supports compensation (saga pattern).
    /// </summary>
    bool SupportsCompensation { get; }

    /// <summary>
    /// Gets whether this workflow type supports parallel step execution.
    /// </summary>
    bool SupportsParallelExecution { get; }

    /// <summary>
    /// Gets whether this workflow type supports persistence for long-running workflows.
    /// </summary>
    bool SupportsPersistence { get; }

    /// <summary>
    /// Gets whether this workflow type supports conditional branching.
    /// </summary>
    bool SupportsConditionalBranching { get; }
}
