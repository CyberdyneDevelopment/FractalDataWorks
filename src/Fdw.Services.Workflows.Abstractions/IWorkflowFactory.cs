using Fdw.Configuration;
using Fdw.Abstractions;

namespace Fdw.Services.Workflows.Abstractions;

/// <summary>
/// Marker interface for workflow factories.
/// </summary>
public interface IWorkflowFactory
{
}

/// <summary>
/// Generic interface for workflow factories with typed configuration.
/// </summary>
/// <typeparam name="TService">The type of workflow this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this factory requires.</typeparam>
public interface IWorkflowFactory<TService, TConfiguration> : IWorkflowFactory, IServiceFactory<TService, TConfiguration>
    where TService : IGenericWorkflow
    where TConfiguration : IGenericConfiguration
{
}
