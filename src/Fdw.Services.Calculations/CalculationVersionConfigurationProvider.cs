using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations;

/// <summary>The provider of the versions of calculations held at organizations and workspaces (FDW-2560).</summary>
public sealed class CalculationVersionConfigurationProvider
    : DomainConfigurationProviderBase<ICalculationVersionImplementationConfiguration, CalculationVersionDomainConfiguration>,
      ICalculationVersionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="CalculationVersionConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">Reaches the data store.</param>
    /// <param name="dataStoreName">The data store the rows are held in.</param>
    /// <param name="pathName">The path of the container.</param>
    /// <param name="containerName">The container of the domain rows.</param>
    public CalculationVersionConfigurationProvider(
        ILogger<CalculationVersionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
