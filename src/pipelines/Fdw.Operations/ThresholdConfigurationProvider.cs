using Fdw.Operations.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations;

/// <summary>The thresholds of a tenant, by name or Id (FDW-3936).</summary>
public sealed class ThresholdConfigurationProvider
    : DomainConfigurationProviderBase<IThresholdImplementationConfiguration>,
      IThresholdConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The data store.</param>
    /// <param name="pathName">The path (schema).</param>
    /// <param name="containerName">The domain table.</param>
    public ThresholdConfigurationProvider(
        ILogger<ThresholdConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
