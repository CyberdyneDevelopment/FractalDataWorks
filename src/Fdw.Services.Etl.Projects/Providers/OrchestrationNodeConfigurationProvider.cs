using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Commands;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Providers;

/// <summary>Supplies the OrchestrationNode configuration.</summary>
public sealed class OrchestrationNodeConfigurationProvider
    : DomainConfigurationProviderBase<IOrchestrationNodeImplementationConfiguration>,
      IOrchestrationNodeConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodeConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public OrchestrationNodeConfigurationProvider(
        ILogger<OrchestrationNodeConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "pipe", "OrchestrationNode")
    {
    }
}
