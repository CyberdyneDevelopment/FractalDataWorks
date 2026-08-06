using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Workflows.Abstractions;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Fdw.Services.Workflows;

/// <summary>
/// TypeCollection for workflow service types.
/// </summary>
/// <remarks>
/// <para>
/// Provides compile-time discovery and O(1) lookup of workflow types.
/// Use <see cref="ByName"/> to look up workflow types by name,
/// or <see cref="All"/> to iterate over all registered workflow types.
/// </para>
/// <para>
/// Workflow types define different coordination engines:
/// <list type="bullet">
/// <item><description><b>Saga</b>: Distributed transactions with compensation</description></item>
/// <item><description><b>StateMachine</b>: State-based workflow orchestration</description></item>
/// <item><description><b>Simple</b>: Sequential step execution</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Look up by name
/// var sagaType = WorkflowTypes.ByName("Saga");
///
/// // Register all workflow types with DI
/// foreach (var type in WorkflowTypes.All())
/// {
///     type.Register(services);
/// }
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(WorkflowTypeBase<IGenericWorkflow, IWorkflowFactory<IGenericWorkflow, WorkflowConfiguration>, WorkflowConfiguration>),
    typeof(IWorkflowType),
    typeof(WorkflowTypes),
    GenerateProvider = true,
    ServiceInterface = typeof(IGenericWorkflow),
    ConfigurationType = typeof(WorkflowConfiguration),
    ProviderType = typeof(DefaultServiceProvider<IGenericWorkflow, WorkflowConfiguration, IWorkflowFactory<IGenericWorkflow, WorkflowConfiguration>, IServiceConfigurationProvider<WorkflowConfiguration>>),
    ProviderInterface = typeof(IFdwServiceProvider<IGenericWorkflow, WorkflowConfiguration>),
    ServiceCategory = "Workflow")]
public partial class WorkflowTypes : ServiceTypeCollectionBase<
    WorkflowTypeBase<IGenericWorkflow, IWorkflowFactory<IGenericWorkflow, WorkflowConfiguration>, WorkflowConfiguration>,
    IWorkflowType<IGenericWorkflow, WorkflowConfiguration, IWorkflowFactory<IGenericWorkflow, WorkflowConfiguration>>>
{
    // Configure(), Register() and Initialize() are source-generated

    /// <summary>
    /// Sets this collection's Register body: the option sweep, then this domain's provider.
    /// </summary>
    /// <remarks>
    /// The provider is one registration for the whole collection and this declaration already names it,
    /// so the body that registers it is written here beside the declaration. Setting it as the phase's
    /// body is what makes it replaceable: an application calling <c>Registration(...)</c> replaces the
    /// sweep and this registration together, which is the correct semantic for a host taking over phase 2.
    /// </remarks>
    static WorkflowTypes()
    {
        var sweepOptions = RegisterFunc;
        Registration((builder, loggerFactory) =>
        {
            sweepOptions(builder, loggerFactory);
            builder.Services.AddScoped<IFdwServiceProvider<IGenericWorkflow, WorkflowConfiguration>>(sp =>
            {
                var provider = new DefaultServiceProvider<IGenericWorkflow, WorkflowConfiguration, IWorkflowFactory<IGenericWorkflow, WorkflowConfiguration>, IServiceConfigurationProvider<WorkflowConfiguration>>(
                    sp,
                    sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultServiceProvider<IGenericWorkflow, WorkflowConfiguration, IWorkflowFactory<IGenericWorkflow, WorkflowConfiguration>, IServiceConfigurationProvider<WorkflowConfiguration>>>()
                    ?? NullLogger<DefaultServiceProvider<IGenericWorkflow, WorkflowConfiguration, IWorkflowFactory<IGenericWorkflow, WorkflowConfiguration>, IServiceConfigurationProvider<WorkflowConfiguration>>>.Instance);
                var stLogger = sp.GetService<ILoggerFactory>()?.CreateLogger("WorkflowTypes");
                try
                {
                    if (sp.GetService<IServiceConfigurationProvider<WorkflowConfiguration>>() is { } cfgProvider)
                    {
                        // Why the result is read: a provider that did not take its parent still constructs, and
                        // every later read silently misses. The failure has to be said out loud here or nowhere.
                        var parentResult = provider.RegisterParentProvider(cfgProvider);
                        if (!parentResult.IsSuccess && stLogger != null)
                            ServiceTypeLog.FactoryRegistrationFailed(stLogger, "WorkflowTypes", parentResult.CurrentMessage ?? "WorkflowTypes");
                    }
                }
                catch (Exception ex)
                {
                    // Why rethrow: a throw here was previously silent, and a provider that failed to take
                    // its parent is unusable in a way that only surfaces much later.
                    if (stLogger != null) ServiceTypeLog.FactoryRegistrationException(stLogger, ex, "WorkflowTypes");
                    throw;
                }
                return provider;
            });
            return builder;
        });
    }
}
