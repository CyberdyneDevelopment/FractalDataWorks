using System;
using System.Collections.Generic;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workflows.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Workflows;

/// <summary>Configuration provider for workflow configurations. Thin wrapper over
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>.</summary>
public class WorkflowConfigurationProvider : DefaultConfigurationProvider<WorkflowConfiguration, WorkflowConfigurationCommand>
{
    /// <summary>
    /// Registers the WorkflowConfigurationProvider with DI, targeting this domain's own default
    /// location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<WorkflowConfigurationProvider>(sp =>
            new WorkflowConfigurationProvider(
                sp.GetService<ILogger<WorkflowConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
        services.TryAddSingleton<DefaultConfigurationProvider<WorkflowConfiguration, WorkflowConfigurationCommand>>(
            sp => sp.GetRequiredService<WorkflowConfigurationProvider>());
        services.TryAddSingleton<IServiceConfigurationProvider<WorkflowConfiguration>>(
            sp => sp.GetRequiredService<WorkflowConfigurationProvider>());
    }

    /// <summary>Initializes a new instance of the <see cref="WorkflowConfigurationProvider"/> class.</summary>
    public WorkflowConfigurationProvider(
        ILogger<WorkflowConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "workflow",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<WorkflowConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
