using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Providers;

/// <summary>Supplies the OrchestrationNode implementation's own configuration.</summary>
public sealed class OrchestrationNodeImplementationConfigurationProvider
    : ImplementationProviderBase<OrchestrationNodeImplementationConfiguration, IOrchestrationNodeImplementationConfiguration>,
      IOrchestrationNodeImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodeImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public OrchestrationNodeImplementationConfigurationProvider(
        ILogger<OrchestrationNodeImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "pipe", "OrchestrationNodeImplementation")
    {
    }
}
