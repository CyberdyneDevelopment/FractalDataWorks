using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations;

/// <summary>Reads and writes what a threshold carries (FDW-3936).</summary>
public sealed class ThresholdImplementationConfigurationProvider
    : ImplementationProviderBase<ThresholdImplementationConfiguration, IThresholdImplementationConfiguration>,
      IThresholdImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The data store.</param>
    /// <param name="pathName">The path (schema).</param>
    /// <param name="containerName">The table.</param>
    public ThresholdImplementationConfigurationProvider(
        ILogger<ThresholdImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
