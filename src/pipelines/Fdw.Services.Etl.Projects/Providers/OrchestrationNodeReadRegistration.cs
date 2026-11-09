using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Memberships;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Providers;

/// <summary>
/// The registrations of the read side of the orchestration nodes: the node providers and the read of which projects run each pipeline (FDW-4449).
/// </summary>
/// <remarks>
/// Two options register it, the run side that starts the orchestrator (<c>DefaultOrchestrationServiceType</c>) and the read side that does not
/// (<c>OrchestrationNodesServiceType</c>), and the order options register in is not controlled. Every line is therefore a TryAdd, and a host that runs both gets one
/// of each. It lives here, beside the providers it names, because it is the one place that can name them without the hosted service.
/// </remarks>
public static class OrchestrationNodeReadRegistration
{
    /// <summary>Registers the node providers and the project memberships read, leaving anything already registered as it is.</summary>
    /// <param name="services">The host's service collection.</param>
    public static void Register(IServiceCollection services)
    {
        services.TryAddSingleton<IOrchestrationNodeImplementationConfigurationProvider>(sp => new OrchestrationNodeImplementationConfigurationProvider(
            sp.GetRequiredService<ILogger<OrchestrationNodeImplementationConfigurationProvider>>(),
            sp.GetRequiredService<IConfigurationGatewayProvider>(), PipelineServiceTypes.ConfigurationConnection, "pipe", "OrchestrationNodeConfiguration"));
        services.TryAddSingleton<OrchestrationNodeConfigurationProvider>(sp =>
        {
            var domain = new OrchestrationNodeConfigurationProvider(
                sp.GetRequiredService<ILogger<OrchestrationNodeConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(), PipelineServiceTypes.ConfigurationConnection, "pipe", "OrchestrationNode");
            domain.Register("OrchestrationNode", sp.GetRequiredService<IOrchestrationNodeImplementationConfigurationProvider>());
            return domain;
        });
        services.TryAddSingleton<IOrchestrationNodeConfigurationProvider>(sp => sp.GetRequiredService<OrchestrationNodeConfigurationProvider>());
        services.TryAddScoped<IPipelineProjectMemberships, PipelineProjectMemberships>();
    }
}
